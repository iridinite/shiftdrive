/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

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

        public string FireSound;

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

        public void Update(float deltaTime, Ship owner, WeaponMount mount) {
            // out-of-ammo / reloading processing
            if (Ammo != AmmoType.None && AmmoLeft < AmmoPerShot) {
                // need ammo reserve
                if (AmmoClipsLeft < 1 && Ammo != AmmoType.Dummy)
                    return;

                // begin reloading
                if (ReloadProgress < ReloadTime) {
                    ReloadProgress += deltaTime;
                    return;
                }
                ReloadProgress = 0f;
                AmmoLeft = AmmoPerClip;
                if (owner.Type == ObjectType.PlayerShip && // AI ships have unlimited clips
                    Ammo != AmmoType.Dummy) // dummy ammo has no clips
                    AmmoClipsLeft--;
            }

            // find a target to fire upon
            GameObject target = owner.SelectTarget(mount, this);
            if (target == null) {
                // decrease charge if not targeting something
                Charge = Math.Max(0f, Charge - deltaTime);
                return;
            };

            // increment charge
            Charge += deltaTime;
            if (Charge < ChargeTime) return;
            Charge = 0f;

            // draw a beam for beam weapons
            bool serverside = owner.World.IsServer;
            if (!serverside && ProjType == WeaponType.Beam)
                ParticleManager.CreateBeam(owner.Position + mount.Position, target.Position, ProjSprite, "map/beam-impact");

            // remove ammo for this shot
            if (Ammo != AmmoType.None)
                AmmoLeft -= AmmoPerShot;


            // here be dragons, no clients beyond this here sign
            // (we're spawning objects and dealing damage, so server-only access)
            if (!serverside) return;

            // firing sound effect
            Assets.GetSound(FireSound).Play3D(owner.World.GetPlayerShip().Position, owner.Position);

            switch (ProjType) {
                case WeaponType.Projectile:
                    // launch a projectile object
                    Vector2 fireAt = GetFiringSolution(owner, target);
                    float randombearing = (float)Utils.RNG.NextDouble() * ProjSpread * 2 - ProjSpread;
                    NetServer.World.AddObject(new Projectile(NetServer.World, ProjSprite, Ammo,
                        owner.Position + mount.Position,
                        Utils.Repeat(Utils.CalculateBearing(owner.Position, fireAt) + randombearing, 0f, 360f),
                        ProjSpeed, Damage,
                        owner.Faction));
                    break;
                case WeaponType.Beam:
                    // beam weapon - visual effect is done on client (see above)
                    // just deal damage immediately because it's an instant-hit weapon anyway
                    target.TakeDamage(Damage, true);
                    break;
            }

            // consume fuel for weapon fire
            if (owner.Type == ObjectType.PlayerShip) {
                PlayerShip plr = owner as PlayerShip;
                Debug.Assert(plr != null);
                plr.ConsumeFuel(PowerDraw);
            }
        }
        
        /// <summary>
        /// Calculates the point where the weapon must fire to hit the target by the time the projectiles arrive.
        /// </summary>
        /// <param name="self">The object where the projectiles originate from.</param>
        /// <param name="target">The object that should be hit.</param>
        public Vector2 GetFiringSolution(GameObject self, GameObject target) {
            Vector2 velocity = target.Movement;
            Vector2 delta = target.Position - self.Position;

            float a = velocity.LengthSquared() - ProjSpeed * ProjSpeed;
            if (a >= 0) return target.Position;

            float b = 2 * Vector2.Dot(delta, velocity);
            float c = delta.LengthSquared();

            float rt = (float)Math.Sqrt(b * b - 4 * a * c);
            float dt1 = (-b + rt) / (2 * a);
            float dt2 = (-b - rt) / (2 * a);
            float dt = dt1 < 0 ? dt2 : dt1;

            return target.Position + velocity * dt;
        }

        public static Weapon FromLua(IntPtr L, int tableidx) {
            LuaAPI.luaL_checktype(L, tableidx, LuaType.Table);

            Weapon ret = new Weapon();
            ret.Name = LuaAPI.luaH_gettablestring(L, tableidx, "name");
            ret.Description = LuaAPI.luaH_gettablestring(L, tableidx, "desc");
            ret.Mount = (MountSize)LuaAPI.luaH_gettableint(L, tableidx, "mount");

            ret.FireSound = LuaAPI.luaH_gettablestring(L, tableidx, "firesound");

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
            ret.Mount = (MountSize)instream.ReadByte();
            ret.Powered = instream.ReadBoolean();

            ret.FireSound = instream.ReadString();

            ret.PowerDraw = instream.ReadSingle();
            ret.DamageType = (DamageType)instream.ReadByte();
            ret.Damage = instream.ReadSingle();
            ret.ChargeTime = instream.ReadSingle();
            ret.Charge = instream.ReadSingle();
            ret.Range = instream.ReadSingle();

            ret.ProjType = (WeaponType)instream.ReadByte();
            ret.ProjSprite = instream.ReadString();
            ret.ProjSpeed = instream.ReadSingle();
            ret.ProjSpread = instream.ReadSingle();

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
            outstream.Write((byte)Mount);
            outstream.Write(Powered);

            outstream.Write(FireSound);

            outstream.Write(PowerDraw);
            outstream.Write((byte)DamageType);
            outstream.Write(Damage);
            outstream.Write(ChargeTime);
            outstream.Write(Charge);
            outstream.Write(Range);

            outstream.Write((byte)ProjType);
            outstream.Write(ProjSprite);
            outstream.Write(ProjSpeed);
            outstream.Write(ProjSpread);

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
