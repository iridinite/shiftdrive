/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> for the helmsman's station.
    /// </summary>
    internal sealed class ConsoleHelm : Console {

        private float targetThrottle;

        private bool glowVisible;
        private Vector2 glowPos;
        private float glowSize;

        public ConsoleHelm() {
            lock (NetClient.worldLock) {
                glowVisible = false;
                targetThrottle = Player.throttle;
            }
        }

        public override void Draw(SpriteBatch spriteBatch) {
            DrawLocalArea(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            int baseBarHeight = (SDGame.Inst.GameHeight - 100) / 2 - 100;

            // Throttle bar
            // we show local target throttle so that the UI always animates and is responsive,
            // even if the server will only actually apply the target throttle several frames later.
            // bar filling
            int throttleBarY = SDGame.Inst.GameHeight - 40 - baseBarHeight;
            int throttleFillH = (int)(baseBarHeight * targetThrottle);
            int throttleFillY = baseBarHeight - throttleFillH;
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
            Vector2 screenCenter = new Vector2(SDGame.Inst.GameWidth / 2f, SDGame.Inst.GameHeight / 2f);
            if (Mouse.GetLeftDown() && Vector2.DistanceSquared(screenCenter, Mouse.Position) < 122500) {
                // send a steering message to the server
                float newbearing = Utils.CalculateBearing(screenCenter, Mouse.Position);
                using (Packet packet = new Packet(PacketID.HelmSteering)) {
                    packet.Write(newbearing);
                    NetClient.Send(packet);
                }

                glowPos = Mouse.Position;
                glowSize = 0f;
                glowVisible = true;
            }

            // throttle input
            float oldThrottle = targetThrottle;
            if (KeyInput.GetHeld(Keys.W)) {
                targetThrottle += (float)gameTime.ElapsedGameTime.TotalSeconds;
            } else if (KeyInput.GetHeld(Keys.S)) {
                targetThrottle -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            }
            targetThrottle = MathHelper.Clamp(targetThrottle, 0f, 1f);

            // replicate throttle input to server, but avoid clogging bandwidth
            if (Math.Abs(oldThrottle - targetThrottle) > 0.01f)
                using (Packet packet = new Packet(PacketID.HelmThrottle)) {
                    packet.Write(targetThrottle);
                    NetClient.Send(packet);
                }

            // animate the clicky glow pulse
            if (glowVisible) {
                glowSize += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (glowSize >= 1f) glowVisible = false;
            }
        }

    }

}