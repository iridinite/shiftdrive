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
        /// Specifies a style with which to render a sprite.
        /// </summary>
        private enum SpriteBlend {
            AlphaBlend,
            Additive,
            HalfBlend
        }

        /// <summary>
        /// Represents a single frame in the animation.
        /// </summary>
        private class SpriteFrame {
            public Texture2D texture;
            public SpriteBlend blend;
            public float wait;
        }

        /// <summary>
        /// Represents a sprite frame that is queued for rendering.
        /// </summary>
        /// <remarks>
        /// This type is a struct so QueueDraw below can abuse the value
        /// copying behaviour of structs.
        /// </remarks>
        private struct QueuedFrame {
            public Texture2D texture;
            public Color color;
            public Vector2 position;
            public float rotation;
            public float scale;
        }

        /// <summary>
        /// Represents a texture layer in a layered sprite.
        /// </summary>
        private class SpriteLayer {
            public readonly List<SpriteFrame> frames = new List<SpriteFrame>();
            public float scale;
            public float rotate;
            public float rotateSpeed;

            public int frameNo;
            public float frameTime;
        }

        private List<SpriteLayer> layers = new List<SpriteLayer>();
        private bool isPrototype;
        private bool offsetRandom;

        private static readonly List<QueuedFrame> drawQueueAlpha = new List<QueuedFrame>();
        private static readonly List<QueuedFrame> drawQueueAdditive = new List<QueuedFrame>();

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
            ret.layers = this.layers;
            if (offsetRandom) // randomize offsets
                foreach (SpriteLayer layer in ret.layers)
                    layer.frameTime = (float)Utils.RNG.NextDouble() * layer.frames[0].wait;
            return ret;
        }

        /// <summary>
        /// Draws this sprite using the specified <see cref="SpriteBatch"/>.
        /// </summary>
        public void QueueDraw(Vector2 position, Color color, float rotation) {
            if (isPrototype) return;

            foreach (SpriteLayer layer in layers) {
                SpriteFrame currentFrame = layer.frames[layer.frameNo];
                QueuedFrame queuedFrame = new QueuedFrame();
                queuedFrame.texture = currentFrame.texture;
                queuedFrame.position = position;
                queuedFrame.rotation = layer.rotate + rotation;
                queuedFrame.scale = layer.scale;
                queuedFrame.color = color;

                switch (currentFrame.blend) {
                    case SpriteBlend.AlphaBlend:
                        drawQueueAlpha.Add(queuedFrame);
                        break;
                    case SpriteBlend.Additive:
                        drawQueueAdditive.Add(queuedFrame);
                        break;
                    case SpriteBlend.HalfBlend:
                        queuedFrame.color.R = (byte)(color.R * (color.A / 255f));
                        queuedFrame.color.G = (byte)(color.G * (color.A / 255f));
                        queuedFrame.color.B = (byte)(color.B * (color.A / 255f));
                        queuedFrame.color.A /= 2;
                        drawQueueAlpha.Add(queuedFrame);
                        break;
                }
            }
        }

        /// <summary>
        /// Renders all sprite sheets that have been queued using alpha blending.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> to render with.</param>
        public static void RenderAlpha(SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            foreach (QueuedFrame frame in drawQueueAlpha)
                DrawInternal(spriteBatch, frame.texture, frame.position, frame.color, frame.rotation, frame.scale);
            drawQueueAlpha.Clear();
            spriteBatch.End();
        }

        /// <summary>
        /// Renders all sprite sheets that have been queued using additive blending.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> to render with.</param>
        public static void RenderAdditive(SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp);
            foreach (QueuedFrame frame in drawQueueAdditive)
                DrawInternal(spriteBatch, frame.texture, frame.position, frame.color, frame.rotation, frame.scale);
            drawQueueAdditive.Clear();
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a single texture using a <see cref="SpriteBatch"/>.
        /// </summary>
        private static void DrawInternal(SpriteBatch spriteBatch, Texture2D tex, Vector2 position, Color color, float rotation, float scale) {
            spriteBatch.Draw(
                tex,
                position * (SDGame.Inst.GameWidth / 1280f),
                null,
                color,
                rotation,
                new Vector2(tex.Width * .5f, tex.Height * .5f),
                (SDGame.Inst.GameWidth / 1920f) * scale,
                SpriteEffects.None,
                0f);
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
