/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a fire-and-forget particle effect.
    /// </summary>
    internal sealed class Particle : GameObject {

        public float life;
        public float lifemax;

        public float scalestart;
        public float scaleend;
        public float rotatespeed;
        public float rotateoffset;

        public Color colorstart;
        public Color colorend;

        public Particle() {
            type = ObjectType.Particle;
            life = 0f;
            lifemax = 0f;
            scalestart = 1f;
            scaleend = 1f;
            rotatespeed = 0f;
            rotateoffset = 0f;
            colorstart = Color.White;
            colorend = Color.White;
        }
        
        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);

            // update sprite color. this should also be done on the server because sprite
            // color is re-transmitted with the GameObject. not updating it on the server
            // causes the sprite to flicker on the client for 1 frame after each game update.
            color = Color.Lerp(colorstart, colorend, life / lifemax);
            // modify SpriteSheet properties that are client-side only
            if (!world.IsServer) {
                sprite.GetLayer(0).scale = MathHelper.Lerp(scalestart, scaleend, life / lifemax);
                sprite.GetLayer(0).rotateSpeed = rotatespeed;
            }

            // increment lifetime
            life += deltaTime;
            if (life >= lifemax) this.Destroy();
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            writer.Write(life);
            writer.Write(lifemax);
            writer.Write(colorstart.PackedValue);
            writer.Write(colorend.PackedValue);
            writer.Write(scalestart);
            writer.Write(scaleend);
            writer.Write(rotatespeed);
            writer.Write(rotateoffset);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);
            life = reader.ReadSingle();
            lifemax = reader.ReadSingle();
            colorstart.PackedValue = reader.ReadUInt32();
            colorend.PackedValue = reader.ReadUInt32();
            scalestart = reader.ReadSingle();
            scaleend = reader.ReadSingle();
            rotatespeed = reader.ReadSingle();
            rotateoffset = reader.ReadSingle();
        }

    }

}
