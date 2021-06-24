
using System;
using static System.Console;

namespace com.spaceflint.dbg
{
    public class Debugger
    {

        // --------------------------------------------------------------------
        // construct debug object and run its main loop

        public Debugger (IMachine machine)
        {
            debuggee = (IDebuggee) machine;
            parser = new Parser(debuggee);
            PrintCurrentInstruction();
            ReadEvalPrintLoop();
        }

        // --------------------------------------------------------------------
        // debugger main loop

        private void ReadEvalPrintLoop ()
        {
            for (;;)
            {
                Write("-");
                var (cmdVerb, cmd) = Parser.SplitFirstChar(ReadLine(), 'Q');

                bool ok = true;
                switch (cmdVerb)
                {
                    case '\0':
                        break;

                    case 'H':
                        HelpCommand();
                        break;

                    case '?':
                        ok = EvalCommand(cmd);
                        break;

                    case 'D':
                        ok = DumpCommand(cmd);
                        break;

                    case 'E':
                        ok = EnterCommand(cmd);
                        break;

                    case 'G':
                        ok = GoCommand(cmd);
                        break;

                    case 'P':
                        ok = ProceedCommand(cmd);
                        break;

                    case 'T':
                        ok = TraceCommand(cmd);
                        break;

                    case 'Q':
                        ((IMachine) debuggee).Dispose();
                        return;

                    case 'R':
                        ok = RegisterCommand(cmd);
                        break;

                    case 'U':
                        ok = UnasmCommand(cmd);
                        break;

                    default:
                        WriteLine("unknown command");
                        break;
                }

                if (! ok)
                    WriteLine("error in command");
            }
        }

        // --------------------------------------------------------------------
        // register command.  print or modify register

        private void HelpCommand ()
        {
            WriteLine("Q                                            - quit");
            WriteLine("? expr                                       - evaluate");
            WriteLine("D [startAddr] [L byteCount]                  - dump");
            WriteLine("E addr byte1 [byteN...]                      - edit memory");
            WriteLine("G [= startAddr] [stopAddr1] [stopAddrN...]   - run program");
            WriteLine("P [= startAddr] [stepCount]                  - step over");
            WriteLine("R [register [= value]]                       - print/set register");
            WriteLine("T [= startAddr] [stepCount]                  - step into");
            WriteLine("U [startAddr] [L instructionCount]           - disassemble");
        }

        // --------------------------------------------------------------------
        // register command.  print or modify register

        private bool EvalCommand (string cmd)
        {
            var (value, rest) = parser.ParseValue(cmd);
            if (rest == null || rest.Length != 0)
                return false;
            WriteLine($"{cmd} = hex {value:X} dec {value}");
            return true;
        }

        // --------------------------------------------------------------------
        // register command.  print or modify register

        private bool RegisterCommand (string cmd)
        {
            if (cmd.Length == 0)
            {
                PrintCurrentInstruction();
            }
            else
            {
                var (regName, regExpr) = Parser.SplitNextWord(cmd);
                if (! debuggee.IsRegister(regName))
                    return false;

                else if (regExpr.Length == 0)
                    WriteLine(debuggee.PrintRegister(regName));

                else
                {
                    if (regExpr[0] == '=')
                        regExpr = regExpr.Substring(1).TrimStart();

                    var (regValue, rest) = parser.ParseValue(regExpr);
                    if (rest == null || rest.Length != 0)
                        return false;

                    debuggee.SetRegister(regName, regValue);
                }
            }
            return true;
        }

        // --------------------------------------------------------------------
        // enter command.  write bytes into memory

        private bool EnterCommand (string cmd)
        {
            int seg, ofs, val;
            (seg, ofs, cmd) = parser.ParseAddress(
                                        cmd, debuggee.GetDataSegment());
            if (cmd == null)
                return false;
            for (;;)
            {
                (val, cmd) = parser.ParseValue(cmd);
                if (cmd == null)
                    return false;
                debuggee.SetByte(seg, ofs++, val);
                if (cmd.Length == 0)
                    return true;
            }
        }

