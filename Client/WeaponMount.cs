/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents the maximum weapon size that a mount can hold.
    /// </summary>
    internal enum MountSize {
        Small = 1,
        Medium = 2,
        Large = 3
    }

    /// <summary>
    /// Represents an attachment point for a weapon.
    /// </summary>
    internal sealed class WeaponMount {

        public Vector2 Offset;
        public Vector2 Position;
        public float Bearing;
        public float Arc;
        public float OffsetMag;
        public MountSize Size;

        public static WeaponMount FromLua(IntPtr L, int tableidx) {
            LuaAPI.luaL_checktype(L, tableidx, LuaAPI.LUA_TTABLE);

            WeaponMount ret = new WeaponMount();
            LuaAPI.lua_getfield(L, tableidx, "position");
            LuaAPI.lua_checkfieldtype(L, tableidx, "position", -1, LuaAPI.LUA_TTABLE);
            ret.Offset = LuaAPI.lua_tovec2(L, -1);
            ret.OffsetMag = -ret.Offset.Length();
            LuaAPI.lua_pop(L, 1);
            ret.Bearing = LuaAPI.luaH_gettablefloat(L, tableidx, "bearing");
            ret.Arc = LuaAPI.luaH_gettablefloat(L, tableidx, "arc");
            ret.Size = (MountSize)LuaAPI.luaH_gettableint(L, tableidx, "size");
            return ret;
        }

    }

}
