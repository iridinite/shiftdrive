using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a predefined particle effect.
    /// </summary>
    internal enum ParticleEffect {
        Explosion
    }

    /// <summary>
    /// A singleton that keeps track of particles and allows placing them in the sprite draw queue.
    /// </summary>
    internal static class ParticleManager {

        private static readonly List<Particle> particles = new List<Particle>();

        /// <summary>
        /// Adds a <seealso cref="Particle"/> to the manager.
        /// </summary>
        public static void Register(Particle p) {
            particles.Add(p);
        }

        /// <summary>
        /// Adds particles in the manager to the sprite draw queue.
        /// </summary>
        /// <param name="min">The upper-left corner of the world view area.</param>
        /// <param name="max">The bottom-right corner of the world view area.</param>
        public static void QueueDraw(Vector2 min, Vector2 max) {
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

        /// <summary>
        /// Updates and decays particles.
        /// </summary>
        /// <param name="deltaTime">Delta-time value.</param>
        public static void Update(float deltaTime) {
            for (int i = particles.Count - 1; i >= 0; i--) {
                Particle p = particles[i];
                p.sprite.GetLayer(0).scale = MathHelper.Lerp(p.scalestart, p.scaleend, p.life / p.lifemax);
                //p.sprite.GetLayer(0).rotate = p.facing + p.rotateoffset
                // increment lifetime
                p.life += deltaTime;
                if (p.life >= p.lifemax)
                    particles.RemoveAt(i);
            }
        }

        /// <summary>
        /// Removes all registered particles.
        /// </summary>
        public static void Clear() {
            particles.Clear();
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

    }

}
