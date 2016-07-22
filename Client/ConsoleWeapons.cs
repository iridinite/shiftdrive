﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {
    
    /// <summary>
    /// The interface for the Weapons console.
    /// </summary>
    internal sealed class ConsoleWeapons : Console {

        public override void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            graphicsDevice.Clear(Color.FromNonPremultiplied(0, 0, 32, 255));

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            DrawLocalArea(spriteBatch);
            DrawFuelGauge(spriteBatch);

            // draw a list of currently active weapons
            int weaponBoxY = SDGame.Inst.GameHeight - Player.weaponMax * 90 - 20;
            for (int i = 0; i < Player.weaponMax; i++) {
                // draw box background
                int weaponCurrentY = weaponBoxY + i * 90;
                spriteBatch.Draw(Assets.txRect, new Rectangle(0, weaponCurrentY, 230, 80), Color.Gray);

                // find the weapon in this index
                Weapon wep = Player.weapons[i];
                if (wep == null) {
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("no_weapon"), new Vector2(18, weaponCurrentY + 10), Color.FromNonPremultiplied(48, 48, 48, 255));
                    continue;
                }
                
                // draw weapon details
                spriteBatch.Draw(Assets.txItemIcons, new Rectangle(188, weaponCurrentY + 10, 32, 32), new Rectangle((int)wep.Type * 32, 32, 32, 32), Color.White);
                spriteBatch.Draw(Assets.txChargeBar, new Rectangle(16, weaponCurrentY + 55, 128, 16), new Rectangle(0, 16, 128, 16), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, wep.Name, new Vector2(18, weaponCurrentY + 10), Color.Black);
                spriteBatch.DrawString(Assets.fontDefault, wep.Name, new Vector2(16, weaponCurrentY + 8), Color.White);
            }

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
        }

    }

}