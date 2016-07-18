/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a shielded spaceship.
    /// </summary>
    internal abstract class Ship : NamedObject {

        public float hull;
        public float hullMax;
        public float shield;
        public float shieldMax;
        public bool shieldActive;

        public float topSpeed;
        public float turnRate;

        public float throttle;
        public float steering;

        public byte faction;

        protected Ship() {
            hull = 100f;
            hullMax = 100f;
            shield = 100f;
            shieldMax = 100f;
            shieldActive = false;
        }

        public override void Update(GameState world, float deltaTime) {
            // apply throttle velocity based on the ship's facing
            position += new Vector2(
                (float)Math.Cos(MathHelper.ToRadians(facing - 90f)),
                (float)Math.Sin(MathHelper.ToRadians(facing - 90f)))
                * throttle * topSpeed * deltaTime;
            position.X = MathHelper.Clamp(position.X, 0f, 1000f);
            position.Y = MathHelper.Clamp(position.Y, 0f, 1000f);
            // apply maneuver: find whether turning left or right is fastest
            // MathHelper.Clamp(Utils.Repeat(((steering - facing) < 360 - (steering - facing) ? steering - facing : facing - steering), 0f, 360f), -1f, 1f);
            float deltaFacing = MathHelper.Clamp(Utils.Repeat((steering - facing) + 180, 0f, 360f) - 180f, -1f, 1f);
            facing = Utils.Repeat(facing + deltaFacing * turnRate * deltaTime, 0f, 360f);
        }

        public override void TakeDamage(float damage) {
            // apply damage to shields first, if possible
            if (shieldActive && shield > 0f) {
                shield -= MathHelper.Clamp(shield - damage, 0f, shieldMax);
                return;
            }
            // otherwise, apply damage to hull
            hull = MathHelper.Clamp(hull - damage, 0f, hullMax);
            // zero hull = ship destruction
            if (hull <= 0f) Destroy();
        }

        public bool IsAlly(Ship other) {
            return faction == other.faction;
        }
        
        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);
            
            writer.Write(hull);
            writer.Write(hullMax);
            writer.Write(shield);
            writer.Write(shieldMax);
            writer.Write(shieldActive);

            writer.Write(topSpeed);
            writer.Write(turnRate);
            writer.Write(throttle);
            writer.Write(steering);

            writer.Write(faction);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            hull = reader.ReadSingle();
            hullMax = reader.ReadSingle();
            shield = reader.ReadSingle();
            shieldMax = reader.ReadSingle();
            shieldActive = reader.ReadBoolean();
            topSpeed = reader.ReadSingle();
            turnRate = reader.ReadSingle();
            throttle = reader.ReadSingle();
            steering = reader.ReadSingle();
            faction = reader.ReadByte();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "hull":
                    LuaAPI.lua_pushnumber(L, hull);
                    break;
                case "hullmax":
                    LuaAPI.lua_pushnumber(L, hullMax);
                    break;
                case "shield":
                    LuaAPI.lua_pushnumber(L, shield);
                    break;
                case "shieldmax":
                    LuaAPI.lua_pushnumber(L, shieldMax);
                    break;
                case "topspeed":
                    LuaAPI.lua_pushnumber(L, topSpeed);
                    break;
                case "turnrate":
                    LuaAPI.lua_pushnumber(L, turnRate);
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
                case "hull":
                    hull = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, hullMax);
                    break;
                case "hullmax":
                    hullMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    hull = MathHelper.Clamp(hull, 0f, hullMax);
                    break;
                case "shield":
                    shield = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, shieldMax);
                    break;
                case "shieldmax":
                    shieldMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    shield = MathHelper.Clamp(shield, 0f, shieldMax);
                    break;
                case "topspeed":
                    topSpeed = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                case "turnrate":
                    turnRate = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }
    }

    /// <summary>
    /// Represents a player-controlled ship.
    /// </summary>
    internal sealed class PlayerShip : Ship {

        public byte player;

        // Fuel reserves.
        // Integer part is fuel cell count, decimal part is reservoir.
        public float fuel;

        // the position the ship was at when it was destroyed.
        private bool destroyed;

        public PlayerShip() {
            type = ObjectType.PlayerShip;
            iconfile = "player";
            iconcolor = Color.Blue;
            bounding = 10f;
            destroyed = false;
        }

        public override void Update(GameState world, float deltaTime) {
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

    }

    /// <summary>
    /// Represents an AI-controlled ship.
    /// </summary>
    internal sealed class AIShip : Ship {
        
        public override void Update(GameState world, float deltaTime) {
            base.Update(world, deltaTime);
        }

    }

}