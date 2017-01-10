/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {

    [Flags]
    internal enum CollisionLayer : uint {
        None =          0,
        Default =       1 << 0,
        Ship =          1 << 1,
        Asteroid =      1 << 2,
        Projectile =    1 << 3,
        Station =       1 << 4,

        All =           unchecked((uint)-1)
    }

}
