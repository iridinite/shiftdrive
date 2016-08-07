/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a ship station view to be rendered in <seealso cref="FormGame"/>.
    /// </summary>
    internal abstract class Console {
        public abstract void Draw(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch);
        
        /// <summary>
        /// Shorthand for NetClient.World.GetPlayerShip()
        /// </summary>
        protected static PlayerShip Player { get { return NetClient.World.GetPlayerShip(); } }

        /// <summary>
        /// Draws the local area with the player ship and a radar ring.
        /// </summary>
        /// <param name="spriteBatch">A SpriteBatch to render with. SpriteBatch.Begin must have been successfully called!</param>
        protected void DrawLocalArea(SpriteBatch spriteBatch) {
            const float viewradius = 250f;
            Vector2 min = new Vector2(Player.position.X - viewradius, Player.position.Y - viewradius);
            Vector2 max = new Vector2(Player.position.X + viewradius, Player.position.Y + viewradius);

            // start a sprite batch and keep it open so we can draw object name text
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            spriteBatch.Draw(Assets.GetTexture("back/nebula1"), new Rectangle(0, 0, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight), new Rectangle((int)Player.position.X, (int)Player.position.Y, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight), Color.White);

            foreach (GameObject obj in NetClient.World.Objects.Values) {
                // don't bother drawing if outside window boundings
                if (Vector2.DistanceSquared(Player.position, obj.position) > 300f * 300f)
                    continue;
                // calculate screen coordinates for this object
                float xrel = (obj.position.X - min.X) / (max.X - min.X) * SDGame.Inst.GameWidth;
                float yrel = (obj.position.Y - min.Y) / (max.Y - min.Y) * SDGame.Inst.GameWidth - ((SDGame.Inst.GameWidth - SDGame.Inst.GameHeight) / 2f);
                Vector2 screenpos = new Vector2(xrel, yrel);

                // draw the object
                switch (obj.type) {
                    case ObjectType.PlayerShip:
                    case ObjectType.AIShip:
                        // don't draw the name for the local player ship
                        if (obj.id == Player.id) goto default;
                        // draw ship name above it
                        Ship shipobj = obj as Ship;
                        Debug.Assert(shipobj != null);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, new Vector2(screenpos.X, screenpos.Y - 30) - Assets.fontDefault.MeasureString(shipobj.nameshort) / 2f, shipobj.color);
                        goto default;

                    default:
                        obj.sprite.QueueDraw(screenpos, obj.color, MathHelper.ToRadians(obj.facing));
                        break;
                }
            }

            spriteBatch.End();

            // perform the queued object renders
            SpriteSheet.RenderAlpha(spriteBatch);
            SpriteSheet.RenderAdditive(spriteBatch);

            // draw a radar ring
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            spriteBatch.Draw(Assets.textures["ui/radar"], new Vector2(SDGame.Inst.GameWidth / 2f, SDGame.Inst.GameHeight / 2f), null, Color.White, 0f, new Vector2(256, 256), 1f, SpriteEffects.None, 0f);
            spriteBatch.End();
        }

        /// <summary>
        /// Draws a fuel bar on the side of the screen.
        /// </summary>
        /// <param name="spriteBatch"></param>
        protected void DrawFuelGauge(SpriteBatch spriteBatch) {
            // the fuel value's decimal part is the reservoir contents
            float reservoir = Player.fuel - (float)Math.Floor(Player.fuel);
            spriteBatch.Draw(Assets.textures["ui/itemicons"], new Rectangle(SDGame.Inst.GameWidth - 90, 75, 32, 32), new Rectangle(32, 0, 32, 32), Color.White);
            spriteBatch.DrawString(Assets.fontDefault, ((int)Math.Floor(Player.fuel)).ToString(), new Vector2(SDGame.Inst.GameWidth - 55, 84), Color.White);

            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(SDGame.Inst.GameWidth - 88, (int)(119f + 200f * (1f - reservoir)), 48, (int)(200f * reservoir)), new Rectangle(64, (int)(119f + 200f * (1f - reservoir)), 64, (int)(200f * reservoir)), Color.White);
            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(SDGame.Inst.GameWidth - 88, 119, 48, 24), new Rectangle(0, 0, 64, 24), Color.White);
            spriteBatch.Draw(Assets.textures["ui/fillbar"], new Rectangle(SDGame.Inst.GameWidth - 88, 295, 48, 24), new Rectangle(0, 24, 64, 24), Color.White);
        }

        public virtual void Update(GameTime gameTime) {
        }

    }

}