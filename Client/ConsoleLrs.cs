﻿/*
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

        public override void Draw(SpriteBatch spriteBatch) {
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            int gridSize = MathHelper.Min(SDGame.Inst.GameWidth, SDGame.Inst.GameHeight) - 128;
            Vector2 gridPos = new Vector2(SDGame.Inst.GameWidth / 2 - gridSize / 2, SDGame.Inst.GameHeight / 2 - gridSize / 2);

            // draw map icons for all game objects
            spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle((int)gridPos.X, (int)gridPos.Y, gridSize, gridSize), Color.DarkOliveGreen);
            foreach (GameObject obj in NetClient.World.Objects.Values) {
                spriteBatch.Draw(Assets.GetTexture("ui/rect"), gridPos + new Vector2(gridSize * (obj.position.X / NetServer.MAPSIZE), gridSize * (obj.position.Y / NetServer.MAPSIZE)), null, Color.White, MathHelper.ToRadians(obj.facing), new Vector2(16, 16), 0.25f, SpriteEffects.None, 0f);
            }

            // draw info underneath mouse cursor
            int mouseMapX = (int)((Mouse.X - gridPos.X) / gridSize * NetServer.MAPSIZE);
            int mouseMapY = (int)((Mouse.Y - gridPos.Y) / gridSize * NetServer.MAPSIZE);
            spriteBatch.DrawString(Assets.fontDefault, "POS " + mouseMapX + ", " + mouseMapY, new Vector2(Mouse.X, Mouse.Y + 25), Color.LightYellow);
            spriteBatch.DrawString(Assets.fontDefault, "DIR " +
                (int)Utils.CalculateBearing(new Vector2(Player.position.X, Player.position.Y),
                new Vector2(mouseMapX, mouseMapY)),
                new Vector2(Mouse.X, Mouse.Y + 50),
                Color.LightYellow);

            spriteBatch.End();
        }

        public override void Update(GameTime gameTime) {
        }

    }

}