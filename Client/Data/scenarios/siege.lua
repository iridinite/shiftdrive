local player = CreateShip(require("ships/broadside"), "player")
player.position = {500, 500}
player.fuel = 11

Create("asteroid", {startpoint = {0, 0}, endpoint = {1000, 1000}, range = 100, count = 100})
Create("blackhole").position = {350, 400}