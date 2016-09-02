/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents an AI-controlled ship.
    /// </summary>
    internal sealed class AIShip : Ship {

        private readonly List<AITask> brain;

        private float travelAlignTime;
        private float travelRandTime;
        private Vector2 travelDest;

        public AIShip(GameState world) : base(world) {
            brain = new List<AITask>();
            brain.Add(AITask.TravelDestination);
            brain.Add(AITask.TravelRandomize);
            brain.Add(AITask.AvoidBlackHole);
            type = ObjectType.AIShip;
        }

        public override void Update(float deltaTime) {
            // walk through the AI tasks
            foreach (AITask task in brain) {
                switch (task) {
                    case AITask.TravelDestination:
                        // calculate bearing towards current target and head there
                        travelAlignTime -= deltaTime;
                        if (travelAlignTime > 0f) continue;
                        travelAlignTime = 6f;
                        this.steering = Utils.CalculateBearing(position, travelDest);
                        this.throttle = MathHelper.Max(1f, MathHelper.Clamp(Vector2.Distance(position, travelDest), 0f, 1f));
                        break;

                    case AITask.TravelRandomize:
                        // occasionally pick a new travel destination
                        travelRandTime -= deltaTime;
                        if (!(travelRandTime <= 0f)) break;
                        travelRandTime = (float)(Utils.RNG.NextDouble() * 15.0 + 10.0);
                        travelDest = new Vector2(Utils.RNG.Next(0, 1000), Utils.RNG.Next(0, 1000));
                        break;

                    case AITask.AvoidBlackHole:
                        // detect nearby black holes and steer away from them
                        foreach (GameObject obj in world.Objects.Values) {
                            if (obj.type != ObjectType.BlackHole) continue;
                            if (!(Vector2.DistanceSquared(obj.position, this.position) <= 40000)) continue; // 200 units
                            // calculate 'away' vector to steer away from the black hole
                            Vector2 safedir = this.position - obj.position;
                            this.steering = Utils.CalculateBearing(position, position + safedir);
                        }
                        break;
                }
            }

            base.Update(deltaTime);
        }

        protected override GameObject SelectTarget(WeaponMount mount, Weapon weapon) {
            return null;
        }

    }

}
