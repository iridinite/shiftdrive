/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a mouse-over tooltip.
    /// </summary>
    internal class Tooltip {

        private static readonly List<Tooltip> renderQueue = new List<Tooltip>();

        private readonly Rectangle trigger;
        private readonly string text;

        private bool visible;
        private int posx;
        private int posy;
        private readonly int boxwidth;
        private readonly int boxheight;

        public Tooltip(Rectangle trigger, string text) {
            this.trigger = trigger;
            this.text = Utils.WrapText(Assets.fontTooltip, text, 350f);
            visible = false;

            Vector2 boxsize = Assets.fontTooltip.MeasureString(text) + new Vector2(20, 20);
            boxwidth = (int)boxsize.X;
            boxheight = (int)boxsize.Y;
        }

        public void Draw() {
            if (!visible) return;
            renderQueue.Add(this);
        }

        public void Update(float deltaTime) {
            // is the user hovering over the trigger area?
            if (Input.GetMouseInArea(trigger)) {
                // apply mouse coordinates
                posx = Input.MouseX + 16;
                posy = Input.MouseY + 16;
                // if mouse overlaps tooltip itself, hide it
                if (Input.GetMouseInArea(posx, posy, boxwidth, boxheight)) {
                    visible = false;
                    return;
                }
                // if already visible, no need to do anything else
                visible = true;

            } else {
                visible = false;
            }
        }

        public static void DrawQueued(SpriteBatch spriteBatch) {
            Texture2D tex = Assets.GetTexture("ui/tooltip");

            foreach (Tooltip tt in renderQueue) {
                spriteBatch.Draw(tex, new Rectangle(tt.posx, tt.posy, 16, 16), new Rectangle(0, 0, 16, 16), Color.White); // top left
                spriteBatch.Draw(tex, new Rectangle(tt.posx + 16, tt.posy, tt.boxwidth - 32, 16), new Rectangle(16, 0, 16, 16), Color.White); // top middle
                spriteBatch.Draw(tex, new Rectangle(tt.posx + tt.boxwidth - 16, tt.posy, 16, 16), new Rectangle(32, 0, 16, 16), Color.White); // top right

                spriteBatch.Draw(tex, new Rectangle(tt.posx, tt.posy + 16, 16, tt.boxheight - 32), new Rectangle(0, 16, 16, 16), Color.White); // middle left
                spriteBatch.Draw(tex, new Rectangle(tt.posx + 16, tt.posy + 16, tt.boxwidth - 32, tt.boxheight - 32), new Rectangle(16, 16, 16, 16), Color.White); // center
                spriteBatch.Draw(tex, new Rectangle(tt.posx + tt.boxwidth - 16, tt.posy + 16, 16, tt.boxheight - 32), new Rectangle(32, 16, 16, 16), Color.White); // middle right

                spriteBatch.Draw(tex, new Rectangle(tt.posx, tt.posy + tt.boxheight - 16, 16, 16), new Rectangle(0, 32, 16, 16), Color.White); // bottom left
                spriteBatch.Draw(tex, new Rectangle(tt.posx + 16, tt.posy + tt.boxheight - 16, tt.boxwidth - 32, 16), new Rectangle(16, 32, 16, 16), Color.White); // bottom middle
                spriteBatch.Draw(tex, new Rectangle(tt.posx + tt.boxwidth - 16, tt.posy + tt.boxheight - 16, 16, 16), new Rectangle(32, 32, 16, 16), Color.White); // bottom right

                spriteBatch.DrawString(Assets.fontTooltip, tt.text, new Vector2(tt.posx + 10, tt.posy + 10), Color.White);
            }
            renderQueue.Clear();
        }

    }

}
