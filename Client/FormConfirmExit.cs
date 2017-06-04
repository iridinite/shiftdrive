/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a form requesting confirmation for application termination.
    /// </summary>
    internal sealed class FormConfirmExit : Control {

        private readonly TextButton btnQuit, btnCancel;
        private int leaveAction;

        public FormConfirmExit() {
            Children.Add(new Skybox());
            Children.Add(new PanelGameTitle(0f));

            // create UI controls
            btnQuit = new TextButton(0, SDGame.Inst.GameWidth / 2 - 185, SDGame.Inst.GameHeight / 2 + 100, 180, 40, Locale.Get("confirmexit_yes"));
            btnQuit.OnClick += btnConnect_Click;
            Children.Add(btnQuit);
            btnCancel = new TextButton(1, SDGame.Inst.GameWidth / 2 + 5, SDGame.Inst.GameHeight / 2 + 100, 180, 40, Locale.Get("confirmexit_no"));
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
            btnCancel.OnClosed += btnCancel_Closed;
            Children.Add(btnCancel);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            spriteBatch.DrawString(Assets.fontDefault, Locale.Get("confirmexit"),
                new Vector2((int)(SDGame.Inst.GameWidth / 2f - Assets.fontDefault.MeasureString(Locale.Get("confirmexit")).X / 2f),
                    SDGame.Inst.GameHeight / 2f), Color.White);
        }

        private void btnConnect_Click(Control sender) {
            leaveAction = 0;
            btnQuit.Close();
            btnCancel.Close();
        }

        private void btnCancel_Click(Control sender) {
            leaveAction = 1;
            btnQuit.Close();
            btnCancel.Close();
        }

        private void btnCancel_Closed(Control sender) {
            if (leaveAction == 0) {
                SDGame.Inst.Exit();
            } else {
                SDGame.Inst.SetUIRoot(new FormMainMenu());
            }
        }

    }

}
