/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {
#if WINDOWS || LINUX
    public static class Program {

        [STAThread]
        public static void Main() {
            // handler for logging crashes
            AppDomain.CurrentDomain.UnhandledException += Program_UnhandledException;
            // run the game
            using (var game = new SDGame())
                game.Run();
        }

        private static void Program_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
            if (e.ExceptionObject is Exception ex)
                Logger.WriteExceptionReport(ex);
        }

    }
#endif
}