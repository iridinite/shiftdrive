return {
  nameshort = GetRandomShipId(),
  namefull = localize("ship_mdfrigate"),
  
  topspeed = 30,
  turnrate = 30,
  hullmax = 100,
  shieldmax = 100,
  cargo = 2,
  bounding = 22,
  
  sprite = "ship/mdfrigate",
  
  mounts = {
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
  flares = {
    {-8.5, 18},
    {-10.5, 18},
    {8.5, 18},
    {10.5, 18}
  },
  weapons = {
    require("weapons/plasmacannon"),
    require("weapons/autocannon"),
    require("weapons/autocannon")
  }
}