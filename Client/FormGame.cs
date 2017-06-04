/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form displaying in-game client state and a linked <seealso cref="ShiftDrive.Console"/>.
    /// </summary>
    internal class FormGame : Control {
        private Console Console { get; set; }

        private readonly List<Button> consoleButtons;

        private float gameOverTime;
        private float gameOverFade;

        public FormGame() {
            gameOverTime = 4f;
            gameOverFade = 0f;

            // create console switch buttons
            consoleButtons = new List<Button>();
            AddConsoleButton(0, 4, sender => Console = new ConsoleSettings(), Locale.Get("console_settings")); // settings

            //
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Helm))
                AddConsoleButton(1, -1, sender => Console = new ConsoleHelm(), Locale.Get("console_helm"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Weapons))
                AddConsoleButton(2, -1, sender => Console = new ConsoleWeapons(), Locale.Get("console_wep"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Engineering))
                AddConsoleButton(3, -1, null, Locale.Get("console_eng"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Quartermaster))
                AddConsoleButton(4, -1, null, Locale.Get("console_quar"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Intelligence))
                AddConsoleButton(5, -1, null, Locale.Get("console_intel"));

            Debug.Assert(consoleButtons.Count > 1, "no Console after FormGame init");
            AddConsoleButton(6, -1, sender => Console = new ConsoleLrs(), Locale.Get("console_lrs")); // debug LRS
        }

        private void AddConsoleButton(int icon, int y, OnClickHandler onClick, string tooltip) {
            // unspecified y means just place at the bottom of the list
            if (y == -1) y = consoleButtons.Count * 40 + 4;
            // create a new button and add it to the list
            ImageButton cbtn = new ImageButton(-1, 4, y, 36, 36, Assets.GetTexture("ui/consolebuttons"), Color.Black);
            cbtn.SetSourceRect(new Rectangle(icon * 32, 0, 32, 32));
            cbtn.SetTooltip(tooltip);
            cbtn.OnClick += onClick;
            consoleButtons.Add(cbtn);
            // open the first ship console in the list (settings is at index 0)
            if (consoleButtons.Count == 2)
                onClick(cbtn);
        }

        protected override void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                Console.Render(graphicsDevice, spriteBatch);
            }
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                Console.Draw(spriteBatch);
            }

            // black overlay when fading out
            if (gameOverFade > 0f)
                spriteBatch.Draw(Assets.GetTexture("ui/rect"),
                    new Rectangle(0, 0, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight),
                    Color.Black * gameOverFade);
        }

        protected override void OnUpdate(GameTime gameTime) {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lock (NetClient.worldLock) {
                // switch to game-over screen upon ship destruction
                var player = NetClient.World.GetPlayerShip();
                if (player.destroyed) {
                    gameOverTime -= dt;
                    if (gameOverTime <= 2f) gameOverFade += dt / 2f;
                    if (gameOverTime <= 0f) SDGame.Inst.SetUIRoot(new FormGameOver());
                } else {
                    // only process input when we're still alive
                    // update the console itself
                    Console.Update(gameTime);
                }
            }
        }
    }

}
