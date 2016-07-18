/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;

namespace ShiftDrive {

    [Flags]
    internal enum PlayerRole {
        Helm = 1,
        Weapons = 2,
        Engineering = 4,
        Science = 8,
        Comms = 16
    }

}