
using ArrayList = System.Collections.ArrayList;

namespace com.spaceflint.x86
{
    public class Dos : Cpu.IPlugin
    {

        // --------------------------------------------------------------------
        // constructor

        public Dos (Machine machine, object initObject)
        {
            Machine = machine;
            var cpu = Machine.Cpu;

            // register DOS interrupts 20H and 21H, and no I/O ports
            var dosInterrupts = new int[] { 0x20, 0x21 };
            cpu.RegisterPlugin(this, dosInterrupts, null);

            if (initObject is byte[] program)
            {
                var segment = (program[0] == 'M' && program[1] == 'Z')
                            ? LoadProgram_EXE(program)
                            : LoadProgram_COM(program);

                if (segment < 0)
                {
                    throw new System.InvalidProgramException(
                        $"Program too large to load (error {segment})");
                }

                InitProgramSegmentPrefix(segment);
            }
        }

        // --------------------------------------------------------------------
        // LoadProgram_COM

        private int LoadProgram_COM (byte[] program)
        {
            var segment = AllocateMemory((program.Length + 15 + 0x100) / 16);
            if (segment >= 0)
            {
                var cpu = Machine.Cpu;

                cpu.SetWord((int) Cpu.Reg.SS, segment);
                cpu.SetWord((int) Cpu.Reg.SP, 0xFFFE);

                int address = (segment << 4) + 0x100;
                cpu.SetWord((int) Cpu.Reg.CS, segment);
                cpu.InstructionAddress = address;

                int length = program.Length;
                cpu.SetWord((int) Cpu.Reg.CX, length);

                for (var i = 0; i < length; i++)
                    cpu.SetByte(i + address, program[i]);
            }

            return segment;
        }

        // --------------------------------------------------------------------
        // LoadProgram_EXE

        private int LoadProgram_EXE (byte[] program)
        {
            if (GetWord(program, 0x1A) != 0)
                return -2;      // DOS overlay not supported

            // the word at header + 4 is program size in 512-byte pages.
            // multiply this by 32 to get number of 16-byte paragraphs,
            // then subtract the length of the header, which is specified
            // in word header + 8, as the number of 16-byte paragraphs,
            // then add 16 paragraphs (256 bytes) room for the PSP.

            int headerLen = GetWord(program, 0x08);
            int allocLen = (GetWord(program, 0x04) << 5) - headerLen + 0x10;
            int copyLen = program.Length - (headerLen <<= 4);

            if ((allocLen << 4) < copyLen || copyLen < 1)
                return -3;      // something is wrong with the header

            var pspSegment = AllocateMemory(allocLen);
            if (pspSegment >= 0)
            {
                var cpu = Machine.Cpu;

                var baseSegment = pspSegment + 0x10;
                int address = baseSegment << 4;
                for (var i = 0; i < copyLen; i++)
                    cpu.SetByte(address + i, program[headerLen + i]);

                // the word at header + 0x16 is the code segment
                int codeSegment = baseSegment + GetWord(program, 0x16);
                DoFixups(cpu, program, baseSegment, codeSegment);
                InitRegs(cpu, program, baseSegment, codeSegment);
            }

            return pspSegment;

            static void DoFixups (Cpu cpu, byte[] program,
                                  int baseSegment, int codeSegment)
            {
                // the word at header + 0x18 is the offset to the first
                // fixup or relocation item, the word at header + 0x06
                // holds the number of items to fixup.  each fixup item
                // is a four-byte segment:offset pair

                int relocCount = GetWord(program, 0x06);
                int relocEntry = GetWord(program, 0x18);

                for (var i = 0; i < relocCount; i++)
                {
                    int relocAddr =
                            (baseSegment + GetWord(program, relocEntry + 2) << 4)
                           + GetWord(program, relocEntry);
                    cpu.SetWord(relocAddr,
                                    cpu.GetWord(relocAddr) + baseSegment);
                    relocEntry += 4;
                }
            }

            static void InitRegs (Cpu cpu, byte[] program,
                                  int baseSegment, int codeSegment)
            {
                // relocate the SS:SP pair that appears as a segment:offset
                // pair at header + 0x0E.  if zero, select default CS:FFFE

                int ss = GetWord(program, 0x0E);
                int sp = GetWord(program, 0x10);
                if (ss == 0 || sp == 0)
                {
                    ss = codeSegment;
                    sp = 0xFFFE;
                }
                else
                    ss += baseSegment;

                cpu.SetWord((int) Cpu.Reg.SS, ss);
                cpu.SetWord((int) Cpu.Reg.SP, sp);

                // set CS:IP according to the header
                cpu.SetWord((int) Cpu.Reg.CS, codeSegment);
                cpu.InstructionAddress = (codeSegment << 4)
                                       + GetWord(program, 0x14);

                // set CX:BX to program length minus 0x200
                int length = program.Length - 0x200;
                cpu.SetWord((int) Cpu.Reg.BX, length >> 16);
                cpu.SetWord((int) Cpu.Reg.CX, (ushort) length);
            }

            static int GetWord (byte[] program, int index) =>
                program[index] | (program[index + 1] << 8);
        }

