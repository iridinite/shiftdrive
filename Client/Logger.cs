/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Security.Permissions;

namespace ShiftDrive {

    /// <summary>
    /// Provides an interface for writing to an output log file.
    /// </summary>
    internal static class Logger {

        internal static readonly DirectoryInfo BaseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        private static StreamWriter Writer;

        static Logger() {
            Debug.Assert(HasWritePermission());
            Writer = new StreamWriter(BaseDir.FullName + "output.log");
        }

        /// <summary>
        /// Closes the logger and writes the log file to disk.
        /// </summary>
        public static void Close() {
            Writer?.Close();
            Writer?.Dispose();
            Writer = null;
        }

        /// <summary>
        /// Verifies if the calling thread has permission to write a file to the application directory.
        /// </summary>
        public static bool HasWritePermission() {
            PermissionSet permissionSet = new PermissionSet(PermissionState.None);
            FileIOPermission writePermission = new FileIOPermission(FileIOPermissionAccess.Write, BaseDir.FullName);
            permissionSet.AddPermission(writePermission);

            return permissionSet.IsSubsetOf(AppDomain.CurrentDomain.PermissionSet);
        }

        private static void LogHeader(string header) {
            Writer?.Write($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} [{header}] ");
        }

        /// <summary>
        /// Writes an informative message to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void Log(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("INFO");
            Writer?.WriteLine(message);
        }

        /// <summary>
        /// Writes a warning message to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void LogWarning(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("WARN");
            Writer?.WriteLine(message);
            SDGame.Inst.Print("WARN: " + message);
        }

        /// <summary>
        /// Writes an error report to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public static void LogError(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("ERROR");
            Writer?.WriteLine(message);
            SDGame.Inst.Print("ERROR: " + message, true);
        }

        /// <summary>
        /// Writes a text file to the application directory describing an <see cref="Exception"/>.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/> to describe.</param>
        public static void WriteExceptionReport(Exception ex) {
            if (!HasWritePermission()) return;

            try {
                using (StreamWriter exWriter = new StreamWriter(
                    $"{BaseDir.FullName}crash{DateTime.Now.ToFileTime()}.log")) {
                    exWriter.WriteLine("===================================");
                    exWriter.WriteLine("EXCEPTION REPORT");
                    exWriter.WriteLine("===================================");
                    exWriter.WriteLine();

                    exWriter.WriteLine("ShiftDrive Client " + Utils.GetVersionString());
                    exWriter.WriteLine($"Time: {DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}");
                    exWriter.WriteLine("OS: " + Environment.OSVersion);
                    exWriter.WriteLine("Target Site: " + ex.TargetSite);
                    exWriter.WriteLine();

                    exWriter.WriteLine(ex);

                    exWriter.WriteLine();
                    exWriter.WriteLine("===================================");
                    exWriter.WriteLine("END OF EXCEPTION REPORT");
                    exWriter.WriteLine("===================================");
                }
            } catch (Exception) {
                // we're already failing if this method is called at all,
                // no point in throwing more exceptions if logging fails.
            }
        }

    }

}
