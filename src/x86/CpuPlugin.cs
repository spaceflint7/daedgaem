
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // RegisterPlugin

        public void RegisterPlugin (IPlugin plugin, int[] interrupts, int[] ports)
        {
            var mem = stateBytes;

            foreach (var x in interrupts ?? new int[0])
            {
                if (this.interrupts[x] is null)
                {
                    this.interrupts[x] = plugin;

                    // set up interrupt table vector XX at address 0000:(XX*4)
                    // to point to interrupt handler address at FF00:00XX,
                    // and the interrupt handler itself as a HLT instruction
                    mem[x * 4    ]   = (byte) x;
                    mem[x * 4 + 1]   = 0x00;
                    mem[x * 4 + 2]   = 0xF0;
                    mem[x * 4 + 3]   = 0xFF;
                    mem[0xFFF00 + x] = 0xF4; // HLT
                }
                else    // interupt is already registered
                    throw new System.ArgumentException();
            }

            foreach (var x in ports ?? new int[0])
            {
                if (this.ports[x] is null)
                    this.ports[x] = plugin;
                else    // port is already registered
                    throw new System.ArgumentException();
            }
        }

        // --------------------------------------------------------------------
        // RegisterTimer

        public void RegisterTimer (PluginTimer plugin)
        {
            timerCallback = plugin;
        }

        // --------------------------------------------------------------------
        // ReadPort

        private int ReadPort (int size, int which)
        {
            IPlugin plugin;
            int error = 0;
            if (size == 1 && which <= MaxPort)
            {
                if ((plugin = ports[which]) is not null)
                {
                    error = plugin.ReadPort (which);
                    if (error >= 0)
                        return error;   // value from port
                }
            }
            #if DEBUGGER
            throw new System.InvalidProgramException(
                            $"Invalid {size}-byte port IN {which:X4}"
                          + $" (error {error}, hex {-error:X4})"
                          + $" near {InstructionAddress:X5}");
            #else
            return 0;
            #endif
        }

        // --------------------------------------------------------------------
        // WritePort

        private void WritePort (int size, int value, int which)
        {
            IPlugin plugin;
            int error = 0;
            if (size == 1 && which <= MaxPort)
            {
                if ((plugin = ports[which]) is not null)
                {
                    error = plugin.WritePort (which, value);
                    if (error >= 0)
                        return;
                }
            }
            #if DEBUGGER
            throw new System.InvalidProgramException(
                            $"Invalid {size}-byte port OUT {which:X4}"
                          + $" (error {error}, hex {-error:X4})"
                          + $" near {InstructionAddress:X5}");
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE4 - IN AL, Ib

        private sealed class I_E4_In_AL_Ib : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte) cpu.ReadPort(1, cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => $"IN      AL,{cpu.GetInstructionByte():X2}";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xE6 - OUT Ib, AL

        private sealed class I_E6_Out_Ib_AL : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.WritePort(1, mem[(int) Reg.AX], cpu.GetInstructionByte());
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                $"OUT     {cpu.GetInstructionByte():X2},AL";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xEC - IN AL, DX

        private sealed class I_EC_In_AL_DX : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                mem[(int) Reg.AX] = (byte) cpu.ReadPort(1,
                            mem[(int) Reg.DX] | (mem[(int) Reg.DX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "IN      AL,DX";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xEE - OUT DX, AL

        private sealed class I_EE_Out_DX_AL : Instruction
        {
            public override void Process (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                cpu.WritePort(1, mem[(int) Reg.AX],
                              mem[(int) Reg.DX] | (mem[(int) Reg.DX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "OUT     DX,AL";
            #endif
        }

        // --------------------------------------------------------------------
        // 0xF4 - HLT

        private sealed class I_F4_Halt : Instruction
        {

            public override void Process (Cpu cpu)
            {
                // a HLT instruction in range FFF0:0000 to FFF0:00FF
                // is an escape mechanism to call a registered plugin

                int addr;
                if ((addr = cpu.InstructionAddress) > 0xFFF00)
                {
                    // note that InstructionAddress is already one byte
                    // past the HLT instruction, therefore we subtract one
                    int which;
                    if ((which = addr - 1 - 0xFFF00) < 256)
                    {
                        IPlugin plugin;
                        if ((plugin = cpu.interrupts[which]) is not null)
                        {
                            int error = plugin.Interrupt(which);

                            if (error == YieldUntilInterrupt)
                            {
                                // if the plugin wishes to yield until interrupt,
                                // rewind the instruction address to re-execute the
                                // same special HLT instruction, then continue below
                                // as if this is a normal HLT instruction
                                cpu.InstructionAddress = addr - 1;
                            }
                            else if (error < 0)
                            {
                                throw new System.InvalidProgramException(
                                                $"Invalid interrupt {which:X2}H"
                                              + $" (error {error}, hex {-error:X4})"
                                              + $" near {cpu.InstructionAddress:X5}");
                            }
                            else
                            {
                                // upon return from the plugin, simulate the effect
                                // of RETF 0002 rather than IRET.  this discards the
                                // flags on the stack in favor of the current flags,
                                // excepting the trap (TF) and interrupts (IF) flags

                                var mem = cpu.stateBytes;
                                int ip = cpu.Pop_Word();
                                int cs = cpu.Pop_Word();

                                int flags = cpu.Pop_Word();
                                cpu.trapFlag = flags << (31 - 8);
                                cpu.interruptFlag = flags << (31 - 9);

                                mem[(int) Reg.CS    ] = (byte) cs;
                                mem[(int) Reg.CS + 1] = (byte) (cs >> 8);

                                cpu.InstructionAddress =
                                        (cpu.codeSegmentAddress = cs << 4) + ip;

                                cpu.ServiceInterrupt();

                                return;
                            }
                        }
                    }
                }

                // halt processing until an interrupt occurs.  we get here
                // for a normal HLT instruction, or a special one that called
                // a plugin, but the plugin returned the special return code

                cpu.WaitForSignal();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "HLT";
            #endif
        }

        // --------------------------------------------------------------------
        // interface for plugin component

        public interface IPlugin
        {
            int Interrupt (int which);

            int ReadPort (int which);

            int WritePort (int which, int value);
        }

        public abstract class PluginTimer
        {
            public abstract void Tick (int deltaMilliseconds);
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] private PluginTimer timerCallback = null;
        [java.attr.RetainType] private IPlugin[] interrupts = new IPlugin[256];
        [java.attr.RetainType] private IPlugin[] ports = new IPlugin[MaxPort + 1];
        private const int MaxPort = 0x3DA;

        public const int YieldUntilInterrupt = int.MinValue;
    }
}
