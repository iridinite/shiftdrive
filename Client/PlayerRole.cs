/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {

    /// <summary>
    /// Represents the <seealso cref="Console"/> of a player on the ship.
    /// </summary>
    [Flags]
    internal enum PlayerRole {
        Helm = 1,
        Weapons = 2,
        Engineering = 4,
        Quartermaster = 8,
        Intelligence = 16
    }

}