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
  ent.flares = prototype.flares or {}
  
  return ent
end

function GetRandomShipId()
  -- take random characters from this list. 'I' and 'O' are excluded because
  -- they are easily mistaken for other characters
  local chartbl = {"A", "B", "C", "D", "E", "F", "G", "H", "J", "K",
    "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z"}
  return chartbl[math.random(#chartbl)] .. tostring(math.random(10, 99))
end

function vec2(x, y)
  -- generate an array with a metatable so components can be easily
  -- set using 'x' and 'y' meta-variables.
  -- beware that this function is called directly from C# (GameObject.LuaGet)
  return setmetatable({x or 0, y or x or 0}, {
      __index = function(t, k)
        if k == "x" then return rawget(t, 1) end
        if k == "y" then return rawget(t, 2) end
        return nil
      end,
      __newindex = function(t, k, v)
        if k == "x" then rawset(t, 1, v) end
        if k == "y" then rawset(t, 2, v) end
      end
    }
  )
end
