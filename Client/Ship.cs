/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
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

        public float throttle;
        public float steering;

        public byte faction;

        private bool needRetransmit;

        protected Ship(GameState world) : base(world) {
            needRetransmit = true;
            hull = 100f;
            hullMax = 100f;
            shield = 100f;
            shieldMax = 100f;
            shieldActive = false;
            damping = 0.75f;
            mountsNum = 0;
            mounts = new WeaponMount[WEAPON_ARRAY_SIZE];
            weapons = new Weapon[WEAPON_ARRAY_SIZE];
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

            // fire ze missiles
            if (world.IsServer) { // only fire projectiles on the server
                for (int i = 0; i < mountsNum; i++) {
                    // update mount point position
                    if (mounts[i] == null) continue;
                    float relangle = Utils.CalculateBearing(Vector2.Zero, mounts[i].Offset);

                    mounts[i].Position = new Vector2(
                        mounts[i].OffsetMag * (float)Math.Cos(MathHelper.ToRadians(facing + relangle + 90f)),
                        mounts[i].OffsetMag * (float)Math.Sin(MathHelper.ToRadians(facing + relangle + 90f)));

                    // update weapon charges
                    if (weapons[i] == null) continue;
                    Weapon wep = weapons[i];
                    wep.Charge += deltaTime;
                    if (wep.Charge < wep.ChargeTime) continue;

                    wep.Charge = 0f;
                    float randombearing = (float)Utils.RNG.NextDouble() * wep.ProjSpread * 2 - wep.ProjSpread;
                    //NetServer.AddObject(new Projectile(wep.ProjSprite, position + mounts[i].Position, Utils.Repeat(facing + mounts[i].Bearing + randombearing, 0f, 360f), wep.ProjSpeed, this.faction));
                }
            }

            changed = changed || throttle > 0f || Math.Abs(deltaFacing) > 0.001f;
        }

        public override void TakeDamage(float damage) {
            // always retransmit
            changed = true;
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
                Particle.CreateExplosion(NetServer.world, position);
            base.Destroy();
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

            TakeDamage(highestVelocity * SDGame.Inst.GetDeltaTime() * 2);

            if (other.type == ObjectType.Asteroid) {
                // reduce pushback
                velocity *= 0.5f;
            }
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            
            writer.Write(hull);
            writer.Write(shield);
            writer.Write(shieldActive);

            writer.Write(throttle);
            writer.Write(steering);

            writer.Write(needRetransmit);
            if (!needRetransmit) return;
            needRetransmit = false;

            writer.Write(hullMax);
            writer.Write(shieldMax);
            writer.Write(topSpeed);
            writer.Write(turnRate);

            writer.Write(mountsNum);
            for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                if (weapons[i] != null) {
                    writer.Write((byte)1);
                    weapons[i].Serialize(writer);
                } else {
                    writer.Write((byte)0);
                }
            }

            writer.Write(faction);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            hull = reader.ReadSingle();
            shield = reader.ReadSingle();
            shieldActive = reader.ReadBoolean();

            throttle = reader.ReadSingle();
            steering = reader.ReadSingle();

            if (!reader.ReadBoolean()) return;

            hullMax = reader.ReadSingle();
            shieldMax = reader.ReadSingle();
            topSpeed = reader.ReadSingle();
            turnRate = reader.ReadSingle();

            mountsNum = reader.ReadByte();
            for (int i = 0; i < WEAPON_ARRAY_SIZE; i++) {
                if (reader.ReadByte() == 1) {
                    weapons[i] = Weapon.FromStream(reader);
                } else {
                    weapons[i] = null;
                }
            }

            faction = reader.ReadByte();
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
                    break;
                case "hullmax":
                    hullMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    hull = MathHelper.Clamp(hull, 0f, hullMax);
                    needRetransmit = true;
                    break;
                case "shield":
                    shield = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, shieldMax);
                    break;
                case "shieldmax":
                    shieldMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    shield = MathHelper.Clamp(shield, 0f, shieldMax);
                    needRetransmit = true;
                    break;
                case "topspeed":
                    topSpeed = (float)LuaAPI.luaL_checknumber(L, 3);
                    needRetransmit = true;
                    break;
                case "turnrate":
                    turnRate = (float)LuaAPI.luaL_checknumber(L, 3);
                    needRetransmit = true;
                    break;
                case "faction":
                    faction = (byte)LuaAPI.luaL_checknumber(L, 3);
                    needRetransmit = true;
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
                    needRetransmit = true;
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
                    needRetransmit = true;
                    break;
                default:
                    return base.LuaSet(L);
            }
            changed = true;
            return 0;
        }
    }

}