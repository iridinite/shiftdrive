/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormGame : IForm {
        public Console Console { get; set; }

        private readonly List<Button> consoleButtons;
        private float hullFlicker;
        private float hullPrevious;
        private float hullDeclineWait;
        private float hullDecline;

        public FormGame() {
            hullFlicker = 0f;
            hullPrevious = 0f;
            hullDeclineWait = 0f;
            hullDecline = 0f;

            consoleButtons = new List<Button>();
            AddConsoleButton(0, 4, BtnHelm_OnClick); // settings
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Helm))
                AddConsoleButton(1, -1, BtnHelm_OnClick);
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Weapons))
                AddConsoleButton(2, -1, BtnWeap_OnClick);
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Engineering))
                AddConsoleButton(3, -1, BtnWeap_OnClick);
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Quartermaster))
                AddConsoleButton(4, -1, BtnWeap_OnClick);
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Intelligence))
                AddConsoleButton(5, -1, BtnWeap_OnClick);
            AddConsoleButton(6, -1, BtnLRS_OnClick); // debug LRS

            Console = new ConsoleHelm();
        }

        private void AddConsoleButton(int icon, int y, OnClickHandler onClick) {
            // unspecified y means just place at the bottom of the list
            if (y == -1) y = consoleButtons.Count * 40 + 4;
            // create a new button and add it to the list
            ImageButton cbtn = new ImageButton(0, 4, y, 36, 36, Assets.textures["ui/consolebuttons"], Color.Black);
            cbtn.SetSourceRect(new Rectangle(icon * 32, 0, 32, 32));
            cbtn.OnClick += onClick;
            consoleButtons.Add(cbtn);
        }

        private void BtnHelm_OnClick(Control sender) {
            Console = new ConsoleHelm();
        }

        private void BtnWeap_OnClick(Control sender) {
            Console = new ConsoleWeapons();
        }

        private void BtnLRS_OnClick(Control sender) {
            Console = new ConsoleLrs();
        }
        
        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                PlayerShip player = NetClient.World.GetPlayerShip();

                Console.Draw(graphicsDevice, spriteBatch);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                spriteBatch.Draw(Assets.textures["ui/consolepanel"], new Rectangle(0, -496 + consoleButtons.Count * 40, 64, 512), null, Color.White, 0f, Vector2.Zero, SpriteEffects.None, 0f);
                spriteBatch.Draw(Assets.textures["ui/announcepanel"], new Rectangle(SDGame.Inst.GameWidth - 450, -20, 512, 64), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, "Sample announcement text", new Vector2(SDGame.Inst.GameWidth - 430, 12), Color.White);

                // hull integrity bar
                const int hullbarx = 64;
                float hullFraction = player.hull / player.hullMax;
                Color outlineColor = hullFraction <= 0.35f && hullFlicker >= 0.5f ? Color.Red : Color.White;
                Color hullbarColor = hullFraction <= 0.35f ? Color.Red : hullFraction <= 0.7f ? Color.Orange : Color.Green;
                spriteBatch.Draw(Assets.textures["ui/hullbar"], new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 64, 512, 64), Color.White);
                spriteBatch.Draw(Assets.textures["ui/hullbar"], new Rectangle(hullbarx, 0, (int)(hullDecline / player.hullMax * 512f), 64), new Rectangle(0, 128, (int)(hullDecline / player.hullMax * 512f), 64), Color.Purple);
                spriteBatch.Draw(Assets.textures["ui/hullbar"], new Rectangle(hullbarx, 0, (int)(hullFraction * 512f), 64), new Rectangle(0, 128, (int)(hullFraction * 512f), 64), hullbarColor);
                spriteBatch.Draw(Assets.textures["ui/hullbar"], new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 0, 512, 64), outlineColor);
                spriteBatch.DrawString(Assets.fontDefault, (int)(hullFraction * 100f) + "%", new Vector2(hullbarx + 472, 34), Color.Black);
                spriteBatch.DrawString(Assets.fontDefault, (int)(hullFraction * 100f) + "%", new Vector2(hullbarx + 470, 32), outlineColor);

                foreach (Button b in consoleButtons)
                    b.Draw(spriteBatch);
                
                spriteBatch.End();
            }
        }

        public void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lock (NetClient.worldLock) {
                Console.Update(gameTime);

                // show and animate negative changes to hull integrity.
                PlayerShip player = NetClient.World.GetPlayerShip();
                if (player.hull > hullDecline) { // no animation for upped hull
                    hullDecline = player.hull;
                    hullPrevious = player.hull;
                }
                if (hullDeclineWait > 0f)
                    hullDeclineWait -= dt;
                if (hullDecline > player.hull) { // there is still a decline bar to show
                    if (hullPrevious > player.hull) { // if we just took damage, wait before declining
                        hullDeclineWait = 0.75f;
                        hullPrevious = player.hull;
                    }
                    if (hullDeclineWait <= 0f) // animate the hull bar declination
                        hullDecline = MathHelper.Max(player.hull, hullDecline - dt * player.hullMax * 0.2f);
                }


                foreach (Button b in consoleButtons)
                    b.Update(gameTime);
            }

            hullFlicker += dt;
            if (hullFlicker >= 1f) hullFlicker = 0f;
        }
    }

}
