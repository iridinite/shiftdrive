/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements an <seealso cref="IForm"/> showing game-over state before returning to the <seealso cref="FormLobby"/>.
    /// </summary>
    internal class FormGameOver : IForm {

        private enum GameOverAnimState {
            HoldPre,
            FadeIn,
            HoldIn,
            FadeOut,
            HoldOut
        }

        private readonly List<string> quoteLines;
        private readonly string quoteAuthor;
        private GameOverAnimState state;
        private float fadeCoeff;
        private float holdTime;

        public FormGameOver() {
            // initialize animation
            state = GameOverAnimState.HoldPre;
            fadeCoeff = 0f;
            holdTime = 1f;
            // pick a random quote from the list
            // note: each line is transformed to uppercase. this could be hardcoded into the
            // string table, but seeing as this one-time operation is cheap and proper-case
            // improves the table's readability, I'm opting to do it here instead.
            int quoteNum = Utils.RNG.Next(1, 7);
            quoteAuthor = Locale.Get("deathquote_" + quoteNum + "b").ToUpperInvariant();
            quoteLines = new List<string>(3);
            // split the full quote line into words
            string quoteFullLine = Locale.Get("deathquote_" + quoteNum + "a").ToUpperInvariant();
            string[] quoteParts = quoteFullLine.Split(' ');
            // assemble the words back into lines with line-breaks
            StringBuilder sb = new StringBuilder(128);
            foreach (string word in quoteParts) {
                sb.Append(word);
                sb.Append(' ');

                if (Assets.fontQuote.MeasureString(sb.ToString()).X < 600f) continue;
                quoteLines.Add(sb.ToString());
                sb.Clear();
            }
            quoteLines.Add(sb.ToString());
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // draw each line from the selected quote
            int xoffset = SDGame.Inst.GameWidth / 2;
            int yoffset = SDGame.Inst.GameHeight / 2 - 60 - (int)(40f * fadeCoeff);
            foreach (string line in quoteLines) {
                Vector2 linesize = Assets.fontQuote.MeasureString(line);
                spriteBatch.DrawString(Assets.fontQuote, line,
                    new Vector2(xoffset - (int)(linesize.X / 2f) + 2, yoffset + 2),
                    Color.FromNonPremultiplied(96, 96, 96, 255) * fadeCoeff);
                spriteBatch.DrawString(Assets.fontQuote, line, new Vector2(xoffset - (int)(linesize.X / 2f), yoffset),
                    Color.White * fadeCoeff);

                yoffset += (int)linesize.Y + 8;
            }
            // and the author name underneath
            spriteBatch.DrawString(Assets.fontBold, quoteAuthor,
                new Vector2(xoffset - (int)(Assets.fontBold.MeasureString(quoteAuthor).X / 2f), yoffset + 30),
                Color.DarkGray * fadeCoeff);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            switch (state) {
                case GameOverAnimState.HoldPre:
                    // hold text on the screen
                    holdTime -= dt;
                    if (holdTime < 0f) state = GameOverAnimState.FadeIn;
                    break;

                case GameOverAnimState.FadeIn:
                    // scroll the text upwards and fade it in
                    fadeCoeff += (1f - fadeCoeff) * 0.75f * dt;
                    if (fadeCoeff >= 0.9f) {
                        fadeCoeff = 0.9f;
                        holdTime = 5f;
                        state = GameOverAnimState.HoldIn;
                    }
                    break;

                case GameOverAnimState.HoldIn:
                    // hold text on the screen
                    holdTime -= dt;
                    if (holdTime < 0f) state = GameOverAnimState.FadeOut;
                    break;

                case GameOverAnimState.FadeOut:
                    // linearly fade back out
                    fadeCoeff -= dt * 0.5f;
                    if (fadeCoeff <= 0f) {
                        fadeCoeff = 0f;
                        holdTime = 3f;
                        state = GameOverAnimState.HoldOut;
                    }
                    break;

                case GameOverAnimState.HoldOut:
                    // hold black screen for a few seconds
                    holdTime -= dt;
                    if (holdTime < 0f) SDGame.Inst.ActiveForm = new FormLobby();
                    break;
            }
        }

    }

}
