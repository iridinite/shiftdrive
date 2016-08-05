/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Shows and animates informative messages in the corner of the screen.
    /// </summary>
    internal sealed class DeveloperConsole {

        private class ConsoleItem {
            public string message;
            public Color color;
            public float lifetime;
            public float opacity;
            public int height;
        }

        private readonly List<ConsoleItem> msgs = new List<ConsoleItem>();

        /// <summary>
        /// Adds an item to the console queue.
        /// </summary>
        /// <param name="text">The text to queue.</param>
        /// <param name="error">Whether to print the message in red or not.</param>
        public void AddMessage(string text, bool error = false) {
            ConsoleItem ci = new ConsoleItem();
            ci.lifetime = Math.Min(5f + (text.Length * 0.05f), 30f);
            ci.opacity = 1f;
            ci.message = text; //Globals.WrapText(Globals.fontConsole, text, 800);
            ci.height = -1;
            ci.color = error ? Color.FromNonPremultiplied(255, 96, 96, 255) : Color.White;
            msgs.Add(ci);
        }

        /// <summary>
        /// Removes all items from the console queue.
        /// </summary>
        public void Clear() {
            foreach (ConsoleItem msg in msgs) {
                msg.lifetime = 0f;
            }
        }

        /// <summary>
        /// Renders the console.
        /// </summary>
        /// <param name="spriteBatch">A SpriteBatch to draw with.</param>
        public void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            try {
                int y = 8;
                if (msgs.Count > 0) {
                    y -= (int)(msgs[0].height * (1f - msgs[0].opacity));
                }

                for (int i = 0; i < msgs.Count; i++) {
                    if (msgs[i].height == -1)
                        msgs[i].height = (int)Assets.fontDefault.MeasureString(msgs[i].message).Y;
                    spriteBatch.DrawString(Assets.fontDefault, msgs[i].message, new Vector2(9, y + 1), Color.Black * msgs[i].opacity);
                    spriteBatch.DrawString(Assets.fontDefault, msgs[i].message, new Vector2(8, y), msgs[i].color * msgs[i].opacity);
                    y += (msgs[i].height + 2); // (int)((msgs[i].height + 2) * msgs[i].opacity);
                }

                // make sure we don't get crazy overflows
                if (y > SDGame.Inst.GameHeight - 16) msgs.RemoveAt(0);
            } catch (Exception) {
                // swallow any errors
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Animates the messages in the console.
        /// </summary>
        /// <param name="deltaTime">The number of seconds passed since the previous Update.</param>
        public void Update(float deltaTime) {
            for (int i = msgs.Count - 1; i >= 0; i--) {
                if (msgs[i].lifetime >= 0f) {
                    msgs[i].lifetime -= deltaTime;
                } else if (i == 0) {
                    msgs[i].opacity -= deltaTime * 5;
                    if (msgs[i].opacity <= 0f) msgs.RemoveAt(i);
                }
            }
        }

    }
}
