/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormLobby : IForm {

        private readonly TextButton btnHelm, btnWeap, btnEngi, btnQuar, btnIntel, btnReady, btnDisconnect;
        private int leaveAction;
        private bool isReady;

        public FormLobby() {
            int cx = SDGame.Inst.GameWidth / 2;
            int cy = SDGame.Inst.GameHeight / 2;
            btnHelm = new TextButton(1, cx - 80, cy - 150, 200, 40, Utils.LocaleGet("console_helm"));
            btnHelm.OnClick += BtnHelm_OnClick;
            btnWeap = new TextButton(2, cx - 80, cy - 100, 200, 40, Utils.LocaleGet("console_wep"));
            btnWeap.OnClick += BtnWeap_OnClick;
            btnEngi = new TextButton(3, cx - 80, cy - 50, 200, 40, Utils.LocaleGet("console_eng"));
            btnEngi.OnClick += BtnEngi_OnClick;
            btnQuar = new TextButton(4, cx - 80, cy, 200, 40, Utils.LocaleGet("console_quar"));
            btnQuar.OnClick += BtnQuar_OnClick;
            btnIntel = new TextButton(5, cx - 80, cy + 50, 200, 40, Utils.LocaleGet("console_intel"));
            btnIntel.OnClick += BtnIntel_OnClick;

            btnDisconnect = new TextButton(0, 20, SDGame.Inst.GameHeight - 60, 200, 40, Utils.LocaleGet("disconnect"));
            btnDisconnect.CancelSound = true;
            btnDisconnect.OnClick += BtnDisconnect_OnClick;
            btnReady = new TextButton(6, SDGame.Inst.GameWidth - 220, SDGame.Inst.GameHeight - 60, 200, 40, Utils.LocaleGet("ready"));
            btnReady.OnClick += BtnReady_OnClick;

            isReady = false;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            spriteBatch.DrawString(Assets.fontDefault, "Connected.", new Vector2(50, 50), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, "Players: " + NetClient.PlayerCount, new Vector2(50, 150), Color.White);
            spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle(SDGame.Inst.GameWidth - 270, SDGame.Inst.GameHeight - 60, 40, 40), isReady ? Color.Green : Color.Red);

            btnHelm.Draw(spriteBatch);
            btnWeap.Draw(spriteBatch);
            btnEngi.Draw(spriteBatch);
            btnQuar.Draw(spriteBatch);
            btnIntel.Draw(spriteBatch);

            for (int i = 0; i < 5; i++) {
                PlayerRole thisrole = (PlayerRole) (1 << i);
                if (NetClient.TakenRoles.HasFlag(thisrole))
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("role_taken"), new Vector2(SDGame.Inst.GameWidth / 2 + 130, SDGame.Inst.GameHeight / 2 - 138 + (i * 50)), Color.White);
                spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle(SDGame.Inst.GameWidth / 2 - 130, SDGame.Inst.GameHeight / 2 - 150 + (i * 50), 40, 40), NetClient.MyRoles.HasFlag(thisrole) ? Color.Green : Color.Red);
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
            btnQuar.Update(gameTime);
            btnIntel.Update(gameTime);
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
            btnQuar.Close();
            btnIntel.Close();
            btnReady.Close();
            btnDisconnect.Close();
        }

        private void BtnDisconnect_OnClick(Control sender) {
            leaveAction = 0;
            CloseAll();
            NetClient.Disconnect();
        }

        private void BtnReady_OnClick(Control sender) {
            // toggle ready state and propagate to server
            isReady = !isReady;

            using (Packet p = new Packet(PacketID.Ready)) {
                p.Write(isReady);
                NetClient.Send(p);
            }
        }

        private void ToggleRole(PlayerRole role) {
            using (Packet packet = new Packet(PacketID.SelectRole)) {
                packet.Write((byte)role);
                NetClient.Send(packet);
            }
        }

        private void BtnHelm_OnClick(Control sender) {
            ToggleRole(PlayerRole.Helm);
        }

        private void BtnWeap_OnClick(Control sender) {
            ToggleRole(PlayerRole.Weapons);
        }

        private void BtnEngi_OnClick(Control sender) {
            ToggleRole(PlayerRole.Engineering);
        }

        private void BtnQuar_OnClick(Control sender) {
            ToggleRole(PlayerRole.Quartermaster);
        }

        private void BtnIntel_OnClick(Control sender) {
            ToggleRole(PlayerRole.Intelligence);
        }

    }

}