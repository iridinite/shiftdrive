function CreateShip(prototype, enttype)
  assert(type(prototype) == "table", "CreateShip expected a table")
  
  local ent = Create(enttype or "ship")
  ent.nameshort = prototype.nameshort or ""
  ent.namefull = prototype.namefull or ""
  ent.desc = prototype.desc or ""
  ent.topspeed = prototype.topspeed or 25
  ent.turnrate = prototype.turnrate or 35
  ent.hullmax = prototype.hullmax or 1
  ent.hull = ent.hullmax
  ent.shieldmax = prototype.shieldmax or 1
  ent.shield = ent.shieldmax
  ent.iconname = prototype.iconname or "ship"
  ent.iconcolor = prototype.iconcolor or 0xFFFFFFFF
  ent.bounding = prototype.bounding or 8
  return ent
end

function GetRandomShipId()
  -- take random characters from this list. 'I' and 'O' are excluded because
  -- they are easily mistaken for other characters
  local chartbl = {"A", "B", "C", "D", "E", "F", "G", "H", "J", "K",
    "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"}
  return chartbl[math.random(#chartbl)] .. tostring(math.random(10, 99))
end