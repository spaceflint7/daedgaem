
#if DEBUGGER

using com.spaceflint.dbg;

namespace com.spaceflint.x86
{
    public partial class Machine : IDebuggee
    {

        // --------------------------------------------------------------------
        // compare addresses

        bool IDebuggee.IsEqualAddress (int seg1, int ofs1, int seg2, int ofs2) =>

            ((seg1 << 4) + (ofs1 & 0xFFFF)) == ((seg2 << 4) + (ofs2 & 0xFFFF));

        // --------------------------------------------------------------------
        // get default data segment

        int IDebuggee.GetDataSegment () => cpu.GetWord((int) Cpu.Reg.DS);

        // --------------------------------------------------------------------
        // get default code segment

        int IDebuggee.GetCodeSegment () => cpu.GetWord((int) Cpu.Reg.CS);

        // --------------------------------------------------------------------
        // get address of next instruction to execute

        (int, int) IDebuggee.GetInstructionAddress ()
        {
            var addr = cpu.InstructionAddress;
            var cs = cpu.GetWord((int) Cpu.Reg.CS);
            var ip = (addr - (cs << 4)) & 0xFFFF;
            return (cs, ip);
        }

        // --------------------------------------------------------------------
        // set address of next instruction to execute

        void IDebuggee.SetInstructionAddress(int seg, int ofs)
        {
            cpu.SetWord((int) Cpu.Reg.CS, seg);
            cpu.InstructionAddress = seg << 4 + ofs;
        }

        // --------------------------------------------------------------------
        // get memory byte

        int IDebuggee.GetByte (int seg, int ofs) =>

            cpu.GetByte(((seg << 4) + ofs) & 0xFFFFF);

        // --------------------------------------------------------------------
        // set memory byte

        void IDebuggee.SetByte (int seg, int ofs, int val) =>

            cpu.SetByte(((seg << 4) + ofs) & 0xFFFFF, (byte) val);

        // --------------------------------------------------------------------
        // check register name

        bool IDebuggee.IsRegister (string name) => RegisterOffset(name) != -1;

        // --------------------------------------------------------------------
        // get register value

        int IDebuggee.GetRegister (string name)
        {
            return RegisterOffset(name) switch
            {
                (int) Cpu.Reg.IP => cpu.InstructionAddress
                                        - (cpu.GetWord((int) Cpu.Reg.CS) << 4),
                (int) Cpu.Reg.FL => cpu.Flags,
                int x => cpu.GetWord(x),
            };
        }

        // --------------------------------------------------------------------
        // set register value

        void IDebuggee.SetRegister (string name, int value)
        {
            switch (RegisterOffset(name))
            {
                case (int) Cpu.Reg.CS:
                    var ipReg = cpu.InstructionAddress
                              - (cpu.GetWord((int) Cpu.Reg.CS) << 4);
                    cpu.InstructionAddress = (value << 4) + ipReg;
                    cpu.SetWord((int) Cpu.Reg.CS, value);
                    break;

                case (int) Cpu.Reg.IP:
                    cpu.InstructionAddress =
                        (value & 0xFFFF) + (cpu.GetWord((int) Cpu.Reg.CS) << 4);
                    break;

                case (int) Cpu.Reg.FL:
                    cpu.Flags = value;
                    break;

                case int x:
                    cpu.SetWord(x, value);
                    break;
            }
        }

        // --------------------------------------------------------------------
        // check register name

        private static int RegisterOffset (string name)
        {
            return name switch
            {
                "AX" => (int) Cpu.Reg.AX,
                "BX" => (int) Cpu.Reg.BX,
                "CX" => (int) Cpu.Reg.CX,
                "DX" => (int) Cpu.Reg.DX,
                "SP" => (int) Cpu.Reg.SP,
                "BP" => (int) Cpu.Reg.BP,
                "SI" => (int) Cpu.Reg.SI,
                "DI" => (int) Cpu.Reg.DI,
                "ES" => (int) Cpu.Reg.ES,
                "CS" => (int) Cpu.Reg.CS,
                "SS" => (int) Cpu.Reg.SS,
                "DS" => (int) Cpu.Reg.DS,
                "IP" => (int) Cpu.Reg.IP,
                "FL" => (int) Cpu.Reg.FL,
                _    => -1
            };
        }

