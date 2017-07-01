/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    internal sealed class Mine : GameObject {

        public Mine(GameState world) : base(world) {
            Type = ObjectType.Mine;
            Facing = Utils.RandomFloat(0f, 360f);
            SpriteName = "map/mine";
            Bounding = 4f;
            ZOrder = 72;
            Layer = CollisionLayer.Default;
            LayerMask = CollisionLayer.All;
        }
        
        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // must run this on server, and can't explode twice
            if (!World.IsServer || IsDestroyScheduled()) return;

            // find nearby objects
            var nearbyObjects = World.BVH.Query(new BVHBox(
                Position - new Vector2(75.0f, 75.0f),
                Position + new Vector2(75.0f, 75.0f)));
            foreach (GameObject gobj in nearbyObjects) {
                // don't trigger because of other nearby mines
                if (gobj.Type == ObjectType.Mine) continue;
                
                // too close? blow up!
                float dist = Vector2.DistanceSquared(gobj.Position, this.Position);
                if (dist >= 75f * 75f) continue;
                this.Destroy();
            }
        }

        public override void TakeDamage(float damage, bool sound = false) {
            // any damage causes the mine to immediately explode
            this.Destroy();
        }

        public override void Destroy() {
            if (IsDestroyScheduled()) return;
            base.Destroy();

            if (!World.IsServer) return;
            Assets.GetSound("ExplosionMedium").Play();
            NetServer.DoDamagingExplosion(this.Position, 90f, 150f);
        }

    }

}
