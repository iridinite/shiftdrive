/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace ShiftDrive {

    /// <summary>
    /// A simple static container, for holding game assets like textures and meshes.
    /// </summary>
    internal static class Assets {

        public static SpriteFont
            fontDefault,
            fontBold;
        
        public static readonly Dictionary<string, Texture2D>
            textures = new Dictionary<string, Texture2D>();
        
        public static Model
            mdlSkybox;

        public static Effect
            fxUnlit;

        public static SoundEffect
            sndUIConfirm,
            sndUICancel,
            sndUIAppear1,
            sndUIAppear2,
            sndUIAppear3,
            sndUIAppear4;

        public static Texture2D GetTexture(string name) {
            if (!textures.ContainsKey(name.ToLowerInvariant()))
                throw new KeyNotFoundException("Texture '" + name + "' was not found.");
            return textures[name.ToLowerInvariant()];
        }

    }

}
