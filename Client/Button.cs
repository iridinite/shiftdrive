/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
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

        protected int EffY { get { return y + ((height - EffHeight) / 2); } }
        protected int EffHeight { get { return (int)(height * expand); } }

        public bool IsClosed { get { return closing && expand <= 0f; } }

        public event OnClickHandler OnClick;

        protected int state;
        protected readonly int order;
        protected float expand, expandDelay;
        protected bool closing, opening;

        private Tooltip tooltip;

        protected Button(int order, int x, int y, int width, int height) {
            // x = -1 means that the button should be centered
            this.x = x == -1 ? (SDGame.Inst.GameWidth / 2 - width / 2) : x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.order = order;
            Enabled = true;
            CancelSound = false;
            state = 0;
            tooltip = null;
            Open();
        }

        public override void Draw(SpriteBatch spriteBatch) {
            if (expand <= .1f) return;
            Texture2D txButton = Assets.textures["ui/button"];

            spriteBatch.Draw(txButton, new Rectangle(x, EffY, 8, 8), new Rectangle(state * 24, 0, 8, 8), Color.White); // top left
            spriteBatch.Draw(txButton, new Rectangle(x + 8, EffY, width - 16, 8), new Rectangle(state * 24 + 8, 0, 8, 8), Color.White); // top middle
            spriteBatch.Draw(txButton, new Rectangle(x + width - 8, EffY, 8, 8), new Rectangle(state * 24 + 16, 0, 8, 8), Color.White); // top right

            spriteBatch.Draw(txButton, new Rectangle(x, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24, 8, 8, 8), Color.White); // middle left
            spriteBatch.Draw(txButton, new Rectangle(x + 8, EffY + 8, width - 16, EffHeight - 16), new Rectangle(state * 24 + 8, 8, 8, 8), Color.White); // center
            spriteBatch.Draw(txButton, new Rectangle(x + width - 8, EffY + 8, 8, EffHeight - 16), new Rectangle(state * 24 + 16, 8, 8, 8), Color.White); // middle right

            spriteBatch.Draw(txButton, new Rectangle(x, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24, 16, 8, 8), Color.White); // bottom left
            spriteBatch.Draw(txButton, new Rectangle(x + 8, EffY + EffHeight - 8, width - 16, 8), new Rectangle(state * 24 + 8, 16, 8, 8), Color.White); // bottom middle
            spriteBatch.Draw(txButton, new Rectangle(x + width - 8, EffY + EffHeight - 8, 8, 8), new Rectangle(state * 24 + 16, 16, 8, 8), Color.White); // bottom right

            tooltip?.Draw();
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
                            case 1: Assets.sndUIAppear1.Play(Config.GetVolumeSound(), 0f, 0f); break;
                            case 2: Assets.sndUIAppear2.Play(Config.GetVolumeSound(), 0f, 0f); break;
                            case 3: Assets.sndUIAppear3.Play(Config.GetVolumeSound(), 0f, 0f); break;
                            case 4: Assets.sndUIAppear4.Play(Config.GetVolumeSound(), 0f, 0f); break;
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

            // update tooltip position and visibility
            tooltip?.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

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
                                Assets.sndUICancel.Play(Config.GetVolumeSound(), 0f, 0f);
                            else
                                Assets.sndUIConfirm.Play(Config.GetVolumeSound(), 0f, 0f);
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

        public void SetTooltip(string text) {
            tooltip = new Tooltip(new Rectangle(x, y, width, height), text);
        }

    }

}
