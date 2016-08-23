WeaponType = {
  BEAM = 0,
  PROJECTILE = 1
}

DamageType = {
  THERMAL = 0,
  KINETIC = 1,
  EXPLOSIVE = 2
}

AmmoType = {
  NONE = 0,
  MISSILE = 1,
  EMP = 2,
  MINE = 3,
  BULLET = 4
}

Mount = {
  SMALL = 1,
  MEDIUM = 2,
  LARGE = 3
}

-- Color table: helper for constructing packed XNA color structs
Color = {}
local mt = {__call = function(self, r, g, b, a)
    return lshift(a or 255, 24) + lshift(b, 16) + lshift(g, 8) + r
end}
setmetatable(Color, mt)

-- define a few common colors
Color.White = Color(255, 255, 255)
Color.Black = Color(0, 0, 0)
Color.Red = Color(255, 0, 0)
Color.Blue = Color(0, 0, 255)
Color.Green = Color(0, 200, 0)
