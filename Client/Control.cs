/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Specifies how a <seealso cref="Control"/> is drawn.
    /// </summary>
    internal enum ControlDrawMode {
        ChildrenLast,
        ChildrenFirst
    }

    /// <summary>
    /// Represents an interactive UI component.
    /// </summary>
    internal abstract class Control {
        protected int X { get; set; }
        protected int Y { get; set; }

        protected int Width { get; set; }
        protected int Height { get; set; }

        public bool Visible { get; set; } = true;
        public ControlDrawMode DrawMode { get; set; } = ControlDrawMode.ChildrenFirst;

        protected readonly List<Control> Children = new List<Control>();

        protected virtual void OnDraw(SpriteBatch spriteBatch) {}
        protected virtual void OnUpdate(GameTime gameTime) {}
        protected virtual void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {}
        protected virtual void OnDestroy() {}

        public void Render(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            if (!Visible) return;
            if (DrawMode == ControlDrawMode.ChildrenLast)
                OnRender(graphicsDevice, spriteBatch);
            Children.ForEach(ctl => ctl.Render(graphicsDevice, spriteBatch));
            if (DrawMode == ControlDrawMode.ChildrenFirst)
                OnRender(graphicsDevice, spriteBatch);
        }

        public void Draw(SpriteBatch spriteBatch) {
            if (!Visible) return;
            if (DrawMode == ControlDrawMode.ChildrenLast)
                OnDraw(spriteBatch);
            Children.ForEach(ctl => ctl.Draw(spriteBatch));
            if (DrawMode == ControlDrawMode.ChildrenFirst)
                OnDraw(spriteBatch);
        }

        public void Update(GameTime gameTime) {
            OnUpdate(gameTime);
            Children.ForEach(ctl => ctl.Update(gameTime));
        }

        public void Destroy() {
            OnDestroy();
            Children.ForEach(ctl => ctl.Destroy());
        }
    }

}
