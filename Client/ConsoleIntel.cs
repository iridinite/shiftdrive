/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> for the intel officer's station.
    /// </summary>
    internal sealed class ConsoleIntel : Console {

        private readonly PanelWorldView worldView;

        public ConsoleIntel() {
            int waterfallWidth = SDGame.Inst.GameWidth > 1280 ? 400 : 350;

            worldView = new PanelWorldView(SDGame.Inst.GameWidth - waterfallWidth, SDGame.Inst.GameHeight);
            Children.Add(worldView);
            Children.Add(new PanelCommsWaterfall(waterfallWidth));
            Children.Add(new PanelAnnounce());
            Children.Add(new PanelHullBar());

            DrawMode = ControlDrawMode.ChildrenFirst;
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(20, SDGame.Inst.GameHeight - 50, 200, 20), Color.CornflowerBlue);
            spriteBatch.DrawString(Assets.fontDefault, "Zoom:", new Vector2(20, SDGame.Inst.GameHeight - 80), Color.White);
        }

    }

}
