/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// A <seealso cref="NamedObject"/> representing a gravitational singularity.
    /// </summary>
    internal sealed class BlackHole : NamedObject {

        public BlackHole(GameState world) : base(world) {
            Type = ObjectType.BlackHole;
            Facing = 0f;
            SpriteName = "map/blackhole";
            Bounding = 0f; // no bounding sphere; Update handles gravity pull
            ZOrder = 250;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // simulate gravitational pull
            IEnumerable<uint> keys = World.Objects.Keys.OrderByDescending(k => k);
            foreach (uint key in keys) {
                GameObject gobj = World.Objects[key];
                // black holes don't affect themselves...
                if (gobj.Type == ObjectType.BlackHole) continue;

                // exclude objects that have no physics
                if (gobj.LayerMask == CollisionLayer.None
                    || gobj.IsDestroyScheduled()
                    || gobj.Bounding <= 0f) continue;

                // objects closer than 140 units are affected by the grav pull
                if (!(Vector2.DistanceSquared(gobj.Position, this.Position) < 19600)) continue;

                // find the direction in which to pull the object.
                // extra check makes sure objects at center don't get pulled to NaN/NaN
                Vector2 pulldir = Vector2.Normalize(Position - gobj.Position);
                if (!(pulldir.LengthSquared() > 0f)) continue;

                // pull this object in closer
                float pullpower = 1f - Vector2.Distance(gobj.Position, this.Position) / 140f;
                gobj.Position += pulldir * pullpower * pullpower * deltaTime * 60f;
                gobj.changed |= ObjectProperty.Position;

                // notify player ship
                if (pullpower >= 0.1f && gobj.Type == ObjectType.PlayerShip && World.IsServer)
                    NetServer.PublishAnnouncement(AnnouncementId.BlackHole);

                // objects that are too close to the center are damaged
                if (pullpower >= 0.2f) gobj.TakeDamage(pullpower * pullpower * deltaTime * 35f);

                // and stuff at the center is simply destroyed
                if (pullpower >= 0.95f) gobj.Destroy();
            }
        }
    }

}