/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;

namespace ShiftDrive {
#if WINDOWS || LINUX
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
#if !DEBUG
            try {
#endif
            using (var game = new SDGame())
                game.Run();
#if !DEBUG
            } catch (Exception ex) {
                Logger.WriteExceptionReport(ex);
            }
#endif
        }
    }
#endif
}