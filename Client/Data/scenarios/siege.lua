-- Siege gamemode
-- creates a fairly generic world with linear progression

local player = Create("player")
player.position = {400, 500}
player.topspeed = 35
player.turnrate = 40
player.iconname = "player"
player.iconcolor = 0xFFFF00FF

Create("asteroid", {startpoint = {100, 100}, endpoint = {900, 900}, range = 100, count = 100})

local bh = Create("blackhole")
repeat
  -- require black hole position to be a minimum safe distance away
  bh.position = {math.random(0, 1000), math.random(0, 1000)}
until math.dist(player.position, bh.position) > 200
--[[
-- test the Create function some more
for i = 1, 4 do
  local bhn = Create("blackhole")
  bhn.position = {i * 100, i * 50}
  bhn.iconcolor = 0xFF000080 + (0x0F0A0F * i)
  print(bhn.id)
end]]