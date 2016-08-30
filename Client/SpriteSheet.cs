﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an animated sprite.
    /// </summary>
    internal sealed class SpriteSheet {
        
        /// <summary>
        /// Specifies a style with which to render a sprite.
        /// </summary>
        internal enum SpriteBlend {
            AlphaBlend,
            Additive,
            HalfBlend
        }

        /// <summary>
        /// Represents a single frame in the animation.
        /// </summary>
        internal class SpriteFrame {
            public Texture2D texture;
            public SpriteBlend blend;
            public float wait;
        }

        /// <summary>
        /// Represents a texture layer in a layered sprite.
        /// </summary>
        internal class SpriteLayer {
            public List<SpriteFrame> frames = new List<SpriteFrame>();
            public float rotate;
            public float rotateSpeed;
            public float scale;

            public int frameNo;
            public float frameTime;
        }

        public List<SpriteLayer> layers = new List<SpriteLayer>();
        public bool isPrototype;
        private bool offsetRandom;

        /// <summary>
        /// Reads a sprite sheet prototype from a file.
        /// </summary>
        /// <param name="filename">The path to the file.</param>
        public static SpriteSheet FromFile(string filename) {
            SpriteSheet ret = new SpriteSheet();
            ret.isPrototype = true;

            // variables for recording parsing state
            SpriteLayer layer = null;
            SpriteFrame frame = null;
            SpriteBlend blend = SpriteBlend.AlphaBlend;
            float? currentFrameWait = null;
            bool staticMode = false;

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
                        case "blend": // Specify a blend mode for the sprite
                            if (parts[1].Equals("alpha", StringComparison.InvariantCulture))
                                blend = SpriteBlend.AlphaBlend;
                            else if (parts[1].Equals("additive", StringComparison.InvariantCulture))
                                blend = SpriteBlend.Additive;
                            else if (parts[1].Equals("half", StringComparison.InvariantCulture))
                                blend = SpriteBlend.HalfBlend;
                            else
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unrecognized blend state '{parts[1]}'");
                            break;

                        case "offset": // Specify a random offset into the first frame
                            if (parts[1].Equals("random", StringComparison.InvariantCulture))
                                ret.offsetRandom = true;
                            else
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unrecognized offset setting '{parts[1]}'");
                            break;

                        case "frame": // Add a new frame with a given texture
                            // create a default layer
                            if (layer == null) {
                                layer = new SpriteLayer();
                                layer.scale = 1f;
                                layer.rotateSpeed = 0f;
                            }
                            // flush any existing frame to the active layer
                            if (frame != null) {
                                if (currentFrameWait == null)
                                    throw new InvalidDataException($"In sprite sheet '{filename}': Encountered frame before wait specification: '{parts[1]}'");
                                if (staticMode)
                                    throw new InvalidDataException($"In sprite sheet '{filename}': Static sprite cannot have more than one frame");
                                frame.wait = currentFrameWait.Value;
                                frame.blend = blend;
                                layer.frames.Add(frame);
                            }
                            frame = new SpriteFrame();
                            frame.texture = Assets.GetTexture(parts[1].Trim('"'));
                            break;

                        case "layer":
                            // flush any existing frame to the active layer
                            if (frame != null) {
                                if (currentFrameWait == null)
                                    throw new InvalidDataException($"In sprite sheet '{filename}': Unspecified frame wait time at '{line}'");
                                frame.wait = currentFrameWait.Value;
                                frame.blend = blend;
                                layer.frames.Add(frame);
                            }
                            // save currently open layer to the sheet
                            frame = null;
                            if (layer != null) ret.layers.Add(layer);

                            layer = new SpriteLayer();
                            layer.scale = 1f;
                            layer.rotateSpeed = 0f;
                            break;

                        case "rotate":
                            if (!float.TryParse(parts[1], NumberStyles.Float,
                                CultureInfo.InvariantCulture.NumberFormat, out layer.rotateSpeed))
                                throw new InvalidDataException($"In sprite sheet '{filename}': Failed to parse rotate speed '{parts[1]}'");
                            break;

                        case "scale":
                            if (!float.TryParse(parts[1], NumberStyles.Float,
                                CultureInfo.InvariantCulture.NumberFormat, out layer.scale))
                                throw new InvalidDataException($"In sprite sheet '{filename}': Failed to parse scale '{parts[1]}'");
                            break;

                        case "wait": // Specify the time to wait until the next frame
                            float val;
                            if (staticMode)
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unexpected wait time declaration in static sprite");
                            if (!float.TryParse(parts[1], NumberStyles.Float,
                                CultureInfo.InvariantCulture.NumberFormat, out val))
                                throw new InvalidDataException($"In sprite sheet '{filename}': Failed to parse wait time '{parts[1]}'");
                            currentFrameWait = val;
                            break;

                        case "static": // Specify that this sprite has no animation
                            // Assign a dummy value to the frame wait time. Because there are no other
                            // frames, the value has no meaning, but it's cleaner to have the sprite
                            // sheet itself explicitly specify that no animation is intentional.
                            if (currentFrameWait != null)
                                throw new InvalidDataException($"In sprite sheet '{filename}': Unexpected static declaration after wait time declaration");
                            currentFrameWait = 1f;
                            staticMode = true;
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
                    frame.blend = blend;
                    layer.frames.Add(frame);
                }
                ret.layers.Add(layer);

                return ret;
            }
        }

        /// <summary>
        /// Instantiates a copy of this <see cref="SpriteSheet"/> that can be tied to a specific <seealso cref="GameObject"/>.
        /// </summary>
        /// <returns></returns>
        public SpriteSheet Clone() {
            if (!isPrototype) throw new InvalidOperationException("Cannot clone instantiated sprite sheet. Clone a prototype instead.");

            SpriteSheet ret = new SpriteSheet();
            ret.isPrototype = false;
            ret.layers = new List<SpriteLayer>();
            foreach (SpriteLayer protlayer in layers) {
                SpriteLayer clonelayer = new SpriteLayer();
                clonelayer.frames = protlayer.frames;
                clonelayer.frameTime = protlayer.frameTime;
                clonelayer.frameNo = 0;
                clonelayer.rotate = 0f;
                clonelayer.rotateSpeed = protlayer.rotateSpeed;
                clonelayer.scale = protlayer.scale;
                ret.layers.Add(clonelayer);
            }
            if (offsetRandom) // randomize offsets
                foreach (SpriteLayer layer in ret.layers)
                    layer.frameTime = (float)Utils.RNG.NextDouble() * layer.frames[0].wait;
            return ret;
        }

        /// <summary>
        /// Returns a specified <see cref="SpriteLayer"/> from this sheet.
        /// </summary>
        /// <param name="index">The layer index to look up.</param>
        public SpriteLayer GetLayer(int index) {
            return layers[index];
        }

        /// <summary>
        /// Updates this sprite sheet, advancing the animation as necessary.
        /// </summary>
        /// <param name="deltaTime">The time that passed since the last call to Update.</param>
        public void Update(float deltaTime) {
            if (isPrototype) return;

            foreach (SpriteLayer layer in layers) {
                // advance rotation speed
                layer.rotate += layer.rotateSpeed * deltaTime;

                // add delta time to this frame's lifetime
                layer.frameTime += deltaTime;
                if (!(layer.frameTime > layer.frames[layer.frameNo].wait)) continue;

                // if the wait time is over, advance the frame
                layer.frameNo++;
                layer.frameTime = 0f;
                if (layer.frameNo >= layer.frames.Count) layer.frameNo = 0;
            }
        }

    }

}
