﻿/*
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

        private bool needRetransmit;

        protected Ship() {
            needRetransmit = true;
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

            changed = changed || throttle > 0f || Math.Abs(deltaFacing) > 0.001f;
        }

        public override void TakeDamage(float damage) {
            // always retransmit
            changed = true;
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
            writer.Write(shield);
            writer.Write(shieldMax);
            writer.Write(shieldActive);

            writer.Write(throttle);
            writer.Write(steering);

            writer.Write(needRetransmit);
            if (!needRetransmit) return;
            needRetransmit = false;

            writer.Write(hullMax);
            writer.Write(shieldMax);
            writer.Write(topSpeed);
            writer.Write(turnRate);
            writer.Write(faction);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            hull = reader.ReadSingle();
            shield = reader.ReadSingle();
            shieldActive = reader.ReadBoolean();

            throttle = reader.ReadSingle();
            steering = reader.ReadSingle();

            if (!reader.ReadBoolean()) return;

            hullMax = reader.ReadSingle();
            shieldMax = reader.ReadSingle();
            topSpeed = reader.ReadSingle();
            turnRate = reader.ReadSingle();
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
                    needRetransmit = true;
                    break;
                case "shield":
                    shield = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, shieldMax);
                    break;
                case "shieldmax":
                    shieldMax = MathHelper.Clamp((float)LuaAPI.luaL_checknumber(L, 3), 0f, 9999f);
                    shield = MathHelper.Clamp(shield, 0f, shieldMax);
                    needRetransmit = true;
                    break;
                case "topspeed":
                    topSpeed = (float)LuaAPI.luaL_checknumber(L, 3);
                    needRetransmit = true;
                    break;
                case "turnrate":
                    turnRate = (float)LuaAPI.luaL_checknumber(L, 3);
                    needRetransmit = true;
                    break;
                default:
                    return base.LuaSet(L);
            }
            changed = true;
            return 0;
        }
    }

}