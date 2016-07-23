/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a projectile in flight.
    /// </summary>
    internal sealed class Projectile : GameObject {
        
        public Vector2 velocity;
        public float lifetime;
        public byte faction;

        public Projectile() {
            type = ObjectType.Projectile;
            color = Color.White;
        }

        public Projectile(string spritename, Vector2 position, float facing, float speed, byte faction) : this() {
            this.spritename = spritename;
            //this.sprite = Assets.GetTexture(spritename);
            this.position = position;
            this.facing = facing;
            this.velocity = speed * new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(facing - 90f)),
                (float)Math.Sin(MathHelper.ToRadians(facing - 90f)));
            this.faction = faction;
            this.lifetime = 0f;
        }

        public override void Update(GameState world, float deltaTime) {
            // move forward
            position += velocity * deltaTime;
            lifetime += deltaTime;

            // TODO: collision detection

            // get rid of projectiles that have traveled very far already
            if (world.IsServer && lifetime >= 10f) Destroy();

            // as these move every frame, always re-send them
            changed = true;
        }

        public override void Destroy() {
            // detonate
            base.Destroy();
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(velocity.X);
            writer.Write(velocity.Y);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);
            velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            lifetime = 0f;
        }

    }

}
