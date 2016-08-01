/*
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
            graphicsDevice.Clear(Color.Black);
            DrawLocalArea(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            DrawFuelGauge(spriteBatch);

            // draw a list of currently active weapons
            int weaponBoxY = SDGame.Inst.GameHeight - Player.mountsNum * 90 - 20;
            for (int i = 0; i < Player.mountsNum; i++) {
                // draw box background
                int weaponCurrentY = weaponBoxY + i * 90;
                spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle(0, weaponCurrentY, 230, 80), Color.Gray);

                // find the weapon in this index
                Weapon wep = Player.weapons[i];
                if (wep == null) {
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("no_weapon"), new Vector2(18, weaponCurrentY + 10), Color.FromNonPremultiplied(48, 48, 48, 255));
                    continue;
                }
                
                // draw weapon details
                spriteBatch.Draw(Assets.textures["ui/itemicons"], new Rectangle(188, weaponCurrentY + 10, 32, 32), new Rectangle((int)wep.DamageType * 32, 32, 32, 32), Color.White);
                spriteBatch.Draw(Assets.textures["ui/chargebar"], new Rectangle(16, weaponCurrentY + 55, 128, 16), new Rectangle(0, 16, 128, 16), Color.White);
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