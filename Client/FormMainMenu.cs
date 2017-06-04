/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form showing the main menu.
    /// </summary>
    internal class FormMainMenu : Control {

        private readonly TextButton btnConnect, btnHost, btnOptions, btnQuit;
        private int leaveAction;

        public FormMainMenu() {
            Children.Add(new Skybox());

            // create UI controls
            btnConnect = new TextButton(0, -1, SDGame.Inst.GameHeight / 2 + 100, 260, 40, Locale.Get("menu_connect"));
            btnConnect.OnClick += btnConnect_Click;
            Children.Add(btnConnect);
            btnHost = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 + 150, 260, 40, Locale.Get("menu_host"));
            btnHost.OnClick += btnHost_OnClick;
            btnHost.Enabled = false;
            Children.Add(btnHost);
            btnOptions = new TextButton(2, SDGame.Inst.GameWidth / 2 - 130, SDGame.Inst.GameHeight / 2 + 200, 125, 40, Locale.Get("menu_options"));
            btnOptions.OnClick += btnOptions_OnClick;
            Children.Add(btnOptions);
            btnQuit = new TextButton(3, SDGame.Inst.GameWidth / 2 + 5, SDGame.Inst.GameHeight / 2 + 200, 125, 40, Locale.Get("menu_exit"));
            btnQuit.OnClick += btnClose_Click;
            btnQuit.OnClosed += btnClose_Closed;
            Children.Add(btnQuit);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            Utils.DrawTitle(spriteBatch);
        }

        protected override void OnUpdate(GameTime gameTime) {
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, 0f);
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

        private void btnClose_Closed(Control sender) {
            switch (leaveAction) {
                case 0:
                    SDGame.Inst.SetUIRoot(new FormConnect());
                    break;
                case 1:
                    SDGame.Inst.SetUIRoot(new FormMainMenu());
                    break;
                case 2:
                    SDGame.Inst.SetUIRoot(new FormOptions());
                    break;
                case 3:
                    SDGame.Inst.SetUIRoot(new FormConfirmExit());
                    break;
            }
        }

    }

}
