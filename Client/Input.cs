/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    internal static class Mouse {
        private static MouseState prev;
        private static MouseState curr;

        public static int X { get { return curr.X; } }
        public static int Y { get { return curr.Y; } }
        public static Vector2 Position { get { return new Vector2(curr.X, curr.Y); } }

        public static void Update() {
            prev = curr;
            curr = Microsoft.Xna.Framework.Input.Mouse.GetState();
        }
        
        public static bool IsInArea(Rectangle rect) {
            return curr.X >= rect.X &&
                curr.X <= rect.X + rect.Width &&
                curr.Y >= rect.Y &&
                curr.Y <= rect.Y + rect.Height;
        }

        public static bool IsInArea(int x, int y, int w, int h) {
            return IsInArea(new Rectangle(x, y, w, h));
        }

        public static bool GetLeft() {
            return curr.LeftButton == ButtonState.Pressed;
        }

        public static bool GetLeftDown() {
            return curr.LeftButton == ButtonState.Pressed &&
                prev.LeftButton == ButtonState.Released;
        }

        public static bool GetLeftUp() {
            return prev.LeftButton == ButtonState.Pressed &&
                curr.LeftButton == ButtonState.Released;
        }

        public static bool GetRight() {
            return curr.RightButton == ButtonState.Pressed;
        }

        public static bool GetRightDown() {
            return curr.RightButton == ButtonState.Pressed &&
                prev.RightButton == ButtonState.Released;
        }
    }

    internal static class KeyInput {

        private static KeyboardState prev;
        private static KeyboardState cur;

        public static void Update() {
            prev = cur;
            cur = Keyboard.GetState();
        }

        public static bool GetHeld(Keys k) {
            return cur.IsKeyDown(k);
        }

        public static bool GetDown(Keys k) {
            return cur.IsKeyDown(k) && prev.IsKeyUp(k);
        }

        public static bool GetUp(Keys k) {
            return cur.IsKeyUp(k) && prev.IsKeyDown(k);
        }

    }

}