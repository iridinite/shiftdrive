/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Implements a <seealso cref="Console"/> showing a map overview for debugging purposes.
    /// </summary>
    internal sealed class ConsoleLrs : Console {

        protected override void OnDraw(SpriteBatch spriteBatch) {
            int gridSize = MathHelper.Min(SDGame.Inst.GameWidth, SDGame.Inst.GameHeight) - 128;
            Vector2 gridPos = new Vector2(SDGame.Inst.GameWidth / 2 - gridSize / 2, SDGame.Inst.GameHeight / 2 - gridSize / 2);

            // draw map icons for all game objects
            spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle((int)gridPos.X, (int)gridPos.Y, gridSize, gridSize), Color.DarkOliveGreen);
            foreach (GameObject obj in NetClient.World.Objects.Values) {
                spriteBatch.Draw(Assets.GetTexture("ui/rect"), gridPos + new Vector2(gridSize * (obj.Position.X / NetServer.MAPSIZE), gridSize * (obj.Position.Y / NetServer.MAPSIZE)), null, Color.White, MathHelper.ToRadians(obj.Facing), new Vector2(16, 16), 0.25f, SpriteEffects.None, 0f);
            }

            // draw info underneath mouse cursor
            Vector2 playerPosition = NetClient.World.GetPlayerShip().Position;
            int mouseMapX = (int)((Input.MouseX - gridPos.X) / gridSize * NetServer.MAPSIZE);
            int mouseMapY = (int)((Input.MouseY - gridPos.Y) / gridSize * NetServer.MAPSIZE);
            spriteBatch.DrawString(Assets.fontDefault, "POS " + mouseMapX + ", " + mouseMapY, new Vector2(Input.MouseX, Input.MouseY + 25), Color.LightYellow);
            spriteBatch.DrawString(Assets.fontDefault, "DIR " +
                (int)Utils.CalculateBearing(new Vector2(playerPosition.X, playerPosition.Y),
                    new Vector2(mouseMapX, mouseMapY)),
                new Vector2(Input.MouseX, Input.MouseY + 50),
                Color.LightYellow);
        }

        protected override void OnUpdate(GameTime gameTime) {}

    }

}
