/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an animated sprite.
    /// </summary>
    internal sealed class SpriteSheet {

        /// <summary>
        /// Represents a single frame in the animation.
        /// </summary>
        private class SpriteFrame {
            public Texture2D texture;
            public float wait;
        }

        private List<SpriteFrame> frames;
        private BlendState blend;
        private bool isPrototype;

        private int frameNo;
        private float frameTime;

        private bool offsetRandom;
        private float offset;

        /// <summary>
        /// Reads a sprite sheet prototype from a file.
        /// </summary>
        /// <param name="filename">The path to the file.</param>
        public static SpriteSheet FromFile(string filename) {
            SpriteSheet ret = new SpriteSheet();
            SpriteFrame frame = null;
            float? currentFrameWait = null;
            ret.frames = new List<SpriteFrame>();
            ret.isPrototype = true;

            using (StreamReader reader = new StreamReader(filename)) {
                while (!reader.EndOfStream) {
                    // read a line from the text file
                    string line = reader.ReadLine();
                    if (String.IsNullOrWhiteSpace(line)) continue; // blank line

                    // parse the command in the line
                    line = line.Trim();
                    if (line.StartsWith("#")) continue; // comment
                    string[] parts = line.Split(new[] {' '}, 2);
                    if (parts.Length < 1)
                        throw new InvalidDataException($"In sprite sheet '{filename}': Could not parse line '{line}'");

                    // follow the command
                    switch (parts[0].Trim()) {
                        case "blend":
                            if (parts[1].Equals("alpha", StringComparison.InvariantCulture))
                                ret.blend = BlendState.AlphaBlend;
                            else if (parts[1].Equals("additive", StringComparison.InvariantCulture))
                                ret.blend = BlendState.Additive;
                            else
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unrecognized blend state '{parts[1]}'");
                            break;

                        case "offset":
                            if (parts[1].Equals("random", StringComparison.InvariantCulture))
                                ret.offsetRandom = true;
                            else if (!float.TryParse(parts[1], NumberStyles.Float,
                                CultureInfo.InvariantCulture.NumberFormat, out ret.offset))
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unrecognized offset setting '{parts[1]}'");
                            break;

                        case "frame":
                            if (frame != null) {
                                // flush frame to sheet
                                if (currentFrameWait == null)
                                    throw new InvalidDataException($"In sprite sheet '{filename}':Encountered frame before wait specification: '{parts[1]}'");
                                frame.wait = currentFrameWait.Value;
                                ret.frames.Add(frame);
                            }
                            frame = new SpriteFrame();
                            frame.texture =
                                Assets.GetTexture(parts[1].Trim('"')
                                    .Replace("\\\"", "\"")
                                    .Replace("\n", Environment.NewLine));
                            break;

                        case "wait":
                            float val;
                            if (!float.TryParse(parts[1], NumberStyles.Float,
                                CultureInfo.InvariantCulture.NumberFormat, out val))
                                throw new InvalidDataException($"In sprite sheet '{filename}': Failed to parse wait time '{parts[1]}'");
                            currentFrameWait = val;
                            break;

                        default:
                            throw new InvalidDataException($"In sprite sheet '{filename}': Could not parse line '{line}'");
                    }

                }

                // flush last frame to sheet
                if (frame != null) {
                    if (currentFrameWait == null)
                        throw new InvalidDataException($"In sprite sheet '{filename}': Unspecified frame wait time at end of file");
                    frame.wait = currentFrameWait.Value;
                    ret.frames.Add(frame);
                }

                return ret;
            }
        }

        /// <summary>
        /// Instantiates a copy of this <see cref="SpriteSheet"/> that can be tied to a specific <seealso cref="GameObject"/>.
        /// </summary>
        /// <returns></returns>
        public SpriteSheet Clone() {
            SpriteSheet ret = new SpriteSheet();
            ret.isPrototype = false;
            ret.frames = this.frames;
            ret.blend = this.blend;
            ret.frameNo = 0;
            ret.frameTime = offsetRandom ? (float)Utils.RNG.NextDouble() * frames[0].wait : offset;
            ret.offsetRandom = this.offsetRandom;
            ret.offset = this.offset;
            return ret;
        }

        /// <summary>
        /// Draws this sprite using the specified <see cref="SpriteBatch"/>.
        /// </summary>
        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, float rotation) {
            if (isPrototype) return;

            Texture2D tex = frames[frameNo].texture; // shorthand
            spriteBatch.Draw(
                tex,
                position * (SDGame.Inst.GameWidth / 1280f), 
                null,
                color,
                rotation,
                new Vector2(tex.Width * .5f, tex.Height * .5f),
                SDGame.Inst.GameWidth / 1920f,
                SpriteEffects.None,
                0f);
        }

        /// <summary>
        /// Updates this sprite sheet, advancing the animation as necessary.
        /// </summary>
        /// <param name="gameTime">The time that passed since the last call to Update.</param>
        public void Update(GameTime gameTime) {
            if (isPrototype) return;

            // add delta time to this frame's lifetime
            frameTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (!(frameTime > frames[frameNo].wait)) return;

            // if the wait time is over, advance the frame
            frameNo++;
            frameTime = 0f;
            if (frameNo >= frames.Count) frameNo = 0;
        }

    }

}
