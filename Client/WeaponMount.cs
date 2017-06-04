/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
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
        public MountSize Size;

        public static WeaponMount FromLua(IntPtr L, int tableidx) {
            LuaAPI.luaL_checktype(L, tableidx, LuaType.Table);

            WeaponMount ret = new WeaponMount();
            LuaAPI.lua_getfield(L, tableidx, "position");
            LuaAPI.lua_checkfieldtype(L, tableidx, "position", -1, LuaType.Table);
            ret.Offset = LuaAPI.lua_tovec2(L, -1);
            LuaAPI.lua_pop(L, 1);
            ret.Bearing = LuaAPI.luaH_gettablefloat(L, tableidx, "bearing");
            ret.Arc = LuaAPI.luaH_gettablefloat(L, tableidx, "arc") / 2f;
            ret.Size = (MountSize)LuaAPI.luaH_gettableint(L, tableidx, "size");
            return ret;
        }

        public static WeaponMount FromStream(Packet instream) {
            WeaponMount ret = new WeaponMount();
            ret.Offset = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            ret.Position = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            ret.Bearing = instream.ReadSingle();
            ret.Arc = instream.ReadSingle();
            ret.Size = (MountSize)instream.ReadByte();
            return ret;
        }

        public void Serialize(Packet outstream) {
            outstream.Write(Offset.X);
            outstream.Write(Offset.Y);
            outstream.Write(Position.X);
            outstream.Write(Position.Y);
            outstream.Write(Bearing);
            outstream.Write(Arc);
            outstream.Write((byte)Size);
        }

    }

}
