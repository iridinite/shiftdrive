/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    internal class FormMessage : IForm {
        
        private readonly Button btnCancel;
        private readonly string message;

        public FormMessage(string message) {
            // store the message
            this.message = message;
            // create UI controls
            btnCancel = new Button(1, -1, SDGame.Inst.GameHeight / 2 + 200, 250, 40, Utils.LocaleGet("returntomenu"));
            btnCancel.OnClick += btnCancel_Click;
        }

        public void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            Skybox.Draw(graphicsDevice);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            spriteBatch.DrawString(Assets.fontDefault, message, new Vector2((int)(SDGame.Inst.GameWidth / 2 - Assets.fontDefault.MeasureString(message).X / 2), SDGame.Inst.GameHeight / 2 - 100), Color.White);
            btnCancel.Draw(spriteBatch);

            spriteBatch.End();
        }

        public void Update(GameTime gameTime) {
            Skybox.Update(gameTime);
            
            btnCancel.Update(gameTime);

            if (btnCancel.IsClosed) {
                SDGame.Inst.ActiveForm = new FormMainMenu();
            }
        }
        
        private void btnCancel_Click(Button sender) {
            btnCancel.Close();
        }
        
    }

}
