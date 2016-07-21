/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using ShiftDrive;

namespace ShiftDrive {

    /// <summary>
    /// The core application for the game client.
    /// </summary>
    public class SDGame : Game {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        public static SDGame Inst { get; private set; }

        /// <summary>
        /// The currently active form object.
        /// </summary>
        internal IForm ActiveForm { get; set; }

        /// <summary>
        /// The projection matrix used for 3D rendering.
        /// </summary>
        public Matrix Projection { get; private set; }

        /// <summary>
        /// Width of the game client viewport, in pixels.
        /// </summary>
        public int GameWidth { get; private set; }
        /// <summary>
        /// Height of the game client viewport, in pixels.
        /// </summary>
        public int GameHeight { get; private set; }

        public SDGame() {
            Inst = this;

            Window.AllowUserResizing = false;
            IsMouseVisible = true;

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = 1366;
            graphics.PreferredBackBufferHeight = 768;

            Content.RootDirectory = "Content";
        }
        
        protected override void Initialize() {
            // initialize MonoGame core
            base.Initialize();
        }
        
        protected override void LoadContent() {
            // create a new SpriteBatch, which can be used to draw textures
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load string table
            Utils.LocaleLoad();

            // load game assets
            Assets.fontDefault = Content.Load<SpriteFont>("Fonts/Default");
            Assets.fontDefault.LineSpacing = 20;
            Assets.fontBold = Content.Load<SpriteFont>("Fonts/Bold");

            Assets.txTitle = Content.Load<Texture2D>("Textures/UI/title");
            Assets.txRect = Content.Load<Texture2D>("Textures/UI/rect");
            Assets.txButton = Content.Load<Texture2D>("Textures/UI/button");
            Assets.txTextEntry = Content.Load<Texture2D>("Textures/UI/textentry");
            Assets.txRadarRing = Content.Load<Texture2D>("Textures/UI/radar");
            Assets.txSkybox = Content.Load<Texture2D>("Textures/Models/Skybox");
            Assets.txGlow1 = Content.Load<Texture2D>("Textures/UI/glow1");
            Assets.txAnnouncePanel = Content.Load<Texture2D>("Textures/UI/announcepanel");
            Assets.txFillbar = Content.Load<Texture2D>("Textures/UI/fillbar");
            Assets.txHullBar = Content.Load<Texture2D>("Textures/UI/hullbar");
            Assets.txItemIcons = Content.Load<Texture2D>("Textures/UI/itemicons");

            Assets.txMapIcons = new Dictionary<string, Texture2D>();
            Assets.txMapIcons.Add("player", Content.Load<Texture2D>("Textures/Map/player"));
            Assets.txMapIcons.Add("asteroid", Content.Load<Texture2D>("Textures/Map/asteroid"));
            Assets.txMapIcons.Add("mine", Content.Load<Texture2D>("Textures/Map/mine"));
            Assets.txMapIcons.Add("nebula", Content.Load<Texture2D>("Textures/Map/nebula"));
            Assets.txMapIcons.Add("blackhole", Content.Load<Texture2D>("Textures/Map/blackhole"));

            Assets.mdlSkybox = Content.Load<Model>("Models/Skybox");

            Assets.fxUnlit = Content.Load<Effect>("Fx/Unlit");

            Assets.sndUIConfirm = Content.Load<SoundEffect>("Audio/SFX/ui_confirm");
            Assets.sndUICancel = Content.Load<SoundEffect>("Audio/SFX/ui_cancel");
            Assets.sndUIAppear1 = Content.Load<SoundEffect>("Audio/SFX/ui_appear1");
            Assets.sndUIAppear2 = Content.Load<SoundEffect>("Audio/SFX/ui_appear2");
            Assets.sndUIAppear3 = Content.Load<SoundEffect>("Audio/SFX/ui_appear3");
            Assets.sndUIAppear4 = Content.Load<SoundEffect>("Audio/SFX/ui_appear4");
        }
        
        protected override void UnloadContent() {
            base.UnloadContent();
        }
        
        protected override void Update(GameTime gameTime) {
            // update viewport info
            GameWidth = GraphicsDevice.Viewport.Width;
            GameHeight = GraphicsDevice.Viewport.Height;

            // gather user input
            Mouse.Update();
            KeyInput.Update();

            // server heartbeat
            NetServer.Update(gameTime);

            // update client objects; movement prediction removes the jarred look
            if (NetClient.Connected) { // TODO && NetClient.SimRunning)
                lock (NetClient.worldLock) {
                    foreach (GameObject gobj in NetClient.World.Objects.Values) {
                        gobj.Update(NetClient.World, (float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                }
            }

            // re-calculate the 3D projection matrix
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60.0f), GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);

            // open a default form if none is active
            if (ActiveForm == null)
                ActiveForm = new FormMainMenu();
            // update the active form
            ActiveForm.Update(gameTime);

            base.Update(gameTime);
        }
        
        protected override void Draw(GameTime gameTime) {
            // clear canvas
            GraphicsDevice.Clear(Color.Black);
            // active form should draw its contents
            if (ActiveForm != null)
                ActiveForm.Draw(GraphicsDevice, spriteBatch);

            base.Draw(gameTime);
        }
    }
}
