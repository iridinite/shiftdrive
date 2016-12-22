/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a mouse-over tooltip.
    /// </summary>
    internal class Tooltip {

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

        public void Draw(SpriteBatch spriteBatch) {
            if (!visible) return;

            Texture2D tex = Assets.textures["ui/tooltip"];
            spriteBatch.Draw(tex, new Rectangle(posx, posy, 16, 16), new Rectangle(0, 0, 16, 16), Color.White); // top left
            spriteBatch.Draw(tex, new Rectangle(posx + 16, posy, boxwidth - 32, 16), new Rectangle(16, 0, 16, 16), Color.White); // top middle
            spriteBatch.Draw(tex, new Rectangle(posx + boxwidth - 16, posy, 16, 16), new Rectangle(32, 0, 16, 16), Color.White); // top right

            spriteBatch.Draw(tex, new Rectangle(posx, posy + 16, 16, boxheight - 32), new Rectangle(0, 16, 16, 16), Color.White); // middle left
            spriteBatch.Draw(tex, new Rectangle(posx + 16, posy + 16, boxwidth - 32, boxheight - 32), new Rectangle(16, 16, 16, 16), Color.White); // center
            spriteBatch.Draw(tex, new Rectangle(posx + boxwidth - 16, posy + 16, 16, boxheight - 32), new Rectangle(32, 16, 16, 16), Color.White); // middle right

            spriteBatch.Draw(tex, new Rectangle(posx, posy + boxheight - 16, 16, 16), new Rectangle(0, 32, 16, 16), Color.White); // bottom left
            spriteBatch.Draw(tex, new Rectangle(posx + 16, posy + boxheight - 16, boxwidth - 32, 16), new Rectangle(16, 32, 16, 16), Color.White); // bottom middle
            spriteBatch.Draw(tex, new Rectangle(posx + boxwidth - 16, posy + boxheight - 16, 16, 16), new Rectangle(32, 32, 16, 16), Color.White); // bottom right

            spriteBatch.DrawString(Assets.fontTooltip, text, new Vector2(posx + 10, posy + 10), Color.White);
        }

        public void Update(float deltaTime) {
            // is the user hovering over the trigger area?
            if (Mouse.IsInArea(trigger)) {
                // apply mouse coordinates
                posx = Mouse.X + 16;
                posy = Mouse.Y + 16;
                // if mouse overlaps tooltip itself, hide it
                if (Mouse.IsInArea(posx, posy, boxwidth, boxheight)) {
                    visible = false;
                    return;
                }
                // if already visible, no need to do anything else
                visible = true;

            } else {
                visible = false;
            }
        }

    }

}
