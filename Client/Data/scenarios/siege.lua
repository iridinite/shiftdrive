Create("blackhole").position = {350, 400}

local player = CreateShip(require("ships/mdfrigate"), "player")
player.position = {500, 500}
player.fuel = 11
player.faction = 1

local npc = CreateShip(require("ships/broadside"))
npc.position = {600, 500}
npc.faction = 2

Create("asteroid", {startpoint = {0, 0}, endpoint = {1000, 1000}, range = 100, count = 50})
