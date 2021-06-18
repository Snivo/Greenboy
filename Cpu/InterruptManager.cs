using System;
using System.Collections.Generic;
using System.Text;

namespace GreenboyV2.Cpu
{
    [Flags]
    enum Interrupt
    {
        VBlank = 0x01,
        LCD = 0x02,
        Timer = 0x04,
        Serial = 0x08,
        Joypad = 0x10
    }

    static class InterruptManager
    {
        public static GBZ80 CPU { get; set; }

        public static bool IME { get; private set; } = true;
        static int imeDelay = -1;

        public static bool IsDelayedIME { get; private set; }

        public static bool IsInterruptPending() => (CPU.ReadByte(0xFFFF) & CPU.ReadByte(0xFF0F)) > 0;

        public static bool InterruptServiceRequest()
        {
            Interrupt I = (Interrupt)(CPU.ReadByte(0xFFFF) & CPU.ReadByte(0xFF0F));

            imeDelay--;
            if (imeDelay == 0)
                IME = true;

            if (!IME)
                return false;

            if ((I & Interrupt.VBlank) != 0)
            {
                CPU.Push16(CPU.PC.Value);
                CPU.PC.Value = 0x40;
                CPU.MCycles = 5;
                IME = false;
                RevokeInterrupt(Interrupt.VBlank);
                return true;
            }
            else if ((I & Interrupt.LCD) != 0)
            {
                CPU.Push16(CPU.PC.Value);
                CPU.PC.Value = 0x48;
                CPU.MCycles = 5;
                IME = false;
                RevokeInterrupt(Interrupt.LCD);
                return true;
            }
            else if ((I & Interrupt.Timer) != 0)
            {
                CPU.Push16(CPU.PC.Value);
                CPU.PC.Value = 0x50;
                CPU.MCycles = 5;
                IME = false;
                RevokeInterrupt(Interrupt.Timer);
                return true;
            }
            else if ((I & Interrupt.Serial) != 0)
            {
                CPU.Push16(CPU.PC.Value);
                CPU.PC.Value = 0x58;
                CPU.MCycles = 5;
                IME = false;
                RevokeInterrupt(Interrupt.Serial);
                return true;
            }
            else if ((I & Interrupt.Joypad) != 0)
            {
                CPU.Push16(CPU.PC.Value);
                CPU.PC.Value = 0x60;
                CPU.MCycles = 5;
                IME = false;
                RevokeInterrupt(Interrupt.Joypad);
                return true;
            }

            return false;
        }

        public static void RequestInterrupt(Interrupt i)
        {
            CPU.WriteByte(0xFF0F, (byte)(((Interrupt)CPU.ReadByte(0xFF0F)) | i));
        }

        public static void RevokeInterrupt(Interrupt i)
        {
            CPU.WriteByte(0xFF0F, (byte)(((Interrupt)CPU.ReadByte(0xFF0F)) & ~i));
        }

        public static void SetIME(bool set)
        {
            IME = set;
            imeDelay = -1;
        }

        public static void DelayIME() => imeDelay = 2;
    }
}
