/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
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
        BlackHole
    }
    
    /// <summary>
    /// Represents a serializable object in the game world.
    /// </summary>
    internal abstract class GameObject {
        public ushort id;
        public ObjectType type;

        public Vector2 position;
        public float facing;
        public int sector;
        
        public string iconfile;
        public Color iconcolor;

        public float bounding;

        public bool changed;

        private readonly lua_CFunction refLuaGet, refLuaSet;
        private bool destroyScheduled;
        private static ushort nextId;

        protected GameObject() {
            id = ++nextId;
            refLuaGet = LuaGet;
            refLuaSet = LuaSet;
            changed = true;
        }

        /// <summary>
        /// Updates this object.
        /// </summary>
        /// <param name="world">A reference to the <see cref="GameState"/> that this object is in.</param>
        /// <param name="deltaTime">The number of seconds that passed since the previous update.</param>
        public abstract void Update(GameState world, float deltaTime);

        /// <summary>
        /// Applies damage to this object. The derived class decides how
        /// damage affects the object, if at all.
        /// </summary>
        /// <param name="damage">The damage total to apply.</param>
        public virtual void TakeDamage(float damage) {
            // breakable objects should override this
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
            destroyScheduled = true;
        }

        /// <summary>
        /// Returns a value indicating whether this object is scheduled for deletion.
        /// </summary>
        public bool ShouldDestroy() {
            return destroyScheduled;
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
                case "iconname":
                    LuaAPI.lua_pushstring(L, iconfile);
                    break;
                case "iconcolor":
                    LuaAPI.lua_pushnumber(L, iconcolor.PackedValue);
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
                    break;
                case "facing":
                    facing = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                case "bounding":
                    bounding = (float)LuaAPI.luaL_checknumber(L, 3);
                    break;
                case "iconname":
                    iconfile = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "iconcolor":
                    iconcolor.PackedValue = (uint)LuaAPI.luaL_checknumber(L, 3);
                    break;
                default:
                    LuaAPI.lua_pushstring(L, "attempt to modify unknown field '" + key + "'");
                    LuaAPI.lua_error(L);
                    break;
            }
            changed = true;
            return 0;
        }
        
        /// <summary>
        /// Serializes the object as a byte array.
        /// </summary>
        /// <param name="writer"></param>
        public virtual void Serialize(BinaryWriter writer) {
            writer.Write(id);
            writer.Write((byte)type);
            writer.Write(position.X);
            writer.Write(position.Y);
            writer.Write(facing);
            writer.Write((byte)sector);
            
            writer.Write(iconfile);
            writer.Write(iconcolor.PackedValue);

            writer.Write(bounding);
        }

        /// <summary>
        /// Deserializes the object, reading a byte array from the stream as written by Serialize.
        /// </summary>
        /// <param name="reader"></param>
        public virtual void Deserialize(BinaryReader reader) {
            id = reader.ReadUInt16();
            type = (ObjectType)reader.ReadByte();
            position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            facing = reader.ReadSingle();
            sector = reader.ReadByte();
            
            iconfile = reader.ReadString();
            iconcolor.PackedValue = reader.ReadUInt32();

            bounding = reader.ReadSingle();
        }
    }
    
}
