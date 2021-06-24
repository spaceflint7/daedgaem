
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // run cpu

        public long Run (int instsPerSecond)
        {
            if (interruptEvent < 0)
                interruptEvent = 1;     // cancel stopped state

            var instTable = Cpu.instTable;
            long instCount = 0;

            long ticksPerMillisecond =
                            System.Diagnostics.Stopwatch.Frequency / 1000;
            long lastTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            long deltaTicks;

            var instsPerMillisecond = instsPerSecond / 1000;
            if (instsPerMillisecond < 5)
                instsPerMillisecond = 5;

            int spinCount = lastSpinCount;
            if (spinCount < 1)
                spinCount = 1;
            int execCount = lastExecCount;
            if (execCount < 5)
                execCount = instsPerMillisecond;

            do
            {
                // check how many instructions were processed during the
                // last millisecond, and adjust the spin and exec counts
                // to reach the requested number of instructions per second,
                // with an inner loop that runs for roughly 1 ms each time

                var currTicks = System.Diagnostics.Stopwatch.GetTimestamp();
                if ((deltaTicks = currTicks - lastTicks) >= ticksPerMillisecond)
                {
                    lastTicks = currTicks;

                    // notify the timer callback that about 1 ms has passed
                    timerCallback.Tick((int) (deltaTicks / ticksPerMillisecond));

                    if (spinCount > 0)
                        spinCount--;
                    else if (execCount > 5)
                        execCount--;
                }
                else if (execCount < instsPerMillisecond)
                    execCount++;
                else
                    spinCount++;

                // process instructions until receiving a signal

                for (int iExec = 0; iExec < execCount; iExec++)
                {
                    #if DEBUGGER
                    // assume modrm will decode using DS; modrm decoders
                    // that use SS will overwrite this value.
                    // see also ThrowIfWrapAroundModrm()
                    modrmSegmentAddress = dataSegmentAddress;
                    #endif
                    instTable[GetInstructionByte()].Process(this);

                    // delay to reach target instructions per second
                    for (int iSpin = 0; iSpin < spinCount; iSpin++)
                        System.Threading.Thread.Yield();
                }

                instCount += execCount;
            }
            while (ServiceInterrupt());

            // save the spin and exec counts for the next run
            lastSpinCount = spinCount;
            lastExecCount = execCount;

            return instCount;
        }

        // --------------------------------------------------------------------
        // service interrupt request

        private bool ServiceInterrupt ()
        {
            if (interruptEvent < 0)                     // if cpu stopped
                return false;

            interruptEvent = 0;

            if (interruptFlag >= 0)                     // if interrupts disabled
                return true;

            // get the mask of interrupts that need servicing.
            // ignore the interrupt if we are waiting for an EOI command
            int mask = System.Threading.Interlocked.Add(
                                        ref interruptMask, 0);
            if ((mask & 0x40000000) != 0)
                return true;

            for (int irqCounter = 1; irqCounter <= 8; irqCounter++)
            {
                // avoid irq starvation by always starting the count
                // from the last irq handled, plus one
                int irq = (lastIrqHandled + irqCounter) & 7;

                int irqMask = 1 << irq;
                if ((mask & irqMask) != 0)
                {
                    // clear a low bit that represents the pending IRQ,
                    // and set a high bit that indicates not to service
                    // any more interrupts until an EOI command is sent
                    do
                    {
                        mask = System.Threading.Interlocked.CompareExchange(
                                        ref interruptMask,
                                        (mask & ~irqMask) | 0x40000000,
                                        mask);
                    }
                    while ((mask & (irqMask | 0x40000000)) != 0x40000000);

                    lastIrqHandled = irq;
                    InvokeInterrupt(irq + 8);
                    break;
                }
            }

            return true;
        }

        // --------------------------------------------------------------------
        // stop cpu

        public void Signal_STOP ()
        {
            interruptEvent = -1;
        }

        // --------------------------------------------------------------------
        // stop cpu

        public void Signal_IRQ (int irq)
        {
            int mask = System.Threading.Interlocked.Add(
                                        ref interruptMask, 0);

            // discard the signal if the irq level is inhibited.
            // the inhibit bits are bits 16..23 of the interrupt mask.
            int irqMask = 1 << irq;
            if ((mask & (irqMask << 16)) != 0)
            {
                #if DEBUGGER
                System.Console.WriteLine($"DROPPING IRQ {irq}: INHIBIT MASK = {mask:X8}");
                #endif
                return;
            }

            do
            {
                mask = System.Threading.Interlocked.CompareExchange(
                                        ref interruptMask,
                                        mask | irqMask,
                                        mask);
            }
            while ((mask & irqMask) == 0);

            interruptEvent |= 1;
        }

        // --------------------------------------------------------------------
        // signal end of interrupt

        public void Signal_EOI ()
        {
            // handle EOI (end of interrupt) to resume interrupt servicing.
            // WritePort() calls this when command 20H is sent to port 20H.
            // may also be invoked by plugins.

            #if DEBUGGER
            int mask = System.Threading.Interlocked.Add(
                                        ref interruptMask, 0);
            if ((mask & 0x40000000) == 0)
            {
                throw new System.InvalidProgramException(
                    $"Unexpected EOI near {InstructionAddress:X5}");
            }
            #else
            int mask = 0;
            #endif

            do
            {
                mask = System.Threading.Interlocked.CompareExchange(
                                        ref interruptMask,
                                        mask & ~0x40000000,
                                        mask);
            }
            while ((mask & 0x40000000) != 0);

            interruptEvent |= 1;
        }

        // --------------------------------------------------------------------
        // return or update the mask of inhibited interrupts

        private int InhibitInterrupts (int newMask)
        {
            int mask = System.Threading.Interlocked.Add(
                                        ref interruptMask, 0);
            if (newMask != -1)
            {
                newMask = (newMask & 0xFF) << 16;
                do
                {
                    newMask = (mask & ~0x00FF0000) | (newMask & 0xFF0000);
                    mask = System.Threading.Interlocked.CompareExchange(
                                            ref interruptMask,
                                            newMask,
                                            mask);
                }
                while (mask != newMask);
            }

            return (byte) (mask >> 16);
        }

        // --------------------------------------------------------------------
        // halt processing until an interrupt occurs

        public void WaitForSignal ()
        {
            while (interruptEvent == 0)
            {
                System.Threading.Thread.Yield();
            }
        }

        // --------------------------------------------------------------------
        // step cpu (no interrupts)

        public void Step ()
        {
            // process one instruction explicitly without servicing interrupts
            #if DEBUGGER
            modrmSegmentAddress = dataSegmentAddress; // see Run()
            #endif
            instTable[GetInstructionByte()].Process(this);
        }

        // --------------------------------------------------------------------
        // service interrupts, then step cpu

        public void StepInterruptible ()
        {
            Step();
            if (interruptEvent != 0)
                ServiceInterrupt();
        }

        // --------------------------------------------------------------------
        // interrupt controller for PIC 8259

        private partial class InterruptController : Cpu.IPlugin
        {
            [java.attr.RetainType] private Cpu self;

            public InterruptController (Cpu _self) => self = _self;

            // --------------------------------------------------------------------
            // ReadPort

            public int ReadPort (int which)
            {
                if (which == 0x21)
                    return (byte) self.InhibitInterrupts(-1);
                return -1;
            }

            // --------------------------------------------------------------------
            // WritePort

            public int WritePort (int which, int value)
            {
                if (which == 0x20)
                {
                    // handle EOI (end of interrupt, command 20H to port 20H)
                    if (value == 0x20)
                    {
                        self.Signal_EOI();
                        return 0;
                    }
                }
                else if (which == 0x21)
                {
                    self.InhibitInterrupts(value);
                    return 0;
                }
                // unexpected value
                return -value;
            }

            // --------------------------------------------------------------------
            // Interrupt

            public int Interrupt (int which)
            {
                throw new System.InvalidProgramException(
                    $"Exception {which:X2} near {self.InstructionAddress:X5}");
            }
        }

    }
}
