
#define PREFETCH_4
//#define PREFETCH_8

using System;
using System.IO;

namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // default constructor

        public Cpu ()
        {
            // the memory is 1MB+64KB for base RAM.  we also reserve 8 bytes,
            // in case Prefetch() is called near address 0xFFFFF, so it can
            // prefetch zeroes.  and finally, we reserve room for registers.
            stateBytes = new byte[memorySize + prefetchSize + (int) Reg.Size];

            // reset instruction pointer (this takes prefetch into account)
            InstructionAddress = 0;

            // set flags to interrupts enabled
            Flags = (1 << 9);

            // register ports 20H and 21H with the plugin for PIC 8259.
            // we also use this plugin to register exception interrupts
            RegisterPlugin(new InterruptController(this),
                           new int[] { 0x00, 0x01, 0x02, 0x03, 0x04 },
                           new int[] { 0x20, 0x21 });

            // set machine model type
            stateBytes[0xFFFFE] = 0xFE;     // IBM XT
        }

        // --------------------------------------------------------------------
        // de-serializing constructor

        public Cpu (Stream input, int addr) : this()
        {
            Load(input, addr);
        }

        // --------------------------------------------------------------------
        // load binary into memory

        public void Load (Stream input, int addr)
        {
            int ch;
            while ((ch = input.ReadByte()) != -1)
                stateBytes[addr++] = (byte) ch;
        }

        // --------------------------------------------------------------------
        // get byte (8-bit)

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public int GetByte (int addr) => stateBytes[addr];

        // --------------------------------------------------------------------
        // set byte (8-bit)

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public void SetByte (int addr, byte v) => stateBytes[addr] = v;

        // --------------------------------------------------------------------
        // get word (16-bit)

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public int GetWord (int addr)
        {
            var bytes = this.stateBytes;
            return (bytes[addr] | (bytes[addr + 1] << 8));
        }

        // --------------------------------------------------------------------
        // set word (16-bit)

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        public void SetWord (int addr, int v)
        {
            var bytes = this.stateBytes;
            bytes[addr] = (byte) v;
            bytes[addr + 1] = (byte) (v >> 8);
        }

        // --------------------------------------------------------------------
        // get, set flags as a word

        public int Flags
        {
            // note that the internal prefetchAddress value should alway
            // point beyond the last fetch, so we have to take this into
            // account when getting and setting the property

            get =>   // shift sign bit of carry flag into bit #0
                     ((carryFlag >> 31) & 0x0001)
                     // shift sign bit of adjust flag into bit #4
                   | ((adjustFlag >> 31) & 0x0010)
                     // shift sign bit of sign flag into bit #7
                   | ((signFlag >> 31) & 0x0080)
                     // shift sign bit of trap flag into bit #8
                   | ((trapFlag >> 31) & 0x0100)
                     // shift sign bit of interrupt flag into bit #9
                   | ((interruptFlag >> 31) & 0x0200)
                     // shift sign bit of direction flag into bit #10
                   | ((directionFlag >> 31) & 0x0400)
                     // shift sign bit of overflow flag into bit #11
                   | ((overflowFlag >> 31) & 0x0800)
                     // if zeroFlag is 0, result is -1, otherwise 0;
                     // then shift sign bit of result into bit #6
                   | ((((-1 - zeroFlag) & (zeroFlag - 1)) >> 31) & 0x0040)
                     // table entry results in 0x0004 for even-parity
                   |   parityTable[parityFlag & 0xFF]
                     // the following bits are always set
                   |  0xF002;

            set
            {
                carryFlag = value << (31 - 0);      // CF < 0 if bit #0 set
                adjustFlag = value << (31 - 4);     // AF < 0 if bit #4 set
                zeroFlag = (value & 0x40) ^ 0x40;   // ZF = 0 if bit #6 set
                signFlag = value << (31 - 7);       // SF < 0 if bit #7 set
                trapFlag = value << (31 - 8);       // TF < 0 if bit #8 set
                interruptFlag = value << (31 - 9);  // IF < 0 if bit #9 set
                directionFlag = value << (31 - 10); // DF < 0 if bit #10 set
                overflowFlag = value << (31 - 11);  // OF < 0 if bit #11 set
                // if bit #2 set, we select parityTable[5] which has PF=1
                // if bit not set, we select parityTable[1] which has PF=0
                parityFlag = (value & 0x0004) + 1;
            }
        }

        // --------------------------------------------------------------------
        // get, set address used for instruction fetching

        public int InstructionAddress
        {
            // note that the internal prefetchAddress value should alway
            // point beyond the last fetch, so we have to take this into
            // account when getting and setting the property

            get => prefetchAddress + (prefetchIndex >> 3) + 1;
            set => prefetchAddress = (value & 0xFFFFF)
                                   - ((prefetchIndex = prefetchBits) >> 3) - 1;
        }

        // --------------------------------------------------------------------
        // adjust instruction address with an offset

        public void InstructionBranch (int offset)
        {
            // relative jumps must wrap-around IP within the same CS,
            // so merely adding an offset to InstructionAddress is not
            // correct behavior.  the following is required, and this
            // method does an optimized version of it.
            //
            // InstructionAddress = codeSegmentAddress + (0xFFFF &
            //      (InstructionAddress - codeSegmentAddress + offset))

            int cs;
            prefetchAddress =
                ((cs = codeSegmentAddress) + (0xFFFF &
                    (   (prefetchAddress + (prefetchIndex >> 3) + 1)
                      - cs + offset)) & 0xFFFFF)
                - ((prefetchIndex = prefetchBits) >> 3) - 1;
        }

        // --------------------------------------------------------------------
        // prefetch instruction bytes, when the prefetch buffer is empty

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]

        private int Prefetch ()
        {
            // for slightly better performance, we don't AND with 0xFFFFF
            // when prefetching memory, which means that very near the 1MB
            // limit, we will prefetch incorrectly.

            var bytes = this.stateBytes;

            int address;
            var byteZero = bytes[address = (prefetchAddress +=
                                                (prefetchBits >> 3) + 1)];

            #if PREFETCH_4
            prefetchBuffer =  bytes[address + 1]
                           | (bytes[address + 2] << 8)
                           | (bytes[address + 3] << 16)
                           | (bytes[address + 4] << 24);
            #elif PREFETCH_8
            #pragma warning disable 0675
            prefetchBuffer = (uint)  bytes[address + 1]
                           | (uint) (bytes[address + 2] << 8)
                           | (uint) (bytes[address + 3] << 16)
                           | (uint) (bytes[address + 4] << 24)
                           | (((long) bytes[address + 5]) << 32)
                           | (((long) bytes[address + 6]) << 40)
                           | (((long) bytes[address + 7]) << 48)
                           | (((long) bytes[address + 8]) << 56);
            #pragma warning restore 0675
            #endif

            prefetchIndex = 0;

            return byteZero;
        }

        // --------------------------------------------------------------------
        // get next instruction byte

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        private int GetInstructionByte ()
        {
            int o;
            #if DEBUGGER
            if (InstructionAddress - codeSegmentAddress > 0xFFFF)
            {
                throw new System.InvalidProgramException(
                    $"Wrap-around in prefetch near {InstructionAddress:X5}");
            }
            #endif
            if ((o = prefetchIndex) <= prefetchBits - 8)
            {
                prefetchIndex = o + 8;
                return ((int) (prefetchBuffer >> o)) & 0xFF;
            }
            return Prefetch();
        }

        // --------------------------------------------------------------------
        // get next instruction word

        [System.Runtime.CompilerServices.MethodImpl(
            System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]

        private int GetInstructionWord ()
        {
            int o;
            if ((o = prefetchIndex) <= prefetchBits - 16)
            {
                prefetchIndex = o + 16;
                return ((int) (prefetchBuffer >> o)) & 0xFFFF;
            }
            else
                return GetInstructionByte() | (GetInstructionByte() << 8);
        }

        // --------------------------------------------------------------------
        // reset caches after updating registers

        public void CacheSegmentRegisters ()
        {
            var mem = stateBytes;

            codeSegmentAddress =
                (mem[(int) Reg.CS] << 4) | (mem[(int) Reg.CS + 1] << 12);

            dataSegmentAddress =
                    (mem[(int) Reg.DS] << 4) | (mem[(int) Reg.DS + 1] << 12);

            extraSegmentAddress =
                    (mem[(int) Reg.ES] << 4) | (mem[(int) Reg.ES + 1] << 12);

            stackSegmentAddressForModrm =
            stackSegmentAddressForPushPop =
                    (mem[(int) Reg.SS] << 4) | (mem[(int) Reg.SS + 1] << 12);
        }

        // --------------------------------------------------------------------
        // hitpoint variables

        [java.attr.RetainType] public int HitPointAddress = int.MinValue;
        [java.attr.RetainType] public Action HitPointAction;

        // --------------------------------------------------------------------

        [java.attr.RetainType] private byte[] stateBytes;
        private const int memorySize = 0x100000 + 0x10000;  // 1MB+64KB
        private const int prefetchSize = 8;

        [java.attr.RetainType] private int prefetchAddress;
        [java.attr.RetainType] private int prefetchIndex;
        #if PREFETCH_4
        [java.attr.RetainType] private int prefetchBuffer;
        private const int prefetchBits = 32;
        #elif PREFETCH_8
        [java.attr.RetainType] private long prefetchBuffer;
        private const int prefetchBits = 64;
        #endif

        [java.attr.RetainType] private int codeSegmentAddress;
        [java.attr.RetainType] private int dataSegmentAddress;
        [java.attr.RetainType] private int extraSegmentAddress;
        [java.attr.RetainType] private int stackSegmentAddressForModrm;
        [java.attr.RetainType] private int stackSegmentAddressForPushPop;
        #if DEBUGGER
        // used for segment wrap-around checks in a debug build; see Run ()
        [java.attr.RetainType] private int modrmSegmentAddress;
        #endif

        // detect updates of segment registers; see SegmentOverride ()
        [java.attr.RetainType] private int segmentOverrideFlags;
        private const int inSegmentOverride = 1;
        private const int dsSegmentUpdated = 2;
        private const int ssSegmentUpdated = 4;

        // non-zero if a signal occurred; -1 if a STOP signal
        [java.attr.RetainType] private volatile int interruptEvent;
        // bits 0..7 are interrupts (IRQs) that are pending servicing;
        // bits 16..23 are interrupt levels that are inhibited;
        // bit 30 is set on IRQ servicing and cleared by EOI command.
        private int interruptMask;
        // round-robin counter to prevent irq starvation
        [java.attr.RetainType] private int lastIrqHandled;

        // the last spin count and exec count (see CpuRun)
        [java.attr.RetainType] private int lastSpinCount;
        [java.attr.RetainType] private int lastExecCount;

        // --------------------------------------------------------------------
        // registers

        public enum Reg
        {
            // offsets correspond to reg field in the modrm byte (00xxx000)
            // which means each register is aligned on an 8-byte offset
            AX = memorySize + prefetchSize,
            CX = AX + 8,
            DX = CX + 8,
            BX = DX + 8,
            SP = BX + 8,
            BP = SP + 8,
            SI = BP + 8,
            DI = SI + 8,

            // similarly, offsets correspond to reg field in the modrm byte,
            // when it is interpreted as 'sreg' for segment register.
            ES = AX + 4,
            CS = CX + 4,
            SS = DX + 4,    // cached in stackSegmentAddressForXxx
            DS = BX + 4,    // cached in dataSegmentAddress

            #if DEBUGGER
            IP = SP + 4,    // IP pseudo register
            FL = BP + 4,    // flags pseudo register
            #endif

            Size = DI + 8,
        }

        [java.attr.RetainType] private int carryFlag;       // true if < 0
        [java.attr.RetainType] private int adjustFlag;      // true if < 0
        [java.attr.RetainType] private int signFlag;        // true if < 0
        [java.attr.RetainType] private int overflowFlag;    // true if < 0
        [java.attr.RetainType] private int parityFlag;      // use lookup table
        [java.attr.RetainType] private int zeroFlag;        // true if = 0
        [java.attr.RetainType] private int trapFlag;        // true if < 0
        [java.attr.RetainType] private int interruptFlag;   // true if < 0
        [java.attr.RetainType] private int directionFlag;   // true if < 0

        // --------------------------------------------------------------------
        // parity table

        [java.attr.RetainType] private readonly static int[] parityTable = InitParityTable();

        static int[] InitParityTable ()
        {
            // count the number of bits in each byte value in range 0..255,
            // store value 0x0004 (representing PF=1 flag) for even bits
            var table = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int n = 0;
                for (int j = 0; j < 8; j++)
                    n += (i & (1 << j)) >> j;
                table[i] = ((n + 1) & 1) << 2;  // 4 when (n % 2 == 0)
            }
            return table;
        }

    }
}
