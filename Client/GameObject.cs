/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;

namespace ShiftDrive {
    
    internal enum ObjectType {
        Generic,
        Asteroid,
        Mine,
        Nebula,
        AIShip,
        PlayerShip,
        Station,
        BlackHole,
        Projectile,
        Particle
    }
    
    /// <summary>
    /// Represents a serializable object in the game world.
    /// </summary>
    internal abstract class GameObject {
        public uint id;
        public ObjectType type;

        public Vector2 position;
        public Vector2 velocity;
        public float facing;
        public byte sector;

        public string spritename;
        public SpriteSheet sprite;
        public Color color;

        public float damping;
        public float bounding;
        public CollisionLayer layer;
        public CollisionLayer layermask;

        public bool changed;
        protected readonly GameState world;

        private readonly lua_CFunction
            refLuaGet,
            refLuaSet,
            refLuaTakeDamage,
            refLuaDestroy,
            refLuaIsShip,
            refLuaIsTerrain;

        private bool destroyScheduled;
        private static uint nextId;

        protected GameObject(GameState world) {
            this.world = world;
            id = ++nextId;
            refLuaGet = LuaGet;
            refLuaSet = LuaSet;
            refLuaTakeDamage = clua_TakeDamage;
            refLuaDestroy = clua_Destroy;
            refLuaIsShip = clua_IsShip;
            refLuaIsTerrain = clua_IsTerrain;
            changed = true;
            color = Color.White;
            damping = 1.0f;
            layer = CollisionLayer.Default;
            layermask = CollisionLayer.All;
        }

        /// <summary>
        /// Updates this object.
        /// </summary>
        /// <param name="deltaTime">The number of seconds that passed since the previous update.</param>
        public virtual void Update(float deltaTime) {
            // handle collision with objects
            if (bounding > 0f) {
                var possibleCollisions = world.QueryGrid(this);
                foreach (GameObject obj in possibleCollisions) {
                    if (obj.id == this.id)
                        continue; // don't collide with self
                    if (obj.bounding <= 0f)
                        continue; // don't collide with objects with no bounding sphere
                    if ((obj.layer & this.layermask) == 0)
                        continue; // do we collide with the layer the other object is on?

                    float dist = Vector2.DistanceSquared(obj.position, this.position);
                    float totalbound = obj.bounding + this.bounding;
                    if (dist > totalbound * totalbound)
                        continue; // bounding sphere check

                    // calculate contact information
                    Vector2 normal = Vector2.Normalize(this.position - obj.position);
                    float penetration = this.bounding + obj.bounding - (float)Math.Sqrt(dist);
                    // handle collision response
                    OnCollision(obj, normal, penetration);
                }
            }

            // integrate velocity into position
            position += velocity * deltaTime;
            velocity *= (float)Math.Pow(damping, deltaTime);

            // animate the object sprite
            if (!world.IsServer) sprite.Update(deltaTime);

            // re-transmit object if it's moving around
            changed = changed || velocity.LengthSquared() > 0.01f;
        }
        
        /// <summary>
        /// Applies damage to this object. The derived class decides how
        /// damage affects the object, if at all.
        /// </summary>
        /// <param name="damage">The damage total to apply.</param>
        public virtual void TakeDamage(float damage) {
            // breakable objects should override this
        }

        /// <summary>
        /// Handle a collision with another <see cref="GameObject"/>.
        /// </summary>
        protected virtual void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // apply minimum translation vector to resolve collision
            this.position += normal * penetration;
            this.velocity += normal * penetration;
        }

        /// <summary>
        /// Specifies whether this object counts as terrain.
        /// Terrain objects are only sent once to clients, saving bandwidth.
        /// </summary>
        public virtual bool IsTerrain() {
            return false;
        }

        /// <summary>
        /// Schedules this object for deletion. The server will erase the object
        /// once the update cycle has been completed.
        /// </summary>
        public virtual void Destroy() {
            if (!world.IsServer) return;
            destroyScheduled = true;
        }

        /// <summary>
        /// Returns a value indicating whether this object is scheduled for deletion.
        /// </summary>
        public bool ShouldDestroy() {
            return destroyScheduled;
        }

        /// <summary>
        /// Returns true if this GameObject is a PlayerShip or AIShip.
        /// </summary>
        public bool IsShip() {
            return type == ObjectType.AIShip || type == ObjectType.PlayerShip;
        }

        /// <summary>
        /// Pushes a table to the Lua stack that represents this object.
        /// </summary>
        /// <param name="L">A pointer to the Lua state.</param>
        public void PushToLua(IntPtr L) {
            // push a dummy table with a metatable behind it
            LuaAPI.lua_createtable(L, 0, 0);
            LuaAPI.lua_createtable(L, 0, 2);
            LuaAPI.lua_pushcclosure(L, refLuaGet, 0);
            LuaAPI.lua_setfield(L, -2, "__index");
            LuaAPI.lua_pushcclosure(L, refLuaSet, 0);
            LuaAPI.lua_setfield(L, -2, "__newindex");
            LuaAPI.lua_setmetatable(L, -2);
        }

