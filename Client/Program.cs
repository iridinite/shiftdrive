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
            using (var game = new SDGame())
                game.Run();
        }
    }
#endif
}