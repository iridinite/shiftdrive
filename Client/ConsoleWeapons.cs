/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> for the weapon officer's station.
    /// </summary>
    internal sealed class ConsoleWeapons : Console {

        private readonly PanelWorldView worldView;

        public ConsoleWeapons() {
            worldView = new PanelWorldView(SDGame.Inst.GameWidth, SDGame.Inst.GameHeight);
            Children.Add(worldView);
            Children.Add(new PanelHullBar());
            Children.Add(new PanelAnnounce());
            Children.Add(new PanelFuelGauge());

            var btnShields = new TextButton(-1, SDGame.Inst.GameWidth - 200, 400, 120, 40, Locale.Get("shields_toggle"));
            btnShields.OnClick += btnShields_Click;
            Children.Add(btnShields);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            var player = NetClient.World.GetPlayerShip();

            // draw a list of currently active weapons
            int weaponBoxX = SDGame.Inst.GameWidth / 2 - player.weaponsNum * 80;
            for (int i = 0; i < player.weaponsNum; i++) {
                // draw box background
                int weaponCurrentX = weaponBoxX + i * 160;
                spriteBatch.Draw(Assets.GetTexture("ui/weaponbg"), new Rectangle(weaponCurrentX, SDGame.Inst.GameHeight - 60, 150, 60), Color.DimGray);

                // find the weapon in this index
                Weapon wep = player.weapons[i];
                if (wep == null) {
                    spriteBatch.DrawString(Assets.fontDefault, Locale.Get("no_weapon"), new Vector2(weaponCurrentX + 8, SDGame.Inst.GameHeight - 52), Color.FromNonPremultiplied(48, 48, 48, 255));
                    continue;
                }

                // draw weapon details
                int chargeWidth = (int)(128 * (wep.Charge / wep.ChargeTime));
                if (wep.ChargeTime > 0.5f || wep.Ammo == AmmoType.None) {
                    // for weapons with a long charge, draw a charge bar
                    spriteBatch.Draw(Assets.GetTexture("ui/chargebar"), new Rectangle(weaponCurrentX + 11, SDGame.Inst.GameHeight - 32, chargeWidth, 16), new Rectangle(0, 0, chargeWidth, 16), Color.LightGoldenrodYellow);
                    spriteBatch.Draw(Assets.GetTexture("ui/chargebar"), new Rectangle(weaponCurrentX + 11, SDGame.Inst.GameHeight - 32, 128, 16), new Rectangle(0, 16, 128, 16), Color.White);
                } else {
                    // for rapid fire weapons, a charge bar is pointless, so draw ammo tally instead
                    //int ammoInClip = wep.AmmoLeft - (wep.AmmoLeft / wep.AmmoPerClip - 1) * wep.AmmoPerClip;
                    spriteBatch.DrawString(Assets.fontTooltip, wep.AmmoLeft + " / " + wep.AmmoPerClip, new Vector2(weaponCurrentX + 16, SDGame.Inst.GameHeight - 32), wep.AmmoLeft == 0 ? Color.Orange : Color.White);
                    spriteBatch.DrawString(Assets.fontTooltip, "x " + wep.AmmoClipsLeft, new Vector2(weaponCurrentX + 110, SDGame.Inst.GameHeight - 32), wep.AmmoClipsMax == 0 ? Color.Orange : Color.White);
                    // alert text for reloading / out of ammo
                    if (wep.AmmoLeft == 0 && wep.ReloadProgress > 0f)
                        spriteBatch.DrawString(Assets.fontTooltip, Locale.Get("reloading"), new Vector2(weaponCurrentX + 24, SDGame.Inst.GameHeight - 16), Color.Orange);
                    else if (wep.AmmoLeft == 0 && wep.AmmoClipsLeft == 0)
                        spriteBatch.DrawString(Assets.fontTooltip, Locale.Get("outofammo"), new Vector2(weaponCurrentX + 24, SDGame.Inst.GameHeight - 16), Color.Orange);
                }
                spriteBatch.DrawString(Assets.fontTooltip, wep.Name, new Vector2(weaponCurrentX + 9, SDGame.Inst.GameHeight - 51), Color.Black);
                spriteBatch.DrawString(Assets.fontTooltip, wep.Name, new Vector2(weaponCurrentX + 8, SDGame.Inst.GameHeight - 52), Color.White);
            }
        }

        protected override void OnUpdate(GameTime gameTime) {
            // ignore input from dead players
            if (NetClient.World.GetPlayerShip().destroyed) return;

            // add targets that the player clicks on
            if (!Input.GetMouseLeftDown()) return;

            var player = NetClient.World.GetPlayerShip();
            foreach (PanelWorldView.TargetableObject tobj in worldView.Targetables) {
                if (Vector2.Distance(Input.MousePosition, tobj.screenpos) > 32) continue;

                using (Packet packet = new Packet(PacketID.WeapTarget)) {
                    packet.Write(tobj.objid);
                    packet.Write(!player.targets.Contains(tobj.objid));
                    NetClient.Send(packet);
                }
            }
        }

        private void btnShields_Click(Control sender) {
            using (Packet packet = new Packet(PacketID.WeapShields))
                NetClient.Send(packet);
        }
    }

}
