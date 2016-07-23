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
            color = Color.Brown;
            bounding = 10f;
        }

        public override void Update(GameState world, float deltaTime) {
        }

        public override bool IsTerrain() {
            return true;
        }

    }

}