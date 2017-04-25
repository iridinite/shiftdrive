/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Exposes common functionality used throughout the program.
    /// </summary>
    internal static class Utils {

        private static float logoY = 100f;

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
        /// Calculates the position of an object in screen coordinates, based on the specified world view area.
        /// </summary>
        /// <param name="min">The upper-left corner of the view area.</param>
        /// <param name="max">The bottom-right corner of the view area.</param>
        /// <param name="pos">The object's position in world coordinates.</param>
        public static Vector2 CalculateScreenPos(Vector2 min, Vector2 max, Vector2 pos) {
            return new Vector2(
                (pos.X - min.X) / (max.X - min.X) * SDGame.Inst.GameWidth,
                (pos.Y - min.Y) / (max.Y - min.Y) * SDGame.Inst.GameWidth - (SDGame.Inst.GameWidth - SDGame.Inst.GameHeight) / 2f);
        }

        /// <summary>
        /// Calculates the position for a given offset when rotated by the specified angle around the origin (0, 0).
        /// </summary>
        /// <param name="offset">The offset to rotate, relative to (0, 0).</param>
        /// <param name="angle">The angle to rotate by.</param>
        public static Vector2 CalculateRotatedOffset(Vector2 offset, float angle) {
            float offsetlen = offset.Length();
            float relangle = CalculateBearing(Vector2.Zero, -offset);

            return new Vector2(
                offsetlen * (float)Math.Cos(MathHelper.ToRadians(angle + relangle + 90f)),
                offsetlen * (float)Math.Sin(MathHelper.ToRadians(angle + relangle + 90f)));
        }

        /// <summary>
        /// Returns a random floating point number in the specified range.
        /// </summary>
        /// <param name="min">The inclusive lower bound.</param>
        /// <param name="max">The exclusive upper bound.</param>
        public static float RandomFloat(float min, float max) {
            return (float)RNG.NextDouble() * (max - min) + min;
        }

        /// <summary>
        /// Word-wraps a string so that it will not overflow a box of the specified width.
        /// </summary>
        /// <param name="font">A <see cref="SpriteFont"/> to reference for measurement.</param>
        /// <param name="text">The text to wrap.</param>
        /// <param name="width">The line-wrap edge, in pixels.</param>
        /// <returns></returns>
        public static string WrapText(SpriteFont font, string text, float width) {
            StringBuilder result = new StringBuilder();
            string[] lines = text.Replace("\x0D", "").Split('\x0A'); // strip \r, split on \n

            for (int i = 0; i < lines.Length; i++) {
                // add words until overflow, then line-break
                string line = lines[i];
                string totalline = "";
                string[] words = line.Split(' ');
                foreach (string word in words) {
                    if (font.MeasureString(totalline + word).X > width) {
                        result.AppendLine(totalline);
                        totalline = "";
                    }
                    totalline += word + " ";
                }
                // don't leave off the last line
                if (totalline.Length > 0)
                    result.Append(totalline);
                // add line breaks in between, but not on the last line
                if (i < lines.Length)
                    result.AppendLine();
            }

            return result.ToString();
        }

        /// <summary>
        /// Draws the game title at the specified height, along with version info in the corner.
        /// </summary>
        public static void DrawTitle(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/title"), new Vector2(SDGame.Inst.GameWidth / 2 - 128, SDGame.Inst.GameHeight / 4f + logoY), Color.White);

            string versionstr = GetVersionString() +
                                " / Protocol " + NetShared.ProtocolVersion;
            spriteBatch.DrawString(Assets.fontDefault, Locale.Get("credit"), new Vector2(16, SDGame.Inst.GameHeight - 28), Color.Gray);
            spriteBatch.DrawString(Assets.fontDefault, versionstr, new Vector2(SDGame.Inst.GameWidth - Assets.fontDefault.MeasureString(versionstr).X - 16, SDGame.Inst.GameHeight - 28), Color.Gray);
        }

        /// <summary>
        /// Updates the position of the game title, making a smooth animation.
        /// </summary>
        /// <param name="deltaTime">The delta-time to interpolate with.</param>
        /// <param name="target">The target height in pixels.</param>
        public static void UpdateTitle(float deltaTime, float target) {
            logoY += (target - logoY) * 4f * deltaTime;
        }

        /// <summary>
        /// Returns a string identifying the game version.
        /// </summary>
        public static string GetVersionString() {
            Version v = Assembly.GetExecutingAssembly().GetName().Version;
            return $"v{v.Major}.{v.Minor}.{v.Revision}";
        }
        
        /// <summary>
        /// Returns the cross product of two 2D vectors, treating them as 3D vectors where the Z axis is zero for both vectors.
        /// </summary>
        public static float Cross(this Vector2 a, Vector2 b) {
            return a.X * b.Y - a.Y * b.X;
        }

#if DEBUG
        public static StringBuilder GetDebugInfo() {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Debug info panel - press F3 to close");
            sb.AppendLine();

            sb.AppendLine("-- SERVER --");
            if (NetServer.IsListening()) {
                sb.AppendLine($"Connections: {NetServer.GetPlayerCount()}");
                sb.AppendLine($"GameObjects: {NetServer.world.Objects.Count}");
                sb.AppendLine($"Next ID: {GameObject.GetNextId()}");
                sb.AppendLine($"Heartbeat: {NetServer.GetHeartbeatTime() * 1000f:F1} ms");
                sb.AppendLine($"Lua: {NetServer.GetLuaTop()} stack / {NetServer.GetLuaMemory():F1} kB");
                sb.AppendLine($"Events: {NetServer.GetEventCount()}");
            } else {
                sb.AppendLine("not running");
            }
            sb.AppendLine();

            sb.AppendLine("-- CLIENT --");
            if (NetClient.Connected) {
                sb.AppendLine($"Sim Running: {NetClient.SimRunning}");
                sb.AppendLine($"Players: {NetClient.PlayerCount}");
                sb.AppendLine($"Own Roles: {(int)NetClient.MyRoles}");
                sb.AppendLine($"Taken Roles: {(int)NetClient.TakenRoles}");
                sb.AppendLine($"Particles: {ParticleManager.GetCount()}");
                sb.AppendLine($"GameObjects: {NetClient.World.Objects.Count}");
            } else {
                sb.AppendLine("disconnected");
            }

            return sb;
        }
#endif

    }

}