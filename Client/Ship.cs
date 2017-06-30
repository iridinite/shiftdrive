/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a shielded spaceship.
    /// </summary>
    internal abstract class Ship : NamedObject {

        private const int WEAPON_ARRAY_SIZE = 8;

        public float Hull { get; protected set; }
        public float HullMax { get; private set; }
        public float Shield { get; protected set; }
        public float ShieldMax { get; private set; }
        public bool ShieldActive { get; set; }

        public float TopSpeed { get; private set; }
        public float TurnRate { get; private set; }

        public byte WeaponsCount { get; private set; }
        public WeaponMount[] Mounts { get; }
        public Weapon[] Weapons { get; }

        public float throttle { get; set; }
        public float steering { get; set; }

        public byte faction { get; private set; }

        private float flaretime;
        private float shieldRegenPause;
        private readonly List<Vector2> flareSpawners;

        protected Ship(GameState world) : base(world) {
            Hull = 100f;
            HullMax = 100f;
            Shield = 100f;
            ShieldMax = 100f;
            ShieldActive = false;
            Damping = 0.75f;
            WeaponsCount = 0;
            Mounts = new WeaponMount[WEAPON_ARRAY_SIZE];
            Weapons = new Weapon[WEAPON_ARRAY_SIZE];
            flareSpawners = new List<Vector2>();
            flaretime = 0f;
            shieldRegenPause = 0f;
            ZOrder = 128;
            Layer = CollisionLayer.Ship;
            LayerMask = CollisionLayer.Ship | CollisionLayer.Asteroid | CollisionLayer.Default;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            Velocity = Vector2.Zero;

            // apply throttle velocity based on the ship's facing
            Vector2 movementByEngine = new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(Facing - 90f)),
                (float)Math.Sin(MathHelper.ToRadians(Facing - 90f)))
                * throttle * TopSpeed;
            Movement += movementByEngine;
            Position += movementByEngine * deltaTime;
            //position.X = MathHelper.Clamp(position.X, 0f, 1000f);
            //position.Y = MathHelper.Clamp(position.Y, 0f, 1000f);
            // apply maneuver: find whether turning left or right is fastest
            float deltaFacing = MathHelper.Clamp(Utils.Repeat((steering - Facing) + 180, 0f, 360f) - 180f, -1f, 1f);
            Facing = Utils.Repeat(Facing + deltaFacing * TurnRate * deltaTime, 0f, 360f);

            // retransmit modified properties
            if (throttle > 0f)
                changed |= ObjectProperty.Position;
            if (Math.Abs(deltaFacing) > 0.001f)
                changed |= ObjectProperty.Facing;

            // shield regeneration
            if (Shield < ShieldMax) {
                if (shieldRegenPause > 0f)
                    shieldRegenPause -= deltaTime;
                else {
                    Shield = Math.Min(Shield + deltaTime / 2f, ShieldMax);
                    changed |= ObjectProperty.Health;
                }
            }

            // update weapon charge / ammo states
            for (int i = 0; i < WeaponsCount; i++) {
                Weapon wep = Weapons[i];
                if (wep == null) continue;

                // sanity check, active weapons must have a mount
                Debug.Assert(Mounts[i] != null);
                // charge and fire the weapon
                wep.Update(deltaTime, this, Mounts[i]);
            }

            // update mount point position
            for (int i = 0; i < WeaponsCount; i++) {
                if (Mounts[i] == null) continue;
                Mounts[i].Position = Utils.CalculateRotatedOffset(Mounts[i].Offset, Facing);
            }
            
            // engine flares
            if (!(throttle > 0f) || World.IsServer) return;
            if (flaretime > 0f) { // space out evenly
                flaretime -= deltaTime;
                return;
            }
            flaretime = 0.01f;
            // create particles for engine exhaust
            foreach (Vector2 flarepos in flareSpawners) {
                Particle flare = new Particle();
                flare.lifemax = 3f;
                flare.sprite = Assets.GetSprite("map/engineflare").Clone();
                flare.scalestart = 1.1f;
                flare.scaleend = 0.9f;
                flare.colorstart = Color.White * (throttle * 0.6f + 0.15f);
                flare.colorend = Color.Transparent;
                flare.facing = Facing;
                flare.zorder = 160;
                flare.position = Position + Utils.CalculateRotatedOffset(flarepos, Facing);
                ParticleManager.Register(flare);
            }
        }
        
        public override void TakeDamage(float damage, bool sound = false) {
            // need to resend hull and shields
            changed |= ObjectProperty.Health;

            // delay shield recharge
            shieldRegenPause = 10f;

            // apply damage to shields first, if possible
            if (ShieldActive && Shield > 0f) {
                if (World.IsServer && sound)
                    Assets.GetSound("DamageShield").Play3D(World.GetPlayerShip().Position, Position);
                Shield = MathHelper.Clamp(Shield - damage, 0f, ShieldMax);
                return;
            }
            // otherwise, apply damage to hull
            if (World.IsServer && sound)
                Assets.GetSound("DamageHull").Play3D(World.GetPlayerShip().Position, Position);
            Hull = MathHelper.Clamp(Hull - damage, 0f, HullMax);
            // zero hull = ship destruction
            if (Hull <= 0f && World.IsServer) Destroy();
        }

        public override void Destroy() {
            if (!IsDestroyScheduled()) {
                Assets.GetSound("ExplosionMedium").Play3D(World.GetPlayerShip().Position, Position);
                NetServer.PublishParticleEffect(ParticleEffect.Explosion, Position);
            }
            base.Destroy();
        }

        public override bool IsTargetable() {
            return true;
        }

        public bool IsAlly(Ship other) {
            return faction == other.faction;
        }

        public bool IsNeutral() {
            return faction == 0;
        }

        public Color GetFactionColor(Ship observer) {
            if (IsNeutral()) return Color.CornflowerBlue;
            return IsAlly(observer) ? Color.LightGreen : Color.Red;
        }

        public virtual GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject target = null;
            float closest = float.MaxValue;

            // find closest object. station has 360 weapon so don't care about weapon arcs
            foreach (GameObject gobj in World.Objects.Values) {
                // make sure we can actually shoot this thing
                if (!GetCanTarget(gobj, weapon.Range, mount.Bearing + this.Facing, mount.Arc))
                    continue;

                // keep closest object
                float dist = Vector2.DistanceSquared(gobj.Position, this.Position);
                if (dist > closest) continue;

                closest = dist;
                target = gobj;
            }

            return target;
        }

        protected bool GetCanTarget(GameObject target, float range, float bearing, float arc) {
            // must be targetable at all
            if (!target.IsTargetable())
                return false;

            // if ship, cannot fire on friendlies
            if (target.IsShip()) {
                Ship ship = target as Ship;
                Debug.Assert(ship != null);
                if (ship.IsAlly(this))
                    return false;
            }

            // cannot exceed weapon range
            if (Vector2.DistanceSquared(target.Position, this.Position) > range * range)
                return false;

            // cannot fall outside weapon arc
            float targetAngle = Utils.CalculateBearing(this.Position, target.Position);
            float arcfrom = Utils.Repeat(bearing - arc, 0f, 360f);
            float arcto = Utils.Repeat(bearing + arc, 0f, 360f);

            while (arcto < arcfrom) arcto += 360f;
            while (targetAngle < arcfrom) targetAngle += 360f;

            return targetAngle >= arcfrom && targetAngle <= arcto;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // resolve collision
            base.OnCollision(other, normal, penetration);

            // find the highest velocity involved in the collision
            float highestVelocity = throttle * TopSpeed * Math.Abs(penetration);
            // if colliding with another ship, factor in that ship's speed
            Ship otherShip = other as Ship;
            if (otherShip != null)
                highestVelocity = Math.Max(highestVelocity,
                    otherShip.throttle * otherShip.TopSpeed * Math.Abs(penetration));

            // cap damage and apply
            float damage = Math.Min(highestVelocity * SDGame.Inst.GetDeltaTime() * 2, 0.25f);
            TakeDamage(damage);

            // TODO: collision sounds

            if (other.Type == ObjectType.Asteroid) {
                // reduce pushback
                Velocity *= 0.5f;
            }
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            // hull and shield status
            if (changed.HasFlag(ObjectProperty.Health)) {
                outstream.Write(Hull);
                outstream.Write(Shield);
                outstream.Write(ShieldActive);
            }

            if (changed.HasFlag(ObjectProperty.HealthMax)) {
                outstream.Write(HullMax);
                outstream.Write(ShieldMax);
            }

            // movement
            if (changed.HasFlag(ObjectProperty.Throttle))
                outstream.Write(throttle);
            if (changed.HasFlag(ObjectProperty.Steering))
                outstream.Write(steering);
            if (changed.HasFlag(ObjectProperty.MoveStats)) {
                outstream.Write(TopSpeed);
                outstream.Write(TurnRate);
            }

            // mounts and weapons data
            if (changed.HasFlag(ObjectProperty.Mounts)) {
                outstream.Write(WeaponsCount);
                for (int i = 0; i < WeaponsCount; i++)
                    Mounts[i].Serialize(outstream);
            }
            if (changed.HasFlag(ObjectProperty.Weapons)) {
                outstream.Write(WeaponsCount);
                for (int i = 0; i < WeaponsCount; i++)
                    Weapons[i].Serialize(outstream);
            }

            // engine flare positions
            if (changed.HasFlag(ObjectProperty.Flares)) {
                outstream.Write((byte)flareSpawners.Count);
                for (int i = 0; i < flareSpawners.Count; i++) {
                    outstream.Write(flareSpawners[i].X);
                    outstream.Write(flareSpawners[i].Y);
                }
            }

            // combat faction
            if (changed.HasFlag(ObjectProperty.Faction))
                outstream.Write(faction);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.Health)) {
                Hull = instream.ReadSingle();
                Shield = instream.ReadSingle();
                ShieldActive = instream.ReadBoolean();
            }

            if (recvChanged.HasFlag(ObjectProperty.HealthMax)) {
                HullMax = instream.ReadSingle();
                ShieldMax = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Throttle))
                throttle = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Steering))
                steering = instream.ReadSingle();

            if (recvChanged.HasFlag(ObjectProperty.MoveStats)) {
                TopSpeed = instream.ReadSingle();
                TurnRate = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Mounts)) {
                byte mountsNum = instream.ReadByte();
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (i >= mountsNum)
                        Mounts[i] = null;
                    else
                        Mounts[i] = WeaponMount.FromStream(instream);
                }
            }

            if (recvChanged.HasFlag(ObjectProperty.Weapons)) {
                WeaponsCount = instream.ReadByte();
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (i >= WeaponsCount)
                        Weapons[i] = null;
                    else
                        Weapons[i] = Weapon.FromStream(instream);
                }
            }

            if (recvChanged.HasFlag(ObjectProperty.Flares)) {
                int flaresCount = instream.ReadByte();
                flareSpawners.Clear();
                for (int i = 0; i < flaresCount; i++)
                    flareSpawners.Add(new Vector2(instream.ReadSingle(), instream.ReadSingle()));
            }

            if (recvChanged.HasFlag(ObjectProperty.Faction))
                faction = instream.ReadByte();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "hull":
                    LuaAPI.lua_pushnumber(L, Hull);
                    break;
                case "hullmax":
                    LuaAPI.lua_pushnumber(L, HullMax);
                    break;
                case "shield":
                    LuaAPI.lua_pushnumber(L, Shield);
                    break;
                case "shieldmax":
                    LuaAPI.lua_pushnumber(L, ShieldMax);
                    break;
                case "topspeed":
                    LuaAPI.lua_pushnumber(L, TopSpeed);
                    break;
                case "turnrate":
                    LuaAPI.lua_pushnumber(L, TurnRate);
                    break;
                case "faction":
                    LuaAPI.lua_pushnumber(L, faction);
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
                case "hull":
                    Hull = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, HullMax);
                    changed |= ObjectProperty.Health;
                    break;
                case "hullmax":
                    HullMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    Hull = MathHelper.Clamp(Hull, 0f, HullMax);
                    changed |= ObjectProperty.HealthMax;
                    break;
                case "shield":
                    Shield = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, ShieldMax);
                    changed |= ObjectProperty.Health;
                    break;
                case "shieldmax":
                    ShieldMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    Shield = MathHelper.Clamp(Shield, 0f, ShieldMax);
                    changed |= ObjectProperty.HealthMax;
                    break;
                case "topspeed":
                    TopSpeed = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.MoveStats;
                    break;
                case "turnrate":
                    TurnRate = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.MoveStats;
                    break;
                case "faction":
                    faction = (byte)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Faction;
                    break;
                case "flares":
                    // table of engine flare points
                    for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                        LuaAPI.lua_rawgeti(L, 3, i + 1);
                        if (LuaAPI.lua_type(L, 4) == LuaType.Nil)
                            break;
                        if (LuaAPI.lua_type(L, 4) != LuaType.Table) {
                            LuaAPI.lua_pushstring(L, "expected tables in flares list");
                            LuaAPI.lua_error(L);
                        }
                        flareSpawners.Add(LuaAPI.lua_tovec2(L, 4));
                        LuaAPI.lua_pop(L, 1);
                    }
                    break;
                case "mounts":
                    // table of weapon mount points
                    for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                        LuaAPI.lua_rawgeti(L, 3, i + 1);
                        if (LuaAPI.lua_type(L, 4) == LuaType.Nil) {
                            Mounts[i] = null;
                        } else {
                            Mounts[i] = WeaponMount.FromLua(L, 4);
                            WeaponsCount++;
                        }
                        LuaAPI.lua_pop(L, 1);
                    }
                    changed |= ObjectProperty.Mounts;
                    break;
                case "weapons":
                    // parse the table of weapon tables
                    for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                        LuaAPI.lua_rawgeti(L, 3, i + 1);
                        if (LuaAPI.lua_type(L, 4) == LuaType.Nil)
                            Weapons[i] = null;
                        else
                            Weapons[i] = Weapon.FromLua(L, 4);
                        LuaAPI.lua_pop(L, 1);
                    }
                    changed |= ObjectProperty.Weapons;
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }
    }

}