using System;
using System.Collections.Generic;
using System.Text;

namespace GreenboyV2.Cpu
{
    public class Opcode
    {
        public byte OP { get; private set; }
        public string Mnemonic { get; private set; }
        public int Size { get; private set; }
        public int TCycles { get => MCycles * 4; }
        public int MCycles { get; private set; }
        public Action Call { get; }

        public override string ToString()
        {
            return $"${OP:X2} - {Mnemonic}";
        }

        public string ToString( object obj )
        {
            return string.Format(ToString(), obj);
        }

        public Opcode( byte op, string mnemonic, int size, int mcycles, Action call )
        {
            OP = op;
            Mnemonic = mnemonic;
            Size = size;
            MCycles = mcycles;
            Call = call;
        }
    }
}
