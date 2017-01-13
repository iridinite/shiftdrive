/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using static ShiftDrive.LuaAPI;

namespace ShiftDrive {

    /// <summary>
    /// Represents an instance of the Lua runtime.
    /// </summary>
    internal sealed class LuaState : IDisposable {

        /// <summary>
        /// Describes creation parameters for a set of nameless objects.
        /// </summary>
        private struct NamelessObjectParams {
            public Vector2 startpoint;
            public Vector2 endpoint;
            public Vector2 increment;
            public float range;
            public int count;
        }

        internal readonly IntPtr L;

        private readonly Dictionary<string, int> compiledfns;
        private readonly Dictionary<int, int> events;
        private readonly List<lua_CFunction> bigHeapODelegates;
        private lua_CFunction paniccallback, errhandlerfn;

        public LuaState() {
            compiledfns = new Dictionary<string, int>();
            events = new Dictionary<int, int>();
            bigHeapODelegates = new List<lua_CFunction>();

            paniccallback = clua_panic;
            errhandlerfn = lua_errorhandler;

            L = luaL_newstate();
            lua_atpanic(L, paniccallback);
            PushDelegate(luaopen_base);
            lua_call(L, 0, 0);
            PushDelegate(luaopen_table);
            lua_call(L, 0, 0);
            PushDelegate(luaopen_math);
            lua_call(L, 0, 0);
            PushDelegate(luaopen_string);
            lua_call(L, 0, 0);

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

            if (lua_gettop(L) != 0) {
                lua_pushstring(L, "unbalanced stack after Lua state initialization");
                lua_error(L);
                return;
            }

#if DEBUG
            GC.Collect();
#endif
        }

        public void Dispose() {
            lua_close(L);
        }

        public void Print(string s, bool error = false) {
            SDGame.Inst.Print(s, error);
        }

        public void Precompile() {
            lua_createtable(L, 0, 0);

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
                    if (luaL_loadstringex(L, script, scriptname) != 0)
                        throw new LuaException($"Failed to compile script {file.Name}: {lua_tostring(L, -1)}");
                    // insert the compiled Lua function into the table and obtain a reference integer
                    compiledfns.Add(scriptname, luaL_ref(L, -2));

                } catch (IOException e) {
                    throw new LuaException($"I/O error occurred while compiling {file.Name}: {e}");
                }
            }
            // save the table of compiled functions into the Lua registry
            lua_setregistry(L, "precompiled");
        }

        public void PushDelegate(lua_CFunction fn) {
            // this list has no function other than to maintain a reference to the delegate,
            // so the garbage collector won't destroy it (it can't see the unmanaged ref in Lua)
            if (!bigHeapODelegates.Contains(fn)) bigHeapODelegates.Add(fn);
            lua_pushcclosure(L, fn, 0);
        }

        public void RegisterFunction(string name, lua_CFunction fn) {
            PushDelegate(fn);
            lua_setglobal(L, name);
        }

        private NamelessObjectParams GetNamelessParams(int tableidx) {
            NamelessObjectParams ret = new NamelessObjectParams();

            // require metadata table as the second parameter
            luaL_checktype(L, tableidx, LUA_TTABLE);

            lua_getfield(L, tableidx, "startpoint");
            lua_getfield(L, tableidx, "endpoint");
            lua_checkfieldtype(L, tableidx, "startpoint", -2, LUA_TTABLE);
            lua_checkfieldtype(L, tableidx, "endpoint", -1, LUA_TTABLE);
            ret.startpoint = lua_tovec2(L, -2);
            ret.endpoint = lua_tovec2(L, -1);
            lua_pop(L, 2); // remove the table fields from the stack
            ret.range = luaH_gettablefloat(L, tableidx, "range");
            ret.count = luaH_gettableint(L, tableidx, "count");
            ret.increment = (ret.endpoint - ret.startpoint) / (ret.count - 1);

            return ret;
        }

        public void LoadFile(string filename) {
            if (!compiledfns.ContainsKey(filename))
                throw new LuaException(lua_getwhere(L, 0) + "no such script exists '" + filename + "'");

            lua_getregistry(L, "precompiled");
            lua_rawgeti(L, -1, compiledfns[filename]);
            lua_insert(L, -2);
            lua_pop(L, 1);
        }

