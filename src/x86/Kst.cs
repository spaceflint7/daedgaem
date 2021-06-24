
using ArrayList = System.Collections.ArrayList;

namespace com.spaceflint.x86
{
    public partial class Kst : Cpu.IPlugin
    {

        // --------------------------------------------------------------------
        // constructor

        public Kst (Machine _machine)
        {
            Machine = _machine;

            // register BIOS interrupts 11H, 13H, 16H and 1AH
            var kstInterrupts = new int[] { 0x11, 0x15, 0x16, 0x1A };
            var kstPorts = new int[] { 0x40, 0x42, 0x43, 0x60, 0x61, 0x201 };
            Machine.Cpu.RegisterPlugin(this, kstInterrupts, kstPorts);

            CreateTimerInterrupt();
            CreateKeyboardInterrupt();

            // register the timer callback
            timerPlugin = new TimerPlugin(_machine);
            Machine.Cpu.RegisterTimer(timerPlugin);

            // register the input callback
            inputClient = new InputClient(_machine);
            Machine.Shell.Input.Register(inputClient);

            // set the equipment flags at 0040:0010 (see also INT 11h below)
            // - turn on the game adapter bit (0x1000)
            Machine.Cpu.SetWord(0x410,
                                Machine.Cpu.GetWord(0x410) | 0x1000);
        }

        // --------------------------------------------------------------------
        // CreateTimerInterrupt

