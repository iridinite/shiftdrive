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
            Button btnHelm = new Button(0, 3, 3, 100, 35, "HELM");
            btnHelm.OnClick += BtnHelm_OnClick;
            consoleButtons.Add(btnHelm);
            Button btnWeap = new Button(0, 105, 3, 100, 35, "WEAP");
            btnWeap.OnClick += BtnWeap_OnClick;
            consoleButtons.Add(btnWeap);
            Button btnLRS = new Button(0, 207, 3, 100, 35, "LRS");
            btnLRS.OnClick += BtnLRS_OnClick;
            consoleButtons.Add(btnLRS);

            Console = new ConsoleHelm();
        }

        private void BtnHelm_OnClick(Button sender) {
            Console = new ConsoleHelm();
        }

        private void BtnWeap_OnClick(Button sender) {
            Console = new ConsoleWeapons();
        }

        private void BtnLRS_OnClick(Button sender) {
            Console = new ConsoleLrs();
        }
        
        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                PlayerShip player = NetClient.World.GetPlayerShip();

                Console.Draw(graphicsDevice, spriteBatch);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                spriteBatch.Draw(Assets.textures["ui/announcepanel"], new Rectangle(-490 + (consoleButtons.Count * 105), 0, 512, 64), null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                spriteBatch.Draw(Assets.textures["ui/announcepanel"], new Rectangle(SDGame.Inst.GameWidth - 450, 0, 512, 64), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, "Sample announcement text", new Vector2(SDGame.Inst.GameWidth - 430, 12), Color.White);

                // hull integrity bar
                int hullbarx = MathHelper.Max((consoleButtons.Count * 105) + 40, SDGame.Inst.GameWidth / 2 - 350);
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
