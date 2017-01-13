/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;

namespace ShiftDrive {

    internal sealed class Mine : GameObject {

        public Mine(GameState world) : base(world) {
            type = ObjectType.Mine;
            facing = Utils.RandomFloat(0f, 360f);
            spritename = "map/mine";
            bounding = 4f;
            zorder = 72;
            layer = CollisionLayer.Default;
            layermask = CollisionLayer.All;
        }
        
        public override void Update(float deltaTime) {
            base.Update(deltaTime);

            // must run this on server, and can't explode twice
            if (!world.IsServer || IsDestroyScheduled()) return;

            // find nearby objects
            var nearbyObjects = world.QueryGrid(this);
            foreach (GameObject gobj in nearbyObjects) {
                // don't trigger because of other nearby mines
                if (gobj.type == ObjectType.Mine) continue;
                
                // too close? blow up!
                float dist = Vector2.DistanceSquared(gobj.position, this.position);
                if (dist >= 75f * 75f) continue;
                this.Destroy();
            }
        }

        public override void TakeDamage(float damage) {
            // any damage causes the mine to immediately explode
            this.Destroy();
        }

        public override void Destroy() {
            if (IsDestroyScheduled()) return;
            base.Destroy();

            if (!world.IsServer) return;
            NetServer.DoDamagingExplosion(this.position, 90f, 150f);
        }

    }

}
