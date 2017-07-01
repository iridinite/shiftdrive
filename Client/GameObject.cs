/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Xna.Framework;
using static ShiftDrive.LuaAPI;

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

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Position)]
        public Vector2 Position { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Velocity)]
        public Vector2 Velocity { get; set; }
        public Vector2 Movement { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Facing)]
        public float Facing { get; set; }
        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Sector)]
        public byte Sector { get; set; }
        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.ZOrder)]
        public byte ZOrder { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Sprite, "Sprite")]
        public string SpriteName { get; protected set; }
        public SpriteSheet Sprite { get; protected set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Damping)]
        public float Damping { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Bounding)]
        public float Bounding { get; set; }

        public CollisionLayer Layer { get; set; }
        public CollisionLayer LayerMask { get; set; }

        public GameState World { get; }
        public ObjectProperty Changed { get; set; }

        private bool destroyScheduled;
        private LuaState lua;

        public static uint NextID { get; set; }
        private List<PropertyInfo> reflectedPropsRead;
        private List<PropertyInfo> reflectedPropsWrite;
        private List<MethodInfo> reflectedMethods;

        protected GameObject(GameState world) {
            this.World = world;
            ID = ++NextID;
            Changed = ObjectProperty.All;
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
                var possibleCollisions = World.BVH.Query(new BVHBox(this));
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
                Changed |= ObjectProperty.Velocity | ObjectProperty.Position;

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
            this.Changed |= ObjectProperty.Position | ObjectProperty.Velocity;
        }

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
        /// Sets this object's <seealso cref="LuaState"/> reference.
        /// </summary>
        public void SetLuaState(LuaState ls) {
            this.lua = ls;
        }

        /// <summary>
        /// Pushes a table to the Lua stack that represents this object.
        /// </summary>
        public void PushToLua() {
            // we use a userdata so we can subscribe to the __gc metamethod
            // might as well use the memory to save the object ID there
            IntPtr ud = lua_newuserdata(lua.LState, new UIntPtr(sizeof(uint)));
            Marshal.WriteInt32(ud, (int)ID);
            lua_createtable(lua.LState, 0, 4);
            //LuaAPI.lua_pushstring(ls.LState, ID.ToString());
            //LuaAPI.lua_setfield(ls.LState, -2, "guid");
            lua.PushDelegate(LuaM_Get);
            lua_setfield(lua.LState, -2, "__index");
            lua.PushDelegate(LuaM_Set);
            lua_setfield(lua.LState, -2, "__newindex");
            lua.PushDelegate(LuaM_GC);
            lua_setfield(lua.LState, -2, "__gc");
            lua_createtable(lua.LState, 0, 0);
            lua_setfield(lua.LState, -2, "__metatable");
            lua_setmetatable(lua.LState, -2);
            //Lua.lua_pushvalue(L, -1);
            //Lua.lua_setfield(L, Lua.LUA_REGISTRYINDEX, "char");
            lua.PinGameObject(this);
        }

        /// <summary>
        /// Discovers scriptable objects by using reflection, and caches them.
        /// </summary>
        private void DiscoverScriptables() {
            var reflPropAll = GetType().GetProperties();
            reflectedPropsRead = (from prop in reflPropAll
                let attributes = prop.GetCustomAttributes(typeof(ScriptablePropertyAttribute), true)
                let attr = attributes.Length > 0 ? attributes.OfType<ScriptablePropertyAttribute>().First() : null
                where attr != null && attr.Access.HasFlag(ScriptAccess.Read)
                select prop).ToList();
            reflectedPropsWrite = (from prop in reflPropAll
                let attributes = prop.GetCustomAttributes(typeof(ScriptablePropertyAttribute), true)
                let attr = attributes.Length > 0 ? attributes.OfType<ScriptablePropertyAttribute>().First() : null
                where attr != null && attr.Access.HasFlag(ScriptAccess.Write)
                select prop).ToList();

            var reflMethodsAll = GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            reflectedMethods = (from method in reflMethodsAll
                let attributes = method.GetCustomAttributes(typeof(ScriptableFunctionAttribute), true)
                let attr = attributes.Length > 0 ? attributes.OfType<ScriptableFunctionAttribute>().First() : null
                where attr != null
                select method).ToList();
        }

        /// <summary>
        /// Verifies that the specified stack object is a userdata representing this instance.
        /// </summary>
        /// <param name="L">A pointer to the Lua state.</param>
        /// <param name="stackIndex">The index in the Lua stack.</param>
        private void VerifySelfUserdata(IntPtr L, int stackIndex) {
            luaL_checktype(L, stackIndex, LuaType.Userdata);
            uint givenID = (uint)Marshal.ReadInt32(lua_touserdata(L, stackIndex));
            if (ID == givenID) return;

            lua_pushstring(L, "bad self - object ID mismatch");
            lua_error(L);
        }

        /// <summary>
        /// Implements the __index metamethod.
        /// </summary>
        private int LuaM_Get(IntPtr L) {
            if (reflectedMethods == null) DiscoverScriptables();
            Debug.Assert(reflectedPropsRead != null);
            Debug.Assert(reflectedMethods != null);

            VerifySelfUserdata(L, 1);
            string key = luaL_checkstring(L, 2);

            // iterate over properties with reflection
            foreach (var prop in reflectedPropsRead) {
                // find the ScriptablePropertyAttribute on this element
                var attributes = prop.GetCustomAttributes(typeof(ScriptablePropertyAttribute), true);
                var attr = attributes.Length > 0 ? (ScriptablePropertyAttribute)attributes[0] : null;
                Debug.Assert(attr != null);

                // name must match
                if (!(attr.Alias ?? prop.Name).Equals(key, StringComparison.InvariantCulture))
                    continue;

                // marshal and push the object
                if (prop.PropertyType == typeof(string))
                    lua_pushstring(L, (string)prop.GetValue(this));
                else if (prop.PropertyType == typeof(int))
                    lua_pushnumber(L, (int)prop.GetValue(this));
                else if (prop.PropertyType == typeof(byte))
                    lua_pushnumber(L, (byte)prop.GetValue(this));
                else if (prop.PropertyType == typeof(float))
                    lua_pushnumber(L, (float)prop.GetValue(this));
                else if (prop.PropertyType == typeof(bool))
                    lua_pushboolean(L, (bool)prop.GetValue(this) ? 1 : 0);
                else if (prop.PropertyType == typeof(Vector2)) {
                    Vector2 val = (Vector2)prop.GetValue(this);
                    lua_getglobal(L, "vec2"); // defined in util.lua
                    lua_pushnumber(L, val.X);
                    lua_pushnumber(L, val.Y);
                    lua_call(L, 2, 1);
                }
                else
                    throw new InvalidCastException($"Invalid property {prop.Name} of type {prop.PropertyType}");
                return 1;
            }

            // function names are prefixed to avoid ambiguity
            key = "LuaD_" + key;

            // iterate over functions
            foreach (var fn in reflectedMethods) {
                // name must match
                if (!fn.Name.Equals(key, StringComparison.InvariantCulture))
                    continue;

                // push the function on the Lua stack
                lua.PushDelegate((lua_CFunction)fn.CreateDelegate(typeof(lua_CFunction), this));
                return 1;
            }

            // no value found
            lua_pushnil(L);
            return 1;
        }

        /// <summary>
        /// Implements the __newindex metamethod.
        /// </summary>
        private int LuaM_Set(IntPtr L) {
            if (reflectedMethods == null) DiscoverScriptables();
            Debug.Assert(reflectedPropsWrite != null);

            VerifySelfUserdata(L, 1);
            string key = luaL_checkstring(L, 2);

            // iterate over properties with reflection
            foreach (var prop in reflectedPropsWrite) {
                // find the ScriptablePropertyAttribute on this element
                var attributes = prop.GetCustomAttributes(typeof(ScriptablePropertyAttribute), true);
                var attr = attributes.Length > 0 ? (ScriptablePropertyAttribute)attributes[0] : null;
                Debug.Assert(attr != null);

                // name must match
                if (!(attr.Alias ?? prop.Name).Equals(key, StringComparison.InvariantCulture))
                    continue;

                // marshal and push the object
                if (prop.PropertyType == typeof(string))
                    prop.SetValue(this, lua_tostring(L, 3));
                else if (prop.PropertyType == typeof(int))
                    prop.SetValue(this, (int)lua_tonumber(L, 3));
                else if (prop.PropertyType == typeof(byte))
                    prop.SetValue(this, (byte)lua_tonumber(L, 3));
                else if (prop.PropertyType == typeof(float))
                    prop.SetValue(this, (float)lua_tonumber(L, 3));
                else if (prop.PropertyType == typeof(bool))
                    prop.SetValue(this, lua_toboolean(L, 3) == 1);
                else if (prop.PropertyType == typeof(Vector2))
                    prop.SetValue(this, lua_tovec2(L, 3));
                else if (prop.PropertyType == typeof(WeaponMount[])) {
                    WeaponMount[] mounts = new WeaponMount[Ship.WEAPON_ARRAY_SIZE];
                    for (int i = 0; i < Ship.WEAPON_ARRAY_SIZE; i++) {
                        lua_rawgeti(L, 3, i + 1);
                        mounts[i] = lua_type(L, 4) == LuaType.Nil ? null : WeaponMount.FromLua(L, 4);
                        lua_pop(L, 1);
                    }
                    prop.SetValue(this, mounts);
                }
                else if (prop.PropertyType == typeof(Weapon[])) {
                    Weapon[] weapons = new Weapon[Ship.WEAPON_ARRAY_SIZE];
                    for (int i = 0; i < Ship.WEAPON_ARRAY_SIZE; i++) {
                        lua_rawgeti(L, 3, i + 1);
                        weapons[i] = lua_type(L, 4) == LuaType.Nil ? null : Weapon.FromLua(L, 4);
                        lua_pop(L, 1);
                    }
                    prop.SetValue(this, weapons);
                }
                else if (prop.PropertyType == typeof(Vector2[])) {
                    List<Vector2> flares = new List<Vector2>();
                    for (int i = 0; i < Ship.WEAPON_ARRAY_SIZE; i++) {
                        lua_rawgeti(L, 3, i + 1);
                        if (lua_type(L, 4) == LuaType.Nil) break;
                        if (lua_type(L, 4) != LuaType.Table) {
                            lua_pushstring(L, "expected vec2's in flares list");
                            lua_error(L);
                        }
                        flares.Add(lua_tovec2(L, 4));
                        lua_pop(L, 1);
                    }
                    prop.SetValue(this, flares.ToArray());
                }
                else
                    throw new InvalidCastException($"Invalid property {prop.Name} type {prop.PropertyType}");

                // update the changed flags on this object
                Changed |= attr.ChangeFlag;

                return 0;
            }

            lua_pushstring(L, $"attempt to write to unknown property '{key}'");
            return lua_error(L);
        }

        /// <summary>
        /// Implements the __gc metamethod.
        /// </summary>
        private int LuaM_GC(IntPtr L) {
#if DEBUG
            SDGame.Inst.Print($"GC'd {ID}");
#endif
            lua.ReleaseGameObject(this);
            return 0;
        }

        [ScriptableFunction, UsedImplicitly]
        protected int LuaD_TakeDamage(IntPtr L) {
            VerifySelfUserdata(L, 1);
            TakeDamage((int)luaL_checknumber(L, 2));
            return 0;
        }

        [ScriptableFunction, UsedImplicitly]
        protected int LuaD_Destroy(IntPtr L) {
            VerifySelfUserdata(L, 1);
            Destroy();
            return 0;
        }

        [ScriptableFunction, UsedImplicitly]
        protected int LuaD_IsShip(IntPtr L) {
            VerifySelfUserdata(L, 1);
            lua_pushboolean(L, IsShip() ? 1 : 0);
            return 1;
        }

        /// <summary>
        /// Serializes the object as a byte array.
        /// </summary>
        public virtual void Serialize(Packet outstream) {
            if (Changed.HasFlag(ObjectProperty.Position)) {
                outstream.Write(Position.X);
                outstream.Write(Position.Y);
            }
            if (Changed.HasFlag(ObjectProperty.Velocity)) {
                outstream.Write(Velocity.X);
                outstream.Write(Velocity.Y);
            }
            if (Changed.HasFlag(ObjectProperty.Facing))
                outstream.Write(Facing);
            if (Changed.HasFlag(ObjectProperty.Sector))
                outstream.Write(Sector);
            if (Changed.HasFlag(ObjectProperty.ZOrder))
                outstream.Write(ZOrder);
            if (Changed.HasFlag(ObjectProperty.Bounding))
                outstream.Write(Bounding);
            if (Changed.HasFlag(ObjectProperty.Damping))
                outstream.Write(Damping);
            if (Changed.HasFlag(ObjectProperty.Layer)) {
                outstream.Write((uint)Layer);
                outstream.Write((uint)LayerMask);
            }

            if (Changed.HasFlag(ObjectProperty.Sprite))
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

    }

}
