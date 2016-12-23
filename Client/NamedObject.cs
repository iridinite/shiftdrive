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

        protected NamedObject(GameState world) : base(world) {
            spritename = "ui/rect";
            nameshort = "OBJ";
            namefull = "Object";
            desc = "";
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            if (changed.HasFlag(ObjectProperty.NameShort))
                outstream.Write(nameshort);
            if (changed.HasFlag(ObjectProperty.NameFull))
                outstream.Write(namefull);
            if (changed.HasFlag(ObjectProperty.Description))
                outstream.Write(desc);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.NameShort))
                nameshort = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.NameFull))
                namefull = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.Description))
                desc = instream.ReadString();
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
                    changed |= ObjectProperty.NameShort;
                    break;
                case "namefull":
                    namefull = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.NameFull;
                    break;
                case "desc":
                    desc = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.Description;
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}
