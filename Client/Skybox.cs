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
    internal static class Skybox {

        private static float rotation = Utils.RandomFloat(0f, MathHelper.TwoPi);

        private static readonly Matrix view = Matrix.CreateLookAt(
            new Vector3(0, 0, 0),
            new Vector3(0f, -0.25f, 1.5f),
            Vector3.Up);

        public static void Draw() {
            // Use the unlit shader to render a skybox.
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

        public static void Update(GameTime gameTime) {
            rotation += (float)(gameTime.ElapsedGameTime.TotalSeconds * 0.04);
            while (rotation >= MathHelper.TwoPi) rotation -= MathHelper.TwoPi;
        }

    }

}
