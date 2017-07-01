/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal sealed class PanelCommsWaterfall : Control {

        private RenderTarget2D rtWaterfall;
        private float scrollTargetPos;          // target y position
        private float scrollAnimatedPos;        // currently visible y position

        public PanelCommsWaterfall(int width) {
            Width = width;
            Height = SDGame.Inst.GameHeight - 80;
            NetClient.CommsReceived += NetClient_CommsReceived;
        }

        private void NetClient_CommsReceived(CommMessage msg) {
            // calculate the height of the message, then shift down the current position so that
            // it looks like the new message slides in from the top
            var text = Utils.WrapText(Assets.fontDefault, msg.Body, Width - 20);
            var textsize = Assets.fontDefault.MeasureString(text);
            var heightInc = (int)textsize.Y + 30;
            scrollAnimatedPos -= heightInc;
        }

        protected override void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            if (rtWaterfall == null || rtWaterfall.IsDisposed || rtWaterfall.IsContentLost)
                rtWaterfall = new RenderTarget2D(graphicsDevice, Width, Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            graphicsDevice.SetRenderTarget(rtWaterfall);
            graphicsDevice.Clear(Color.Transparent);

            lock (NetClient.Inbox) {
                // don't even bother starting up a spritebatch if we have nothing to draw
                if (NetClient.Inbox.Count == 0) return;

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                int y = (int)scrollAnimatedPos;
                foreach (CommMessage msg in NetClient.Inbox) {
                    var text = Utils.WrapText(Assets.fontDefault, msg.Body, Width - 20);
                    var textsize = Assets.fontDefault.MeasureString(text);
                    var timestamp = msg.GetFuzzyTimestamp();
                    var timestampsize = Assets.fontDefault.MeasureString(timestamp);
                    // TODO: skip off-screen boxes
                    spriteBatch.DrawBorder(Assets.GetTexture("ui/roundrect"), new Rectangle(0, y, Width, (int)textsize.Y + 24), Color.White, 16);
                    spriteBatch.DrawString(Assets.fontBold, msg.Sender, new Vector2(10, y + 10), Color.Black);
                    spriteBatch.DrawString(Assets.fontDefault, timestamp, new Vector2(Width - 10 - (int)timestampsize.X, y + 10), Color.FromNonPremultiplied(96, 96, 96, 255));
                    spriteBatch.DrawString(Assets.fontDefault, text, new Vector2(10, y + 35), Color.FromNonPremultiplied(32, 32, 32, 255));

                    y += (int)textsize.Y + 30;
                }
                spriteBatch.End();
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            var bgRect = new Rectangle(SDGame.Inst.GameWidth - Width - 20, 0, Width + 20, SDGame.Inst.GameHeight);
            spriteBatch.Draw(Assets.GetTexture("ui/darkbg"), bgRect, bgRect, Color.White);

            spriteBatch.Draw(rtWaterfall, new Vector2(SDGame.Inst.GameWidth - Width - 10, 70), Color.White);
        }

        protected override void OnUpdate(GameTime gameTime) {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (Math.Abs(scrollTargetPos - scrollAnimatedPos) <= 0.5f)
                // if difference is subpixel, just snap to the target position
                scrollAnimatedPos = scrollTargetPos;
            else
                // animate the scrolling
                scrollAnimatedPos += (scrollTargetPos - scrollAnimatedPos) * 16.0f * deltaTime;

            // apply scrolling input
            if (!Input.GetMouseInArea(new Rectangle(SDGame.Inst.GameWidth - Width - 20, 0, Width + 20, SDGame.Inst.GameHeight))) return;
            scrollTargetPos += Input.MouseScroll * 0.4f;
            scrollTargetPos = MathHelper.Min(scrollTargetPos, 0.0f);
        }

        protected override void OnDestroy() {
            // properly disconnect the event
            NetClient.CommsReceived -= NetClient_CommsReceived;
        }

    }

}