        // --------------------------------------------------------------------
        // go command.  run program

        private bool GoCommand (string cmd)
        {
            int startSeg, startOfs;
            (startSeg, startOfs, cmd) = parser.ParseStartAddress(
                                            cmd, debuggee.GetCodeSegment());
            if (cmd == null)
                return false;

            var stopSegs = new int[100];
            var stopOfs = new int[100];
            int stopCount = 0;

            while (cmd.Length > 0)
            {
                int i = stopCount++;
                (stopSegs[i], stopOfs[i], cmd) = parser.ParseAddress(
                                            cmd, debuggee.GetCodeSegment());
                if (cmd == null)
                    return false;
            }

            if (startSeg != -1 && startOfs != -1)
                debuggee.SetInstructionAddress(startSeg, startOfs);

            if (stopCount == 0)
            {
                var machine = (IMachine) debuggee;
                machine.Run();
                var mips = machine.LastRunCount / machine.LastRunTime / 1000000.0;
                WriteLine($"{machine.LastRunCount} instructions executed in {machine.LastRunTime} seconds, MIPS = {mips}");
                PrintCurrentInstruction();
                return true;
            }

            for (;;)
            {
                debuggee.Step(true);
                var (seg, ofs) = debuggee.GetInstructionAddress();
                for (int i = 0; i < stopCount; i++)
                {
                    if (debuggee.IsEqualAddress(seg, ofs, stopSegs[i], stopOfs[i]))
                    {
                        PrintCurrentInstruction();
                        return true;
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // proceed command.  step over instructions

        private bool ProceedCommand (string cmd)
        {
            int startSeg, startOfs;
            (startSeg, startOfs, cmd) = parser.ParseStartAddress(
                                            cmd, debuggee.GetCodeSegment());
            if (cmd == null)
                return false;

            int count;
            if (cmd.Length > 0)
            {
                (count, cmd) = parser.ParseValue(cmd);
                if (cmd == null || count < 0)
                    return false;
            }
            else
                count = 1;

            if (startSeg != -1 && startOfs != -1)
                debuggee.SetInstructionAddress(startSeg, startOfs);
            else
                (startSeg, startOfs) = debuggee.GetInstructionAddress();

            int nextSeg, nextOfs;

            for (;;)
            {
                bool notCallInst = ! debuggee.IsCallInstruction();

                for (;;)
                {
                    var (_, instLen) =
                            debuggee.PrintInstruction(startSeg, startOfs, false);

                    debuggee.Step(false);
                    (nextSeg, nextOfs) = debuggee.GetInstructionAddress();

                    if (notCallInst || debuggee.IsEqualAddress(
                                                startSeg, startOfs + instLen,
                                                nextSeg, nextOfs))
                        break;
                }

                WriteLine(debuggee.PrintRegisters());
                PrintInstruction(nextSeg, nextOfs, true);

                if (--count == 0)
                {
                    (lastCodeSeg, lastCodeOfs) = (nextSeg, nextOfs);
                    return true;
                }

                startSeg = nextSeg;
                startOfs = nextOfs;
            }
        }

        // --------------------------------------------------------------------
        // trace command.  step into instructions

        private bool TraceCommand (string cmd)
        {
            int startSeg, startOfs;
            (startSeg, startOfs, cmd) = parser.ParseStartAddress(
                                            cmd, debuggee.GetCodeSegment());
            if (cmd == null)
                return false;

            int count;
            if (cmd.Length > 0)
            {
                (count, cmd) = parser.ParseValue(cmd);
                if (cmd == null || count < 0)
                    return false;
            }
            else
                count = 1;

            if (startSeg != -1 && startOfs != -1)
                debuggee.SetInstructionAddress(startSeg, startOfs);

            for (;;)
            {
                debuggee.Step(false);
                PrintCurrentInstruction();
                if (--count == 0)
                    return true;
            }
        }

        // --------------------------------------------------------------------
        // unassemble command.  display disassembly

        private bool UnasmCommand (string cmd)
        {
            int seg, ofs, len;
            if (cmd.Length == 0)
            {
                (seg, ofs) = (lastCodeSeg, lastCodeOfs);
                len = 0x20;
            }
            else
            {
                (seg, ofs, cmd) = parser.ParseAddress(
                                        cmd, debuggee.GetCodeSegment());
                (len, cmd) = parser.ParseLength(cmd);
                if (cmd == null)
                    return false;
                if (len <= 0)
                    len = 0x20;
            }

            while (len > 0)
            {
                int instLen = PrintInstruction(seg, ofs, false);
                ofs += instLen;
                len -= instLen;
                if (ofs > 0xFFFF)
                {
                    ofs &= 0xFFFF;
                    break;
                }
            }

            (lastCodeSeg, lastCodeOfs) = (seg, ofs);
            return true;
        }

        // --------------------------------------------------------------------
        // dump command.  display memory

        private bool DumpCommand (string cmd)
        {
            int seg, ofs, len;
            if (cmd.Length == 0)
            {
                (seg, ofs) = (lastDataSeg, lastDataOfs);
                len = 0x80;
            }
            else
            {
                (seg, ofs, cmd) = parser.ParseAddress(
                                        cmd, debuggee.GetDataSegment());
                (len, cmd) = parser.ParseLength(cmd);
                if (cmd == null)
                    return false;
                if (len <= 0)
                    len = 0x80;
            }

            int startOfs = ofs;
            int endOfs = ofs + len;
            ofs &= 0xFFF0;

            while (len > 0)
            {
                var str = $"{seg:X4}:{(ofs & 0xFFF0):X4}  ";
                var mem = "";
                for (int i = 0; i < 16; i++)
                {
                    if (ofs < startOfs || ofs >= endOfs)
                    {
                        str += "  ";
                        mem += " ";
                    }
                    else
                    {
                        var b = debuggee.GetByte(seg, ofs);
                        str += $"{b:X2}";
                        if (b >= 32 && b <= 127)
                            mem += ((char) b).ToString();
                        else
                            mem += ".";
                        len--;
                    }
                    if (i == 7)
                        str += "-";
                    else
                        str += " ";
                    ofs++;
                }
                WriteLine(str + mem);
                if (ofs > 0xFFFF)
                {
                    endOfs = 0;
                    break;
                }
            }

            (lastDataSeg, lastDataOfs) = (seg, endOfs);
            return true;
        }

        // --------------------------------------------------------------------
        // print instruction

        private int PrintInstruction (int seg, int ofs, bool isCurrent)
        {
            var (instStr, instLen) =
                            debuggee.PrintInstruction(seg, ofs, isCurrent);

            string bytesStr = "";
            for (int i = 0; i < instLen; i++)
                bytesStr += $"{debuggee.GetByte(seg, ofs + i):X2}";
            while (bytesStr.Length < 18)
                bytesStr += " ";

            WriteLine($"{seg:X4}:{ofs:X4} {bytesStr}{instStr}");
            return instLen;
        }

        // --------------------------------------------------------------------
        // print current instruction

        private void PrintCurrentInstruction ()
        {
            WriteLine(debuggee.PrintRegisters());
            (lastCodeSeg, lastCodeOfs) = debuggee.GetInstructionAddress();
            PrintInstruction(lastCodeSeg, lastCodeOfs, true);
        }

        // --------------------------------------------------------------------

        private IDebuggee debuggee;
        private Parser parser;
        private int lastCodeSeg = -1, lastCodeOfs = -1;
        private int lastDataSeg = -1, lastDataOfs = -1;

    }
}
