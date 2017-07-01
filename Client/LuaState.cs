/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

        internal readonly IntPtr LState;

        private readonly Dictionary<string, int> compiledfns;
        private readonly Dictionary<int, int> events;
        private readonly List<GCHandle> pinnedDelegates;
        private readonly List<GameObject> pinnedObjects;
        private readonly lua_CFunction panicCallback, errorCallback;

        public LuaState() {
            compiledfns = new Dictionary<string, int>();
            events = new Dictionary<int, int>();
            pinnedDelegates = new List<GCHandle>();
            pinnedObjects = new List<GameObject>();

            panicCallback = clua_panic;
            errorCallback = LuaD_ErrorHandler;

            LState = luaL_newstate();
            lua_atpanic(LState, panicCallback);
            PushDelegate(luaopen_base);
            PushDelegate(luaopen_table);
            PushDelegate(luaopen_math);
            PushDelegate(luaopen_string);
            lua_call(LState, 0, 0);
            lua_call(LState, 0, 0);
            lua_call(LState, 0, 0);
            lua_call(LState, 0, 0);

            // replace built-in RNG with .NET version
            lua_getglobal(LState, "math");
            lua_pushnil(LState); // won't need math.randomseed anymore
            lua_setfield(LState, -2, "randomseed");
            RegisterFunction("random", LuaD_Rand);
            lua_pop(LState, 1);

            // generic functions
            RegisterFunction("require", LuaD_Require);
            RegisterFunction("print", LuaD_Print);

            RegisterFunction("localize", LuaD_Localize);

            RegisterFunction("lshift", LuaD_LShift);
            RegisterFunction("rshift", LuaD_RShift);

            RegisterFunction("Event", LuaD_Event);
            RegisterFunction("GetDeltaTime", LuaD_GetDeltaTime);
            RegisterFunction("Create", LuaD_CreateObject);
            RegisterFunction("GetObjectByName", LuaD_GetObjectByName);
            RegisterFunction("GetObjectByID", LuaD_GetObjectByID);
            RegisterFunction("GetPlayerShip", LuaD_GetPlayer);

            RegisterFunction("SendComms", LuaD_SendComms);

            if (lua_gettop(LState) != 0) {
                lua_pushstring(LState, "unbalanced stack after Lua state initialization");
                lua_error(LState);
            }

#if DEBUG
            GC.Collect();
#endif
        }

        public void Dispose() {
            lua_close(LState);
        }

        public void Precompile() {
            lua_createtable(LState, 0, 0);

            DirectoryInfo dir = new DirectoryInfo("Data/Scripts");
            IOrderedEnumerable<FileInfo> files = dir.GetFiles("*.lua", SearchOption.AllDirectories).OrderBy(f => f.Name);
            foreach (FileInfo file in files) {
                try {
                    // load the script file into a string
                    string script;
                    using (StreamReader r = new StreamReader(file.FullName)) {
                        script = r.ReadToEnd();
                    }
                    // strip the script file name of the scripts folder path and the lua extension
                    string scriptname = file.FullName.Substring(dir.FullName.Length + 1).ToLowerInvariant();
                    scriptname = scriptname.Substring(0, scriptname.Length - file.Extension.Length).Replace('\\', '/');
                    // compile the string as a Lua chunk
                    if (luaL_loadstringex(LState, script, scriptname) != 0)
                        throw new LuaException($"Failed to compile script {file.Name}: {lua_tostring(LState, -1)}");
                    // insert the compiled Lua function into the table and obtain a reference integer
                    compiledfns.Add(scriptname, luaL_ref(LState, -2));
                } catch (IOException e) {
                    throw new LuaException($"I/O error occurred while compiling {file.Name}: {e}");
                }
            }
            // save the table of compiled functions into the Lua registry
            lua_setregistry(LState, "precompiled");
        }

        public void PushDelegate(lua_CFunction fn) {
            // this list has no function other than to maintain a reference to the delegate,
            // so the garbage collector won't destroy it (it can't see the unmanaged ref in Lua)
            pinnedDelegates.Add(GCHandle.Alloc(fn));
            lua_pushcclosure(LState, fn, 0);
        }

        public void PinGameObject(GameObject gobj) {
            pinnedObjects.Add(gobj);
        }

        public void ReleaseGameObject(GameObject gobj) {
            // remove the reference
            pinnedObjects.Remove(gobj);
            // if there is still another copy of this object left, don't delete its delegates
            if (pinnedObjects.Find(other => other.ID == gobj.ID) != null) {
                SDGame.Inst.Print($"Skipped delegate release (obj {gobj.ID})");
                return;
            }

            // iterate over all saved delegates
            for (int i = pinnedDelegates.Count - 1; i >= 0; i--) {
                // if this delegate points to this Character instance, free it
                var handle = pinnedDelegates[i];
                var del = (lua_CFunction)handle.Target;
                if (del.Target != gobj) continue;

                handle.Free();
                pinnedDelegates.RemoveAt(i);
            }
        }

        public void RegisterFunction(string name, lua_CFunction fn) {
            PushDelegate(fn);
            lua_setglobal(LState, name);
        }

        private NamelessObjectParams GetNamelessParams(int tableidx) {
            NamelessObjectParams ret = new NamelessObjectParams();

            // require metadata table as the second parameter
            luaL_checktype(LState, tableidx, LuaType.Table);

            lua_getfield(LState, tableidx, "startpoint");
            lua_getfield(LState, tableidx, "endpoint");
            luaH_checkfieldtype(LState, tableidx, "startpoint", -2, LuaType.Table);
            luaH_checkfieldtype(LState, tableidx, "endpoint", -1, LuaType.Table);
            ret.startpoint = lua_tovec2(LState, -2);
            ret.endpoint = lua_tovec2(LState, -1);
            lua_pop(LState, 2); // remove the table fields from the stack
            ret.range = luaH_gettablefloat(LState, tableidx, "range");
            ret.count = luaH_gettableint(LState, tableidx, "count");
            ret.increment = (ret.endpoint - ret.startpoint) / Math.Max(1, ret.count - 1);

            return ret;
        }

        public void LoadFile(string filename) {
            if (!compiledfns.ContainsKey(filename.ToLowerInvariant()))
                throw new LuaException(lua_getwhere(LState, 0) + "no such script exists '" + filename + "'");

            lua_getregistry(LState, "precompiled");
            lua_rawgeti(LState, -1, compiledfns[filename]);
            lua_insert(LState, -2);
            lua_pop(LState, 1);
        }

