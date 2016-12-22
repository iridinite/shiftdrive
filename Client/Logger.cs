/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
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
    internal sealed class Logger : IDisposable {

        internal static readonly DirectoryInfo BaseDir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        private StreamWriter Writer;

        private bool disposedValue = false; // To detect redundant calls

        public Logger() {
            Debug.Assert(HasWritePermission());
            Writer = new StreamWriter(BaseDir.FullName + "output.log");
        }

        ~Logger() {
            Dispose(false);
        }

        private void Dispose(bool disposing) {
            if (disposedValue) return;
            disposedValue = true;

            Writer?.Close();
            Writer?.Dispose();
            Writer = null;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        private void LogHeader(string header) {
            Writer?.Write($"{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()} [{header}] ");
        }

        /// <summary>
        /// Writes an informative message to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void Log(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("INFO");
            Writer?.WriteLine(message);
        }

        /// <summary>
        /// Writes a warning message to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void LogWarning(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("WARN");
            Writer?.WriteLine(message);
#if DEBUG
            SDGame.Inst.Print(message);
#endif
        }

        /// <summary>
        /// Writes an error report to the output log.
        /// </summary>
        /// <param name="message">The message to write.</param>
        public void LogError(string message) {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));

            LogHeader("ERROR");
            Writer?.WriteLine(message);
            SDGame.Inst.Print(message, true);
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
