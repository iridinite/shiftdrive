/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a predefined particle effect.
    /// </summary>
    internal enum ParticleEffect {
        Explosion,
        BulletImpact,
        Beam
    }

    /// <summary>
    /// A singleton that keeps track of particles and allows placing them in the sprite draw queue.
    /// </summary>
    internal static class ParticleManager {

        private static readonly List<Particle> particles = new List<Particle>();
        private static readonly object particleLock = new object();

        /// <summary>
        /// Adds a <seealso cref="Particle"/> to the manager.
        /// </summary>
        public static void Register(Particle p) {
            lock (particleLock) {
                particles.Add(p);
            }
        }

        /// <summary>
        /// Adds particles in the manager to the sprite draw queue.
        /// </summary>
        /// <param name="min">The upper-left corner of the world view area.</param>
        /// <param name="max">The bottom-right corner of the world view area.</param>
        public static void QueueDraw(Vector2 min, Vector2 max) {
            lock (particleLock) {
                foreach (Particle p in particles) {
                    Vector2 screenpos = Utils.CalculateScreenPos(min, max, p.position);
                    SpriteQueue.QueueSprite(
                        p.sprite,
                        screenpos,
                        Color.Lerp(p.colorstart, p.colorend, p.life / p.lifemax),
                        MathHelper.ToRadians(p.facing),
                        p.zorder);
                }
            }
        }

        /// <summary>
        /// Updates and decays particles.
        /// </summary>
        /// <param name="deltaTime">Delta-time value.</param>
        public static void Update(float deltaTime) {
            lock (particleLock) {
                for (int i = particles.Count - 1; i >= 0; i--) {
                    Particle p = particles[i];
                    p.sprite.Update(deltaTime);
                    p.sprite.GetLayer(0).scale = MathHelper.Lerp(p.scalestart, p.scaleend, p.life / p.lifemax);
                    //p.sprite.GetLayer(0).rotate = p.facing + p.rotateoffset
                    // increment lifetime
                    p.life += deltaTime;
                    if (p.life >= p.lifemax)
                        particles.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Removes all registered particles.
        /// </summary>
        public static void Clear() {
            lock (particleLock) {
                particles.Clear();
            }
        }

        /// <summary>
        /// Returns the number of registered particles.
        /// </summary>
        public static int GetCount() {
            return particles.Count;
        }

        /// <summary>
        /// Implementation for Explosion effect.
        /// </summary>
        public static void CreateExplosion(Vector2 position) {
            // shockwave
            Particle wave = new Particle();
            wave.sprite = Assets.GetSprite("map/shockwave").Clone();
            wave.lifemax = 2f;
            wave.colorstart = Color.White * 2f;
            wave.colorend = Color.Transparent;
            wave.position = position;
            wave.scalestart = 0.2f;
            wave.scaleend = 5f;
            Register(wave);

            // large flare in the center
            Particle flare = new Particle();
            flare.sprite = Assets.GetSprite("map/flare").Clone();
            flare.lifemax = 1.5f;
            flare.colorstart = Color.White * 2f;
            flare.colorend = Color.Transparent;
            flare.position = position;
            flare.scalestart = 1.0f;
            flare.scaleend = 3.0f;
            Register(flare);

            // several fast moving small particles
            for (int i = 0; i < 16; i++) {
                Particle p = new Particle();
                p.sprite = Assets.GetSprite("map/explosion").Clone();
                p.lifemax = 5f;
                p.colorend = Color.Transparent;
                p.rotateoffset = Utils.RandomFloat(0, MathHelper.TwoPi);
                p.rotatespeed = Utils.RandomFloat(-2f, 2f);
                p.scalestart = Utils.RandomFloat(0.25f, 0.4f) + (i * 0.02f);
                p.scaleend = p.scalestart + Utils.RandomFloat(0.5f, 1.0f);
                p.position = position;
                p.velocity = new Vector2(Utils.RandomFloat(-16f, 16f), Utils.RandomFloat(-16f, 16f));
                Register(p);
            }
        }

        /// <summary>
        /// Implementation for BulletImpact effect.
        /// </summary>
        public static void CreateBulletImpact(Vector2 position, float facing) {
            Particle impact = new Particle();
            impact.sprite = Assets.GetSprite("map/bullet-impact").Clone();
            impact.position = position;
            impact.facing = facing;
            impact.lifemax = 0.4f;
            Register(impact);
        }

        /// <summary>
        /// Implementation for Beam effect.
        /// </summary>
        public static void CreateBeam(Vector2 position, Vector2 target, string beamsprite, string impactsprite) {
            Vector2 beamLength = target - position;
            Vector2 deltapos = Vector2.Normalize(beamLength) * 8.5f;
            float beamLengthToGo = beamLength.Length() * 0.5f;
            float facing = Utils.CalculateBearing(position, target);

            while (beamLengthToGo > 0f) {
                Particle beam = new Particle();
                beam.position = position;
                beam.facing = facing;
                beam.lifemax = 0.15f;
                //beam.colorend = Color.Transparent;
                beam.sprite = Assets.GetSprite(beamsprite).Clone();
                Register(beam);

                beamLengthToGo -= 4f; // 32 pixels
                position += deltapos;
            }

            Particle impact = new Particle();
            impact.sprite = Assets.GetSprite(impactsprite).Clone();
            impact.position = target;
            impact.lifemax = 0.25f;
            impact.facing = facing;
            impact.colorend = Color.Transparent;
            Register(impact);
        }

    }

}
