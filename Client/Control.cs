/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents an interactive UI component.
    /// </summary>
    internal abstract class Control {

        public int x { get; set; }
        public int y { get; set; }

        public int width { get; set; }
        public int height { get; set; }

        public abstract void Draw(SpriteBatch spriteBatch);
        public abstract void Update(GameTime gameTime);
        
        public virtual void OnDestroy() {}

    }

}