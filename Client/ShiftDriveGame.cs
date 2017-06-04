/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016-2017.
*/

using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    /// <summary>
    /// The core application for the game client.
    /// </summary>
    internal sealed class SDGame : Game {

        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        internal static SDGame Inst { get; private set; }
        internal static Logger Logger => Inst.loggerInst;

        private readonly Logger loggerInst;
        private readonly DeveloperConsole console;

        private GameTime time;
        private float deltaTime;

#if DEBUG
        private bool debugPanelShown = false;
#endif

        /// <summary>
        /// The currently active form object.
        /// </summary>
        private Control UIRoot { get; set; }

        /// <summary>
        /// The projection matrix used for 3D rendering.
        /// </summary>
        internal Matrix Projection { get; private set; }

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

            loggerInst = new Logger();
            console = new DeveloperConsole();
            Config.Load();

            Window.AllowUserResizing = false;
            IsMouseVisible = true;

            graphics = new GraphicsDeviceManager(this) {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = Config.ResolutionW,
                PreferredBackBufferHeight = Config.ResolutionH,
                IsFullScreen = Config.FullScreen
            };

            Content.RootDirectory = "Content";

            Exiting += SDGame_Exiting;
        }

        protected override void Initialize() {
            // initialize MonoGame core
            base.Initialize();

            loggerInst.Log("ShiftDrive Client " + Utils.GetVersionString());
        }

        protected override void LoadContent() {
            // create a new SpriteBatch, which can be used to draw textures
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // load string table
            Locale.LoadStrings("Data//locale//en-GB//strings.txt");

            // load game assets
            Assets.LoadContent(GraphicsDevice, Content);
        }

        protected override void Update(GameTime gameTime) {
            // update viewport info
            GameWidth = GraphicsDevice.Viewport.Width;
            GameHeight = GraphicsDevice.Viewport.Height;
            time = gameTime;
            deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // gather user input
            Input.Update();

            // server heartbeat
            NetServer.Update(gameTime);

            // update client objects; movement prediction removes the jarred look
            if (NetClient.Connected) {
                // TODO && NetClient.SimRunning)
                lock (NetClient.worldLock) {
                    NetClient.World.UpdateGrid();
                    foreach (GameObject gobj in NetClient.World.Objects.Values) {
                        gobj.Update(deltaTime);
                    }
                }
                // update particles
                ParticleManager.Update(deltaTime);
            }

            // re-calculate the 3D projection matrix
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(60.0f), GraphicsDevice.Viewport.AspectRatio, 0.01f, 1000f);

            // open a default form if none is active
            if (UIRoot == null)
                UIRoot = new FormMainMenu();
            // update the active form
            UIRoot.Update(gameTime);
            console.Update(deltaTime);

#if DEBUG
            // toggle debug tools with F3
            if (Input.GetKeyDown(Keys.F3))
                debugPanelShown = !debugPanelShown;
#endif

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            // some controls may want to draw to rendertargets
            UIRoot?.Render(GraphicsDevice, spriteBatch);
            // active form should draw its contents
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            UIRoot?.Draw(spriteBatch);
            spriteBatch.End();

            // draw always-on-top UI elements
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
            Tooltip.DrawQueued(spriteBatch);
            console.Draw(spriteBatch);
            spriteBatch.End();

#if DEBUG
            // draw debug tools
            if (debugPanelShown) {
                int dbgPanelX = GameWidth - 400;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(Assets.GetTexture("ui/rect"), new Rectangle(dbgPanelX, 0, 400, GameHeight), Color.Black * 0.6f);
                spriteBatch.DrawString(Assets.fontTooltip, Utils.GetDebugInfo(), new Vector2(dbgPanelX + 10, 10), Color.White);
                spriteBatch.End();
            }
#endif

            base.Draw(gameTime);
        }

        private void SDGame_Exiting(object sender, EventArgs e) {
            Config.Save();
            loggerInst?.Dispose();
        }

        /// <summary>
        /// Prints a message to the developer console.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="error">Whether to draw the message in red or not.</param>
        public void Print(string message, bool error = false) {
            console.AddMessage(message, error);
        }

        /// <summary>
        /// Sets the game's currently active root UI object to the specified object.
        /// </summary>
        public void SetUIRoot(Control val) {
            UIRoot?.Destroy();
            UIRoot = val;
        }

        /// <summary>
        /// A public accessor for the delta time value.
        /// </summary>
        public float GetDeltaTime() {
            return deltaTime;
        }

        /// <summary>
        /// A public accessor for the current time.
        /// </summary>
        public GameTime GetTime() {
            return time;
        }

    }

}
