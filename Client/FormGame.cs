/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form displaying in-game client state and a linked <seealso cref="ShiftDrive.Console"/>.
    /// </summary>
    internal class FormGame : Control {
        private Console Console { get; set; }

        private float gameOverTime;
        private float gameOverFade;

        public FormGame() {
            DrawMode = ControlDrawMode.ChildrenLast;
            gameOverTime = 4f;
            gameOverFade = 0f;

            // create console switch buttons
            var switcher = new PanelConsoleSwitcher();
            switcher.AddConsoleButton(0, sender => Console = new ConsoleSettings(), Locale.Get("console_settings")); // settings

            // add buttons for each selected role
            Debug.Assert(NetClient.TakenRoles != 0, "no roles selected");
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Helm))
                switcher.AddConsoleButton(1, sender => Console = new ConsoleHelm(), Locale.Get("console_helm"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Weapons))
                switcher.AddConsoleButton(2, sender => Console = new ConsoleWeapons(), Locale.Get("console_wep"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Engineering))
                switcher.AddConsoleButton(3, null, Locale.Get("console_eng"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Quartermaster))
                switcher.AddConsoleButton(4, null, Locale.Get("console_quar"));
            if (NetClient.TakenRoles.HasFlag(PlayerRole.Intelligence))
                switcher.AddConsoleButton(5, sender => Console = new ConsoleIntel(), Locale.Get("console_intel"));

            switcher.AddConsoleButton(6, sender => Console = new ConsoleLrs(), Locale.Get("console_lrs")); // debug LRS
            Children.Add(switcher);
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
                if (player.Destroyed) {
                    gameOverTime -= dt;
                    if (gameOverTime <= 2f) gameOverFade += dt / 2f;
                    if (gameOverTime <= 0f) SDGame.Inst.SetUIRoot(new FormGameOver());
                }

                // update the console itself
                Console.Update(gameTime);
            }
        }
    }

}
