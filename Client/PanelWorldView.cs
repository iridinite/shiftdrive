/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ShiftDrive {

    /// <summary>
    /// A UI element that draws objects in the game world.
    /// </summary>
    internal sealed class PanelWorldView : Control {

        public struct TargetableObject {
            public uint objid;
            public Vector2 screenpos;
            //public Vector2 leadingpos;
        }

        public readonly List<TargetableObject> Targetables = new List<TargetableObject>();

        private RenderTarget2D rtWorldView, rtWorldHud;
        private readonly int viewportWidth, viewportHeight;
        private readonly Vector2 viewportSize;

        private static float reticleSpin;

        public PanelWorldView(int viewportWidth, int viewportHeight) {
            this.viewportWidth = viewportWidth;
            this.viewportHeight = viewportHeight;
            viewportSize = new Vector2(viewportWidth, viewportHeight);
        }

        protected override void OnRender(GraphicsDevice graphicsDevice, SpriteBatch spriteBatch) {
            // prepare the area HUD render target
            if (rtWorldView == null || rtWorldView.IsDisposed || rtWorldView.IsContentLost)
                rtWorldView = new RenderTarget2D(graphicsDevice, viewportWidth, viewportHeight, false, SurfaceFormat.Color,
                    DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            if (rtWorldHud == null || rtWorldHud.IsDisposed || rtWorldHud.IsContentLost)
                rtWorldHud = new RenderTarget2D(graphicsDevice, viewportWidth, viewportHeight, false, SurfaceFormat.Color,
                    DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

            // start a sprite batch and keep it open so we can draw object name text
            graphicsDevice.SetRenderTarget(rtWorldHud);
            graphicsDevice.Clear(Color.Transparent);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);

            Targetables.Clear();

            var player = NetClient.World.GetPlayerShip();

            const float viewradius = 256f;
            Vector2 min = new Vector2(player.position.X - viewradius, player.position.Y - viewradius);
            Vector2 max = new Vector2(player.position.X + viewradius, player.position.Y + viewradius);
            SDGame.Inst.Print((viewportSize.X - (viewportSize.X - viewportSize.Y) / 2f).ToString());

            foreach (GameObject obj in NetClient.World.Objects.Values) {
                // don't draw objects with no sprite
                if (obj.sprite == null)
                    continue;
                // don't bother drawing if outside window boundings
                if (Vector2.DistanceSquared(player.position, obj.position) > 350f * 350f)
                    continue;
                // calculate screen coordinates for this object
                Vector2 screenpos = Utils.CalculateScreenPos(min, max, viewportSize, obj.position);

                // remember this object for targeting
                if (obj.IsTargetable() && obj.id != player.id) {
                    TargetableObject tobj = new TargetableObject {
                        objid = obj.id,
                        screenpos = screenpos,
                        //leadingpos = Utils.CalculateScreenPos(min, max, Player.weapons[0].GetFiringSolution(Player, obj))
                    };
                    Targetables.Add(tobj);
                }

                if (obj.type == ObjectType.PlayerShip) {
                    // if player ship is destroyed, don't draw it
                    // (special case for player because we don't want to actually delete the object)
                    // also to support multiplayer in the future, don't assume this PlayerShip is the local player
                    PlayerShip otherPlayer = obj as PlayerShip;
                    Debug.Assert(otherPlayer != null);
                    if (otherPlayer.destroyed) continue;
                } else if (obj.type == ObjectType.AIShip) {
                    // draw ship name above it
                    Ship shipobj = obj as Ship;
                    Debug.Assert(shipobj != null);
                    int hullBarWidth = (int)(shipobj.hull / shipobj.hullMax * 72f);
                    Vector2 textpos = new Vector2(screenpos.X, screenpos.Y - 55) - Assets.fontDefault.MeasureString(shipobj.nameshort) / 2f;
                    // text outline
                    spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(-1, -1), Color.Black);
                    spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(1, -1), Color.Black);
                    spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(-1, 1), Color.Black);
                    spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos + new Vector2(1, 1), Color.Black);
                    // colored text
                    spriteBatch.DrawString(Assets.fontDefault, shipobj.nameshort, textpos, shipobj.GetFactionColor(player));
                    spriteBatch.Draw(Assets.GetTexture("ui/rect"),
                        new Rectangle((int)screenpos.X - hullBarWidth / 2, (int)screenpos.Y - 45, hullBarWidth, 8),
                        shipobj.GetFactionColor(player));
                }

                // draw the object
                SpriteQueue.QueueSprite(obj.sprite, screenpos, MathHelper.ToRadians(obj.facing), obj.zorder);
            }

            // queue the particle sprites
            ParticleManager.QueueDraw(min, max, viewportSize);

            // we're done drawing text and health bars
            spriteBatch.End();

            // now we can put the world view together
            graphicsDevice.SetRenderTarget(rtWorldView);
            graphicsDevice.Clear(Color.Black);

            // draw background
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointWrap);
            spriteBatch.Draw(Assets.GetTexture("back/nebula1"), new Rectangle(0, 0, viewportWidth, viewportHeight),
                new Rectangle((int)player.position.X, (int)player.position.Y, viewportWidth, viewportHeight), Color.White);
            spriteBatch.End();

            // perform the queued object renders
            SpriteQueue.RenderAlpha(spriteBatch, viewportSize);
            SpriteQueue.RenderAdditive(spriteBatch, viewportSize);
        }

        protected override void OnDraw(SpriteBatch spriteBatch) {
            // draw the world view
            spriteBatch.Draw(rtWorldView, new Rectangle(0, 0, viewportWidth, viewportHeight), Color.White);

            // draw reticles around targeted objects
            var player = NetClient.World.GetPlayerShip();
            foreach (TargetableObject tobj in Targetables) {
                if (!player.targets.Contains(tobj.objid)) continue;

                spriteBatch.Draw(Assets.GetTexture("ui/reticle"), tobj.screenpos, null, Color.White, reticleSpin, new Vector2(32, 32), 1f,
                    SpriteEffects.None, 0f);
                // spriteBatch.Draw(Assets.GetTexture("ui/leadingreticle"), tobj.leadingpos, null, Color.White, reticleSpin, new Vector2(16, 16), 1f,
                //   SpriteEffects.None, 0f);
            }

            // draw text HUD and a radar ring
            spriteBatch.Draw(rtWorldHud, new Rectangle(0, 0, viewportWidth, viewportHeight), Color.White);
            spriteBatch.Draw(Assets.GetTexture("ui/radar"), new Vector2(viewportWidth / 2f, viewportHeight / 2f), null, Color.White,
                0f, new Vector2(256, 256), 1f, SpriteEffects.None, 0f);
        }

        protected override void OnUpdate(GameTime gameTime) {
            reticleSpin += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        protected override void OnDestroy() {
            if (rtWorldHud != null && !rtWorldHud.IsDisposed)
                rtWorldHud.Dispose();
            if (rtWorldView != null && !rtWorldView.IsDisposed)
                rtWorldView.Dispose();
        }

    }

}
