/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a text field that can receive user input.
    /// </summary>
    internal sealed class TextField : Control {

        public string Text { get; set; }

        private bool focus;

        private double blinktime;
        private bool blinkmode;

        public TextField(int x, int y, int width) {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = 24;
            Text = "";
            blinktime = 1.0;
            blinkmode = true;
            focus = false;

            SDGame.Inst.Window.TextInput += Window_TextInput;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X, Y, 8, 24), new Rectangle(0, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X + 8, Y, Width - 16, 24), new Rectangle(8, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X + Width - 8, Y, 8, 24), new Rectangle(16, 0, 8, 24), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, blinkmode && focus ? Text + "|" : Text, new Vector2(X + 6, Y + 6), Color.Black);
        }

        protected override void OnUpdate(GameTime gameTime) {
            // do not accept input if invisible
            if (!Visible || !IsActiveLayer) {
                focus = false;
                return;
            }

            // animate the blinking cursor
            blinktime -= gameTime.ElapsedGameTime.TotalSeconds;
            if (blinktime <= 0.0) {
                blinkmode = !blinkmode;
                blinktime = 1.0;
            }

            // test for input focus
            if (Input.GetMouseLeftDown()) {
                if (Input.GetMouseInArea(X, Y, Width, Height)) {
                    focus = true;
                    blinkmode = true;
                    blinktime = 1.0;
                } else {
                    focus = false;
                }
            }
        }

        protected override void OnDestroy() {
            SDGame.Inst.Window.TextInput -= Window_TextInput;
            base.OnDestroy();
        }

        private void Window_TextInput(object sender, TextInputEventArgs e) {
            if (!focus) return;
            int ascii = Convert.ToInt32(e.Character);
            if (ascii == 8) { // backspace
                if (Text.Length > 0) Text = Text.Substring(0, Text.Length - 1);
            } else if (ascii < 32) { // control characters
                return;
            } else {
                // new character
                Text += e.Character;
            }
        }

    }

}
