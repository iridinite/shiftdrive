/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// A <seealso cref="GameObject"/> representing a single asteroid.
    /// </summary>
    internal sealed class Asteroid : GameObject {

        public Asteroid() {
            type = ObjectType.Asteroid;
            facing = Utils.RNG.Next(0, 360);
            spritename = "map/asteroid";
            color = Color.White;
            bounding = 7f;
        }

        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);
        }

        protected override void OnCollision(GameObject other, float dist) {
            base.OnCollision(other, dist);

            // asteroids shouldn't move so much if ships bump into them, because
            // they should look heavy and sluggish
            if (other.type == ObjectType.AIShip || other.type == ObjectType.PlayerShip)
                this.velocity *= 0.5f;
        }

        public override bool IsTerrain() {
            return true;
        }

    }

}