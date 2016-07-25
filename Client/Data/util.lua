function CreateShip(prototype, enttype)
  assert(type(prototype) == "table", "CreateShip expected a table")
  
  local ent = Create(enttype or "ship")
  ent.nameshort = prototype.nameshort or GetRandomShipId()
  ent.namefull = prototype.namefull or ""
  ent.desc = prototype.desc or ""
  ent.topspeed = prototype.topspeed or 25
  ent.turnrate = prototype.turnrate or 35
  ent.hullmax = prototype.hullmax or 1
  ent.hull = ent.hullmax
  ent.shieldmax = prototype.shieldmax or 1
  ent.shield = ent.shieldmax
  ent.sprite = prototype.sprite or "ship"
  ent.color = prototype.color or Color.White
  ent.bounding = prototype.bounding or 8
  
  ent.mounts = prototype.mounts or {}
  ent.weapons = prototype.weapons or {}
  
  return ent
end

function GetRandomShipId()
  -- take random characters from this list. 'I' and 'O' are excluded because
  -- they are easily mistaken for other characters
  local chartbl = {"A", "B", "C", "D", "E", "F", "G", "H", "J", "K",
    "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"}
  return chartbl[math.random(#chartbl)] .. tostring(math.random(10, 99))
end


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
