using System;
using System.Collections.Generic;
using System.Text;

namespace GreenboyV2.Pixel
{
    public abstract class PPU
    {
        #region Registers
        public byte LCDC { get; protected set; }
        protected byte stat;
        public byte STAT { get => (byte)((stat & 0xFC) | (LYCFlag ? 0 : 1) | Mode); }
        public byte SCY { get; protected set; }
        public byte SCX { get; protected set; }
        public byte LY { get; protected set; }
        public byte LYC;
        public byte WY { get; protected set; }
        public byte WX { get; protected set; }
        public byte DMA { get; protected set; }

        //LCDC Registers
        public bool LCDEnable { get => LCDC.GetBit(7); set => LCDC.SetBit(7, value); }
        public bool WindowMode { get => LCDC.GetBit(6); set => LCDC.SetBit(6, value); }
        public ushort WindowTileMapArea { get => WindowMode ? (ushort)0x9C00 : (ushort)0x9800; }
        public bool WindowEnable { get => LCDC.GetBit(5); set => LCDC.SetBit(5, value); }
        public bool TileDataMode { get => LCDC.GetBit(4); set => LCDC.SetBit(4, value); }
        public ushort TileDataArea { get => TileDataMode ? (ushort)0x8000 : (ushort)0x8800; }
        public bool BGMode { get => LCDC.GetBit(3); set => LCDC.SetBit(3, value); }
        public ushort BGTileMapArea { get => BGMode ? (ushort)0x9C00 : (ushort)0x9800; }
        public bool OBJSize { get => LCDC.GetBit(2); set => LCDC.SetBit(2, value); }
        public bool OBJEnable { get => LCDC.GetBit(1); set => LCDC.SetBit(1, value); }
        public bool BGWindowPriority { get => LCDC.GetBit(1); set => LCDC.SetBit(0, value); }

        //STAT Registers
        public bool LYCInterrupt { get => stat.GetBit(6); set => stat.SetBit(6, value); }
        public bool M2Interrupt { get => stat.GetBit(5); set => stat.SetBit(5, value); }
        public bool M1Interrupt { get => stat.GetBit(4); set => stat.SetBit(4, value); }
        public bool M0CInterrupt { get => stat.GetBit(3); set => stat.SetBit(3, value); }
        public bool LYCFlag { get => LY == LYC; }
        public int Mode { get; protected set; }
        #endregion
        protected int bgTile = 0;
        protected int fifoLoop = 0;
        protected int x;
        protected RenderWindow render;
        protected PixelFIFO BGFifo;
        public abstract void Clock();

        private byte ReadByte(ushort addr) => 0;

        public void Mode0()
        {

        }

        public void Mode1()
        {

        }

        public void Mode2()
        {

        }

        public void Mode3()
        {
            //FIFO fetch
            switch (fifoLoop)
            {
                case 0:
                    bgTile = ReadByte((ushort)(BGTileMapArea + (LY / 8 * 32 + x / 8)));
                    if (x == 0)
                        x = -(SCX % 8);
                    break;

                case 2:
                    byte low = ReadByte((ushort)(TileDataArea + (bgTile * (LY % 8))));
                    BGFifo.Push(new Pixel() { Color =  low & 0x3});
                    BGFifo.Push(new Pixel() { Color =  (low >> 2) & 0x3});
                    BGFifo.Push(new Pixel() { Color =  (low >> 4) & 0x3});
                    BGFifo.Push(new Pixel() { Color =  (low >> 6) & 0x3});
                    break;

                case 4:
                    byte high = ReadByte((ushort)(TileDataArea + (bgTile * (LY % 8)) + 1));
                    BGFifo.Push(new Pixel() { Color = high & 0x3 });
                    BGFifo.Push(new Pixel() { Color = (high >> 2) & 0x3 });
                    BGFifo.Push(new Pixel() { Color = (high >> 4) & 0x3 });
                    BGFifo.Push(new Pixel() { Color = (high >> 6) & 0x3 });
                    break;
                
                case 7:
                    BGFifo.Push();
                    break;
            }

            //FIFO draw
            if (BGFifo.CanPop())
                if (x < 0)
                    BGFifo.Pop();
                else
                    render.WritePixel(x, LY, BGFifo.Pop());
        }
    }
}
