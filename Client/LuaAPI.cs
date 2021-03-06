﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace ShiftDrive {

    /// <summary>
    /// Represents a C# function that is intended to be called from Lua.
    /// </summary>
    /// <param name="L">A pointer to the Lua state.</param>
    [UnmanagedFunctionPointer(LuaAPI.LUA_CALLING_CONVENTION)]
    public delegate int lua_CFunction(IntPtr L);

    /// <summary>
    /// Represents an error that occurs during a Lua-related operation.
    /// </summary>
    [Serializable]
    internal class LuaException : Exception {
        public LuaException(string message) : base(message) {}
    }

    /// <summary>
    /// Represents a Lua value type. Matches the macros in lua.h.
    /// </summary>
    public enum LuaType {
        None = -1,

        Nil = 0,
        Boolean = 1,
        LightUserdata = 2,
        Number = 3,
        String = 4,
        Table = 5,
        Function = 6,
        Userdata = 7,
        Thread = 8
    }

    /// <summary>
    /// A container class for P/Invoke Lua API functions.
    /// </summary>
    internal static class LuaAPI {

        internal const string LIBNAME = "lua.dll";
        internal const CallingConvention LUA_CALLING_CONVENTION = CallingConvention.Cdecl;

        private const int LUA_REGISTRYINDEX = -10000;
        private const int LUA_ENVIRONINDEX = -10001;
        private const int LUA_GLOBALSINDEX = -10002;

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern IntPtr luaL_newstate();

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_close(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION, BestFitMapping = false)]
        internal static extern int luaL_loadstringex(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string s, [MarshalAs(UnmanagedType.LPStr)] string chunkname);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern lua_CFunction lua_atpanic(IntPtr L, lua_CFunction newfn);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_error(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION, BestFitMapping = false)]
        internal static extern int luaL_argerror(IntPtr L, int narg, [MarshalAs(UnmanagedType.LPStr)] string extramsg);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void luaL_where(IntPtr L, int lvl);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION, BestFitMapping = false)]
        internal static extern void luaL_traceback(IntPtr L, IntPtr L1, [MarshalAs(UnmanagedType.LPStr)] string msg, int lvl);

        internal static string lua_getwhere(IntPtr L, int lvl = 1) {
            string ret;
            luaL_where(L, lvl);
            ret = lua_tostring(L, -1);
            lua_pop(L, 1);
            return ret;
        }

        internal static int luaL_error(IntPtr L, string message) {
            luaL_where(L, 1);
            lua_pushstring(L, message);
            lua_concat(L, 2);
            return lua_error(L);
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaopen_base(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaopen_table(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaopen_string(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaopen_math(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_concat(IntPtr L, int n);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_call(IntPtr L, int nargs, int nresults);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_pcall(IntPtr L, int nargs, int nresults, int errfunc);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_pushcclosure(IntPtr L, lua_CFunction fn, int n);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_gettop(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_settop(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_remove(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_insert(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_replace(IntPtr L, int index);

        internal static void lua_pop(IntPtr L, int n) {
            lua_settop(L, -(n) - 1);
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION, BestFitMapping = false)]
        internal static extern void lua_setfield(IntPtr L, int index, [MarshalAs(UnmanagedType.LPStr)] string k);

        internal static void lua_setglobal(IntPtr L, string k) {
            lua_setfield(L, LUA_GLOBALSINDEX, k);
        }

        internal static void lua_setregistry(IntPtr L, string k) {
            lua_setfield(L, LUA_REGISTRYINDEX, k);
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION, BestFitMapping = false)]
        internal static extern void lua_getfield(IntPtr L, int index, [MarshalAs(UnmanagedType.LPStr)] string k);

        internal static void lua_getglobal(IntPtr L, string k) {
            lua_getfield(L, LUA_GLOBALSINDEX, k);
        }

        internal static void lua_getregistry(IntPtr L, string k) {
            lua_getfield(L, LUA_REGISTRYINDEX, k);
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_setmetatable(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_getmetatable(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_isnumber(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_isstring(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern LuaType lua_type(IntPtr luaState, int index);

        [DllImport(LIBNAME, EntryPoint = "lua_typename", CallingConvention = LUA_CALLING_CONVENTION)]
        private static extern IntPtr lua_typename_backend(IntPtr luaState, LuaType index);

        internal static string lua_typename(IntPtr L, LuaType index) {
            return Marshal.PtrToStringAnsi(lua_typename_backend(L, index));
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        private static extern void lua_pushlstring(IntPtr L, [MarshalAs(UnmanagedType.LPStr)] string s, UIntPtr len);

        internal static void lua_pushstring(IntPtr L, string s) {
            if (s == null) {
                lua_pushnil(L);
            } else {
                lua_pushlstring(L, s, new UIntPtr(unchecked((ulong)s.Length)));
            }
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_pushnumber(IntPtr L, double n);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_pushnil(IntPtr L);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_pushboolean(IntPtr L, int b);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_pushvalue(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_createtable(IntPtr L, int narr, int nrec);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        public static extern IntPtr lua_newuserdata(IntPtr L, UIntPtr size);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaL_ref(IntPtr L, int t);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void luaL_unref(IntPtr L, int t, int r);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_rawgeti(IntPtr L, int index, int n);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void lua_rawseti(IntPtr L, int index, int n);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern double lua_tonumber(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int lua_toboolean(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        public static extern IntPtr lua_touserdata(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        private static extern IntPtr lua_tolstring(IntPtr L, int index, ref UIntPtr len);

        internal static string lua_tostring(IntPtr L, int index) {
            var len = UIntPtr.Zero;
            var stringPtr = lua_tolstring(L, index, ref len);

            return stringPtr == IntPtr.Zero
                ? null
                : Marshal.PtrToStringAnsi(stringPtr, checked((int)len.ToUInt32()));
        }

        internal static Vector2 lua_tovec2(IntPtr L, int idx) {
            Vector2 ret;
            luaL_checktype(L, idx, LuaType.Table);
            lua_rawgeti(L, idx, 1);
            ret.X = (float)lua_tonumber(L, -1);
            lua_pop(L, 1);
            lua_rawgeti(L, idx, 2);
            ret.Y = (float)lua_tonumber(L, -1);
            lua_pop(L, 1);
            return ret;
        }

        internal static int luaH_gettableint(IntPtr L, int tableidx, string name) {
            lua_getfield(L, tableidx, name);
            luaH_checkfieldtype(L, tableidx, name, -1, LuaType.Number);
            int ret = (int)lua_tonumber(L, -1);
            lua_pop(L, 1);
            return ret;
        }

        internal static float luaH_gettablefloat(IntPtr L, int tableidx, string name) {
            lua_getfield(L, tableidx, name);
            luaH_checkfieldtype(L, tableidx, name, -1, LuaType.Number);
            float ret = (float)lua_tonumber(L, -1);
            lua_pop(L, 1);
            return ret;
        }

        internal static string luaH_gettablestring(IntPtr L, int tableidx, string name) {
            lua_getfield(L, tableidx, name);
            luaH_checkfieldtype(L, tableidx, name, -1, LuaType.String);
            string ret = lua_tostring(L, -1);
            lua_pop(L, 1);
            return ret;
        }

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern void luaL_checktype(IntPtr L, int narg, LuaType t);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern double luaL_checknumber(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern int luaL_checkboolean(IntPtr L, int index);

        [DllImport(LIBNAME, CallingConvention = LUA_CALLING_CONVENTION)]
        internal static extern IntPtr luaL_checklstring(IntPtr L, int index, out uint size);

        internal static string luaL_checkstring(IntPtr L, int index) {
            uint size;
            return Marshal.PtrToStringAnsi(luaL_checklstring(L, index, out size), (int)size);
        }

        internal static void luaH_checkfieldtype(IntPtr L, int tableidx, string fieldname, int fieldidx, LuaType reqtype) {
            // checks the type of a field obtained from a table, throwing a descriptive error if mismatching
            if (lua_type(L, fieldidx) == reqtype) return;
            luaL_argerror(L, tableidx,
                "bad type to field '" + fieldname + "': expected " + lua_typename(L, reqtype) + ", got " + lua_typename(L, lua_type(L, fieldidx)));
        }

    }

}
