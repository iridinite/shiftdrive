/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A UI element that displays the player ship's hull and shield integrity as a bar.
    /// </summary>
    internal sealed class PanelHullBar : Control {

        private float hullFlicker = 0f;
        private float hullPrevious = 0f;
        private float hullDeclineWait = 0f;
        private float hullDecline = 0f;

        private static float shakeTime;
        private static float shakeTimeMax;

        public PanelHullBar() {
            // reset the shake if we're making a new hull bar. this is to avoid having the bar
            // suddenly shake when switching consoles, after damage was taken a long time ago
            shakeTime = 0f;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            var player = NetClient.World.GetPlayerShip();

            // hull integrity bar
            int hullbarx = 64;
            if (shakeTime > 0f)
                // apply a random offset to the hullbar when taking damage
                hullbarx += (int)(Utils.RandomFloat(-1.0f, 7.0f) * (shakeTime / shakeTimeMax));
            float hullFraction = player.Hull / player.HullMax;
            float shieldFraction = player.Shield / player.ShieldMax;
            Color outlineColor = hullFraction <= 0.35f && hullFlicker >= 0.5f ? Color.Red : Color.White;
            Color hullbarColor = hullFraction <= 0.35f ? Color.Red : hullFraction <= 0.7f ? Color.Orange : Color.SlateGray;

            // background
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 64, 512, 64), Color.White);

            // health bars
            if (player.Shield <= 0f || !player.ShieldActive) {
                // if shields are not dead, draw decline below the main hull
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx, 0, (int)(hullDecline / player.HullMax * 512f), 64),
                    new Rectangle(0, 128, (int)(hullDecline / player.HullMax * 512f), 64),
                    Color.Purple);
            }

            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                new Rectangle(hullbarx, 0, (int)(hullFraction * 512f), 64),
                new Rectangle(0, 128, (int)(hullFraction * 512f), 64),
                hullbarColor);

            if (player.ShieldActive && player.Shield > 0f) {
                // if shields are active, draw decline on top of the main hull so we can still see it
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx, 0, (int)(hullDecline / player.ShieldMax * 512f), 64),
                    new Rectangle(0, 128, (int)(hullDecline / player.ShieldMax * 512f), 64),
                    Color.Purple);
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx, 0, (int)(shieldFraction * 512f), 64),
                    new Rectangle(0, 128, (int)(shieldFraction * 512f), 64),
                    Color.LightSkyBlue);
            }

            // outline
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 0, 512, 64), outlineColor);
        }

        protected override void OnUpdate(GameTime gameTime) {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var player = NetClient.World.GetPlayerShip();
            var healthVal = player.ShieldActive && player.Shield > 0f ? player.Shield : player.Hull;
            var healthValMax = player.ShieldActive && player.Shield > 0f ? player.ShieldMax : player.HullMax;

            if (healthVal > hullDecline) {
                // no animation for upped hull
                hullDecline = healthVal;
                hullPrevious = healthVal;
            }

            if (hullDeclineWait > 0f)
                hullDeclineWait -= dt;

            if (hullDecline > healthVal) {
                // there is still a decline bar to show
                if (hullPrevious > healthVal) {
                    // if we just took damage, wait before declining
                    hullDeclineWait = 0.75f;
                    hullPrevious = healthVal;
                }
                if (hullDeclineWait <= 0f) // animate the hull integrity loss
                    hullDecline = MathHelper.Max(healthVal, hullDecline - dt * healthValMax * 0.2f);
            }

            hullFlicker += dt;
            if (hullFlicker >= 1f) hullFlicker = 0f;

            if (shakeTime > 0f) shakeTime -= dt;
        }

        public static void SetShake(float time) {
            Debug.Assert(time > 0f);
            shakeTime = time;
            shakeTimeMax = time;
        }

    }

}
