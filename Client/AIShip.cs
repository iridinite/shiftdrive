/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

namespace ShiftDrive {

    /// <summary>
    /// Represents an AI-controlled ship.
    /// </summary>
    internal sealed class AIShip : Ship {

        public AIShip() {
            type = ObjectType.AIShip;
        }

        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);
        }

    }

}
