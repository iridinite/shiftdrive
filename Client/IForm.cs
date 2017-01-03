/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a full-screen form.
    /// </summary>
    internal interface IForm {
        void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);
        void Update(GameTime gameTime);
    }

}