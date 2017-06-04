/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form showing a menu for connecting to a game server.
    /// </summary>
    internal class FormConnect : Control {

        /// <summary>
        /// Determines which UI segments to display.
        /// </summary>
        private enum FormState {
            Default,
            Connecting,
            ConnectFailed
        }

        private readonly TextField txtIP;

        private readonly TextButton
            btnConnect,
            btnConnectFailConfirm,
            btnBackToMenu;

        private FormState state;
        private string connectErrorMsg;

        public FormConnect() {
            Children.Add(new Skybox());

            int centerY = SDGame.Inst.GameHeight / 2;
            state = FormState.Default;

            // create UI controls
            txtIP = new TextField(SDGame.Inst.GameWidth / 2 - 125, centerY + 50, 250);
            txtIP.text = Config.ServerIP;
            Children.Add(txtIP);
            btnConnect = new TextButton(0, -1, centerY + 110, 250, 40, Locale.Get("connect"));
            btnConnect.OnClick += btnConnect_Click;
            Children.Add(btnConnect);
            btnConnectFailConfirm = new TextButton(1, -1, centerY + 160, 250, 40, Locale.Get("ok"));
            btnConnectFailConfirm.Visible = false;
            btnConnectFailConfirm.OnClick += btnConnectFailConfirm_Click;
            Children.Add(btnConnectFailConfirm);
            btnBackToMenu = new TextButton(1, -1, centerY + 160, 250, 40, Locale.Get("cancel"));
            btnBackToMenu.CancelSound = true;
            btnBackToMenu.OnClosed += sender => {
                if (state == FormState.Default)
                    SDGame.Inst.SetUIRoot(new FormMainMenu());
            };
            btnBackToMenu.OnClick += btnBackToMenu_Click;
            Children.Add(btnBackToMenu);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            Utils.DrawTitle(spriteBatch);

            switch (state) {
                case FormState.Default:
                    spriteBatch.DrawString(Assets.fontBold, Locale.Get("menu_connect"), new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontBold.MeasureString(Locale.Get("menu_connect")).X / 2), SDGame.Inst.GameHeight / 2f - 100), Color.White);

                    spriteBatch.DrawString(Assets.fontDefault, Locale.Get("serverip"), new Vector2(SDGame.Inst.GameWidth / 2f - 125, SDGame.Inst.GameHeight / 2f + 30), Color.White);
                    break;

                case FormState.Connecting:
                    spriteBatch.DrawString(Assets.fontBold, Locale.Get("connecting"), new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontBold.MeasureString(Locale.Get("connecting")).X / 2), SDGame.Inst.GameHeight / 2f), Color.White);
                    break;

                case FormState.ConnectFailed:
                    // if we had a connection error, draw the error text
                    spriteBatch.DrawString(Assets.fontBold, Locale.Get("err_connfailed"), new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontBold.MeasureString(Locale.Get("err_connfailed")).X / 2), SDGame.Inst.GameHeight / 2f - 50), Color.White);
                    spriteBatch.DrawString(Assets.fontDefault, connectErrorMsg, new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontDefault.MeasureString(connectErrorMsg).X / 2), SDGame.Inst.GameHeight / 2f), Color.White);
                    break;
            }
        }

        protected override void OnUpdate(GameTime gameTime) {
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, -100f);
        }

        private void btnConnect_Click(Control sender) {
            state = FormState.Connecting;

            // TODO: remove this later. temp server creation for easy local testing
            if (NetServer.Active) NetServer.Stop();
            if (!NetServer.PrepareWorld())
                return;
            NetServer.Start();
            // connect to the remote server
            NetClient.Connect(txtIP.text, ConnectResult);
            // hide UI
            txtIP.Visible = false;
            btnConnect.Close();
            btnBackToMenu.Close();
        }

        private void btnBackToMenu_Click(Control sender) {
            btnConnect.Close();
            btnBackToMenu.Close();
        }

        private void btnConnectFailConfirm_Click(Control sender) {
            btnConnect.Open();
            btnBackToMenu.Open();
            btnConnectFailConfirm.Close();
            btnConnectFailConfirm.Visible = false;
            btnBackToMenu.Visible = true;
            btnConnect.Visible = true;
            txtIP.Visible = true;
            state = FormState.Default;
        }

        private void ConnectResult(bool success, string errmsg) {
            if (success) {
                SDGame.Inst.SetUIRoot(new FormLobby());
            } else {
                connectErrorMsg = Utils.WrapText(
                    Assets.fontDefault,
                    errmsg,
                    MathHelper.Min(SDGame.Inst.GameWidth - 400, 1000));

                btnConnectFailConfirm.Visible = true;
                btnBackToMenu.Visible = false;
                btnConnect.Visible = false;
                btnConnectFailConfirm.Open();
                state = FormState.ConnectFailed;
            }
        }

    }

}
