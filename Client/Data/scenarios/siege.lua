-- Siege gamemode
-- creates a fairly generic world with linear progression

local player = CreateShip(require("ships/player-lcruiser"), "player")
player.position = {400, 500}
player.fuel = 5.4

--local npc = CreateShip(require("ships/bulkcargo"))
--npc.facing = 105
--npc.position = {350, 450}

Create("asteroid", {startpoint = {100, 100}, endpoint = {900, 900}, range = 100, count = 100})

Create("asteroid", {startpoint = {400, 400}, endpoint = {600, 600}, range = 0, count = 10})

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