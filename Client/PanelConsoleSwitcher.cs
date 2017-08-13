/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A UI element that shows a list of role buttons in the top left corner.
    /// </summary>
    internal sealed class PanelConsoleSwitcher : Control {

        public PanelConsoleSwitcher() {
            // our OnDraw is a background, so draw children over it
            DrawMode = ControlDrawMode.ChildrenLast;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/consolepanel"), new Rectangle(0, -496 + GetChildrenCount() * 40, 64, 512), null, Color.White,
                0f, Vector2.Zero, SpriteEffects.None, 0f);
        }

        public void AddConsoleButton(int icon, OnClickHandler fn, string tooltip) {
            var y = GetChildrenCount() * 40 + 4;
            // create a new button and add it to the list
            ImageButton cbtn = new ImageButton(-1, 4, y, 36, 36, Assets.GetTexture("ui/consolebuttons"), Color.Black);
            cbtn.SetSourceRect(new Rectangle(icon * 32, 0, 32, 32));
            cbtn.SetTooltip(tooltip);
            cbtn.OnClick += fn;
            AddChild(cbtn);
            // open the first ship console in the list (settings is at index 0)
            if (GetChildrenCount() == 2)
                fn(cbtn);
        }

    }

}
