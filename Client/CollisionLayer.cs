/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;

namespace ShiftDrive {
    
    [Flags]
    internal enum CollisionLayer {
        None = 0,
        Default = 1,
        Ship = 2,
        Asteroid = 4,
        Projectile = 8,
        Station = 16,

        All = 2147483647
    }

}
