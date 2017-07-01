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

            spriteBatch.DrawBorder(Assets.GetTexture("ui/button"), new Rectangle(X, EffY, Width, EffHeight), Color.White, 8, state);
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
