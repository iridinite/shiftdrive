/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {

    /// <summary>
    /// Represents a <see cref="GameObject"/> that has a name and can be examined by a player.
    /// </summary>
    internal abstract class NamedObject : GameObject {
        public string NameShort { get; set; }
        public string NameFull { get; set; }
        public string Description { get; set; }

        protected NamedObject(GameState world) : base(world) {
            SpriteName = "ui/rect";
            NameShort = "OBJ";
            NameFull = "Object";
            Description = "";
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            if (changed.HasFlag(ObjectProperty.NameShort))
                outstream.Write(NameShort);
            if (changed.HasFlag(ObjectProperty.NameFull))
                outstream.Write(NameFull);
            if (changed.HasFlag(ObjectProperty.Description))
                outstream.Write(Description);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.NameShort))
                NameShort = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.NameFull))
                NameFull = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.Description))
                Description = instream.ReadString();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "nameshort":
                    LuaAPI.lua_pushstring(L, NameShort);
                    break;
                case "namefull":
                    LuaAPI.lua_pushstring(L, NameFull);
                    break;
                case "desc":
                    LuaAPI.lua_pushstring(L, Description);
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
                    NameShort = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.NameShort;
                    break;
                case "namefull":
                    NameFull = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.NameFull;
                    break;
                case "desc":
                    Description = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.Description;
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}
