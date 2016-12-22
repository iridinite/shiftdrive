/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an interactive button with an image label.
    /// </summary>
    internal sealed class ImageButton : Button {

        private Texture2D image;
        private Rectangle? imageSource;
        private Vector2 imageSize;
        private Color imageColor;

        public ImageButton(int order, int x, int y, int width, int height, Texture2D image)
            : base(order, x, y, width, height) {
            this.image = image;
            imageColor = Color.White;
            SetSourceRect(null);
        }

        public ImageButton(int order, int x, int y, int width, int height, Texture2D image, Color color)
            : this(order, x, y, width, height, image) {
            imageColor = color;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            base.Draw(spriteBatch);
            if (expand < 1f) return;

            int textOffset = (state == 2) ? 2 : 0;
            spriteBatch.Draw(image,
                new Rectangle(
                    (int)(x + width / 2 - imageSize.X / 2),
                    (int)(y + height / 2 - imageSize.Y / 2 + textOffset),
                    (int)imageSize.X,
                    (int)imageSize.Y),
                imageSource,
                imageColor);
        }

        public void SetSourceRect(Rectangle? rect) {
            imageSource = rect;
            imageSize = rect.HasValue
                ? new Vector2(rect.Value.Width, rect.Value.Height)
                : new Vector2(image.Width, image.Height);
        }

    }

}
