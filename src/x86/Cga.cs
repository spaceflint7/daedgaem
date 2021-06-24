
namespace com.spaceflint.x86
{
    public partial class Cga : Cpu.IPlugin
    {

        // --------------------------------------------------------------------
        // constructor

        public Cga (Machine _machine)
        {
            Machine = _machine;

            // register BIOS interrupt 10H and 21H,
            // and ports 0x3D8, 0x3D9, 0x3DA
            var cgaInterrupts = new int[] { 0x10 };
            var cgaPorts = new int[] { 0x3D8, 0x3D9, 0x3DA };
            Machine.Cpu.RegisterPlugin(this, cgaInterrupts, cgaPorts);

            // create video modes
            modes = new IShell.IVideo.Client[8];
            modes[0] = modes[1] = new TextMode40(this);
            modes[2] = modes[3] = /*modes[7] =*/ new TextMode80(this);
            modes[4] = modes[5] = new GraphicsMode320(this);

            // start with 40x25 text mode (mode 1)
            SetVideoMode(1);

            // set the equipment flags at 0040:0010 (see also INT 11h below)
            // - set the initial video mode to 40x25 (0x0010)
            Machine.Cpu.SetWord(0x410,
                                Machine.Cpu.GetWord(0x410) | 0x0010);
        }

        // --------------------------------------------------------------------
        // Interrupt

        public int Interrupt (int which)
        {
            int serviceNumber = Machine.Cpu.GetByte((int) Cpu.Reg.AX + 1);
            switch (serviceNumber)
            {
                case 0x00:

                    SetVideoMode();
                    break;

                case 0x01:

                    SetCursorSize();
                    break;

                case 0x02:

                    if (SetCursorPos())
                        break;
                    goto default;

                case 0x03:

                    if (GetCursorPos())
                        break;
                    goto default;

                case 0x05:

                    if (SelectPage())
                        break;
                    goto default;

                case 0x06:
                case 0x07:
                    return ScrollWindow((serviceNumber - 7) * 2 + 1);

                case 0x08:

                    if (ReadCharacter())
                        break;
                    goto default;

                case 0x09:
                case 0x0A:

                    if (WriteCharacter(0x0A - serviceNumber))
                        break;
                    goto default;

                case 0x0B:

                    if (SetPalette())
                        break;
                    goto default;

                case 0x0E:

                    if (WriteCharacterTeletype())
                        break;
                    goto default;

                case 0x0F:

                    GetVideoState();
                    break;

                case 0x10:
                case 0x11:
                case 0x12:
                case 0x1B:
                case 0xEF:
                    // fail silently on EGA+ functions
                    break;

                default:

                    return -serviceNumber;
            }

            return 0; // success
        }

        // --------------------------------------------------------------------
        // SetVideoMode (INT 10h, AH = 00h)

        private void SetVideoMode (int mode = -1)
        {
            if (mode == -1)
                mode = Machine.Cpu.GetWord((int) Cpu.Reg.AX);
            #if DEBUGGER
            System.Console.WriteLine($"CGA: SET VIDEO MODE {mode:X4}");
            #endif
            if (mode <= 7)
            {
                // 80 columns in modes 2, 3, 6, 7, otherwise 40 columns
                int columns = (mode & 2) != 0 ? 80 : 40;
                // video size is 16384 for graphics,
                // or 2 bytes per character for text
                int memsize = (mode >= 4 && mode <= 6) ? 0x4000
                            : ((columns * 25 * 2 + 0xFF) & 0xFF00);
                int port = (mode == 7) ? /* MDA */ 0x3B4 : /* CGA */ 0x3D4;

                Machine.Cpu.SetByte(0x449, (byte) mode);
                Machine.Cpu.SetWord(0x44A, columns);
                Machine.Cpu.SetWord(0x44C, memsize);
                Machine.Cpu.SetWord(0x463, port);

                // cursor positions in 8 text pages
                for (int i = 0x450; i < 0x460; i += 2)
                    Machine.Cpu.SetWord(i, 0);
                // cursor starting and ending scan lines
                Machine.Cpu.SetWord(0x460, 0x0607);

                // clear video ram and select new video mode
                for (int i = 0; i < memsize; i += 2)
                    Machine.Cpu.SetWord(0xB8000 + i, 0);

                if (mode >= 4 && mode <= 6)
                    SelectGraphicsPalette(5, 0);

                Machine.Shell.Video.Mode(modes[mode]);
            }
        }

