return {
  nameshort = GetRandomShipId(),
  namefull = localize("ship_broadside"),
  
  topspeed = 20,
  turnrate = 30,
  hullmax = 100,
  shieldmax = 80,
  cargo = 4,
  bounding = 30,
  
  sprite = "ship/broadside",
  
  mounts = {
    {
      position = {10, -8},
      bearing = 90,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {10, -3},
      bearing = 90,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {10, 2},
      bearing = 90,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {10, 7},
      bearing = 90,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {-10, -8},
      bearing = 270,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {-10, -3},
      bearing = 270,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {-10, 2},
      bearing = 270,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {-10, 7},
      bearing = 270,
      arc = 100,
      size = Mount.SMALL
    }
  },
  weapons = {
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