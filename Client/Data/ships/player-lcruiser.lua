-- Player Ship - Light Cruiser
-- medium ship, slightly above average engines, below average offense
return {
  nameshort = GetRandomShipId(),
  namefull = localize("ship_plr_lcruiser"),
  
  topspeed = 35,
  turnrate = 35,
  hullmax = 100,
  shieldmax = 80,
  cargo = 4,
  
  mounts = {
    {
      position = {-5, 0},
      bearing = 290,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {5, 0},
      bearing = 70,
      arc = 100,
      size = Mount.SMALL
    },
    {
      position = {0, -5},
      bearing = 0,
      arc = 100,
      size = Mount.MEDIUM
    }
  },
  weapons = {
    require("weapons/burstlaser"),
    require("weapons/burstlaser"),
    require("weapons/autocannon")
  },
  
  iconname = "player",
  iconcolor = Color(0, 144, 255)
}