        // --------------------------------------------------------------------
        // InitProgramSegmentPrefix

        private void InitProgramSegmentPrefix (int segment)
        {
            var cpu = Machine.Cpu;
            int addr = segment << 4;
            cpu.SetWord(addr,        0x20CD);   // INT 20h
            cpu.SetWord(addr + 0x02, 0x9FFF);   // top of memory in paragraphs
            cpu.SetWord(addr + 0x06, 0xFEF0);   // top of segment

            cpu.SetWord((int) Cpu.Reg.DS, segment);
            cpu.SetWord((int) Cpu.Reg.ES, segment);

            cpu.CacheSegmentRegisters();
        }

        // --------------------------------------------------------------------
        // Interrupt

        public int Interrupt (int which)
        {
            int serviceNumber = which << 8;
            if (which == 0x21)
                serviceNumber |= Machine.Cpu.GetByte((int) Cpu.Reg.AX + 1);

            int error = 0;

            switch (serviceNumber)
            {
                case 0x2000:        // INT 20h
                case 0x2100:        // INT 21h, AH = 00h
                case 0x214C:        // INT 21h, AH = 4Ch

                    // request to terminate program
                    Machine.Cpu.InstructionAddress--;
                    Machine.Stop();
                    return 0;

                case 0x2106:        // AH = 06h

                    System.Console.Write((char) Machine.Cpu.GetByte((int) Cpu.Reg.AX));
                    return 0;

                case 0x2109:        // AH = 09h

                    PrintMessage();
                    break;

                case 0x2119:        // AH = 19h

                    // get current default drive in AL - return drive A:
                    Machine.Cpu.SetByte((int) Cpu.Reg.AX, 0);
                    break;

                case 0x2125:        // AH = 25h
                case 0x2135:        // AH = 35h

                    GetOrSetInterruptVector(serviceNumber - 0x2125);
                    break;

                case 0x2130:        // AH = 30h

                    DosVersion();
                    break;

                case 0x2133:        // AH = 33h

                    if (CtrlBreak())
                        break;
                    goto default;

                case 0x2137:        // AH = 37h

                    if (SwitchChar())
                        break;
                    goto default;

                case 0x2138:        // AH = 38h

                    // get country information.  return CF=1, error code 2
                    Machine.Cpu.Flags |= 1;
                    Machine.Cpu.SetByte((int) Cpu.Reg.AX, 2);
                    break;

                case 0x2144:        // AH = 44h

                    if (GetDeviceInfo())
                        break;
                    goto default;

                case 0x2148:        // AH = 48h
                case 0x2149:        // AH = 49h
                case 0x214A:        // AH = 4Ah

                    if (MemoryServices(serviceNumber))
                        break;
                    goto default;

                default:
                    error = -serviceNumber;
                    break;
            }

            return error;
        }

        // --------------------------------------------------------------------
        // PrintMessage (INT 21h, AH = 4Ch)

        private void PrintMessage ()
        {
            var cpu = Machine.Cpu;
            var adr = (cpu.GetWord((int) Cpu.Reg.DS) << 4)
                    +  cpu.GetWord((int) Cpu.Reg.DX);
            var sb = new System.Text.StringBuilder();
            for (int count = 0; count < 256; count++)
            {
                var ch = cpu.GetByte(adr++);
                if (ch == '$')
                    break;
                sb.Append((char) ch);
            }
            Machine.Shell.Alert(sb.ToString(), false);
        }

        // --------------------------------------------------------------------
        // DosVersion (INT 21h, AH = 30h)

        private void DosVersion ()
        {
            Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x0A02);  // DOS 2.1
            Machine.Cpu.SetWord((int) Cpu.Reg.BX, 0);       // serial number
            Machine.Cpu.SetWord((int) Cpu.Reg.CX, 0);       //     zero
        }

        // --------------------------------------------------------------------
        // CtrlBreak (INT 21h, AH = 33h)

        private bool CtrlBreak ()
        {
            var al = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            if (al == 0)
            {
                // return DL = break checking flag (always off)
                Machine.Cpu.SetByte((int) Cpu.Reg.DX, 0);
                return true;
            }
            if (al == 1)
            {
                // set break checking flag via DL
                if (Machine.Cpu.GetByte((int) Cpu.Reg.DX) == 0)
                    return true;
            }
            return false;
        }

