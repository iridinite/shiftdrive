/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using System.IO;

namespace ShiftDrive {
    
    internal sealed class GameState {

        public readonly List<GameObject> Objects;

        public bool IsServer;

        // for caching, client-side
        private PlayerShip _cachedPlayer;

        /// <summary>
        /// Returns the player ship object. Currently does not support multiple player ships.
        /// </summary>
        public PlayerShip GetPlayerShip() {
            // iterate through all objects to find the local player, and cache it
            if (_cachedPlayer != null) return _cachedPlayer;
            for (int i = 0; i < Objects.Count; i++)
                if (Objects[i].type == ObjectType.PlayerShip) { //&& (Objects[i] as PlayerShip).player == LocalPlayer) {
                    _cachedPlayer = Objects[i] as PlayerShip;
                    return _cachedPlayer;
                }
            return null;
        }

        public GameState() {
            Objects = new List<GameObject>();
        }

        public void Serialize(BinaryWriter writer) {
            // write named objects
            writer.Write(Objects.Count);
            for (int i = 0; i < Objects.Count; i++) {
                writer.Write((byte)Objects[i].type);
                Objects[i].Serialize(writer);
            }
        }

        public void Deserialize(BinaryReader reader) {
            _cachedPlayer = null;
            Objects.Clear();

            int objectCount = reader.ReadInt32();
            for (int i = 0; i < objectCount; i++) {
                ObjectType objtype = (ObjectType)reader.ReadByte();
                NamedObject obj;
                switch (objtype) {
                    case ObjectType.PlayerShip:
                        obj = new PlayerShip();
                        break;
                    case ObjectType.AIShip:
                        obj = new AIShip();
                        break;
                    case ObjectType.BlackHole:
                        obj = new BlackHole();
                        break;
                    default:
                        throw new Exception(Utils.LocaleGet("err_unknownobject") + " (" + objtype + ")");
                }
                obj.Deserialize(reader);
                Objects.Add(obj);
            }
        }

    }

}