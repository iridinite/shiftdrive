/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

namespace ShiftDrive {

    /// <summary>
    /// Represents a <see cref="GameObject"/> that has a name and can be examined by a player.
    /// </summary>
    internal abstract class NamedObject : GameObject {

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.NameShort)]
        public string NameShort { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.NameFull)]
        public string NameFull { get; set; }

        [ScriptableProperty(ScriptAccess.ReadWrite, ObjectProperty.Description)]
        public string Description { get; set; }

        protected NamedObject(GameState world) : base(world) {
            SpriteName = "ui/rect";
            NameShort = "OBJ";
            NameFull = "Object";
            Description = "";
        }

        public override void Serialize(Packet outstream) {
            base.Serialize(outstream);

            if (Changed.HasFlag(ObjectProperty.NameShort))
                outstream.Write(NameShort);
            if (Changed.HasFlag(ObjectProperty.NameFull))
                outstream.Write(NameFull);
            if (Changed.HasFlag(ObjectProperty.Description))
                outstream.Write(Description);
        }

        public override void Deserialize(Packet instream, ObjectProperty recvChanged) {
            base.Deserialize(instream, recvChanged);

            if (recvChanged.HasFlag(ObjectProperty.NameShort))
                NameShort = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.NameFull))
                NameFull = instream.ReadString();
            if (recvChanged.HasFlag(ObjectProperty.Description))
                Description = instream.ReadString();
        }

    }

}
