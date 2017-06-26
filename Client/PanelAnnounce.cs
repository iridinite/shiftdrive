/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a UI element that shows the server's last announcement text.
    /// </summary>
    internal sealed class PanelAnnounce : Control {

        private string announceText = String.Empty;
        private float announceHoldTime;

        public PanelAnnounce() {
            // subscribe to networking events
            NetClient.Announcement += NetClient_Announcement;
        }

        private void NetClient_Announcement(string text) {
            announceHoldTime = 10f;
            announceText = text;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/announcepanel"), new Rectangle(SDGame.Inst.GameWidth - 450, -20, 512, 64), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, announceText, new Vector2(SDGame.Inst.GameWidth - 430, 12), Color.White);
        }

        protected override void OnUpdate(GameTime gameTime) {
            // animate announcement text
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            announceHoldTime -= dt;
            if (announceHoldTime < 0f)
                announceText = "";
        }

        protected override void OnDestroy() {
            NetClient.Announcement -= NetClient_Announcement;
        }

    }

}
