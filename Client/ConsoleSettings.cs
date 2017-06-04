/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> showing an in-game settings menu.
    /// </summary>
    internal sealed class ConsoleSettings : Console {
        public ConsoleSettings() {
            var btnToLobby = new TextButton(-1, -1, 300, 300, 40, Locale.Get("returntolobby"));
            btnToLobby.OnClick += sender => SDGame.Inst.SetUIRoot(new FormLobby());
            Children.Add(btnToLobby);
        }
    }

}
