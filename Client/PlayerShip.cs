/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a player-controlled ship.
    /// </summary>
    internal sealed class PlayerShip : Ship {

        public byte player;

        // Fuel reserves.
        // Integer part is fuel cell count, decimal part is reservoir.
        public float fuel;

        // indicates that the ship has been destroyed (not scheduled for deletion)
        public bool destroyed;

        public List<uint> targets;

        private int hullWarningShown;

        public PlayerShip(GameState world) : base(world) {
            type = ObjectType.PlayerShip;
            targets = new List<uint>();
            destroyed = false;
        }

        public override void Update(float deltaTime) {
            // force stand-still if destroyed
            if (destroyed) return;

            // base update: throttle, steering, weapons
            base.Update(deltaTime);

            // ship operation and throttle eats up energy
            ConsumeFuel(deltaTime * 0.004f);
            ConsumeFuel(throttle * deltaTime * 0.0083333f);

            // ship state announcements
            if (world.IsServer) {
                // hull integrity warnings
                float hullFraction = hull / hullMax;
                if (hullFraction <= 0.25f && hullWarningShown < 3) {
                    NetServer.PublishAnnouncement(AnnouncementId.Hull25);
                    hullWarningShown = 3;
                } else if (hullFraction <= 0.5f && hullWarningShown < 2) {
                    NetServer.PublishAnnouncement(AnnouncementId.Hull50);
                    hullWarningShown = 2;
                } else if (hullFraction <= 0.75f && hullWarningShown < 1) {
                    NetServer.PublishAnnouncement(AnnouncementId.Hull75);
                    hullWarningShown = 1;
                } else if (hullFraction > 0.75f) {
                    // reset warnings if above 75%
                    hullWarningShown = 0;
                }

                // fuel reserves warnings
                if (fuel < 1f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelCritical);
                else if (fuel < 3f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelLow);

                // shield strength warnings
                if (shieldActive && shield <= 0f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldDown);
                else if (shieldActive && shield / shieldMax <= 0.25f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldLow);
            }
        }

        public override void Destroy() {
            // override because we do not want player ships to be scheduled for deletion,
            // that would cause null ref exceptions on the clients.

            // run only once
            if (destroyed || !world.IsServer) return;
            destroyed = true;
            // deplete hull bar, in case it wasn't empty yet
            hull = 0f;
            // stop moving
            throttle = 0f;
            // disable all collision and physics (black holes, in particular)
            bounding = 0f;
            layer = CollisionLayer.None;
            layermask = CollisionLayer.None;
            // fancy display
            NetServer.PublishParticleEffect(ParticleEffect.Explosion, position);
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            if (destroyed) return;
            base.OnCollision(other, normal, penetration);
        }

        protected override GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject bestTarget = null;
            float closest = float.MaxValue;

            foreach (uint targetid in targets) {
                if (!world.Objects.ContainsKey(targetid)) continue;
                GameObject target = world.Objects[targetid];

                // make sure we can actually shoot this thing
                if (!GetCanTarget(target, weapon.Range, mount.Bearing + this.facing, mount.Arc))
                    continue;

                // keep closest object
                float dist = Vector2.DistanceSquared(target.position, this.position);
                if (dist > closest) continue;

                closest = dist;
                bestTarget = target;
            }

            return bestTarget;
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            outstream.Write(destroyed);
            outstream.Write(player);
            outstream.Write(fuel);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            destroyed = instream.ReadBoolean();
            player = instream.ReadByte();
            fuel = instream.ReadSingle();
        }

        public void ConsumeFuel(float amount) {
            fuel -= amount;
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "fuel":
                    LuaAPI.lua_pushnumber(L, fuel);
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
                case "fuel":
                    fuel = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}