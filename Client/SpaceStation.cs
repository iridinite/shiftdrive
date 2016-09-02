/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.Diagnostics;
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

        protected override GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            GameObject target = null;
            float closest = float.MaxValue;

            // find closest object. station has 360 weapon so don't care about weapon arcs
            foreach (GameObject gobj in world.Objects.Values) {
                // make sure we can actually shoot this thing
                if (!gobj.IsTargetable()) continue;
                // must be hostile
                if (gobj.IsShip()) {
                    Ship ship = gobj as Ship;
                    Debug.Assert(ship != null);
                    if (ship.IsAlly(this)) continue;
                }
                // calc distance, skip if out of range or not closest
                float dist = Vector2.DistanceSquared(gobj.position, this.position);
                if (dist > weapon.Range * weapon.Range) continue;
                if (dist > closest) continue;

                closest = dist;
                target = gobj;
            }

            return target;
        }

    }

}
