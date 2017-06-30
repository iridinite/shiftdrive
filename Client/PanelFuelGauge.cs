/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A UI element that displays the player ship's fuel reserves.
    /// </summary>
    internal sealed class PanelFuelGauge : Control {

        protected override void OnDraw(SpriteBatch spriteBatch) {
            // the fuel value's decimal part is the reservoir contents
            var player = NetClient.World.GetPlayerShip();
            var reservoir = player.Fuel - (float)Math.Floor(player.Fuel);

            // fuel cell count icon + text
            spriteBatch.Draw(Assets.GetTexture("ui/itemicons"),
                new Rectangle(SDGame.Inst.GameWidth - 90, 75, 32, 32),
                new Rectangle(32, 0, 32, 32),
                Color.White);
            spriteBatch.DrawString(Assets.fontDefault,
                ((int)Math.Floor(player.Fuel)).ToString(),
                new Vector2(SDGame.Inst.GameWidth - 55, 84),
                Color.White);

            // reservoir bar
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"),
                new Rectangle(SDGame.Inst.GameWidth - 88, (int)(119f + 200f * (1f - reservoir)), 48, (int)(200f * reservoir)),
                new Rectangle(64, (int)(119f + 200f * (1f - reservoir)), 64, (int)(200f * reservoir)),
                Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"),
                new Rectangle(SDGame.Inst.GameWidth - 88, 119, 48, 24),
                new Rectangle(0, 0, 64, 24),
                Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/fillbar"),
                new Rectangle(SDGame.Inst.GameWidth - 88, 295, 48, 24),
                new Rectangle(0, 24, 64, 24),
                Color.White);
        }

    }

}