        // --------------------------------------------------------------------
        // SetCursorSize (INT 10h, AH = 01h)

        private void SetCursorSize ()
        {
            Machine.Cpu.SetWord(0x460, Machine.Cpu.GetWord((int) Cpu.Reg.CX));
        }

        // --------------------------------------------------------------------
        // SetCursorPos (INT 10h, AH = 02h)

        private bool SetCursorPos ()
        {
            if (Machine.Cpu.GetByte((int) Cpu.Reg.BX + 1) != 0)
                return false;   // only support page 0

            int col = Machine.Cpu.GetByte((int) Cpu.Reg.DX);
            int row = Machine.Cpu.GetByte((int) Cpu.Reg.DX + 1);
            /*if (row >= 25 || col >= Machine.Cpu.GetByte(0x44A))
                return false;   // row or column not within range*/

            Machine.Cpu.SetByte(0x450, (byte) col);
            Machine.Cpu.SetByte(0x451, (byte) row);
            return true;
        }

        // --------------------------------------------------------------------
        // SetCursorPos (INT 10h, AH = 03h)

        private bool GetCursorPos ()
        {
            if (Machine.Cpu.GetByte((int) Cpu.Reg.BX + 1) != 0)
                return false;   // only support page 0

            // return cursor start and end scan lines in CX
            Machine.Cpu.SetWord((int) Cpu.Reg.CX, Machine.Cpu.GetWord(0x460));
            // return cursor row and column in DX
            Machine.Cpu.SetWord((int) Cpu.Reg.DX, Machine.Cpu.GetWord(0x450));

            return true;
        }

        // --------------------------------------------------------------------
        // SelectPage (INT 10h, AH = 05h)

        private bool SelectPage ()
        {
            /*int mode = Machine.Cpu.GetByte(0x449);
            if (mode >= 4 && mode <= 6)     // graphics mode
                return false;*/

            int pageNum = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            if (pageNum != 0)
                return false;

            return true;
        }

        // --------------------------------------------------------------------
        // ScrollWindow (INT 10h, AH = 06h, AH = 07h)

        private int ScrollWindow (int dir)
        {
            int mode = Machine.Cpu.GetByte(0x449);
            int lines = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            int blank = Machine.Cpu.GetByte((int) Cpu.Reg.BX) & 0xFF00;
            int x0y0 = Machine.Cpu.GetWord((int) Cpu.Reg.CX);
            int x1y1 = Machine.Cpu.GetWord((int) Cpu.Reg.DX);
            #if DEBUGGER
            System.Console.WriteLine($"CGA: SCROLL {lines} LINES, DIRECTION {dir}, FROM {x0y0:X4} TO {x1y1:X4}");
            #endif
            if (lines == 0 || lines >= 25)
            {
                // clear entire screen
                int memsize = Machine.Cpu.GetWord(0x44C);
                for (int i = 0; i < memsize; i += 2)
                    Machine.Cpu.SetWord(0xB8000 + i, blank);
            }
            else if (dir == 1)                   // scroll down
            {
                // scroll up is not supported
                return -0x6702;
            }
            else if (mode >= 4 && mode <= 6)     // graphics mode
            {
                // scroll in graphics not suported
                return -0x6703;
            }
            else
            {
                int width = Machine.Cpu.GetByte(0x44A);
                if (width != 40 && width != 80)
                    return -0x6780;

                int x0 = x0y0 & 0xFF;
                int x1 = x1y1 & 0xFF;
                int y0 = x0y0 >> 8;
                int y1 = x1y1 >> 8;
                if (x1 < x0 || y1 < y0)
                    return -30;
                int ptr, endPtr;

                for (int y = y0; y <= y1; y++)
                {
                    ptr = 0xB8000 + (y * width + x0) * 2;
                    endPtr = ptr + (x1 - x0) * 2;
                    for (; ptr < endPtr; ptr += 2)
                    {
                        Machine.Cpu.SetWord(ptr,
                                Machine.Cpu.GetWord(ptr + width * 2));
                    }
                }

                ptr = 0xB8000 + (y1 * width + x0) * 2;
                endPtr = ptr + (x1 - x0) * 2;
                for (; ptr < endPtr; ptr += 2)
                    Machine.Cpu.SetWord(ptr, blank);
            }

            return 0;
        }

        // --------------------------------------------------------------------
        // SetPalette (INT 10h, AH = 0Bh)

