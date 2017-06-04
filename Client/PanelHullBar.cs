/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

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

        protected override void OnDraw(SpriteBatch spriteBatch) {
            var player = NetClient.World.GetPlayerShip();

            // hull integrity bar
            const int hullbarx = 64;
            float hullFraction = player.hull / player.hullMax;
            float shieldFraction = player.shield / player.shieldMax;
            Color outlineColor = hullFraction <= 0.35f && hullFlicker >= 0.5f ? Color.Red : Color.White;
            Color hullbarColor = hullFraction <= 0.35f ? Color.Red : hullFraction <= 0.7f ? Color.Orange : Color.Green;

            // background
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 64, 512, 64), Color.White);

            // hull
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                new Rectangle(hullbarx, 0, (int)(hullDecline / player.hullMax * 512f), 64),
                new Rectangle(0, 128, (int)(hullDecline / player.hullMax * 512f), 64),
                Color.Purple);
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                new Rectangle(hullbarx, 0, (int)(hullFraction * 512f), 64),
                new Rectangle(0, 128, (int)(hullFraction * 512f), 64),
                hullbarColor);

            // shields
            int shieldPixels = (int)(shieldFraction * 352f); // chop off 160 pixels left
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                new Rectangle(hullbarx + 160 + 352 - shieldPixels, 0, shieldPixels, 64),
                new Rectangle(160 + 352 - shieldPixels, 192, shieldPixels, 64),
                player.shieldActive ? Color.LightSkyBlue : Color.Gray);
            // outline
            spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 0, 512, 64), outlineColor);
        }

        protected override void OnUpdate(GameTime gameTime) {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var player = NetClient.World.GetPlayerShip();

            if (player.hull > hullDecline) {
                // no animation for upped hull
                hullDecline = player.hull;
                hullPrevious = player.hull;
            }

            if (hullDeclineWait > 0f)
                hullDeclineWait -= dt;

            if (hullDecline > player.hull) {
                // there is still a decline bar to show
                if (hullPrevious > player.hull) {
                    // if we just took damage, wait before declining
                    hullDeclineWait = 0.75f;
                    hullPrevious = player.hull;
                }
                if (hullDeclineWait <= 0f) // animate the hull integrity loss
                    hullDecline = MathHelper.Max(player.hull, hullDecline - dt * player.hullMax * 0.2f);
            }

            hullFlicker += dt;
            if (hullFlicker >= 1f) hullFlicker = 0f;
        }

    }

}
