/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Manages the storage of user configuration settings.
    /// </summary>
    internal sealed class Config {
        
        private const string cfgFileName = "config.dat";
        private const byte cfgVersion = 1;

        public ushort ResolutionW { get; set; } = 1280;
        public ushort ResolutionH { get; set; } = 720;
        public bool FullScreen { get; set; } = false;

        public byte VolumeSound { get; set; } = 10;
        public byte VolumeMusic { get; set; } = 8;

        public static Config Inst { get; internal set; }

        public static Config Load() {
            if (!Logger.HasWritePermission())
                SDGame.Logger.LogError("Warning: ShiftDrive does not have permission to save files to the app directory.\nAny settings you change will be lost.");

#if DEBUG
            // if debugging, we probably always want windowed mode etc.
            return new Config();
#else
            if (!File.Exists(Logger.BaseDir.FullName + cfgFileName))
                return new Config();

            try {
                Config cfg = new Config();
                using (FileStream stream = new FileStream(Logger.BaseDir.FullName + cfgFileName, FileMode.Open)) {
                    using (BinaryReader reader = new BinaryReader(stream)) {
                        if (reader.ReadByte() != cfgVersion) throw new InvalidDataException("Invalid config file version");

                        cfg.ResolutionW = reader.ReadUInt16();
                        cfg.ResolutionH = reader.ReadUInt16();
                        cfg.FullScreen = reader.ReadBoolean();
                        cfg.VolumeSound = reader.ReadByte();
                        cfg.VolumeMusic = reader.ReadByte();
                    }
                }
                return cfg;

            } catch (Exception ex) {
                SDGame.Logger.LogError("Failed to read config: " + ex);
                return new Config();
            }
#endif
        }

        public void Save() {
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
                SDGame.Logger.LogError("Failed to write config: " + ex);
            }
        }

    }

}
