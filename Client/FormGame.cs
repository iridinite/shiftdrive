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

        public FormGame() {
            hullFlicker = 0f;
            hullPrevious = 0f;

            consoleButtons = new List<Button>();
            Button btnHelm = new Button(0, 3, 3, 100, 35, "HELM");
            btnHelm.OnClick += BtnHelm_OnClick;
            consoleButtons.Add(btnHelm);
            Button btnLRS = new Button(0, 105, 3, 100, 35, "LRS");
            btnLRS.OnClick += BtnLRS_OnClick;
            consoleButtons.Add(btnLRS);

            Console = new ConsoleHelm();
        }

        private void BtnHelm_OnClick(Button sender) {
            Console = new ConsoleHelm();
        }

        private void BtnLRS_OnClick(Button sender) {
            Console = new ConsoleLrs();
        }

        private void DrawHullbarPiece(int x, int smin, int smax, float hullmin, float hullmax) {
            
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                PlayerShip player = NetClient.World.GetPlayerShip();

                Console.Draw(graphicsDevice, spriteBatch);

                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

                spriteBatch.Draw(Assets.txAnnouncePanel, new Rectangle(-490 + (consoleButtons.Count * 105), 0, 512, 64), null, Color.White, 0f, Vector2.Zero, SpriteEffects.FlipHorizontally, 0f);
                spriteBatch.Draw(Assets.txAnnouncePanel, new Rectangle(SDGame.Inst.GameWidth - 450, 0, 512, 64), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, "Sample announcement text", new Vector2(SDGame.Inst.GameWidth - 430, 12), Color.White);

                // hull integrity bar
                int hullbarx = MathHelper.Max((consoleButtons.Count * 105) + 40, SDGame.Inst.GameWidth / 2 - 350);
                float hullFraction = player.hull / player.hullMax;
                //int hullbarw = SDGame.Inst.GameWidth - 550 - hullbarx;
                //int hullbarsegment =
                Color outlineColor = player.hull <= 0.35f && hullFlicker >= 0.5f ? Color.Red : Color.White;
                Color hullbarColor = player.hull <= 0.35f ? Color.Red : player.hull <= 0.7f ? Color.Orange : Color.Green;
                spriteBatch.Draw(Assets.txHullBar, new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 64, 512, 64), Color.White);
                spriteBatch.Draw(Assets.txHullBar, new Rectangle(hullbarx, 0, (int)(hullPrevious / player.hullMax * 512f), 64), new Rectangle(0, 128, (int)(hullPrevious / player.hullMax * 512f), 64), Color.DarkRed);
                spriteBatch.Draw(Assets.txHullBar, new Rectangle(hullbarx, 0, (int)(hullFraction * 512f), 64), new Rectangle(0, 128, (int)(hullFraction * 512f), 64), hullbarColor);
                spriteBatch.Draw(Assets.txHullBar, new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 0, 512, 64), outlineColor);
                spriteBatch.DrawString(Assets.fontDefault, (int)(hullFraction * 100f) + "%", new Vector2(hullbarx + 472, 34), Color.Black);
                spriteBatch.DrawString(Assets.fontDefault, (int)(hullFraction * 100f) + "%", new Vector2(hullbarx + 470, 32), outlineColor);

                foreach (Button b in consoleButtons)
                    b.Draw(spriteBatch);

                //spriteBatch.DrawString(Assets.fontDefault, "pos: " + NetClient.World.GetPlayerShip().position.ToString(), new Vector2(115, 415), Color.White);
                //spriteBatch.DrawString(Assets.fontDefault, "bng: " + NetClient.World.GetPlayerShip().facing.ToString(), new Vector2(115, 435), Color.White);
                //spriteBatch.DrawString(Assets.fontDefault, "sri: " + NetClient.World.GetPlayerShip().steering.ToString(), new Vector2(115, 455), Color.White);

                spriteBatch.End();
            }
        }

        public void Update(GameTime gameTime) {
            lock (NetClient.worldLock) {
                Console.Update(gameTime);

                // show and animate negative changes to hull integrity.
                PlayerShip player = NetClient.World.GetPlayerShip();
                if (player.hull > hullPrevious) // no animation for upped hull
                    hullPrevious = player.hull;
                if (hullPrevious > player.hull) // player just took damage
                    hullPrevious = MathHelper.Max(player.hull, hullPrevious - (float)(gameTime.ElapsedGameTime.TotalSeconds * player.hullMax * 0.2f));

                foreach (Button b in consoleButtons)
                    b.Update(gameTime);
            }

            hullFlicker += (float) gameTime.ElapsedGameTime.TotalSeconds;
            if (hullFlicker >= 1f) hullFlicker = 0f;
        }
    }

}
