/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

namespace ShiftDrive {

    /// <summary>
    /// Implements a form showing a menu of customizable settings.
    /// </summary>
    internal class FormOptions : Control {

        private readonly TextButton btn1, btn2, btn3, btn4, btn5, btn6, btnCancel;

        public FormOptions() {
            AddChild(new Skybox());
            AddChild(new PanelGameTitle(-100f));

            // create UI controls
            btn1 = new TextButton(0, -1, SDGame.Inst.GameHeight / 2 - 100, 250, 40, Locale.Get("option_1"));
            btn2 = new TextButton(1, -1, SDGame.Inst.GameHeight / 2 - 55, 250, 40, Locale.Get("option_2"));
            btn3 = new TextButton(2, -1, SDGame.Inst.GameHeight / 2 - 10, 250, 40, Locale.Get("option_3"));
            btn4 = new TextButton(3, -1, SDGame.Inst.GameHeight / 2 + 35, 250, 40, Locale.Get("option_4"));
            btn5 = new TextButton(4, -1, SDGame.Inst.GameHeight / 2 + 80, 250, 40, Locale.Get("option_5"));
            btn6 = new TextButton(5, -1, SDGame.Inst.GameHeight / 2 + 125, 250, 40, Locale.Get("option_6"));
            btnCancel = new TextButton(6, -1, SDGame.Inst.GameHeight / 2 + 210, 250, 40, Locale.Get("cancel"));
            AddChild(btn1);
            AddChild(btn2);
            AddChild(btn3);
            AddChild(btn4);
            AddChild(btn5);
            AddChild(btn6);
            btnCancel.CancelSound = true;
            btnCancel.OnClick += btnCancel_Click;
            btnCancel.OnClosed += sender => SDGame.Inst.SetUIRoot(new FormMainMenu());
            AddChild(btnCancel);
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
