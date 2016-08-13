/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
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

        public Particle(GameState world) : base(world) {
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
        
        public override void Update(float deltaTime) {
            base.Update(deltaTime);

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

        public static void CreateExplosion(GameState world, Vector2 position) {
            if (!world.IsServer)
                throw new InvalidOperationException("Cannot create objects on client");

            // several fast moving small particles
            for (int i = 0; i < 20; i++) {
                Particle p = new Particle(NetServer.world);
                p.spritename = "map/explosion";
                p.lifemax = 5f;
                p.colorend = Color.Transparent;
                p.rotateoffset = Utils.RandomFloat(0, MathHelper.TwoPi);
                p.rotatespeed = Utils.RandomFloat(-1f, 1f);
                p.scalestart = Utils.RandomFloat(0.1f, 0.25f) + (i * 0.02f);
                p.scaleend = p.scalestart + Utils.RandomFloat(1.0f, 1.5f);
                p.position = position;
                p.velocity = new Vector2(Utils.RandomFloat(-30f, 30f), Utils.RandomFloat(-30, 30f));
                world.Objects.Add(p.id, p);
            }
            // a few large particles that stay near the center
            for (int i = 0; i < 5; i++) {
                Particle p = new Particle(NetServer.world);
                p.spritename = "map/explosion";
                p.lifemax = 4f;
                p.colorend = Color.Transparent;
                p.rotateoffset = Utils.RandomFloat(0, MathHelper.TwoPi);
                p.rotatespeed = Utils.RandomFloat(-2f, 2f);
                p.scalestart = Utils.RandomFloat(0.75f, 1.25f);
                p.scaleend = p.scalestart + Utils.RandomFloat(0.5f, 1.0f);
                p.position = position;
                p.velocity = new Vector2(Utils.RandomFloat(-10f, 10f), Utils.RandomFloat(-10, 10f));
                world.Objects.Add(p.id, p);
            }
        }

    }

}
