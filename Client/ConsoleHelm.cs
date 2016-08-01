/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    internal sealed class ConsoleHelm : Console {

        private readonly Button
            btnShiftShow,
            btnShiftConfirm,
            btnShiftAbort;

        private readonly TextField
            txtShiftDist,
            txtShiftDir;

        private bool shiftPanelOpen;
        private bool shiftConfirm;

        private bool shiftCharging;
        private float shiftCharge;
        private float targetThrottle;

        private bool glowVisible;
        private Vector2 glowPos;
        private float glowSize;

        public ConsoleHelm() {
            lock (NetClient.worldLock) {
                glowVisible = false;
                shiftPanelOpen = false;
                targetThrottle = Player.throttle;
            }

            txtShiftDist = new TextField(20, 170, 100);
            txtShiftDir = new TextField(20, 220, 100);

            btnShiftShow = new Button(1, 16, 100, 30, 30, ">");
            btnShiftShow.OnClick += BtnShiftShow_OnClick;
            btnShiftConfirm = new Button(1, 170, 215, 100, 35, "Jump");
            btnShiftConfirm.OnClick += BtnShiftConfirm_OnClick;
            btnShiftConfirm.Enabled = false;
            btnShiftAbort = new Button(1, 200, 200, 100, 35, "Abort");
        }

        public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            graphicsDevice.Clear(Color.Black);
            DrawLocalArea(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            int baseBarHeight = (SDGame.Inst.GameHeight - 100) / 2 - 100;

            /*if (shiftPanelOpen) {
                // expanded shift drive control panel
                spriteBatch.DrawString(Assets.fontBold, "Shift Drive Control", new Vector2(55, 105), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, "Direction (km):", new Vector2(20, 150), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, "Bearing:", new Vector2(20, 200), Color.White);
                txtShiftDist.Draw(spriteBatch);
                txtShiftDir.Draw(spriteBatch);
                btnShiftConfirm.Draw(spriteBatch);
                btnShiftShow.Draw(spriteBatch);
            } else if (shiftCharging) {
                // shift drive is charging, draw progress bar
                // status text
                spriteBatch.DrawString(Assets.fontDefault, "Shift Drive Charging...", new Vector2(100, 120), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, ((int)(shiftCharge * 100f)).ToString() + "%", new Vector2(100, 145), Color.White);
                // bar filling
                int shiftbarheight = (int)(baseBarHeight * shiftCharge);
                int shiftbary = 100 + (baseBarHeight - shiftbarheight) / 2;
                spriteBatch.Draw(Assets.txFillbar, new Rectangle(40, shiftbary, 64, shiftbarheight), new Rectangle(64, shiftbary, 64, shiftbarheight), Color.White);
                // bar outline
                spriteBatch.Draw(Assets.txFillbar, new Rectangle(40, 100, 64, 24), new Rectangle(0, 0, 64, 24), Color.White);
                spriteBatch.Draw(Assets.txFillbar, new Rectangle(40, 276, 64, 24), new Rectangle(0, 24, 64, 24), Color.White);
            } else {
                // collapsed shift control panel
                spriteBatch.DrawString(Assets.fontBold, "Shift Drive", new Vector2(55, 105), Color.White);
                btnShiftShow.Draw(spriteBatch);
            }*/

            // Throttle bar
            // we show local target throttle so that the UI always animates and is responsive,
            // even if the server will only actually apply the target throttle several frames later.
            // bar filling
            int throttleBarY = SDGame.Inst.GameHeight - 40 - baseBarHeight;
            int throttleFillH = (int)(baseBarHeight * targetThrottle);
            int throttleFillY =  baseBarHeight - throttleFillH;
            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(40, throttleBarY + throttleFillY, 64, throttleFillH), new Rectangle(64, throttleFillY, 64, throttleFillH), Color.White);
            // bar outline
            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(40, throttleBarY, 64, 24), new Rectangle(0, 0, 64, 24), Color.White);
            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(40, throttleBarY + baseBarHeight - 24, 64, 24), new Rectangle(0, 24, 64, 24), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, "Throttle", new Vector2(32, throttleBarY - 25), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, (int)(targetThrottle * 100f) + "%", new Vector2(128, throttleBarY + throttleFillY - 10), Color.White);

            // fuel bar
            DrawFuelGauge(spriteBatch);
            
            // pulse where user clicked
            if (glowVisible)
                spriteBatch.Draw(Assets.textures["ui/glow1"], glowPos, null, Color.White * Math.Max(0f, 1f - glowSize), 0f, new Vector2(16, 16), glowSize * 4f, SpriteEffects.None, 0f);

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);

            // rotate ship towards clicks inside the maneuver ring
            if (Mouse.GetLeftDown() && Vector2.DistanceSquared(new Vector2(SDGame.Inst.GameWidth / 2, SDGame.Inst.GameHeight / 2), Mouse.Position) < 122500) {
                // send a steering message to the server
                float newbearing = Utils.CalculateBearing(new Vector2(SDGame.Inst.GameWidth / 2, SDGame.Inst.GameHeight / 2), Mouse.Position);
                Packet steerPacket = new Packet(PacketType.HelmSteering, BitConverter.GetBytes(newbearing));
                NetClient.Send(steerPacket);

                glowPos = Mouse.Position;
                glowSize = 0f;
                glowVisible = true;
            }

            // replicate throttle input to server
            if (KeyInput.GetHeld(Keys.W)) {
                targetThrottle += (float)gameTime.ElapsedGameTime.TotalSeconds;
            } else if (KeyInput.GetHeld(Keys.S)) {
                targetThrottle -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            targetThrottle = MathHelper.Clamp(targetThrottle, 0f, 1f);
            NetClient.Send(new Packet(PacketType.HelmThrottle, BitConverter.GetBytes(targetThrottle)));

            // temp shfit charge
            if (shiftCharging) {
                shiftCharge += (float)gameTime.ElapsedGameTime.TotalSeconds * 0.2f;
                if (shiftCharge >= 1f) shiftCharge = 1f;
            }

            // UI updates
            btnShiftShow.Update(gameTime);
            if (shiftPanelOpen) {
                int dummy;
                txtShiftDist.Update(gameTime);
                txtShiftDir.Update(gameTime);
                btnShiftConfirm.Enabled = (txtShiftDist.text.Length > 0 && txtShiftDir.text.Length > 0 &&
                     int.TryParse(txtShiftDist.text, out dummy) && int.TryParse(txtShiftDir.text, out dummy));
                btnShiftConfirm.Update(gameTime);
            }

            // animate the clicky glow pulse
            if (glowVisible) {
                glowSize += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (glowSize >= 1f) glowVisible = false;
            }
        }

        private void BtnShiftShow_OnClick(Button sender) {
            // toggle display of shift drive controls
            shiftConfirm = false;
            shiftPanelOpen = !shiftPanelOpen;
            btnShiftConfirm.Caption = "Jump";
            btnShiftShow.Caption = shiftPanelOpen ? "<" : ">";
        }

        private void BtnShiftConfirm_OnClick(Button sender) {
            if (shiftConfirm) {
                shiftPanelOpen = false;
                shiftCharging = true;
                shiftCharge = 0f;
            } else {
                shiftConfirm = true;
                btnShiftConfirm.Caption = "Confirm?";
            }
        }

    }

}