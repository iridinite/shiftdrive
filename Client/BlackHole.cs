/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
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
            type = ObjectType.BlackHole;
            facing = 0f;
            spritename = "map/blackhole";
            bounding = 0f; // no bounding sphere; Update handles gravity pull
            zorder = 250;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // simulate gravitational pull
            IEnumerable<uint> keys = world.Objects.Keys.OrderByDescending(k => k);
            foreach (uint key in keys) {
                GameObject gobj = world.Objects[key];
                // black holes don't affect themselves...
                if (gobj.type == ObjectType.BlackHole) continue;

                // exclude objects that have no physics
                if (gobj.layermask == CollisionLayer.None || gobj.bounding <= 0f) continue;

                // objects closer than 140 units are affected by the grav pull
                if (!(Vector2.DistanceSquared(gobj.position, this.position) < 19600)) continue;

                // pull this object in closer
                Vector2 pulldir = Vector2.Normalize(position - gobj.position);
                float pullpower = 1f - Vector2.Distance(gobj.position, this.position) / 140f;
                gobj.position += pulldir * pullpower * pullpower * deltaTime * 40f;
                gobj.changed |= ObjectProperty.Position;

                // notify player ship
                if (pullpower >= 0.1f && gobj.type == ObjectType.PlayerShip && world.IsServer)
                    NetServer.PublishAnnouncement(AnnouncementId.BlackHole);

                // objects that are too close to the center are damaged
                if (pullpower >= 0.35f) gobj.TakeDamage(pullpower * pullpower * deltaTime * 10f);

                // and stuff at the center is simply destroyed
                if (pullpower >= 0.95f) gobj.Destroy();
            }
        }
    }

}