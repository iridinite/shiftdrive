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
        Dummy = 1,
        Missile = 2,
        EMP = 3,
        Mine = 4,
        Bullet = 5
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
        public float Range;

        public WeaponType ProjType;
        public string ProjSprite;
        public float ProjSpeed;
        public float ProjSpread;

        public AmmoType Ammo;
        public int AmmoPerShot;
        public int AmmoPerClip;
        public int AmmoClipsMax;
        public int AmmoLeft;
        public int AmmoClipsLeft;
        public float ReloadTime;
        public float ReloadProgress;

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
            ret.Range = LuaAPI.luaH_gettablefloat(L, tableidx, "range");

            ret.ProjType = (WeaponType)LuaAPI.luaH_gettableint(L, tableidx, "weapontype");
            if (ret.ProjType == WeaponType.Projectile) {
                ret.ProjSprite = LuaAPI.luaH_gettablestring(L, tableidx, "projsprite");
                ret.ProjSpeed = LuaAPI.luaH_gettablefloat(L, tableidx, "projspeed");
                ret.ProjSpread = LuaAPI.luaH_gettablefloat(L, tableidx, "projspread");
            }

            ret.Ammo = (AmmoType)LuaAPI.luaH_gettableint(L, tableidx, "ammotype");
            if (ret.Ammo != AmmoType.None) {
                ret.AmmoPerShot = LuaAPI.luaH_gettableint(L, tableidx, "ammouse");
                ret.AmmoPerClip = LuaAPI.luaH_gettableint(L, tableidx, "ammoclip");
                ret.AmmoClipsMax = ret.Ammo == AmmoType.Dummy ? 0 : LuaAPI.luaH_gettableint(L, tableidx, "ammomax");
                ret.ReloadTime = LuaAPI.luaH_gettablefloat(L, tableidx, "reloadtime");
            }

            ret.Powered = false;
            ret.AmmoLeft = ret.AmmoPerClip;
            ret.AmmoClipsLeft = ret.AmmoClipsMax;
            ret.Charge = 0f;
            ret.ReloadProgress = 0f;
            return ret;
        }

        public static Weapon FromStream(Packet instream) {
            Weapon ret = new Weapon();
            ret.Name = instream.ReadString();
            ret.Description = instream.ReadString();
            ret.Powered = instream.ReadBoolean();
            ret.PowerDraw = instream.ReadSingle();
            ret.DamageType = (DamageType)instream.ReadByte();
            ret.Damage = instream.ReadSingle();
            ret.ChargeTime = instream.ReadSingle();
            ret.Charge = instream.ReadSingle();
            ret.Ammo = (AmmoType)instream.ReadByte();
            ret.AmmoPerShot = instream.ReadByte();
            ret.AmmoPerClip = instream.ReadUInt16();
            ret.AmmoLeft = instream.ReadUInt16();
            ret.AmmoClipsMax = instream.ReadUInt16();
            ret.AmmoClipsLeft = instream.ReadUInt16();
            ret.ReloadTime = instream.ReadSingle();
            ret.ReloadProgress = instream.ReadSingle();
            return ret;
        }

        public void Serialize(Packet outstream) {
            outstream.Write(Name);
            outstream.Write(Description);
            outstream.Write(Powered);
            outstream.Write(PowerDraw);
            outstream.Write((byte)DamageType);
            outstream.Write(Damage);
            outstream.Write(ChargeTime);
            outstream.Write(Charge);
            outstream.Write((byte)Ammo);
            outstream.Write((byte)AmmoPerShot);
            outstream.Write((ushort)AmmoPerClip);
            outstream.Write((ushort)AmmoLeft);
            outstream.Write((ushort)AmmoClipsMax);
            outstream.Write((ushort)AmmoClipsLeft);
            outstream.Write(ReloadTime);
            outstream.Write(ReloadProgress);
        }

    }

}
