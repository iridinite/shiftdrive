/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormOptions : IForm {
        
        private readonly Button btn1, btn2, btn3, btn4, btn5, btn6, btnCancel;
        
        public FormOptions() {
            // create UI controls
            btn1 = new Button(0, -1, SDGame.Inst.GameHeight / 2 - 100, 250, 40, Utils.LocaleGet("option_1"));
            btn2 = new Button(1, -1, SDGame.Inst.GameHeight / 2 - 55, 250, 40, Utils.LocaleGet("option_2"));
            btn3 = new Button(2, -1, SDGame.Inst.GameHeight / 2 - 10, 250, 40, Utils.LocaleGet("option_3"));
            btn4 = new Button(3, -1, SDGame.Inst.GameHeight / 2 + 35, 250, 40, Utils.LocaleGet("option_4"));
            btn5 = new Button(4, -1, SDGame.Inst.GameHeight / 2 + 80, 250, 40, Utils.LocaleGet("option_5"));
            btn6 = new Button(5, -1, SDGame.Inst.GameHeight / 2 + 125, 250, 40, Utils.LocaleGet("option_6"));
            btnCancel = new Button(6, -1, SDGame.Inst.GameHeight / 2 + 210, 250, 40, Utils.LocaleGet("cancel"));
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            Utils.DrawTitle(spriteBatch, -100f);

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

        private void btnCancel_Click(Button sender) {
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
