/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a projectile in flight.
    /// </summary>
    internal sealed class Projectile : GameObject {
        
        public float lifetime;
        public byte faction;

        public Projectile(GameState world) : base(world) {
            type = ObjectType.Projectile;
            color = Color.White;
            bounding = 1f;
            layer = CollisionLayer.Projectile;
            layermask = CollisionLayer.Ship | CollisionLayer.Asteroid;
        }

        public Projectile(GameState world, string spritename, Vector2 position, float facing, float speed, byte faction) : this(world) {
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

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // move forward
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
        
    }

}
