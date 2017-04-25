/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A simple static container, for holding game assets like textures and meshes.
    /// </summary>
    internal static class Assets {
        private static readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, SpriteSheet> sprites = new Dictionary<string, SpriteSheet>();
        private static readonly Dictionary<string, SoundCue> sounds = new Dictionary<string, SoundCue>();

        public static SpriteFont
            fontDefault,
            fontBold,
            fontTooltip,
            fontQuote;

        public static Model
            mdlSkybox;

        public static TextureCube
            tecSkybox;

        public static Effect
            fxUnlit,
            fxSkybox;

        public static Texture2D GetTexture(string name) {
            name = name.ToLowerInvariant();
            if (!textures.ContainsKey(name))
                throw new KeyNotFoundException($"Texture '{name}' was not found.");
            return textures[name];
        }

        public static SpriteSheet GetSprite(string name) {
            name = name.ToLowerInvariant();
            if (!sprites.ContainsKey(name))
                throw new KeyNotFoundException($"Sprite '{name}' was not found.");
            return sprites[name];
        }

        public static SoundCue GetSound(string name) {
            name = name.ToLowerInvariant();
            if (!sounds.ContainsKey(name))
                throw new KeyNotFoundException($"Sound '{name}' was not found.");
            return sounds[name];
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
            string[] cubeFaces = {"RT", "LF", "UP", "DN", "FT", "BK"};
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
            LoadSoundCues(content);
        }

        private static void LoadSoundCues(ContentManager content) {
            // load manifest file
            XmlDocument cueManifest = new XmlDocument();
            cueManifest.Load(content.RootDirectory + "/Audio/cuemanifest.xml");

            XmlNode xmlRoot = cueManifest.DocumentElement;
            if (xmlRoot == null || !String.Equals(xmlRoot.Name, "cues", StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidDataException("Sound cue manifest does not have root element named 'cues'.");

            // go over each child node
            XmlNode xmlCue = xmlRoot.FirstChild;
            while (xmlCue != null) {
                // sanity test
                if (!String.Equals(xmlCue.Name, "cue", StringComparison.InvariantCultureIgnoreCase))
                    throw new InvalidDataException("Child elements of 'cues' must be named 'cue'.");

                // grab cue name
                string cueName = xmlCue.Attributes?["name"]?.Value;
                if (cueName == null)
                    throw new InvalidDataException("Cue element must have an attribute named 'name'.");

                // go over the sound list
                SoundCue newCue = new SoundCue();
                XmlNode xmlCueSound = xmlCue.FirstChild;
                while (xmlCueSound != null) {
                    string soundName = xmlCueSound.Attributes?["name"]?.Value;
                    if (soundName == null)
                        throw new InvalidDataException("Sound element must have an attribute named 'name'.");
                    newCue.LoadSound(content, soundName);

                    xmlCueSound = xmlCueSound.NextSibling;
                }

                if (newCue.Count < 1)
                    throw new InvalidDataException("Cue element must have at least one sound");

                // save the cue
                sounds.Add(cueName.ToLowerInvariant(), newCue);

                xmlCue = xmlCue.NextSibling;
            }
        }
    }

}
