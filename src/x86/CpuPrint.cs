
#if DEBUGGER

namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // print one instruction at current instruction address

        public string PrintInstruction (bool printCurrent)
        {
            printingCurrentInstruction = printCurrent;
            segmentOverrideForPrint = 0;
            return instTable[GetInstructionByte()]?.Print(this) ?? "???";
        }

        // --------------------------------------------------------------------
        // segment override for printing instructions

        private string SegmentOverrideForPrint (Reg segEnum)
        {
            var mem = stateBytes;

            int seg = (int) segEnum;
            segmentOverrideForPrint = seg;

            var oldDataSeg = dataSegmentAddress;
            var oldStackSeg = stackSegmentAddressForModrm;
            var oldModrmSeg = modrmSegmentAddress;
            dataSegmentAddress = stackSegmentAddressForModrm =
                modrmSegmentAddress = (mem[seg] | (mem[seg + 1] << 8)) << 4;

            string s = instTable[GetInstructionByte()].Print(this);
            if (s.Length <= 8)
            {
                // insert prefix into short instructions with no operands
                var segReg = segmentOverrideForPrint - (int) Reg.ES;
                s = $"{s,-8} {RegNamesSeg[segReg]}:";
            }

            dataSegmentAddress = oldDataSeg;
            stackSegmentAddressForModrm = oldStackSeg;
            modrmSegmentAddress = oldModrmSeg;

            return s;
        }

        // --------------------------------------------------------------------
        // move instruction address X positions

        private Cpu SkipInstructionAddress(int offset)
        {
            InstructionAddress = InstructionAddress + offset;
            return this;
        }

        // --------------------------------------------------------------------
        // print override segment in address

        private string PrintSegmentPrefix (Reg seg, string s, int imm = 0)
        {
            if (imm == 8)
            {
                imm = (int) (sbyte) GetInstructionByte();
                if (imm < 0)
                    s += $"-{(-imm & 0xFF):X2}";
                else
                    s += $"+{(imm & 0xFF):X2}";
            }
            else if (imm == 16)
            {
                imm = (int) (short) GetInstructionWord();
                if (imm < 0)
                    s += $"-{(-imm & 0xFFFF):X4}";
                else
                    s += $"+{(imm & 0xFFFF):X4}";
            }
            s = "[" + s + "]";
            if (segmentOverrideForPrint != 0)
            {
                var segReg = segmentOverrideForPrint - (int) Reg.ES;
                s = $"{RegNamesSeg[segReg]}:" + s;
            }
            else
                segmentOverrideForPrint = (int) seg;
            return s;
        }

        // --------------------------------------------------------------------
        // decode modrm address for printing

        (int, string, string) DecodeAndPrintModrm (int modrm, int size)
        {
            int addrVal;
            string addrStr;
            string regName;
            int saveAddr = InstructionAddress;

            if (size == 1)
            {
                var decoder = modrmTableByte[((modrm & 0xC0) >> 3) | (modrm & 7)];
                addrVal = decoder.Decode(this);
                InstructionAddress = saveAddr;
                addrStr = decoder.Print(this);
                regName = RegNamesByte[modrm & 0x38];
            }
            else if (size == 2)
            {
                var decoder = modrmTableWord[((modrm & 0xC0) >> 3) | (modrm & 7)];
                addrVal = decoder.Decode(this);
                InstructionAddress = saveAddr;
                addrStr = decoder.Print(this);
                regName = RegNamesWord[modrm & 0x38];
            }
            else
                throw new System.ArgumentException();

            return (addrVal, addrStr, regName);
        }

        // --------------------------------------------------------------------
        // print instruction with modrm addressing

        private string PrintModrm (string name, int size)
        {
            var modrm = GetInstructionByte();

            // by default, we print 'XXX Eb,Gb' (or Ew,Gw), a negative size
            // parameter swaps the order to 'XXX Gb,Eb' (or Gw,Ew)
            bool swap = false;
            if (size < 0)
            {
                size = -size;
                swap = true;
            }

            var (addrVal, addrStr, regStr) = DecodeAndPrintModrm(modrm, size);
            var (op1, op2) = swap ? (regStr, addrStr) : (addrStr, regStr);
            return PrintMemoryRef($"{name,-8}{op1},{op2}", addrVal, size);
        }

        // --------------------------------------------------------------------
        // print instruction with modrm addressing but without register

        private string PrintModrmNoReg (string name, int size, int modrm)
        {
            var (addrVal, addrStr, regStr) = DecodeAndPrintModrm(modrm, size);
            if (addrVal < (int) Cpu.Reg.AX)
                addrStr = $"{((size == 1) ? "BYTE" : "WORD")} PTR {addrStr}";
            return PrintMemoryRef($"{name,-8}{addrStr}", addrVal, size);
        }

        // --------------------------------------------------------------------
        // print instruction with modrm addressing and segment register

        private string PrintModrmSegReg (string name, bool swap)
        {
            var modrm = GetInstructionByte();
            var (addrVal, addrStr, _) = DecodeAndPrintModrm(modrm, 2);
            var regStr = RegNamesSeg[modrm & 0x38];
            var (op1, op2) = swap ? (regStr, addrStr) : (addrStr, regStr);
            return PrintMemoryRef($"{name,-8}{op1},{op2}", addrVal, 2);
        }

        // --------------------------------------------------------------------
        // print instruction with modrm addressing and immediate

        private string PrintModrmImm (string name, int size, int modrm)
        {
            var (addrVal, addrStr, _) = DecodeAndPrintModrm(modrm,
                                            /* convert 3 to 2 */ size / 2 + 1);
            if (addrVal < (int) Cpu.Reg.AX)
                addrStr = $"{((size == 1) ? "BYTE" : "WORD")} PTR {addrStr}";
            string text = $"{name,-8}{addrStr},";
            if (size == 3)
            {
                var imm = (sbyte) GetInstructionByte();
                if (imm < 0)
                    text += $"-{-imm:X2}";
                else
                    text += $"+{imm:X2}";
                size = 2;
            }
            else text += size switch
            {
                1 => $"{GetInstructionByte():X2}",
                2 => $"{GetInstructionWord():X4}",
                _ => throw new System.ArgumentException(),
            };
            return PrintMemoryRef(text, addrVal, size);
        }

        // --------------------------------------------------------------------
        // print shift instruction with modrm addressing

        private string PrintModrmShift (int size, string count)
        {
            var modrm = GetInstructionByte();
            var (addrVal, addrStr, regStr) = DecodeAndPrintModrm(modrm, size);
            if (addrVal < (int) Cpu.Reg.AX)
                addrStr = $"{((size == 1) ? "BYTE" : "WORD")} PTR {addrStr}";
            var name = (modrm & 0x38) switch
            {
                0x00 => "ROL",
                0x08 => "ROR",
                0x10 => "RCL",
                0x18 => "RCR",
                0x20 => "SHL",
                0x28 => "SHR",
                0x38 => "SAR",
                _    => "???",
            };
            return PrintMemoryRef($"{name,-8}{addrStr},{count}", addrVal, size);
        }

        // --------------------------------------------------------------------
        // print instruction with register and immediate value

        private string PrintRegImm (string name, Reg reg, int size)
        {
            var (op1, op2) = size switch
            {
                1 => (RegNamesByte[(int) reg - (int) Reg.AX],
                      $"{GetInstructionByte():X2}"),
                2 => (RegNamesWord[(int) reg - (int) Reg.AX],
                    $"{GetInstructionWord():X4}"),
                _ => ("???", "???"),
            };
            return $"{name,-8}{op1},{op2}";
        }

        // --------------------------------------------------------------------
        // print instruction with just an immediate value

        private string PrintImm (string name, int size)
        {
            var op = size switch
            {
                1 => $"{GetInstructionByte():X2}",
                2 => $"{GetInstructionWord():X4}",
                _ => "???",
            };
            return $"{name,-8}{op}";
        }

        // --------------------------------------------------------------------
        // print instruction with offset value

        private string PrintRegOfs (string name, Reg reg, int size)
        {
            // by default, the register operand is printed first
            bool swap = false;
            if (size < 0)
            {
                size = -size;
                swap = true;
            }

            var op1 = size switch
            {
                1 => RegNamesByte[(int) reg - (int) Reg.AX],
                2 => RegNamesWord[(int) reg - (int) Reg.AX],
                _ => "???",
            };

            var addr = GetInstructionWord();
            var op2 = $"[{addr:X4}]";
            if (segmentOverrideForPrint == 0)
                segmentOverrideForPrint = (int) Reg.DS;
            else
            {
                var segReg = segmentOverrideForPrint - (int) Reg.ES;
                op2 = $"{RegNamesSeg[segReg]}:{op2}";
            }

            if (swap)
                (op1, op2) = (op2, op1);

            addr += modrmSegmentAddress;
            return PrintMemoryRef($"{name,-8}{op1},{op2}", addr, size);
        }

        // --------------------------------------------------------------------
        // print instruction without a second operand

        private string PrintReg (string name, Reg reg, int size)
        {
            var op = size switch
            {
                1 => RegNamesByte[(int) reg - (int) Reg.AX],
                2 => RegNamesWord[(int) reg - (int) Reg.AX],
                _ => "???",
            };
            return $"{name,-8}{op}";
        }

        // --------------------------------------------------------------------
        // print contents of referenced memory

        private string PrintMemoryRef (string text, int addr, int size)
        {
            if (segmentOverrideForPrint != 0 && printingCurrentInstruction)
            {
                var contents = size switch
                {
                    1 => $"{stateBytes[addr]:X2}",
                    2 => $"{stateBytes[addr] | (stateBytes[addr + 1] << 8):X4}",
                    _ => "???",
                };
                //addr = (addr - (GetWord(segmentOverrideForPrint) << 4)) & 0xFFFF;
                addr = (addr - modrmSegmentAddress) & 0xFFFF;
                var segReg = segmentOverrideForPrint - (int) Reg.ES;
                text = $"{text,-41}{RegNamesSeg[segReg]}:{addr:X4}={contents}";
            }
            return text;
        }

        // --------------------------------------------------------------------
        // print jump instruction

        private string PrintJump (string name, bool? taken, int size = 1)
        {
            if (size == 8)
            {
                var ofs = GetInstructionWord();
                return $"{name,-8}{GetInstructionWord():X4}:{ofs:X4}";
            }

            var cs = stateBytes[(int) Reg.CS]
                   | (stateBytes[(int) Reg.CS + 1] << 8);
            var target = ((size == 1 ? (sbyte) GetInstructionByte()
                                    : (short) GetInstructionWord())
                       + InstructionAddress - (cs << 4)) & 0xFFFF;
            var text = $"{name,-8}{target:X4}";
            if (printingCurrentInstruction && taken is not null)
                text += $" [BR={(taken.Value ? 1 : 0)}]";
            return text;
        }

        // --------------------------------------------------------------------

        private int segmentOverrideForPrint;
        private bool printingCurrentInstruction;

        private static readonly System.Collections.Generic.Dictionary<int, string>
            RegNamesByte = new System.Collections.Generic.Dictionary<int, string> {
            { 0x00, "AL" }, { 0x08, "CL" }, { 0x10, "DL" }, { 0x18, "BL" },
            { 0x20, "AH" }, { 0x28, "CH" }, { 0x30, "DH" }, { 0x38, "BH" },
        };

        private static readonly System.Collections.Generic.Dictionary<int, string>
            RegNamesWord = new System.Collections.Generic.Dictionary<int, string> {
            { 0x00, "AX" }, { 0x08, "CX" }, { 0x10, "DX" }, { 0x18, "BX" },
            { 0x20, "SP" }, { 0x28, "BP" }, { 0x30, "SI" }, { 0x38, "DI" },
        };

        private static readonly System.Collections.Generic.Dictionary<int, string>
            RegNamesSeg = new System.Collections.Generic.Dictionary<int, string> {
            { 0x00, "ES" }, { 0x08, "CS" }, { 0x10, "SS" }, { 0x18, "DS" },
        };

        public bool CarryFlag => carryFlag < 0;
        public bool AdjustFlag => adjustFlag < 0;
        public bool SignFlag => signFlag < 0;
        public bool OverflowFlag => overflowFlag < 0;
        public bool ZeroFlag => zeroFlag == 0;
        public bool ParityFlag => parityTable[parityFlag & 0xFF] == 4;
        public bool DirectionFlag => directionFlag < 0;
        public bool InterruptFlag => interruptFlag < 0;

    }
}

#endif
