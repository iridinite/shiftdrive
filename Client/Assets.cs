/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A simple static container, for holding game assets like textures and meshes.
    /// </summary>
    internal static class Assets {

        public static SpriteFont
            fontDefault,
            fontBold,
            fontTooltip,
            fontQuote;

        public static readonly Dictionary<string, Texture2D>
            textures = new Dictionary<string, Texture2D>();

        public static readonly Dictionary<string, SpriteSheet>
            sprites = new Dictionary<string, SpriteSheet>();

        public static Model
            mdlSkybox;

        public static TextureCube
            tecSkybox;

        public static Effect
            fxUnlit,
            fxSkybox;

        public static SoundEffect
            sndUIConfirm,
            sndUICancel,
            sndUIAppear1,
            sndUIAppear2,
            sndUIAppear3,
            sndUIAppear4;

        public static Texture2D GetTexture(string name) {
            if (!textures.ContainsKey(name.ToLowerInvariant()))
                throw new KeyNotFoundException($"Texture '{name}' was not found.");
            return textures[name.ToLowerInvariant()];
        }

        public static SpriteSheet GetSprite(string name) {
            if (!sprites.ContainsKey(name.ToLowerInvariant()))
                throw new KeyNotFoundException($"Sprite '{name}' was not found.");
            return sprites[name.ToLowerInvariant()];
        }
        
        private static string CleanFilename(FileInfo file, DirectoryInfo dir) {
            string shortname = file.FullName.Substring(dir.FullName.Length).Replace('\\', '/').ToLowerInvariant();
            shortname = shortname.Substring(0, shortname.Length - file.Extension.Length);
            return shortname;
        }

        public static void LoadContent(GraphicsDevice graphicsDevice, ContentManager content) {
            // load font textures
            fontDefault = content.Load<SpriteFont>("Fonts/Default");
            fontDefault.LineSpacing = 20;
            fontBold = content.Load<SpriteFont>("Fonts/Bold");
            fontTooltip = content.Load<SpriteFont>("Fonts/Tooltip");
            fontQuote = content.Load<SpriteFont>("Fonts/Quote");

            // get folder containing texture and sprite files
            DirectoryInfo texturesDir = new DirectoryInfo($"{content.RootDirectory}{Path.DirectorySeparatorChar}Textures{Path.DirectorySeparatorChar}");

            // enumerate and load all textures
            foreach (FileInfo file in texturesDir.GetFiles("*.xnb", SearchOption.AllDirectories)) {
                string shortname = CleanFilename(file, texturesDir);
                textures.Add(shortname, content.Load<Texture2D>("Textures/" + shortname));
            }
            // parse sprite sheet definitions
            foreach (FileInfo file in texturesDir.GetFiles("*.xml", SearchOption.AllDirectories)) {
                string shortname = CleanFilename(file, texturesDir);
                sprites.Add(shortname, SpriteSheet.FromFile(file.FullName));
            }

            // set up skybox
            mdlSkybox = content.Load<Model>("Models/Skybox");
            tecSkybox = new TextureCube(graphicsDevice, 1024, false, SurfaceFormat.Color);

            // assemble skybox cubemap from individual textures
            int skyNum = Utils.RNG.Next(1, 4);
            string[] cubeFaces = { "RT", "LF", "UP", "DN", "FT", "BK" };
            for (int i = 0; i < 6; i++) {
                using (Texture2D skyFace = content.Load<Texture2D>("Skyboxes/sky" + skyNum + cubeFaces[i])) {
                    Color[] skyData = new Color[1024 * 1024];
                    skyFace.GetData(skyData);
                    tecSkybox.SetData((CubeMapFace)i, skyData);
                }
            }

            // load shaders
            fxUnlit = content.Load<Effect>("Shaders/Unlit");
            fxSkybox = content.Load<Effect>("Shaders/Skybox");

            // sound effects
            sndUIConfirm = content.Load<SoundEffect>("Audio/SFX/ui_confirm");
            sndUICancel = content.Load<SoundEffect>("Audio/SFX/ui_cancel");
            sndUIAppear1 = content.Load<SoundEffect>("Audio/SFX/ui_appear1");
            sndUIAppear2 = content.Load<SoundEffect>("Audio/SFX/ui_appear2");
            sndUIAppear3 = content.Load<SoundEffect>("Audio/SFX/ui_appear3");
            sndUIAppear4 = content.Load<SoundEffect>("Audio/SFX/ui_appear4");
        }

    }

}
