/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    internal sealed class PanelThrottleBar : Control {

        private float targetThrottle;

        public PanelThrottleBar() {
            lock (NetClient.worldLock) {
                targetThrottle = NetClient.World.GetPlayerShip().throttle;
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            int baseBarHeight = (SDGame.Inst.GameHeight - 100) / 2 - 100;

            // Throttle bar
            // we show local target throttle so that the UI always animates and is responsive,
            // even if the server will only actually apply the target throttle several frames later.
            // bar filling
            int throttleBarY = SDGame.Inst.GameHeight - 40 - baseBarHeight;
            int throttleFillH = (int)(baseBarHeight * targetThrottle);
            int throttleFillY = baseBarHeight - throttleFillH;
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"), new Rectangle(40, throttleBarY + throttleFillY, 64, throttleFillH), new Rectangle(64, throttleFillY, 64, throttleFillH), Color.White);
            // bar outline
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"), new Rectangle(40, throttleBarY, 64, 24), new Rectangle(0, 0, 64, 24), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"), new Rectangle(40, throttleBarY + baseBarHeight - 24, 64, 24), new Rectangle(0, 24, 64, 24), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, Locale.Get("throttle"), new Vector2(32, throttleBarY - 25), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, (int)(targetThrottle * 100f) + "%", new Vector2(128, throttleBarY + throttleFillY - 10), Color.White);
        }

        protected override void OnUpdate(GameTime gameTime) {
            // ignore input from dead players
            if (NetClient.World.GetPlayerShip().Destroyed) return;

            // throttle input
            float oldThrottle = targetThrottle;
            if (Input.GetKey(Keys.W)) {
                targetThrottle += (float)gameTime.ElapsedGameTime.TotalSeconds;
            } else if (Input.GetKey(Keys.S)) {
                targetThrottle -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            targetThrottle = MathHelper.Clamp(targetThrottle, 0f, 1f);

            // replicate throttle input to server, but avoid clogging bandwidth
            if (Math.Abs(oldThrottle - targetThrottle) > 0.01f)
                using (Packet packet = new Packet(PacketID.HelmThrottle)) {
                    packet.Write(targetThrottle);
                    NetClient.Send(packet);
                }
        }

    }

}
