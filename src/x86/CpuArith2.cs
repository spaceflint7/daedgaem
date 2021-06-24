
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // increment byte and set flags

        private int Inc_Byte (int x)
        {
            int r = (x + 1) & 0xFF;

            // AF < 0 if (r & 0x0F) == 0 (i.e., carry out of bit 3)
            adjustFlag = (r & 0x0F) - 1;
            // SF < 0 if (r & 0x80) != 0
            signFlag = r << 24;
            // OF < 0 if r == 0x80 (i.e. positive to negative)
            overflowFlag = ((x & 0x80) - (r & 0x80)) << 24;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // increment word and set flags

        private int Inc_Word (int x)
        {
            int r = (x + 1) & 0xFFFF;

            // AF < 0 if (r & 0x0F) == 0 (i.e., carry out of bit 3)
            adjustFlag = (r & 0x0F) - 1;
            // SF < 0 if (r & 0x8000) != 0
            signFlag = r << 16;
            // OF < 0 if r == 0x8000 (i.e. positive to negative)
            overflowFlag = ((x & 0x8000) - (r & 0x8000)) << 16;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // increment byte and set flags

        private int Dec_Byte (int x)
        {
            int r = (x - 1) & 0xFF;

            // AF < 0 if (r & 0x0F) == 0x0F (i.e., borrow into bit 3)
            adjustFlag = 0x0E - (r & 0x0F);
            // SF < 0 if (r & 0x80) != 0
            signFlag = r << 24;
            // OF < 0 if r == 0x7F (i.e. negative to positive)
            overflowFlag = ((r & 0x80) - (x & 0x80)) << 24;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // increment byte and set flags

        private int Dec_Word (int x)
        {
            int r = (x - 1) & 0xFFFF;

            // AF < 0 if (r & 0x0F) == 0x0F (i.e., borrow into bit 3)
            adjustFlag = 0x0E - (r & 0x0F);
            // SF < 0 if (r & 0x8000) != 0
            signFlag = r << 16;
            // OF < 0 if r == 0x7F (i.e. negative to positive)
            overflowFlag = ((r & 0x8000) - (x & 0x8000)) << 16;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // shift or rotate a byte or a word

        private int ShiftRotate (int modrm, int bits, int count, int v)
        {
            int tmp;
            switch ((modrm >> 3) & 7)
            {
                // ROL - rotate left N bits
                // CF <- result low bit
                // OF <- result high bit xor result low bit

                case 0:
                    count &= bits - 1;
                    v = (int) (((uint) v << count) | ((uint) v >> (bits - count)));
                    overflowFlag = (carryFlag = v << 31) ^ (v << (32 - bits));
                    break;

                // ROR - rotate right N bits
                // CF <- result high bit
                // OF <- xor of two highest bits in result

                case 1:
                    count &= bits - 1;
                    v = (int) (((uint) v >> count) | ((uint) v << (bits - count)));
                    overflowFlag = (carryFlag = tmp = v << (32 - bits)) ^ (tmp << 1);
                    break;

                // RCL - rotate left through carry N bits
                // CF <- result high bit
                // OF <- result high bit xor result low bit

                case 2:
                    count = (count - 1) % (bits + 1);
                    v = (int) (   (uint) (((v << 1) | ((carryFlag >> 31) & 1)) << count)
                                | ((uint) v >> (bits - count)));
                    overflowFlag = (carryFlag = tmp = v << (31 - bits)) ^ (tmp << 1);
                    break;

                // RCR - rotate right through carry N bits
                // CF <- result low bit
                // OF <- high bit of original value xor original carry

                case 3:
                    count = (count - 1) % (bits + 1);
                    overflowFlag = (v << (32 - bits)) ^ (tmp = carryFlag);
                    // the carry flag is converted into 0 or 1, then
                    // shifted left 8 or 16 bits, then OR'ed with the
                    // 8 or 16 bits of the original value
                    carryFlag = (tmp = v | (((tmp >> 31) & 1) << bits)) << (31 - count);
                    v = (int) (((uint) tmp >> (count + 1)) | ((uint) v << (bits - count)));
                    break;

                // SHL - shift left N bits
                // SAL - signed/arithmetic shift left N bits
                // CF <- last bit shifted out of the operand
                //          (if count < bits; otherwise undefined)
                // OF <- result high bit xor last bit shifted
                //          (if count = 1; otherwise undefined)
                // SF, ZF, PF set according to result

                case 4:
                case 6:
                    overflowFlag = (carryFlag = v << (count + 31 - bits))
                                 ^ (signFlag =
                        // if count >= bits, we AND the result with zero;
                        // if count < bits, we AND the result with 0xFFFFFFFF
                                        (v = v << count & ((count - bits) >> 31))
                        // shift the 8- or 16-bit sign into a 32-bit sign
                                            << (32 - bits));
                    zeroFlag = ((1 << bits) - 1) & (parityFlag = v);
                    break;

                // SHR - shift right N bits
                // CF <- last bit shifted out of the operand
                //          (if count < bits; otherwise undefined)
                // OF <- high bit of initial value
                //          (if count = 1; otherwise undefined)
                // SF, ZF, PF set according to result

                case 5:
                    overflowFlag = v << (32 - bits);
                    carryFlag = v << (32 - count);
                    signFlag = (v = (int) ((uint) v >> count)
                        // if count >= bits, we AND the result with zero;
                        // if count < bits, we AND the result with 0xFFFFFFFF
                                        & ((count - bits) >> 31))
                        // shift the 8- or 16-bit sign into a 32-bit sign
                                            << (32 - bits);
                    zeroFlag = ((1 << bits) - 1) & (parityFlag = v);
                    break;

                // SAR - signed/arithmetic shift right N bits
                // CF <- last bit shifted out of the operand
                //          (if count < bits; otherwise undefined)
                // OF <- zero
                // SF, ZF, PF set according to result

                case 7:
                    overflowFlag = 0;
                    carryFlag = v << (32 - count);
                    // shift left 16 or 24 bits to get a 32-bit signed value,
                    // which can be used directly as the sign flag
                    signFlag = v = v << (32 - bits);
                    // now we can shift right to get a signed right shift.
                    // but note that we do two shift rights, because we may
                    // have to shift 16 on a 16-bit operand, yielding a 32 bit
                    // right shift, which would get masked into a zero shift.
                    v = ((v >> count >> (32 - bits))
                    // if count >= bits, we AND the result with zero;
                    // if count < bits, we AND the result with 0xFFFFFFFF
                            & (tmp = (count - bits) >> 31))
                    // if count < bits, we OR the result with zero;
                    // if count >= bits, we OR with zero or 0xFFFFFFFF,
                    // depending on the sign of the result
                                | ((~ tmp) & (v >> 31));
                    zeroFlag = ((1 << bits) - 1) & (parityFlag = v);
                    break;
            }

            return v;
        }

        // --------------------------------------------------------------------
        // 0x40 - INC AX

        private sealed class I_40_Inc_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8));
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x41 - INC CX

        private sealed class I_41_Inc_CX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8));
                mem[(int) Reg.CX    ] = (byte) res;
                mem[(int) Reg.CX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.CX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x42 - INC DX

        private sealed class I_42_Inc_DX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.DX] | (mem[(int) Reg.DX + 1] << 8));
                mem[(int) Reg.DX    ] = (byte) res;
                mem[(int) Reg.DX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.DX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x43 - INC BX

        private sealed class I_43_Inc_BX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8));
                mem[(int) Reg.BX    ] = (byte) res;
                mem[(int) Reg.BX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.BX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x44 - INC SP

        private sealed class I_44_Inc_SP : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8));
                mem[(int) Reg.SP    ] = (byte) res;
                mem[(int) Reg.SP + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.SP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x45 - INC BP

        private sealed class I_45_Inc_BP : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8));
                mem[(int) Reg.BP    ] = (byte) res;
                mem[(int) Reg.BP + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.BP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x46 - INC SI

        private sealed class I_46_Inc_SI : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8));
                mem[(int) Reg.SI    ] = (byte) res;
                mem[(int) Reg.SI + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.SI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x47 - INC DI

        private sealed class I_47_Inc_DI : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Inc_Word(
                                mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8));
                mem[(int) Reg.DI    ] = (byte) res;
                mem[(int) Reg.DI + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("INC", Reg.DI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x48 - DEC AX

        private sealed class I_48_Dec_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8));
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x49 - DEC CX

        private sealed class I_49_Dec_CX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8));
                mem[(int) Reg.CX    ] = (byte) res;
                mem[(int) Reg.CX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.CX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4A - DEC DX

        private sealed class I_4A_Dec_DX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.DX] | (mem[(int) Reg.DX + 1] << 8));
                mem[(int) Reg.DX    ] = (byte) res;
                mem[(int) Reg.DX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.DX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4B - DEC BX

        private sealed class I_4B_Dec_BX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8));
                mem[(int) Reg.BX    ] = (byte) res;
                mem[(int) Reg.BX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.BX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4C - DEC SP

        private sealed class I_4C_Dec_SP : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8));
                mem[(int) Reg.SP    ] = (byte) res;
                mem[(int) Reg.SP + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.SP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4D - DEC BP

        private sealed class I_4D_Dec_BP : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8));
                mem[(int) Reg.BP    ] = (byte) res;
                mem[(int) Reg.BP + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.BP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4E - DEC SI

        private sealed class I_4E_Dec_SI : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8));
                mem[(int) Reg.SI    ] = (byte) res;
                mem[(int) Reg.SI + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.SI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x4F - DEC DI

        private sealed class I_4F_Dec_DI : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Dec_Word(
                                mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8));
                mem[(int) Reg.DI    ] = (byte) res;
                mem[(int) Reg.DI + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintReg("DEC", Reg.DI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x84 - TEST Gb,Eb

        private sealed class I_84_Test_GbEb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                cpu.And_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("TEST", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x85 - TEST Gw,Ew

        private sealed class I_85_Test_GwEw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                cpu.And_Word(mem[gw] | (mem[gw + 1] << 8),
                             mem[ew] | (mem[ew + 1] << 8));

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("TEST", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA8 - TEST AL,Ib

        private sealed class I_A8_Test_AL_Ib : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.And_Byte(mem[(int) Reg.AX], cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                $"TEST    AL,{cpu.GetInstructionByte():X2}";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA9 - TEST AX,Iw

        private sealed class I_A9_Test_AX_Iw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.And_Byte(mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                             cpu.GetInstructionWord());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                $"TEST    AX,{cpu.GetInstructionWord():X4}";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD0 - shift Eb,1

        private sealed class I_D0_Shift_Eb1 : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[eb] = (byte) cpu.ShiftRotate(modrm, 8, 1, mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmShift(1, "1");
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD1 - shift Ew,1

        private sealed class I_D1_Shift_Ew1 : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int res = cpu.ShiftRotate(modrm, 16, 1, mem[ew] | (mem[ew + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmShift(2, "1");
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD2 - shift Eb,CL

        private sealed class I_D2_Shift_EbCL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int count;
                if ((count = /* CL */ mem[(int) Reg.CX]) != 0)
                {
                    mem[eb] = (byte) cpu.ShiftRotate(modrm, 8, count, mem[eb]);
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmShift(1, "CL");
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD3 - shift Ew,CL

        private sealed class I_D3_Shift_EwCL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int count;
                if ((count = /* CL */ mem[(int) Reg.CX]) != 0)
                {
                    int res = cpu.ShiftRotate(modrm, 16, count,
                                              mem[ew] | (mem[ew + 1] << 8));
                    mem[ew    ] = (byte) res;
                    mem[ew + 1] = (byte) (res >> 8);
                }

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmShift(2, "CL");
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF6 - TEST/NOT/NEG/MUL/IMUL/DIV/IDIV Eb

        private sealed class I_F6_TwoByte : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int res;

                switch ((modrm >> 3) & 7)
                {
                    case 0:     // TEST Eb,Ib
                    case 1:     // same
                        cpu.And_Byte(mem[eb], cpu.GetInstructionByte());
                        return;

                    case 2:     // NOT Eb
                        mem[eb] = (byte) (mem[eb] ^ -1);
                        return;

                    case 3:     // NEG Eb
                        mem[eb] = (byte) (res = -mem[eb]);
                        // AF < 0 if low 4-bits are not zero
                        cpu.adjustFlag = -(res & 0x0F);
                        // SF < 0 if (r & 0x80) != 0
                        cpu.signFlag = res << 24;
                        // set CF=1 (negative value) if result is non-zero
                        cpu.carryFlag = ((-1 - res) ^ (res - 1));
                        // set OV=1 if result (and source) is exactly 0x80
                        cpu.overflowFlag = ((0x7F - res) ^ (res - 0x81)) ^ -1;
                        // set ZF and PF
                        cpu.zeroFlag = cpu.parityFlag = res & 0xFF;
                        return;

                    case 4:     // MUL Eb
                        res = mem[(int) Reg.AX] * mem[eb];
                        // on 8086, ZF is set if lower half of result is zero
                        cpu.zeroFlag = res & 0xFF;
                        mem[(int) Reg.AX    ] = (byte) res;
                        mem[(int) Reg.AX + 1] = (byte) (res = res >> 8);
                        // set CF = OF = zero if result high 8-bits are zero
                        // (already shifted above);  otherwise CF=OF=1 (negative)
                        cpu.overflowFlag = cpu.carryFlag = ((-1 - res) ^ (res - 1));
                        return;

                    case 5:     // IMUL Eb
                        res = ((sbyte) mem[(int) Reg.AX]) * ((sbyte) mem[eb]);
                        // set CF=OF=1 (i.e. negative) if carry into high 8-bits
                        cpu.overflowFlag = cpu.carryFlag = ((sbyte) res) ^ res;
                        mem[(int) Reg.AX    ] = (byte) res;
                        mem[(int) Reg.AX + 1] = (byte) (res >> 8);
                        return;

                    case 6:     // DIV Eb
                        byte udiv;
                        if ((udiv = mem[eb]) == 0)
                            cpu.InvokeInterrupt(0); // divide by zero
                        else
                        {
                            var acc = (uint) (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8));
                            if ((res = (int) (acc / udiv)) > byte.MaxValue)
                                cpu.InvokeInterrupt(0); // divide overflow
                            else
                            {
                                mem[(int) Reg.AX    ] = (byte) res;
                                mem[(int) Reg.AX + 1] = (byte) (acc % udiv);
                            }
                        }
                        return;

                    case 7:     // IDIV Eb
                        sbyte sdiv;
                        if ((sdiv = (sbyte) mem[eb]) == 0)
                            cpu.InvokeInterrupt(0); // divide by zero
                        else
                        {
                            var acc = (int) (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8));
                            if (((res = (int) (acc / sdiv)) > sbyte.MaxValue) || (res < sbyte.MinValue))
                                cpu.InvokeInterrupt(0); // divide overflow
                            else
                            {
                                mem[(int) Reg.AX    ] = (byte) res;
                                mem[(int) Reg.AX + 1] = (byte) (acc % sdiv);
                            }
                        }
                        return;

                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xF6, modrm);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                var modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmImm("TEST", 1, modrm),
                    0x08 => cpu.PrintModrmImm("TEST", 1, modrm),
                    0x10 => cpu.PrintModrmNoReg("NOT", 1, modrm),
                    0x18 => cpu.PrintModrmNoReg("NEG", 1, modrm),
                    0x20 => cpu.PrintModrmNoReg("MUL", 1, modrm),
                    0x28 => cpu.PrintModrmNoReg("IMUL", 1, modrm),
                    0x30 => cpu.PrintModrmNoReg("DIV", 1, modrm),
                    0x38 => cpu.PrintModrmNoReg("IDIV", 1, modrm),
                    _    => "???",
                };
            }
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF7 - TEST/NOT/NEG/MUL/IMUL/DIV/IDIV Ew

        private sealed class I_F7_TwoByte : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int res;
                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif

                switch ((modrm >> 3) & 7)
                {
                    case 0:     // TEST Ew,Iw
                    case 1:     // same
                        cpu.And_Word(mem[ew] | (mem[ew + 1] << 8),
                                     cpu.GetInstructionWord());
                        return;

                    case 2:     // NOT Ew
                        mem[ew] = (byte) (res = ((mem[ew] | (mem[ew + 1] << 8)) ^ -1));
                        mem[ew + 1] = (byte) (res >> 8);
                        return;

                    case 3:     // NEG Ew
                        mem[ew] = (byte) (res = -(mem[ew] | (mem[ew + 1] << 8)));
                        mem[ew + 1] = (byte) (res >> 8);
                        // AF < 0 if low 4-bits are not zero
                        cpu.adjustFlag = -(res & 0x0F);
                        // SF < 0 if (r & 0x8000) != 0
                        cpu.signFlag = res << 16;
                        // set CF=1 (negative value) if result is non-zero
                        cpu.carryFlag = ((-1 - res) ^ (res - 1));
                        // set OV=1 if result (and source) is exactly 0x8000
                        cpu.overflowFlag = ((0x7FFF - res) ^ (res - 0x8001)) ^ -1;
                        // set ZF and PF
                        cpu.zeroFlag = cpu.parityFlag = res & 0xFF;
                        return;

                    case 4:     // MUL Ew
                        res = (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8))
                            * (mem[ew] | (mem[ew + 1] << 8));
                        // on 8086, ZF is set if lower half of result is zero
                        cpu.zeroFlag = res & 0xFFFF;
                        mem[(int) Reg.AX    ] = (byte) res;
                        mem[(int) Reg.AX + 1] = (byte) (res >> 8);
                        mem[(int) Reg.DX    ] = (byte) (res = res >> 16);
                        mem[(int) Reg.DX + 1] = (byte) (res >> 8);
                        // set CF = OF = zero if result high 16-bits are zero
                        // (already shifted above);  otherwise CF=OF=1 (negative)
                        cpu.overflowFlag = cpu.carryFlag = ((-1 - res) ^ (res - 1));
                        return;

                    case 5:     // IMUL Ew
                        res = ((short) (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8)))
                            * ((short) (mem[ew] | (mem[ew + 1] << 8)));
                        // set CF=OF=1 (i.e. negative) if carry into high 16-bits
                        cpu.overflowFlag = cpu.carryFlag = ((short) res) ^ res;
                        mem[(int) Reg.AX    ] = (byte) res;
                        mem[(int) Reg.AX + 1] = (byte) (res >> 8);
                        mem[(int) Reg.DX    ] = (byte) (res >> 16);
                        mem[(int) Reg.DX + 1] = (byte) (res >> 24);
                        return;

                    case 6:     // DIV Ew
                        ushort udiv;
                        if ((udiv = (ushort) (mem[ew] | (mem[ew + 1] << 8))) == 0)
                            cpu.InvokeInterrupt(0); // divide by zero
                        else
                        {
                            var acc = (uint) (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8)
                                    | (mem[(int) Reg.DX] << 16) | (mem[(int) Reg.DX + 1] << 24));
                            if ((res = (int) (acc / udiv)) > ushort.MaxValue)
                                cpu.InvokeInterrupt(0); // divide overflow
                            else
                            {
                                mem[(int) Reg.AX    ] = (byte) res;
                                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
                                mem[(int) Reg.DX    ] = (byte) (res = (int) (acc % udiv));
                                mem[(int) Reg.DX + 1] = (byte) (res >> 8);
                            }
                        }
                        return;

                    case 7:     // IDIV Ew
                        short sdiv;
                        if ((sdiv = (short) (mem[ew] | (mem[ew + 1] << 8))) == 0)
                            cpu.InvokeInterrupt(0); // divide by zero
                        else
                        {
                            var acc = (uint) (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8)
                                    | (mem[(int) Reg.DX] << 16) | (mem[(int) Reg.DX + 1] << 24));
                            if (((res = (int) (acc / sdiv)) > short.MaxValue) || (res < short.MinValue))
                                cpu.InvokeInterrupt(0); // divide overflow
                            else
                            {
                                mem[(int) Reg.AX    ] = (byte) res;
                                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
                                mem[(int) Reg.DX    ] = (byte) (res = (int) (acc % sdiv));
                                mem[(int) Reg.DX + 1] = (byte) (res >> 8);
                            }
                        }
                        return;

                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xF7, modrm);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                var modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmImm("TEST", 2, modrm),
                    0x08 => cpu.PrintModrmImm("TEST", 2, modrm),
                    0x10 => cpu.PrintModrmNoReg("NOT", 2, modrm),
                    0x18 => cpu.PrintModrmNoReg("NEG", 2, modrm),
                    0x20 => cpu.PrintModrmNoReg("MUL", 2, modrm),
                    0x28 => cpu.PrintModrmNoReg("IMUL", 2, modrm),
                    0x30 => cpu.PrintModrmNoReg("DIV", 2, modrm),
                    0x38 => cpu.PrintModrmNoReg("IDIV", 2, modrm),
                    _    => "???",
                };
            }
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFE - INC/DEC

        private sealed class I_FE_TwoByte : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);

                switch ((modrm >> 3) & 7)
                {
                    case 0:     // INC
                        mem[eb] = (byte) cpu.Inc_Byte(mem[eb]);
                        return;
                    case 1:     // DEC
                        mem[eb] = (byte) cpu.Dec_Byte(mem[eb]);
                        return;
                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xFE, modrm);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                var modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmNoReg("INC", 1, modrm),
                    0x08 => cpu.PrintModrmNoReg("DEC", 1, modrm),
                    _    => I_FE_TwoByte.InvalidInstructionAndGoBack1(cpu),
                };
            }

            public static string InvalidInstructionAndGoBack1 (Cpu cpu)
            {
                // for invalid two-byte instructions that begin with
                // 0xFE or 0xFF, we go back one byte
                cpu.InstructionAddress--;
                return "???";
            }
            #endif
        }

    }
}