        private bool SetPalette ()
        {
            int mode = Machine.Cpu.GetByte(0x449);
            if (mode >= 4 && mode <= 6)     // graphics mode
            {
                if (mode == 6)              // fail in 640x200 mode
                    return false;
                int value = Machine.Cpu.GetWord((int) Cpu.Reg.BX);
                #if DEBUGGER
                System.Console.WriteLine($"CGA: SET GRAPHICS PALETTE VIA BIOS TO {value:X4}");
                #endif
                if (value < 0x100)  // BH=0, set background color
                    SelectGraphicsPalette(-1, value & 15);
                else                // BH=1, select 4-color palette
                    SelectGraphicsPalette(value & 3, -1);
                // activate new palette
                Machine.Shell.Video.Mode(modes[mode]);
            }
            // no-op in text mode
            return true;
        }

        // --------------------------------------------------------------------
        // GetVideoState (INT 10h, AH = 0Fh)

        private void GetVideoState ()
        {
            int al = Machine.Cpu.GetByte(0x449);            // mode
            int ah = Machine.Cpu.GetByte(0x44A);            // columns
            Machine.Cpu.SetWord((int) Cpu.Reg.AX, al | (ah << 8));
            Machine.Cpu.SetByte((int) Cpu.Reg.BX + 1, 0);   // page number
        }

        // --------------------------------------------------------------------
        // IsGraphicsMode

        /*private bool IsGraphicsMode ()
        {
            int mode = Machine.Cpu.GetByte(0x449);
            return (mode >= 4 && mode <= 6);
        }*/

        // --------------------------------------------------------------------
        // ReadCharacter (INT 10h, AH = 08)

        private bool ReadCharacter ()
        {
            int addr = GetAddressAtCursor();
            if (addr < 0)
                return false;

            Machine.Cpu.SetWord((int) Cpu.Reg.AX, Machine.Cpu.GetWord(addr));
            return true;
        }

        // --------------------------------------------------------------------
        // WriteCharacter (INT 10h, AH = 09h, AH = 0Ah)

        private bool WriteCharacter (int withAttr, int count = -1)
        {
            int mode = Machine.Cpu.GetByte(0x449);

            int ch = Machine.Cpu.GetByte((int) Cpu.Reg.AX);
            int attr = Machine.Cpu.GetByte((int) Cpu.Reg.BX);
            if (count == -1)
                count = Machine.Cpu.GetByte((int) Cpu.Reg.CX);

            if (mode >= 4 && mode <= 6)     // graphics mode
            {
                return WriteCharacterGraphics(ch, attr, count);
            }

            int addr = GetAddressAtCursor();
            if (addr < 0)
                return false;

            while (count-- != 0)
            {
                if (withAttr != 0)
                    Machine.Cpu.SetWord(addr, ch | (attr << 8));
                else
                    Machine.Cpu.SetByte(addr, (byte) ch);
            }

            return true;
        }

        // --------------------------------------------------------------------
        // WriteCharacterGraphics (INT 10h, AH = 09h, AH = 0Ah, graphics mode)

        private bool WriteCharacterGraphics (int ch, int attr, int count)
        {
            attr &= 3;
            int cursor = Machine.Cpu.GetWord(0x450);
            int cursorX = (cursor & 0x7F) << 1;             // mul by two
            int cursorY = ((cursor >> 8) & 0x1F) << 2;      // mul by eight
            var mask = s_font[Machine.Cpu.GetByte((int) Cpu.Reg.AX)];
            #if DEBUGGER
            System.Console.WriteLine($"CGA: PRINT CHARACTER {(char) ch} (ROW {cursorY} COL {cursorX}) ATTR {attr:X4}");
            #endif
            for (int y = 7; y >= 0; y--)
            {
                int addr = 0xB8000 + cursorX + (cursorY + (y >> 1)) * 80 + ((y & 1) << 13);
                int w = 0;
                for (int x = 0; x < 16; x += 2)
                {
                    if ((mask & 1) != 0)
                        w |= attr << x;
                    mask >>= 1;
                }
                Machine.Cpu.SetByte(addr,     (byte) (w >> 8));
                Machine.Cpu.SetByte(addr + 1, (byte) w);
            }

            return true;
        }

        // --------------------------------------------------------------------
        // WriteCharacterTeletype (INT 10h, AH = 0Eh)

