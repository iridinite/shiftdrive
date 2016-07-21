/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Represents a <see cref="GameObject"/> that has a name and can be examined by a player.
    /// </summary>
    internal abstract class NamedObject : GameObject {
        public string nameshort;
        public string namefull;
        public string desc;

        protected NamedObject() {
            iconfile = "";
            nameshort = "OBJ";
            namefull = "Object";
            desc = "";
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);

            writer.Write(nameshort);
            writer.Write(namefull);
            writer.Write(desc);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            nameshort = reader.ReadString();
            namefull = reader.ReadString();
            desc = reader.ReadString();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "nameshort":
                    LuaAPI.lua_pushstring(L, nameshort);
                    break;
                case "namefull":
                    LuaAPI.lua_pushstring(L, namefull);
                    break;
                case "desc":
                    LuaAPI.lua_pushstring(L, desc);
                    break;
                default:
                    return base.LuaGet(L);
            }
            return 1;
        }

        protected override int LuaSet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "nameshort":
                    nameshort = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "namefull":
                    namefull = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "desc":
                    desc = LuaAPI.luaL_checkstring(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            changed = true;
            return 0;
        }

    }

}
