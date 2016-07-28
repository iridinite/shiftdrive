/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
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

        public Asteroid() {
            type = ObjectType.Asteroid;
            facing = Utils.RNG.Next(0, 360);
            spritename = "map/asteroid";
            color = Color.White;
            bounding = 7f;
        }

        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);

            facing += angularVelocity * deltaTime;
            angularVelocity *= (float)Math.Pow(0.8f, deltaTime);
            // re-transmit object if it's moving around
            changed = changed || angularVelocity > 0.01f;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            base.OnCollision(other, normal, penetration);

            // spin around!
            angularVelocity += (1f + penetration * 4f) * (float)(Utils.RNG.NextDouble() * 2.0 - 1.0);

            // asteroids shouldn't move so much if ships bump into them, because
            // they should look heavy and sluggish
            if (other.type == ObjectType.AIShip || other.type == ObjectType.PlayerShip)
                this.velocity *= 0.8f;
        }

        public override bool IsTerrain() {
            return true;
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(angularVelocity);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);
            angularVelocity = reader.ReadSingle();
        }

    }

}