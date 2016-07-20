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

        private static float blackHoleRotation = 0f;

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

            for (int i = 0; i < NetClient.World.Objects.Count; i++) {
                GameObject obj = NetClient.World.Objects[i];
                // don't bother drawing if outside window boundings
                if (Vector2.DistanceSquared(Player.position, obj.position) > 1000 * 1000)
                    continue;
                // calculate screen coordinates for this object
                float xrel = (obj.position.X - min.X) / (max.X - min.X);
                float yrel = (obj.position.Y - min.Y) / (max.Y - min.Y);
                Vector2 screenpos = new Vector2(xrel * SDGame.Inst.GameWidth, yrel * SDGame.Inst.GameHeight);

                // draw the object
                switch (obj.type) {
                    case ObjectType.BlackHole:
                        // black hole is drawn rotating, and with a second larger one around it
                        spriteBatch.Draw(Assets.txMapIcons[obj.iconfile], screenpos, null, obj.iconcolor, blackHoleRotation, new Vector2(Assets.txMapIcons[obj.iconfile].Width / 2f, Assets.txMapIcons[obj.iconfile].Height / 2f), .5f, SpriteEffects.None, 0f);
                        spriteBatch.Draw(Assets.txMapIcons[obj.iconfile], screenpos, null, obj.iconcolor, -blackHoleRotation, new Vector2(Assets.txMapIcons[obj.iconfile].Width / 2f, Assets.txMapIcons[obj.iconfile].Height / 2f), 1f,  SpriteEffects.None, 0f);
                        break;

                    case ObjectType.PlayerShip:
                    case ObjectType.AIShip:
                        // don't draw the name for the local player ship
                        if (obj.id == Player.id) goto default;
                        // draw ship name above it
                        Ship shipobj = obj as Ship;
                        Debug.Assert(shipobj != null);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, new Vector2(screenpos.X, screenpos.Y - 30) - Assets.fontDefault.MeasureString(shipobj.nameshort) / 2f, shipobj.iconcolor);
                        goto default;

                    default:
                        spriteBatch.Draw(Assets.txMapIcons[obj.iconfile], screenpos, null, obj.iconcolor, MathHelper.ToRadians(obj.facing), new Vector2(Assets.txMapIcons[obj.iconfile].Width / 2f, Assets.txMapIcons[obj.iconfile].Height / 2f), .5f, SpriteEffects.None, 0f);
                        break;
                }
            }

            // draw a radar ring
            spriteBatch.Draw(Assets.txRadarRing, new Vector2(SDGame.Inst.GameWidth / 2f, SDGame.Inst.GameHeight / 2f), null, Color.White, 0f, new Vector2(256, 256), 1f, SpriteEffects.None, 0f);
        }

        public virtual void Update(GameTime gameTime) {
            blackHoleRotation += (float)(gameTime.ElapsedGameTime.TotalSeconds * 0.25);
            while (blackHoleRotation >= MathHelper.TwoPi) blackHoleRotation -= MathHelper.TwoPi;
        }
    }

}