/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ShiftDrive {

    /// <summary>
    /// A look-up system for key/value pairs with swappable value tables, allowing for
    /// easy translation and localization of strings.
    /// </summary>
    /// <remarks>
    /// Language files must follow the following format:
    /// - Files should be in UTF-8 plain text format,
    /// - One line may contain only one key/value pair,
    /// - An equals-sign (=) seperates key from value, in that order,
    /// - Leading and trailing whitespace is ignored/trimmed,
    /// - Keys are case-insensitive,
    /// - Comments are introduced by a number sign (#),
    /// - Empty lines or otherwise invalid lines are ignored.
    /// </remarks>
    internal static class Locale {

        private static readonly Dictionary<string, string> stringTable = new Dictionary<string, string>();

        /// <summary>
        /// Loads a string table from a file.
        /// </summary>
        /// <remarks>
        /// Any previously loaded string table will be preserved.
        /// If an exception is thrown, loading will be canceled. Entries that have already
        /// been loaded before the exception occurred will be preserved.
        /// Language files must follow the file format as specified in the class remarks.
        /// </remarks>
        /// <param name="filePath">The path to the file that should be loaded.</param>
        public static void LoadStrings(string filePath) {
            try {
                using (StreamReader reader = new StreamReader(filePath, Encoding.UTF8)) {
                    while (!reader.EndOfStream) {
                        string line = reader.ReadLine();
                        // ReSharper disable once PossibleNullReferenceException
                        if (line.StartsWith("#", StringComparison.InvariantCultureIgnoreCase)) continue;
                        if (line.Trim().Length < 1) continue;

                        string[] parts = line.Split(new[] { '=' }, 2);
                        if (parts.Length != 2) continue;

                        stringTable.Add(parts[0].Trim().ToUpperInvariant(), parts[1].Trim());
                    }
                }
            } catch (IOException ex) {
                SDGame.Logger.LogError($"Failed to read string table '{filePath}': {ex.Message}");
            }
        }

        /// <summary>
        /// Looks up the specified key in the loaded string table(s).
        /// </summary>
        /// <param name="key">The key to use. Not case-sensitive.</param>
        /// <returns>The string value associated with the specified key. If the key does not exist,
        /// the key is returned converted to uppercase.</returns>
        public static string Get(string key) {
            if (String.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            key = key.ToUpperInvariant();
            return stringTable.ContainsKey(key) ? stringTable[key] : key;
        }

    }

}
