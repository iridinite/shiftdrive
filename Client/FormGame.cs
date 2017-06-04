﻿/*
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
        public Console Console { get; set; }

        private readonly List<Button> consoleButtons;
        private float hullFlicker;
        private float hullPrevious;
        private float hullDeclineWait;
        private float hullDecline;

        private float gameOverTime;
        private float gameOverFade;

        private string announceText;
        private float announceHoldTime;

        public FormGame() {
            hullFlicker = 0f;
            hullPrevious = 0f;
            hullDeclineWait = 0f;
            hullDecline = 0f;
            announceHoldTime = 0f;
            gameOverTime = 4f;
            gameOverFade = 0f;
            announceText = "";

            // subscribe to networking events
            NetClient.Announcement += NetClient_Announcement;

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

        private void NetClient_Announcement(string text) {
            announceHoldTime = 10f;
            announceText = text;
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

        protected override void OnDraw(SpriteBatch spriteBatch) {
            lock (NetClient.worldLock) {
                PlayerShip player = NetClient.World.GetPlayerShip();

                Console.Draw(spriteBatch);
                
                spriteBatch.Draw(Assets.GetTexture("ui/consolepanel"), new Rectangle(0, -496 + consoleButtons.Count * 40, 64, 512), null, Color.White,
                    0f, Vector2.Zero, SpriteEffects.None, 0f);
                spriteBatch.Draw(Assets.GetTexture("ui/announcepanel"), new Rectangle(SDGame.Inst.GameWidth - 450, -20, 512, 64), Color.White);
                spriteBatch.DrawString(Assets.fontDefault, announceText, new Vector2(SDGame.Inst.GameWidth - 430, 12), Color.White);

                // hull integrity bar
                const int hullbarx = 64;
                float hullFraction = player.hull / player.hullMax;
                float shieldFraction = player.shield / player.shieldMax;
                Color outlineColor = hullFraction <= 0.35f && hullFlicker >= 0.5f ? Color.Red : Color.White;
                Color hullbarColor = hullFraction <= 0.35f ? Color.Red : hullFraction <= 0.7f ? Color.Orange : Color.Green;
                // background
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 64, 512, 64), Color.White);
                // hull
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx, 0, (int)(hullDecline / player.hullMax * 512f), 64),
                    new Rectangle(0, 128, (int)(hullDecline / player.hullMax * 512f), 64),
                    Color.Purple);
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx, 0, (int)(hullFraction * 512f), 64),
                    new Rectangle(0, 128, (int)(hullFraction * 512f), 64),
                    hullbarColor);
                // shields
                int shieldPixels = (int)(shieldFraction * 352f); // chop off 160 pixels left
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"),
                    new Rectangle(hullbarx + 160 + 352 - shieldPixels, 0, shieldPixels, 64),
                    new Rectangle(160 + 352 - shieldPixels, 192, shieldPixels, 64),
                    player.shieldActive ? Color.LightSkyBlue : Color.Gray);
                // outline
                spriteBatch.Draw(Assets.GetTexture("ui/hullbar"), new Rectangle(hullbarx, 0, 512, 64), new Rectangle(0, 0, 512, 64), outlineColor);
                
                // black overlay when fading out
                if (gameOverFade > 0f)
                    spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(0, 0, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight),
                        Color.Black * gameOverFade);
            }
        }

        protected override void OnUpdate(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            lock (NetClient.worldLock) {
                // show and animate negative changes to hull integrity.
                PlayerShip player = NetClient.World.GetPlayerShip();
                if (player.hull > hullDecline) {
                    // no animation for upped hull
                    hullDecline = player.hull;
                    hullPrevious = player.hull;
                }
                if (hullDeclineWait > 0f)
                    hullDeclineWait -= dt;
                if (hullDecline > player.hull) {
                    // there is still a decline bar to show
                    if (hullPrevious > player.hull) {
                        // if we just took damage, wait before declining
                        hullDeclineWait = 0.75f;
                        hullPrevious = player.hull;
                    }
                    if (hullDeclineWait <= 0f) // animate the hull integrity loss
                        hullDecline = MathHelper.Max(player.hull, hullDecline - dt * player.hullMax * 0.2f);
                }

                // animate announcement text
                announceHoldTime -= dt;
                if (announceHoldTime < 0f)
                    announceText = "";

                // switch to game-over screen upon ship destruction
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

            hullFlicker += dt;
            if (hullFlicker >= 1f) hullFlicker = 0f;
        }
    }

}
