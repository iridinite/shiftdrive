/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Iridinite.Localization;

namespace ShiftDrive {

    internal static class Utils {

        private static float logoY = 100f;
        private static Localizer loc;

        internal static Random RNG { get; private set; }

        static Utils() {
            RNG = new Random();
        }

        /// <summary>
        /// Wraps t around so that it is never smaller than min and never larger than max.
        /// </summary>
        /// <param name="t">The value to wrap around.</param>
        /// <param name="min">The lower bound to wrap to.</param>
        /// <param name="max">The upper bound to wrap to.</param>
        /// <returns></returns>
        public static float Repeat(float t, float min, float max) {
            while (t < min) t += (max - min);
            while (t >= max) t -= (max - min);
            return t;
        }

        /// <summary>
        /// Calculates and returns the bearing (angle) from one point to another.
        /// </summary>
        /// <param name="origin">The point of reference.</param>
        /// <param name="target">The point to calculate the relative bearing of.</param>
        /// <returns></returns>
        public static float CalculateBearing(Vector2 origin, Vector2 target) {
            float dx = target.X - origin.X;
            float dy = target.Y - origin.Y;
            return Repeat((float)(Math.Atan2(dy, dx) * 180 / Math.PI) + 450f, 0f, 360f);
        }

        /// <summary>
        /// Draws the game title at the specified height, along with version info in the corner.
        /// </summary>
        public static void DrawTitle(SpriteBatch spriteBatch, float y) {
            logoY += (y - logoY) / 16f;
            spriteBatch.Draw(Assets.txTitle, new Vector2(SDGame.Inst.GameWidth / 2 - 128, SDGame.Inst.GameHeight / 4f + logoY), Color.White);

            string versionstr = "v" + Assembly.GetExecutingAssembly().GetName().Version +
                                " / Protocol " + NetShared.ProtocolVersion;
            spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("credit"), new Vector2(16, SDGame.Inst.GameHeight - 28), Color.Gray);
            spriteBatch.DrawString(Assets.fontDefault, versionstr, new Vector2(SDGame.Inst.GameWidth - Assets.fontDefault.MeasureString(versionstr).X - 16, SDGame.Inst.GameHeight - 28), Color.Gray);
        }

        public static void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end) {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads the string tables for localization.
        /// </summary>
        public static void LocaleLoad() {
            loc = new Localizer();
            loc.LoadLocale("Data//locale.txt");
            loc.LoadPhrases("Data//phrases.txt");
        }

        /// <summary>
        /// Returns the value for a key/value pair in the string table.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public static string LocaleGet(string key) {
            return loc.GetString(key);
        }

        /// <summary>
        /// Generates a random phrase for the given key in the phrase table.
        /// </summary>
        /// <param name="key">The key to look up.</param>
        public static string LocalePhrase(string key) {
            return loc.GetPhrase(key);
        }

    }
    
}