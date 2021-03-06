﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Queues a number of sprites for rendering.
    /// </summary>
    internal static class SpriteQueue {

        /// <summary>
        /// Represents a sprite frame that is queued for rendering.
        /// </summary>
        private struct QueuedFrame {
            public Texture2D texture;
            public Color color;
            public Vector2 position;
            public float rotation;
            public float scale;
            public byte zorder;
        }

        private static readonly List<QueuedFrame> drawQueueAlpha = new List<QueuedFrame>();
        private static readonly List<QueuedFrame> drawQueueAdditive = new List<QueuedFrame>();

        /// <summary>
        /// Draws this sprite using the specified <see cref="SpriteBatch"/>.
        /// </summary>
        public static void QueueSprite(SpriteSheet spr, Vector2 position, float rotation, byte zorder) {
            Debug.Assert(!spr.isPrototype, "Cannot queue sprite prototype, use Clone() to duplicate it");

            int zorderOffset = 0;
            foreach (SpriteSheet.SpriteLayer layer in spr.layers) {
                SpriteSheet.SpriteFrame currentFrame = layer.frames[layer.frameNo];
                QueuedFrame queuedFrame = new QueuedFrame();
                queuedFrame.texture = currentFrame.texture;
                queuedFrame.position = position;
                queuedFrame.rotation = layer.rotate + rotation;
                queuedFrame.scale = layer.scale;
                queuedFrame.color = layer.color;
                queuedFrame.zorder = unchecked((byte)(zorder - zorderOffset));
                zorderOffset++;

                switch (layer.blend) {
                    case SpriteSheet.SpriteBlend.AlphaBlend:
                        drawQueueAlpha.Add(queuedFrame);
                        break;
                    case SpriteSheet.SpriteBlend.Additive:
                        drawQueueAdditive.Add(queuedFrame);
                        break;
                    case SpriteSheet.SpriteBlend.HalfBlend:
                        queuedFrame.color.R = (byte)(layer.color.R * (layer.color.A / 255f));
                        queuedFrame.color.G = (byte)(layer.color.G * (layer.color.A / 255f));
                        queuedFrame.color.B = (byte)(layer.color.B * (layer.color.A / 255f));
                        queuedFrame.color.A /= 2;
                        drawQueueAlpha.Add(queuedFrame);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(layer.blend));
                }
            }
        }

        /// <summary>
        /// Renders all sprite sheets that have been queued using alpha blending.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> to render with.</param>
        public static void RenderAlpha(SpriteBatch spriteBatch, Vector2 viewport) {
            spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend, SamplerState.PointClamp);
            foreach (QueuedFrame frame in drawQueueAlpha)
                DrawInternal(spriteBatch, frame.texture, viewport, frame.position, frame.color, frame.rotation, frame.scale, (float)frame.zorder / byte.MaxValue);
            drawQueueAlpha.Clear();
            spriteBatch.End();
        }

        /// <summary>
        /// Renders all sprite sheets that have been queued using additive blending.
        /// </summary>
        /// <param name="spriteBatch">The <see cref="SpriteBatch"/> to render with.</param>
        public static void RenderAdditive(SpriteBatch spriteBatch, Vector2 viewport) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp);
            foreach (QueuedFrame frame in drawQueueAdditive)
                DrawInternal(spriteBatch, frame.texture, viewport, frame.position, frame.color, frame.rotation, frame.scale, (float)frame.zorder / byte.MaxValue);
            drawQueueAdditive.Clear();
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a single texture using a <see cref="SpriteBatch"/>.
        /// </summary>
        private static void DrawInternal(SpriteBatch spriteBatch, Texture2D tex, Vector2 viewport, Vector2 position, Color color, float rotation, float scale, float zorder) {
            // at 1080p resolution, draw sprites at 100% scale. if resolution goes lower, downscale the
            // sprites appropriately, so the final image retains the same sense of scale
            spriteBatch.Draw(
                tex,
                position,
                null,
                color,
                rotation,
                new Vector2(tex.Width * .5f, tex.Height * .5f),
                viewport.X / 1920f * scale,
                SpriteEffects.None,
                zorder);
        }

    }

}
