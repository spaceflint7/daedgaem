
namespace com.spaceflint.dbg
{
    public interface IDebuggee
    {

        bool IsEqualAddress (int seg1, int ofs1, int seg2, int ofs2);

        int GetDataSegment ();

        int GetCodeSegment ();

        (int, int) GetInstructionAddress ();

        void SetInstructionAddress (int seg, int ofs);

        int GetByte (int seg, int ofs);

        void SetByte (int seg, int ofs, int val);

        bool IsRegister (string name);

        int GetRegister (string name);

        void SetRegister (string name, int value);

        string PrintRegisters ();

        string PrintRegister (string name);

        (string, int) PrintInstruction (int seg, int ofs, bool cur);

        void Step (bool interruptible);

        bool IsCallInstruction ();
    }
}
