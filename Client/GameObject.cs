/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;

// ReSharper disable MemberCanBeProtected.Global
// ReSharper disable MemberCanBePrivate.Global

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
        Layer           = 1 << 10,

        Health          = 1 << 11,
        HealthMax       = 1 << 12,
        MoveStats       = 1 << 13,
        Throttle        = 1 << 14,
        Steering        = 1 << 15,
        Mounts          = 1 << 16,
        Weapons         = 1 << 17,
        Faction         = 1 << 18,
        Flares          = 1 << 19,
        Targets         = 1 << 20,

        PlayerData      = 1 << 21,
        ProjectileData  = 1 << 22,

        NameShort       = 1 << 23,
        NameFull        = 1 << 24,
        Description     = 1 << 25,

        All             = unchecked((uint)-1)
    }

    /// <summary>
    /// Represents a serializable object in the game world.
    /// </summary>
    internal abstract class GameObject {
        public uint ID { get; set; }
        public ObjectType Type { get; protected set; }

        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Movement { get; set; }
        public float Facing { get; set; }
        public byte Sector { get; set; }
        public byte ZOrder { get; set; }

        public string SpriteName { get; protected set; }
        public SpriteSheet Sprite { get; protected set; }

        public float Damping { get; set; }
        public float Bounding { get; set; }
        public CollisionLayer Layer { get; set; }
        public CollisionLayer LayerMask { get; set; }

        public GameState World { get; }

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
            this.World = world;
            ID = ++nextId;
            refLuaGet = LuaGet;
            refLuaSet = LuaSet;
            refLuaTakeDamage = clua_TakeDamage;
            refLuaDestroy = clua_Destroy;
            refLuaIsShip = clua_IsShip;
            changed = ObjectProperty.All;
            Damping = 1.0f;
            ZOrder = 127;
            Layer = CollisionLayer.Default;
            LayerMask = CollisionLayer.All;
        }

        /// <summary>
        /// Updates this object.
        /// </summary>
        /// <param name="deltaTime">The number of seconds that passed since the previous update.</param>
        public virtual void Update(float deltaTime) {
            // sanity checks: all objects should always have valid positioning
            Debug.Assert(!float.IsNaN(Position.X) && !float.IsNaN(Position.Y), "object position is NaN");
            Debug.Assert(!float.IsNaN(Velocity.X) && !float.IsNaN(Velocity.Y), "object velocity is NaN");

            // handle collision with objects
            if (Bounding > 0f) {
                var possibleCollisions = World.QueryGrid(this);
                foreach (GameObject obj in possibleCollisions) {
                    if (obj.ID == this.ID)
                        continue; // don't collide with self
                    if (obj.Bounding <= 0f)
                        continue; // don't collide with objects with no bounding sphere
                    if ((obj.Layer & this.LayerMask) == 0)
                        continue; // do we collide with the layer the other object is on?

                    float dist = Vector2.DistanceSquared(obj.Position, this.Position);
                    float totalbound = obj.Bounding + this.Bounding;
                    if (dist > totalbound * totalbound)
                        continue; // bounding sphere check

                    // calculate contact information
                    Vector2 normal = Vector2.Normalize(this.Position - obj.Position);
                    float penetration = this.Bounding + obj.Bounding - (float)Math.Sqrt(dist);
                    // handle collision response
                    OnCollision(obj, normal, penetration);
                }
            }

            // integrate velocity into position
            Vector2 oldVelocity = Velocity;
            Movement = Velocity;
            Position += Movement * deltaTime;
            Velocity *= (float)Math.Pow(Damping, deltaTime);

            // retransmit velocity only if velocity changes, and include position as safeguard against desync.
            // we don't need to send position normally, because the client correctly predicts position for static velocity.
            if ((oldVelocity - Velocity).LengthSquared() > 0.001f)
                changed |= ObjectProperty.Velocity | ObjectProperty.Position;

            // animate the object sprite
            if (!World.IsServer) Sprite?.Update(deltaTime);
        }

        /// <summary>
        /// Applies damage to this object. The derived class decides how
        /// damage affects the object, if at all.
        /// </summary>
        /// <param name="damage">The damage total to apply.</param>
        /// <param name="sound">Indicates whether to play a sound, if applicable.</param>
        public virtual void TakeDamage(float damage, bool sound = false) {
            // breakable objects should override this
        }

        /// <summary>
        /// Handle a collision with another <see cref="GameObject"/>.
        /// </summary>
        protected virtual void OnCollision(GameObject other, Vector2 normal, float penetration) {
            // apply minimum translation vector to resolve collision
            this.Position += normal * penetration;
            this.Velocity += normal * penetration;
            this.changed |= ObjectProperty.Position | ObjectProperty.Velocity;
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
            if (!World.IsServer) return;
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
            return Type == ObjectType.AIShip || Type == ObjectType.PlayerShip;
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
                    LuaAPI.lua_pushnumber(L, ID);
                    break;
                case "position":
                    // position is a two-element table {x, y}
                    // use the 'vec2' function in util.lua to create a vector
                    LuaAPI.lua_getglobal(L, "vec2");
                    LuaAPI.lua_pushnumber(L, Position.X);
                    LuaAPI.lua_pushnumber(L, Position.Y);
                    LuaAPI.lua_call(L, 2, 1);
                    break;
                case "facing":
                    LuaAPI.lua_pushnumber(L, Facing);
                    break;
                case "bounding":
                    LuaAPI.lua_pushnumber(L, Bounding);
                    break;
                case "sprite":
                    LuaAPI.lua_pushstring(L, SpriteName);
                    break;
                case "zorder":
                    LuaAPI.lua_pushnumber(L, ZOrder);
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
                    Position = LuaAPI.lua_tovec2(L, 3);
                    World.ReinsertGrid(this);
                    changed |= ObjectProperty.Position;
                    break;
                case "facing":
                    Facing = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Facing;
                    break;
                case "bounding":
                    Bounding = (float)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.Bounding;
                    World.ReinsertGrid(this);
                    break;
                case "sprite":
                    SpriteName = LuaAPI.luaL_checkstring(L, 3);
                    changed |= ObjectProperty.Sprite;
                    break;
                case "zorder":
                    ZOrder = (byte)LuaAPI.luaL_checknumber(L, 3);
                    changed |= ObjectProperty.ZOrder;
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
                outstream.Write(Position.X);
                outstream.Write(Position.Y);
            }
            if (changed.HasFlag(ObjectProperty.Velocity)) {
                outstream.Write(Velocity.X);
                outstream.Write(Velocity.Y);
            }
            if (changed.HasFlag(ObjectProperty.Facing))
                outstream.Write(Facing);
            if (changed.HasFlag(ObjectProperty.Sector))
                outstream.Write((byte)Sector);
            if (changed.HasFlag(ObjectProperty.ZOrder))
                outstream.Write(ZOrder);
            if (changed.HasFlag(ObjectProperty.Bounding))
                outstream.Write(Bounding);
            if (changed.HasFlag(ObjectProperty.Damping))
                outstream.Write(Damping);
            if (changed.HasFlag(ObjectProperty.Layer)) {
                outstream.Write((uint)Layer);
                outstream.Write((uint)LayerMask);
            }

            if (changed.HasFlag(ObjectProperty.Sprite))
                outstream.Write(SpriteName);
        }

        /// <summary>
        /// Deserializes the object, reading a byte array from the stream as written by Serialize.
        /// </summary>
        public virtual void Deserialize(Packet instream, ObjectProperty recvChanged) {
            if (recvChanged.HasFlag(ObjectProperty.Position))
                Position = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            if (recvChanged.HasFlag(ObjectProperty.Velocity))
                Velocity = new Vector2(instream.ReadSingle(), instream.ReadSingle());
            if (recvChanged.HasFlag(ObjectProperty.Facing))
                Facing = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Sector))
                Sector = instream.ReadByte();
            if (recvChanged.HasFlag(ObjectProperty.ZOrder))
                ZOrder = instream.ReadByte();
            if (recvChanged.HasFlag(ObjectProperty.Bounding))
                Bounding = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Damping))
                Damping = instream.ReadSingle();
            if (recvChanged.HasFlag(ObjectProperty.Layer)) {
                Layer = (CollisionLayer)instream.ReadUInt32();
                LayerMask = (CollisionLayer)instream.ReadUInt32();
            }

            if (recvChanged.HasFlag(ObjectProperty.Sprite)) {
                SpriteName = instream.ReadString();
                Sprite = Assets.GetSprite(SpriteName).Clone();
            }
        }

        /// <summary>
        /// Resets the sequence of object ID numbers.
        /// </summary>
        public static void ResetIds() {
            nextId = 0U;
        }

    }

}
