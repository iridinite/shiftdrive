/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;

namespace ShiftDrive {

    /// <summary>
    /// Describes how a property may be manipulated from a Lua script.
    /// </summary>
    [Flags]
    internal enum ScriptAccess {
        None = 0,
        Read = 1 << 0,
        Write = 1 << 1,
        ReadWrite = Read | Write
    }

    /// <summary>
    /// Indicates that a property can be accessed from a Lua script.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    internal sealed class ScriptablePropertyAttribute : Attribute {
        public ScriptAccess Access { get; }
        public ObjectProperty ChangeFlag { get; }
        public string Alias { get; }

        public ScriptablePropertyAttribute(ScriptAccess Access, ObjectProperty ChangeFlag = ObjectProperty.None, string Alias = null) {
            this.Access = Access;
            this.ChangeFlag = ChangeFlag;
            this.Alias = Alias;
        }
    }

    /// <summary>
    /// Indicates that a function can be called from a Lua script.
    /// </summary>
    /// <remarks>
    /// The method name must start with the prefix 'LuaD_'.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class ScriptableFunctionAttribute : Attribute {}

}
