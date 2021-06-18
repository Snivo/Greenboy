using System;
using System.Linq;

namespace GreenboyV2.Cpu
{
    public class Register8
    {
        public static explicit operator byte(Register8 r) => r.Value;
        public static Register8 operator +(Register8 r, byte val)
        {
            r.Value += val;
            return r;
        }
        public static Register8 operator +(Register8 r, Register8 rB) => r + rB.Value;
        public static Register8 operator -(Register8 r, byte val)
        {
            r.Value -= val;
            return r;
        }
        public static Register8 operator -(Register8 r, Register8 rB) => r - rB.Value;

        public string Name { get; }
        public virtual byte Value { get; set; }

        public void SetBits(int set, int bits) => Value = (byte)((Value & ~bits) | (set & bits));
        public bool GetBit(int bit)
        {
            int tester = 1 << bit;
            return (Value & tester) == tester;
        }
        public byte GetAndInc() => Value++;
        public byte IncAndGet() => ++Value;
        public byte GetAndDec() => Value--;
        public byte DecAndGet() => --Value;

        public Register8(string name)
        {
            Name = name;
        }
    }

    public class FlagRegister : Register8
    {
        byte val;

        public override byte Value
        {
            get => val;
            set => val = (byte)(value & 0xF0);
        }

        public FlagRegister( string name ) : base(name)
        {

        }
    }

    public class Register16
    {
        public Register8 Low { get; }
        public Register8 High { get; }

        public static explicit operator ushort(Register16 r) => r.Value;
        public static Register16 operator +(Register16 r, ushort val)
        {
            r.Value += val;
            return r;
        }
        public static Register16 operator +(Register16 r, Register16 rB) => r + rB.Value;
        public static Register16 operator +(Register16 r, Register8 rB) => r + rB.Value;
        public static Register16 operator -(Register16 r, ushort val)
        {
            r.Value -= val;
            return r;
        }
        public static Register16 operator -(Register16 r, Register16 rB) => r - rB.Value;
        public static Register16 operator -(Register16 r, Register8 rB) => r - rB.Value;



        public string LowName { get => Low.Name; }
        public string HighName { get => High.Name; }
        public byte HighValue { get => High.Value; set => High.Value = value; }
        public byte LowValue { get => Low.Value; set => Low.Value = value; }
        public string Name { get => HighName + LowName; }
        public ushort Value
        {
            get => (ushort)((HighValue << 8) | LowValue);
            set
            {
                LowValue = (byte)(value);
                HighValue = (byte)(value >> 8);
            }
        }

        public ushort GetAndInc() => Value++;
        public ushort IncAndGet() => ++Value;
        public ushort GetAndDec() => Value--;
        public ushort DecAndGet() => --Value;

        public Register16(Register8 high, Register8 low)
        {
            High = high;
            Low = low;
        }
    }
}