        private void CreateTimerInterrupt ()
        {
            var cpu = Machine.Cpu;

            // F000:FEA5 is address of INT 08h in XT BIOS.
            // note that the IRET serves a second purpose as INT 1Ch,
            // and that some INT 1Ch clients expect that we save DX.
            //
            //      PUSH DS              (0x1E)
            //      PUSH DX              (0x52)
            //      PUSH AX              (0x50)
            //      MOV  AX, 0040        (0xB8 0x40 0x00)
            //      MOV  DS, AX          (0x8E 0xD8)
            //      INC  WORD PTR [6C]   (0xFF 0x06 0x6C 0x00)
            //      JNZ  +6              (0x75 0x04)
            //      INC  WORD PTR [6E]   (0xFF 0x06 0x6E 0x00)
            //      INT  1C              (0xCD 0x1C)
            //      MOV  AL, 20          (0xB0 0x20)
            //      OUT  20, AL          (0xE6 0x20)
            //      POP  AX              (0x58)
            //      POP  DX              (0x5A)
            //      POP  DS              (0x1F)
            //      IRET                 (0xCF)
            //

            cpu.SetWord(0xF0000 + 0xFEA5 + 0x00, 0x521E);
            cpu.SetByte(0xF0000 + 0xFEA5 + 0x02, 0x50);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x03, 0x40B8);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x06, 0xD88E);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x08, 0x06FF);
            cpu.SetByte(0xF0000 + 0xFEA5 + 0x0A, 0x6C);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x0C, 0x0475);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x0E, 0x06FF);
            cpu.SetByte(0xF0000 + 0xFEA5 + 0x10, 0x6E);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x12, 0x1CCD);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x14, 0x20B0);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x16, 0x20E6);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x18, 0x5A58);
            cpu.SetWord(0xF0000 + 0xFEA5 + 0x1A, 0xCF1F);

            // setup interrupt vector 08h to point to the top of the code
            cpu.SetWord(0x08 * 4 + 0, 0xFEA5);
            cpu.SetWord(0x08 * 4 + 2, 0xF000);

            // set up INT 1Ch to point to the IRET at the bottom of the code
            cpu.SetWord(0x1C * 4 + 0, 0xFEC0);
            cpu.SetWord(0x1C * 4 + 2, 0xF000);
        }

        // --------------------------------------------------------------------
        // CreateKeyboardInterrupt

        private void CreateKeyboardInterrupt ()
        {
            var cpu = Machine.Cpu;

            // F000:E987 is address of INT 09h in XT BIOS.

            cpu.SetByte(0xF0000 + 0xE987 + 0x00, 0x50);     // PUSH AX
            cpu.SetWord(0xF0000 + 0xE987 + 0x01, 0x20B0);   // MOV AL, 20
            cpu.SetWord(0xF0000 + 0xE987 + 0x03, 0x20E6);   // OUT 20, AL
            cpu.SetWord(0xF0000 + 0xE987 + 0x05, 0xCF58);   // POP AX; IRET

            // setup interrupt vector 09h to point to the code above
            cpu.SetWord(0x09 * 4 + 0, 0xE987);
            cpu.SetWord(0x09 * 4 + 2, 0xF000);
        }

        // --------------------------------------------------------------------
        // Interrupt

        public int Interrupt (int which)
        {
            int serviceNumber = (which << 8)
                              | Machine.Cpu.GetByte((int) Cpu.Reg.AX + 1);

            switch (serviceNumber)
            {
                case 0x1506:
                    // INT 15h, AH = 06h, unknown
                    Machine.Cpu.Flags |= 1;
                    break;

                case 0x1600:
                    return GetKey();

                case 0x1601:
                    PeekKey();
                    break;

                case 0x1A00:
                    // INT 1Ah, AH = 00h, get time of day into CX:DX (AL=00h)
                    Machine.Cpu.SetWord((int) Cpu.Reg.DX, Machine.Cpu.GetWord(0x46C));
                    Machine.Cpu.SetWord((int) Cpu.Reg.CX, Machine.Cpu.GetWord(0x46E));
                    Machine.Cpu.SetByte((int) Cpu.Reg.AX, 0);
                    break;

                default:

                    if (which == 0x11)
                    {
                        // INT 11h just sets AX to the word at 0040:0010
                        Machine.Cpu.SetWord((int) Cpu.Reg.AX,
                                            Machine.Cpu.GetWord(0x410));
                        return 0;
                    }

                    return -serviceNumber;
            }

            return 0;
        }

        // --------------------------------------------------------------------
        // ReadPort

        public int ReadPort (int which) => which switch
        {
            // 8253 timer ports
            (>= 0x40) and (<= 0x42) => timerPlugin.GetInterval(which),
            // 8253 command port -- not supported
            // 0x43 => -1,

            // 8255 ppi keyboard input port,
            0x60 => inputClient.GetScanCode(),
            // 8255 ppi command port B
            0x61 => inputClient.GetCommand(),

            // no joystick
            0x201 => 0xFF,

            // error on any other port
            _  => -1,
        };

        // --------------------------------------------------------------------
        // WritePort

        public int WritePort (int which, int value) => which switch
        {
            // 8253 timer ports
            (>= 0x40) and (<= 0x42) => timerPlugin.SetInterval(which, value),
            // 8253 command port
            0x43 => timerPlugin.SetCommand(value),

            // 8255 ppi command port B
            0x61 => inputClient.SetCommand(value),

            // no joystick
            0x201 => 0,

            // error on any other port
            _ => - (0xFF00 | value),
        };

        // --------------------------------------------------------------------
        // return next key (INT 16H, AH = 00h)

        private int GetKey ()
        {
            for (;;)
            {
                var key = inputClient.GetNextKey(true);
                if (key == int.MinValue)
                    return Cpu.YieldUntilInterrupt;

                // reflect the status of modifier keys in the
                // keyboard flags byte at 0040:0017
                int mask = ((key >> 8) & 0x7F) switch
                {
                    0x36 => 0x01,   // right shift
                    0x2A => 0x02,   // left shift
                    0x1D => 0x04,   // ctrl
                    0x38 => 0x08,   // alt
                    0x46 => 0x10,   // scroll lock
                    0x45 => 0x20,   // num lock
                    0x3A => 0x40,   // caps lock
                    _    => 0x00,
                };

                int flags = Machine.Cpu.GetByte(0x417);
                if (mask != 0)
                {
                    if ((key & 0x8000) == 0)
                        flags |= mask;      // key press, turn bit on
                    else
                        flags &= ~mask;     // key release, turn bit off
                    Machine.Cpu.SetByte(0x417, (byte) flags);
                    continue;
                }

                if ((key & 0x8000) == 0)    // key press
                {
                    if ((flags & 0x08) != 0)
                    {
                        int ascii = key & 0xFF;
                        bool validKey = (ascii >= 'A' && ascii <= 'Z');
                        if (! validKey)
                        {
                            #if DEBUGGER
                            System.Console.WriteLine($"DROPPING INVALID ALT KEY {key:X4}");
                            #endif
                            continue;
                        }
                        key &= 0x7F00;      // reset ascii part if alt is held
                    }

                    // reset the circular buffer at 40:1E
                    Machine.Cpu.SetWord(0x41A, 0x1E);
                    Machine.Cpu.SetWord(0x41C, 0x1E);

                    Machine.Cpu.SetWord((int) Cpu.Reg.AX, key);
                    return 0;
                }
            }
        }

        // --------------------------------------------------------------------
        // peek next key (INT 16H, AH = 01h)

        private void PeekKey ()
        {
            var key = inputClient.GetNextKey(false);
            if (key == int.MinValue)
            {
                // set ZF=1 if key is not available
                Machine.Cpu.Flags |= 0x40;
            }
            else if (    (key & 0x8000) != 0
                      || key == 0x2A00 /* left shift */
                      || key == 0x3600 /* right shift */
                      || key == 0x1D00 /* ctrl */
                      || key == 0x3800 /* alt */
                      || key == 0x4600 /* scroll lock */
                      || key == 0x4500 /* num lock */
                      || key == 0x3A00 /* caps lock */)
            {
                // discard key releases, and non-ascii keys, and set ZF=1
                inputClient.GetNextKey(true);
                Machine.Cpu.Flags |= 0x40;
            }
            else
            {
                // set ZF=0, AX=key code if key is available
                Machine.Cpu.Flags &= ~0x40;
                Machine.Cpu.SetWord((int) Cpu.Reg.AX, key);
            }
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] private Machine Machine;
        [java.attr.RetainType] private TimerPlugin timerPlugin;
        [java.attr.RetainType] private InputClient inputClient;

        // --------------------------------------------------------------------
        // TimerPlugin

        private class TimerPlugin : Cpu.PluginTimer
        {

            // --------------------------------------------------------------------
            // constructor

            public TimerPlugin (Machine machine)
            {
                cpu = machine.Cpu;

                // set timer 0 to default of 18.2 times per second
                limit_0 = (65536 * 1000 * LIMIT_FACTOR) / TIMER_BASE;
                limit_2 = limit_0;

                // no timer is being programmed at this time
                lowByteTimer = highByteTimer = -1;
            }

            // --------------------------------------------------------------------
            // timer tick

            public override void Tick (int deltaMilliseconds)
            {
                if ((count_0 += deltaMilliseconds * LIMIT_FACTOR) >= limit_0)
                {
                    count_0 -= limit_0;
                    if (count_0 > limit_0)
                        count_0 = limit_0 - 1;

                    cpu.Signal_IRQ(0);
                }
            }

            // --------------------------------------------------------------------
            // set timer interval

            public int SetInterval (int timer, int value)
            {
                if (lowByteTimer == -1)
                {
                    // remember the first of two bytes being written
                    lowByteValue = value;
                    lowByteTimer = timer;
                    #if DEBUGGER
                    if (timer == 0x40)
                        System.Console.WriteLine($"TIMER {timer:X2} BEGIN PROGRAMMING WITH VALUE {value:X2}");
                    #endif
                }
                else if (lowByteTimer == timer)
                {
                    int divisor = (value << 8) | lowByteValue;
                    if (divisor == 0)
                        divisor = 65536;
                    int interval = (divisor * 1000 * LIMIT_FACTOR) / TIMER_BASE;
                    lowByteTimer = -1;

                    #if DEBUGGER
                    if (timer == 0x40)
                        System.Console.WriteLine($"TIMER {timer:X2} SECOND VALUE {value:X2} DIVISOR {divisor} INTERVAL {interval/(float)LIMIT_FACTOR} MS");
                    #endif

                    if (timer == 0x40)
                    {
                        limit_0 = interval;
                        count_0 = 0;
                    }
                    else if (timer == 0x42)
                    {
                        limit_2 = interval;
                        count_2 = 0;
                    }
                    else
                    {
                        // we only support timers 0 and 2
                        return - (0xFF00 | timer);
                    }
                }
                else
                {
                    // inconsistent timer programming
                    return - ((lowByteTimer << 8) | timer);
                }

                return 0;
            }

            // --------------------------------------------------------------------
            // GetInterval

            public int GetInterval (int timer)
            {
                // programs generally send a latch command (mode AND 0x30 = 0x00)
                // to control port 43H before reading.  this tells 8253 to keep a
                // copy of the counter in some temporary storage so it can be read
                // correctly.  we ignore writes to port 43H, but always make a copy.

                if (highByteTimer == -1)
                {
                    int count;
                    if (timer == 0x40)
                        count = limit_0 - count_0;
                    else if (timer == 0x42)
                        count = limit_2 - count_2;
                    else
                    {
                        // we only support timers 0 and 2
                        return - (0xFF00 | timer);
                    }

                    // we actually count the number of (milliseconds * LIMIT_FACTOR)
                    // since the last trigger, but here we have to report the number
                    // of 1/1193181Hz pulses until the next trigger.  note that we
                    // already inverted the count (in the condition just above).
                    count = (int) (((long) count * TIMER_BASE) / (LIMIT_FACTOR * 1000));

                    // remember the second of two bytes being read
                    highByteValue = count >> 8;
                    highByteTimer = timer;

                    return (count & 0xFF);
                }
                else if (highByteTimer == timer)
                {
                    highByteTimer = -1;
                    return highByteValue;
                }
                else
                {
                    // inconsistent timer programming
                    return - ((highByteTimer << 8) | timer);
                }
            }

            // --------------------------------------------------------------------
            // set timer command

            public int SetCommand (int value)
            {
                // port 43H command format:
                // (76)(54)(321)(0)
                // bits 76 - counter select, invalid if both bits set
                // bits 54 - both bits clear to latch (see GetInerval),
                //           both bits set to access both LSB and MSB,
                //           other values invalid
                // bits 321 - 000=interrupt on terminal count,
                //            011=square wave generator
                // bit 0 - always zero for normal binary mode (else BCD)

                if (value == 0x00)
                {
                    // latch command for counter 0.  we ignore because we
                    // latch automatically.  see GetInterval ()
                    return 0;
                }

                if (value == 0xB6)
                {
                    // select square wave for counter 2.  we ignore until
                    // we support sound output
                    return 0;
                }

                // other values are not supported
                return -1;
            }

            // --------------------------------------------------------------------

            [java.attr.RetainType] private Cpu cpu;

            [java.attr.RetainType] public volatile int limit_0;
            [java.attr.RetainType] public volatile int count_0;
            [java.attr.RetainType] public volatile int limit_2;
            [java.attr.RetainType] public volatile int count_2;

            // stores LSB during counter writes
            [java.attr.RetainType] public int lowByteValue;
            [java.attr.RetainType] public int lowByteTimer;

            // stores MSB during counter reads
            [java.attr.RetainType] public int highByteValue;
            [java.attr.RetainType] public int highByteTimer;

            // clock rate of the 8253 timer component
            private const int TIMER_BASE = 1193181;
            // the limit is multiplied by this factor for increased accuracy
            private const int LIMIT_FACTOR = 30;

        }

        // --------------------------------------------------------------------
        // InputClient

        private class InputClient : IShell.IInput.Client
        {

            // --------------------------------------------------------------------
            // constructor

            public InputClient (Machine machine)
            {
                cpu = machine.Cpu;
                queue = new ArrayList();
                port61 = 0x30;
            }

            // --------------------------------------------------------------------
            // key press

            public override void KeyPress (int scanCode, int asciiCode)
            {
                int keyCode = ((scanCode & 0x7F) << 8) | (asciiCode & 0xFF);
                int count;
                lock (queue)
                {
                    if ((count = queue.Count) < 6)
                        queue.Add(keyCode);
                }
                if (count == 0)
                {
                    cpu.Signal_IRQ(1);
                    if (asciiCode != 0)
                    {
                        // write the key into the circular buffer at 40:1E,
                        // and reset the head (at 40:1A) and tail (at 40:1C)
                        cpu.SetWord(0x41E, keyCode);
                        cpu.SetWord(0x41A, 0x1E);
                        cpu.SetWord(0x41C, 0x20);
                    }
                }
            }

            // --------------------------------------------------------------------
            // key release

            public override void KeyRelease (int scanCode)
            {
                int keyCode = ((scanCode & 0x7F) | 0x80) << 8;
                int count;
                lock (queue)
                {
                    if ((count = queue.Count) < 6)
                        queue.Add(keyCode);
                }
                if (count == 0)
                    cpu.Signal_IRQ(1);
            }

            // --------------------------------------------------------------------
            // pop key from queue or just peek key

            public int GetNextKey (bool pop)
            {
                int key = int.MinValue;
                lock (queue)
                {
                    if (queue.Count > 0)
                    {
                        key = (int) queue[0];
                        if (pop)
                        {
                            queue.RemoveAt(0);
                            if (queue.Count > 0)
                                cpu.Signal_IRQ(1);
                        }
                    }
                }
                return key;
            }

            // --------------------------------------------------------------------
            // GetScanCode

            public int GetScanCode ()
            {
                int key = GetNextKey(false);
                return (key == int.MinValue) ? 0 : (key >> 8);
            }

            // --------------------------------------------------------------------
            // GetCommand

            public int GetCommand ()
            {
                // read from port 61 (ppi port B)
                return port61;
            }

            // --------------------------------------------------------------------
            // SetCommand

            public int SetCommand (int value)
            {
                // write to port 61, the 8255 ppi command port B

                if ((port61 & 0x80) == 0x80 && (value & 0x80) == 0)
                {
                    // high bit is used to reset keyboard IRQ1 and discard
                    // last scan code, by first writing with bit 0x80 set,
                    // and then writing again with bit 0x80 clear
                    GetNextKey(true);
                }

                if ((value & ~0x83) != (port61 & ~0x83))
                {
                    // bits 0x01 and 0x02 control speaker output, which
                    // we currently ignore.  we do not support setting
                    // any other bits in port B
                    return -1;
                }

                port61 = value;
                return 0;
            }

            // --------------------------------------------------------------------

            [java.attr.RetainType] private Cpu cpu;
            [java.attr.RetainType] private ArrayList queue;

            // port 61 is used to reset IRQ 1 and control the speaker
            [java.attr.RetainType] private int port61;
        }

    }
}
