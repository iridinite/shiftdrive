/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    internal sealed class SpaceStation : Ship {

        public SpaceStation(GameState world) : base(world) {
            type = ObjectType.Station;
            zorder = 220;
            bounding = 20f;
            layer = CollisionLayer.Station;
            layermask = CollisionLayer.None;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // no action; station doesn't care about collision
        }

        public override void Update(float deltaTime) {
            // station never moves
            throttle = 0f;
            facing = 0f;
            steering = 0f;
            // base update
            base.Update(deltaTime);
        }
    }

}
