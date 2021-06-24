
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // push word onto the stack

        private void Push_Word (int v)
        {
            var mem = stateBytes;
            int sp;
            #if DEBUGGER
            // wrap-around occurs only if SP == 0001 when PUSH is executed
            sp = mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8);
            if (sp == 1)
            {
                throw new System.InvalidProgramException(
                    $"Wrap-around in stack near {InstructionAddress:X5}");
            }
            var a = stackSegmentAddressForPushPop + (sp = 0xFFFF & (sp - 2));
            #else
            // no debugger, subtract SP without checking for wrap-around
            var a = stackSegmentAddressForPushPop
                  + (sp = 0xFFFF & ((    mem[(int) Reg.SP]
                                      | (mem[(int) Reg.SP + 1] << 8)) - 2));
            #endif

            mem[a    ]            = (byte) v;
            mem[a + 1]            = (byte) (v >> 8);
            mem[(int) Reg.SP    ] = (byte) sp;
            mem[(int) Reg.SP + 1] = (byte) (sp >> 8);
        }

        // --------------------------------------------------------------------
        // pop a word off the stack

        private int Pop_Word ()
        {
            var mem = stateBytes;
            int sp;
            var a = stackSegmentAddressForPushPop
                  + (sp = mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8));
            #if DEBUGGER
            // wrap-around occurs only if SP == FFFF when POP is executed
            if (sp >= 0xFFFF)
            {
                throw new System.InvalidProgramException(
                    $"Wrap-around in stack near {InstructionAddress:X5}");
            }
            #endif
            int v = mem[a] | (mem[a + 1] << 8);
            mem[(int) Reg.SP    ] = (byte) (sp += 2);
            mem[(int) Reg.SP + 1] = (byte) (sp >> 8);
            return v;
        }

        // --------------------------------------------------------------------
        // pop a word off the stack and advance the stack pointer

        private int Pop_Word_And_Advance (int extra)
        {
            var mem = stateBytes;
            int sp;
            var a = stackSegmentAddressForPushPop
                  + (sp = mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8));
            int v = mem[a] | (mem[a + 1] << 8);
            mem[(int) Reg.SP    ] = (byte) (sp += 2 + extra);
            mem[(int) Reg.SP + 1] = (byte) (sp >> 8);
            #if DEBUGGER
            // wrap-around check, see also Pop_Word ()
            if (sp > 0xFFFF)
            {
                throw new System.InvalidProgramException(
                    $"Wrap-around in stack near {InstructionAddress:X5}");
            }
            #endif
            return v;
        }

        // --------------------------------------------------------------------
        // 0x06 - PUSH ES

        private sealed class I_06_Push_ES : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.ES] | (mem[(int) Reg.ES + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    ES";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x07 - POP ES

        private sealed class I_07_Pop_ES : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int es;
                mem[(int) Reg.ES    ] = (byte) (es = cpu.Pop_Word());
                mem[(int) Reg.ES + 1] = (byte) (es >> 8);
                cpu.CacheSegmentRegisters();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     ES";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x0E - PUSH CS

        private sealed class I_0E_Push_CS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.CS] | (mem[(int) Reg.CS + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    CS";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x16 - PUSH SS

        private sealed class I_16_Push_SS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.SS] | (mem[(int) Reg.SS + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    SS";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x17 - POP SS

        private sealed class I_17_Pop_SS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ss;
                mem[(int) Reg.SS    ] = (byte) (ss = cpu.Pop_Word());
                mem[(int) Reg.SS + 1] = (byte) (ss >> 8);
                cpu.CacheSegmentRegisters();

                // tell SegmentOverride () not to restore SS
                if ((cpu.segmentOverrideFlags & inSegmentOverride) != 0)
                    cpu.segmentOverrideFlags |= ssSegmentUpdated;

                // updating SS disables interrupts for one instruction,
                // to permit the following instruction to update SP
                cpu.Step();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     SS";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1E - PUSH DS

        private sealed class I_1E_Push_DS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.DS] | (mem[(int) Reg.DS + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    DS";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x1F - POP DS

        private sealed class I_1F_Pop_DS : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int ds;
                mem[(int) Reg.DS    ] = (byte) (ds = cpu.Pop_Word());
                mem[(int) Reg.DS + 1] = (byte) (ds >> 8);
                cpu.CacheSegmentRegisters();

                // tell SegmentOverride () not to restore DS
                if ((cpu.segmentOverrideFlags & inSegmentOverride) != 0)
                    cpu.segmentOverrideFlags |= dsSegmentUpdated;
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     DS";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x50 - PUSH AX

        private sealed class I_50_Push_AX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.AX] | (mem[(int) Reg.AX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    AX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x51 - PUSH CX

        private sealed class I_51_Push_CX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.CX] | (mem[(int) Reg.CX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    CX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x52 - PUSH DX

        private sealed class I_52_Push_DX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.DX] | (mem[(int) Reg.DX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    DX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x53 - PUSH BX

        private sealed class I_53_Push_BX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    BX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x54 - PUSH SP

        private sealed class I_54_Push_SP : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                // the 8086 pushes the value of SP after it is decremented
                cpu.Push_Word(
                    (mem[(int) Reg.SP] | (mem[(int) Reg.SP + 1] << 8)) - 2);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    SP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x55 - PUSH BP

        private sealed class I_55_Push_BP : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    BP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x56 - PUSH SI

        private sealed class I_56_Push_SI : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    SI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x57 - PUSH DI

        private sealed class I_57_Push_DI : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.Push_Word(mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSH    DI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x58 - POP AX

        private sealed class I_58_Pop_AX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.AX    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.AX + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     AX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x59 - POP CX

        private sealed class I_59_Pop_CX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.CX    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.CX + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     CX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5A - POP DX

        private sealed class I_5A_Pop_DX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.DX    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.DX + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     DX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5B - POP BX

        private sealed class I_5B_Pop_BX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.BX    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.BX + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     BX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5C - POP SP

        private sealed class I_5C_Pop_SP : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.SP    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.SP + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     SP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5D - POP BP

        private sealed class I_5D_Pop_BP : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.BP    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.BP + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     BP";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5E - POP SI

        private sealed class I_5E_Pop_SI : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.SI    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.SI + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     SI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x5F - POP DI

        private sealed class I_5F_Pop_DI : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                int v;
                mem[(int) Reg.DI    ] = (byte) (v = cpu.Pop_Word());
                mem[(int) Reg.DI + 1] = (byte) (v >> 8);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POP     DI";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x8F - POP Ew

        private sealed class I_8F_Pop_Ew : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                var modrm = cpu.GetInstructionByte();
                var ew = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);
                int v;
                mem[ew    ] = (byte) (v = cpu.Pop_Word());
                mem[ew + 1] = (byte) (v >> 8);

                #if DEBUGGER
                cpu.ThrowIfWrapAroundModrm(ew);
                #endif
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintModrmNoReg("POP", 2, cpu.GetInstructionByte());
            #endif
        }

        // --------------------------------------------------------------------
        // 0x9C - PUSHF

        private sealed class I_9C_PushFlags : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.Push_Word(cpu.Flags);
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "PUSHF";
            #endif
        }

        // --------------------------------------------------------------------
        // 0x9D - POPF

        private sealed class I_9D_PopFlags : Instruction
        {
            public override void Process (Cpu cpu)
            {
                cpu.Flags = cpu.Pop_Word();
                cpu.ServiceInterrupt();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "POPF";
            #endif
        }

    }
}
