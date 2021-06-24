
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // modrm decoder objects shared between 16-bit and 8-bit tables

        [java.attr.RetainType] private static ModrmDecoder modrm_BX_SI    = new Modrm_BX_SI();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX_DI    = new Modrm_BX_DI();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_SI    = new Modrm_BP_SI();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_DI    = new Modrm_BP_DI();
        [java.attr.RetainType] private static ModrmDecoder modrm_SI       = new Modrm_SI();
        [java.attr.RetainType] private static ModrmDecoder modrm_DI       = new Modrm_DI();
        [java.attr.RetainType] private static ModrmDecoder modrm_16       = new Modrm_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX       = new Modrm_BX();

        [java.attr.RetainType] private static ModrmDecoder modrm_BX_SI_8  = new Modrm_BX_SI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX_DI_8  = new Modrm_BX_DI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_SI_8  = new Modrm_BP_SI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_DI_8  = new Modrm_BP_DI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_SI_8     = new Modrm_SI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_DI_8     = new Modrm_DI_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_8     = new Modrm_BP_8();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX_8     = new Modrm_BX_8();

        [java.attr.RetainType] private static ModrmDecoder modrm_BX_SI_16 = new Modrm_BX_SI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX_DI_16 = new Modrm_BX_DI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_SI_16 = new Modrm_BP_SI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_DI_16 = new Modrm_BP_DI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_SI_16    = new Modrm_SI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_DI_16    = new Modrm_DI_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BP_16    = new Modrm_BP_16();
        [java.attr.RetainType] private static ModrmDecoder modrm_BX_16    = new Modrm_BX_16();

        [java.attr.RetainType] private static ModrmDecoder modrm_reg_AX   = new Modrm_Reg_AX();
        [java.attr.RetainType] private static ModrmDecoder modrm_reg_CX   = new Modrm_Reg_CX();
        [java.attr.RetainType] private static ModrmDecoder modrm_reg_DX   = new Modrm_Reg_DX();
        [java.attr.RetainType] private static ModrmDecoder modrm_reg_BX   = new Modrm_Reg_BX();

        // --------------------------------------------------------------------
        // modrm decoder tables for 16-bit and 8-bit instructions

        // call using
        // modrmTableXxxx[((modrm & 0xC0) >> 3) | (modrm & 7)].Decode(cpu);

        [java.attr.RetainType] private static ModrmDecoder[] modrmTableWord = new ModrmDecoder[]
        {  /* mod|r/m */
            /* 00|000 */ modrm_BX_SI,               // modrm=00
            /* 00|001 */ modrm_BX_DI,
            /* 00|010 */ modrm_BP_SI,
            /* 00|011 */ modrm_BP_DI,
            /* 00|100 */ modrm_SI,
            /* 00|101 */ modrm_DI,
            /* 00|110 */ modrm_16,
            /* 00|111 */ modrm_BX,
            /* 01|000 */ modrm_BX_SI_8,             // modrm=01
            /* 01|001 */ modrm_BX_DI_8,
            /* 01|010 */ modrm_BP_SI_8,
            /* 01|011 */ modrm_BP_DI_8,
            /* 01|100 */ modrm_SI_8,
            /* 01|101 */ modrm_DI_8,
            /* 01|110 */ modrm_BP_8,
            /* 01|111 */ modrm_BX_8,
            /* 10|000 */ modrm_BX_SI_16,            // modrm=10
            /* 10|001 */ modrm_BX_DI_16,
            /* 10|010 */ modrm_BP_SI_16,
            /* 10|011 */ modrm_BP_DI_16,
            /* 10|100 */ modrm_SI_16,
            /* 10|101 */ modrm_DI_16,
            /* 10|110 */ modrm_BP_16,
            /* 10|111 */ modrm_BX_16,
            /* 11|000 */ modrm_reg_AX,              // modrm=11 for 16-bit register
            /* 11|001 */ modrm_reg_CX,
            /* 11|002 */ modrm_reg_DX,
            /* 11|003 */ modrm_reg_BX,
            /* 11|004 */ new Modrm_Reg_SP(),
            /* 11|005 */ new Modrm_Reg_BP(),
            /* 11|006 */ new Modrm_Reg_SI(),
            /* 11|007 */ new Modrm_Reg_DI(),
        };

        [java.attr.RetainType] private static ModrmDecoder[] modrmTableByte = new ModrmDecoder[]
        {  /* mod|r/m */
            /* 00|000 */ modrm_BX_SI,               // modrm=00
            /* 00|001 */ modrm_BX_DI,
            /* 00|010 */ modrm_BP_SI,
            /* 00|011 */ modrm_BP_DI,
            /* 00|100 */ modrm_SI,
            /* 00|101 */ modrm_DI,
            /* 00|110 */ modrm_16,
            /* 00|111 */ modrm_BX,
            /* 01|000 */ modrm_BX_SI_8,             // modrm=01
            /* 01|001 */ modrm_BX_DI_8,
            /* 01|010 */ modrm_BP_SI_8,
            /* 01|011 */ modrm_BP_DI_8,
            /* 01|100 */ modrm_SI_8,
            /* 01|101 */ modrm_DI_8,
            /* 01|110 */ modrm_BP_8,
            /* 01|111 */ modrm_BX_8,
            /* 10|000 */ modrm_BX_SI_16,            // modrm=10
            /* 10|001 */ modrm_BX_DI_16,
            /* 10|010 */ modrm_BP_SI_16,
            /* 10|011 */ modrm_BP_DI_16,
            /* 10|100 */ modrm_SI_16,
            /* 10|101 */ modrm_DI_16,
            /* 10|110 */ modrm_BP_16,
            /* 10|111 */ modrm_BX_16,

            // for modrm=11 for 8-bit register, decoding for AX and AL is
            // the same because they share the same offset in memory, so
            // can share the decoding object.  except when compiling with
            // debugger support and a Print() method in the ModrmDecoder
            // interface, then there is a difference between AX and AL.

            #if DEBUGGER
            /* 11|000 */ new Modrm_Reg_AL(),        // modrm=11 for 8-bit register
            /* 11|001 */ new Modrm_Reg_CL(),
            /* 11|002 */ new Modrm_Reg_DL(),
            /* 11|003 */ new Modrm_Reg_BL(),
            #else
            /* 11|000 */ modrm_reg_AX,              // modrm=11 for 8-bit register
            /* 11|001 */ modrm_reg_CX,
            /* 11|002 */ modrm_reg_DX,
            /* 11|003 */ modrm_reg_BX,
            #endif
            /* 11|004 */ new Modrm_Reg_AH(),
            /* 11|005 */ new Modrm_Reg_CH(),
            /* 11|006 */ new Modrm_Reg_DH(),
            /* 11|007 */ new Modrm_Reg_BH(),
        };

        // --------------------------------------------------------------------
        // abstract class with a single method to decode a modrm sequence

        private abstract class ModrmDecoder
        {
            public abstract int Decode (Cpu cpu);

            #if DEBUGGER
            public abstract string Print (Cpu cpu);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|000 = [BX+SI]

        private sealed class Modrm_BX_SI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+SI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|001 = [BX+DI]

        private sealed class Modrm_BX_DI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+DI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|010 = [BP+SI]

        private sealed class Modrm_BP_SI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+SI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|011 = [BP+DI]

        private sealed class Modrm_BP_DI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+DI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|100 = [SI]

        private sealed class Modrm_SI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "SI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|101 = [DI]

        private sealed class Modrm_DI : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "DI");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|110 = [disp16]

        private sealed class Modrm_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                return   cpu.dataSegmentAddress
                       + cpu.GetInstructionWord();
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, $"{cpu.GetInstructionWord():X4}");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 00|xxx|111 = [BX]

        private sealed class Modrm_BX : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX");
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|000 = [BX+SI+disp8]

        private sealed class Modrm_BX_SI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+SI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|001 = [BX+DI+disp8]

        private sealed class Modrm_BX_DI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+DI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|010 = [BP+SI+disp8]

        private sealed class Modrm_BP_SI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+SI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|011 = [BP+DI+disp8]

        private sealed class Modrm_BP_DI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+DI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|100 = [SI+disp8]

        private sealed class Modrm_SI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "SI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|101 = [DI+disp8]

        private sealed class Modrm_DI_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "DI", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|110 = [BP+disp8]

        private sealed class Modrm_BP_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|111 = [BX+disp8]

        private sealed class Modrm_BX_8 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (int) (sbyte) cpu.GetInstructionByte()));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX", 8);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|000 = [BX+SI+disp16]

        private sealed class Modrm_BX_SI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+SI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|001 = [BX+DI+disp16]

        private sealed class Modrm_BX_DI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX+DI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|010 = [BP+SI+disp16]

        private sealed class Modrm_BP_SI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+SI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|011 = [BP+DI+disp16]

        private sealed class Modrm_BP_DI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP+DI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|100 = [SI+disp16]

        private sealed class Modrm_SI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.SI] | (mem[(int) Reg.SI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "SI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|101 = [DI+disp16]

        private sealed class Modrm_DI_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.DI] | (mem[(int) Reg.DI + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "DI", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 10|xxx|110 = [BP+disp16]

        private sealed class Modrm_BP_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                #if DEBUGGER
                cpu.modrmSegmentAddress = cpu.stackSegmentAddressForModrm;
                #endif
                var mem = cpu.stateBytes;
                return   cpu.stackSegmentAddressForModrm + (0xFFFF & (
                       + (mem[(int) Reg.BP] | (mem[(int) Reg.BP + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.SS, "BP", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 01|xxx|111 = [BX+disp16]

        private sealed class Modrm_BX_16 : ModrmDecoder
        {
            public override int Decode (Cpu cpu)
            {
                var mem = cpu.stateBytes;
                return   cpu.dataSegmentAddress + (0xFFFF & (
                       + (mem[(int) Reg.BX] | (mem[(int) Reg.BX + 1] << 8))
                       + ((short) cpu.GetInstructionWord())));
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) =>
                cpu.PrintSegmentPrefix(Reg.DS, "BX", 16);
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|000 = AX or AL

        private sealed class Modrm_Reg_AX : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.AX;

        #if DEBUGGER

            public override string Print (Cpu cpu) => "AX";
        }

        private sealed class Modrm_Reg_AL : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.AX;

            public override string Print (Cpu cpu) => "AL";

        #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|001 = CX or CL

        private sealed class Modrm_Reg_CX : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.CX;

        #if DEBUGGER

            public override string Print (Cpu cpu) => "CX";
        }

        private sealed class Modrm_Reg_CL : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.CX;

            public override string Print (Cpu cpu) => "CL";

        #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|010 = DX or DL

        private sealed class Modrm_Reg_DX : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.DX;

        #if DEBUGGER

            public override string Print (Cpu cpu) => "DX";
        }

        private sealed class Modrm_Reg_DL : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.DX;

            public override string Print (Cpu cpu) => "DL";

        #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|011 = BX or BL

        private sealed class Modrm_Reg_BX : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.BX;

        #if DEBUGGER

            public override string Print (Cpu cpu) => "BX";
        }

        private sealed class Modrm_Reg_BL : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.BX;

            public override string Print (Cpu cpu) => "BL";

        #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|100 = SP or AH

        private sealed class Modrm_Reg_SP : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.SP;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "SP";
            #endif
        }

        private sealed class Modrm_Reg_AH : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.AX + 1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "AH";
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|101 = BP or CH

        private sealed class Modrm_Reg_BP : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.BP;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "BP";
            #endif
        }

        private sealed class Modrm_Reg_CH : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.CX + 1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "CH";
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|110 = SI or DH

        private sealed class Modrm_Reg_SI : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.SI;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "SI";
            #endif
        }

        private sealed class Modrm_Reg_DH : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.DX + 1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "DH";
            #endif
        }

        // --------------------------------------------------------------------
        // modrm byte = 11|xxx|111 = DI or BH

        private sealed class Modrm_Reg_DI : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.DI;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "DI";
            #endif
        }

        private sealed class Modrm_Reg_BH : ModrmDecoder
        {
            public override int Decode (Cpu cpu) => (int) Reg.BX + 1;

            #if DEBUGGER
            public override string Print (Cpu cpu) => "BH";
            #endif
        }

        // --------------------------------------------------------------------
        // check that word operand does not cross segment boundary

        #if DEBUGGER

        private void ThrowIfWrapAroundOffset (int ofs, int len)
        {
            if (ofs + len > 0x10000)
            {
                throw new System.InvalidProgramException(
                    $"Wrap-around in operand near {InstructionAddress:X5}");
            }
        }

        private void ThrowIfWrapAroundModrm (int addr, int len = 2)
        {
            // check wrap-around only if address does not reference a register
            if (addr < (int) Cpu.Reg.AX)
                ThrowIfWrapAroundOffset(addr - modrmSegmentAddress, len);
        }

        #endif
    }
}

