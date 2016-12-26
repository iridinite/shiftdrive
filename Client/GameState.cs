/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftDrive {

    /// <summary>
    /// Represents a game world and manages a collection of <seealso cref="GameObject"/>s.
    /// </summary>
    internal sealed class GameState {

        public readonly Dictionary<uint, GameObject> Objects;
        public bool IsServer;

        private CollisionGrid grid;
        private uint cachedPlayerShipId;

        public GameState() {
            Objects = new Dictionary<uint, GameObject>();
            grid = new CollisionGrid();
            cachedPlayerShipId = 0;
        }

        /// <summary>
        /// Returns the player ship object. Currently does not support multiple player ships.
        /// </summary>
        public PlayerShip GetPlayerShip() {
            // iterate through all objects to find the local player, and cache it
            if (cachedPlayerShipId != 0) return Objects[cachedPlayerShipId] as PlayerShip;
            foreach (var pair in Objects)
                if (pair.Value.type == ObjectType.PlayerShip) {
                    cachedPlayerShipId = pair.Value.id;
                    return pair.Value as PlayerShip;
                }
            return null;
        }

        /// <summary>
        /// Inserts an object into this GameState.
        /// </summary>
        public void AddObject(GameObject obj) {
            Objects.Add(obj.id, obj);
            if (obj.bounding > 0f) grid.Insert(obj);
        }

        /// <summary>
        /// Queries this GameState's CollisionGrid.
        /// </summary>
        public List<GameObject> QueryGrid(GameObject obj) {
            return grid.Query(obj);
        }

        /// <summary>
        /// Removes the specified GameObject from the grid if present, and inserts it.
        /// </summary>
        public void ReinsertGrid(GameObject obj) {
            grid.Remove(obj);
            grid.Insert(obj);
        }

        /// <summary>
        /// Updates the CollisionGrid.
        /// </summary>
        public void UpdateGrid() {
            grid.Update();
        }

        public void Serialize(Packet outstream) {
            // write serialized objects that have changed
            foreach (var pair in Objects) {
                GameObject obj = pair.Value;
                if (obj.IsDestroyScheduled()) {
                    outstream.Write(pair.Value.id);
                    outstream.Write(true);

                } else if (obj.changed > ObjectProperty.None) {
                    outstream.Write(pair.Value.id);
                    outstream.Write(false);
                    outstream.Write((byte)pair.Value.type);
                    outstream.Write((uint)pair.Value.changed);
                    pair.Value.Serialize(outstream);
                    pair.Value.changed = ObjectProperty.None;
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
                    if (Objects.ContainsKey(objid)) grid.Remove(Objects[objid]);
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
                        case ObjectType.BlackHole: obj = new BlackHole(this); break;
                        case ObjectType.Projectile: obj = new Projectile(this); break;
                        default:
                            throw new Exception(Utils.LocaleGet("err_unknownobject") + " (" + objtype + ")");
                    }
                    obj.Deserialize(instream, recvChanged);
                    obj.id = objid;
                    AddObject(obj);
                } else {
                    // update this object with info from the stream
                    Objects[objid].Deserialize(instream, recvChanged);
                }
            }
        }

    }

}