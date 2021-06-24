
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // invoke interrupt handler via interrupt vector table

        private void InvokeInterrupt (int which)
        {
            var mem = stateBytes;
            var addr = which << 2;

            int cs;
            Push_Word(Flags);
            Push_Word((cs = codeSegmentAddress) >> 4); // CS
            Push_Word(InstructionAddress - cs);        // IP

            trapFlag = interruptFlag = 0;

            var ip = mem[addr] | (mem[addr + 1] << 8);
            byte csLo, csHi;

            mem[(int) Reg.CS    ] = csLo = mem[addr + 2];
            mem[(int) Reg.CS + 1] = csHi = mem[addr + 3];

            InstructionAddress = ip +
                    (codeSegmentAddress = (csLo << 4) | (csHi << 12));
        }

        // --------------------------------------------------------------------
        // 0x70 - JO  (OF=1)

        private sealed class I_70_JumpO : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.overflowFlag < 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JO", cpu.overflowFlag < 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x71 - JNO (OF=0)

        private sealed class I_71_JumpNO : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.overflowFlag >= 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JNO", cpu.overflowFlag >= 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x72 - JC  (CF=1) (also JB, JNAE)

        private sealed class I_72_JumpC : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.carryFlag < 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JC", cpu.carryFlag < 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x73 - JNC (CF=0) (also JAE, JNB)

        private sealed class I_73_JumpNC : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.carryFlag >= 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JNC", cpu.carryFlag >= 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x74 - JZ  (ZF=1) (also JE)

        private sealed class I_74_JumpZ : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.zeroFlag == 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JZ", cpu.zeroFlag == 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x75 - JNZ (ZF=0) (also JNE)

        private sealed class I_75_JumpNZ : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.zeroFlag != 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JNZ", cpu.zeroFlag != 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x76 - JBE (CF=1 or ZF=1) (also JNA)

        private sealed class I_76_JumpBE : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.carryFlag < 0 || cpu.zeroFlag == 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JBE", cpu.carryFlag < 0 || cpu.zeroFlag == 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x77 - JA  (CF=0 and ZF=0) (also JNBE)

        private sealed class I_77_JumpA : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.carryFlag >= 0 && cpu.zeroFlag != 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JA", cpu.carryFlag >= 0 && cpu.zeroFlag != 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x78 - JS  (SF=1)

        private sealed class I_78_JumpS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.signFlag < 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JS", cpu.signFlag < 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x79 - JNS (SF=0)

        private sealed class I_79_JumpNS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.signFlag >= 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JNS", cpu.signFlag >= 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7A - JPE (PF=1) (also JP)

        private sealed class I_7A_JumpPE : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (parityTable[cpu.parityFlag & 0xFF] != 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JPE", parityTable[cpu.parityFlag & 0xFF] != 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7B - JPO (PF=0) (also JNP)

        private sealed class I_7B_JumpPO : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (parityTable[cpu.parityFlag & 0xFF] == 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JPO", parityTable[cpu.parityFlag & 0xFF] == 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7C - JL  (SF!=OF) (also JNGE)

        private sealed class I_7C_JumpL : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if ((cpu.signFlag ^ cpu.overflowFlag) < 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JL", (cpu.signFlag ^ cpu.overflowFlag) < 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7D - JGE (SF=OF)  (also JNL)

        private sealed class I_7D_JumpGE : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if ((cpu.signFlag ^ cpu.overflowFlag) >= 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JGE", (cpu.signFlag ^ cpu.overflowFlag) >= 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7E - JLE (ZF=1 or SF!=OF) (also JNG)

        private sealed class I_7E_JumpLE : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.zeroFlag == 0 || (cpu.signFlag ^ cpu.overflowFlag) < 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JLE", cpu.zeroFlag == 0 || (cpu.signFlag ^ cpu.overflowFlag) < 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x7F - JG  (ZF=0 and SF=OF) (also JNLE)

        private sealed class I_7F_JumpG : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionByte();
                if (cpu.zeroFlag != 0 && (cpu.signFlag ^ cpu.overflowFlag) >= 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JGE", cpu.zeroFlag != 0 && (cpu.signFlag ^ cpu.overflowFlag) >= 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0x9A - CALL Ap

        private sealed class I_9A_Call_Ap : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ip = cpu.GetInstructionWord();
                var cs = cpu.GetInstructionWord();

                int cs0;
                cpu.Push_Word((cs0 = cpu.codeSegmentAddress) >> 4); // CS
                cpu.Push_Word(cpu.InstructionAddress - cs0);        // IP

                mem[(int) Reg.CS    ] = (byte) cs;
                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                cpu.InstructionAddress =
                        (cpu.codeSegmentAddress = cs << 4) + ip;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintJump("CALL", null, 8);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC2 - RET Iw

        private sealed class I_C2_Ret_Iw : Instruction
        {

            public override void Process (Cpu cpu)
            {
                cpu.InstructionAddress = cpu.codeSegmentAddress
                                       + cpu.Pop_Word_And_Advance(
                                                cpu.GetInstructionWord());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                $"RET     {cpu.GetInstructionWord():X4}";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xC3 - RET

        private sealed class I_C3_Ret : Instruction
        {

            public override void Process (Cpu cpu)
            {
                cpu.InstructionAddress = cpu.codeSegmentAddress
                                       + cpu.Pop_Word();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "RET";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xCA - RETF Iw

        private sealed class I_CA_Ret_Far_Iw : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ip = cpu.Pop_Word();
                int cs = cpu.Pop_Word_And_Advance(cpu.GetInstructionWord());

                mem[(int) Reg.CS    ] = (byte) cs;
                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                cpu.InstructionAddress =
                        (cpu.codeSegmentAddress = cs << 4) + ip;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                $"RETF    {cpu.GetInstructionWord():X4}";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xCB - RETF

        private sealed class I_CB_Ret_Far : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ip = cpu.Pop_Word();
                int cs = cpu.Pop_Word();

                mem[(int) Reg.CS]     = (byte) cs;
                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                cpu.InstructionAddress =
                        (cpu.codeSegmentAddress = cs << 4) + ip;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "RETF";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xCD - INT nn

        private sealed class I_CD_Interrupt : Instruction
        {

            public override void Process (Cpu cpu) =>
                cpu.InvokeInterrupt(cpu.GetInstructionByte());

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintImm("INT", 1);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xCF - IRET

        private sealed class I_CF_IntRet : Instruction
        {

            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ip = cpu.Pop_Word();
                int cs = cpu.Pop_Word();
                cpu.Flags = cpu.Pop_Word();

                mem[(int) Reg.CS]     = (byte) cs;
                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                cpu.InstructionAddress =
                        (cpu.codeSegmentAddress = cs << 4) + ip;

                cpu.ServiceInterrupt();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "IRET";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE0 - LOOPNZ Jb

        private sealed class I_E0_LoopNZ_Jb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ofs = cpu.GetInstructionByte();
                int cx = mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8);
                mem[(int) Reg.CX    ] = (byte) (--cx);
                mem[(int) Reg.CX + 1] = (byte) (cx >> 8);
                if (cx != 0 && cpu.zeroFlag != 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("LOOPNZ", (1 != (    cpu.stateBytes[(int) Reg.CX]
                                                | (cpu.stateBytes[(int) Reg.CX + 1] << 8)))
                                           && cpu.zeroFlag != 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE1 - LOOPZ Jb

        private sealed class I_E1_LoopZ_Jb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ofs = cpu.GetInstructionByte();
                int cx = mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8);
                mem[(int) Reg.CX    ] = (byte) (--cx);
                mem[(int) Reg.CX + 1] = (byte) (cx >> 8);
                if (cx != 0 && cpu.zeroFlag == 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("LOOPZ", (1 != (    cpu.stateBytes[(int) Reg.CX]
                                               | (cpu.stateBytes[(int) Reg.CX + 1] << 8)))
                                          && cpu.zeroFlag == 0);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE2 - LOOP Jb

        private sealed class I_E2_Loop_Jb : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ofs = cpu.GetInstructionByte();
                int cx = mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8);
                mem[(int) Reg.CX    ] = (byte) (--cx);
                mem[(int) Reg.CX + 1] = (byte) (cx >> 8);
                if (cx != 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("LOOP", (1 != (    cpu.stateBytes[(int) Reg.CX]
                                              | (cpu.stateBytes[(int) Reg.CX + 1] << 8))));
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE3 - JCXZ Jb

        private sealed class I_E3_JumpCXZ : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ofs = cpu.GetInstructionByte();
                if ((mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8)) == 0)
                    cpu.InstructionBranch((sbyte) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintJump("JCXZ", (0 == (    cpu.stateBytes[(int) Reg.CX]
                                              | (cpu.stateBytes[(int) Reg.CX + 1] << 8))));
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE8 - CALL Jv

        private sealed class I_E8_Call_Jv : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var ofs = cpu.GetInstructionWord();
                cpu.Push_Word(cpu.InstructionAddress - cpu.codeSegmentAddress);
                cpu.InstructionBranch((short) ofs);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintJump("CALL", null, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE9 - JMP Jv

        private sealed class I_E9_Jump_Jv : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.InstructionBranch((short) cpu.GetInstructionWord());

                if (cpu.InstructionAddress == cpu.HitPointAddress)
                    cpu.HitPointAction();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintJump("JMP", null, 2);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xEA - JMP Ap

        private sealed class I_EA_Jump_Ap : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var ip = cpu.GetInstructionWord();
                var cs = cpu.GetInstructionWord();

                mem[(int) Reg.CS    ] = (byte) cs;
                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                cpu.InstructionAddress =
                        (cpu.codeSegmentAddress = cs << 4) + ip;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintJump("JMP", null, 8);
            #endif
        }

        // --------------------------------------------------------------------
        // 0xEB - JMP Jb

        private sealed class I_EB_Jump_Jb : Instruction
        {
            public override void Process (Cpu cpu) =>
                cpu.InstructionBranch((sbyte) cpu.GetInstructionByte());

            #if DEBUGGER
            public override string Print (Cpu cpu) => cpu.PrintJump("JMP", null);
            #endif
        }

    }
}
