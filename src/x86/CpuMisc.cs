
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // set segment override and process the next instruction

        private void SegmentOverride (int seg)
        {
            var mem = stateBytes;

            #if DEBUGGER
            if ((segmentOverrideFlags & inSegmentOverride) != 0)
            {
                throw new System.InvalidProgramException(
                    $"Invalid segment prefix near {InstructionAddress:X5}");
            }
            #endif
            segmentOverrideFlags |= inSegmentOverride;

            var oldDataSeg = dataSegmentAddress;
            var oldStackSeg = stackSegmentAddressForModrm;
            dataSegmentAddress = stackSegmentAddressForModrm =
                                      (mem[seg] | (mem[seg + 1] << 8)) << 4;

            Step();

            // we should not restore DS or SS if it was changed by the
            // instruction we executed (e.g. 0xC5 LDS instruction)

            if ((segmentOverrideFlags & dsSegmentUpdated) == 0)
                dataSegmentAddress = oldDataSeg;

            if ((segmentOverrideFlags & ssSegmentUpdated) == 0)
                stackSegmentAddressForModrm = oldStackSeg;

            segmentOverrideFlags = 0;
        }

        // --------------------------------------------------------------------
        // 0x26 - ES prefix

        private sealed class I_26_ES_Prefix : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.SegmentOverride((int) Reg.ES);

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.SegmentOverrideForPrint(Reg.ES);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x27 - DAA

        private sealed class I_27_DAA : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int al = mem[(int) Reg.AX];

                if (cpu.adjustFlag < 0 || (al & 0x0F) > 9)
                {
                    al = (al + 6) & 0xFF;
                    cpu.adjustFlag = -1;
                }
                else
                    cpu.adjustFlag = 0;

                if (cpu.carryFlag < 0 || al > 0x9F)
                {
                    al = (al + 0x60) & 0xFF;
                    cpu.carryFlag = -1;
                }
                else
                    cpu.carryFlag = 0;

                mem[(int) Reg.AX] = (byte) al;
                cpu.signFlag = al << 24;
                cpu.zeroFlag = cpu.parityFlag = al;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "DAA";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2E - CS prefix

        private sealed class I_2E_CS_Prefix : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.SegmentOverride((int) Reg.CS);

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.SegmentOverrideForPrint(Reg.CS);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x2F - DAS

        private sealed class I_2F_DAS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int al = mem[(int) Reg.AX];

                if (cpu.adjustFlag < 0 || (al & 0x0F) > 9)
                {
                    al = (al - 6) & 0xFF;
                    cpu.adjustFlag = -1;
                }
                else
                    cpu.adjustFlag = 0;

                if (cpu.carryFlag < 0 || al > 0x9F)
                {
                    al = (al - 0x60) & 0xFF;
                    cpu.carryFlag = -1;
                }
                else
                    cpu.carryFlag = 0;

                mem[(int) Reg.AX] = (byte) al;
                cpu.signFlag = al << 24;
                cpu.zeroFlag = cpu.parityFlag = al;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "DAA";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x36 - SS prefix

        private sealed class I_36_SS_Prefix : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.SegmentOverride((int) Reg.SS);

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.SegmentOverrideForPrint(Reg.SS);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x37 - AAA

        private sealed class I_37_AAA : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ax = mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8);
                if (cpu.adjustFlag < 0 || (ax & 0x0F) > 9)
                {
                    ax = (ax + 0x106) & 0xFFFF;
                    mem[(int) Reg.AX + 1] = (byte) (ax >> 8);
                    cpu.adjustFlag = cpu.carryFlag = -1;
                }
                mem[(int) Reg.AX]     = (byte) (ax & 0x0F);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "AAA";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x3E - DS prefix

        private sealed class I_3E_DS_Prefix : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.SegmentOverride((int) Reg.DS);

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.SegmentOverrideForPrint(Reg.DS);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x98 - CBW

        private sealed class I_98_CBW : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ax;
                mem[(int) Reg.AX] = (byte) (ax = (int) (sbyte) mem[(int) Reg.AX]);
                mem[(int) Reg.AX + 1] = (byte) (ax >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CBW";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x99 - CWD

        private sealed class I_99_CWD : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ax = mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8);
                mem[(int) Reg.DX] = mem[(int) Reg.DX + 1] =
                                                (byte) ((ax << 16) >> 31);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CWD";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xD4 - AAM

        private sealed class I_D4_AAM : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var imm = cpu.GetInstructionByte();
                #if DEBUGGER
                if (imm == 0)
                {
                    throw new System.InvalidProgramException(
                        $"Invalid AAM near {cpu.InstructionAddress:X5}");
                }
                #endif
                int acc = mem[(int) Reg.AX];
                mem[(int) Reg.AX + 1] = (byte) (acc / imm);     // AH
                mem[(int) Reg.AX    ] = (byte) (acc %= imm);    // AL
                cpu.signFlag = acc << 24;
                cpu.zeroFlag = cpu.parityFlag = acc;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "AAM";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF5 - CMC

        private sealed class I_F5_CMC : Instruction
        {
            public override void Process (Cpu cpu) => cpu.carryFlag ^= -1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CMC";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF8 - CLC

        private sealed class I_F8_CLC : Instruction
        {
            public override void Process (Cpu cpu) => cpu.carryFlag = 0;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CLC";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF9 - STC

        private sealed class I_F9_STC : Instruction
        {
            public override void Process (Cpu cpu) => cpu.carryFlag = -1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "STC";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFA - CLI

        private sealed class I_FA_CLI : Instruction
        {
            public override void Process (Cpu cpu) => cpu.interruptFlag = 0;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CLI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFB - STI

        private sealed class I_FB_STI : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.interruptFlag = -1;
                cpu.ServiceInterrupt();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "STI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFC - CLD

        private sealed class I_FC_CLD : Instruction
        {
            public override void Process (Cpu cpu) => cpu.directionFlag = 0;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CLD";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFD - STD

        private sealed class I_FD_STD : Instruction
        {
            public override void Process (Cpu cpu) => cpu.directionFlag = -1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "STD";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xFF - INC/DEC/CALL/JMP/PUSH

        private sealed class I_FF_TwoByte : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif

                int res, cs, ip;
                switch ((modrm >> 3) & 7)
                {
                    case 0:     // INC
                        res = cpu.Inc_Word(mem[ew] | (mem[ew + 1] << 8));
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;

                    case 1:     // DEC
                        res = cpu.Dec_Word(mem[ew] | (mem[ew + 1] << 8));
                        mem[ew    ] = (byte) res;
                        mem[ew + 1] = (byte) (res >> 8);
                        return;

                    case 2:     // CALL NEAR
                        cpu.Push_Word(   cpu.InstructionAddress   // IP
                                       - cpu.codeSegmentAddress);
                        cpu.InstructionAddress = cpu.codeSegmentAddress
                                               + (mem[ew] | (mem[ew + 1] << 8));
                        return;

                    case 3:     // CALL FAR
                        #if DEBUGGER
                        cpu.ThrowIfWrapAroundModrm(ew, 4);
                        #endif
                        cpu.Push_Word(
                                (cs = cpu.codeSegmentAddress) >> 4); // CS
                        cpu.Push_Word(cpu.InstructionAddress - cs);  // IP
                        ip = mem[ew] | (mem[ew + 1] << 8);
                        cs = (mem[(int) Reg.CS] = mem[ew + 2])
                           | ((mem[(int) Reg.CS + 1] = mem[ew + 3]) << 8);
                        cpu.InstructionAddress =
                                (cpu.codeSegmentAddress = cs << 4) + ip;
                        return;

                    case 4:     // JMP NEAR
                        cpu.InstructionAddress = cpu.codeSegmentAddress
                                               + (mem[ew] | (mem[ew + 1] << 8));
                        return;

                    case 5:     // JMP FAR
                        #if DEBUGGER
                        cpu.ThrowIfWrapAroundModrm(ew, 4);
                        #endif
                        ip = mem[ew] | (mem[ew + 1] << 8);
                        cs = (mem[(int) Reg.CS] = mem[ew + 2])
                           | ((mem[(int) Reg.CS + 1] = mem[ew + 3]) << 8);
                        cpu.InstructionAddress =
                                (cpu.codeSegmentAddress = cs << 4) + ip;
                        return;

                    case 6:     // PUSH Ew
                        cpu.Push_Word(mem[ew] | (mem[ew + 1] << 8));
                        return;

                    default:
                        I_XX_Invalid.ThrowInvalid(cpu, 0xFF, modrm);
                        return;
                }
            }

            #if DEBUGGER
            public override string Print (Cpu cpu)
            {
                var modrm = cpu.GetInstructionByte();
                return (modrm & 0x38) switch
                {
                    0x00 => cpu.PrintModrmNoReg("INC", 2, modrm),
                    0x08 => cpu.PrintModrmNoReg("DEC", 2, modrm),
                    0x10 => cpu.PrintModrmNoReg("CALL", 2, modrm),
                    0x18 => cpu.PrintModrmNoReg("CALL   D", 2, modrm),
                    0x20 => cpu.PrintModrmNoReg("JMP", 2, modrm),
                    0x28 => cpu.PrintModrmNoReg("JMP    D", 2, modrm),
                    0x30 => cpu.PrintModrmNoReg("PUSH", 2, modrm),
                    _    => I_FE_TwoByte.InvalidInstructionAndGoBack1(cpu),
                };
            }
            #endif
        }

    }
}
