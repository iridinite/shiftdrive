/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

namespace ShiftDrive {

    /// <summary>
    /// Represents a task for an AI-controlled ship
    /// </summary>
    internal enum AITask {
        Invalid = 0,
        TravelDestination = 1,
        TravelRandomize = 2,
        ChaseEnemy = 3,
        ChaseStation = 4,
        ChaseAnger = 5,
        AvoidMine = 6,
        AvoidBlackHole = 7
    }

}
