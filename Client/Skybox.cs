/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Contains static utilities to render a skybox.
    /// </summary>
    internal sealed class Skybox : Control {

        private static RenderTarget2D rtSkybox;
        private static float rotation = Utils.RandomFloat(0f, MathHelper.TwoPi);

        private static readonly Matrix view = Matrix.CreateLookAt(
            new Vector3(0, 0, 0),
            new Vector3(0f, -0.25f, 1.5f),
            Vector3.Up);

        protected override void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            if (rtSkybox == null || rtSkybox.IsContentLost || rtSkybox.IsDisposed)
                rtSkybox = new RenderTarget2D(graphicsDevice, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight, false, SurfaceFormat.Color, DepthFormat.None);

            // draw the skybox to a render target so we can show it using the SpriteBatch later
            graphicsDevice.SetRenderTarget(rtSkybox);
            Effect fx = Assets.fxSkybox; // shortcut
            fx.Parameters["View"].SetValue(Matrix.CreateRotationY(rotation) * Matrix.CreateRotationZ(rotation) * view);
            fx.Parameters["Projection"].SetValue(SDGame.Inst.Projection);
            fx.Parameters["SkyboxTexture"].SetValue(Assets.tecSkybox);
            foreach (ModelMesh mesh in Assets.mdlSkybox.Meshes) {
                foreach (ModelMeshPart part in mesh.MeshParts) {
                    part.Effect = fx;
                }

                fx.CurrentTechnique.Passes[0].Apply();
                mesh.Draw();
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(rtSkybox, Vector2.Zero, Color.White);
        }

        protected override void OnUpdate(GameTime gameTime) {
            rotation += (float)(gameTime.ElapsedGameTime.TotalSeconds * 0.04);
            while (rotation >= MathHelper.TwoPi) rotation -= MathHelper.TwoPi;
        }

    }

}
