/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements an <seealso cref="IForm"/> showing a menu of customizable settings.
    /// </summary>
    internal class FormOptions : IForm {

        private readonly TextButton btn1, btn2, btn3, btn4, btn5, btn6, btnCancel;

        public FormOptions() {
            // create UI controls
            btn1 = new TextButton(0, -1, SDGame.Inst.GameHeight / 2 - 100, 250, 40, Locale.Get("option_1"));
            btn2 = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 - 55, 250, 40, Locale.Get("option_2"));
            btn3 = new TextButton(2, -1, SDGame.Inst.GameHeight / 2 - 10, 250, 40, Locale.Get("option_3"));
            btn4 = new TextButton(3, -1, SDGame.Inst.GameHeight / 2 + 35, 250, 40, Locale.Get("option_4"));
            btn5 = new TextButton(4, -1, SDGame.Inst.GameHeight / 2 + 80, 250, 40, Locale.Get("option_5"));
            btn6 = new TextButton(5, -1, SDGame.Inst.GameHeight / 2 + 125, 250, 40, Locale.Get("option_6"));
            btnCancel = new TextButton(6, -1, SDGame.Inst.GameHeight / 2 + 210, 250, 40, Locale.Get("cancel"));
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw();

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Utils.DrawTitle(spriteBatch);

            if (btnCancel.IsClosed) {
                SDGame.Inst.ActiveForm = new FormMainMenu();

            } else {
                btn1.Draw(spriteBatch);
                btn2.Draw(spriteBatch);
                btn3.Draw(spriteBatch);
                btn4.Draw(spriteBatch);
                btn5.Draw(spriteBatch);
                btn6.Draw(spriteBatch);
                btnCancel.Draw(spriteBatch);
            }

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);
            Utils.UpdateTitle((float)gameTime.ElapsedGameTime.TotalSeconds, -100f);

            btn1.Update(gameTime);
            btn2.Update(gameTime);
            btn3.Update(gameTime);
            btn4.Update(gameTime);
            btn5.Update(gameTime);
            btn6.Update(gameTime);
            btnCancel.Update(gameTime);

            if (btnCancel.IsClosed)
                SDGame.Inst.ActiveForm = new FormMainMenu();
        }

        private void btnCancel_Click(Control sender) {
            btn1.Close();
            btn2.Close();
            btn3.Close();
            btn4.Close();
            btn5.Close();
            btn6.Close();
            btnCancel.Close();
        }

    }

}
