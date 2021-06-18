using System;
using System.Collections.Generic;
using System.Text;

namespace GreenboyV2.Cpu
{
    enum JumpCondition
    {
        None = -1,
        NZ = 0,
        Z = 1,
        NC = 2,
        C = 3
    }

    public partial class GBZ80
    {
        public Opcode[] opcodes;
        public Opcode[] cbOpcodes;

        bool TestCondition(int cycles, JumpCondition jc = JumpCondition.None)
        {
            switch (jc)
            {
                case JumpCondition.NZ:
                    if (!F.GetBit(7))
                    {
                        MCycles += cycles;
                        return true;
                    }
                    break;
                case JumpCondition.Z:
                    if (F.GetBit(7))
                    {
                        MCycles += cycles;
                        return true;
                    }
                    break;
                case JumpCondition.NC:
                    if (!F.GetBit(4))
                    {
                        MCycles += cycles;
                        return true;
                    }
                    break;
                case JumpCondition.C:
                    if (F.GetBit(4))
                    {
                        MCycles += cycles;
                        return true;
                    }
                    break;
                default:
                    return true;
            }
            return false;
        }

        void OpcodeADC(byte u8)
        {
            int carry = (F.Value & FLAG_C) >> 4;
            int flags = (((u8 & 0x0F) + (A.Value & 0x0F) + carry) & 0x10) == 0x10 ? FLAG_H : 0;
            int temp = A.Value + u8 + carry;
            flags |= (temp & 0x100) == 0x100 ? FLAG_C : 0;
            flags |= (temp & 0xFF) == 0 ? FLAG_Z : 0;

            A.Value = (byte)temp;
            F.SetBits(flags, 0xF0);
        }

        void OpcodeADD(byte u8)
        {
            int flags = (((u8 & 0x0F) + (A.Value & 0x0F)) & 0x10) == 0x10 ? FLAG_H : 0;
            int temp = A.Value + u8;
            flags |= (temp & 0x100) == 0x100 ? FLAG_C : 0;
            flags |= (temp & 0xFF) == 0 ? FLAG_Z : 0;

            A.Value = (byte)temp;
            F.SetBits(flags, 0xF0);
        }

        void OpcodeADD(ushort u16)
        {
            int flags = (((HL.Value & 0xFFF) + (u16 & 0xFFF)) & 0x1000) == 0x1000 ? FLAG_H : 0;
            int temp = HL.Value + u16;
            flags |= (temp & 0x10000) == 0x10000 ? FLAG_C : 0;

            HL.Value = (ushort)temp;
            F.SetBits(flags, 0x70);
        }

        void OpcodeADDSP( sbyte d )
        {
            int flags = (((SP.Value & 0x0F) + (d & 0x0F)) & 0x10) == 0x10 ? FLAG_H : 0;
            flags |= (((SP.Value & 0xFF) + (d & 0xFF) & 0x100) == 0x100) ? FLAG_C : 0;

            F.SetBits(flags, 0xF0);
            SP.Value = (ushort)(SP.Value + d);
        }

        void OpcodeAND(byte u8)
        {
            A.Value &= u8;
            int flags = FLAG_H;
            flags |= A.Value == 0 ? FLAG_Z : 0;

            F.SetBits(flags, 0xF0);
        }

        void OpcodeBIT(byte val, byte u3)
        {
            int temp = (val & (1 << u3)) == 0 ? FLAG_Z : 0;
            temp |= FLAG_H;

            F.SetBits(temp, 0xE0);
        }

        void OpcodeCALL(ushort addr, JumpCondition jc = JumpCondition.None)
        {
            if (TestCondition(3, jc))
            {
                Push(PC.HighValue);
                Push(PC.LowValue);
                PC.Value = addr;
            }
        }

        void OpcodeCB()
        {
            opcode = cbOpcodes[Immediate()];
            opcode.Call();
            MCycles = opcode.MCycles - 1;
        }

        void OpcodeCCF() => F.SetBits(F.GetBit(4) ? 0 : FLAG_C, 0x70);

