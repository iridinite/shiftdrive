Create("blackhole").Position = {350, 400}

local player = Create("player")
ApplyTemplate(player, require("ships/mdfrigate"))
player.Position = {500, 500}
player.Fuel = 11
player.Faction = 1

local npc = Create("ship")
ApplyTemplate(npc, require("ships/mdfrigate"))
npc.Position = {600, 500}
npc.Faction = 2

local asdfasdf = Create("station")
asdfasdf.Position = {700, 600}
asdfasdf.Sprite = "ship/station"

Create("asteroid", {startpoint = {0, 0}, endpoint = {1000, 1000}, range = 100, count = 50})
