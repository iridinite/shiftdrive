/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Contains static utilities to render a skybox.
    /// </summary>
    internal static class Skybox {

        private static float rotation = Utils.RandomFloat(0f, MathHelper.TwoPi);

        public static void Draw(GraphicsDevice graphicsDevice) {
            // Use the unlit shader to render a skybox.
            Effect fx = Assets.fxUnlit; // shortcut
            fx.Parameters["WVP"].SetValue(Matrix.CreateRotationY(rotation) * Matrix.CreateRotationZ(rotation) *
                                          Matrix.CreateLookAt(new Vector3(0f, -0.25f, 2f), new Vector3(0, 0, 0),
                                              Vector3.Up) * SDGame.Inst.Projection);
            fx.Parameters["ModelTexture"].SetValue(Assets.textures["ui/skybox"]);
            foreach (ModelMesh mesh in Assets.mdlSkybox.Meshes) {
                foreach (ModelMeshPart part in mesh.MeshParts) {
                    part.Effect = fx;
                }

                fx.CurrentTechnique.Passes[0].Apply();
                mesh.Draw();
            }
        }

        public static void Update(GameTime gameTime) {
            rotation += (float)(gameTime.ElapsedGameTime.TotalSeconds * 0.04);
            while (rotation >= MathHelper.TwoPi) rotation -= MathHelper.TwoPi;
        }

    }

}