        private bool WriteCharacterTeletype ()
        {
            bool ok = WriteCharacter(withAttr: 1, count: 1);
            // advance cursor
            Machine.Cpu.SetWord(0x450, Machine.Cpu.GetWord(0x450) + 1);
            return ok;
        }

        // --------------------------------------------------------------------
        // GetAddressAtCursor

        private int GetAddressAtCursor ()
        {
            int mode = Machine.Cpu.GetByte(0x449);
            if (mode >= 4 && mode <= 6)     // graphics mode
                return -1;

            int pageNum = Machine.Cpu.GetByte((int) Cpu.Reg.BX + 1);
            if (pageNum != 0)
                return -1;

            int cursor = Machine.Cpu.GetWord(0x450 + pageNum * 2);
            int cursorX = cursor & 0xFF;
            int cursorY = (cursor >> 8) & 0xFF;
            int width = Machine.Cpu.GetByte(0x44A);
            return 0xB8000 + (cursorX + cursorY * width) * 2;
        }

        // --------------------------------------------------------------------
        // ReadPort

        public int ReadPort (int which)
        {
            if (which == 0x3DA)
            {
                // port 3DAh - status register
                // bit 0x01 - set during horizontal retrace
                //            we flip this bit every read
                // bit 0x04 - set to indicate light pen is disabled
                // bit 0x08 - set during vertical retrace
                //            we set this bit once every 270 reads
                int result = 4 | (statusRegister & 1);
                if (++statusRegister == 270)
                {
                    statusRegister = 0;
                    result |= 9;
                }
                return result;
            }
            return -1;
        }

        // --------------------------------------------------------------------
        // WritePort

        public int WritePort (int which, int value)
        {
            int mode = Machine.Cpu.GetByte(0x449);
            #if DEBUGGER
            System.Console.WriteLine($"CGA: OUT PORT {which:X4} VALUE {value:X2} IN MODE {mode:X4}");
            #endif
            if (mode == 4)
            {
                if (which == 0x3D8)
                {
                    // port 3D8h - mode control register
                    // bit 0x01 - 80-column text mode (otherwise 40-column)
                    // bit 0x02 - graphics mode (otherwise text)
                    // bit 0x04 - third graphics palette (otherwise set by 3D9h)
                    // bit 0x08 - enable video output (otherwise disable)
                    // bit 0x10 - 640x200 graphics (otherwise 320x200)
                    // bit 0x20 - enable blinking
                    if (value == 0x00 || value == 0x0A)
                    {
                        // override palette if 0x00 or 0x0A are set in mode 4
                        SelectGraphicsPalette(4, 0);
                        Machine.Shell.Video.Mode(modes[mode]);
                        return 0;
                    }
                }
                if (which == 0x3D9)
                {
                    // port 3D9h - color control register
                    // bits 0x0F - background color index
                    // bit 0x10  - high intensity graphics (otherwise low)
                    // bit 0x20  - first graphics palette (otherwise second)
                    which = (value & 0x10) >> 2  // shift bit 0x10 to bit 0x04
                          | (value & 0x20) >> 5; // shift bit 0x20 to bit 0x01
                    SelectGraphicsPalette(which, value & 0x0F);
                    Machine.Shell.Video.Mode(modes[mode]);
                    return 0;
                }
            }
            return -1;
        }

        // --------------------------------------------------------------------
        // SelectGraphicsPalette

        private void SelectGraphicsPalette (int which, int zeroIndex)
        {
            if (zeroIndex != -1)
                palette4[0] = palette16[zeroIndex];

            if (which != -1)
            {
                int p1, p2, p3;
                switch (which & 7)
                {
                    case 0:     p1 = 2;     // dark green
                                p2 = 4;     // dark red
                                p3 = 6;     // brown
                                break;
                    case 1:     p1 = 3;     // dark cyan
                                p2 = 5;     // dark magenta
                                p3 = 7;     // light gray
                                break;
                    case 2:     p1 = 3;     // dark cyan
                                p2 = 4;     // dark red
                                p3 = 7;     // light gray
                                break;
                    case 4:     p1 = 10;    // light green
                                p2 = 12;    // light red
                                p3 = 14;    // yellow
                                break;
                    case 5:     p1 = 11;    // light cyan
                                p2 = 13;    // light magenta
                                p3 = 15;    // white
                                break;
                    case 6:     p1 = 11;    // light cyan
                                p2 = 12;    // light red
                                p3 = 15;    // white
                                break;
                    default:    throw new System.ArgumentException();
                }

                palette4[1] = palette16[p1];
                palette4[2] = palette16[p2];
                palette4[3] = palette16[p3];
            }

            // dummy update on the volatile field to force a memory barrier
            #pragma warning disable 1717
            palette4 = palette4;
            #pragma warning restore 1717
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] private Machine Machine;
        [java.attr.RetainType] private IShell.IVideo.Client[] modes;

