/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormMainMenu : IForm {

        private readonly TextButton btnConnect, btnHost, btnOptions, btnQuit;
        private int leaveAction;

        public FormMainMenu() {
            // create UI controls
            Skybox.SetIdleRotation(true);
            btnConnect = new TextButton(0, -1, SDGame.Inst.GameHeight / 2 + 100, 260, 40, Utils.LocaleGet("menu_connect"));
            btnConnect.OnClick += btnConnect_Click;
            btnHost = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 + 150, 260, 40, Utils.LocaleGet("menu_host"));
            btnHost.OnClick += btnHost_OnClick;
            btnHost.Enabled = false;
            btnOptions = new TextButton(2, SDGame.Inst.GameWidth / 2 - 130, SDGame.Inst.GameHeight / 2 + 200, 125, 40, Utils.LocaleGet("menu_options"));
            btnOptions.OnClick += btnOptions_OnClick;
            btnQuit = new TextButton(3, SDGame.Inst.GameWidth / 2 + 5, SDGame.Inst.GameHeight / 2 + 200, 125, 40, Utils.LocaleGet("menu_exit"));
            btnQuit.OnClick += btnClose_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Utils.DrawTitle(spriteBatch);

            btnConnect.Draw(spriteBatch);
            btnHost.Draw(spriteBatch);
            btnOptions.Draw(spriteBatch);
            btnQuit.Draw(spriteBatch);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, 0f);

            btnConnect.Update(gameTime);
            btnHost.Update(gameTime);
            btnOptions.Update(gameTime);
            btnQuit.Update(gameTime);

            // leave this form if the user clicked a button
            if (btnQuit.IsClosed) { // last button to close
                switch (leaveAction) {
                    case 0:
                        SDGame.Inst.ActiveForm = new FormConnect();
                        break;
                    case 1:
                        SDGame.Inst.ActiveForm = new FormMainMenu();
                        break;
                    case 2:
                        SDGame.Inst.ActiveForm = new FormOptions();
                        break;
                    case 3:
                        SDGame.Inst.ActiveForm = new FormConfirmExit();
                        break;
                }
            }
        }

        private void CloseButtons() {
            btnConnect.Close();
            btnHost.Close();
            btnOptions.Close();
            btnQuit.Close();
        }

        private void btnConnect_Click(Control sender) {
            leaveAction = 0;
            CloseButtons();
        }

        private void btnHost_OnClick(Control sender) {
            leaveAction = 1;
            CloseButtons();
        }

        private void btnOptions_OnClick(Control sender) {
            leaveAction = 2;
            CloseButtons();
        }

        private void btnClose_Click(Control sender) {
            leaveAction = 3;
            CloseButtons();
        }

    }

}