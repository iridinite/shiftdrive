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
        public float damage;
        public byte faction;

        private bool damageApplied;

        public Projectile(GameState world) : base(world) {
            type = ObjectType.Projectile;
            color = Color.White;
            bounding = 1f;
            damageApplied = false;
            layer = CollisionLayer.Projectile;
            layermask = CollisionLayer.Ship | CollisionLayer.Asteroid;
        }

        public Projectile(GameState world, string spritename, Vector2 position, float facing, float speed, float damage,
            byte faction) : this(world) {
            this.spritename = spritename;
            this.position = position;
            this.facing = facing;
            this.velocity = speed * new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(facing - 90f)),
                (float)Math.Sin(MathHelper.ToRadians(facing - 90f)));
            this.faction = faction;
            this.damage = damage;
            this.lifetime = 0f;
        }

        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // move forward
            lifetime += deltaTime;

            // get rid of projectiles that have traveled very far already
            if (world.IsServer && lifetime >= 10f) Destroy();

            // as these move every frame, always re-send them
            changed = true;
        }

        protected override void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // cannot hit stuff twice
            if (damageApplied) return;
            // ignore hits on friendly ships
            if (other.IsShip()) {
                Ship othership = other as Ship;
                if (othership?.faction == this.faction) return;
            }
            // apply damage to whatever we hit
            damageApplied = true;
            other.TakeDamage(damage);
            this.Destroy();
        }
        
    }

}
