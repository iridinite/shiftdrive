/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Fires when the user clicks the associated <see cref="Control"/>.
    /// </summary>
    /// <param name="sender">The <see cref="Control"/> that initiated the event.</param>
    internal delegate void OnClickHandler(Control sender);

    /// <summary>
    /// Represents an interactive button.
    /// </summary>
    internal abstract class Button : Control {
        public bool Enabled { get; set; }
        public bool CancelSound { get; set; }

        protected int EffY => Y + ((Height - EffHeight) / 2);
        protected int EffHeight => (int)(Height * expand);

        public event OnClickHandler OnClick;
        public event OnClickHandler OnClosed;

        protected int state;
        protected readonly int order;
        protected float expand;
        private float expandDelay;
        private bool closing, opening, closeEventFired;

        private Tooltip tooltip;

        protected Button(int order, int x, int y, int width, int height) {
            // x = -1 means that the button should be centered
            this.X = x == -1 ? (SDGame.Inst.GameWidth / 2 - width / 2) : x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
            this.order = order;
            Enabled = true;
            CancelSound = false;
            state = 0;
            tooltip = null;
            Open();
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            if (expand <= .1f) return;
            Texture2D txButton = Assets.GetTexture("ui/button");

            spriteBatch.Draw(txButton, new Rectangle(X, EffY, 8, 8), new Rectangle(state * 24, 0, 8, 8), Color.White); // top left
            spriteBatch.Draw(txButton, new Rectangle(X + 8, EffY, Width - 16, 8), new Rectangle(state * 24 + 8, 0, 8, 8), Color.White); // top middle
            spriteBatch.Draw(txButton, new Rectangle(X + Width - 8, EffY, 8, 8), new Rectangle(state * 24 + 16, 0, 8, 8), Color.White); // top right

            spriteBatch.Draw(txButton, new Rectangle(X, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24, 8, 8, 8), Color.White); // middle left
            spriteBatch.Draw(txButton, new Rectangle(X + 8, EffY + 8, Width - 16, EffHeight - 16), new Rectangle(state * 24 + 8, 8, 8, 8), Color.White); // center
            spriteBatch.Draw(txButton, new Rectangle(X + Width - 8, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24 + 16, 8, 8, 8), Color.White); // middle right

            spriteBatch.Draw(txButton, new Rectangle(X, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24, 16, 8, 8), Color.White); // bottom left
            spriteBatch.Draw(txButton, new Rectangle(X + 8, EffY + EffHeight - 8, Width - 16, 8), new Rectangle(state * 24 + 8, 16, 8, 8), Color.White); // bottom middle
            spriteBatch.Draw(txButton, new Rectangle(X + Width - 8, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24 + 16, 16, 8, 8), Color.White); // bottom right

            tooltip?.Draw();
        }

        protected override void OnUpdate(GameTime gameTime) {
            // button appear/disappear animations
            if (expandDelay > 0f) {
                expandDelay -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            } else {
                if (closing) {
                    expand = MathHelper.Max(0f, expand - (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                    if (expand <= 0f && !closeEventFired) {
                        closeEventFired = true;
                        OnClosed?.Invoke(this);
                    }
                } else {
                    expand = MathHelper.Min(1f, expand + (float)gameTime.ElapsedGameTime.TotalSeconds * 8);
                    if (expand >= 1f && opening) {
                        opening = false;
                        Assets.GetSound("UIAppear").Play();
                    }
                }
            }

            // disabled state
            if (!Enabled) {
                state = 3;
                return;
            }

            // do not respond to user input while animating
            if (!Visible || closing || expand < 1f)
                return;

            // update tooltip position and visibility
            tooltip?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            switch (state) {
                case 0: // normal
                    if (Input.GetMouseInArea(X, Y, Width, Height)) state = 1;
                    break;
                case 1: // hover
                    if (!Input.GetMouseInArea(X, Y, Width, Height)) {
                        state = 0;
                        break;
                    }
                    if (Input.GetMouseLeftDown()) state = 2;
                    break;
                case 2: // down
                    if (Input.GetMouseLeftUp()) {
                        state = 0;
                        if (Input.GetMouseInArea(X, Y, Width, Height)) {
                            if (CancelSound)
                                Assets.GetSound("UICancel").Play();
                            else
                                Assets.GetSound("UIConfirm").Play();
                            OnClick?.Invoke(this);
                        }
                    }
                    break;
                case 3: // disabled
                    state = 0;
                    break;
            }
        }

        public void Open() {
            closeEventFired = false;

            // -1 is override to disable animation
            if (order == -1) {
                closing = false;
                opening = false;
                expand = 1f;
                return;
            }

            closing = false;
            opening = true;
            expand = 0.1f;
            expandDelay = order * 0.075f;
        }

        public void Close() {
            closing = true;
            opening = false;
            closeEventFired = false;
            expand = 1f;
            expandDelay = order * 0.05f;
        }

        public void SetTooltip(string text) {
            tooltip = new Tooltip(new Rectangle(X, Y, Width, Height), text);
        }
    }

}