        [java.attr.RetainType] private volatile int statusRegister;

        [java.attr.RetainType] private static int[] palette16 = new int[]
        {
            /* black       */ 0x000000,     /* dark blue     */ 0x0000AA,
            /* dark green  */ 0x00AA00,     /* dark cyan     */ 0x00AAAA,
            /* dark red    */ 0xAA0000,     /* dark magenta  */ 0xAA00AA,
            /* brown       */ 0xAA5500,     /* light gray    */ 0xAAAAAA,
            /* dark gray   */ 0x555555,     /* light blue    */ 0x5555FF,
            /* light green */ 0x55FF55,     /* light cyan    */ 0x55FFFF,
            /* light red   */ 0xFF5555,     /* light magenta */ 0xFF55FF,
            /* yellow      */ 0xFFFF55,     /* white         */ 0xFFFFFF
        };
        [java.attr.RetainType] private volatile int[] palette4 = new int[4];

        [java.attr.RetainType] private static long[] s_font = InitializeFont();

        // --------------------------------------------------------------------
        // InitializeFont

        private static long[] InitializeFont()
        {
            // hex dump of font:
            // xxd -u -g 8 https://github.com/spacerace/romfont/blob/master/other_sources/ibm_pc/IBM_PC_V1_8x8.bin
            var font = new long[256];

            // control characters
            font[0x01] = unchecked ((long) 0x7E81A581BD99817E);
            font[0x02] = unchecked ((long) 0x7EFFDBFFC3E7FF7E);
            font[0x03] = unchecked ((long) 0x6CFEFEFE7C381000);
            font[0x04] = unchecked ((long) 0x10387CFE7C381008);
            font[0x05] = unchecked ((long) 0x387C38FEFE7C387C);
            font[0x06] = unchecked ((long) 0x1010387CFE7C387C);
            font[0x07] = unchecked ((long) 0x0000183C3C180000);
            font[0x08] = unchecked ((long) 0xFFFFE7C3C3E7FFFF);
            font[0x09] = unchecked ((long) 0x003C664242663C00);
            font[0x0A] = unchecked ((long) 0xFFC399BDBD99C3FF);
            font[0x0B] = unchecked ((long) 0x0F070F7DCCCCCC78);
            font[0x0C] = unchecked ((long) 0x3C6666663C187E18);
            font[0x0D] = unchecked ((long) 0x3F333F303070F0E0);
            font[0x0E] = unchecked ((long) 0x7F637F636367E6C0);
            font[0x0F] = unchecked ((long) 0x995A3CE7E73C5A99);
            font[0x10] = unchecked ((long) 0x80E0F8FEF8E08000);
            font[0x11] = unchecked ((long) 0x020E3EFE3E0E0200);
            font[0x12] = unchecked ((long) 0x183C7E18187E3C18);
            font[0x13] = unchecked ((long) 0x6666666666006600);
            font[0x14] = unchecked ((long) 0x7FDBDB7B1B1B1B00);
            font[0x15] = unchecked ((long) 0x3E63386C6C38CC78);
            font[0x16] = unchecked ((long) 0x000000007E7E7E00);
            font[0x17] = unchecked ((long) 0x183C7E187E3C18FF);
            font[0x18] = unchecked ((long) 0x183C7E1818181800);
            font[0x19] = unchecked ((long) 0x181818187E3C1800);
            font[0x1A] = unchecked ((long) 0x00180CFE0C180000);
            font[0x1B] = unchecked ((long) 0x003060FE60300000);
            font[0x1C] = unchecked ((long) 0x0000C0C0C0FE0000);
            font[0x1D] = unchecked ((long) 0x002466FF66240000);
            font[0x1E] = unchecked ((long) 0x00183C7EFFFF0000);
            font[0x1F] = unchecked ((long) 0x00FFFF7E3C180000);

            font['!']  = unchecked ((long) 0x3078783030003000);
            font['\"'] = unchecked ((long) 0x6C6C6C0000000000);
            font['#']  = unchecked ((long) 0x6C6CFE6CFE6C6C00);
            font['$']  = unchecked ((long) 0x307CC0780CF83000);
            font['%']  = unchecked ((long) 0x00C6CC183066C600);
            font['&']  = unchecked ((long) 0x386C3876DCCC7600);
            font['\''] = unchecked ((long) 0x6060C00000000000);
            font['(']  = unchecked ((long) 0x1830606060301800);
            font[')']  = unchecked ((long) 0x6030181818306000);
            font['*']  = unchecked ((long) 0x00663CFF3C660000);
            font['+']  = unchecked ((long) 0x003030FC30300000);
            font[',']  = unchecked ((long) 0x0000000000303060);
            font['-']  = unchecked ((long) 0x000000FC00000000);
            font['.']  = unchecked ((long) 0x0000000000303000);
            font['/']  = unchecked ((long) 0x060C183060C08000);

            font['0']  = unchecked ((long) 0x7CC6CEDEF6E67C00);
            font['1']  = unchecked ((long) 0x307030303030FC00);
            font['2']  = unchecked ((long) 0x78CC0C3860CCFC00);
            font['3']  = unchecked ((long) 0x78CC0C380CCC7800);
            font['4']  = unchecked ((long) 0x1C3C6CCCFE0C1E00);
            font['5']  = unchecked ((long) 0xFCC0F80C0CCC7800);
            font['6']  = unchecked ((long) 0x3860C0F8CCCC7800);
            font['7']  = unchecked ((long) 0xFCCC0C1830303000);
            font['8']  = unchecked ((long) 0x78CCCC78CCCC7800);
            font['9']  = unchecked ((long) 0x78CCCC7C0C187000);

            font[':']  = unchecked ((long) 0x0030300000303000);
            font[';']  = unchecked ((long) 0x0030300000303060);
            font['<']  = unchecked ((long) 0x183060C060301800);
            font['=']  = unchecked ((long) 0x0000FC0000FC0000);
            font['>']  = unchecked ((long) 0x6030180C18306000);
            font['?']  = unchecked ((long) 0x78CC0C1830003000);
            font['@']  = unchecked ((long) 0x7CC6DEDEDEC07800);

            font['A']  = unchecked ((long) 0x3078CCCCFCCCCC00);
            font['B']  = unchecked ((long) 0xFC66667C6666FC00);
            font['C']  = unchecked ((long) 0x3C66C0C0C0663C00);
            font['D']  = unchecked ((long) 0xF86C6666666CF800);
            font['E']  = unchecked ((long) 0xFE6268786862FE00);
            font['F']  = unchecked ((long) 0xFE6268786860F000);
            font['G']  = unchecked ((long) 0x3C66C0C0CE663E00);
            font['H']  = unchecked ((long) 0xCCCCCCFCCCCCCC00);
            font['I']  = unchecked ((long) 0x7830303030307800);
            font['J']  = unchecked ((long) 0x1E0C0C0CCCCC7800);
            font['K']  = unchecked ((long) 0xE6666C786C66E600);
            font['L']  = unchecked ((long) 0xF06060606266FE00);
            font['M']  = unchecked ((long) 0xC6EEFEFED6C6C600);
            font['N']  = unchecked ((long) 0xC6E6F6DECEC6C600);
            font['O']  = unchecked ((long) 0x386CC6C6C66C3800);
            font['P']  = unchecked ((long) 0xFC66667C6060F000);
            font['Q']  = unchecked ((long) 0x78CCCCCCDC781C00);
            font['R']  = unchecked ((long) 0xFC66667C6C66E600);
            font['S']  = unchecked ((long) 0x78CCE0701CCC7800);
            font['T']  = unchecked ((long) 0xFCB4303030307800);
            font['U']  = unchecked ((long) 0xCCCCCCCCCCCCFC00);
            font['V']  = unchecked ((long) 0xCCCCCCCCCC783000);
            font['W']  = unchecked ((long) 0xC6C6C6D6FEEEC600);
            font['X']  = unchecked ((long) 0xC6C66C38386CC600);
            font['Y']  = unchecked ((long) 0xCCCCCC7830307800);
            font['Z']  = unchecked ((long) 0xFEC68C183266FE00);

            font['[']  = unchecked ((long) 0x7860606060607800);
            font['\\'] = unchecked ((long) 0xC06030180C060200);
            font[']']  = unchecked ((long) 0x7818181818187800);
            font['^']  = unchecked ((long) 0x10386CC600000000);
            font['_']  = unchecked ((long) 0x00000000000000FF);
            font['`']  = unchecked ((long) 0x3030180000000000);

            font['a']  = unchecked ((long) 0x0000780C7CCC7600);
            font['b']  = unchecked ((long) 0xE060607C6666DC00);
            font['c']  = unchecked ((long) 0x000078CCC0CC7800);
            font['d']  = unchecked ((long) 0x1C0C0C7CCCCC7600);
            font['e']  = unchecked ((long) 0x000078CCFCC07800);
            font['f']  = unchecked ((long) 0x386C60F06060F000);
            font['g']  = unchecked ((long) 0x000076CCCC7C0CF8);
            font['h']  = unchecked ((long) 0xE0606C766666E600);
            font['i']  = unchecked ((long) 0x3000703030307800);
            font['j']  = unchecked ((long) 0x0C000C0C0CCCCC78);
            font['k']  = unchecked ((long) 0xE060666C786CE600);
            font['l']  = unchecked ((long) 0x7030303030307800);
            font['m']  = unchecked ((long) 0x0000CCFEFED6C600);
            font['n']  = unchecked ((long) 0x0000F8CCCCCCCC00);
            font['o']  = unchecked ((long) 0x000078CCCCCC7800);
            font['p']  = unchecked ((long) 0x0000DC66667C60F0);
            font['q']  = unchecked ((long) 0x000076CCCC7C0C1E);
            font['r']  = unchecked ((long) 0x0000DC766660F000);
            font['s']  = unchecked ((long) 0x00007CC0780CF800);
            font['t']  = unchecked ((long) 0x10307C3030341800);
            font['u']  = unchecked ((long) 0x0000CCCCCCCC7600);
            font['v']  = unchecked ((long) 0x0000CCCCCC783000);
            font['w']  = unchecked ((long) 0x0000C6D6FEFE6C00);
            font['x']  = unchecked ((long) 0x0000C66C386CC600);
            font['y']  = unchecked ((long) 0x0000CCCCCC7C0CF8);
            font['z']  = unchecked ((long) 0x0000FC983064FC00);

            font['{']  = unchecked ((long) 0x1C3030E030301C00);
            font['|']  = unchecked ((long) 0x1818180018181800);
            font['}']  = unchecked ((long) 0xE030301C3030E000);
            font['~']  = unchecked ((long) 0x76DC000000000000);
            font[127]  = unchecked ((long) 0x0010386CC6C6FE00);

            // use a dummy X character for the range 128..255
            for (int i = 128; i < 255; i++)
                font[i] = unchecked ((long) 0x0042241818244200);

            return font;
        }

