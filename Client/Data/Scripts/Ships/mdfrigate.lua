return {
    NameShort = GetRandomShipId(),
    NameFull = localize("ship_mdfrigate"),

    TopSpeed = 30,
    TurnRate = 30,
    HullMax = 100,
    ShieldMax = 100,
    Bounding = 22,

    Sprite = "ship/mdfrigate",

    Mounts = {
        {
            position = {0, 0},
            bearing = 0,
            arc = 100,
            size = Mount.SMALL
        },
        {
            position = {10, 4},
            bearing = 0,
            arc = 100,
            size = Mount.SMALL
        },
        {
            position = {-10, 4},
            bearing = 0,
            arc = 100,
            size = Mount.SMALL
        }
    },
    Trails = {
        {-8.5, 18},
        {-10.5, 18},
        {8.5, 18},
        {10.5, 18}
    },
    Weapons = {
        require("weapons/plasmacannon"),
        require("weapons/autocannon"),
        require("weapons/autocannon")
    }
}
