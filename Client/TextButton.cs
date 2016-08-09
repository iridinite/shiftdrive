/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an interactive text button.
    /// </summary>
    internal sealed class TextButton : Button {

        public string Caption { get; set; }

        public TextButton(int order, int x, int y, int width, int height, string caption)
            : base(order, x, y, width, height) {
            this.Caption = caption;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            base.Draw(spriteBatch);
            if (expand < 1f) return;

            int textOffset = (state == 2) ? 7 : 4;
            Vector2 txtSize = Assets.fontDefault.MeasureString(Caption);
            spriteBatch.DrawString(
                Assets.fontDefault,
                Caption,
                new Vector2(
                    (int)(x + width / 2 - txtSize.X / 2),
                    (int)(y + height / 2 - txtSize.Y / 2 + textOffset)),
                Color.Black);
        }

    }

}
