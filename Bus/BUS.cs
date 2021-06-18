using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GreenboyV2.Cpu;

namespace GreenboyV2.SysBus
{
    public enum Source
    {
        CPU,
        PPU
    }

    public class BUS
    {
        // This is temporary till i get the CPU working fully
        byte[] memory = new byte[0x10000];

        public byte ReadByte(ushort addr, Source src)
        {
            return memory[addr];
        }

        public void WriteByte(ushort addr, byte val, Source src)
        {
            if (addr < 0x8000)
            {
                return;
            }
            memory[addr] = val;
        }
    }
}
