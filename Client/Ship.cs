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

        public float hull;
        public float hullMax;
        public float shield;
        public float shieldMax;
        public bool shieldActive;

        public float topSpeed;
        public float turnRate;

        public byte mountsNum;
        public WeaponMount[] mounts;
        public Weapon[] weapons;
        public List<Vector2> flares;

        public float throttle;
        public float steering;

        public byte faction;

        private float flaretime;
        private float shieldRegenPause;

        protected Ship(GameState world) : base(world) {
            hull = 100f;
            hullMax = 100f;
            shield = 100f;
            shieldMax = 100f;
            shieldActive = false;
            damping = 0.75f;
            mountsNum = 0;
            mounts = new WeaponMount[WEAPON_ARRAY_SIZE];
            weapons = new Weapon[WEAPON_ARRAY_SIZE];
            flares = new List<Vector2>();
            flaretime = 0f;
            shieldRegenPause = 0f;
            zorder = 128;
            layer = CollisionLayer.Ship;
            layermask = CollisionLayer.Ship | CollisionLayer.Asteroid | CollisionLayer.Default;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);
            velocity = Vector2.Zero;

            // apply throttle velocity based on the ship's facing
            position += new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(facing - 90f)),
                (float)Math.Sin(MathHelper.ToRadians(facing - 90f)))
                * throttle * topSpeed * deltaTime;
            position.X = MathHelper.Clamp(position.X, 0f, 1000f);
            position.Y = MathHelper.Clamp(position.Y, 0f, 1000f);
            // apply maneuver: find whether turning left or right is fastest
            float deltaFacing = MathHelper.Clamp(Utils.Repeat((steering - facing) + 180, 0f, 360f) - 180f, -1f, 1f);
            facing = Utils.Repeat(facing + deltaFacing * turnRate * deltaTime, 0f, 360f);

            // retransmit modified properties
            if (throttle > 0f)
                changed |= ObjectProperty.Position;
            if (Math.Abs(deltaFacing) > 0.001f)
                changed |= ObjectProperty.Facing;

            // shield regeneration
            if (shield < shieldMax) {
                if (shieldRegenPause > 0f)
                    shieldRegenPause -= deltaTime;
                else
                    // TODO: Check: is local-only change sufficient?
                    // hopefully client predicts regeneration correctly, so replication is unnecessary.
                    shield = Math.Min(shield + deltaTime / 2f, shieldMax);
                    // changed |= ObjectProperty.Health;
            }

            // update weapon charge / ammo states
            for (int i = 0; i < mountsNum; i++) {
                Weapon wep = weapons[i];
                if (wep == null) continue;
                if (mounts[i] == null) continue;

                // out-of-ammo processing
                if (wep.Ammo != AmmoType.None && wep.AmmoLeft < wep.AmmoPerShot) {
                    // need ammo reserve
                    if (wep.AmmoClipsLeft < 1 && wep.Ammo != AmmoType.Dummy)
                        continue;

                    // begin reloading
                    if (wep.ReloadProgress < wep.ReloadTime) {
                        wep.ReloadProgress += deltaTime;
                        continue;
                    }
                    wep.ReloadProgress = 0f;
                    wep.AmmoLeft = wep.AmmoPerClip;
                    if (this.type == ObjectType.PlayerShip && // AI ships have unlimited clips
                        wep.Ammo != AmmoType.Dummy) // dummy ammo has no clips
                        wep.AmmoClipsLeft--;
                }

                // find a target to fire upon
                GameObject target = SelectTarget(mounts[i], wep);
                if (target == null) continue;

                // increment charge
                wep.Charge += deltaTime;
                if (wep.Charge < wep.ChargeTime) continue;
                wep.Charge = 0f;

                // remove ammo for this shot
                if (wep.Ammo != AmmoType.None)
                    wep.AmmoLeft -= wep.AmmoPerShot;

                // if running on server, fire the weapon
                if (!world.IsServer) continue;
                float randombearing = (float)Utils.RNG.NextDouble() * wep.ProjSpread * 2 - wep.ProjSpread;
                NetServer.world.AddObject(new Projectile(NetServer.world, wep.ProjSprite,
                    position + mounts[i].Position,
                    Utils.Repeat(Utils.CalculateBearing(this.position, target.position) + randombearing, 0f, 360f), wep.ProjSpeed, wep.Damage,
                    this.faction));

                // consume fuel for weapon fire
                if (this.type == ObjectType.PlayerShip) {
                    PlayerShip plr = this as PlayerShip;
                    Debug.Assert(plr != null);
                    plr.ConsumeFuel(wep.PowerDraw);
                }
            }


            // server-side stuff
            if (world.IsServer) {
                // update mount point position
                for (int i = 0; i < mountsNum; i++) {
                    if (mounts[i] == null) continue;
                    mounts[i].Position = Utils.CalculateRotatedOffset(mounts[i].Offset, facing);
                }
            }

            // engine flares
            if (!(throttle > 0f) || world.IsServer) return;
            if (flaretime > 0f) { // space out evenly
                flaretime -= deltaTime;
                return;
            }
            flaretime = 0.01f;
            // create particles for engine exhaust
            foreach (Vector2 flarepos in flares) {
                Particle flare = new Particle();
                flare.lifemax = 3f;
                flare.sprite = Assets.GetSprite("map/engineflare").Clone();
                flare.scalestart = 1.1f;
                flare.scaleend = 0.9f;
                flare.colorstart = Color.White * (throttle * 0.6f + 0.15f);
                flare.colorend = Color.Transparent;
                flare.facing = facing;
                flare.zorder = 160;
                flare.position = position + Utils.CalculateRotatedOffset(flarepos, facing);
                ParticleManager.Register(flare);
            }
        }

        public override void TakeDamage(float damage) {
            // need to resend hull and shields
            changed |= ObjectProperty.Health;

            // delay shield recharge
            shieldRegenPause = 10f;

            // apply damage to shields first, if possible
            if (shieldActive && shield > 0f) {
                shield = MathHelper.Clamp(shield - damage, 0f, shieldMax);
                return;
            }
            // otherwise, apply damage to hull
            hull = MathHelper.Clamp(hull - damage, 0f, hullMax);
            // zero hull = ship destruction
            if (hull <= 0f && world.IsServer) Destroy();
        }

        public override void Destroy() {
            if (!IsDestroyScheduled())
                NetServer.PublishParticleEffect(ParticleEffect.Explosion, position);
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

        protected virtual GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject target = null;
            float closest = float.MaxValue;

            // find closest object. station has 360 weapon so don't care about weapon arcs
            foreach (GameObject gobj in world.Objects.Values) {
                // make sure we can actually shoot this thing
                if (!GetCanTarget(gobj, weapon.Range, mount.Bearing + this.facing, mount.Arc))
                    continue;

                // keep closest object
                float dist = Vector2.DistanceSquared(gobj.position, this.position);
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
            if (Vector2.DistanceSquared(target.position, this.position) > range * range)
                return false;

            // cannot fall outside weapon arc
            float targetAngle = Utils.CalculateBearing(this.position, target.position);
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
            float highestVelocity = throttle * topSpeed * Math.Abs(penetration);
            // if colliding with another ship, factor in that ship's speed
            Ship otherShip = other as Ship;
            if (otherShip != null)
                highestVelocity = Math.Max(highestVelocity,
                    otherShip.throttle * otherShip.topSpeed * Math.Abs(penetration));

            // cap damage and apply
            float damage = Math.Min(highestVelocity * SDGame.Inst.GetDeltaTime() * 2, 0.25f);
            TakeDamage(damage);

            if (other.type == ObjectType.Asteroid) {
                // reduce pushback
                velocity *= 0.5f;
            }
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            // hull and shield status
            if (changed.HasFlag(ObjectProperty.Health)) {
                outstream.Write(hull);
                outstream.Write(shield);
                outstream.Write(shieldActive);
            }

            if (changed.HasFlag(ObjectProperty.HealthMax)) {
                outstream.Write(hullMax);
                outstream.Write(shieldMax);
            }

            // movement
            if (changed.HasFlag(ObjectProperty.Throttle))
                outstream.Write(throttle);
            if (changed.HasFlag(ObjectProperty.Steering))
                outstream.Write(steering);
            if (changed.HasFlag(ObjectProperty.MoveStats)) {
                outstream.Write(topSpeed);
                outstream.Write(turnRate);
            }

            // mounts and weapons data
            if (changed.HasFlag(ObjectProperty.Mounts)) {
                outstream.Write(mountsNum);
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (weapons[i] != null) {
                        outstream.Write((byte)1);
                        weapons[i].Serialize(outstream);
                    } else {
                        outstream.Write((byte)0);
                    }
                }
            }

            // engine flare positions
            if (changed.HasFlag(ObjectProperty.Flares)) {
                outstream.Write((byte)flares.Count);
                for (int i = 0; i < flares.Count; i++) {
                    outstream.Write(flares[i].X);
                    outstream.Write(flares[i].Y);
                }
            }

            // combat faction
            if (changed.HasFlag(ObjectProperty.Faction))
                outstream.Write(faction);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.Health)) {
                hull = instream.ReadSingle();
                shield = instream.ReadSingle();
                shieldActive = instream.ReadBoolean();
            }

            if (recvChanged.HasFlag(ObjectProperty.HealthMax)) {
                hullMax = instream.ReadSingle();
                shieldMax = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Throttle))
                throttle = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Steering))
                steering = instream.ReadSingle();

            if (recvChanged.HasFlag(ObjectProperty.MoveStats)) {
                topSpeed = instream.ReadSingle();
                turnRate = instream.ReadSingle();
            }

            if (recvChanged.HasFlag(ObjectProperty.Mounts)) {
                mountsNum = instream.ReadByte();
                for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                    if (instream.ReadByte() == 1) {
                        weapons[i] = Weapon.FromStream(instream);
                    } else {
                        weapons[i] = null;
                    }
                }
            }

            if (recvChanged.HasFlag(ObjectProperty.Flares)) {
                int flaresCount = instream.ReadByte();
                flares.Clear();
                for (int i = 0; i < flaresCount; i++)
                    flares.Add(new Vector2(instream.ReadSingle(), instream.ReadSingle()));
            }

            if (recvChanged.HasFlag(ObjectProperty.Faction))
                faction = instream.ReadByte();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "hull":
                    LuaAPI.lua_pushnumber(L, hull);
                    break;
                case "hullmax":
                    LuaAPI.lua_pushnumber(L, hullMax);
                    break;
                case "shield":
                    LuaAPI.lua_pushnumber(L, shield);
                    break;
                case "shieldmax":
                    LuaAPI.lua_pushnumber(L, shieldMax);
                    break;
                case "topspeed":
                    LuaAPI.lua_pushnumber(L, topSpeed);
                    break;
                case "turnrate":
                    LuaAPI.lua_pushnumber(L, turnRate);
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
                    hull = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, hullMax);
                    changed |= ObjectProperty.Health;
                    break;
                case "hullmax":
                    hullMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    hull = MathHelper.Clamp(hull, 0f, hullMax);
                    changed |= ObjectProperty.HealthMax;
                    break;
                case "shield":
                    shield = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, shieldMax);
                    changed |= ObjectProperty.Health;
                    break;
                case "shieldmax":
                    shieldMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    shield = MathHelper.Clamp(shield, 0f, shieldMax);
                    changed |= ObjectProperty.HealthMax;
                    break;
                case "topspeed":
                    topSpeed = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.MoveStats;
                    break;
                case "turnrate":
                    turnRate = (float)LuaAPI.luaL_checknumber(L, 3);
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
                        if (LuaAPI.lua_type(L, 4) == LuaAPI.LUA_TNIL)
                            break;
                        if (LuaAPI.lua_type(L, 4) != LuaAPI.LUA_TTABLE) {
                            LuaAPI.lua_pushstring(L, "expected tables in flares list");
                            LuaAPI.lua_error(L);
                        }
                        flares.Add(LuaAPI.lua_tovec2(L, 4));
                        LuaAPI.lua_pop(L, 1);
                    }
                    break;
                case "mounts":
                    // table of weapon mount points
                    for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                        LuaAPI.lua_rawgeti(L, 3, i + 1);
                        if (LuaAPI.lua_type(L, 4) == LuaAPI.LUA_TNIL) {
                            mounts[i] = null;
                        } else {
                            mounts[i] = WeaponMount.FromLua(L, 4);
                            mountsNum++;
                        }
                        LuaAPI.lua_pop(L, 1);
                    }
                    changed |= ObjectProperty.Mounts;
                    break;
                case "weapons":
                    // parse the table of weapon tables
                    for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                        LuaAPI.lua_rawgeti(L, 3, i + 1);
                        if (LuaAPI.lua_type(L, 4) == LuaAPI.LUA_TNIL)
                            weapons[i] = null;
                        else
                            weapons[i] = Weapon.FromLua(L, 4);
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