using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using GreenboyV2.Cpu;
using GreenboyV2.SysBus;

namespace GreenboyV2
{

    public class RenderWindow : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        GBZ80 CPU;
        BUS BUS;

        Texture2D screenSpace;
        Color[] screenBuffer = new Color[160*144];
        Matrix viewMatrix = Matrix.Identity;
        bool forceRatio = true;
        float cycles = 0;
        ulong lastBenchmark = 0;
        long tickRate = 4194304;
        long fps = 0;
        long cps = 0;

        bool windowInit = false;

        public RenderWindow()
        {

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        public void WritePixel( int x, int y, Color col )
        {
            if (x > 159 || x < 0)
                return;
            if (y > 143 || y < 0)
                return;

            screenBuffer[y * 160 + x] = col;
        }

        protected override void Initialize()
        {
            graphics.PreferredBackBufferWidth = 160;
            graphics.PreferredBackBufferHeight = 144;
            graphics.ApplyChanges();

            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnResize;

            BUS = new BUS();
            CPU = new GBZ80(BUS);

            CPU.Bus = BUS;
            CPU.PC.Value = 0x0100;
            CPU.SP.Value = 0xFFFE;

            InterruptManager.CPU = CPU;

            //CPU.GenerateTest();
            //CPU.TestOP();
            //Console.WriteLine("Test finished, press any key to continue!");
            //Console.ReadKey();

            //CPU.ManualTest();

            screenSpace = new Texture2D(GraphicsDevice, 160, 144);

            //IsFixedTimeStep = false;
            graphics.SynchronizeWithVerticalRetrace = false;
            graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        private void OnResize( object sender, EventArgs args )
        {
            Point siz = Window.ClientBounds.Size;
            graphics.PreferredBackBufferWidth = siz.X;
            graphics.PreferredBackBufferHeight = siz.Y;
            graphics.ApplyChanges();

            float viewW = GraphicsDevice.Viewport.Width;
            float viewH = GraphicsDevice.Viewport.Height;

            float x = 0;
            float y = 0;
            float w = viewW / 160;
            float h = viewH / 144;
            float hViewW = viewW / 2;
            float hViewH = viewH / 2;

            if (forceRatio)
                if (w > h)
                {
                    w = h;
                    x = hViewW - w * 160 / 2;
                }
                else
                {
                    h = w;
                    y = hViewH - h * 144 / 2;
                }

            viewMatrix = Matrix.CreateScale(w, h, 1)
                * Matrix.CreateTranslation(x, y, 0);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (windowInit)
            { 
                ulong curTime = (ulong)gameTime.TotalGameTime.Ticks;
                cycles += tickRate / 60f;

                for (; cycles >= 1; cycles--)
                {
                    if (cps < tickRate)
                    {
                        CPU.Clock();
                        cps++;
                    }
                }
                fps++;


                if (curTime >= lastBenchmark + 10000000)
                {
                    lastBenchmark = curTime;
                    //Console.WriteLine("{0}/{1}", fps, cps);
                    fps = 0;
                    cps = 0;
                }
            }



            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            if (!windowInit)
                windowInit = true;

            GraphicsDevice.Clear(Color.LightGray);

            screenSpace.SetData(screenBuffer);

            spriteBatch.Begin(transformMatrix: viewMatrix);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
