/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftDrive {
    
    internal sealed class GameState {

        public readonly Dictionary<ushort, GameObject> Objects;
        public bool IsServer;
        
        private ushort cachedPlayerShipId;

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

        public GameState() {
            Objects = new Dictionary<ushort, GameObject>();
            cachedPlayerShipId = 0;
        }

        public void Serialize(BinaryWriter writer) {
            // write serialized objects that have changed
            foreach (var pair in Objects) {
                GameObject obj = pair.Value;
                if (obj.ShouldDestroy()) {
                    writer.Write(pair.Value.id);
                    writer.Write(true);

                } else if (obj.changed) {
                    writer.Write(pair.Value.id);
                    writer.Write(false);
                    writer.Write((byte)pair.Value.type);
                    pair.Value.Serialize(writer);
                    pair.Value.changed = false;
                }
            }
            // 0x0000 marks the end of the message
            // (makes sense to use 0 because that's an invalid object ID)
            writer.Write((ushort)0);
        }

        public void Deserialize(BinaryReader reader) {
            while (true) {
                // read next object ID. zero means end of message
                ushort objid = reader.ReadUInt16();
                if (objid == 0) break;

                // set flag means a deleted object
                if (reader.ReadBoolean()) {
                    Objects.Remove(objid);
                    continue;
                }

                ObjectType objtype = (ObjectType)reader.ReadByte();

                if (!Objects.ContainsKey(objid)) {
                    // this is a new object
                    GameObject obj;
                    switch (objtype) {
                        case ObjectType.PlayerShip:
                            obj = new PlayerShip();
                            break;
                        case ObjectType.AIShip:
                            obj = new AIShip();
                            break;
                        case ObjectType.Asteroid:
                            obj = new Asteroid();
                            break;
                        case ObjectType.BlackHole:
                            obj = new BlackHole();
                            break;
                        default:
                            throw new Exception(Utils.LocaleGet("err_unknownobject") + " (" + objtype + ")");
                    }
                    obj.Deserialize(reader);
                    Objects.Add(objid, obj);
                }
                else {
                    // update this object with info from the stream
                    Objects[objid].Deserialize(reader);
                }
            }
        }

    }

}