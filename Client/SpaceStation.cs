/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a space station that can be docked to.
    /// </summary>
    internal sealed class SpaceStation : Ship {

        public SpaceStation(GameState world) : base(world) {
            Type = ObjectType.Station;
            ZOrder = 220;
            Bounding = 20f;
            Layer = CollisionLayer.Station;
            LayerMask = CollisionLayer.None;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // no action; station doesn't care about collision
        }

        public override void Update(float deltaTime) {
            // station never moves
            throttle = 0f;
            Facing = 0f;
            steering = 0f;
            // base update
            base.Update(deltaTime);
        }

    }

}
