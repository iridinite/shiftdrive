/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Represents the kind of damage that a weapon deals.
    /// </summary>
    /// <remarks>
    /// If you change this enum, make sure to also update the table in main.lua.
    /// </remarks>
    internal enum DamageType {
        Thermal = 0,
        Kinetic = 1,
        Explosive = 2
    }

    /// <summary>
    /// Represents what sort of ammo the weapon consumes when fired.
    /// </summary>
    /// <remarks>
    /// If you change this enum, make sure to also update the table in main.lua.
    /// </remarks>
    internal enum AmmoType {
        None = 0,
        Missile = 1,
        EMP = 2,
        Mine = 3
    }

    /// <summary>
    /// Represents a weapon on a ship.
    /// </summary>
    internal sealed class Weapon {

        public string Name;
        public string Description;

        public bool Powered;
        public float PowerDraw;

        public DamageType Type;
        public float Damage;
        public float ChargeTime;
        public float Charge;

        public AmmoType Ammo;
        public int AmmoUsed;

        public static Weapon FromLua(IntPtr L, int tableidx) {
            LuaAPI.luaL_checktype(L, tableidx, LuaAPI.LUA_TTABLE);

            Weapon ret = new Weapon();
            ret.Name = LuaAPI.luaH_gettablestring(L, tableidx, "name");
            ret.Description = LuaAPI.luaH_gettablestring(L, tableidx, "desc");
            ret.PowerDraw = LuaAPI.luaH_gettablefloat(L, tableidx, "draw");
            ret.Damage = LuaAPI.luaH_gettablefloat(L, tableidx, "damage");
            ret.ChargeTime = LuaAPI.luaH_gettablefloat(L, tableidx, "chargetime");
            ret.Type = (DamageType)LuaAPI.luaH_gettableint(L, tableidx, "damagetype");
            ret.Ammo = (AmmoType)LuaAPI.luaH_gettableint(L, tableidx, "ammotype");
            ret.AmmoUsed = ret.Ammo == AmmoType.None ? 0 : LuaAPI.luaH_gettableint(L, tableidx, "ammousage");
            ret.Powered = false;
            ret.Charge = 0f;
            return ret;
        }

        public static Weapon FromStream(BinaryReader reader) {
            Weapon ret = new Weapon();
            ret.Name = reader.ReadString();
            ret.Description = reader.ReadString();
            ret.Powered = reader.ReadBoolean();
            ret.PowerDraw = reader.ReadSingle();
            ret.Type = (DamageType)reader.ReadByte();
            ret.Damage = reader.ReadSingle();
            ret.ChargeTime = reader.ReadSingle();
            ret.Charge = reader.ReadSingle();
            ret.Ammo = (AmmoType)reader.ReadByte();
            ret.AmmoUsed = reader.ReadByte();
            return ret;
        }

        public void Serialize(BinaryWriter writer) {
            writer.Write(Name);
            writer.Write(Description);
            writer.Write(Powered);
            writer.Write(PowerDraw);
            writer.Write((byte)Type);
            writer.Write(Damage);
            writer.Write(ChargeTime);
            writer.Write(Charge);
            writer.Write((byte)Ammo);
            writer.Write((byte)AmmoUsed);
        }
        
    }

}