        /// <summary>
        /// Implementation of the __index Lua metamethod.
        /// </summary>
        /// <param name="L">A pointer to the Lua state.</param>
        /// <returns>The number of objects pushed on the stack.</returns>
        protected virtual int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                // -- PROPERTIES --
                case "id":
                    LuaAPI.lua_pushnumber(L, id);
                    break;
                case "position":
                    // position is a two-element table {x, y}
                    LuaAPI.lua_createtable(L, 2, 0);
                    LuaAPI.lua_pushnumber(L, position.X);
                    LuaAPI.lua_rawseti(L, -2, 1);
                    LuaAPI.lua_pushnumber(L, position.Y);
                    LuaAPI.lua_rawseti(L, -2, 2);
                    break;
                case "facing":
                    LuaAPI.lua_pushnumber(L, facing);
                    break;
                case "bounding":
                    LuaAPI.lua_pushnumber(L, bounding);
                    break;
                case "sprite":
                    LuaAPI.lua_pushstring(L, spritename);
                    break;
                case "color":
                    LuaAPI.lua_pushnumber(L, color.PackedValue);
                    break;

                // -- METHODS --
                case "TakeDamage":
                    LuaAPI.lua_pushcclosure(L, refLuaTakeDamage, 0);
                    break;
                case "Destroy":
                    LuaAPI.lua_pushcclosure(L, refLuaDestroy, 0);
                    break;
                case "IsTerrain":
                    LuaAPI.lua_pushcclosure(L, refLuaIsTerrain, 0);
                    break;
                case "IsShip":
                    LuaAPI.lua_pushcclosure(L, refLuaIsShip, 0);
                    break;

                default:
                    LuaAPI.lua_pushstring(L, "attempt to read unknown field '" + key + "'");
                    LuaAPI.lua_error(L);
                    break;
            }
            return 1;
        }

        /// <summary>
        /// Implementation of the __newindex Lua metamethod.
        /// </summary>
        /// <param name="L">A pointer to the Lua state.</param>
        /// <returns>The number of objects pushed on the stack.</returns>
        protected virtual int LuaSet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "position":
                    // position is a two-element table {x, y}
                    position = LuaAPI.lua_tovec2(L, 3);
                    world.ReinsertGrid(this);
                    break;
                case "facing":
                    facing = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                case "bounding":
                    bounding = (float)LuaAPI.luaL_checknumber(L, 3);
                    world.ReinsertGrid(this);
                    break;
                case "sprite":
                    spritename = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "color":
                    color.PackedValue = (uint)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    LuaAPI.lua_pushstring(L, "attempt to modify unknown field '" + key + "'");
                    LuaAPI.lua_error(L);
                    break;
            }
            changed = true;
            return 0;
        }

        private int clua_TakeDamage(IntPtr L) {
            TakeDamage((int)LuaAPI.luaL_checknumber(L, 2));
            return 0;
        }

        private int clua_Destroy(IntPtr L) {
            Destroy();
            return 0;
        }

        private int clua_IsShip(IntPtr L) {
            LuaAPI.lua_pushboolean(L, IsShip() ? 1 : 0);
            return 1;
        }

        private int clua_IsTerrain(IntPtr L) {
            LuaAPI.lua_pushboolean(L, IsTerrain() ? 1 : 0);
            return 1;
        }

        /// <summary>
        /// Serializes the object as a byte array.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Serialize(BinaryWriter writer) {
            writer.Write((byte)type);
            writer.Write(position.X);
            writer.Write(position.Y);
            writer.Write(velocity.X);
            writer.Write(velocity.Y);
            writer.Write(facing);
            writer.Write((byte)sector);
            writer.Write(bounding);
            writer.Write(damping);

            writer.Write(spritename);
            writer.Write(color.PackedValue);
        }

        /// <summary>
        /// Deserializes the object, reading a byte array from the stream as written by Serialize.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(BinaryReader reader) {
            type = (ObjectType)reader.ReadByte();
            position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            velocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            facing = reader.ReadSingle();
            sector = reader.ReadByte();
            bounding = reader.ReadSingle();
            damping = reader.ReadSingle();

            string oldsprite = spritename;
            spritename = reader.ReadString();
            if (sprite == null || !spritename.Equals(oldsprite, StringComparison.InvariantCulture))
                sprite = Assets.GetSprite(spritename).Clone();

            color.PackedValue = reader.ReadUInt32();
        }

        /// <summary>
        /// Resets the sequence of object ID numbers.
        /// </summary>
        public static void ResetIds() {
            nextId = 0U;
        }

    }
    
}