#if DEBUG
        internal string StackDump() {
            StringBuilder sb = new StringBuilder();
            int top = lua_gettop(L);
            sb.Append(">> Stack dump (");
            sb.Append(top + " items): ");

            for (int i = 1; i <= top; i++) {
                switch (lua_type(L, i)) {
                    case LUA_TSTRING:
                        sb.Append("string: \"" + lua_tostring(L, i) + "\"");
                        break;
                    case LUA_TBOOLEAN:
                        sb.Append("boolean: " + (lua_toboolean(L, i) == 1 ? "true" : "false"));
                        break;
                    case LUA_TNUMBER:
                        sb.Append("number: " + lua_tonumber(L, i));
                        break;
                    default:
                        sb.Append(lua_typename(L, lua_type(L, i)));
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
            lua_pushcclosure(L, errhandlerfn, 0);
            lua_insert(L, 1);

            // transfer control to Lua
            if (lua_pcall(L, nargs, nresults, 1) != 0) {
                // error handler: lua_errorhandler should have appended a stack trace to the error message
                string errmsg = "Lua error: " + lua_tostring(L, -1).Replace("\t", "  ");
                lua_remove(L, 1); // pop error handler
                lua_pop(L, 1); // pop error string
                throw new LuaException(errmsg);
            }

            // pop the error handler
            lua_remove(L, 1);
        }

        public void RunEvents() {
            // pull up the events table
            lua_settop(L, 0);
            lua_getregistry(L, "events");
            if (lua_type(L, -1) != LUA_TTABLE) {
                // if it doesn't exist, there's nothing for us to do
                lua_pop(L, 1);
                return;
            }
            // iterate through the list of events
            foreach (var pair in events) {
                try {
                    // first find the conditional function and run it
                    lua_rawgeti(L, -1, pair.Key);
                    Call(0, 1);
                    if (lua_toboolean(L, -1) == 1) {
                        // the condition evaluates to true, so run the action
                        lua_pop(L, 1);
                        lua_rawgeti(L, -1, pair.Value);
                        Call(0, 0);
                    } else {
                        // just pop the boolean result
                        lua_pop(L, 1);
                    }

                } catch (LuaException e) {
                    // remove the failing event from the list. make sure to break out
                    // because we're modifying the collection of events
                    SDGame.Logger.LogError("Event Error: " + e.Message);
                    lua_settop(L, 1);
                    luaL_unref(L, -1, pair.Key);
                    luaL_unref(L, -1, pair.Value);
                    lua_pop(L, 1);
                    events.Remove(pair.Key);
                    break;
                }
            }
            // remove the registry table
            lua_pop(L, 1);
        }

        private int clua_panic(IntPtr L) {
            Logger.WriteExceptionReport(new ApplicationException("Lua Panic! Error in unprotected environment: " + lua_tostring(L, -1)));
            // I don't know if the string should be popped or not. Then again, it probably doesn't really matter
            // because if Lua panics then the game will crash as soon as we return from this handler anyway.

            //lua_pop(state, 1); // pop the error message from the stack
            throw new LuaException("Lua panic: " + lua_tostring(L, -1));
        }

        private int lua_errorhandler(IntPtr L) {
            // make sure we got the error message
            if (lua_isstring(L, 1) == 0) return 0;

            string errmsg = lua_tostring(L, 1);
            luaL_traceback(L, L, errmsg, 2);

            return 1;
        }

        private int clua_print(IntPtr L) {
            Print(luaL_checkstring(L, 1));
            return 0;
        }

        private int clua_require(IntPtr L) {
            int basestack = lua_gettop(L);
            LoadFile(luaL_checkstring(L, 1));
            Call(0, -1);
            return lua_gettop(L) - basestack;
        }

        private int clua_sinceepoch(IntPtr L) {
            TimeSpan t = new TimeSpan(DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks);
            lua_pushnumber(L, t.TotalSeconds);
            return 1;
        }

        private int clua_lshift(IntPtr L) {
            lua_pushnumber(L, (int)luaL_checknumber(L, 1) << (int)luaL_checknumber(L, 2));
            return 1;
        }


        private int clua_rshift(IntPtr L) {
            lua_pushnumber(L, (int)luaL_checknumber(L, 1) >> (int)luaL_checknumber(L, 2));
            return 1;
        }

        private int clua_localize(IntPtr L) {
            lua_pushstring(L, Locale.Get(luaL_checkstring(L, 1)));
            return 1;
        }

        private int clua_phrase(IntPtr L) {
            //lua_pushstring(L, Utils.LocalePhrase(luaL_checkstring(L, 1)));
            lua_pushnil(L); // TODO
            return 1;
        }

        private int clua_event(IntPtr L) {
            // expect two function arguments
            luaL_checktype(L, 1, LUA_TFUNCTION);
            luaL_checktype(L, 2, LUA_TFUNCTION);

            // pull up the event registry table, create if it doesn't exist
            lua_getregistry(L, "events");
            if (lua_type(L, -1) != LUA_TTABLE) {
                lua_pop(L, 1); // get rid of the faux events table
                lua_createtable(L, 0, 0); // create a new table
            }

            // push copies of the two passed functions on top of the stack
            lua_pushvalue(L, 2);
            lua_pushvalue(L, 1);
            // then assign them reference numbers and pop the copies
            int refCond = luaL_ref(L, -3);
            int refActn = luaL_ref(L, -2);

            // save the registry table
            lua_setregistry(L, "events");

            // keep the reference integers around
            events.Add(refCond, refActn);
            return 0;
        }

        private int clua_create(IntPtr L) {
            GameObject newobj = null;
            string objtype = luaL_checkstring(L, 1);

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
                NamelessObjectParams nparam = GetNamelessParams(2);
                for (int i = 0; i < nparam.count; i++) {
                    Asteroid rock = new Asteroid(NetServer.world);
                    rock.position = nparam.startpoint + (nparam.increment * i) + // base movement along the start-end line, plus random range
                                    new Vector2(Utils.RandomFloat(nparam.range, -nparam.range), Utils.RandomFloat(nparam.range, -nparam.range));
                    NetServer.world.AddObject(rock);
                }
                return 0;

            } else if (objtype.Equals("mine", StringComparison.InvariantCultureIgnoreCase)) {
                // Mine Field
                NamelessObjectParams nparam = GetNamelessParams(2);
                for (int i = 0; i < nparam.count; i++) {
                    Mine mine = new Mine(NetServer.world);
                    mine.position = nparam.startpoint + (nparam.increment * i) + // base movement along the start-end line, plus random range
                                    new Vector2(Utils.RandomFloat(nparam.range, -nparam.range), Utils.RandomFloat(nparam.range, -nparam.range));
                    NetServer.world.AddObject(mine);
                }
                return 0;

            } else {
                lua_pushstring(L, "Unknown object type '" + objtype + "'");
                lua_error(L);
                return 0;
            }

            // in the case of a named object, make sure to push it to Lua and the server state
            // nameless object creations will create several instances, so don't bother returning just one
            NetServer.world.AddObject(newobj);
            newobj.PushToLua(L);

            return 1;
        }

        private int clua_getObjectByName(IntPtr L) {
            string name = luaL_checkstring(L, 1);
            foreach (var pair in NetServer.world.Objects) {
                // check if this object is a matching named object
                NamedObject nobj = pair.Value as NamedObject;
                if (nobj == null || !nobj.nameshort.Equals(name)) continue;

                nobj.PushToLua(L);
                return 1;
            }
            // no result found, return nil
            lua_pushnil(L);
            return 1;
        }

        private int clua_getObjectById(IntPtr L) {
            uint id = (uint)luaL_checknumber(L, 1);
            foreach (var pair in NetServer.world.Objects) {
                if (pair.Value.id != id) continue;
                pair.Value.PushToLua(L);
                return 1;
            }
            // no result found, return nil
            lua_pushnil(L);
            return 1;
        }

        private int clua_getPlayer(IntPtr L) {
            PlayerShip plr = NetServer.world.GetPlayerShip();
            if (plr == null)
                lua_pushnil(L);
            else
                plr.PushToLua(L);
            return 1;
        }

    }

}