        // --------------------------------------------------------------------
        // GetOrSetInterruptVector (INT 21h, AH = 25h, AH = 35h)

        private void GetOrSetInterruptVector (int setIfZero_getIfNonZero)
        {
            // AL specifies the interrupt vector
            var addr = Machine.Cpu.GetByte((int) Cpu.Reg.AX) << 2;
            int srcSeg, srcOfs, dstSeg, dstOfs;
            if (setIfZero_getIfNonZero == 0)
            {
                // AH = 25h, set interrupt vector from DS:DX
                srcSeg = (int) Cpu.Reg.DS;
                srcOfs = (int) Cpu.Reg.DX;
                dstSeg = addr + 2;
                dstOfs = addr;
            }
            else
            {
                // AH = 35h, get interrupt vector into ES:BX
                srcSeg = addr + 2;
                srcOfs = addr;
                dstSeg = (int) Cpu.Reg.ES;
                dstOfs = (int) Cpu.Reg.BX;
            }
            Machine.Cpu.SetWord(dstSeg, Machine.Cpu.GetWord(srcSeg));
            Machine.Cpu.SetWord(dstOfs, Machine.Cpu.GetWord(srcOfs));
        }

        // --------------------------------------------------------------------
        // SwitchChar (INT 21h, AH = 37h)

        private bool SwitchChar ()
        {
            var al = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            if (al == 0)
            {
                // return DL = slash (default switch character)
                Machine.Cpu.SetWord((int) Cpu.Reg.DX, '/');
                return true;
            }
            return false;
        }

        // --------------------------------------------------------------------
        // GetDeviceInfo (INT 21h, AH = 44h)