        // --------------------------------------------------------------------
        // print registers

        string IDebuggee.PrintRegisters ()
        {
            var ax = cpu.GetWord((int) Cpu.Reg.AX);
            var bx = cpu.GetWord((int) Cpu.Reg.BX);
            var cx = cpu.GetWord((int) Cpu.Reg.CX);
            var dx = cpu.GetWord((int) Cpu.Reg.DX);
            var sp = cpu.GetWord((int) Cpu.Reg.SP);
            var bp = cpu.GetWord((int) Cpu.Reg.BP);
            var si = cpu.GetWord((int) Cpu.Reg.SI);
            var di = cpu.GetWord((int) Cpu.Reg.DI);
            var ds = cpu.GetWord((int) Cpu.Reg.DS);
            var es = cpu.GetWord((int) Cpu.Reg.ES);
            var ss = cpu.GetWord((int) Cpu.Reg.SS);
            var cs = cpu.GetWord((int) Cpu.Reg.CS);
            var ip = (cpu.InstructionAddress - (cs << 4)) & 0xFFFF;
            var @of = cpu.OverflowFlag  ? "OV" : "NV";
            var @df = cpu.DirectionFlag ? "DN" : "UP";
            var @if = cpu.InterruptFlag ? "EI" : "DI";
            var @sf = cpu.SignFlag      ? "NG" : "PL";
            var @zf = cpu.ZeroFlag      ? "ZR" : "NZ";
            var @af = cpu.AdjustFlag    ? "AC" : "NA";
            var @pf = cpu.ParityFlag    ? "PE" : "PO";
            var @cf = cpu.CarryFlag     ? "CY" : "NC";
            return $"AX={ax:X4} BX={bx:X4} CX={cx:X4} DX={dx:X4} "
                 + $"SP={sp:X4} BP={bp:X4} SI={si:X4} DI={di:X4}\n"
                 + $"DS={ds:X4} ES={es:X4} SS={ss:X4} CS={cs:X4} IP={ip:X4} "
                 + $"{@of} {@df} {@if} {@sf} {@zf} {@af} {@pf} {@cf}";
        }

        // --------------------------------------------------------------------
        // print registers

        string IDebuggee.PrintRegister (string name) =>
            $"{name} = {((IDebuggee) this).GetRegister(name):X4}";

        // --------------------------------------------------------------------
        // print instruction disassembly

        (string, int) IDebuggee.PrintInstruction (int seg, int ofs, bool printData)
        {
            var addr = ((seg << 4) + ofs) & 0xFFFFF;

            var saveCodeSeg = cpu.GetWord((int) Cpu.Reg.CS);
            var saveInstAddr = cpu.InstructionAddress;

            cpu.SetWord((int) Cpu.Reg.CS, seg);
            cpu.CacheSegmentRegisters();
            cpu.InstructionAddress = addr;

            var str = cpu.PrintInstruction(printData);
            int len = cpu.InstructionAddress - addr;

            cpu.SetWord((int) Cpu.Reg.CS, saveCodeSeg);
            cpu.CacheSegmentRegisters();
            cpu.InstructionAddress = saveInstAddr;

            return (str, len);
        }

        // --------------------------------------------------------------------
        // step program

        void IDebuggee.Step (bool interruptible)
        {
            if (interruptible)
                cpu.StepInterruptible();
            else
                cpu.Step();
        }

        // --------------------------------------------------------------------
        // check if next instruction is a call-like instruction

        bool IDebuggee.IsCallInstruction ()
        {
            // skip segment override prefix
            int addr = cpu.InstructionAddress;
            int op;
            do
            {
                op = cpu.GetByte(addr++);
            }
            while (op == 0x26 || op == 0x2E || op == 0x36 || op == 0x3E);

            if (    op == 0x9A || op == 0xE8        /* CALL */
                 || (op >= 0xCC && op <= 0xCE)      /* INT */
                 || (op >= 0xE0 && op <= 0xE2))     /* LOOP */
                return true;

            if (op == 0xFF)
            {
                op = (cpu.GetByte(addr) & 0x38) >> 3;
                if (op == 2 || op == 3)             /* CALL */
                    return true;
            }

            return false;
        }

    }
}

#endif
