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
  
  weaponslots = 3,
  weapons = {
    require("weapons/burstlaser"),
    require("weapons/burstlaser")
  },
  
  iconname = "player",
  iconcolor = Color(0, 144, 255)
}