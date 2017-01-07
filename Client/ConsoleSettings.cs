/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> showing an in-game settings menu.
    /// </summary>
    internal sealed class ConsoleSettings : Console {

        private readonly Button btnToLobby = new TextButton(-1, -1, 300, 300, 40, Locale.Get("returntolobby"));

        public ConsoleSettings() {
            btnToLobby.OnClick += BtnToLobby_OnClick;
        }

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            btnToLobby.Draw(spriteBatch);
            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
            btnToLobby.Update(gameTime);
        }

        private void BtnToLobby_OnClick(Control sender) {
            SDGame.Inst.ActiveForm = new FormLobby();
        }

    }

}
