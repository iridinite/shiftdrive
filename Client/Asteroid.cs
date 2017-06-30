/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// A <seealso cref="GameObject"/> representing a single asteroid.
    /// </summary>
    internal sealed class Asteroid : GameObject {

        private float angularVelocity;

        public Asteroid(GameState world) : base(world) {
            Type = ObjectType.Asteroid;
            Facing = Utils.RandomFloat(0f, 360f);
            SpriteName = "map/asteroid";
            Bounding = 8f;
            Damping = 0.85f;
            ZOrder = 96;
            Layer = CollisionLayer.Asteroid;
            LayerMask = CollisionLayer.Asteroid | CollisionLayer.Ship;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            Facing += angularVelocity * deltaTime;
            angularVelocity *= (float)Math.Pow(0.8f, deltaTime);
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // spin around!
            angularVelocity += (1f + penetration * 4f) * Utils.RandomFloat(-1f, 1f);

            // asteroids shouldn't move so much if ships bump into them, because
            // they should look heavy and sluggish
            this.Velocity += normal * penetration;
            if (!other.IsShip())
                this.Position += normal * penetration;
            this.changed |= ObjectProperty.Position | ObjectProperty.Velocity | ObjectProperty.AngularVelocity;
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