using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;

namespace ShiftDrive {

    /// <summary>
    /// Represents a collection of <seealso cref="SoundEffect"/>s with an optional set of playback parameters.
    /// </summary>
    internal sealed class SoundCue : IDisposable {
        private readonly List<SoundEffect> sounds;

        /// <summary>
        /// Gets or sets the volume, ranging from 1.0 (full volume) to 0.0 (silence).
        /// </summary>
        public float Volume { get; set; } = 1f;

        /// <summary>
        /// Gets or sets the pitch adjustment, ranging from 1.0 (up an octave) to 0.0 (no change) to -1.0 (down an octave).
        /// </summary>
        public float Pitch { get; set; } = 0f;

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
        /// Plays this <see cref="SoundCue"/> with the currently specified settings.
        /// </summary>
        public void Play() {
            Debug.Assert(sounds.Count > 0, "SoundCue must have at least one SoundEffect");

            // select the sound to play randomly; if only one sound, avoid the expensive RNG call
            SoundEffect selected = null;
            selected = sounds.Count > 1
                ? sounds[Utils.RNG.Next(sounds.Count)]
                : sounds[0];

            selected.Play(Volume, Pitch, 0f);
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
