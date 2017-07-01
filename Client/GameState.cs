/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;

namespace ShiftDrive {

    /// <summary>
    /// Represents a game world and manages a collection of <seealso cref="GameObject"/>s.
    /// </summary>
    internal sealed class GameState {

        public readonly Dictionary<uint, GameObject> Objects;
        public bool IsServer;

        public BVHTree BVH { get; }

        private uint cachedPlayerShipId;

        public GameState() {
            Objects = new Dictionary<uint, GameObject>();
            BVH = new BVHTree();
            cachedPlayerShipId = 0;
        }

        /// <summary>
        /// Returns the player ship object. Currently does not support multiple player ships.
        /// </summary>
        public PlayerShip GetPlayerShip() {
            // iterate through all objects to find the local player, and cache it
            if (cachedPlayerShipId != 0) return Objects[cachedPlayerShipId] as PlayerShip;
            foreach (var pair in Objects)
                if (pair.Value.Type == ObjectType.PlayerShip) {
                    cachedPlayerShipId = pair.Value.ID;
                    return pair.Value as PlayerShip;
                }
            return null;
        }

        /// <summary>
        /// Inserts an object into this GameState.
        /// </summary>
        public void AddObject(GameObject obj) {
            Objects.Add(obj.ID, obj);
            if (obj.Bounding > 0f) BVH.Insert(obj);
        }

        /// <summary>
        /// Removes the specified GameObject from the grid if present, and inserts it.
        /// </summary>
        public void Serialize(Packet outstream, bool forceAll) {
            // write serialized objects that have changed
            foreach (var pair in Objects) {
                GameObject obj = pair.Value;
                if (forceAll) // serialize all objects entirely
                    obj.Changed = ObjectProperty.All;

                if (obj.IsDestroyScheduled()) {
                    outstream.Write(pair.Value.ID);
                    outstream.Write(true);

                } else if (obj.Changed > ObjectProperty.None) {
                    outstream.Write(pair.Value.ID);
                    outstream.Write(false);
                    outstream.Write((byte)pair.Value.Type);
                    outstream.Write((uint)pair.Value.Changed);
                    pair.Value.Serialize(outstream);
                    pair.Value.Changed = ObjectProperty.None;
                }
            }
            // 0x00000000 marks the end of the message
            // (makes sense to use 0 because that's an invalid object ID)
            outstream.Write((uint)0);
        }

        public void Deserialize(Packet instream) {
            while (true) {
                // read next object ID. zero means end of message
                uint objid = instream.ReadUInt32();
                if (objid == 0) break;

                // set flag means a deleted object
                if (instream.ReadBoolean()) {
                    //if (Objects.ContainsKey(objid)) grid.Remove(Objects[objid]);
                    Objects.Remove(objid);
                    continue;
                }

                ObjectType objtype = (ObjectType)instream.ReadByte();
                ObjectProperty recvChanged = (ObjectProperty)instream.ReadUInt32();

                if (!Objects.ContainsKey(objid)) {
                    // this is a new object
                    GameObject obj;
                    switch (objtype) {
                        case ObjectType.PlayerShip: obj = new PlayerShip(this); break;
                        case ObjectType.AIShip: obj = new AIShip(this); break;
                        case ObjectType.Station: obj = new SpaceStation(this); break;
                        case ObjectType.Asteroid: obj = new Asteroid(this); break;
                        case ObjectType.Mine: obj = new Mine(this); break;
                        case ObjectType.BlackHole: obj = new BlackHole(this); break;
                        case ObjectType.Projectile: obj = new Projectile(this); break;
                        default:
                            throw new Exception(Locale.Get("err_unknownobject") + " (" + objtype + ")");
                    }
                    obj.Deserialize(instream, recvChanged);
                    obj.ID = objid;
                    AddObject(obj);
                } else {
                    // update this object with info from the stream
                    Objects[objid].Deserialize(instream, recvChanged);
                }
            }

            RebuildBVHTree();
        }

        /// <summary>
        /// Clears and rebuilds the BVH tree from scratch.
        /// </summary>
        public void RebuildBVHTree() {
            BVH.Clear();
            foreach (var gobj in Objects.Values) {
                BVH.Insert(gobj);
            }
        }

    }

}
