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

        /// <summary>
        /// Called when the control should compose images to the screen.
        /// </summary>
        /// <remarks>
        /// You cannot change SpriteBatch or GraphicsDevice state in OnDraw. If you need to draw something
        /// with a different blend mode or shader, use OnRender.
        /// </remarks>
        protected virtual void OnDraw(SpriteBatch spriteBatch) {}

        /// <summary>
        /// Called when the control should perform a logic update.
        /// </summary>
        /// <param name="gameTime"></param>
        protected virtual void OnUpdate(GameTime gameTime) {}

        /// <summary>
        /// Called when the control should render to any render targets.
        /// </summary>
        /// <param name="graphicsDevice"></param>
        /// <param name="spriteBatch"></param>
        protected virtual void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {}

        /// <summary>
        /// Called when the control should clean up after itself, in preparation for deletion.
        /// </summary>
        protected virtual void OnDestroy() {}

        /// <summary>
        /// Instructs the control and its children to perform any render target drawing operations.
        /// </summary>
        public void Render(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            if (!Visible) return;
            if (DrawMode == ControlDrawMode.ChildrenLast)
                OnRender(graphicsDevice, spriteBatch);
            Children.ForEach(ctl => ctl.Render(graphicsDevice, spriteBatch));
            if (DrawMode == ControlDrawMode.ChildrenFirst)
                OnRender(graphicsDevice, spriteBatch);
        }

        /// <summary>
        /// Instructs the control and its children to compose 2D images to the screen.
        /// </summary>
        /// <param name="spriteBatch"></param>
        public void Draw(SpriteBatch spriteBatch) {
            if (!Visible) return;
            if (DrawMode == ControlDrawMode.ChildrenLast)
                OnDraw(spriteBatch);
            Children.ForEach(ctl => ctl.Draw(spriteBatch));
            if (DrawMode == ControlDrawMode.ChildrenFirst)
                OnDraw(spriteBatch);
        }

        /// <summary>
        /// Runs an update cycle on the control and its children.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime) {
            OnUpdate(gameTime);
            Children.ForEach(ctl => ctl.Update(gameTime));
        }

        /// <summary>
        /// Instructs the control and its children to perform cleanup.
        /// </summary>
        public void Destroy() {
            OnDestroy();
            Children.ForEach(ctl => ctl.Destroy());
        }

    }

}
