/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a player-controlled ship.
    /// </summary>
    internal sealed class PlayerShip : Ship {

        /// <summary>
        /// Network ID for this group of players. Unused, until multi-ships happen.
        /// </summary>
        public byte PlayerID { get; private set; }

        /// <summary>
        /// Fuel reserves.
        /// Integer part is fuel cell count, decimal part is reservoir.
        /// </summary>
        public float Fuel { get; private set; }

        /// <summary>
        /// Indicates whether the ship has been disabled / hidden.
        /// This is easier than outright deleting the object because it avoids having to
        /// implement null checks everywhere.
        /// </summary>
        public bool Destroyed { get; private set; }

        /// <summary>
        /// The list of objects targeted by the Weapons officer.
        /// </summary>
        public List<uint> Targets { get; }

        private int hullWarningShown;

        public PlayerShip(GameState world) : base(world) {
            Type = ObjectType.PlayerShip;
            Targets = new List<uint>();
            Destroyed = false;
        }

        public override void Update(float deltaTime) {
            // force stand-still if destroyed
            if (Destroyed) return;

            // base update: throttle, steering, weapons
            base.Update(deltaTime);

            // remove targets that are no longer in view
            for (int i = Targets.Count - 1; i >= 0; i--) {
                uint targetid = Targets[i];
                // object no longer exists?
                if (!World.Objects.ContainsKey(targetid)) {
                    Targets.RemoveAt(i);
                    continue;
                }

                GameObject obj = World.Objects[targetid];
                // object about to be deleted, or too far away?
                if (obj.IsDestroyScheduled() || Vector2.Distance(obj.Position, this.Position) > 300f)
                    Targets.RemoveAt(i);
            }

            // ship operation and throttle eats up energy
            ConsumeFuel(deltaTime * 0.004f);
            ConsumeFuel(throttle * deltaTime * 0.0083333f);

            // ship state announcements
            if (World.IsServer) {
                // hull integrity warnings
                float hullFraction = Hull / HullMax;
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
                if (Fuel < 1f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelCritical);
                else if (Fuel < 3f)
                    NetServer.PublishAnnouncement(AnnouncementId.FuelLow);

                // shield strength warnings
                if (ShieldActive && Shield <= 0f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldDown);
                else if (ShieldActive && Shield / ShieldMax <= 0.25f)
                    NetServer.PublishAnnouncement(AnnouncementId.ShieldLow);
            }
        }

        public override void Destroy() {
            // override because we do not want player ships to be scheduled for deletion,
            // that would cause null ref exceptions on the clients.

            // run only once
            if (Destroyed || !World.IsServer) return;
            Destroyed = true;
            // deplete hull bar, in case it wasn't empty yet
            Hull = 0f;
            Shield = 0f;
            // stop moving
            throttle = 0f;
            // disable all collision and physics (black holes, in particular)
            Bounding = 0f;
            Layer = CollisionLayer.None;
            LayerMask = CollisionLayer.None;
            // all the stuff changed
            changed |= ObjectProperty.Health | ObjectProperty.Layer | ObjectProperty.Throttle | ObjectProperty.Bounding;
            // fancy display
            Assets.GetSound("ExplosionMedium").Play3D(World.GetPlayerShip().Position, Position);
            NetServer.PublishParticleEffect(ParticleEffect.Explosion, Position);
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            if (Destroyed) return;
            base.OnCollision(other, normal, penetration);
        }

        public override GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject bestTarget = null;
            float closest = float.MaxValue;

            foreach (uint targetid in Targets) {
                if (!World.Objects.ContainsKey(targetid)) continue;
                GameObject target = World.Objects[targetid];

                // make sure we can actually shoot this thing
                if (!GetCanTarget(target, weapon.Range, mount.Bearing + this.Facing, mount.Arc))
                    continue;

                // keep closest object
                float dist = Vector2.DistanceSquared(target.Position, this.Position);
                if (dist > closest) continue;

                closest = dist;
                bestTarget = target;
            }

            return bestTarget;
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            if (changed.HasFlag(ObjectProperty.Targets)) {
                outstream.Write((byte)Targets.Count);
                foreach (var target in Targets)
                    outstream.Write(target);
            }

            if (changed.HasFlag(ObjectProperty.PlayerData))
                outstream.Write(PlayerID);

            outstream.Write(Destroyed);
            outstream.Write(Fuel);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.Targets)) {
                int targetCount = instream.ReadByte();
                Targets.Clear();
                for (int i = 0; i < targetCount; i++)
                    Targets.Add(instream.ReadUInt32());
            }

            if (recvChanged.HasFlag(ObjectProperty.PlayerData))
                PlayerID = instream.ReadByte();

            Destroyed = instream.ReadBoolean();
            Fuel = instream.ReadSingle();
        }

        public void ConsumeFuel(float amount) {
            Fuel -= amount;
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "fuel":
                    LuaAPI.lua_pushnumber(L, Fuel);
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
                    Fuel = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}
