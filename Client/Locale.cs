/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// A language string table system. Wraps around <seealso cref="SettingsFile"/>.
    /// </summary>
    internal static class Locale {

        private static SettingsFile table;

        /// <summary>
        /// Loads a string table from a file.
        /// </summary>
        /// <param name="filePath">The path to the file that should be loaded.</param>
        public static void LoadStrings(string filePath) {
            try {
                table = new SettingsFile(filePath);
            } catch (IOException ex) {
                Logger.LogError($"Failed to read string table '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Looks up the specified key in the loaded string table. If the key is not found,
        /// the <paramref name="key"/> is returned unchanged.
        /// </summary>
        /// <param name="key">The key to use. Not case-sensitive.</param>
        public static string Get(string key) {
            return table.GetString(key, key);
        }

    }

}
