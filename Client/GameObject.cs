/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Reflects the class that should be used to deserialize an object.
    /// </summary>
    internal enum ObjectType {
        Generic,
        Asteroid,
        Mine,
        Nebula,
        AIShip,
        PlayerShip,
        Station,
        BlackHole,
        Projectile
    }

    /// <summary>
    /// Represents a bitmask of properties, used for avoiding serialization of
    /// unchanged <seealso cref="GameObject"/> attributes.
    /// </summary>
    [Flags]
    internal enum ObjectProperty : uint {
        None            = 0,

        Position        = 1 << 0,
        Velocity        = 1 << 1,
        AngularVelocity = 1 << 2,
        Facing          = 1 << 3,
        Sector          = 1 << 4,
        ZOrder          = 1 << 5,
        Sprite          = 1 << 6,
        Color           = 1 << 7,
        Damping         = 1 << 8,
        Bounding        = 1 << 9,

        Health          = 1 << 11,
        HealthMax       = 1 << 12,
        MoveStats       = 1 << 13,
        Throttle        = 1 << 14,
        Steering        = 1 << 15,
        Mounts          = 1 << 16,
        Weapons         = 1 << 17,
        Faction         = 1 << 18,
        Flares          = 1 << 19,

        ProjectileData  = 1 << 21,

        NameShort       = 1 << 22,
        NameFull        = 1 << 23,
        Description     = 1 << 24,

        All             = unchecked((uint)-1)
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
        public byte zorder;

        public string spritename;
        public SpriteSheet sprite;
        public Color color;

        public float damping;
        public float bounding;
        public CollisionLayer layer;
        public CollisionLayer layermask;

        protected readonly GameState world;

        private readonly lua_CFunction
            refLuaGet,
            refLuaSet,
            refLuaTakeDamage,
            refLuaDestroy,
            refLuaIsShip;

        public ObjectProperty changed;
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
            changed = ObjectProperty.All;
            color = Color.White;
            damping = 1.0f;
            zorder = 127;
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
            Vector2 oldVelocity = velocity;
            position += velocity * deltaTime;
            velocity *= (float)Math.Pow(damping, deltaTime);

            // retransmit velocity only if velocity changes, and include position as safeguard against desync.
            // we don't need to send position normally, because the client correctly predicts position for static velocity.
            if ((oldVelocity - velocity).LengthSquared() > 0.001f)
                changed |= ObjectProperty.Velocity | ObjectProperty.Position;

            // animate the object sprite
            if (!world.IsServer) sprite.Update(deltaTime);
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

#if DEBUG
        /// <summary>
        /// Returns the identifier that will be used for the next network object.
        /// </summary>
        public static uint GetNextId() {
            return nextId;
        }
#endif

        /// <summary>
        /// Specifies whether this object can be targeted by weapons.
        /// </summary>
        public virtual bool IsTargetable() {
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
        public bool IsDestroyScheduled() {
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
                    // use the 'vec2' function in util.lua to create a vector
                    LuaAPI.lua_getglobal(L, "vec2");
                    LuaAPI.lua_pushnumber(L, position.X);
                    LuaAPI.lua_pushnumber(L, position.Y);
                    LuaAPI.lua_call(L, 2, 1);
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
                case "zorder":
                    LuaAPI.lua_pushnumber(L, zorder);
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
                    changed |= ObjectProperty.Position;
                    break;
                case "facing":
                    facing = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Facing;
                    break;
                case "bounding":
                    bounding = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Bounding;
                    world.ReinsertGrid(this);
                    break;
                case "sprite":
                    spritename = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.Sprite;
                    break;
                case "zorder":
                    zorder = (byte)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.ZOrder;
                    break;
                case "color":
                    color.PackedValue = (uint)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Color;
                    break;
                default:
                    LuaAPI.lua_pushstring(L, "attempt to modify unknown field '" + key + "'");
                    LuaAPI.lua_error(L);
                    break;
            }
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

        /// <summary>
        /// Serializes the object as a byte array.
        /// </summary>
        public virtual void Serialize(Packet outstream) {
            if (changed.HasFlag(ObjectProperty.Position)) {
                outstream.Write(position.X);
                outstream.Write(position.Y);
            }
            if (changed.HasFlag(ObjectProperty.Velocity)) {
                outstream.Write(velocity.X);
                outstream.Write(velocity.Y);
            }
            if (changed.HasFlag(ObjectProperty.Facing))
                outstream.Write(facing);
            if (changed.HasFlag(ObjectProperty.Sector))
                outstream.Write((byte)sector);
            if (changed.HasFlag(ObjectProperty.ZOrder))
                outstream.Write(zorder);
            if (changed.HasFlag(ObjectProperty.Bounding))
                outstream.Write(bounding);
            if (changed.HasFlag(ObjectProperty.Damping))
                outstream.Write(damping);

            if (changed.HasFlag(ObjectProperty.Sprite))
                outstream.Write(spritename);
            if (changed.HasFlag(ObjectProperty.Color))
                outstream.Write(color.PackedValue);
        }

        /// <summary>
        /// Deserializes the object, reading a byte array from the stream as written by Serialize.
        /// </summary>
        public virtual void Deserialize(Packet instream, ObjectProperty recvChanged) {
            if (recvChanged.HasFlag(ObjectProperty.Position))
                position = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            if (recvChanged.HasFlag(ObjectProperty.Velocity))
                velocity = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            if (recvChanged.HasFlag(ObjectProperty.Facing))
                facing = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Sector))
                sector = instream.ReadByte();
            if (recvChanged.HasFlag(ObjectProperty.ZOrder))
                zorder = instream.ReadByte();
            if (recvChanged.HasFlag(ObjectProperty.Bounding))
                bounding = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Damping))
                damping = instream.ReadSingle();

            if (recvChanged.HasFlag(ObjectProperty.Sprite)) {
                spritename = instream.ReadString();
                sprite = Assets.GetSprite(spritename).Clone();
            }

            if (recvChanged.HasFlag(ObjectProperty.Color))
                color.PackedValue = instream.ReadUInt32();
        }

        /// <summary>
        /// Resets the sequence of object ID numbers.
        /// </summary>
        public static void ResetIds() {
            nextId = 0U;
        }

    }

}
