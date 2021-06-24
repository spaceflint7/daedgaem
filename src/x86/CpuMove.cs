
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // swap word

        private void Swap_Word(int x, int y)
        {
            var mem    = stateBytes;
            var x_lo   = mem[x    ];
            var x_hi   = mem[x + 1];
            mem[x    ] = mem[y    ];
            mem[x + 1] = mem[y + 1];
            mem[y    ] = x_lo;
            mem[y + 1] = x_hi;
        }

        // --------------------------------------------------------------------
        // 0x86 - XCHG Gb,Eb

        private sealed class I_86_Xchg_GbEb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                var x = mem[gb];
                mem[gb] = mem[eb];
                mem[eb] = x;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XCHG", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x87 - XCHG Gw,Ew

        private sealed class I_87_Xchg_GwEw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                cpu.Swap_Word(ew, gw);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("XCHG", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x88 - MOV Eb,Gb

        private sealed class I_88_Mov_EbGb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[eb] = mem[gb];
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("MOV", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x89 - MOV Ew,Gw

        private sealed class I_89_Mov_EwGw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                mem[ew    ] = mem[gw    ];
                mem[ew + 1] = mem[gw + 1];

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("MOV", 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8A - MOV Gb,Eb

        private sealed class I_8A_Mov_GbEb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gb = (modrm & 0x18) + (int) Reg.AX  // select AX..DX
                       + ((modrm & 0x20) >> 5);         // select low or high
                mem[gb] = mem[eb];
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("MOV", -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8B - MOV Gw,Ew

        private sealed class I_8B_Mov_GwEw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                mem[gw    ] = mem[ew    ];
                mem[gw + 1] = mem[ew + 1];

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("MOV", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8C - MOV Ew,Sw

        private sealed class I_8C_Mov_EwSw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var sw = (modrm & 0x38) + (int) Reg.ES; // select seg reg
                mem[ew    ] = mem[sw    ];
                mem[ew + 1] = mem[sw + 1];

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmSegReg("MOV", false);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8D - LEA Gw,Ew

        private sealed class I_8D_Lea_GwEw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                // modrm decoding for LEA must not consider segment addresses
                var oldDataSeg = cpu.dataSegmentAddress;
                var oldStackSeg = cpu.stackSegmentAddressForModrm;
                cpu.dataSegmentAddress = cpu.stackSegmentAddressForModrm = 0;

                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                if (ew > 0xFFFFF)
                    I_XX_Invalid.ThrowInvalid(cpu, 0x8D, modrm);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg
                mem[gw    ] = (byte) ew;
                mem[gw + 1] = (byte) (ew >> 8);

                cpu.dataSegmentAddress = oldDataSeg;
                cpu.stackSegmentAddressForModrm = oldStackSeg;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("LEA", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8E - MOV Sw,Ew

        private sealed class I_8E_Mov_SwEw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var sw = (modrm & 0x38) + (int) Reg.ES; // select seg reg
                mem[sw    ] = mem[ew    ];
                mem[sw + 1] = mem[ew + 1];
                cpu.CacheSegmentRegisters();

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif

                if (sw == (int) Reg.SS)
                {
                    // updating SS disables interrupts for one instruction,
                    // to permit the following instruction to update SP
                    cpu.Step();

                    // tell SegmentOverride () not to restore SS
                    if ((cpu.segmentOverrideFlags & inSegmentOverride) != 0)
                        cpu.segmentOverrideFlags |= ssSegmentUpdated;
                }

                else if (sw == (int) Reg.DS)
                {
                    // tell SegmentOverride () not to restore DS
                    if ((cpu.segmentOverrideFlags & inSegmentOverride) != 0)
                        cpu.segmentOverrideFlags |= dsSegmentUpdated;
                }

            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrmSegReg("MOV", true);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x90 - NOP

        private sealed class I_90_Nop : Instruction
        {
            public override void Process (Cpu cpu) { }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "NOP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x91 - XCHG AX,CX

        private sealed class I_91_Xchg_CX : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.CX);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,CX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x92 - XCHG AX,DX

        private sealed class I_92_Xchg_DX : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.DX);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,DX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x93 - XCHG AX,BX

        private sealed class I_93_Xchg_BX : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.BX);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,BX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x94 - XCHG AX,SP

        private sealed class I_94_Xchg_SP : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.SP);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,SP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x95 - XCHG AX,BP

        private sealed class I_95_Xchg_BP : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.BP);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,BP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x96 - XCHG AX,SI

        private sealed class I_96_Xchg_SI : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.SI);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,SI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x97 - XCHG AX,DI

        private sealed class I_97_Xchg_DI : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.Swap_Word((int) Reg.AX, (int) Reg.DI);

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"XCHG    AX,DI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x9E - SAHF

        private sealed class I_9E_SAHF : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                // SF (0x80), ZF (0x40), AF (0x10), PF (0x04), CF (0x01)
                // copied from AH and stored into FLAGS
                cpu.Flags = (cpu.Flags & ~0xD5) | (mem[(int) Reg.AX + 1] & 0xD5);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"SAHF";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x9F - LAHF

        private sealed class I_9F_LAHF : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                // SF (0x80), ZF (0x40), AF (0x10), PF (0x04), CF (0x01)
                // copied from FLAGS and stored into AH
                mem[(int) Reg.AX + 1] = (byte) (cpu.Flags & 0xFF);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"LAHF";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB0 - MOV AL,Ib

        private sealed class I_B0_Mov_AL_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.AX] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB1 - MOV CL,Ib

        private sealed class I_B1_Mov_CL_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.CX] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.CX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB2 - MOV DL,Ib

        private sealed class I_B2_Mov_DL_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.DX] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.DX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB3 - MOV BL,Ib

        private sealed class I_B3_Mov_BL_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.BX] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.BX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB4 - MOV AH,Ib

        private sealed class I_B4_Mov_AH_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.AX + 1] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintRegImm("MOV", Reg.SP /* AH */, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB5 - MOV CH,Ib

        private sealed class I_B5_Mov_CH_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.CX + 1] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintRegImm("MOV", Reg.BP /* CH */, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB6 - MOV DH,Ib

        private sealed class I_B6_Mov_DH_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.DX + 1] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintRegImm("MOV", Reg.SI /* DH */, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB7 - MOV BH,Ib

        private sealed class I_B7_Mov_BH_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.stateBytes[(int) Reg.BX + 1] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintRegImm("MOV", Reg.DI /* BH */, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB8 - MOV AX,Iw

        private sealed class I_B8_Mov_AX_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.AX    ] = (byte) imm;
                mem[(int) Reg.AX + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xB9 - MOV CX,Iw

        private sealed class I_B9_Mov_CX_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.CX    ] = (byte) imm;
                mem[(int) Reg.CX + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.CX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBA - MOV DX,Iw

        private sealed class I_BA_Mov_DX_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.DX    ] = (byte) imm;
                mem[(int) Reg.DX + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.DX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBB - MOV BX,Iw

        private sealed class I_BB_Mov_BX_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.BX    ] = (byte) imm;
                mem[(int) Reg.BX + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.BX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBC - MOV SP,Iw

        private sealed class I_BC_Mov_SP_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.SP    ] = (byte) imm;
                mem[(int) Reg.SP + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.SP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBD - MOV BP,Iw

        private sealed class I_BD_Mov_BP_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.BP    ] = (byte) imm;
                mem[(int) Reg.BP + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.BP, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBE - MOV SI,Iw

        private sealed class I_BE_Mov_SI_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.SI    ] = (byte) imm;
                mem[(int) Reg.SI + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.SI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xBF - MOV DI,Iw

        private sealed class I_BF_Mov_DI_Iw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionWord();
                mem[(int) Reg.DI    ] = (byte) imm;
                mem[(int) Reg.DI + 1] = (byte) (imm >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegImm("MOV", Reg.DI, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA0 - MOV AL,Ob

        private sealed class I_A0_Mov_AL_Ob : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ob = cpu.dataSegmentAddress + cpu.GetInstructionWord();
                mem[(int) Reg.AX] = mem[ob];
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegOfs("MOV", Reg.AX, 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA1 - MOV AX,Ow

        private sealed class I_A1_Mov_AX_Ow : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ow = cpu.dataSegmentAddress + cpu.GetInstructionWord();
                mem[(int) Reg.AX    ] = mem[ow    ];
                mem[(int) Reg.AX + 1] = mem[ow + 1];

                #if DEBUGGER
                cpu.ThrowIfWrapAroundOffset(ow - cpu.dataSegmentAddress, 2);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegOfs("MOV", Reg.AX, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA2 - MOV Ob,AL

        private sealed class I_A2_Mov_Ob_AL : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ob = cpu.dataSegmentAddress + cpu.GetInstructionWord();
                mem[ob] = mem[(int) Reg.AX];
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegOfs("MOV", Reg.AX, -1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xA3 - MOV Ow,AX

        private sealed class I_A3_Mov_Ow_AX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ow = cpu.dataSegmentAddress + cpu.GetInstructionWord();
                mem[ow    ] = mem[(int) Reg.AX    ];
                mem[ow + 1] = mem[(int) Reg.AX + 1];

                #if DEBUGGER
                cpu.ThrowIfWrapAroundOffset(ow - cpu.dataSegmentAddress, 2);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintRegOfs("MOV", Reg.AX, -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC4 - LES Gw,Mp

        private sealed class I_C4_Load_ES : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg

                mem[gw    ]           = mem[ew    ];
                mem[gw + 1]           = mem[ew + 1];
                mem[(int) Reg.ES    ] = mem[ew + 2];
                mem[(int) Reg.ES + 1] = mem[ew + 3];
                cpu.CacheSegmentRegisters();

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew, 4);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("LES", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC5 - LDS Gw,Mp

        private sealed class I_C5_Load_DS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                var gw = (modrm & 0x38) + (int) Reg.AX; // select 16-bit reg

                mem[gw    ]           = mem[ew    ];
                mem[gw + 1]           = mem[ew + 1];
                mem[(int) Reg.DS    ] = mem[ew + 2];
                mem[(int) Reg.DS + 1] = mem[ew + 3];
                cpu.CacheSegmentRegisters();

                // tell SegmentOverride () not to restore DS
                if ((cpu.segmentOverrideFlags & inSegmentOverride) != 0)
                    cpu.segmentOverrideFlags |= dsSegmentUpdated;

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew, 4);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintModrm("LDS", -2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC6 - MOV Eb,Ib

        private sealed class I_C6_Mov_EbIb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var eb = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                mem[eb] = (byte) cpu.GetInstructionByte();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintModrmImm("MOV", 1, cpu.GetInstructionByte());
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC7 - MOV Ew,Iw

        private sealed class I_C7_Mov_EwIw : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int imm;
                mem[ew    ] = (byte) (imm = cpu.GetInstructionWord());
                mem[ew + 1] = (byte) (imm >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintModrmImm("MOV", 2, cpu.GetInstructionByte());
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD7 - XLAT

        private sealed class I_D7_Xlat : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var adr = cpu.dataSegmentAddress
                        + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                        +  mem[(int) Reg.AX];
                mem[(int) Reg.AX] = mem[adr];   // AL = [BX + AL]
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "XLAT";
            #endif
        }

    }
}
