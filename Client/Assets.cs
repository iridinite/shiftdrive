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

        public static Texture2D
            txTitle,
            txRect,
            txButton,
            txTextEntry,
            txSkybox,
            txRadarRing,
            txGlow1,
            txFillbar,
            txHullBar,
            txChargeBar,
            txAnnouncePanel,
            txItemIcons;

        public static Dictionary<string, Texture2D>
            txMapIcons,
            txModelTextures;
        
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

    }

}
