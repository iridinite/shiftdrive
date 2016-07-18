/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal delegate void OnClickHandler(Button sender);

    /// <summary>
    /// Represents an interactive text button.
    /// </summary>
    internal sealed class Button : Control {
        public string Caption { get; set; }
        public bool Enabled { get; set; }
        public bool CancelSound { get; set; }

        private int EffY { get { return y + ((height - EffHeight) / 2); } }
        private int EffHeight { get { return (int)(height * expand); } }

        public bool IsClosed { get { return closing && expand <= 0f; } }

        public event OnClickHandler OnClick;

        private int state;
        private readonly int order;
        private float expand, expandDelay;
        private bool closing, opening;

        public Button(int order, int x, int y, int width, int height, string caption) {
            // x = -1 means that the button should be centered
            this.x = x == -1 ? (SDGame.Inst.GameWidth / 2 - width / 2) : x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.Caption = caption;
            this.order = order;
            Enabled = true;
            CancelSound = false;
            state = 0;
            Open();
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (expand <= .1f) return;

            spriteBatch.Draw(Assets.txButton, new Rectangle(x, EffY, 8, 8), new Rectangle(state * 24, 0, 8, 8), Color.White); // top left
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + 8, EffY, width - 16, 8), new Rectangle(state * 24 + 8, 0, 8, 8), Color.White); // top middle
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + width - 8, EffY, 8, 8), new Rectangle(state * 24 + 16, 0, 8, 8), Color.White); // top right

            spriteBatch.Draw(Assets.txButton, new Rectangle(x, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24, 8, 8, 8), Color.White); // middle left
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + 8, EffY + 8, width - 16, EffHeight - 16), new Rectangle(state * 24 + 8, 8, 8, 8), Color.White); // center
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + width - 8, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24 + 16, 8, 8, 8), Color.White); // middle right

            spriteBatch.Draw(Assets.txButton, new Rectangle(x, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24, 16, 8, 8), Color.White); // bottom left
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + 8, EffY + EffHeight - 8, width - 16, 8), new Rectangle(state * 24 + 8, 16, 8, 8), Color.White); // bottom middle
            spriteBatch.Draw(Assets.txButton, new Rectangle(x + width - 8, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24 + 16, 16, 8, 8), Color.White); // bottom right

            if (expand >= 1f) {
                int textOffset = (state == 2) ? 7 : 4;
                Vector2 txtSize = Assets.fontDefault.MeasureString(Caption);
                spriteBatch.DrawString(Assets.fontDefault, Caption, new Vector2((int)(x + (width / 2) - (txtSize.X / 2)), (int)(y + (height / 2) - (txtSize.Y / 2) + textOffset)), Color.Black);
            }
        }

        public override void Update(GameTime gameTime) {
            // button appear/disappear animations
            if (expandDelay > 0f) {
                expandDelay -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            } else {
                if (closing) {
                    expand = MathHelper.Max(0f, expand - (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                } else {
                    expand = MathHelper.Min(1f, expand + (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                    if (expand >= 1f && opening) {
                        opening = false;
                        int appearsound = Utils.RNG.Next(1, 5);
                        switch (appearsound) {
                            case 1: Assets.sndUIAppear1.Play(); break;
                            case 2: Assets.sndUIAppear2.Play(); break;
                            case 3: Assets.sndUIAppear3.Play(); break;
                            case 4: Assets.sndUIAppear4.Play(); break;
                        }
                    }
                }
            }

            // disabled state
            if (!Enabled) {
                state = 3;
                return;
            }

            // do not respond to user input while animating
            if (closing || expand < 1f)
                return;

            switch (state) {
                case 0: // normal
                    if (Mouse.IsInArea(x, y, width, height)) state = 1;
                    break;
                case 1: // hover
                    if (!Mouse.IsInArea(x, y, width, height)) {
                        state = 0;
                        break;
                    }
                    if (Mouse.GetLeftDown()) state = 2;
                    break;
                case 2: // down
                    if (Mouse.GetLeftUp()) {
                        state = 0;
                        if (Mouse.IsInArea(x, y, width, height) && OnClick != null) {
                            if (CancelSound)
                                Assets.sndUICancel.Play();
                            else
                                Assets.sndUIConfirm.Play();
                            OnClick(this);
                        }
                    }
                    break;
                case 3: // disabled
                    state = 0;
                    break;
            }
        }

        public void Open() {
            closing = false;
            opening = true;
            expand = 0.1f;
            expandDelay = order * 0.075f;
        }

        public void Close() {
            closing = true;
            opening = false;
            expand = 1f;
            expandDelay = order * 0.05f;
        }
        
    }

}