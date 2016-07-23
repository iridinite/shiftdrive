/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Determines what the weapon fires.
    /// </summary>
    /// <remarks>
    /// If you change this enum, make sure to also update the table in main.lua.
    /// </remarks>
    internal enum WeaponType {
        Beam = 0,
        Projectile = 1
    }

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

        public MountSize Mount;
        public bool Powered;
        public float PowerDraw;

        public DamageType DamageType;
        public float Damage;
        public float ChargeTime;
        public float Charge;

        public WeaponType ProjType;
        public string ProjSprite;
        public float ProjSpeed;
        public float ProjSpread;

        public AmmoType Ammo;
        public int AmmoUsed;

        public static Weapon FromLua(IntPtr L, int tableidx) {
            LuaAPI.luaL_checktype(L, tableidx, LuaAPI.LUA_TTABLE);

            Weapon ret = new Weapon();
            ret.Name = LuaAPI.luaH_gettablestring(L, tableidx, "name");
            ret.Description = LuaAPI.luaH_gettablestring(L, tableidx, "desc");
            ret.Mount = (MountSize)LuaAPI.luaH_gettableint(L, tableidx, "mount");

            ret.DamageType = (DamageType)LuaAPI.luaH_gettableint(L, tableidx, "damagetype");
            ret.PowerDraw = LuaAPI.luaH_gettablefloat(L, tableidx, "draw");
            ret.Damage = LuaAPI.luaH_gettablefloat(L, tableidx, "damage");
            ret.ChargeTime = LuaAPI.luaH_gettablefloat(L, tableidx, "chargetime");
            
            ret.ProjType = (WeaponType)LuaAPI.luaH_gettableint(L, tableidx, "weapontype");
            if (ret.ProjType == WeaponType.Projectile) {
                ret.ProjSprite = LuaAPI.luaH_gettablestring(L, tableidx, "projsprite");
                ret.ProjSpeed = LuaAPI.luaH_gettablefloat(L, tableidx, "projspeed");
                ret.ProjSpread = LuaAPI.luaH_gettablefloat(L, tableidx, "projspread");
            }

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
            ret.DamageType = (DamageType)reader.ReadByte();
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
            writer.Write((byte)DamageType);
            writer.Write(Damage);
            writer.Write(ChargeTime);
            writer.Write(Charge);
            writer.Write((byte)Ammo);
            writer.Write((byte)AmmoUsed);
        }
        
    }

}
