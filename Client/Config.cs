/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework.Audio;

namespace ShiftDrive {

    /// <summary>
    /// Manages the storage of user configuration settings.
    /// </summary>
    internal static class Config {

        private const string cfgFileName = "config.dat";
        private const byte cfgVersion = 1;

        private static byte _volumeSound = 10;

        public static ushort ResolutionW { get; set; } = 1280;
        public static ushort ResolutionH { get; set; } = 720;
        public static bool FullScreen { get; set; } = false;

        public static byte VolumeSound {
            get { return _volumeSound; }
            set {
                _volumeSound = value;
                SoundEffect.MasterVolume = value / 10f;
            }
        }

        public static byte VolumeMusic { get; set; } = 8;

        public static string ServerIP { get; set; } = "localhost";
        public static ushort ServerPort { get; set; } = 7777;
        
        public static void Load() {
            if (!Logger.HasWritePermission())
                Logger.LogWarning("ShiftDrive does not have permission to save files to the app directory.\nAny settings you change will be lost.");

#if DEBUG
            // if debugging, we probably always want windowed mode etc.
#else
            if (!File.Exists(Logger.BaseDir.FullName + cfgFileName))
                return;

            try {
                using (FileStream stream = new FileStream(Logger.BaseDir.FullName + cfgFileName, FileMode.Open)) {
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        if (reader.ReadByte() != cfgVersion) throw new InvalidDataException("Invalid config file version");

                        ResolutionW = reader.ReadUInt16();
                        ResolutionH = reader.ReadUInt16();
                        FullScreen = reader.ReadBoolean();
                        VolumeSound = reader.ReadByte();
                        VolumeMusic = reader.ReadByte();
                    }
                }

            } catch (Exception ex) {
                Logger.LogError("Failed to read config: " + ex);
            }
#endif
        }

        public static void Save() {
            if (!Logger.HasWritePermission()) return;

            try {
                using (FileStream stream = new FileStream(Logger.BaseDir.FullName + cfgFileName, FileMode.Create)) {
                    using (BinaryWriter writer = new BinaryWriter(stream)) {
                        writer.Write(cfgVersion);
                        writer.Write(ResolutionW);
                        writer.Write(ResolutionH);
                        writer.Write(FullScreen);
                        writer.Write(VolumeSound);
                        writer.Write(VolumeMusic);
                    }
                }

            } catch (Exception ex) {
                Logger.LogError("Failed to write config: " + ex);
            }
        }
        
    }

}
