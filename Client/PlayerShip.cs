/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
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
                    NetServer.PublishAnnouncement(AnnouncementId.Hull25, null);
                    hullWarningShown = 3;
                } else if (hullFraction <= 0.5f && hullWarningShown < 2) {
                    NetServer.PublishAnnouncement(AnnouncementId.Hull50, null);
                    hullWarningShown = 2;
                } else if (hullFraction <= 0.75f && hullWarningShown < 1) {
                    NetServer.PublishAnnouncement(AnnouncementId.Hull75, null);
                    hullWarningShown = 1;
                } else if (hullFraction > 0.75f) {
                    // reset warnings if above 75%
                    hullWarningShown = 0;
                }

                // fuel reserves warnings
                if (fuel < 1f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelCritical, null);
                else if (fuel < 3f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelLow, null);

                // shield strength warnings
                if (shieldActive && shield <= 0f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldDown, null);
                else if (shieldActive && shield / shieldMax <= 0.25f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldLow, null);
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
            Particle.CreateExplosion(NetServer.world, position);
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            if (destroyed) return;
            base.OnCollision(other, normal, penetration);
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);

            writer.Write(destroyed);
            writer.Write(player);
            writer.Write(fuel);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            destroyed = reader.ReadBoolean();
            player = reader.ReadByte();
            fuel = reader.ReadSingle();
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