        private bool GetDeviceInfo ()
        {
            var al = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            var bx = Machine.Cpu.GetWord((int) Cpu.Reg.BX);
            if (al == 0 && bx <= 4)
            {
                // return AX = DX = 0x80D3, to indicate the handle
                // is a non-redirected character device, for handles
                // 0 .. 4:  STDIN, STDOUT, STDERR, STDAUX, STDPRN
                Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x80D3);
                Machine.Cpu.SetWord((int) Cpu.Reg.DX, 0x80D3);
                return true;
            }
            return false;
        }

        // --------------------------------------------------------------------
        // MemoryServices (INT 21h, AH = 48h through AH = 4Ah)

        private bool MemoryServices (int serviceNumber)
        {
            bool ok;
            if (serviceNumber == 0x2148)
            {
                // AH = 48h: allocate memory, BX = size in 16-byte paragraphs
                int paragraphs_needed = Machine.Cpu.GetWord((int) Cpu.Reg.BX);
                int alloc_result = AllocateMemory(paragraphs_needed);
                if (alloc_result > 0)
                {
                    Machine.Cpu.SetWord((int) Cpu.Reg.AX, alloc_result);
                    ok = true;
                }
                else
                {
                    // return out of memory error in AX, largest available in BX
                    Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x0008);
                    Machine.Cpu.SetWord((int) Cpu.Reg.BX, -alloc_result);
                    ok = false;
                }
            }
            else if (serviceNumber == 0x2149)
            {
                // AH = 49h: free memory, ES = block previously allocated
                int block_to_free = Machine.Cpu.GetWord((int) Cpu.Reg.ES);
                ok = FreeMemory(block_to_free);
                if (! ok)   // invalid block address
                    Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x0009);
            }
            else if (serviceNumber == 0x214A)
            {
                // AH = 4Ah: modify memory, ES = block, BX = new size
                int block_to_modify = Machine.Cpu.GetWord((int) Cpu.Reg.ES);
                int paragraphs_needed = Machine.Cpu.GetWord((int) Cpu.Reg.BX);
                int modify_result = ModifyMemory(block_to_modify, paragraphs_needed);
                if (modify_result == 0)
                    ok = true;
                else
                {
                    ok = false;
                    if (modify_result < 0)
                    {
                        // return out of memory error in AX, largest available in BX
                        Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x0008);
                        Machine.Cpu.SetWord((int) Cpu.Reg.BX, -modify_result);
                    }
                    else // invalid block address
                        Machine.Cpu.SetWord((int) Cpu.Reg.AX, 0x0009);
                }
            }
            else
                ok = false;
            return ok;
        }

        // --------------------------------------------------------------------
        // ReadPort, WritePort (not applicable for this plugin)

        public int ReadPort (int which) => -1;

        public int WritePort (int which, int value) => -1;

        // --------------------------------------------------------------------
        // dos memory block

        private class MemoryBlock
        {
            [java.attr.RetainType] public int segment;
            [java.attr.RetainType] public int paragraphs;
        }

        // --------------------------------------------------------------------
        // allocate memory block

        private int AllocateMemory (int paragraphs_needed)
        {
            int block_index = 0;
            int largest_block = 0;
            foreach (MemoryBlock block in memoryBlocks)
            {
                // negative number of paragraphs means a free block
                if (block.paragraphs <= -paragraphs_needed)
                {
                    // we found a free block that is at least as large as
                    // the requested size in paragraphs, so we insert a
                    // new allocated block with the requested size
                    memoryBlocks.Insert(block_index,
                        new MemoryBlock
                        {
                            segment = block.segment,
                            paragraphs = paragraphs_needed,
                        });
                    // update the free block to account for the allocation,
                    // by adding the allocated size to the negative size
                    block.segment += paragraphs_needed;
                    block.paragraphs += paragraphs_needed;

                    MergeFreeMemory();
                    return ((MemoryBlock) memoryBlocks[block_index]).segment;
                }
                else if (block.paragraphs < largest_block)
                    largest_block = block.paragraphs;
                block_index++;
            }
            return largest_block;
        }

        // --------------------------------------------------------------------
        // free memory

        private bool FreeMemory (int segment)
        {
            foreach (MemoryBlock block in memoryBlocks)
            {
                if (block.segment == segment && block.paragraphs > 0)
                {
                    // negate the number of paragraphs to mark free
                    block.paragraphs = -block.paragraphs;
                    MergeFreeMemory();
                    return true;
                }
            }
            return false;
        }

        // --------------------------------------------------------------------
        // modify size of memory block

        private int ModifyMemory (int segment, int paragraphs_needed)
        {
            int block_index = 0;
            foreach (MemoryBlock block in memoryBlocks)
            {
                if (block.segment == segment)
                {
                    if (block.paragraphs > paragraphs_needed)
                    {
                        // if the block found is larger than the new requested
                        // size, we trim the block and insert a new free block
                        memoryBlocks.Insert(block_index + 1,
                            new MemoryBlock
                            {
                                segment = block.segment + paragraphs_needed,
                                paragraphs = paragraphs_needed - block.paragraphs,
                            });
                        block.paragraphs = paragraphs_needed;

                        MergeFreeMemory();
                        return 0;
                    }
                    else
                    {
                        // if the requested size is larger than the block, we
                        // need the next block to be free, and large enough to
                        // combine with the old block to satisfy the request
                        int paragraphs_needed_in_next_block =
                                    block.paragraphs - paragraphs_needed;

                        var next_block = (MemoryBlock) memoryBlocks[block_index + 1];

                        if (next_block.paragraphs <= paragraphs_needed_in_next_block)
                        {
                            block.paragraphs = paragraphs_needed;
                            // trim the adjacent free block, and advance its start
                            next_block.segment -= paragraphs_needed_in_next_block;
                            next_block.paragraphs -= paragraphs_needed_in_next_block;

                            MergeFreeMemory();
                            return 0;
                        }
                        else
                        {
                            // the adjacent block is not large enough, or is not
                            // free; return largest combined size as a negative number
                            int paragraphs_in_next_block = next_block.paragraphs;
                            if (paragraphs_in_next_block > 0)
                                paragraphs_in_next_block = 0;

                            return paragraphs_in_next_block - block.paragraphs;
                        }
                    }
                }
                block_index++;
            }
            return int.MaxValue;
        }

        // --------------------------------------------------------------------
        // merge adjacent memory blocks

        private void MergeFreeMemory ()
        {
            for (int block_index = memoryBlocks.Count; --block_index > 0; )
            {
                // if current block is free or zero-size ...
                var curr_block = (MemoryBlock) memoryBlocks[block_index];
                if (curr_block.paragraphs <= 0)
                {
                    // ... and if the previous block is free (or if not free,
                    // then if the current block is zero-size) ...
                    var prev_block = (MemoryBlock) memoryBlocks[block_index - 1];
                    if (prev_block.paragraphs < 0 || curr_block.paragraphs == 0)
                    {
                        // ... then combine the sizes and delete current
                        prev_block.paragraphs += curr_block.paragraphs;
                        memoryBlocks.RemoveAt(block_index);
                    }
                }
            }
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] private Machine Machine;
        [java.attr.RetainType] private ArrayList memoryBlocks = new ArrayList(
            new MemoryBlock[] {
                // unallocated space
                new MemoryBlock { segment = 0x1000, paragraphs = -40704 }
            });

    }
}