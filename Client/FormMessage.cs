/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form showing an informative message and an option to return to the <seealso cref="FormMainMenu"/>.
    /// </summary>
    internal class FormMessage : Control {

        private readonly string message;
        private readonly Vector2 messageSize;

        public FormMessage(string message) {
            AddChild(new Skybox());

            // store the message
            this.message = Utils.WrapText(Assets.fontDefault, message, SDGame.Inst.GameWidth - 200);
            this.messageSize = Assets.fontDefault.MeasureString(this.message);
            // create UI controls
            var btnCancel = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 + 200, 250, 40, Locale.Get("returntomenu"));
            btnCancel.OnClick += sender => ((Button)sender).Close();
            btnCancel.OnClosed += sender => SDGame.Inst.SetUIRoot(new FormMainMenu());
            AddChild(btnCancel);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.DrawString(Assets.fontDefault, message,
                new Vector2(
                    (int)(SDGame.Inst.GameWidth / 2f - messageSize.X / 2f),
                    SDGame.Inst.GameHeight / 2f - messageSize.Y / 2f),
                Color.White);
        }

    }

}
