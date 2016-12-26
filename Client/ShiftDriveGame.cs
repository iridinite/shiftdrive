/*
** Project ShiftDrive
** (C) Mika Molenkamp, 2016.
*/

using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace ShiftDrive {

    /// <summary>
    /// The core application for the game client.
    /// </summary>
    public class SDGame : Game {
        private readonly GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        internal static SDGame Inst { get; private set; }
        internal static Logger Logger { get { return Inst.loggerInst; } }
        private readonly Logger loggerInst;
        private readonly DeveloperConsole console;

        private float deltaTime;

#if DEBUG
        private bool debugPanelShown = false;
#endif

        /// <summary>
        /// The currently active form object.
        /// </summary>
        internal IForm ActiveForm { get; set; }

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
            Config.Inst = Config.Load();

            Window.AllowUserResizing = false;
            IsMouseVisible = true;

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = Config.Inst.ResolutionW;
            graphics.PreferredBackBufferHeight = Config.Inst.ResolutionH;
            graphics.IsFullScreen = Config.Inst.FullScreen;

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
            Utils.LocaleLoad();

            // load game assets
            Assets.fontDefault = Content.Load<SpriteFont>("Fonts/Default");
            Assets.fontDefault.LineSpacing = 20;
            Assets.fontBold = Content.Load<SpriteFont>("Fonts/Bold");
            Assets.fontTooltip = Content.Load<SpriteFont>("Fonts/Tooltip");
            Assets.fontQuote = Content.Load<SpriteFont>("Fonts/Quote");

            DirectoryInfo dir = new DirectoryInfo("Content/Textures/");
            foreach (FileInfo file in dir.GetFiles("*.xnb", SearchOption.AllDirectories)) {
                // load all textures
                string shortname = file.FullName.Substring(dir.FullName.Length).Replace('\\', '/').ToLowerInvariant();
                shortname = shortname.Substring(0, shortname.Length - file.Extension.Length);
                Assets.textures.Add(shortname, Content.Load<Texture2D>("Textures/" + shortname));
            }
            foreach (FileInfo file in dir.GetFiles("*.txt", SearchOption.AllDirectories)) {
                // then parse all sprite sheet prototypes
                string shortname = file.FullName.Substring(dir.FullName.Length).Replace('\\', '/').ToLowerInvariant();
                shortname = shortname.Substring(0, shortname.Length - file.Extension.Length);
                Assets.sprites.Add(shortname, SpriteSheet.FromFile(file.FullName));
            }

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
            deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // gather user input
            Mouse.Update();
            KeyInput.Update();

            // server heartbeat
            NetServer.Update(gameTime);

            // update client objects; movement prediction removes the jarred look
            if (NetClient.Connected) { // TODO && NetClient.SimRunning)
                lock (NetClient.worldLock) {
                    NetClient.World.UpdateGrid();
                    foreach (GameObject gobj in NetClient.World.Objects.Values) {
                        gobj.Update(deltaTime);
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
            console.Update(deltaTime);

#if DEBUG
            // toggle debug tools with F3
            if (KeyInput.GetDown(Keys.F3))
                debugPanelShown = !debugPanelShown;
#endif

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            // clear canvas
            GraphicsDevice.Clear(Color.Black);
            // active form should draw its contents
            ActiveForm?.Draw(GraphicsDevice, spriteBatch);

#if DEBUG
            // draw debug tools
            if (debugPanelShown) {
                int dbgPanelX = GameWidth - 400;
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend);
                spriteBatch.Draw(Assets.textures["ui/rect"], new Rectangle(dbgPanelX, 0, 400, GameHeight), Color.Black * 0.6f);
                spriteBatch.DrawString(Assets.fontTooltip, Utils.GetDebugInfo(), new Vector2(dbgPanelX + 10, 10), Color.White);
                spriteBatch.End();
            }
#endif

            // draw the developer console text
            console.Draw(spriteBatch);

            base.Draw(gameTime);
        }

        private void SDGame_Exiting(object sender, EventArgs e) {
            Config.Inst.Save();
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
        /// A public accessor for the delta time value.
        /// </summary>
        public float GetDeltaTime() {
            return deltaTime;
        }

    }
}
