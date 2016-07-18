/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// A <seealso cref="NamedObject"/> representing a gravitational singularity.
    /// </summary>
    internal sealed class BlackHole : NamedObject {

        public BlackHole() {
            type = ObjectType.BlackHole;
            facing = 0f;
            iconfile = "blackhole";
            iconcolor = Color.Blue;
            bounding = 0f; // no bounding sphere; Update handles gravity pull
        }

        public override void Update(GameState world, float deltaTime) {
            // simulate gravitational pull
            foreach (GameObject gobj in world.Objects) {
                // black holes don't affect themselves...
                if (gobj.type == ObjectType.BlackHole) continue;

                // objects closer than 140 units are affected by the grav pull
                if (!(Vector2.DistanceSquared(gobj.position, this.position) < 19600)) continue;

                // pull this object in closer
                Vector2 pulldir = Vector2.Normalize(position - gobj.position);
                float pullpower = 1f - Vector2.Distance(gobj.position, this.position) / 140f;
                gobj.position += pulldir * pullpower * pullpower * deltaTime * 40f;

                // objects that are too close to the center are damaged
                if (pullpower >= 0.35f) gobj.TakeDamage(pullpower * pullpower * deltaTime * 10f);

                // and stuff at the center is simply destroyed
                if (pullpower >= 0.9f) gobj.Destroy();
            }
        }

    }

}