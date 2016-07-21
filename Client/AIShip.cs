/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents an AI-controlled ship.
    /// </summary>
    internal sealed class AIShip : Ship {

        public AIShip() {
            type = ObjectType.AIShip;
            iconfile = "player";
            iconcolor = Color.Blue;
            bounding = 10f;
        }

        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);
        }

    }

}
