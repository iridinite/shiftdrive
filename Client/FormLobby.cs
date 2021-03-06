﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form showing a game lobby with role selection.
    /// </summary>
    internal class FormLobby : Control {

        private readonly TextButton btnHelm, btnWeap, btnEngi, btnQuar, btnIntel, btnReady, btnDisconnect;
        private int leaveAction;
        private bool isReady;

        public FormLobby() {
            AddChild(new Skybox());

            int cx = SDGame.Inst.GameWidth / 2;
            int cy = SDGame.Inst.GameHeight / 2;
            btnHelm = new TextButton(1, cx - 80, cy - 150, 200, 40, Locale.Get("console_helm"));
            btnHelm.OnClick += BtnHelm_OnClick;
            AddChild(btnHelm);
            btnWeap = new TextButton(2, cx - 80, cy - 100, 200, 40, Locale.Get("console_wep"));
            btnWeap.OnClick += BtnWeap_OnClick;
            AddChild(btnWeap);
            btnEngi = new TextButton(3, cx - 80, cy - 50, 200, 40, Locale.Get("console_eng"));
            btnEngi.OnClick += BtnEngi_OnClick;
            AddChild(btnEngi);
            btnQuar = new TextButton(4, cx - 80, cy, 200, 40, Locale.Get("console_quar"));
            btnQuar.OnClick += BtnQuar_OnClick;
            AddChild(btnQuar);
            btnIntel = new TextButton(5, cx - 80, cy + 50, 200, 40, Locale.Get("console_intel"));
            btnIntel.OnClick += BtnIntel_OnClick;
            AddChild(btnIntel);

            btnDisconnect = new TextButton(0, 20, SDGame.Inst.GameHeight - 60, 200, 40, Locale.Get("disconnect"));
            btnDisconnect.CancelSound = true;
            btnDisconnect.OnClick += BtnDisconnect_OnClick;
            AddChild(btnDisconnect);
            btnReady = new TextButton(6, SDGame.Inst.GameWidth - 220, SDGame.Inst.GameHeight - 60, 200, 40, Locale.Get("ready"));
            btnReady.OnClick += BtnReady_OnClick;
            btnReady.OnClosed += BtnReady_OnClosed;
            AddChild(btnReady);

            isReady = false;

            // make sure no particles are left over from other game sessions
            ParticleManager.Clear();
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.DrawString(Assets.fontDefault, "Connected.", new Vector2(50, 50), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, "Players: " + NetClient.PlayerCount, new Vector2(50, 150), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(SDGame.Inst.GameWidth - 270, SDGame.Inst.GameHeight - 60, 40, 40), isReady ? Color.Green : Color.Red);

            for (int i = 0; i < 5; i++) {
                PlayerRole thisrole = (PlayerRole)(1 << i);
                if (NetClient.TakenRoles.HasFlag(thisrole))
                    spriteBatch.DrawString(Assets.fontDefault, Locale.Get("role_taken"), new Vector2(SDGame.Inst.GameWidth / 2 + 130, SDGame.Inst.GameHeight / 2 - 138 + (i * 50)), Color.White);
                spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(SDGame.Inst.GameWidth / 2 - 130, SDGame.Inst.GameHeight / 2 - 150 + (i * 50), 40, 40), NetClient.MyRoles.HasFlag(thisrole) ? Color.Green : Color.Red);
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

        private void BtnReady_OnClosed(Control sender) {
            switch (leaveAction) {
                case 0:
                    SDGame.Inst.SetUIRoot(new FormMainMenu());
                    break;
                case 1:
                    SDGame.Inst.SetUIRoot(new FormGame());
                    break;
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
