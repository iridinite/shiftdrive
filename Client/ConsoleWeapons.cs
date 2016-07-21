/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {
    
    internal sealed class ConsoleWeapons : Console {

        public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            graphicsDevice.Clear(Color.FromNonPremultiplied(0, 0, 32, 255));

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            DrawLocalArea(spriteBatch);
            DrawFuelGauge(spriteBatch);

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
        }

    }

}