/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ShiftDrive {

    /// <summary>
    /// An INI-like settings file parser.
    /// </summary>
    internal sealed class SettingsFile {

        private readonly Dictionary<string, string> properties;
        private readonly string filename;

        /// <summary>
        /// Constructs a new SettingsFile by parsing a file from disk.
        /// </summary>
        /// <param name="filename">The path to the settings file to read.</param>
        public SettingsFile(string filename) {
            this.filename = filename;
            properties = new Dictionary<string, string>();

            if (!File.Exists(filename)) return;
            using (StreamReader reader = new StreamReader(filename, Encoding.UTF8)) {
                while (!reader.EndOfStream) {
                    // read a line, ignore comments, and split into key/value parts
                    string line = reader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                    string[] parts = line.Split(new[] {'='}, 2);
                    if (parts.Length != 2) continue;

                    // save key/value pair
                    string key = parts[0].Trim().ToUpperInvariant();
                    if (!properties.ContainsKey(key))
                        properties.Add(key, parts[1].Trim());
                }
            }
        }

        /// <summary>
        /// Saves this SettingsFile instance to disk, using the filename that was passed on creation.
        /// </summary>
        public void Save() {
            try {
                using (StreamWriter writer = new StreamWriter(filename, false, Encoding.UTF8)) {
                    foreach (var pair in properties) {
                        writer.WriteLine($"{pair.Key} = {pair.Value}");
                    }
                }
            } catch (IOException ex) {
                Logger.LogError($"Failed to write SettingsFile {filename}: {ex.Message}");
            }
        }

        /// <summary>
        /// Returns a string from the property table.
        /// </summary>
        /// <param name="key">The key to look up in the table. Not case-sensitive.</param>
        /// <param name="def">The default value to return if the key is missing.</param>
        public string GetString(string key, string def = null) {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            key = key.ToUpperInvariant();
            return properties.ContainsKey(key) ? properties[key] : def;
        }

        /// <summary>
        /// Stores a value in the property table.
        /// </summary>
        /// <param name="key">The key to save the value under. Not case-sensitive.</param>
        /// <param name="value">The value to save.</param>
        public void SetString(string key, string value) {
            key = key.ToUpperInvariant();
            if (properties.ContainsKey(key))
                properties[key] = value;
            else
                properties.Add(key, value);
        }

        /// <summary>
        /// Parses and returns an entry from the property table as an integer.
        /// </summary>
        /// <param name="key">The key to look up in the table. Not case-sensitive.</param>
        /// <param name="def">The default value to return if the key is missing, or the value cannot be parsed.</param>
        public int GetInt32(string key, int def = 0) {
            string prop = GetString(key);
            if (prop == null) return def;

            int outval;
            return Int32.TryParse(prop, NumberStyles.Integer, CultureInfo.InvariantCulture, out outval)
                ? outval
                : def;
        }

        /// <summary>
        /// Saves an integer in the property table.
        /// </summary>
        /// <param name="key">The key to save the value under. Not case-sensitive.</param>
        /// <param name="value">The value to save.</param>
        public void SetInt32(string key, int value) {
            SetString(key, value.ToString(CultureInfo.InvariantCulture));
        }


        /// <summary>
        /// Parses and returns an entry from the property table as a boolean.
        /// </summary>
        /// <param name="key">The key to look up in the table. Not case-sensitive.</param>
        /// <param name="def">The default value to return if the key is missing, or the value cannot be parsed.</param>
        public bool GetBool(string key, bool def = false) {
            string prop = GetString(key);
            if (prop == null) return def;

            return !prop.Equals("0", StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Saves an integer in the property table.
        /// </summary>
        /// <param name="key">The key to save the value under. Not case-sensitive.</param>
        /// <param name="value">The value to save.</param>
        public void SetBool(string key, bool value) {
            SetString(key, value ? "1" : "0");
        }

    }

}
