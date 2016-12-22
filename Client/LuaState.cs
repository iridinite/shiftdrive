﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents an instance of the Lua runtime.
    /// </summary>
    internal sealed class LuaState : IDisposable {

        internal readonly IntPtr L;

        private readonly Dictionary<string, int> compiledfns;
        private readonly Dictionary<int, int> events;
        private readonly List<lua_CFunction> bigHeapODelegates;
        private lua_CFunction paniccallback, errhandlerfn;

        public LuaState() {
            compiledfns = new Dictionary<string, int>();
            events = new Dictionary<int, int>();
            bigHeapODelegates = new List<lua_CFunction>();

            paniccallback = new lua_CFunction(clua_panic);
            errhandlerfn = new lua_CFunction(lua_errorhandler);

            L = LuaAPI.luaL_newstate();
            LuaAPI.lua_atpanic(L, paniccallback);
            PushDelegate(LuaAPI.luaopen_base);
            LuaAPI.lua_call(L, 0, 0);
            PushDelegate(LuaAPI.luaopen_table);
            LuaAPI.lua_call(L, 0, 0);
            PushDelegate(LuaAPI.luaopen_math);
            LuaAPI.lua_call(L, 0, 0);
            PushDelegate(LuaAPI.luaopen_string);
            LuaAPI.lua_call(L, 0, 0);

            // generic functions
            RegisterFunction("require", clua_require);
            RegisterFunction("print", clua_print);
            RegisterFunction("now", clua_sinceepoch);

            RegisterFunction("localize", clua_localize);
            RegisterFunction("phrase", clua_phrase);

            RegisterFunction("lshift", clua_lshift);
            RegisterFunction("rshift", clua_rshift);

            RegisterFunction("Event", clua_event);
            RegisterFunction("Create", clua_create);
            RegisterFunction("GetObjectByName", clua_getObjectByName);
            RegisterFunction("GetObjectById", clua_getObjectById);
            RegisterFunction("GetPlayerShip", clua_getPlayer);

            if (LuaAPI.lua_gettop(L) != 0) {
                LuaAPI.lua_pushstring(L, "unbalanced stack after Lua state initialization");
                LuaAPI.lua_error(L);
                return;
            }

#if DEBUG
            GC.Collect();
#endif
        }

        public void Dispose() {
            LuaAPI.lua_close(L);
        }

        public void Print(string s, bool error = false) {
            SDGame.Inst.Print(s, error);
        }

        public void Precompile() {
            LuaAPI.lua_createtable(L, 0, 0);

            DirectoryInfo dir = new DirectoryInfo("Data");
            IOrderedEnumerable<FileInfo> files = dir.GetFiles("*.lua", SearchOption.AllDirectories).OrderBy(f => f.Name);
            foreach (FileInfo file in files) {
                try {
                    // load the script file into a string
                    string script;
                    using (StreamReader r = new StreamReader(file.FullName)) {
                        script = r.ReadToEnd();
                    }
                    // strip the script file name of the scripts folder path and the lua extension
                    string scriptname = file.FullName.Substring(dir.FullName.Length + 1);
                    scriptname = scriptname.Substring(0, scriptname.Length - file.Extension.Length).Replace('\\', '/');
                    // compile the string as a Lua chunk
                    if (LuaAPI.luaL_loadstringex(L, script, scriptname) != 0)
                        throw new LuaException($"Failed to compile script {file.Name}: {LuaAPI.lua_tostring(L, -1)}");
                    // insert the compiled Lua function into the table and obtain a reference integer
                    compiledfns.Add(scriptname, LuaAPI.luaL_ref(L, -2));

                } catch (IOException e) {
                    throw new LuaException($"I/O error occurred while compiling {file.Name}: {e}");
                }
            }
            // save the table of compiled functions into the Lua registry
            LuaAPI.lua_setregistry(L, "precompiled");
        }

        public void PushDelegate(lua_CFunction fn) {
            // this list has no function other than to maintain a reference to the delegate,
            // so the garbage collector won't destroy it (it can't see the unmanaged ref in Lua)
            if (!bigHeapODelegates.Contains(fn)) bigHeapODelegates.Add(fn);
            LuaAPI.lua_pushcclosure(L, fn, 0);
        }

        public void RegisterFunction(string name, lua_CFunction fn) {
            PushDelegate(fn);
            LuaAPI.lua_setglobal(L, name);
        }

        public void LoadFile(string filename) {
            if (!compiledfns.ContainsKey(filename))
                throw new LuaException(LuaAPI.lua_getwhere(L, 0) + "no such script exists '" + filename + "'");

            LuaAPI.lua_getregistry(L, "precompiled");
            LuaAPI.lua_rawgeti(L, -1, compiledfns[filename]);
            LuaAPI.lua_insert(L, -2);
            LuaAPI.lua_pop(L, 1);
        }

#if DEBUG
        internal string StackDump() {
            StringBuilder sb = new StringBuilder();
            int top = LuaAPI.lua_gettop(L);
            sb.Append(">> Stack dump (");
            sb.Append(top.ToString() + " items): ");

            for (int i = 1; i <= top; i++) {
                switch (LuaAPI.lua_type(L, i)) {
                    case LuaAPI.LUA_TSTRING:
                        sb.Append("string: \"" + LuaAPI.lua_tostring(L, i) + "\"");
                        break;
                    case LuaAPI.LUA_TBOOLEAN:
                        sb.Append("boolean: " + (LuaAPI.lua_toboolean(L, i) == 1 ? "true" : "false"));
                        break;
                    case LuaAPI.LUA_TNUMBER:
                        sb.Append("number: " + LuaAPI.lua_tonumber(L, i).ToString());
                        break;
                    default:
                        sb.Append(LuaAPI.lua_typename(L, LuaAPI.lua_type(L, i)));
                        break;
                }
                sb.Append(", ");
            }

            sb.AppendLine("<< End stack");
            return sb.ToString();
        }

        internal int GetEventCount() {
            return events.Count;
        }
#endif

        public void Call(int nargs, int nresults) {
            // push error handler and push it below the other items
            LuaAPI.lua_pushcclosure(L, errhandlerfn, 0);
            LuaAPI.lua_insert(L, 1);

            // transfer control to Lua
            if (LuaAPI.lua_pcall(L, nargs, nresults, 1) != 0) {
                // error handler: lua_errorhandler should have appended a stack trace to the error message
                string errmsg = "Lua error: " + LuaAPI.lua_tostring(L, -1).Replace("\t", "  ");
                LuaAPI.lua_remove(L, 1); // pop error handler
                LuaAPI.lua_pop(L, 1); // pop error string
                throw new LuaException(errmsg);
            }

            // pop the error handler
            LuaAPI.lua_remove(L, 1);
        }

        public void RunEvents() {
            // pull up the events table
            LuaAPI.lua_settop(L, 0);
            LuaAPI.lua_getregistry(L, "events");
            if (LuaAPI.lua_type(L, -1) != LuaAPI.LUA_TTABLE) {
                // if it doesn't exist, there's nothing for us to do
                LuaAPI.lua_pop(L, 1);
                return;
            }
            // iterate through the list of events
            foreach (var pair in events) {
                try {
                    // first find the conditional function and run it
                    LuaAPI.lua_rawgeti(L, -1, pair.Key);
                    Call(0, 1);
                    if (LuaAPI.lua_toboolean(L, -1) == 1) {
                        // the condition evaluates to true, so run the action
                        LuaAPI.lua_pop(L, 1);
                        LuaAPI.lua_rawgeti(L, -1, pair.Value);
                        Call(0, 0);
                    } else {
                        // just pop the boolean result
                        LuaAPI.lua_pop(L, 1);
                    }

                } catch (LuaException e) {
                    // remove the failing event from the list. make sure to break out
                    // because we're modifying the collection of events
                    SDGame.Logger.LogError("Event Error: " + e.Message);
                    LuaAPI.lua_settop(L, 1);
                    LuaAPI.luaL_unref(L, -1, pair.Key);
                    LuaAPI.luaL_unref(L, -1, pair.Value);
                    LuaAPI.lua_pop(L, 1);
                    events.Remove(pair.Key);
                    break;
                }
            }
            // remove the registry table
            LuaAPI.lua_pop(L, 1);
        }

        private int clua_panic(IntPtr L) {
            Logger.WriteExceptionReport(new ApplicationException("Lua Panic! Error in unprotected environment: " + LuaAPI.lua_tostring(L, -1)));
            // I don't know if the string should be popped or not. Then again, it probably doesn't really matter
            // because if Lua panics then the game will crash as soon as we return from this handler anyway.

            //lua_pop(state, 1); // pop the error message from the stack
            throw new LuaException("Lua panic: " + LuaAPI.lua_tostring(L, -1));
        }

        private int lua_errorhandler(IntPtr L) {
            // make sure we got the error message
            if (LuaAPI.lua_isstring(L, 1) == 0) return 0;

            string errmsg = LuaAPI.lua_tostring(L, 1);
            LuaAPI.luaL_traceback(L, L, errmsg, 2);

            return 1;
        }

        private int clua_print(IntPtr L) {
            Print(LuaAPI.luaL_checkstring(L, 1));
            return 0;
        }

        private int clua_require(IntPtr L) {
            int basestack = LuaAPI.lua_gettop(L);
            LoadFile(LuaAPI.luaL_checkstring(L, 1));
            Call(0, -1);
            return LuaAPI.lua_gettop(L) - basestack;
        }

        private int clua_sinceepoch(IntPtr L) {
            TimeSpan t = new TimeSpan(DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks);
            LuaAPI.lua_pushnumber(L, t.TotalSeconds);
            return 1;
        }

        private int clua_lshift(IntPtr L) {
            LuaAPI.lua_pushnumber(L, (int)LuaAPI.luaL_checknumber(L, 1) << (int)LuaAPI.luaL_checknumber(L, 2));
            return 1;
        }


        private int clua_rshift(IntPtr L) {
            LuaAPI.lua_pushnumber(L, (int)LuaAPI.luaL_checknumber(L, 1) >> (int)LuaAPI.luaL_checknumber(L, 2));
            return 1;
        }

        private int clua_localize(IntPtr L) {
            LuaAPI.lua_pushstring(L, Utils.LocaleGet(LuaAPI.luaL_checkstring(L, 1)));
            return 1;
        }

        private int clua_phrase(IntPtr L) {
            LuaAPI.lua_pushstring(L, Utils.LocalePhrase(LuaAPI.luaL_checkstring(L, 1)));
            return 1;
        }

        private int clua_event(IntPtr L) {
            // expect two function arguments
            LuaAPI.luaL_checktype(L, 1, LuaAPI.LUA_TFUNCTION);
            LuaAPI.luaL_checktype(L, 2, LuaAPI.LUA_TFUNCTION);

            // pull up the event registry table, create if it doesn't exist
            LuaAPI.lua_getregistry(L, "events");
            if (LuaAPI.lua_type(L, -1) != LuaAPI.LUA_TTABLE) {
                LuaAPI.lua_pop(L, 1); // get rid of the faux events table
                LuaAPI.lua_createtable(L, 0, 0); // create a new table
            }

            // push copies of the two passed functions on top of the stack
            LuaAPI.lua_pushvalue(L, 2);
            LuaAPI.lua_pushvalue(L, 1);
            // then assign them reference numbers and pop the copies
            int refCond = LuaAPI.luaL_ref(L, -3);
            int refActn = LuaAPI.luaL_ref(L, -2);

            // save the registry table
            LuaAPI.lua_setregistry(L, "events");

            // keep the reference integers around
            events.Add(refCond, refActn);
            return 0;
        }

        private int clua_create(IntPtr L) {
            GameObject newobj = null;
            string objtype = LuaAPI.luaL_checkstring(L, 1);

            // -- Named Objects --
            if (objtype.Equals("player", StringComparison.InvariantCultureIgnoreCase)) {
                // Player Ship
                PlayerShip newship = new PlayerShip(NetServer.world);
                newobj = newship;

            } else if (objtype.Equals("ship", StringComparison.InvariantCultureIgnoreCase)) {
                // NPC Ship
                AIShip newship = new AIShip(NetServer.world);
                newobj = newship;

            } else if (objtype.Equals("station", StringComparison.InvariantCultureIgnoreCase)) {
                // Space Station
                newobj = new SpaceStation(NetServer.world);

            } else if (objtype.Equals("blackhole", StringComparison.InvariantCultureIgnoreCase)) {
                // Black Hole
                newobj = new BlackHole(NetServer.world);

            // -- Nameless Objects --
            } else if (objtype.Equals("asteroid", StringComparison.InvariantCultureIgnoreCase)) {
                // Asteroid Belt
                // require metadata table as the second parameter
                LuaAPI.luaL_checktype(L, 2, LuaAPI.LUA_TTABLE);
                LuaAPI.lua_getfield(L, 2, "startpoint");
                LuaAPI.lua_getfield(L, 2, "endpoint");
                LuaAPI.lua_checkfieldtype(L, 2, "startpoint", 3, LuaAPI.LUA_TTABLE);
                LuaAPI.lua_checkfieldtype(L, 2, "endpoint", 4, LuaAPI.LUA_TTABLE);
                Vector2 start = LuaAPI.lua_tovec2(L, 3);
                Vector2 end = LuaAPI.lua_tovec2(L, 4);
                LuaAPI.lua_pop(L, 2); // remove the table fields from the stack
                int range = LuaAPI.luaH_gettableint(L, 2, "range");
                int count = LuaAPI.luaH_gettableint(L, 2, "count");

                // now that we parsed the Lua table's info, we can create the objects
                Vector2 increment = (end - start) / (count - 1);
                for (int i = 0; i < count; i++) {
                    Asteroid rock = new Asteroid(NetServer.world);
                    rock.position = start + (increment * i) + // base movement along the start-end line, plus random range
                                    new Vector2(Utils.RNG.Next(range * 2) - range, Utils.RNG.Next(range * 2) - range);
                    NetServer.world.AddObject(rock);
                }
                return 0;

            } else {
                LuaAPI.lua_pushstring(L, "Unknown object type '" + objtype + "'");
                LuaAPI.lua_error(L);
                return 0;
            }

            // in the case of a named object, make sure to push it to Lua and the server state
            // nameless object creations will create several instances, so don't bother returning just one
            NetServer.world.AddObject(newobj);
            newobj.PushToLua(L);

            return 1;
        }

        private int clua_getObjectByName(IntPtr L) {
            string name = LuaAPI.luaL_checkstring(L, 1);
            foreach (var pair in NetServer.world.Objects) {
                // check if this object is a matching named object
                NamedObject nobj = pair.Value as NamedObject;
                if (nobj == null || !nobj.nameshort.Equals(name)) continue;

                nobj.PushToLua(L);
                return 1;
            }
            // no result found, return nil
            LuaAPI.lua_pushnil(L);
            return 1;
        }

        private int clua_getObjectById(IntPtr L) {
            uint id = (uint)LuaAPI.luaL_checknumber(L, 1);
            foreach (var pair in NetServer.world.Objects) {
                if (pair.Value.id != id) continue;
                pair.Value.PushToLua(L);
                return 1;
            }
            // no result found, return nil
            LuaAPI.lua_pushnil(L);
            return 1;
        }

        private int clua_getPlayer(IntPtr L) {
            PlayerShip plr = NetServer.world.GetPlayerShip();
            if (plr == null)
                LuaAPI.lua_pushnil(L);
            else
                plr.PushToLua(L);
            return 1;
        }

    }

}