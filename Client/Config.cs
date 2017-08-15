/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Manages the storage of user configuration settings.
    /// </summary>
    internal static class Config {

        private const string CONFIG_FILE_NAME = "settings.ini";
        private static SettingsFile store;

        public static List<Tuple<int, int>> SupportedResolutions { get; } = new List<Tuple<int, int>>();

        public static int ResolutionW { get; set; }
        public static int ResolutionH { get; set; }
        public static bool FullScreen { get; set; }

        public static int VolumeSound { get; set; }
        public static int VolumeMusic { get; set; }

        public static string ServerIP { get; set; }
        public static int ServerPort { get; set; }
        
        public static void Load() {
            if (!Logger.HasWritePermission())
                Logger.LogWarning("ShiftDrive does not have permission to save files to your 'My Games' folder. Any settings you change will be lost.");

            // load settings file from disk
            store = new SettingsFile(Logger.BaseDir.FullName + Path.DirectorySeparatorChar + CONFIG_FILE_NAME);

            // copy the settings into the static properties
            ResolutionW = store.GetInt32("resolutionWidth");
            ResolutionH = store.GetInt32("resolutionHeight");
            FullScreen = store.GetBool("fullscreen", true);

            VolumeSound = store.GetInt32("volumeSound", 100);
            VolumeMusic = store.GetInt32("volumeMusic", 80);

            ServerIP = store.GetString("serverIP", "localhost");
            ServerPort = store.GetInt32("serverPort", 7777);
        }

        public static void Save() {
            if (!Logger.HasWritePermission()) return;
            store.Save();
        }
        
    }

}
