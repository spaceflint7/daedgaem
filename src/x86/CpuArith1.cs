
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // add byte and set flags

        private int Add_Byte (int x, int y)
        {
            int r = (x + y) & 0xFF;

            // CF < 0 if (r < x)
            carryFlag = r - x;
            // AF < 0 if (x & 0x0F) + (y & 0x0F) > 0x0F
            // (i.e., carry out of bit 3)
            adjustFlag = 0x0F - ((x & 0x0F) + (y & 0x0F));
            // SF < 0 if (r & 0x80) != 0
            signFlag = r << 24;
            // OF < 0 if ((x & 0x80) == (y & 0x80)) && ((r & 0x80) != (x & 0x80))
            // (i.e., x and y have same sign, and r has opposite sign)
            overflowFlag = ((x ^ y ^ -1) & (r ^ x)) << 24;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // add word and set flags

        private int Add_Word (int x, int y)
        {
            int r = (x + y) & 0xFFFF;

            // CF < 0 if (r < x)
            carryFlag = r - x;
            // AF < 0 if (x & 0x0F) + (y & 0x0F) > 0x0F
            // (i.e., carry out of bit 3)
            adjustFlag = 0x0F - ((x & 0x0F) + (y & 0x0F));
            // SF > 0 if (r & 0x8000) != 0
            signFlag = r << 16;
            // OF != 0 if ((x & 0x8000) == (y & 0x8000)) && ((r & 0x8000) != (x & 0x8000))
            // (i.e., x and y have same sign, and r has opposite sign)
            overflowFlag = ((x ^ y ^ -1) & (r ^ x)) << 16;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // subtract byte and set flags

        private int Sub_Byte (int x, int y)
        {
            int r = (x - y) & 0xFF;

            // CF < 0 if (x < y)
            carryFlag = x - y;
            // AF < 0 if (x & 0x0F) < (y & 0x0F)
            // (i.e., borrow into bit 3)
            adjustFlag = (x & 0x0F) - (y & 0x0F);
            // SF < 0 if (r & 0x80) != 0
            signFlag = r << 24;
            // OF != 0 if ((x & 0x80) != (y & 0x80)) && ((r & 0x80) == (y & 0x80))
            // (i.e., x and y have opposite sign, and r has same sign as y)
            overflowFlag = ((x ^ y) & (r ^ y ^ -1)) << 24;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // subtract word and set flags

        private int Sub_Word (int x, int y)
        {
            int r = (x - y) & 0xFFFF;

            // CF < 0 if (x < y)
            carryFlag = x - y;
            // AF < 0 if (x & 0x0F) < (y & 0x0F)
            // (i.e., borrow into bit 3)
            adjustFlag = (x & 0x0F) - (y & 0x0F);
            // SF < 0 if (r & 0x8000) != 0
            signFlag = r << 16;
            // OF != 0 if ((x & 0x8000) != (y & 0x8000)) && ((r & 0x8000) == (y & 0x8000))
            // (i.e., x and y have opposite sign, and r has same sign as y)
            overflowFlag = ((x ^ y) & (r ^ y ^ -1)) << 16;

            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'and' byte and set flags

        private int And_Byte (int x, int y)
        {
            int r = x & y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 24;             // SF < 0 if (r & 0x80) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'and' word and set flags

        private int And_Word (int x, int y)
        {
            int r = x & y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 16;             // SF < 0 if (r & 0x8000) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'or' byte and set flags

        private int Or_Byte (int x, int y)
        {
            int r = x | y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 24;             // SF < 0 if (r & 0x80) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'or' word and set flags

        private int Or_Word (int x, int y)
        {
            int r = x | y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 16;             // SF < 0 if (r & 0x8000) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'xor' byte and set flags

        private int Xor_Byte (int x, int y)
        {
            int r = x ^ y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 24;             // SF < 0 if (r & 0x80) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 'xor' word and set flags

        private int Xor_Word (int x, int y)
        {
            int r = x ^ y;

            carryFlag = overflowFlag = 0;
            signFlag = r << 16;             // SF < 0 if (r & 0x8000) != 0
            return zeroFlag = parityFlag = r;
        }

        // --------------------------------------------------------------------
        // 0x00 - ADD Eb,Gb

        private sealed class I_00_Add_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Add_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADD", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x01 - ADD Ew,Gw

        private sealed class I_01_Add_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Add_Word(mem[ew] | (mem[ew + 1] << 8),
                                       mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADD", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x02 - ADD Gb,Eb

        private sealed class I_02_Add_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Add_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADD", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x03 - ADD Gw,Ew

        private sealed class I_03_Add_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Add_Word(mem[gw] | (mem[gw + 1] << 8),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADD", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x04 - ADD AL,Ib

        private sealed class I_04_Add_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Add_Byte(mem[(int) Reg.AX],
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("ADD", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x05 - ADD AX,Iw

        private sealed class I_05_Add_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Add_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("ADD", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x08 - OR Eb,Gb

        private sealed class I_08_Or_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Or_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("OR", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x09 - OR Ew,Gw

        private sealed class I_09_Or_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Or_Word(mem[ew] | (mem[ew + 1] << 8),
                                      mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("OR", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x0A - OR Gb,Eb

        private sealed class I_0A_Or_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Or_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("OR", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x0B - OR Gw,Ew

        private sealed class I_0B_Or_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Or_Word(mem[gw] | (mem[gw + 1] << 8),
                                      mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("OR", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x0C - OR AL,Ib

        private sealed class I_0C_Or_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Or_Byte(mem[(int) Reg.AX],
                                    cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("OR", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x0D - OR AX,Iw

        private sealed class I_0D_Or_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Or_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("OR", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x10 - ADC Eb,Gb

        private sealed class I_10_Adc_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Add_Byte(
                                    // if carry flag < 0, add 1
                                    mem[eb] - (cpu.carryFlag >> 31),
                                    mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADC", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x11 - ADC Ew,Gw

        private sealed class I_11_Adc_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Add_Word((mem[ew] | (mem[ew + 1] << 8))
                                       // if carry flag < 0, add 1
                                       - (cpu.carryFlag >> 31),
                                       mem[gb] | (mem[gb + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADC", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x12 - ADC Gb,Eb

        private sealed class I_12_Adc_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Add_Byte(
                                    // if carry flag < 0, add 1
                                    mem[gb] - (cpu.carryFlag >> 31),
                                    mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADC", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x13 - ADC Gw,Ew

        private sealed class I_13_Adc_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Add_Word((mem[gw] | (mem[gw + 1] << 8))
                                       // if carry flag < 0, add 1
                                       - (cpu.carryFlag >> 31),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("ADC", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x14 - ADC AL,Ib

        private sealed class I_14_Adc_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Add_Byte(mem[(int) Reg.AX]
                                     // if carry flag < 0, add 1
                                     - (cpu.carryFlag >> 31),
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("ADC", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x15 - ADC AX,Iw

        private sealed class I_15_Adc_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Add_Word(
                                (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8))
                                // if carry flag < 0, add 1
                                - (cpu.carryFlag >> 31),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("ADC", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x18 - SBB Eb,Gb

        private sealed class I_18_Sbb_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Sub_Byte(
                                    // if carry flag < 0, subtract 1
                                    mem[eb] + (cpu.carryFlag >> 31),
                                    mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SBB", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x19 - SBB Ew,Gw

        private sealed class I_19_Sbb_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Sub_Word((mem[ew] | (mem[ew + 1] << 8))
                                       // if carry flag < 0, subtract 1
                                       + (cpu.carryFlag >> 31),
                                       mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SBB", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1A - SBB Gb,Eb

        private sealed class I_1A_Sbb_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Sub_Byte(
                                    // if carry flag < 0, subtract 1
                                    mem[gb] + (cpu.carryFlag >> 31),
                                    mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SBB", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1B - SBB Gw,Ew

        private sealed class I_1B_Sbb_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Sub_Word((mem[gw] | (mem[gw + 1] << 8))
                                       // if carry flag < 0, subtract 1
                                       + (cpu.carryFlag >> 31),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SBB", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1C - SBB AL,Ib

        private sealed class I_1C_Sbb_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Sub_Byte(mem[(int) Reg.AX]
                                     // if carry flag < 0, subtract 1
                                     + (cpu.carryFlag >> 31),
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("SBB", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1D - SBB AX,Iw

        private sealed class I_1D_Sbb_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Sub_Word(
                                (mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8))
                                // if carry flag < 0, subtract 1
                                + (cpu.carryFlag >> 31),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("SBB", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x20 - AND Eb,Gb

        private sealed class I_20_And_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.And_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("AND", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x21 - AND Ew,Gw

        private sealed class I_21_And_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.And_Word(mem[ew] | (mem[ew + 1] << 8),
                                       mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("AND", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x22 - AND Gb,Eb

        private sealed class I_22_And_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.And_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("AND", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x23 - AND Gw,Ew

        private sealed class I_23_And_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.And_Word(mem[gw] | (mem[gw + 1] << 8),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("AND", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x24 - AND AL,Ib

        private sealed class I_24_And_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.And_Byte(mem[(int) Reg.AX],
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("AND", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x25 - AND AX,Iw

        private sealed class I_25_And_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.And_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("AND", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x28 - SUB Eb,Gb

        private sealed class I_28_Sub_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Sub_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SUB", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x29 - SUB Ew,Gw

        private sealed class I_29_Sub_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8),
                                       mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SUB", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2A - SUB Gb,Eb

        private sealed class I_2A_Sub_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Sub_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SUB", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2B - SUB Gw,Ew

        private sealed class I_2B_Sub_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Sub_Word(mem[gw] | (mem[gw + 1] << 8),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("SUB", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2C - SUB AL,Ib

        private sealed class I_2C_Sub_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Sub_Byte(mem[(int) Reg.AX],
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("SUB", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2D - SUB AX,Iw

        private sealed class I_2D_Sub_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Sub_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("SUB", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x30 - XOR Eb,Gb

        private sealed class I_30_Xor_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = (byte) cpu.Xor_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XOR", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x31 - XOR Ew,Gw

        private sealed class I_31_Xor_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var res = cpu.Xor_Word(mem[ew] | (mem[ew + 1] << 8),
                                       mem[gw] | (mem[gw + 1] << 8));
                mem[ew    ] = (byte) res;
                mem[ew + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XOR", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x32 - XOR Gb,Eb

        private sealed class I_32_Xor_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[gb] = (byte) cpu.Xor_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XOR", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x33 - XOR Gw,Ew

        private sealed class I_33_Xor_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var res = cpu.Xor_Word(mem[gw] | (mem[gw + 1] << 8),
                                       mem[ew] | (mem[ew + 1] << 8));
                mem[gw    ] = (byte) res;
                mem[gw + 1] = (byte) (res >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XOR", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x34 - XOR AL,Ib

        private sealed class I_34_Xor_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte)
                        cpu.Xor_Byte(mem[(int) Reg.AX],
                                     cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("XOR", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x35 - XOR AX,Iw

        private sealed class I_35_Xor_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var res = cpu.Xor_Word(
                                mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                                cpu.GetInstructionWord());
                mem[(int) Reg.AX    ] = (byte) res;
                mem[(int) Reg.AX + 1] = (byte) (res >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("XOR", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x38 - CMP Eb,Gb

        private sealed class I_38_Cmp_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                cpu.Sub_Byte(mem[eb], mem[gb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("CMP", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x39 - CMP Ew,Gw

        private sealed class I_39_Cmp_EwGw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8),
                             mem[gw] | (mem[gw + 1] << 8));

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("CMP", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x3A - CMP Gb,Eb

        private sealed class I_3A_Cmp_GbEb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                cpu.Sub_Byte(mem[gb], mem[eb]);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("CMP", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x3B - CMP Gw,Ew

        private sealed class I_3B_Cmp_GwEw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                cpu.Sub_Word(mem[gw] | (mem[gw + 1] << 8),
                             mem[ew] | (mem[ew + 1] << 8));

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("CMP", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x3C - CMP AL,Ib

        private sealed class I_3C_Cmp_AL : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Sub_Byte(mem[(int) Reg.AX], cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("CMP", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x3D - CMP AX,Iw

        private sealed class I_3D_Cmp_AX : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Sub_Word(mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                             cpu.GetInstructionWord());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("CMP", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x80 - arith Eb,Ib

        private sealed class I_80_Arith_EbIb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var ib = cpu.GetInstructionByte();

                switch ((modrm >> 3) & 7)
                {
                    case 0:     // ADD
                        mem[eb] = (byte) cpu.Add_Byte(mem[eb], ib);
                        return;
                    case 1:     // OR
                        mem[eb] = (byte) cpu.Or_Byte(mem[eb], ib);
                        return;
                    case 2:     // ADC
                        mem[eb] = (byte)
                            cpu.Add_Byte(// if carry flag < 0, add 1
                                         mem[eb] - (cpu.carryFlag >> 31), ib);
                        return;
                    case 3:     // SBB
                        mem[eb] = (byte)
                            cpu.Sub_Byte(// if carry flag < 0, subtract 1
                                         mem[eb] + (cpu.carryFlag >> 31), ib);
                        return;
                    case 4:     // AND
                        mem[eb] = (byte) cpu.And_Byte(mem[eb], ib);
                        return;
                    case 5:     // SUB
                        mem[eb] = (byte) cpu.Sub_Byte(mem[eb], ib);
                        return;
                    case 6:     // XOR
                        mem[eb] = (byte) cpu.Xor_Byte(mem[eb], ib);
                        return;
                    case 7:     // CMP
                        cpu.Sub_Byte(mem[eb], ib);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                int modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmImm("ADD", 1, modrm),
                    0x08 => cpu.PrintModrmImm("OR",  1, modrm),
                    0x10 => cpu.PrintModrmImm("ADC", 1, modrm),
                    0x18 => cpu.PrintModrmImm("SBB", 1, modrm),
                    0x20 => cpu.PrintModrmImm("AND", 1, modrm),
                    0x28 => cpu.PrintModrmImm("SUB", 1, modrm),
                    0x30 => cpu.PrintModrmImm("XOR", 1, modrm),
                    0x38 => cpu.PrintModrmImm("CMP", 1, modrm),
                    _    => "???",
                };
            }
            #endif
        }

        // --------------------------------------------------------------------
        // 0x81 - arith Ew,Iw

        private sealed class I_81_Arith_EwIw : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var iw = cpu.GetInstructionWord();
                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif

                int res;
                switch ((modrm >> 3) & 7)
                {
                    case 0:     // ADD
                        res = cpu.Add_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 1:     // OR
                        res = cpu.Or_Word((mem[ew] | (mem[ew + 1] << 8))
                                          // if carry flag < 0, subtract 1
                                          + (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 2:     // ADC
                        res = cpu.Add_Word((mem[ew] | (mem[ew + 1] << 8))
                                           // if carry flag < 0, add 1
                                           - (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 3:     // SBB
                        res = cpu.Sub_Word((mem[ew] | (mem[ew + 1] << 8))
                                           // if carry flag < 0, subtract 1
                                           + (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 4:     // AND
                        res = cpu.And_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 5:     // SUB
                        res = cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 6:     // XOR
                        res = cpu.Xor_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 7:     // CMP
                        cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                int modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmImm("ADD", 2, modrm),
                    0x08 => cpu.PrintModrmImm("OR",  2, modrm),
                    0x10 => cpu.PrintModrmImm("ADC", 2, modrm),
                    0x18 => cpu.PrintModrmImm("SBB", 2, modrm),
                    0x20 => cpu.PrintModrmImm("AND", 2, modrm),
                    0x28 => cpu.PrintModrmImm("SUB", 2, modrm),
                    0x30 => cpu.PrintModrmImm("XOR", 2, modrm),
                    0x38 => cpu.PrintModrmImm("CMP", 2, modrm),
                    _    => "???",
                };
            }
            #endif
        }

        // --------------------------------------------------------------------
        // 0x83 - arith Ew,Ib

        private sealed class I_83_Arith_EwIb : Instruction
        {
            public override void Process(Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var iw = (int) (sbyte) cpu.GetInstructionByte();
                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif

                int res;
                switch ((modrm >> 3) & 7)
                {
                    case 0:     // ADD
                        res = cpu.Add_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 1:     // OR
                        res = cpu.Or_Word((mem[ew] | (mem[ew + 1] << 8))
                                          // if carry flag < 0, subtract 1
                                          + (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 2:     // ADC
                        res = cpu.Add_Word((mem[ew] | (mem[ew + 1] << 8))
                                           // if carry flag < 0, add 1
                                           - (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 3:     // SBB
                        res = cpu.Sub_Word((mem[ew] | (mem[ew + 1] << 8))
                                           // if carry flag < 0, subtract 1
                                           + (cpu.carryFlag >> 31), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 4:     // AND
                        res = cpu.And_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 5:     // SUB
                        res = cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 6:     // XOR
                        res = cpu.Xor_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;
                    case 7:     // CMP
                        cpu.Sub_Word(mem[ew] | (mem[ew + 1] << 8), iw);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                int modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmImm("ADD", 3, modrm),
                    0x08 => cpu.PrintModrmImm("OR",  3, modrm),
                    0x10 => cpu.PrintModrmImm("ADC", 3, modrm),
                    0x18 => cpu.PrintModrmImm("SBB", 3, modrm),
                    0x20 => cpu.PrintModrmImm("AND", 3, modrm),
                    0x28 => cpu.PrintModrmImm("SUB", 3, modrm),
                    0x30 => cpu.PrintModrmImm("XOR", 3, modrm),
                    0x38 => cpu.PrintModrmImm("CMP", 3, modrm),
                    _    => "???",
                };
            }
            #endif
        }

    }
}