        void OpcodeCP( byte r8 )
        {
            int temp = A.Value - r8;
            int flag = FLAG_N;
            flag |= (temp & 0xFF) == 0 ? FLAG_Z : 0;
            flag |= (((A.Value & 0x0F) - (r8 & 0x0F)) & 0x10) == 0x10 ? FLAG_H : 0;
            flag |= (temp & 0x100) == 0x100 ? FLAG_C : 0;
            F.SetBits(flag, 0xF0);
        }

        void OpcodeCPL()
        {
            A.Value = (byte)~A.Value;
            F.SetBits(FLAG_N | FLAG_H, 0x60);
        }

        void OpcodeDAA()
        {
            byte value = A.Value;
            byte correction = 0;
            bool flagH = (F.Value & FLAG_H) == FLAG_H;
            bool flagN = (F.Value & FLAG_N) == FLAG_N;
            bool flagC = (F.Value & FLAG_C) == FLAG_C;
            bool setC = false;

            if (flagH || (!flagN && (value & 0xf) > 9))
                correction = 0x6;

            if (flagC || (!flagN && value > 0x99))
            {
                correction |= 0x60;
                setC = true;
            }

            value += flagN ? (byte)-correction : correction;
            F.SetBits((value == 0 ? FLAG_Z : 0) | (setC ? FLAG_C : 0), 0xB0);
            A.Value = value;
        }

        byte OpcodeDEC( byte u8 )
        {
            int temp = u8 - 1;
            int flags = FLAG_N;
            flags |= (temp & 0xFF) == 0 ? FLAG_Z : 0;
            flags |= (((u8 & 0x0F) - 1) & 0x10) == 0x10 ? FLAG_H : 0;
            F.SetBits(flags, 0xE0);
            return (byte)temp;
        }

        void OpcodeDI() => InterruptManager.SetIME(false);

        void OpcodeEI() => InterruptManager.DelayIME();

        void OpcodeHALT()
        {
            if (InterruptManager.IME)
                halt = true;
            else
            {
                if (InterruptManager.IsInterruptPending())
                    haltBug = true;
                else
                    halt = true;
            }
        }

        byte OpcodeINC( byte u8 )
        {
            int temp = u8 + 1;
            int flags = (temp & 0xFF) == 0 ? FLAG_Z : 0;
            flags |= (((u8 & 0x0F) + 1) & 0x10) == 0x10 ? FLAG_H : 0;
            F.SetBits(flags, 0xE0);
            return (byte)temp;
        }

        void OpcodeJP( ushort addr, JumpCondition jc )
        {
            if (TestCondition(1, jc))
                PC.Value = addr;
        }

        void OpcodeJR( byte d, JumpCondition jc )
        {
            if (TestCondition(1, jc))
                PC.Value = (ushort)(PC.Value + (sbyte)d);
        }
        
        void OpcodeLD(Register8 r8, byte u8) => r8.Value = u8;

        void OpcodeLD(Register16 r16, ushort u16) => r16.Value = u16;

        void OpcodeLD(ushort u16, byte u8) => WriteByte(u16, u8);

        void OpcodeLD(ushort addr, ushort u16)
        {
            WriteByte(addr, (byte)(u16 & 0x00FF));
            WriteByte((ushort)(addr + 1), (byte)((u16 & 0xFF00) >> 8));
        }
        
        void OpcodeLDSP( sbyte d )
        {
            int flags = (((SP.Value & 0x0F) + (d & 0x0F)) & 0x10) == 0x10 ? FLAG_H : 0;
            flags |= (((SP.Value & 0xFF) + (d & 0xFF)) & 0x100) == 0x100 ? FLAG_C : 0;

            F.SetBits(flags, 0xF0);
            HL.Value = (ushort)(SP.Value + d);
        }

        void OpcodeOR( byte r8 )
        {
            A.Value |= r8;
            F.SetBits(A.Value == 0 ? FLAG_Z : 0, 0xF0);
        }

        void OpcodePOP(Register16 r16, bool af = false)
        {
            if (af)
                r16.Value = (ushort)(Pop16() & 0xFFF0);
            else
                r16.Value = Pop16();
        }

