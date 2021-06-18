using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace GreenboyV2.Pixel
{
    public class PixelFIFO
    {
        PPU ppu;
        Pixel[] Out;
        Pixel[] In;
        Pixel[] temp;
        int outIdx;
        int inIdx;

        public void Push(Pixel input)
        {
            temp[inIdx++] = input;
        }

        public void Push()
        {
            In = temp;
            temp = new Pixel[8];
        }

        public Color Pop()
        {
            Pixel p = Out[outIdx++];

            if (outIdx == 8)
            {
                Out = In;
                In = null; 
            }

            return new Color(0, 0, 0);
        }

        public bool CanPop() => In != null;

        public PixelFIFO(PPU ppu)
        {
            this.ppu = ppu;
            In = new Pixel[8];
        }
    }
}