#if DEBUG
        internal string StackDump() {
            StringBuilder sb = new StringBuilder();
            int top = lua_gettop(LState);
            sb.Append(">> Stack dump (");
            sb.Append(top.ToString() + " items): ");

            for (int i = 1; i <= top; i++) {
                switch (lua_type(LState, i)) {
                    case LuaType.String:
                        sb.Append("string: \"" + lua_tostring(LState, i) + "\"");
                        break;
                    case LuaType.Boolean:
                        sb.Append("boolean: " + (lua_toboolean(LState, i) == 1 ? "true" : "false"));
                        break;
                    case LuaType.Number:
                        sb.Append("number: " + lua_tonumber(LState, i));
                        break;
                    default:
                        sb.Append(lua_typename(LState, lua_type(LState, i)));
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
            lua_pushcclosure(LState, errorCallback, 0);
            lua_insert(LState, 1);

            // transfer control to Lua
            if (lua_pcall(LState, nargs, nresults, 1) != 0) {
                // error handler: lua_errorhandler should have appended a stack trace to the error message
                string errmsg = "Lua error: " + lua_tostring(LState, -1).Replace("\t", "  ");
                lua_remove(LState, 1); // pop error handler
                lua_pop(LState, 1); // pop error string
                throw new LuaException(errmsg);
            }

            // pop the error handler
            lua_remove(LState, 1);
        }

        public void RunEvents() {
            // pull up the events table
            lua_settop(LState, 0);
            lua_getregistry(LState, "events");
            if (lua_type(LState, -1) != LuaType.Table) {
                // if it doesn't exist, there's nothing for us to do
                lua_pop(LState, 1);
                return;
            }
            // iterate through the list of events
            foreach (var pair in events) {
                try {
                    // first find the conditional function and run it
                    lua_rawgeti(LState, -1, pair.Key);
                    Call(0, 1);
                    if (lua_toboolean(LState, -1) == 1) {
                        // the condition evaluates to true, so run the action
                        lua_pop(LState, 1);
                        lua_rawgeti(LState, -1, pair.Value);
                        Call(0, 0);
                    } else {
                        // just pop the boolean result
                        lua_pop(LState, 1);
                    }
                } catch (LuaException e) {
                    // remove the failing event from the list. make sure to break out
                    // because we're modifying the collection of events
                    SDGame.Logger.LogError("Event Error: " + e.Message);
                    lua_settop(LState, 1);
                    luaL_unref(LState, -1, pair.Key);
                    luaL_unref(LState, -1, pair.Value);
                    lua_pop(LState, 1);
                    events.Remove(pair.Key);
                    break;
                }
            }
            // remove the registry table
            lua_pop(LState, 1);
        }

        private int clua_panic(IntPtr L) {
            Logger.WriteExceptionReport(new ApplicationException("Lua Panic! Error in unprotected environment: " + lua_tostring(L, -1)));
            // I don't know if the string should be popped or not. Then again, it probably doesn't really matter
            // because if Lua panics then the game will crash as soon as we return from this handler anyway.

            //lua_pop(state, 1); // pop the error message from the stack
            throw new LuaException("Lua panic: " + lua_tostring(L, -1));
        }

        private int LuaD_ErrorHandler(IntPtr L) {
            // make sure we got the error message
            if (lua_isstring(L, 1) == 0) return 0;

            string errmsg = lua_tostring(L, 1);
            luaL_traceback(L, L, errmsg, 2);

            return 1;
        }

        private static int LuaD_Rand(IntPtr L) {
            // simulate lua's default math.random, but use .NET RNG
            double rand = Utils.RNG.NextDouble();
            switch (lua_gettop(L)) {
                case 0:
                    lua_pushnumber(L, rand);
                    break;
                case 1: {
                    var upper = (int)luaL_checknumber(L, 1);
                    if (upper < 1) luaL_error(L, "interval is empty");
                    lua_pushnumber(L, Math.Floor(rand * upper) + 1);
                    break;
                }
                case 2: {
                    int lower = (int)luaL_checknumber(L, 1);
                    int upper = (int)luaL_checknumber(L, 2);
                    if (lower > upper) luaL_error(L, "interval is empty");
                    lua_pushnumber(L, Math.Floor(rand * (upper - lower + 1)) + lower);
                    break;
                }
            }
            return 1;
        }

        private int LuaD_Print(IntPtr L) {
            SDGame.Inst.Print(luaL_checkstring(L, 1));
            return 0;
        }

        private int LuaD_Require(IntPtr L) {
            int basestack = lua_gettop(L);
            LoadFile(luaL_checkstring(L, 1));
            Call(0, -1);
            return lua_gettop(L) - basestack;
        }

        private int LuaD_LShift(IntPtr L) {
            lua_pushnumber(L, (int)luaL_checknumber(L, 1) << (int)luaL_checknumber(L, 2));
            return 1;
        }


        private int LuaD_RShift(IntPtr L) {
            lua_pushnumber(L, (int)luaL_checknumber(L, 1) >> (int)luaL_checknumber(L, 2));
            return 1;
        }

        private int LuaD_Localize(IntPtr L) {
            lua_pushstring(L, Locale.Get(luaL_checkstring(L, 1)));
            return 1;
        }

        private int LuaD_GetDeltaTime(IntPtr L) {
            lua_pushnumber(L, SDGame.Inst.GetDeltaTime());
            return 1;
        }

        private int LuaD_Event(IntPtr L) {
            // expect two function arguments
            luaL_checktype(L, 1, LuaType.Function);
            luaL_checktype(L, 2, LuaType.Function);

            // pull up the event registry table, create if it doesn't exist
            lua_getregistry(L, "events");
            if (lua_type(L, -1) != LuaType.Table) {
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

        private int LuaD_CreateObject(IntPtr L) {
            GameObject newobj = null;
            string objtype = luaL_checkstring(L, 1);

            // -- Named Objects --
            if (objtype.Equals("player", StringComparison.InvariantCultureIgnoreCase)) {
                // Player Ship
                PlayerShip newship = new PlayerShip(NetServer.World);
                newobj = newship;
            } else if (objtype.Equals("ship", StringComparison.InvariantCultureIgnoreCase)) {
                // NPC Ship
                AIShip newship = new AIShip(NetServer.World);
                newobj = newship;
            } else if (objtype.Equals("station", StringComparison.InvariantCultureIgnoreCase)) {
                // Space Station
                newobj = new SpaceStation(NetServer.World);
            } else if (objtype.Equals("blackhole", StringComparison.InvariantCultureIgnoreCase)) {
                // Black Hole
                newobj = new BlackHole(NetServer.World);

                // -- Nameless Objects --
            } else if (objtype.Equals("asteroid", StringComparison.InvariantCultureIgnoreCase)) {
                // Asteroid Belt
                NamelessObjectParams nparam = GetNamelessParams(2);
                for (int i = 0; i < nparam.count; i++) {
                    Asteroid rock = new Asteroid(NetServer.World);
                    rock.Position = nparam.startpoint + (nparam.increment * i) + // base movement along the start-end line, plus random range
                        new Vector2(Utils.RandomFloat(nparam.range, -nparam.range), Utils.RandomFloat(nparam.range, -nparam.range));
                    NetServer.World.AddObject(rock);
                }
                return 0;
            } else if (objtype.Equals("mine", StringComparison.InvariantCultureIgnoreCase)) {
                // Mine Field
                NamelessObjectParams nparam = GetNamelessParams(2);
                for (int i = 0; i < nparam.count; i++) {
                    Mine mine = new Mine(NetServer.World);
                    mine.Position = nparam.startpoint + (nparam.increment * i) + // base movement along the start-end line, plus random range
                        new Vector2(Utils.RandomFloat(nparam.range, -nparam.range), Utils.RandomFloat(nparam.range, -nparam.range));
                    NetServer.World.AddObject(mine);
                }
                return 0;
            } else {
                lua_pushstring(L, "Unknown object type '" + objtype + "'");
                lua_error(L);
                return 0;
            }

            // in the case of a named object, make sure to push it to Lua and the server state
            // nameless object creations will create several instances, so don't bother returning just one
            NetServer.World.AddObject(newobj);
            newobj.SetLuaState(this);
            newobj.PushToLua();

            return 1;
        }

        private int LuaD_GetObjectByName(IntPtr L) {
            string name = luaL_checkstring(L, 1);
            foreach (var pair in NetServer.World.Objects) {
                // check if this object is a matching named object
                NamedObject nobj = pair.Value as NamedObject;
                if (nobj == null || !nobj.NameShort.Equals(name)) continue;

                nobj.PushToLua();
                return 1;
            }
            // no result found, return nil
            lua_pushnil(L);
            return 1;
        }

        private int LuaD_GetObjectByID(IntPtr L) {
            uint id = (uint)luaL_checknumber(L, 1);
            foreach (var pair in NetServer.World.Objects) {
                if (pair.Value.ID != id) continue;
                pair.Value.PushToLua();
                return 1;
            }
            // no result found, return nil
            lua_pushnil(L);
            return 1;
        }

        private int LuaD_GetPlayer(IntPtr L) {
            PlayerShip plr = NetServer.World.GetPlayerShip();
            if (plr == null)
                lua_pushnil(L);
            else
                plr.PushToLua();
            return 1;
        }

        private int LuaD_SendComms(IntPtr L) {
            string sender = luaL_checkstring(L, 1);
            string body = luaL_checkstring(L, 2);
            NetServer.PublishComms(new CommMessage(sender, body));
            return 0;
        }

    }

}