        void OpcodePUSH(Register16 r16) => Push16(r16.Value);

        byte OpcodeRES( byte u8, int u3 ) => u8.ResetBit(u3);

        void OpcodeRET(JumpCondition jc = JumpCondition.None, bool reti = false)
        {
            if (TestCondition(3 ,jc))
            {
                PC.Value = Pop16();
                if (reti)
                    InterruptManager.SetIME(true);
            }
        }

        byte OpcodeRL( byte u8, bool resZ = false )
        {
            int temp = (u8 << 1) | ((F.Value & FLAG_C) >> 4);
            int z = resZ ? 0 : ((temp & 0xFF) == 0 ? FLAG_Z : 0);
            F.SetBits(((temp & 0x100) >> 4) | z, 0xF0);
            return (byte)temp;
        }

        byte OpcodeRLC( byte u8, bool resZ = false)
        {
            int temp = (u8 << 1) | (u8 >> 7);
            int z = resZ ? 0 : ((temp & 0xFF) == 0 ? FLAG_Z : 0);
            F.SetBits(((temp & 0x100) >> 4) | z, 0xF0);
            return (byte)temp;
        }

        byte OpcodeRR(byte u8, bool resZ = false)
        {
            int c = (u8 & 0x01) << 4;
            int temp = (u8 >> 1) | ((F.Value & FLAG_C) << 3);
            int z = resZ ? 0 : ((temp & 0xFF) == 0 ? FLAG_Z : 0);
            F.SetBits(c | z, 0xF0);
            return (byte)temp;
        }

        byte OpcodeRRC(byte u8, bool resC = false)
        {
            int c = (u8 & 0x01) << 4;
            int temp = (u8 >> 1) | (u8 << 7);
            int z = resC ? 0 : ((temp & 0xFF) == 0 ? FLAG_Z : 0);
            F.SetBits(c | z, 0xF0);
            return (byte)temp;
        }

        void OpcodeRST(byte vec)
        {
            Push16(PC.Value);
            PC.Value = vec;
        }

        void OpcodeSBC( byte u8 )
        {
            int carry = F.GetBit(4) ? 1 : 0;
            int flags = FLAG_N;
            flags |= (((A.Value & 0x0F) - (u8 & 0x0F) - carry) & 0x10) == 0x10 ? FLAG_H : 0;
            int temp = A.Value - u8 - carry;
            flags |= (temp & 0x100) == 0x100 ? FLAG_C : 0;
            flags |= (temp & 0xFF) == 0 ? FLAG_Z : 0;

            F.SetBits(flags, 0xF0);
            A.Value = (byte)temp;
        }

        void OpcodeSCF() => F.SetBits(FLAG_C, 0x70);

        byte OpcodeSET(byte u8, int u3) => u8.SetBit(u3);

        byte OpcodeSLA( byte u8 )
        {
            int temp = u8 << 1;
            F.SetBits(((temp & 0x100) >> 4) | ((temp & 0xFF) == 0 ? FLAG_Z : 0), 0xF0);
            return (byte)temp;
        }

        byte OpcodeSRA( byte u8 )
        {
            int c = (u8 & 0x01) << 4;
            int temp = (u8 >> 1) | (u8 & 0x80);
            F.SetBits(c | ((temp & 0xFF) == 0 ? FLAG_Z : 0), 0xF0);
            return (byte)temp;
        }

        byte OpcodeSRL( byte u8 )
        {
            int c = (u8 & 0x01) << 4;
            int temp = u8 >> 1;
            F.SetBits(c | ((temp & 0xFF) == 0 ? FLAG_Z : 0), 0xF0);
            return (byte)temp;
        }

        void OpcodeSTOP()
        {
            stop = true;
        }

        void OpcodeSUB( byte u8 )
        {
            int flags = FLAG_N;
            flags |= (((A.Value & 0x0F) - (u8 & 0x0F)) & 0x10) == 0x10 ? FLAG_H : 0;
            int temp = A.Value - u8;
            flags |= (temp & 0x100) == 0x100 ? FLAG_C : 0;
            flags |= (temp & 0xFF) == 0 ? FLAG_Z : 0;

            F.SetBits(flags, 0xF0);
            A.Value = (byte)temp;
        }

