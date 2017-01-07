﻿/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// Represents a ship station view to be rendered in <seealso cref="FormGame"/>.
    /// </summary>
    internal abstract class Console {

        protected struct TargetableObject {
            public uint objid;
            public Vector2 screenpos;
        }

        protected readonly List<TargetableObject> targetables = new List<TargetableObject>();

        private RenderTarget2D rtAreaHud;
        private static float reticleSpin;

        public abstract void Draw(SpriteBatch spriteBatch);

        /// <summary>
        /// Shorthand for NetClient.World.GetPlayerShip()
        /// </summary>
        protected static PlayerShip Player { get { return NetClient.World.GetPlayerShip(); } }

        /// <summary>
        /// Draws the local area with the player ship and a radar ring.
        /// </summary>
        /// <param name="spriteBatch">A SpriteBatch to render with. SpriteBatch.Begin must have been successfully called!</param>
        protected void DrawLocalArea(SpriteBatch spriteBatch) {
            const float viewradius = 256f;
            Vector2 min = new Vector2(Player.position.X - viewradius, Player.position.Y - viewradius);
            Vector2 max = new Vector2(Player.position.X + viewradius, Player.position.Y + viewradius);

            // prepare the area HUD render target
            targetables.Clear();
            GraphicsDevice graphicsDevice = spriteBatch.GraphicsDevice;
            if (rtAreaHud == null || rtAreaHud.IsDisposed || rtAreaHud.IsContentLost)
                rtAreaHud = new RenderTarget2D(graphicsDevice, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            graphicsDevice.SetRenderTarget(rtAreaHud);
            graphicsDevice.Clear(Color.Transparent);

            // start a sprite batch and keep it open so we can draw object name text
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);

            foreach (GameObject obj in NetClient.World.Objects.Values) {
                // don't draw objects with no sprite
                if (obj.sprite == null)
                    continue;
                // don't bother drawing if outside window boundings
                if (Vector2.DistanceSquared(Player.position, obj.position) > 350f * 350f)
                    continue;
                // calculate screen coordinates for this object
                Vector2 screenpos = Utils.CalculateScreenPos(min, max, obj.position);

                // remember this object for targeting
                if (obj.IsTargetable() && obj.id != Player.id) {
                    TargetableObject tobj = new TargetableObject();
                    tobj.objid = obj.id;
                    tobj.screenpos = screenpos;
                    targetables.Add(tobj);
                }

                // draw the object
                switch (obj.type) {
                    case ObjectType.PlayerShip:
                        // if player ship is destroyed, don't draw it
                        // (special case for player because we don't want to actually delete the object)
                        PlayerShip player = obj as PlayerShip;
                        Debug.Assert(player != null);
                        if (player.destroyed) break;
                        goto default;

                    case ObjectType.AIShip:
                        // draw ship name above it
                        Ship shipobj = obj as Ship;
                        Debug.Assert(shipobj != null);
                        int hullBarWidth = (int)(shipobj.hull / shipobj.hullMax * 72f);
                        Vector2 textpos = new Vector2(screenpos.X, screenpos.Y - 55) -
                                          Assets.fontDefault.MeasureString(shipobj.nameshort) / 2f;
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(-1, -1), Color.Black);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(1, -1), Color.Black);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(-1, 1), Color.Black);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(1, 1), Color.Black);
                        spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos, shipobj.GetFactionColor(Player));
                        spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle((int)screenpos.X - hullBarWidth / 2, (int)screenpos.Y - 45, hullBarWidth, 8), shipobj.GetFactionColor(Player));
                        goto default;

                    default:
                        SpriteQueue.QueueSprite(obj.sprite, screenpos, obj.color, MathHelper.ToRadians(obj.facing), obj.zorder);
                        break;
                }
            }
            // we're done drawing text and health bars
            spriteBatch.End();

            // queue the particle sprites
            ParticleManager.QueueDraw(min, max);

            // switch to back buffer and start putting everything together
            graphicsDevice.SetRenderTarget(null);
            graphicsDevice.Clear(Color.Black);

            // draw background
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            spriteBatch.Draw(Assets.GetTexture("back/nebula1"), new Rectangle(0, 0, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight), new Rectangle((int)Player.position.X, (int)Player.position.Y, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight), Color.White);
            spriteBatch.End();

            // perform the queued object renders
            SpriteQueue.RenderAlpha(spriteBatch);
            SpriteQueue.RenderAdditive(spriteBatch);

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);

            // draw reticles around targeted objects
            foreach (TargetableObject tobj in targetables) {
                if (!Player.targets.Contains(tobj.objid)) continue;

                spriteBatch.Draw(Assets.textures["ui/reticle"], tobj.screenpos, null, Color.Red, reticleSpin, new Vector2(32, 32), 1f, SpriteEffects.None, 0f);
            }

            // draw render target and a radar ring
            spriteBatch.Draw(rtAreaHud, new Rectangle(0, 0, SDGame.Inst.GameWidth, SDGame.Inst.GameHeight), Color.White);
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
            reticleSpin += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

    }

}