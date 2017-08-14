/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace ShiftDrive {

    /// <summary>
    /// Represents a collection of <seealso cref="SoundEffect"/>s with an optional set of playback parameters.
    /// </summary>
    internal sealed class SoundCue : IDisposable {

        private readonly List<SoundEffect> sounds;
        private double lastPlayedAt;

        /// <summary>
        /// Gets or sets the volume, ranging from 1.0 (full volume) to 0.0 (silence).
        /// </summary>
        public float Volume { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the pitch adjustment, ranging from 1.0 (up an octave) to 0.0 (no change) to -1.0 (down an octave).
        /// </summary>
        public float Pitch { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the minimum number of seconds that must be between two instances of this sound.
        /// </summary>
        public float Cooldown { get; set; } = 0f;

        /// <summary>
        /// Gets or sets the volume falloff-over-distance multiplier. A value of 2 will cause volume to drop twice as fast. Default is 1.
        /// </summary>
        public float Falloff { get; set; } = 1f;

        /// <summary>
        /// Returns the number of <seealso cref="SoundEffect"/> instances loaded.
        /// </summary>
        /// <returns></returns>
        public int Count {
            get { return sounds.Count; }
        }

        /// <summary>
        /// Constructs a new empty <see cref="SoundCue"/>.
        /// </summary>
        public SoundCue() {
            sounds = new List<SoundEffect>();
            lastPlayedAt = 0f;
        }

        /// <summary>
        /// Loads a <seealso cref="SoundEffect"/> from file and adds it to this <see cref="SoundCue"/>.
        /// </summary>
        /// <param name="content">The <seealso cref="ContentManager"/> with which to load the <seealso cref="SoundEffect"/>.</param>
        /// <param name="filename">The path to the file.</param>
        public void LoadSound(ContentManager content, string filename) {
            sounds.Add(content.Load<SoundEffect>("Audio/" + filename));
        }

        /// <summary>
        /// Randomly selects a <seealso cref="SoundEffect"/> to play.
        /// </summary>
        private SoundEffect SelectSound() {
            Debug.Assert(sounds.Count > 0, "SoundCue must have at least one SoundEffect");

            if (Cooldown > 0f) {
                TimeSpan totalTime = SDGame.Inst.GetTime().TotalGameTime;
                double now = totalTime.TotalSeconds;
                if (now < lastPlayedAt + Cooldown) return null;

                lastPlayedAt = totalTime.TotalSeconds;
            }

            // select the sound to play randomly; if only one sound, avoid the expensive RNG call
            return sounds.Count > 1
                ? sounds[Utils.RNG.Next(sounds.Count)]
                : sounds[0];
        }

        /// <summary>
        /// Plays this <see cref="SoundCue"/> with the currently specified settings.
        /// </summary>
        public void Play() {
            SelectSound()?.Play(Volume, Pitch, 0f);
        }

        /// <summary>
        /// Plays this <see cref="SoundCue"/> in stereo, calculating panning based on object positions.
        /// </summary>
        public void Play3D(Vector2 listener, Vector2 emitter) {
            Vector2 delta = emitter - listener;
            float dist = delta.Length();
            float instPanning = MathHelper.Clamp((emitter.X - listener.X) / 128f, -1f, 1f);
            float instVolume = MathHelper.Clamp(1f - (dist / 300f * Falloff), 0.1f, 1f);

            SelectSound()?.Play(instVolume * Volume, Pitch, instPanning);
        }

        /// <summary>
        /// Releases the <seealso cref="SoundEffect"/>s owned by this object.
        /// </summary>
        public void Dispose() {
            foreach (var sound in sounds)
                sound.Dispose();
            sounds.Clear();
        }

        /// <summary>
        /// Returns a string giving an overview of the object.
        /// </summary>
        public override string ToString() {
            return $"{Count} sounds";
        }

    }

}