        byte OpcodeSWAP( byte u8 )
        {
            byte temp = (byte)((u8 << 4) | (u8 >> 4));
            F.SetBits(temp == 0 ? FLAG_Z : 0, 0xFF);
            return (byte)temp;
        }

        void OpcodeXOR( byte u8 )
        {
            A.Value ^= u8;
            F.SetBits(A.Value == 0 ? FLAG_Z : 0, 0xF0);
        }

        void OpcodeHCF()
        {
            hang = true;
        }

        Opcode GenerateOpcodeEntry( byte op, bool prefix )
        {
            int x = (op & 0xC0) >> 6;
            int y = (op & 0x38) >> 3;
            int z = op & 0x07;
            int p = y >> 1;
            int q = y & 0x01;
            
            Register16[] rp = new Register16[] { BC, DE, HL, SP };
            Register16[] rp2 = new Register16[] { BC, DE, HL, AF };
            Register8[] r = new Register8[] { B, C, D, E, H, L, null, A };

            Register16 r16;
            Register8 r8;

            if (!prefix)
                switch (x)
                {
                    case 0:
                        switch(z)
                        {
                            case 0:
                                switch(y)
                                {
                                    case 0:
                                        //nop
                                        return new Opcode(op, "nop", 1, 1, () => { });
                                    case 1:
                                        //ld [nn], sp
                                        return new Opcode(op, "ld ${0:X4}, sp", 3, 5, () => OpcodeLD(Immediate16(), SP.Value));
                                    case 2:
                                        //stop
                                        return new Opcode(op, "stop", 2, 1, () => OpcodeSTOP());
                                    case 3:
                                        //jr d
                                        return new Opcode(op, "jr d", 2, 3, () => OpcodeJR(Immediate(), JumpCondition.None));
                                    default:
                                        //jd cc[y - 4], d
                                        JumpCondition jc = (JumpCondition)(y - 4);
                                        return new Opcode(op, $"jr {jc:g}, ${{0:X2}}", 2, 2, () => OpcodeJR(Immediate(), jc));
                                }
                            case 1:
                                switch (q)
                                {
                                    case 0:
                                        //ld rp[p], nn
                                        r16 = rp[p];
                                        return new Opcode(op, $"ld {r16.Name}, ${{0:X4}}", 2, 2, () => OpcodeLD(r16, Immediate16()));
                                    case 1:
                                        //add hl, rp[p]
                                        r16 = rp[p];
                                        return new Opcode(op, $"add hl, {r16.Name}", 2, 1, () => OpcodeADD(r16.Value));
                                }
                                break;
                            case 2:
                                switch(q)
                                {
                                    case 0:
                                        switch (p)
                                        {
                                            case 0:
                                                //ld [bc], a
                                                return new Opcode(op, "ld [bc], a", 1, 2, () => OpcodeLD(BC.Value, A.Value));
                                            case 1:
                                                //ld [de], a
                                                return new Opcode(op, "ld [de], a", 1, 2, () => OpcodeLD(DE.Value, A.Value));
                                            case 2:
                                                //ld [hl+], a
                                                return new Opcode(op, "ld [hl+], a", 1, 2, () => OpcodeLD(HL.GetAndInc(), A.Value));
                                            case 3:
                                                //ld [hl-], a
                                                return new Opcode(op, "ld [hl-], a", 1, 2, () => OpcodeLD(HL.GetAndDec(), A.Value));
                                        }
                                        break;
                                    case 1:
                                        switch (p)
                                        {
                                            case 0:
                                                //ld a, [bc]
                                                return new Opcode(op, "ld a, [bc]", 1, 2, () => OpcodeLD(A, ReadByte(BC.Value)));
                                            case 1:
                                                //ld a, [de]
                                                return new Opcode(op, "ld a, [de]", 1, 2, () => OpcodeLD(A, ReadByte(DE.Value)));
                                            case 2:
                                                //ld a, [hl+]
                                                return new Opcode(op, "ld a, [hl+]", 1, 2, () => OpcodeLD(A, ReadByte(HL.GetAndInc())));
                                            case 3:
                                                //ld a, [hl-]
                                                return new Opcode(op, "ld a, [hl-]", 1, 2, () => OpcodeLD(A, ReadByte(HL.GetAndDec())));
                                        }
                                        break;
                                }
                                break;
                            case 3:
                                r16 = rp[p];
                                switch(q)
                                {
                                    case 0:
                                        //inc rp[p]
                                        return new Opcode(op, $"inc {r16.Name}", 1, 2, () => r16.GetAndInc());
                                    case 1:
                                        //dec rp[p]
                                        return new Opcode(op, $"dec {r16.Name}", 1, 2, () => r16.GetAndDec());
                                }
                                break;
                            case 4:
                                //inc r[y]
                                if (y == 6)
                                    return new Opcode(op, "inc [hl]", 1, 3, () => WriteByte(HL.Value, OpcodeINC(ReadByte(HL.Value))));
                                else
                                {
                                    r8 = r[y];
                                    return new Opcode(op, $"inc {r8.Name}", 1, 1, () => r8.Value = OpcodeINC(r8.Value));
                                }
                            case 5:
                                //dec r[y]
                                if (y == 6)
                                    return new Opcode(op, "dec [hl]", 1, 3, () => WriteByte(HL.Value, OpcodeDEC(ReadByte(HL.Value))));

                                r8 = r[y];
                                return new Opcode(op, $"dec {r8.Name}", 1, 1, () => r8.Value = OpcodeDEC(r8.Value));
                            case 6:
                                //ld r[y], n
                                if (y == 6)
                                    return new Opcode(op, "ld [hl], ${0:X2}", 2, 3, () => OpcodeLD(HL.Value, Immediate()));

                                r8 = r[y];
                                return new Opcode(op, $"ld {r8.Name}, ${{0:X2}}", 2, 2, () => OpcodeLD(r8, Immediate()));
                            case 7:
                                switch(y)
                                {
                                    case 0:
                                        //rlca
                                        return new Opcode(op, "rlca", 1, 1, () => A.Value = OpcodeRLC(A.Value, true));
                                    case 1:
                                        //rrca
                                        return new Opcode(op, "rrca", 1, 1, () => A.Value = OpcodeRRC(A.Value, true));
                                    case 2:
                                        //rla
                                        return new Opcode(op, "rla", 1, 1, () => A.Value = OpcodeRL(A.Value, true));
                                    case 3:
                                        //rra
                                        return new Opcode(op, "rra", 1, 1, () => A.Value = OpcodeRR(A.Value, true));
                                    case 4:
                                        //daa
                                        return new Opcode(op, "daa", 1, 1, () => OpcodeDAA());
                                    case 5:
                                        //cpl
                                        return new Opcode(op, "cpl", 1, 1, () => OpcodeCPL());
                                    case 6:
                                        //scf
                                        return new Opcode(op, "scf", 1, 1, () => OpcodeSCF());
                                    case 7:
                                        //ccf
                                        return new Opcode(op, "ccf", 1, 1, () => OpcodeCCF());
                                }
                                break;
                        }
                        break;
                    case 1:
                        if (y == 6 && z == 6)
                            //halt
                            return new Opcode(op, "halt", 1, 1, () => OpcodeHALT());
                        else
                        //ld r[y] r[z]
                        {
                            Register8 arg = r[z];
                            r8 = r[y];
                            if (y == 6)
                                return new Opcode(op, $"ld [hl], {arg.Name}", 1, 2, () => OpcodeLD(HL.Value, arg.Value));

                            if (z == 6)
                                return new Opcode(op, $"ld {r8.Name}, [hl]", 1, 2, () => OpcodeLD(r8, ReadByte(HL.Value)));

                            return new Opcode(op, $"ld {r8.Name}, {arg.Name}", 1, 1, () => OpcodeLD(r8, arg.Value));
                        }
                    case 2:
                        r8 = r[z];
                        switch (y)
                        {
                            case 0:
                                //add a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "add a, [hl]", 1, 2, () => OpcodeADD(ReadByte(HL.Value)));
                                return new Opcode(op, $"add a, {r8.Name}", 1, 1, () => OpcodeADD(r8.Value)); 
                            case 1:
                                //adc a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "adc a, [hl]", 1, 2, () => OpcodeADC(ReadByte(HL.Value)));
                                return new Opcode(op, $"adc a, {r8.Name}", 1, 1, () => OpcodeADC(r8.Value));
                            case 2:
                                //sub a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "sub a, [hl]", 1, 2, () => OpcodeSUB(ReadByte(HL.Value)));
                                return new Opcode(op, $"sub a, {r8.Name}", 1, 1, () => OpcodeSUB(r8.Value));
                            case 3:
                                //sbc a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "sbc a, [hl]", 1, 2, () => OpcodeSBC(ReadByte(HL.Value)));
                                return new Opcode(op, $"sbc a, {r8.Name}", 1, 1, () => OpcodeSBC(r8.Value));
                            case 4:
                                //and a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "and a, [hl]", 1, 2, () => OpcodeAND(ReadByte(HL.Value)));
                                return new Opcode(op, $"and a, {r8.Name}", 1, 1, () => OpcodeAND(r8.Value));
                            case 5:
                                //xor a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "xor a, [hl]", 1, 2, () => OpcodeXOR(ReadByte(HL.Value)));
                                return new Opcode(op, $"xor a, {r8.Name}", 1, 1, () => OpcodeXOR(r8.Value));
                            case 6:
                                //or a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "or a, [hl]", 1, 2, () => OpcodeOR(ReadByte(HL.Value)));
                                return new Opcode(op, $"or a, {r8.Name}", 1, 1, () => OpcodeOR(r8.Value));
                            case 7:
                                //cp a, r[z]
                                if (z == 6)
                                    return new Opcode(op, "cp a, [hl]", 1, 2, () => OpcodeCP(ReadByte(HL.Value)));
                                return new Opcode(op, $"cp a, {r8.Name}", 1, 1, () => OpcodeCP(r8.Value));
                        }
                        break;
                    case 3:
                        switch (z)
                        {
                            case 0:
                                switch (y)
                                {
                                    case 4:
                                        //ld (0xFF00+n), a
                                        return new Opcode(op, "ld [0xFF00+${0:X2}], a", 2, 3, () => OpcodeLD((ushort)(0xFF00 + Immediate()), A.Value));
                                    case 5:
                                        //add sp, d
                                        return new Opcode(op, "add sp, d", 2, 4, () => OpcodeADDSP((sbyte)Immediate()));
                                    case 6:
                                        //ld a, (0xFF00+n)
                                        return new Opcode(op, "ld a, [0xFF00+${0:X2}]", 2, 3, () => OpcodeLD(A, ReadByte((ushort)(0xFF00 + Immediate()))));
                                    case 7:
                                        //ld hl, sp+d
                                        return new Opcode(op, "ld hl, sp+${0:X2}", 2, 3, () => OpcodeLDSP((sbyte)Immediate()));
                                    default:
                                        //ret cc[y]
                                        JumpCondition cc = (JumpCondition)y;
                                        return new Opcode(op, $"ret {y:g}", 1, 2, () => OpcodeRET(cc));
                                }
                            case 1:
                                switch(q)
                                {
                                    case 0:
                                        //pop rp2[p]
                                        r16 = rp2[p];
                                        return new Opcode(op, $"pop {r16.Name}", 1, 3, () => OpcodePOP(r16, r16 == AF));
                                    case 1:
                                        switch(p)
                                        {
                                            case 0:
                                                //ret
                                                return new Opcode(op, "ret", 1, 4, () => OpcodeRET());
                                            case 1:
                                                //reti
                                                return new Opcode(op, "reti", 1, 4, () => OpcodeRET(reti: true));
                                            case 2:
                                                //jp hl
                                                return new Opcode(op, "jp hl", 1, 1, () => OpcodeJP(HL.Value, JumpCondition.None));
                                            case 3:
                                                //ld sp, hl
                                                return new Opcode(op, "ld sp, hl", 1, 2, () => OpcodeLD(SP, HL.Value));
                                        }
                                        break;
                                }
                                break;
                            case 2:
                                switch (y)
                                {
                                    case 4:
                                        //ld [0xFF00+c], a
                                        return new Opcode(op, "ld [$FF00+c], a", 1, 2, () => OpcodeLD((ushort)(0xFF00 + C.Value), A.Value));
                                    case 5:
                                        //ld [nn], a
                                        return new Opcode(op, "ld [${0:X4}], a", 3, 4, () => OpcodeLD(Immediate16(), A.Value));
                                    case 6:
                                        //ld a, [0xFF00+c]
                                        return new Opcode(op, "ld a, [$FF00+c]", 1, 2, () => OpcodeLD(A, ReadByte((ushort)(0xFF00 + C.Value))));
                                    case 7:
                                        //ld a, [nn]
                                        return new Opcode(op, "ld a, [${0:X4}]", 3, 4, () => OpcodeLD(A, ReadByte(Immediate16())));
                                    default:
                                        // jp cc[y], nn
                                        JumpCondition cc = (JumpCondition)y;
                                        return new Opcode(op, $"jp {y:g}, ${{0:X4}}", 3, 3, () => OpcodeJP(Immediate16(), cc));
                                }
                            case 3:
                                switch (y)
                                {
                                    case 0:
                                        //jp nn
                                        return new Opcode(op, "jp ${0:X4}", 3, 4, () => OpcodeJP(Immediate16(), JumpCondition.None));
                                    case 1:
                                        //cb prefix
                                        return new Opcode(op, "cb", 1, 1, () => OpcodeCB());
                                    case 6:
                                        //di
                                        return new Opcode(op, "di", 1, 1, () => OpcodeDI());
                                    case 7:
                                        //ei
                                        return new Opcode(op, "ei", 1, 1, () => OpcodeEI());
                                }
                                break;
                            case 4:
                                //call cc[y], nn
                                if (y >= 0 && y <= 3)
                                {
                                    JumpCondition cc = (JumpCondition)y;
                                    return new Opcode(op, $"call {cc:g}, ${{0:X4}}", 1, 3, () => OpcodeCALL(Immediate16(), cc));
                                }        
                                break;
                            case 5:
                                switch(q)
                                {
                                    case 0:
                                        //push rp2[p]
                                        r16 = rp2[p];
                                        return new Opcode(op, $"push {r16.Name}", 1, 4, () => OpcodePUSH(r16));
                                    case 1:
                                        //call nn
                                        if (p == 0)
                                            return new Opcode(op, "call ${0:X4}", 3, 6, () => OpcodeCALL(Immediate16()));
                                        break;
                                }
                                break;
                            case 6:
                                switch (y)
                                {
                                    case 0:
                                        //add a, n
                                        return new Opcode(op, "add a, ${0:X2}", 2, 2, () => OpcodeADD(Immediate()));
                                    case 1:
                                        //adc a, n
                                        return new Opcode(op, "adc a, ${0:X2}", 2, 2, () => OpcodeADC(Immediate()));
                                    case 2:
                                        //sub a, n
                                        return new Opcode(op, "sub a, ${0:X2}", 2, 2, () => OpcodeSUB(Immediate()));
                                    case 3:
                                        //sbc a, n
                                        return new Opcode(op, "sbc a, ${0:X2}", 2, 2, () => OpcodeSBC(Immediate()));
                                    case 4:
                                        //and a, n
                                        return new Opcode(op, "and a, ${0:X2}", 2, 2, () => OpcodeAND(Immediate()));
                                    case 5:
                                        //xor a, n
                                        return new Opcode(op, "xor a, ${0:X2}", 2, 2, () => OpcodeXOR(Immediate()));
                                    case 6:
                                        //or a, n
                                        return new Opcode(op, "or a, ${0:X2}", 2, 2, () => OpcodeOR(Immediate()));
                                    case 7:
                                        //cp a, n
                                        return new Opcode(op, "cp a, ${0:X2}", 2, 2, () => OpcodeCP(Immediate()));
                                }
                                break;
                            case 7:
                                //rst y*8
                                return new Opcode(op, "rst vec", 1, 4, () => OpcodeRST((byte)(y * 8)));
                        }
                        break;
                }
            else
            {
                r8 = r[z];
                switch (x)
                {
                    case 0:
                        switch(y)
                        {
                            case 0:
                                //rlc r[z]
                                if (z == 6)
                                    return new Opcode(op, "rlc [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeRLC(ReadByte(HL.Value))));
                                return new Opcode(op, $"rlc {r8.Name}", 2, 2, () => r8.Value = OpcodeRLC(r8.Value));
                            case 1:
                                //rrc r[z]
                                if (z == 6)
                                    return new Opcode(op, "rrc [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeRRC(ReadByte(HL.Value))));
                                return new Opcode(op, $"rrc {r8.Name}", 2, 2, () => r8.Value = OpcodeRRC(r8.Value));
                            case 2:
                                //rl r[z]
                                if (z == 6)
                                    return new Opcode(op, "rl [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeRL(ReadByte(HL.Value))));
                                return new Opcode(op, $"rl {r8.Name}", 2, 2, () => r8.Value = OpcodeRL(r8.Value));
                            case 3:
                                //rr r[z]
                                if (z == 6)
                                    return new Opcode(op, "rr [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeRR(ReadByte(HL.Value))));
                                return new Opcode(op, $"rr {r8.Name}", 2, 2, () => r8.Value = OpcodeRR(r8.Value));
                            case 4:
                                //sla r[z]
                                if (z == 6)
                                    return new Opcode(op, "sla [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeSLA(ReadByte(HL.Value))));
                                return new Opcode(op, $"sla {r8.Name}", 2, 2, () => r8.Value = OpcodeSLA(r8.Value));
                            case 5:
                                //sra r[z]
                                if (z == 6)
                                    return new Opcode(op, "sra [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeSRA(ReadByte(HL.Value))));
                                return new Opcode(op, $"sra {r8.Name}", 2, 2, () => r8.Value = OpcodeSRA(r8.Value));
                            case 6:
                                //swap r[z]
                                if (z == 6)
                                    return new Opcode(op, "swap [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeSWAP(ReadByte(HL.Value))));
                                return new Opcode(op, $"swap {r8.Name}", 2, 2, () => r8.Value = OpcodeSWAP(r8.Value));
                            case 7:
                                //srl r[z]
                                if (z == 6)
                                    return new Opcode(op, "srl [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeSRL(ReadByte(HL.Value))));
                                return new Opcode(op, $"srl {r8.Name}", 2, 2, () => r8.Value = OpcodeSRL(r8.Value));
                        }
                        break;
                    case 1:
                        //bit y, r[z]
                        if (z == 6)
                            return new Opcode(op, $"bit {y:X}, [hl]", 2, 3, () => OpcodeBIT(ReadByte(HL.Value), (byte)y));
                        return new Opcode(op, $"bit {y:X}, {r8.Name}", 2, 2, () => OpcodeBIT(r8.Value, (byte)y));
                    case 2:
                        //res y, r[z]
                        if (z == 6)
                            return new Opcode(op, $"res {y:X}, [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeRES(ReadByte(HL.Value), (byte)y)));
                        return new Opcode(op, $"res {y:X}, {r8.Name}", 2, 2, () => r8.Value = OpcodeRES(r8.Value, (byte)y));
                    case 3:
                        //set y, r[z]
                        if (z == 6)
                            return new Opcode(op, $"set {y:X}, [hl]", 2, 4, () => WriteByte(HL.Value, OpcodeSET(ReadByte(HL.Value), (byte)y)));
                        return new Opcode(op, $"set {y:X}, {r8.Name}", 2, 2, () => r8.Value = OpcodeSET(r8.Value, (byte)y));
                }
            }

            // Halt and Catch Fire
            return new Opcode(op, "hcf", 1, 1, () => OpcodeHCF());
        }
    }
}
