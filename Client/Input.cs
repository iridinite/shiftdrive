/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    /// <summary>
    /// Exposes functionality for obtaining mouse and keyboard state.
    /// </summary>
    internal static class Input {
        private static MouseState mousePrev;
        private static MouseState mouseCurr;
        private static KeyboardState keyPrev;
        private static KeyboardState keyCurr;

        public static int MouseX => mouseCurr.X;
        public static int MouseY => mouseCurr.Y;
        public static Vector2 MousePosition => new Vector2(mouseCurr.X, mouseCurr.Y);

        public static void Update() {
            mousePrev = mouseCurr;
            mouseCurr = Mouse.GetState();
            keyPrev = keyCurr;
            keyCurr = Keyboard.GetState();
        }

        public static bool GetMouseInArea(Rectangle rect) {
            return mouseCurr.X >= rect.X &&
                mouseCurr.X <= rect.X + rect.Width &&
                mouseCurr.Y >= rect.Y &&
                mouseCurr.Y <= rect.Y + rect.Height;
        }

        public static bool GetMouseInArea(int x, int y, int w, int h) {
            return GetMouseInArea(new Rectangle(x, y, w, h));
        }

        public static bool GetMouseLeft() {
            return mouseCurr.LeftButton == ButtonState.Pressed;
        }

        public static bool GetMouseLeftDown() {
            return mouseCurr.LeftButton == ButtonState.Pressed &&
                mousePrev.LeftButton == ButtonState.Released;
        }

        public static bool GetMouseLeftUp() {
            return mousePrev.LeftButton == ButtonState.Pressed &&
                mouseCurr.LeftButton == ButtonState.Released;
        }

        public static bool GetMouseRight() {
            return mouseCurr.RightButton == ButtonState.Pressed;
        }

        public static bool GetMouseRightDown() {
            return mouseCurr.RightButton == ButtonState.Pressed &&
                mousePrev.RightButton == ButtonState.Released;
        }

        public static bool GetKey(Keys k) {
            return keyCurr.IsKeyDown(k);
        }

        public static bool GetKeyDown(Keys k) {
            return keyCurr.IsKeyDown(k) && keyPrev.IsKeyUp(k);
        }

        public static bool GetKeyUp(Keys k) {
            return keyCurr.IsKeyUp(k) && keyPrev.IsKeyDown(k);
        }
    }

}
