
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // 0xF2 - REPNE prefix

        private sealed class I_F2_REPNE_Prefix : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1
                int count = mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8);

                int nextOp, imm, imm2;
                switch (nextOp = cpu.GetInstructionByte())
                {
                    //
                    // 0xA4 - REPNE MOVSB
                    //

                    case 0xA4:

                        while (count != 0)
                        {
                            mem[es + di] = mem[ds + si];
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xA5 - REPNE MOVSW
                    //

                    case 0xA5:

                        dir <<= 1;      // -2 or +2
                        while (count != 0)
                        {
                            mem[es + di                 ] = mem[ds + si    ];
                            mem[es + ((di + 1) & 0xFFFF)] = mem[ds + ((si + 1) & 0xFFFF)];
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xAA - REPNE STOSB
                    //

                    case 0xAA:

                        imm = mem[(int) Reg.AX];
                        while (count != 0)
                        {
                            mem[es + di] = (byte) imm;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xAB - REPNE STOSW
                    //

                    case 0xAB:

                        dir <<= 1;      // -2 or +2
                        imm  = mem[(int) Reg.AX];
                        imm2 = mem[(int) Reg.AX + 1];
                        while (count != 0)
                        {
                            mem[es + di    ] = (byte) imm;
                            mem[es + di + 1] = (byte) imm2;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xAE - REPNE SCASB
                    //

                    case 0xAE:

                        imm = mem[(int) Reg.AX];
                        while (count != 0)
                        {
                            cpu.Sub_Byte(imm, mem[es + di]);
                            di = (di + dir) & 0xFFFF;
                            count--;
                            if (cpu.zeroFlag == 0)  // break if ZF=1
                                break;
                        }
                        break;

                    //
                    // 0xAF - REPNE SCASW
                    //

                    case 0xAF:

                        dir <<= 1;      // -2 or +2
                        imm = mem[(int) Reg.AX];
                        imm2 = mem[(int) Reg.AX + 1];
                        while (count != 0)
                        {
                            cpu.Sub_Word(imm2, mem[es + di]
                                            | (mem[es + ((di + 1) & 0xFFFF)] << 8));
                            di = (di + dir) & 0xFFFF;
                            count--;
                            if (cpu.zeroFlag == 0)  // break if ZF=1
                                break;
                        }
                        break;

                    //
                    // invalid instruction
                    //

                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xF2, nextOp);
                        return;
                }

                mem[(int) Reg.CX    ] = (byte) count;
                mem[(int) Reg.CX + 1] = (byte) (count >> 8);
                mem[(int) Reg.SI    ] = (byte) si;
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) di;
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "REPNE";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF3 - REP/REPE prefix

        private sealed class I_F3_REP_Prefix : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1
                int count = mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8);

                int nextOp, imm, imm2;
                switch (nextOp = cpu.GetInstructionByte())
                {
                    //
                    // 0xA4 - REP MOVSB
                    //

                    case 0xA4:

                        while (count != 0)
                        {
                            mem[es + di] = mem[ds + si];
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xA5 - REP MOVSW
                    //

                    case 0xA5:

                        dir <<= 1;      // -2 or +2
                        while (count != 0)
                        {
                            mem[es + di                 ] = mem[ds + si    ];
                            mem[es + ((di + 1) & 0xFFFF)] = mem[ds + ((si + 1) & 0xFFFF)];
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xA6 - REPE CMPSB
                    //

                    case 0xA6:

                        while (count != 0)
                        {
                            cpu.Sub_Byte(mem[ds + si], mem[es + di]);
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                            if (cpu.zeroFlag != 0)  // break if ZF=0
                                break;
                        }
                        break;

                    //
                    // 0xA7 - REPE CMPSW
                    //

                    case 0xA7:

                        dir <<= 1;      // -2 or +2
                        while (count != 0)
                        {
                            cpu.Sub_Word(mem[ds + si] | (mem[ds + ((si + 1) & 0xFFFF)] << 8),
                                         mem[es + di] | (mem[es + ((di + 1) & 0xFFFF)] << 8));
                            si = (si + dir) & 0xFFFF;
                            di = (di + dir) & 0xFFFF;
                            count--;
                            if (cpu.zeroFlag != 0)  // break if ZF=0
                                break;
                        }
                        break;

                    //
                    // 0xAA - REP STOSB
                    //

                    case 0xAA:

                        imm = mem[(int) Reg.AX];
                        while (count != 0)
                        {
                            mem[es + di] = (byte) imm;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xAB - REP STOSW
                    //

                    case 0xAB:

                        dir <<= 1;      // -2 or +2
                        imm  = mem[(int) Reg.AX];
                        imm2 = mem[(int) Reg.AX + 1];
                        while (count != 0)
                        {
                            mem[es + di    ] = (byte) imm;
                            mem[es + di + 1] = (byte) imm2;
                            di = (di + dir) & 0xFFFF;
                            count--;
                        }
                        break;

                    //
                    // 0xAE - REPE SCASB
                    //

                    case 0xAE:

                        imm = mem[(int) Reg.AX];
                        while (count != 0)
                        {
                            cpu.Sub_Byte(imm, mem[es + di]);
                            di = (di + dir) & 0xFFFF;
                            count--;
                            if (cpu.zeroFlag != 0)  // break if ZF=0
                                break;
                        }
                        break;

                    //
                    // invalid instruction
                    //

                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xF3, nextOp);
                        return;
                }

                mem[(int) Reg.CX    ] = (byte) count;
                mem[(int) Reg.CX + 1] = (byte) (count >> 8);
                mem[(int) Reg.SI    ] = (byte) si;
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) di;
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "REP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA4 - MOVSB

        private sealed class I_A4_MOVSB : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1

                mem[es + di] = mem[ds + si];

                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "MOVSB";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA5 - MOVSW

        private sealed class I_A5_MOVSW : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 4 + 2;    // get -2 or +2

                mem[es + di                 ] = mem[ds + si];
                mem[es + ((di + 1) & 0xFFFF)] = mem[ds + ((si + 1) & 0xFFFF)];

                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "MOVSW";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA6 - CMPSB

        private sealed class I_A6_CMPSB : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1

                cpu.Sub_Byte(mem[ds + si], mem[es + di]);

                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CMPSB";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA7 - CMPSW

        private sealed class I_A7_CMPSW : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 4 + 2;    // get -2 or +2

                cpu.Sub_Word(mem[ds + si] | (mem[ds + ((si + 1) & 0xFFFF)] << 8),
                             mem[es + di] | (mem[es + ((di + 1) & 0xFFFF)] << 8));

                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CMPSW";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAA - STOSB

        private sealed class I_AA_STOSB : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1

                mem[es + di] = mem[(int) Reg.AX];
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "STOSB";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAB - STOSW

        private sealed class I_AB_STOSW : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 4 + 2;    // get -2 or +2

                mem[es + di]                  = mem[(int) Reg.AX];
                mem[es + ((di + 1) & 0xFFFF)] = mem[(int) Reg.AX + 1];
                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "STOSW";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAC - LODSB

        private sealed class I_AC_LODSB : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1

                mem[(int) Reg.AX    ] = mem[ds + si];
                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "LODSB";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAD - LODSW

        private sealed class I_AD_LODSW : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds = cpu.dataSegmentAddress;
                int si = mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 4 + 2;    // get -2 or +2

                mem[(int) Reg.AX    ] = mem[ds + si];
                mem[(int) Reg.AX + 1] = mem[ds + ((si + 1) & 0xFFFF)];
                mem[(int) Reg.SI    ] = (byte) (si = (si + dir) & 0xFFFF);
                mem[(int) Reg.SI + 1] = (byte) (si >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "LODSW";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAE - SCASB

        private sealed class I_AE_SCASB : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 2 + 1;    // get -1 or +1

                cpu.Sub_Byte(mem[(int) Reg.AX], mem[es + di]);

                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "SCASB";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xAF - SCASW

        private sealed class I_AF_SCASW : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int es = cpu.extraSegmentAddress;
                int di = mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8);
                int dir = (cpu.directionFlag >> 31) * 4 + 2;    // get -2 or +2

                cpu.Sub_Word(mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8),
                             mem[es + di] | (mem[es + ((di + 1) & 0xFFFF)] << 8));

                mem[(int) Reg.DI    ] = (byte) (di = (di + dir) & 0xFFFF);
                mem[(int) Reg.DI + 1] = (byte) (di >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "SCASW";
            #endif
        }

    }
}
