/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A UI element that shows the game logo and version number.
    /// </summary>
    internal sealed class PanelGameTitle : Control {

        private static float logoY;
        private static readonly string versionstr = Utils.GetVersionString() + " / Protocol " + NetShared.ProtocolVersion;

        private readonly float target;

        public PanelGameTitle(float target) {
            this.target = target;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/title"), new Vector2(SDGame.Inst.GameWidth / 2 - 128, SDGame.Inst.GameHeight / 4f + logoY), Color.White);

            spriteBatch.DrawString(Assets.fontDefault, Locale.Get("credit"), new Vector2(16, SDGame.Inst.GameHeight - 28), Color.Gray);
            spriteBatch.DrawString(Assets.fontDefault, versionstr, new Vector2(SDGame.Inst.GameWidth - Assets.fontDefault.MeasureString(versionstr).X - 16, SDGame.Inst.GameHeight - 28), Color.Gray);
        }

        protected override void OnUpdate(GameTime gameTime) {
            logoY += (target - logoY) * 4f * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

    }

}
