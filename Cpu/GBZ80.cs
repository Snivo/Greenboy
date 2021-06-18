using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using GreenboyV2.SysBus;

namespace GreenboyV2.Cpu
{
    partial class GBZ80
    {
        public BUS Bus { get; set; }

        public const byte FLAG_Z = 0x80;
        public const byte FLAG_N = 0x40;
        public const byte FLAG_H = 0x20;
        public const byte FLAG_C = 0x10;

        public long TotalClocks { get; private set; }
        public int TCycles { get; set; }
        public int MCycles
        {
            get => TCycles / 4;
            set => TCycles = value * 4;
        }

        private Opcode opcode;
        bool halt = false;
        bool haltBug = false;
        bool hang = false;
        bool stop = false;

        public Register16 AF { get; set; }
        public Register8 A { get => AF.High; }
        public Register8 F { get => AF.Low; }
        public Register16 BC { get; set; }
        public Register8 B { get => BC.High; }
        public Register8 C { get => BC.Low; }
        public Register16 DE { get; set; }
        public Register8 D { get => DE.High; }
        public Register8 E { get => DE.Low; }
        public Register16 HL { get; set; }
        public Register8 H { get => HL.High; }
        public Register8 L { get => HL.Low; }
        public Register16 SP { get; set; }
        public Register16 PC { get; set; }

        public byte ReadByte(ushort addr) => Bus.ReadByte(addr, Source.CPU);
        public void WriteByte(ushort addr, byte val) => Bus.WriteByte(addr, val, Source.CPU);
        public byte PeekImmediate(int dist = 0)
        {
            return ReadByte((ushort)(PC.Value + dist));
        }

        public byte Immediate() => ReadByte(PC.GetAndInc());
        public ushort Immediate16() => Immediate().JoinLow(Immediate());
        /*{
            byte low = Immediate();
            return Bit.JoinBytes(Immediate(), low);
        }*/
        public byte Pop() => ReadByte(SP.GetAndInc());
        public ushort Pop16() => Pop().JoinLow(Pop());
        /*{
            byte low = Pop();
            return Bit.JoinBytes(Pop(), low);
        }*/
        public void Push(byte value) => WriteByte(SP.DecAndGet(), value);
        public void Push16(ushort value)
        {
            Push((byte)((value & 0xFF00) >> 8));
            Push((byte)(value & 0x00FF));
        }

        public void Clock()
        {
            if (TotalClocks % 4 == 0)
                MCycle();

            TotalClocks++;
        }

        public void MCycle()
        {
            if (hang)
                return;

            if (halt && !InterruptManager.IsInterruptPending())
                return;

            if (stop)
                return;

            if (MCycles == 0 && !InterruptManager.InterruptServiceRequest())
            {
                opcode = opcodes[Immediate()];

                if (haltBug)
                    PC.GetAndDec();

                MCycles = opcode.MCycles;
                opcode.Call();
            }

            MCycles--;
        }

        public void Reset()
        {
            AF.Value = 0x01B0;
            BC.Value = 0x0013;
            DE.Value = 0x00D8;
            HL.Value = 0x014D;
            SP.Value = 0xFFFE;

            Bus.WriteByte(0xFF05, 0x00, Source.CPU); // tima
            Bus.WriteByte(0xFF06, 0x00, Source.CPU); // tma
            Bus.WriteByte(0xFF07, 0x00, Source.CPU); // tac
            Bus.WriteByte(0xFF10, 0x80, Source.CPU); // nr10
            Bus.WriteByte(0xFF11, 0xBF, Source.CPU); // nr11
            Bus.WriteByte(0xFF12, 0xF3, Source.CPU); // nr12
            Bus.WriteByte(0xFF14, 0xBF, Source.CPU); // nr14
            Bus.WriteByte(0xFF16, 0x3F, Source.CPU); // nr21
            Bus.WriteByte(0xFF17, 0x00, Source.CPU); // nr22
            Bus.WriteByte(0xFF19, 0xBF, Source.CPU); // nr24
            Bus.WriteByte(0xFF1A, 0x7F, Source.CPU); // nr30
            Bus.WriteByte(0xFF1B, 0xFF, Source.CPU); // nr31
            Bus.WriteByte(0xFF1C, 0x9F, Source.CPU); // nr32
            Bus.WriteByte(0xFF1E, 0xBF, Source.CPU); // nr34
            Bus.WriteByte(0xFF20, 0xFF, Source.CPU); // nr41
            Bus.WriteByte(0xFF21, 0x00, Source.CPU); // nr42
            Bus.WriteByte(0xFF22, 0x00, Source.CPU); // nr43
            Bus.WriteByte(0xFF23, 0xBF, Source.CPU); // nr44
            Bus.WriteByte(0xFF24, 0x77, Source.CPU); // nr50
            Bus.WriteByte(0xFF25, 0xF3, Source.CPU); // nr51
            Bus.WriteByte(0xFF26, 0xF1, Source.CPU); // $F1-GB, $F0-SGB ; NR52
            Bus.WriteByte(0xFF40, 0x91, Source.CPU); // lcdc
            Bus.WriteByte(0xFF42, 0x00, Source.CPU); // scy
            Bus.WriteByte(0xFF43, 0x00, Source.CPU); // scx
            Bus.WriteByte(0xFF45, 0x00, Source.CPU); // lyc
            Bus.WriteByte(0xFF47, 0xFC, Source.CPU); // bgp
            Bus.WriteByte(0xFF48, 0xFF, Source.CPU); // OBP0
            Bus.WriteByte(0xFF49, 0xFF, Source.CPU); // OBP1
            Bus.WriteByte(0xFF4A, 0x00, Source.CPU); // WY
            Bus.WriteByte(0xFF4B, 0x00, Source.CPU); // WX
            Bus.WriteByte(0xFFFF, 0x00, Source.CPU); // IE
        }

        public GBZ80(BUS bus)
        {
            AF = new Register16(new Register8("a"), new FlagRegister("f"));
            BC = new Register16(new Register8("b"), new Register8("c"));
            DE = new Register16(new Register8("d"), new Register8("e"));
            HL = new Register16(new Register8("h"), new Register8("l"));
            SP = new Register16(new Register8("s"), new Register8("p"));
            PC = new Register16(new Register8("p"), new Register8("c"));

            opcodes = new Opcode[0x100];
            cbOpcodes = new Opcode[0x100];

            for (int i = 0; i < 0x100; i++)
            {
                opcodes[i] = GenerateOpcodeEntry((byte)i, false);
                cbOpcodes[i] = GenerateOpcodeEntry((byte)i, true);
            }

            Bus = bus;

            Reset();
        }
    }
}
