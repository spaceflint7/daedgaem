
namespace com.spaceflint.x86
{
    public sealed partial class Cpu
    {

        // --------------------------------------------------------------------
        // instruction opcode table

        [java.attr.RetainType] private static Instruction i_XX_Invalid = new I_XX_Invalid();

        [java.attr.RetainType] private static Instruction[] instTable = new Instruction[]
        {
            new I_00_Add_EbGb(),        // 0x00 - ADD Eb,Gb
            new I_01_Add_EwGw(),        // 0x01 - ADD Ew,Gw
            new I_02_Add_GbEb(),        // 0x02 - ADD Gb,Eb
            new I_03_Add_GwEw(),        // 0x03 - ADD Gw,Ew
            new I_04_Add_AL(),          // 0x04 - ADD AL,Ib
            new I_05_Add_AX(),          // 0x05 - ADD AX,Iw
            new I_06_Push_ES(),         // 0x06 - PUSH ES
            new I_07_Pop_ES(),          // 0x07 - POP ES
            new I_08_Or_EbGb(),         // 0x08 - OR Eb,Gb
            new I_09_Or_EwGw(),         // 0x09 - OR Ew,Gw
            new I_0A_Or_GbEb(),         // 0x0A - OR Gb,Eb
            new I_0B_Or_GwEw(),         // 0x0B - OR Gw,Ew
            new I_0C_Or_AL(),           // 0x0C - OR AL,Ib
            new I_0D_Or_AX(),           // 0x0D - OR AX,Iw
            new I_0E_Push_CS(),         // 0x0E - PUSH CS
            i_XX_Invalid,               // 0x0F - POP CS
            new I_10_Adc_EbGb(),        // 0x10 - ADC Eb,Gb
            new I_11_Adc_EwGw(),        // 0x11 - ADC Ew,Gw
            new I_12_Adc_GbEb(),        // 0x12 - ADC Gb,Eb
            new I_13_Adc_GwEw(),        // 0x13 - ADC Gw,Ew
            new I_14_Adc_AL(),          // 0x14 - ADC AL,Ib
            new I_15_Adc_AX(),          // 0x15 - ADC AX,Iw
            new I_16_Push_SS(),         // 0x16 - PUSH SS
            new I_17_Pop_SS(),          // 0x17 - POP SS
            new I_18_Sbb_EbGb(),        // 0x18 - SBB Eb,Gb
            new I_19_Sbb_EwGw(),        // 0x19 - SBB Ew,Gw
            new I_1A_Sbb_GbEb(),        // 0x1A - SBB Gb,Eb
            new I_1B_Sbb_GwEw(),        // 0x1B - SBB Gw,Ew
            new I_1C_Sbb_AL(),          // 0x1C - SBB AL,Ib
            new I_1D_Sbb_AX(),          // 0x1D - SBB AX,Iw
            new I_1E_Push_DS(),         // 0x1E - PUSH DS
            new I_1F_Pop_DS(),          // 0x1F - POP DS
            new I_20_And_EbGb(),        // 0x20 - AND Eb,Gb
            new I_21_And_EwGw(),        // 0x21 - AND Ew,Gw
            new I_22_And_GbEb(),        // 0x22 - AND Gb,Eb
            new I_23_And_GwEw(),        // 0x23 - AND Gw,Ew
            new I_24_And_AL(),          // 0x24 - AND AL,Ib
            new I_25_And_AX(),          // 0x25 - AND AX,Iw
            new I_26_ES_Prefix(),       // 0x26 - ES prefix
            new I_27_DAA(),             // 0x27 - DAA
            new I_28_Sub_EbGb(),        // 0x28 - SUB Eb,Gb
            new I_29_Sub_EwGw(),        // 0x29 - SUB Ew,Gw
            new I_2A_Sub_GbEb(),        // 0x2A - SUB Gb,Eb
            new I_2B_Sub_GwEw(),        // 0x2B - SUB Gw,Ew
            new I_2C_Sub_AL(),          // 0x2C - SUB AL,Ib
            new I_2D_Sub_AX(),          // 0x2D - SUB AX,Iw
            new I_2E_CS_Prefix(),       // 0x2E - CS prefix
            new I_2F_DAS(),             // 0x27 - DAS
            new I_30_Xor_EbGb(),        // 0x30 - XOR Eb,Gb
            new I_31_Xor_EwGw(),        // 0x31 - XOR Ew,Gw
            new I_32_Xor_GbEb(),        // 0x32 - XOR Gb,Eb
            new I_33_Xor_GwEw(),        // 0x33 - XOR Gw,Ew
            new I_34_Xor_AL(),          // 0x34 - XOR AL,Ib
            new I_35_Xor_AX(),          // 0x35 - XOR AX,Iw
            new I_36_SS_Prefix(),       // 0x36 - SS prefix
            new I_37_AAA(),             // 0x37 - AAA
            new I_38_Cmp_EbGb(),        // 0x38 - CMP Eb,Gb
            new I_39_Cmp_EwGw(),        // 0x39 - CMP Ew,Gw
            new I_3A_Cmp_GbEb(),        // 0x3A - CMP Gb,Eb
            new I_3B_Cmp_GwEw(),        // 0x3B - CMP Gw,Ew
            new I_3C_Cmp_AL(),          // 0x3C - CMP AL,Ib
            new I_3D_Cmp_AX(),          // 0x3D - CMP AX,Iw
            new I_3E_DS_Prefix(),       // 0x3E - DS prefix
            i_XX_Invalid,
            new I_40_Inc_AX(),          // 0x40 - INC AX
            new I_41_Inc_CX(),          // 0x41 - INC CX
            new I_42_Inc_DX(),          // 0x42 - INC DX
            new I_43_Inc_BX(),          // 0x43 - INC BX
            new I_44_Inc_SP(),          // 0x44 - INC SP
            new I_45_Inc_BP(),          // 0x45 - INC BP
            new I_46_Inc_SI(),          // 0x46 - INC SI
            new I_47_Inc_DI(),          // 0x47 - INC DI
            new I_48_Dec_AX(),          // 0x48 - DEC AX
            new I_49_Dec_CX(),          // 0x49 - DEC CX
            new I_4A_Dec_DX(),          // 0x4A - DEC DX
            new I_4B_Dec_BX(),          // 0x4B - DEC BX
            new I_4C_Dec_SP(),          // 0x4C - DEC SP
            new I_4D_Dec_BP(),          // 0x4D - DEC BP
            new I_4E_Dec_SI(),          // 0x4E - DEC SI
            new I_4F_Dec_DI(),          // 0x4F - DEC DI
            new I_50_Push_AX(),         // 0x50 - PUSH AX
            new I_51_Push_CX(),         // 0x51 - PUSH CX
            new I_52_Push_DX(),         // 0x52 - PUSH DX
            new I_53_Push_BX(),         // 0x53 - PUSH BX
            new I_54_Push_SP(),         // 0x54 - PUSH SP
            new I_55_Push_BP(),         // 0x55 - PUSH BP
            new I_56_Push_SI(),         // 0x56 - PUSH SI
            new I_57_Push_DI(),         // 0x57 - PUSH DI
            new I_58_Pop_AX(),          // 0x58 - POP AX
            new I_59_Pop_CX(),          // 0x59 - POP CX
            new I_5A_Pop_DX(),          // 0x5A - POP DX
            new I_5B_Pop_BX(),          // 0x5B - POP BX
            new I_5C_Pop_SP(),          // 0x5C - POP SP
            new I_5D_Pop_BP(),          // 0x5D - POP BP
            new I_5E_Pop_SI(),          // 0x5E - POP SI
            new I_5F_Pop_DI(),          // 0x5F - POP DI
            i_XX_Invalid,               // 0x60
            i_XX_Invalid,               // 0x61
            i_XX_Invalid,               // 0x62
            i_XX_Invalid,               // 0x63
            i_XX_Invalid,               // 0x64
            i_XX_Invalid,               // 0x65
            i_XX_Invalid,               // 0x66
            i_XX_Invalid,               // 0x67
            i_XX_Invalid,               // 0x68
            i_XX_Invalid,               // 0x69
            i_XX_Invalid,               // 0x6A
            i_XX_Invalid,               // 0x6B
            i_XX_Invalid,               // 0x6C
            i_XX_Invalid,               // 0x6D
            i_XX_Invalid,               // 0x6E
            i_XX_Invalid,               // 0x6F
            new I_70_JumpO(),           // 0x70 - JO  (OF=1)
            new I_71_JumpNO(),          // 0x71 - JNO (OF=0)
            new I_72_JumpC(),           // 0x72 - JC  (CF=1) (also JB, JNAE)
            new I_73_JumpNC(),          // 0x73 - JNC (CF=0) (also JAE, JNB)
            new I_74_JumpZ(),           // 0x74 - JZ  (ZF=1) (also JE)
            new I_75_JumpNZ(),          // 0x75 - JNZ (ZF=0) (also JNE)
            new I_76_JumpBE(),          // 0x76 - JBE (CF=1 or ZF=1) (also JNA)
            new I_77_JumpA(),           // 0x77 - JA  (CF=0 and ZF=0) (also JNBE)
            new I_78_JumpS(),           // 0x78 - JS  (SF=1)
            new I_79_JumpNS(),          // 0x79 - JNS (SF=0)
            new I_7A_JumpPE(),          // 0x7A - JPE (PF=1) (also JP)
            new I_7B_JumpPO(),          // 0x7B - JPO (PF=0) (also JNP)
            new I_7C_JumpL(),           // 0x7C - JL  (SF!=OF) (also JNGE)
            new I_7D_JumpGE(),          // 0x7D - JGE (SF=OF)  (also JNL)
            new I_7E_JumpLE(),          // 0x7E - JLE (ZF=1 or SF!=OF) (also JNG)
            new I_7F_JumpG(),           // 0x7F - JG  (ZF=0 and SF=OF) (also JNLE)
            new I_80_Arith_EbIb(),      // 0x80 - arith Eb,Ib
            new I_81_Arith_EwIw(),      // 0x81 - arith Ew,Iw
            new I_80_Arith_EbIb(),      // 0x82 - arith Eb,Ib - same as 0x80
            new I_83_Arith_EwIb(),      // 0x83 - arith Ew,Ib
            new I_84_Test_GbEb(),       // 0x84 - TEST Gb,Eb
            new I_85_Test_GwEw(),       // 0x85 - TEST Gw,Ew
            new I_86_Xchg_GbEb(),       // 0x86 - XCHG Gb,Eb
            new I_87_Xchg_GwEw(),       // 0x87 - XCHG Gw,Ew
            new I_88_Mov_EbGb(),        // 0x88 - MOV Eb,Gb
            new I_89_Mov_EwGw(),        // 0x89 - MOV Ew,Gw
            new I_8A_Mov_GbEb(),        // 0x8A - MOV Gb,Eb
            new I_8B_Mov_GwEw(),        // 0x8B - MOV Gw,Ew
            new I_8C_Mov_EwSw(),        // 0x8C - MOV Ew,Sw
            new I_8D_Lea_GwEw(),        // 0x8D - LEA Gw,Ew
            new I_8E_Mov_SwEw(),        // 0x8E - MOV Sw,Ew
            new I_8F_Pop_Ew(),          // 0x8F - POP Ew
            new I_90_Nop(),             // 0x90 - NOP
            new I_91_Xchg_CX(),         // 0x91 - XCHG AX,CX
            new I_92_Xchg_DX(),         // 0x92 - XCHG AX,DX
            new I_93_Xchg_BX(),         // 0x93 - XCHG AX,BX
            new I_94_Xchg_SP(),         // 0x93 - XCHG AX,SP
            new I_95_Xchg_BP(),         // 0x93 - XCHG AX,BP
            new I_96_Xchg_SI(),         // 0x93 - XCHG AX,SI
            new I_97_Xchg_DI(),         // 0x93 - XCHG AX,DI
            new I_98_CBW(),             // 0x98 - CBW
            new I_99_CWD(),             // 0x99 - CWD
            new I_9A_Call_Ap(),         // 0x9A - CALL Ap
            i_XX_Invalid,               // 0x9B - WAIT/FWAIT
            new I_9C_PushFlags(),       // 0x9C - PUSHF
            new I_9D_PopFlags(),        // 0x9D - POPF
            new I_9E_SAHF(),            // 0x9E - SAHF
            new I_9F_LAHF(),            // 0x9F - LAHF
            new I_A0_Mov_AL_Ob(),       // 0xA0 - MOV AL,Ob
            new I_A1_Mov_AX_Ow(),       // 0xA1 - MOV AX,Ow
            new I_A2_Mov_Ob_AL(),       // 0xA2 - MOV Ob,AL
            new I_A3_Mov_Ow_AX(),       // 0xA3 - MOV Ow,AX
            new I_A4_MOVSB(),           // 0xA4 - MOVSB
            new I_A5_MOVSW(),           // 0xA5 - MOVSW
            new I_A6_CMPSB(),           // 0xA6 - CMPSB
            new I_A7_CMPSW(),           // 0xA7 - CMPSW
            new I_A8_Test_AL_Ib(),      // 0xA8 - TEST AL,Ib
            new I_A9_Test_AX_Iw(),      // 0xA9 - TEST AX,Iw
            new I_AA_STOSB(),           // 0xAA - STOSB
            new I_AB_STOSW(),           // 0xAB - STOSW
            new I_AC_LODSB(),           // 0xAC - LODSB
            new I_AD_LODSW(),           // 0xAD - LODSW
            new I_AE_SCASB(),           // 0xAE - SCASB
            new I_AF_SCASW(),           // 0xAF - SCASW
            new I_B0_Mov_AL_Ib(),       // 0xB0 - MOV AL,Ib
            new I_B1_Mov_CL_Ib(),       // 0xB1 - MOV CL,Ib
            new I_B2_Mov_DL_Ib(),       // 0xB2 - MOV DL,Ib
            new I_B3_Mov_BL_Ib(),       // 0xB3 - MOV BL,Ib
            new I_B4_Mov_AH_Ib(),       // 0xB4 - MOV AH,Ib
            new I_B5_Mov_CH_Ib(),       // 0xB5 - MOV CH,Ib
            new I_B6_Mov_DH_Ib(),       // 0xB6 - MOV DH,Ib
            new I_B7_Mov_BH_Ib(),       // 0xB7 - MOV BH,Ib
            new I_B8_Mov_AX_Iw(),       // 0xB8 - MOV AX,Iw
            new I_B9_Mov_CX_Iw(),       // 0xB9 - MOV CX,Iw
            new I_BA_Mov_DX_Iw(),       // 0xBA - MOV DX,Iw
            new I_BB_Mov_BX_Iw(),       // 0xBB - MOV BX,Iw
            new I_BC_Mov_SP_Iw(),       // 0xBC - MOV SP,Iw
            new I_BD_Mov_BP_Iw(),       // 0xBD - MOV BP,Iw
            new I_BE_Mov_SI_Iw(),       // 0xBE - MOV SI,Iw
            new I_BF_Mov_DI_Iw(),       // 0xBE - MOV DI,Iw
            i_XX_Invalid,               // 0xC0 - shift Eb,Ib
            i_XX_Invalid,               // 0xC1 - shift Ew,Ib
            new I_C2_Ret_Iw(),          // 0xC2 - RET Iw
            new I_C3_Ret(),             // 0xC3 - RET
            new I_C4_Load_ES(),         // 0xC4 - LES Gw,Mp
            new I_C5_Load_DS(),         // 0xC5 - LDS Gw,Mp
            new I_C6_Mov_EbIb(),        // 0xC6 - MOV Eb,Ib
            new I_C7_Mov_EwIw(),        // 0xC7 - MOV Ew,Iw
            i_XX_Invalid,               // 0xC8 - ENTER
            i_XX_Invalid,               // 0xC9 - LEAVE
            new I_CA_Ret_Far_Iw(),      // 0xCA - RETF Iw
            new I_CB_Ret_Far(),         // 0xCB - RETF
            i_XX_Invalid,
            new I_CD_Interrupt(),       // 0xCD - INT nn
            i_XX_Invalid,
            new I_CF_IntRet(),          // 0xCF - IRET
            new I_D0_Shift_Eb1(),       // 0xD0 - shift Eb,1
            new I_D1_Shift_Ew1(),       // 0xD1 - shift Ew,1
            new I_D2_Shift_EbCL(),      // 0xD2 - shift Eb,CL
            new I_D3_Shift_EwCL(),      // 0xD3 - shift Ew,CL
            new I_D4_AAM(),             // 0xD4 - AAM
            i_XX_Invalid,
            i_XX_Invalid,
            new I_D7_Xlat(),            // 0xD7 - XLAT
            i_XX_Invalid,               // 0xD8 - x87 instructions
            i_XX_Invalid,               // 0xD9 - x87 instructions
            i_XX_Invalid,               // 0xDA - x87 instructions
            i_XX_Invalid,               // 0xDB - x87 instructions
            i_XX_Invalid,               // 0xDC - x87 instructions
            i_XX_Invalid,               // 0xDD - x87 instructions
            i_XX_Invalid,               // 0xDE - x87 instructions
            i_XX_Invalid,               // 0xDF - x87 instructions
            new I_E0_LoopNZ_Jb(),       // 0xE0 - LOOPNZ Jb
            new I_E1_LoopZ_Jb(),        // 0xE1 - LOOPZ Jb
            new I_E2_Loop_Jb(),         // 0xE2 - LOOP Jb
            new I_E3_JumpCXZ(),         // 0xE3 - JCXZ Jb
            new I_E4_In_AL_Ib(),        // 0xE4 - IN AL, Ib
            i_XX_Invalid,
            new I_E6_Out_Ib_AL(),       // 0xE6 - OUT Ib, AL
            i_XX_Invalid,
            new I_E8_Call_Jv(),         // 0xE8 - CALL Jv
            new I_E9_Jump_Jv(),         // 0xE9 - JMP Jv
            new I_EA_Jump_Ap(),         // 0xEA - JMP Ap
            new I_EB_Jump_Jb(),         // 0xEB - JMP Jb
            new I_EC_In_AL_DX(),        // 0xEC - IN AL, DX
            i_XX_Invalid,
            new I_EE_Out_DX_AL(),       // 0xEE - OUT DX, AL
            i_XX_Invalid,
            i_XX_Invalid,
            i_XX_Invalid,               // 0xF1 - undefined
            new I_F2_REPNE_Prefix(),    // 0xF2 - REPNE prefix
            new I_F3_REP_Prefix(),      // 0xF3 - REP/REPE prefix
            new I_F4_Halt(),            // 0xF4 - HLT
            new I_F5_CMC(),             // 0xF5 - CMC
            new I_F6_TwoByte(),         // 0xF6 - TEST/NOT/NEG/MUL/IMUL/DIV/IDIV Eb
            new I_F7_TwoByte(),         // 0xF7 - TEST/NOT/NEG/MUL/IMUL/DIV/IDIV Ew
            new I_F8_CLC(),             // 0xF8 - CLC
            new I_F9_STC(),             // 0xF9 - STC
            new I_FA_CLI(),             // 0xFA - CLI
            new I_FB_STI(),             // 0xFB - STI
            new I_FC_CLD(),             // 0xFC - CLD
            new I_FD_STD(),             // 0xFD - STD
            new I_FE_TwoByte(),         // 0xFE - INC/DEC
            new I_FF_TwoByte(),         // 0xFF - INC/DEC/CALL/JMP/PUSH
        };

        // --------------------------------------------------------------------
        // invalid/unknown/unrecognized instruction

        private sealed class I_XX_Invalid : Instruction
        {

            public override void Process (Cpu cpu)
            {
                cpu.InstructionAddress = cpu.InstructionAddress - 1;
                ThrowInvalid(cpu, cpu.GetInstructionByte(), cpu.GetInstructionByte());
            }

            public static void ThrowInvalid (Cpu cpu, int op1, int op2)
            {
                var mem = cpu.stateBytes;
                var addr = cpu.InstructionAddress - 2;
                var cs = mem[(int) Reg.CS] | (mem[(int) Reg.CS + 1] << 8);
                var ip = addr - (cs << 4);
                throw new System.InvalidProgramException(
                            $"Invalid instruction at {cs:X4}:{ip:X4} bytes {op1:X2},{op2:X2}");
            }

            #if DEBUGGER
            public override string Print (Cpu cpu) => "???";
            #endif
        }

        // --------------------------------------------------------------------
        // abstract class with a single method to process one instruction

        private abstract class Instruction
        {
            public abstract void Process (Cpu cpu);

            #if DEBUGGER
            public virtual string Print (Cpu cpu) { return "???"; }
            #endif
        }

    }
}

