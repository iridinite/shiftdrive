return {
    NameShort = GetRandomShipId(),
    NameFull = localize("ship_broadside"),

    TopSpeed = 20,
    TurnRate = 30,
    HullMax = 100,
    ShieldMax = 80,
    Bounding = 30,

    Sprite = "ship/broadside",

    Mounts = {
        {
            position = {10, -8},
            bearing = 90,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {10, -3},
            bearing = 90,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {10, 2},
            bearing = 90,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {10, 7},
            bearing = 90,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {-10, -8},
            bearing = 270,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {-10, -3},
            bearing = 270,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {-10, 2},
            bearing = 270,
            arc = 50,
            size = Mount.SMALL
        },
        {
            position = {-10, 7},
            bearing = 270,
            arc = 50,
            size = Mount.SMALL
        }
    },
    Weapons = {
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon")
    }
}
