/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> for the helmsman's station.
    /// </summary>
    internal sealed class ConsoleHelm : Console {

        private bool glowVisible;
        private Vector2 glowPos;
        private float glowSize;

        public ConsoleHelm() {
            Children.Add(new PanelWorldView());
            Children.Add(new PanelHullBar());
            Children.Add(new PanelAnnounce());
            Children.Add(new PanelFuelGauge());
            Children.Add(new PanelThrottleBar());
            glowVisible = false;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            // pulse where user clicked
            if (glowVisible)
                spriteBatch.Draw(Assets.GetTexture("ui/glow1"), glowPos, null, Color.White * Math.Max(0f, 1f - glowSize), 0f, new Vector2(16, 16), glowSize * 4f, SpriteEffects.None, 0f);
        }

        protected override void OnUpdate(GameTime gameTime) {
            // animate the clicky glow pulse
            if (glowVisible) {
                glowSize += (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (glowSize >= 1f) glowVisible = false;
            }

            // ignore input from dead players
            if (NetClient.World.GetPlayerShip().destroyed) return;

            // rotate ship towards clicks inside the maneuver ring
            if (Input.GetMouseLeftDown() && Input.GetMouseInArea(100, 100, SDGame.Inst.GameWidth - 100, SDGame.Inst.GameHeight - 100)) {
                // send a steering message to the server
                Vector2 screenCenter = new Vector2(SDGame.Inst.GameWidth / 2f, SDGame.Inst.GameHeight / 2f);
                float newbearing = Utils.CalculateBearing(screenCenter, Input.MousePosition);
                using (Packet packet = new Packet(PacketID.HelmSteering)) {
                    packet.Write(newbearing);
                    NetClient.Send(packet);
                }

                glowPos = Input.MousePosition;
                glowSize = 0f;
                glowVisible = true;
            }
        }

    }

}
