/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a player-controlled ship.
    /// </summary>
    internal sealed class PlayerShip : Ship {

        public byte player;

        // Fuel reserves.
        // Integer part is fuel cell count, decimal part is reservoir.
        public float fuel;
        
        private bool destroyed;

        public PlayerShip() {
            type = ObjectType.PlayerShip;
            bounding = 10f;
            destroyed = false;
        }

        public override void Update(GameState world, float deltaTime) {
            // ship operation and throttle eats up energy
            ConsumeFuel(deltaTime * 0.004f);
            ConsumeFuel(throttle * deltaTime * 0.0083333f);

            // cannot move if destroyed
            if (!destroyed) {
                // apply throttle and steering
                base.Update(world, deltaTime);
            }
        }

        public override void Destroy() {
            // override because we do not want player ships to be scheduled for deletion,
            // that would cause null ref exceptions on the clients.
            destroyed = true;
            hull = 0f;
            throttle = 0f;
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);

            writer.Write(destroyed);
            writer.Write(player);
            writer.Write(fuel);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            destroyed = reader.ReadBoolean();
            player = reader.ReadByte();
            fuel = reader.ReadSingle();
        }

        public void ConsumeFuel(float amount) {
            fuel -= amount;
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "fuel":
                    LuaAPI.lua_pushnumber(L, fuel);
                    break;
                default:
                    return base.LuaGet(L);
            }
            return 1;
        }

        protected override int LuaSet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "fuel":
                    fuel = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}