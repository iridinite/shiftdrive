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
            bounding = 0f;
            colorstart = Color.White;
            colorend = Color.White;
            zorder = 16;
            layer = CollisionLayer.None;
            layermask = CollisionLayer.None;
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
            if (!changed.HasFlag(ObjectProperty.ParticleData))
                return;
            writer.Write(life);
            writer.Write(lifemax);
            writer.Write(colorstart.PackedValue);
            writer.Write(colorend.PackedValue);
            writer.Write(scalestart);
            writer.Write(scaleend);
            writer.Write(rotatespeed);
            writer.Write(rotateoffset);
        }

        public override void Deserialize(BinaryReader reader, ObjectProperty recvChanged) {
            base.Deserialize(reader, recvChanged);
            if (!recvChanged.HasFlag(ObjectProperty.ParticleData))
                return;
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

            // shockwave
            Particle wave = new Particle(NetServer.world);
            wave.spritename = "map/shockwave";
            wave.lifemax = 2f;
            wave.colorstart = Color.White * 2f;
            wave.colorend = Color.Transparent;
            wave.position = position;
            wave.scalestart = 0.2f;
            wave.scaleend = 5f;
            world.AddObject(wave);

            // large flare in the center
            Particle flare = new Particle(NetServer.world);
            flare.spritename = "map/flare";
            flare.lifemax = 1.5f;
            flare.colorstart = Color.White * 2f;
            flare.colorend = Color.Transparent;
            flare.position = position;
            flare.scalestart = 1.0f;
            flare.scaleend = 3.0f;
            world.AddObject(flare);

            // several fast moving small particles
            for (int i = 0; i < 32; i++) {
                Particle p = new Particle(NetServer.world);
                p.spritename = "map/explosion";
                p.lifemax = 5f;
                p.colorend = Color.Transparent;
                p.rotateoffset = Utils.RandomFloat(0, MathHelper.TwoPi);
                p.rotatespeed = Utils.RandomFloat(-2f, 2f);
                p.scalestart = Utils.RandomFloat(0.25f, 0.4f) + (i * 0.02f);
                p.scaleend = p.scalestart + Utils.RandomFloat(0.5f, 1.0f);
                p.position = position;
                p.velocity = new Vector2(Utils.RandomFloat(-16f, 16f), Utils.RandomFloat(-16f, 16f));
                world.AddObject(p);
            }
        }

    }

}
