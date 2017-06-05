/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Extension methods for <seealso cref="SpriteBatch"/>.
    /// </summary>
    internal static class SpriteBatchExtensions {

        /// <summary>
        /// Draws a background with a border, using a 9-sliced texture.
        /// </summary>
        /// <param name="spriteBatch">The <seealso cref="SpriteBatch"/> instance to draw with.</param>
        /// <param name="tex">The sliced texture to use. Subtextures are expected to be evenly split in a 3x3 grid.</param>
        /// <param name="dest">The screen-coordinate rectangle at which to draw the border and background.</param>
        /// <param name="tilesize">The size, in pixels, of individual tiles (square, both width and height).</param>
        public static void DrawBorder(this SpriteBatch spriteBatch, Texture2D tex, Rectangle dest, int tilesize, int state = 0) {
            int doublesize = tilesize * 2;
            int xoffset = state * tilesize * 3;

            spriteBatch.Draw(tex,
                new Rectangle(dest.X, dest.Y, tilesize, tilesize),
                new Rectangle(xoffset, 0, tilesize, tilesize), Color.White); // top left
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + tilesize, dest.Y, dest.Width - doublesize, tilesize),
                new Rectangle(xoffset + tilesize, 0, tilesize, tilesize), Color.White); // top middle
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + dest.Width - tilesize, dest.Y, tilesize, tilesize),
                new Rectangle(xoffset + doublesize, 0, tilesize, tilesize), Color.White); // top right

            spriteBatch.Draw(tex,
                new Rectangle(dest.X, dest.Y + tilesize, tilesize, dest.Height - doublesize),
                new Rectangle(xoffset, tilesize, tilesize, tilesize), Color.White); // middle left
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + tilesize, dest.Y + tilesize, dest.Width - doublesize, dest.Height - doublesize),
                new Rectangle(xoffset + tilesize, tilesize, tilesize, tilesize), Color.White); // center
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + dest.Width - tilesize, dest.Y + tilesize, tilesize, dest.Height - doublesize),
                new Rectangle(xoffset + doublesize, tilesize, tilesize, tilesize), Color.White); // middle right

            spriteBatch.Draw(tex,
                new Rectangle(dest.X, dest.Y + dest.Height - tilesize, tilesize, tilesize),
                new Rectangle(xoffset, doublesize, tilesize, tilesize), Color.White); // bottom left
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + tilesize, dest.Y + dest.Height - tilesize, dest.Width - doublesize, tilesize),
                new Rectangle(xoffset + tilesize, doublesize, tilesize, tilesize), Color.White); // bottom middle
            spriteBatch.Draw(tex,
                new Rectangle(dest.X + dest.Width - tilesize, dest.Y + dest.Height - tilesize, tilesize, tilesize),
                new Rectangle(xoffset + doublesize, doublesize, tilesize, tilesize), Color.White); // bottom right
        }

    }

}