         // --------------------------------------------------------------------
        // TextModeBase

        private class TextModeBase : IShell.IVideo.Client
        {
            [java.attr.RetainType] private Cga cga;
            [java.attr.RetainType] private Cpu cpu;

            private const int ColumnWidth = 8;
            private const int RowHeight   = 8;

            private const int ScreenRows  = 25;
            private const int ScreenHeightInPixels = ScreenRows * RowHeight;

            [java.attr.RetainType] private int ScreenColumns;
            [java.attr.RetainType] private int ScreenWidthInPixels;

            // --------------------------------------------------------------------
            // constructor

            protected TextModeBase (Cga _cga, int columns)
            {
                cga = _cga;
                cpu = cga.Machine.Cpu;
                ScreenColumns = columns;
                ScreenWidthInPixels = ScreenColumns * ColumnWidth;
            }

            // --------------------------------------------------------------------
            // properties

            public override int Width  => ScreenWidthInPixels;
            public override int Height => ScreenHeightInPixels;

            public override int[] Palette => palette16;

            // --------------------------------------------------------------------
            // Update

            public override void Update (java.nio.ByteBuffer buffer)
            {
                var cpu = this.cpu;
                var font = Cga.s_font;

                int textAddr = 0xB8000;
                int rowOffset = 0;

                for (int y = 0; y < ScreenRows; y++)
                {
                    int offsetLowerRight = (rowOffset += ScreenWidthInPixels * 8);

                    for (int x = 0; x < ScreenColumns; x++)
                    {
                        var mask = font[cpu.GetByte(textAddr++)];
                        var colorByte = cpu.GetByte(textAddr++);
                        var colorFg = (long) (colorByte & 0x0F);
                        var colorBg = (long) ((colorByte >> 4) & 0x0F);

                        int ofs = offsetLowerRight;
                        offsetLowerRight += 8;

                        for (int rowIndex = 7; rowIndex >= 0; rowIndex--)
                        {
                            buffer.putLong(ofs -= ScreenWidthInPixels,
                                    ((long)  (mask & 128) != 0 ? colorFg : colorBg)
                                  | ((long) ((mask & 64)  != 0 ? colorFg : colorBg) << 8)
                                  | ((long) ((mask & 32)  != 0 ? colorFg : colorBg) << 16)
                                  | ((long) ((mask & 16)  != 0 ? colorFg : colorBg) << 24)
                                  | ((long) ((mask & 8)   != 0 ? colorFg : colorBg) << 32)
                                  | ((long) ((mask & 4)   != 0 ? colorFg : colorBg) << 40)
                                  | ((long) ((mask & 2)   != 0 ? colorFg : colorBg) << 48)
                                  | ((long) ((mask & 1)   != 0 ? colorFg : colorBg) << 56)
                            );
                            mask >>= 8;
                        }
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // TextMode40 - 40x25 text mode

        private class TextMode40 : TextModeBase
        {
            public TextMode40 (Cga cga) : base(cga, 40) { }
        }

        // --------------------------------------------------------------------
        // TextMode80 - 80x25 text mode

        private class TextMode80 : TextModeBase
        {
            public TextMode80 (Cga cga) : base(cga, 80) { }
        }

        // --------------------------------------------------------------------
        // GraphicsMode320

        private class GraphicsMode320 : IShell.IVideo.Client
        {
            [java.attr.RetainType] private Cga cga;
            [java.attr.RetainType] private Cpu cpu;

            // --------------------------------------------------------------------
            // constructor

            public GraphicsMode320 (Cga _cga)
            {
                cga = _cga;
                cpu = cga.Machine.Cpu;
            }

            // --------------------------------------------------------------------
            // properties

            public override int Width  => 320;
            public override int Height => 200;

            public override int[] Palette => cga.palette4;

            // --------------------------------------------------------------------
            // Update

            public override void Update (java.nio.ByteBuffer buffer)
            {
                var cpu = this.cpu;

                int dstAddr = -8;
                for (int y = 0; y < 200; y++)
                {
                    int srcAddr = 0xB8000 + (y >> 1) * 80 + ((y & 1) << 13);
                    for (int x = 0; x < 320 / 8; x++)
                    {
                        // each byte in CGA memory in mode 320x200x4 represents
                        // four pixels at 2 bits per pixel.  we look at two bytes
                        // at once, to populate eight pixels every iteration.
                        //
                        // byte0 bits 6..7 is the leftmost of the 8 pixels at (x+0),
                        // bits 4..5 is for (x+1), 2..3 for (x+2), 0..1 for (x+3).
                        // byte1 bits 6..7 is (x+4), etc, byte1 bits 0..1 is (x+7).
                        //
                        // but putLong expects the bytes in little-endian order:
                        // byte0 bits 6,7 (x+0) shifted to bits  0..7  (byte0)
                        // byte0 bits 4,5 (x+1) shifted to bits  8..15 (byte1)
                        // byte0 bits 2,3 (x+2) shifted to bits 16..23 (byte2)
                        // byte0 bits 0,1 (x+3) shifted to bits 24..31 (byte3)
                        // byte1 bits 6,7 (x+4) shifted to bits 32..39 (byte4)
                        // byte1 bits 4,5 (x+5) shifted to bits 40..47 (byte5)
                        // byte1 bits 2,3 (x+6) shifted to bits 48..55 (byte6)
                        // byte1 bits 0,1 (x+7) shifted to bits 56..63 (byte7)

                        var b0 = cpu.GetByte(srcAddr + 0);
                        var b1 = cpu.GetByte(srcAddr + 1);

                        buffer.putLong(dstAddr += 8, (((long) (b0 & 0xC0)) >> 6 )
                                                   | (((long) (b0 & 0x30)) << 4 )
                                                   | (((long) (b0 & 0x0C)) << 14)
                                                   | (((long) (b0 & 0x03)) << 24)
                                                   | (((long) (b1 & 0xC0)) << 26)
                                                   | (((long) (b1 & 0x30)) << 36)
                                                   | (((long) (b1 & 0x0C)) << 46)
                                                   | (((long) (b1 & 0x03)) << 56));
                        srcAddr += 2;
                    }
                }
            }
        }

  }
}