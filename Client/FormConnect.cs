/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormConnect : IForm {

        private readonly TextField txtIP;
        private readonly TextButton btnConnect, btnCancel;
        private int leaveAction;

        private string connectErrorMsg;

        public FormConnect() {
            // create UI controls
            txtIP = new TextField(SDGame.Inst.GameWidth / 2 - 125, SDGame.Inst.GameHeight / 2 + 50, 250);
            txtIP.text = "localhost";
            btnConnect = new TextButton(0, -1, SDGame.Inst.GameHeight / 2 + 110, 250, 40, Utils.LocaleGet("connect"));
            btnConnect.OnClick += btnConnect_Click;
            btnCancel = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 + 160, 250, 40, Utils.LocaleGet("cancel"));
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Utils.DrawTitle(spriteBatch);

            if (btnCancel.IsClosed) {
                spriteBatch.DrawString(Assets.fontBold, Utils.LocaleGet("connecting"), new Vector2(SDGame.Inst.GameWidth / 2f - Assets.fontBold.MeasureString(Utils.LocaleGet("connecting")).X / 2, SDGame.Inst.GameHeight / 2f), Color.White);

            } else {
                spriteBatch.DrawString(Assets.fontBold, Utils.LocaleGet("menu_connect"), new Vector2(SDGame.Inst.GameWidth / 2f - Assets.fontBold.MeasureString(Utils.LocaleGet("menu_connect")).X / 2, SDGame.Inst.GameHeight / 2f - 100), Color.White);
                if (connectErrorMsg != null) {
                    // if we had a connection error, draw the error text
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("err_connfailed"), new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontDefault.MeasureString(Utils.LocaleGet("err_connfailed")).X / 2), SDGame.Inst.GameHeight / 2f - 50), Color.White);
                    spriteBatch.DrawString(Assets.fontDefault, connectErrorMsg, new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontDefault.MeasureString(connectErrorMsg).X / 2), SDGame.Inst.GameHeight / 2f - 20), Color.White);
                }
                spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("serverip"), new Vector2(SDGame.Inst.GameWidth / 2f - 125, SDGame.Inst.GameHeight / 2f + 30), Color.White);
                txtIP.Draw(spriteBatch);
                btnConnect.Draw(spriteBatch);
                btnCancel.Draw(spriteBatch);
            }

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, -100f);

            txtIP.Update(gameTime);
            btnConnect.Update(gameTime);
            btnCancel.Update(gameTime);

            if (btnCancel.IsClosed) {
                if (leaveAction == 0) {
                    SDGame.Inst.ActiveForm = new FormMainMenu();
                }
            }
        }

        private void btnConnect_Click(Control sender) {
            // TODO: remove this later. temp server creation for easy local testing
            ShiftDrive.NetServer.PrepareWorld();
            ShiftDrive.NetServer.Start();
            // connect to the remote server
            NetClient.Connect(txtIP.text, ConnectResult);
            // hide UI
            leaveAction = 1;
            btnConnect.Close();
            btnCancel.Close();
        }

        private void btnCancel_Click(Control sender) {
            leaveAction = 0;
            btnConnect.Close();
            btnCancel.Close();
        }

        private void ConnectResult(bool success, string errmsg) {
            if (success) {
                SDGame.Inst.ActiveForm = new FormLobby();

            } else {
                btnConnect.Open();
                btnCancel.Open();
                connectErrorMsg = errmsg;
            }
        }

    }

}
