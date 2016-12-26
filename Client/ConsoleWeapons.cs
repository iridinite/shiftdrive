/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> for the weapon officer's station.
    /// </summary>
    internal sealed class ConsoleWeapons : Console {

        public override void Draw(SpriteBatch spriteBatch) {
            DrawLocalArea(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            DrawFuelGauge(spriteBatch);

            // draw a list of currently active weapons
            int weaponBoxX = SDGame.Inst.GameWidth / 2 - Player.mountsNum * 80;
            for (int i = 0; i < Player.mountsNum; i++) {
                // draw box background
                int weaponCurrentX = weaponBoxX + i * 160;
                spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle(weaponCurrentX, SDGame.Inst.GameHeight - 60, 150, 60), Color.DimGray);

                // find the weapon in this index
                Weapon wep = Player.weapons[i];
                if (wep == null) {
                    spriteBatch.DrawString(Assets.fontDefault, Utils.LocaleGet("no_weapon"), new Vector2(weaponCurrentX + 8, SDGame.Inst.GameHeight - 52), Color.FromNonPremultiplied(48, 48, 48, 255));
                    continue;
                }

                // draw weapon details
                int chargeWidth = (int)(128 * (wep.Charge / wep.ChargeTime));
                if (wep.ChargeTime > 0.5f || wep.Ammo == AmmoType.None) {
                    // for weapons with a long charge, draw a charge bar
                    spriteBatch.Draw(Assets.textures["ui/chargebar"], new Rectangle(weaponCurrentX + 11, SDGame.Inst.GameHeight - 32, chargeWidth, 16), new Rectangle(0, 0, chargeWidth, 16), Color.LightGoldenrodYellow);
                    spriteBatch.Draw(Assets.textures["ui/chargebar"], new Rectangle(weaponCurrentX + 11, SDGame.Inst.GameHeight - 32, 128, 16), new Rectangle(0, 16, 128, 16), Color.White);
                } else {
                    // for rapid fire weapons, a charge bar is pointless, so draw ammo tally instead
                    //int ammoInClip = wep.AmmoLeft - (wep.AmmoLeft / wep.AmmoPerClip - 1) * wep.AmmoPerClip;
                    spriteBatch.DrawString(Assets.fontTooltip, wep.AmmoLeft + " / " + wep.AmmoPerClip, new Vector2(weaponCurrentX + 16, SDGame.Inst.GameHeight - 32), wep.AmmoLeft == 0 ? Color.Orange : Color.White);
                    spriteBatch.DrawString(Assets.fontTooltip, "x " + wep.AmmoClipsLeft, new Vector2(weaponCurrentX + 110, SDGame.Inst.GameHeight - 32), wep.AmmoClipsMax == 0 ? Color.Orange : Color.White);
                    // alert text for reloading / out of ammo
                    if (wep.AmmoLeft == 0 && wep.ReloadProgress > 0f)
                        spriteBatch.DrawString(Assets.fontTooltip, Utils.LocaleGet("reloading"), new Vector2(weaponCurrentX + 24, SDGame.Inst.GameHeight - 16), Color.Orange);
                    else if (wep.AmmoLeft == 0 && wep.AmmoClipsLeft == 0)
                        spriteBatch.DrawString(Assets.fontTooltip, Utils.LocaleGet("outofammo"), new Vector2(weaponCurrentX + 24, SDGame.Inst.GameHeight - 16), Color.Orange);
                }
                spriteBatch.DrawString(Assets.fontTooltip, wep.Name, new Vector2(weaponCurrentX + 9, SDGame.Inst.GameHeight - 51), Color.Black);
                spriteBatch.DrawString(Assets.fontTooltip, wep.Name, new Vector2(weaponCurrentX + 8, SDGame.Inst.GameHeight - 52), Color.White);
            }

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
            base.Update(gameTime);
        }

    }

}