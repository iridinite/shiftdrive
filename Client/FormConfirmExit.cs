/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements an <seealso cref="IForm"/> requesting confirmation for application termination.
    /// </summary>
    internal sealed class FormConfirmExit : IForm {

        private readonly TextButton btnQuit, btnCancel;
        private int leaveAction;

        public FormConfirmExit() {
            // create UI controls
            btnQuit = new TextButton(0, SDGame.Inst.GameWidth / 2 - 185, SDGame.Inst.GameHeight / 2 + 100, 180, 40, Utils.LocaleGet("confirmexit_yes"));
            btnQuit.OnClick += btnConnect_Click;
            btnCancel = new TextButton(1, SDGame.Inst.GameWidth / 2 + 5, SDGame.Inst.GameHeight / 2 + 100, 180, 40, Utils.LocaleGet("confirmexit_no"));
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Utils.DrawTitle(spriteBatch);

            spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("confirmexit"), new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontDefault.MeasureString(Utils.LocaleGet("confirmexit")).X / 2f), SDGame.Inst.GameHeight / 2f), Color.White);
            btnQuit.Draw(spriteBatch);
            btnCancel.Draw(spriteBatch);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, 0f);

            btnQuit.Update(gameTime);
            btnCancel.Update(gameTime);

            if (btnCancel.IsClosed) {
                if (leaveAction == 0) {
                    SDGame.Inst.Exit();
                } else {
                    SDGame.Inst.ActiveForm = new FormMainMenu();
                }
            }
        }

        private void btnConnect_Click(Control sender) {
            leaveAction = 0;
            btnQuit.Close();
            btnCancel.Close();
        }

        private void btnCancel_Click(Control sender) {
            leaveAction = 1;
            btnQuit.Close();
            btnCancel.Close();
        }

    }

}
