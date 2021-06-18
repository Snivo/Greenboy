using System;
using System.Collections.Generic;
using System.Text;

namespace GreenboyV2.Pixel
{
    struct OAMObject
    {
        byte X, Y, IDX, Flags;

        bool Priority { get => Flags.GetBit(7); set => Flags.SetBit(7, value); }
        bool YFlip { get => Flags.GetBit(6); set => Flags.SetBit(6, value); }
        bool XFlip { get => Flags.GetBit(5); set => Flags.SetBit(5, value); }
        bool Palette { get => Flags.GetBit(4); set => Flags.SetBit(4, value); }
        bool Bank { get => Flags.GetBit(3); set => Flags.SetBit(3, value); }
        int PaletteCGB { get => Flags & 0x03; set => Flags.SetBits((byte)value, 0x03); }
    }
}
