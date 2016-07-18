/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormLobby : IForm {

        private readonly Button btnHelm, btnWeap, btnEngi, btnSci, btnComm, btnReady, btnDisconnect;
        private int leaveAction;
        private bool isReady;

        public FormLobby() {
            int cx = SDGame.Inst.GameWidth / 2;
            int cy = SDGame.Inst.GameHeight / 2;
            btnHelm = new Button(1, cx - 80, cy - 150, 200, 40, Utils.LocaleGet("console_helm"));
            btnHelm.OnClick += BtnHelm_OnClick;
            btnWeap = new Button(2, cx - 80, cy - 100, 200, 40, Utils.LocaleGet("console_wep"));
            btnWeap.OnClick += BtnWeap_OnClick;
            btnEngi = new Button(3, cx - 80, cy - 50, 200, 40, Utils.LocaleGet("console_eng"));
            btnEngi.OnClick += BtnEngi_OnClick;
            btnSci = new Button(4, cx - 80, cy, 200, 40, Utils.LocaleGet("console_sci"));
            btnSci.OnClick += BtnSci_OnClick;
            btnComm = new Button(5, cx - 80, cy + 50, 200, 40, Utils.LocaleGet("console_comm"));
            btnComm.OnClick += BtnComm_OnClick;

            btnDisconnect = new Button(0, 20, SDGame.Inst.GameHeight - 60, 200, 40, Utils.LocaleGet("disconnect"));
            btnDisconnect.CancelSound = true;
            btnDisconnect.OnClick += BtnDisconnect_OnClick;
            btnReady = new Button(6, SDGame.Inst.GameWidth - 220, SDGame.Inst.GameHeight - 60, 200, 40, Utils.LocaleGet("ready"));
            btnReady.OnClick += BtnReady_OnClick;

            isReady = false;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(Assets.fontDefault, "Connected.", new Vector2(50, 50), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, "Players: " + NetClient.PlayerCount, new Vector2(50, 150), Color.White);
            spriteBatch.Draw(Assets.txRect, new Rectangle(SDGame.Inst.GameWidth - 270, SDGame.Inst.GameHeight - 60, 40, 40), isReady ? Color.Green : Color.Red);

            btnHelm.Draw(spriteBatch);
            btnWeap.Draw(spriteBatch);
            btnEngi.Draw(spriteBatch);
            btnSci.Draw(spriteBatch);
            btnComm.Draw(spriteBatch);

            for (int i = 0; i < 5; i++) {
                PlayerRole thisrole = (PlayerRole) (1 << i);
                if (NetClient.TakenRoles.HasFlag(thisrole))
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("role_taken"), new Vector2(SDGame.Inst.GameWidth / 2 + 130, SDGame.Inst.GameHeight / 2 - 138 + (i * 50)), Color.White);
                spriteBatch.Draw(Assets.txRect, new Rectangle(SDGame.Inst.GameWidth / 2 - 130, SDGame.Inst.GameHeight / 2 - 150 + (i * 50), 40, 40), NetClient.MyRoles.HasFlag(thisrole) ? Color.Green : Color.Red);
            }

            btnDisconnect.Draw(spriteBatch);
            btnReady.Draw(spriteBatch);
            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);

            btnHelm.Update(gameTime);
            btnWeap.Update(gameTime);
            btnEngi.Update(gameTime);
            btnSci.Update(gameTime);
            btnComm.Update(gameTime);
            btnDisconnect.Update(gameTime);
            btnReady.Update(gameTime);

            if (btnReady.IsClosed) {
                switch (leaveAction) {
                    case 0:
                        SDGame.Inst.ActiveForm = new FormMainMenu();
                        break;
                    case 1:
                        SDGame.Inst.ActiveForm = new FormGame();
                        break;
                }
            }
        }

        private void CloseAll() {
            btnHelm.Close();
            btnWeap.Close();
            btnEngi.Close();
            btnSci.Close();
            btnComm.Close();
            btnReady.Close();
            btnDisconnect.Close();
        }

        private void BtnDisconnect_OnClick(Button sender) {
            leaveAction = 0;
            CloseAll();
            NetClient.Disconnect();
        }

        private void BtnReady_OnClick(Button sender) {
            // toggle ready state and propagate to server
            isReady = !isReady;
            NetClient.Send(new Packet(PacketType.Ready, isReady));
        }

        private void ToggleRole(PlayerRole role) {
            NetClient.Send(new Packet(PacketType.SelectRole, (byte)role));
        }

        private void BtnHelm_OnClick(Button sender) {
            ToggleRole(PlayerRole.Helm);
        }

        private void BtnWeap_OnClick(Button sender) {
            ToggleRole(PlayerRole.Weapons);
        }

        private void BtnEngi_OnClick(Button sender) {
            ToggleRole(PlayerRole.Engineering);
        }

        private void BtnSci_OnClick(Button sender) {
            ToggleRole(PlayerRole.Science);
        }

        private void BtnComm_OnClick(Button sender) {
            ToggleRole(PlayerRole.Comms);
        }

    }

}