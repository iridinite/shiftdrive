-- Player Ship - Light Cruiser
-- medium ship, slightly above average engines, below average offense
return {
  nameshort = GetRandomShipId(),
  namefull = localize("ship_plr_lcruiser"),
  
  topspeed = 20,
  turnrate = 30,
  hullmax = 100,
  shieldmax = 80,
  cargo = 4,
  
  mounts = {
    {
      position = {0, -20},
      bearing = 0,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {-10, 2},
      bearing = 355,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {10, 2},
      bearing = 5,
      arc = 100,
      size = Mount.SMALL
    }
  },
  weapons = {
    require("weapons/autocannon"),
    require("weapons/autocannon"),
    require("weapons/autocannon")
  },
  
  sprite = "map/player"
}