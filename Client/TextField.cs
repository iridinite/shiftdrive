/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a text field that can receive user input.
    /// </summary>
    internal sealed class TextField : Control {
        public string text;

        private bool focus;

        private double blinktime;
        private bool blinkmode;

        public TextField(int x, int y, int width) {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = 24;
            text = "";
            blinktime = 1.0;
            blinkmode = true;
            focus = false;

            SDGame.Inst.Window.TextInput += Window_TextInput;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.textures["ui/textentry"], new Rectangle(x, y, 8, 24), new Rectangle(0, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.textures["ui/textentry"], new Rectangle(x + 8, y, width - 16, 24), new Rectangle(8, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.textures["ui/textentry"], new Rectangle(x + width - 8, y, 8, 24), new Rectangle(16, 0, 8, 24), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, blinkmode && focus ? text + "|" : text, new Vector2(x + 6, y + 6), Color.Black);
        }

        public override void Update(GameTime gameTime) {
            // animate the blinking cursor
            blinktime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (blinktime <= 0.0) {
                blinkmode = !blinkmode;
                blinktime = 1.0;
            }

            // test for input focus
            if (Mouse.GetLeftDown()) {
                if (Mouse.IsInArea(x, y, width, height)) {
                    focus = true;
                    blinkmode = true;
                    blinktime = 1.0;
                } else {
                    focus = false;
                }
            }
        }

        public override void OnDestroy() {
            SDGame.Inst.Window.TextInput -= Window_TextInput;
            base.OnDestroy();
        }
        
        private void Window_TextInput(object sender, TextInputEventArgs e) {
            if (!focus) return;
            int ascii = Convert.ToInt32(e.Character);
            if (ascii == 8) { // backspace
                if (text.Length > 0) text = text.Substring(0, text.Length - 1);
            } else if (ascii < 32) { // control characters
                return;
            } else {
                // new character
                text += e.Character;
            }
        }

    }

}