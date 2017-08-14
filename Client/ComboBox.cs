/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a dropdown menu from which the user can pick one option.
    /// </summary>
    internal sealed class ComboBox : Control {

        /// <summary>
        /// Represents the dropdown panel managed by a ComboBox.
        /// </summary>
        private sealed class Dropdown : Control {

            private const int ITEM_SPACING = 28;

            private readonly ComboBox master;
            private int hoverIndex = -1;

            public Dropdown(ComboBox master) {
                this.master = master;
                this.X = master.X;
                this.Y = master.Y + master.Height;
                this.Width = master.Width;
                this.Height = master.Items.Count * ITEM_SPACING;
            }

            protected override void OnDraw(SpriteBatch spriteBatch) {
                spriteBatch.DrawBorder(Assets.GetTexture("ui/dropdown"), new Rectangle(X, Y, Width, Height), Color.Gray, 16);

                if (hoverIndex >= 0)
                    spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(X, Y + hoverIndex * ITEM_SPACING, Width, ITEM_SPACING), Color.White * 0.35f);

                for (int i = 0; i < master.Items.Count; i++) {
                    spriteBatch.DrawString(Assets.fontDefault, master.Items[i], new Vector2(X + 4, Y + i * ITEM_SPACING + 6), Color.White);
                }
            }

            protected override void OnUpdate(GameTime gameTime) {
                if (!Visible || !IsActiveLayer) return;

                if (Input.GetMouseInArea(X, Y, Width, Height)) {
                    // update hover index
                    int relativeY = Input.MouseY - Y - 1;
                    hoverIndex = relativeY / ITEM_SPACING;

                    // pick a menu option
                    if (Input.GetMouseLeftDown()) {
                        Debug.Assert(hoverIndex >= 0 && hoverIndex < master.Items.Count);

                        master.SelectedIndex = hoverIndex;
                        SDGame.Inst.PopUI();
                    }
                } else {
                    hoverIndex = -1;
                    // clicking anywhere, inside or outside dropdown, closes it
                    if (Input.GetMouseLeftDown())
                        SDGame.Inst.PopUI();
                }
            }

        }

        public List<string> Items { get; }
        public int SelectedIndex { get; set; }

        public ComboBox(int x, int y, int width) {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = 24;
            Items = new List<string>();
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            // background
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X, Y, 8, 24), new Rectangle(0, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X + 8, Y, Width - 16, 24), new Rectangle(8, 0, 8, 24), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/textentry"), new Rectangle(X + Width - 8, Y, 8, 24), new Rectangle(16, 0, 8, 24), Color.White);
            // arrow icon
            //spriteBatch.Draw(Assets.GetTexture("ui/dropdown"), new Rectangle(X + Width - 24, Y + 4, 16, 16), new Rectangle(0, 48, 16, 16), Color.Black);
            spriteBatch.Draw(Assets.GetTexture("ui/dropdown"), new Rectangle(X + Width - 24, Y + 4, 16, 16), new Rectangle(0, 48, 16, 16), Color.White);
            // selected item text
            spriteBatch.DrawString(Assets.fontDefault, Items[SelectedIndex], new Vector2(X + 6, Y + 6), Color.Black);
            spriteBatch.DrawString(Assets.fontDefault, Items[SelectedIndex], new Vector2(X + 4, Y + 4), Color.White);
        }

        protected override void OnUpdate(GameTime gameTime) {
            if (!Visible || !IsActiveLayer) return;

            if (Input.GetMouseLeftDown() && Input.GetMouseInArea(X, Y, Width, Height)) {
                SDGame.Inst.PushUI(new Dropdown(this));
            }
        }

    }

}
