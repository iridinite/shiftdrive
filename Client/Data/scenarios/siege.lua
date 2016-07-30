-- Siege gamemode
-- creates a fairly generic world with linear progression

local player = CreateShip(require("ships/player-lcruiser"), "player")
player.position = {400, 500}
player.fuel = 5.4

--[[local npc = CreateShip(require("ships/bulkcargo"))
npc.position = {400, 450}
]]
Create("asteroid", {startpoint = {0, 0}, endpoint = {1000, 1000}, range = 200, count = 300})
--[[
for i = 1, 5 do
  local bh = Create("blackhole")
  repeat
    -- require black hole position to be a minimum safe distance away
    bh.position = {math.random(0, 1000), math.random(0, 1000)}
  until math.dist(player.position, bh.position) > 200
end
]]