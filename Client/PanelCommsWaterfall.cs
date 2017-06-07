/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal sealed class PanelCommsWaterfall : Control {

        private RenderTarget2D rtWaterfall;

        public PanelCommsWaterfall(int width) {
            Width = width;
            Height = SDGame.Inst.GameHeight - 80;
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

                int y = 0;
                foreach (CommMessage msg in NetClient.Inbox) {
                    var text = Utils.WrapText(Assets.fontDefault, msg.Body, Width - 20);
                    var textsize = Assets.fontDefault.MeasureString(text);
                    var timestamp = msg.GetFuzzyTimestamp();
                    var timestampsize = Assets.fontDefault.MeasureString(timestamp);
                    spriteBatch.DrawBorder(Assets.GetTexture("ui/roundrect"), new Rectangle(0, y, Width, (int)textsize.Y + 24), 16);
                    spriteBatch.DrawString(Assets.fontBold, msg.Sender, new Vector2(10, y + 10), Color.Black);
                    spriteBatch.DrawString(Assets.fontDefault, timestamp, new Vector2(Width - 10 - (int)timestampsize.X, y + 10), Color.FromNonPremultiplied(96, 96, 96, 255));
                    spriteBatch.DrawString(Assets.fontDefault, text, new Vector2(10, y + 35), Color.FromNonPremultiplied(32, 32, 32, 255));

                    y += (int)textsize.Y + 30;
                }
                spriteBatch.End();
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(SDGame.Inst.GameWidth - Width - 20, 0, Width + 20, SDGame.Inst.GameHeight), Color.CornflowerBlue);
            spriteBatch.Draw(rtWaterfall, new Vector2(SDGame.Inst.GameWidth - Width - 10, 70), Color.White);
        }



    }

}
