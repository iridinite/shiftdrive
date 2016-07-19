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
        public uint id;
        public ObjectType type;

        public Vector2 position;
        public float facing;
        public int sector;
        
        public string iconfile;
        public Color iconcolor;

        public float bounding;

        private readonly lua_CFunction refLuaGet, refLuaSet;
        private bool destroyScheduled;
        private static uint nextId;

        public abstract void Update(GameState world, float deltaTime);

        protected GameObject() {
            id = ++nextId;
            refLuaGet = LuaGet;
            refLuaSet = LuaSet;
        }

        public virtual void TakeDamage(float damage) {
            // breakable objects should override this
        }

        public virtual void Destroy() {
            destroyScheduled = true;
        }

        public bool ShouldDestroy() {
            return destroyScheduled;
        }

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
            return 0;
        }

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

        public virtual void Deserialize(BinaryReader reader) {
            id = reader.ReadUInt32();
            type = (ObjectType)reader.ReadByte();
            position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            facing = reader.ReadSingle();
            sector = reader.ReadByte();
            
            iconfile = reader.ReadString();
            iconcolor.PackedValue = reader.ReadUInt32();

            bounding = reader.ReadSingle();
        }
    }
    
    /// <summary>
    /// Represents a <see cref="GameObject"/> that has a name and can be examined by a player.
    /// </summary>
    internal abstract class NamedObject : GameObject {
        public string nameshort;
        public string namefull;
        public string desc;

        protected NamedObject() {
            iconfile = "";
            nameshort = "OBJ";
            namefull = "Object";
            desc = "";
        }

        public override void Serialize(BinaryWriter writer) {
            base.Serialize(writer);

            writer.Write(nameshort);
            writer.Write(namefull);
            writer.Write(desc);
        }

        public override void Deserialize(BinaryReader reader) {
            base.Deserialize(reader);

            nameshort = reader.ReadString();
            namefull = reader.ReadString();
            desc = reader.ReadString();
        }

        protected override int LuaGet(IntPtr L) {
            if (LuaAPI.lua_isstring(L, 2) != 1) return 0;
            string key = LuaAPI.lua_tostring(L, 2);
            switch (key) {
                case "nameshort":
                    LuaAPI.lua_pushstring(L, nameshort);
                    break;
                case "namefull":
                    LuaAPI.lua_pushstring(L, namefull);
                    break;
                case "desc":
                    LuaAPI.lua_pushstring(L, desc);
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
                case "nameshort":
                    nameshort = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "namefull":
                    namefull = LuaAPI.luaL_checkstring(L, 3);
                    break;
                case "desc":
                    desc = LuaAPI.luaL_checkstring(L, 3);
                    break;
                default:
                    return base.LuaSet(L);
            }
            return 0;
        }

    }

}
