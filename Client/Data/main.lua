require("extensions")
require("util")

DamageType = {
  THERMAL = 0,
  KINETIC = 1,
  EXPLOSIVE = 2
}

AmmoType = {
  NONE = 0,
  MISSILE = 1,
  EMP = 2,
  MINE = 3
}

math.randomseed(now())
