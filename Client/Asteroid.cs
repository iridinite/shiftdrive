/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// A <seealso cref="GameObject"/> representing a single asteroid.
    /// </summary>
    internal sealed class Asteroid : GameObject {

        private float angularVelocity;

        public Asteroid(GameState world) : base(world) {
            type = ObjectType.Asteroid;
            facing = Utils.RNG.Next(0, 360);
            spritename = "map/asteroid";
            color = Color.White;
            bounding = 8f;
            damping = 0.85f;
            zorder = 96;
            layer = CollisionLayer.Asteroid;
            layermask = CollisionLayer.Asteroid | CollisionLayer.Ship;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            facing += angularVelocity * deltaTime;
            angularVelocity *= (float)Math.Pow(0.8f, deltaTime);

            // re-transmit object if it's moving around
            if (Math.Abs(angularVelocity) > 0.001f)
                changed |= ObjectProperty.AngularVelocity;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // spin around!
            angularVelocity += (1f + penetration * 4f) * Utils.RandomFloat(-1f, 1f);

            // asteroids shouldn't move so much if ships bump into them, because
            // they should look heavy and sluggish
            this.velocity += normal * penetration;
            if (!other.IsShip()) {
                this.position += normal * penetration;
            }
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);
            if (changed.HasFlag(ObjectProperty.AngularVelocity))
                outstream.Write(angularVelocity);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);
            if (recvChanged.HasFlag(ObjectProperty.AngularVelocity))
                angularVelocity = instream.ReadSingle();
        }

    }

}