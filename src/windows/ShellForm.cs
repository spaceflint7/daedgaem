
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using com.spaceflint.x86;

namespace com.spaceflint
{
    public class ShellForm : Form, IShell, IShell.IInput
    {

        // --------------------------------------------------------------------
        // constructor

        public ShellForm (int screenWidth, int screenHeight, IMachine machine)
        {
            MinimumSize = new Size(1,1);
            ClientSize = new Size(screenWidth, screenHeight);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;

            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;

            #if DEBUGGER
            Opacity = 0.5;
            /*Activated += (_, _) =>
            {
                SetForegroundWindow(GetConsoleWindow());
                TopMost = true;
                Opacity = 0.5;
            };
            Deactivate += (_, _) =>
            {
                TopMost = false;
            };*/
            MouseEnter += (_, _) =>
            {
                Opacity = 1.0;
            };
            MouseLeave += (_, _) =>
            {
                Opacity = 0.5;
            };
            #endif

            machineObject = machine;
            machineThread =
                    new Thread(_this => ((ShellForm) _this).ThreadMain());
        }

        // --------------------------------------------------------------------
        // prevent focus stealing on startup

        #if DEBUGGER
        protected override bool ShowWithoutActivation => true;

        // --------------------------------------------------------------------
        // import some Windows methods for focus management

        [System.Runtime.InteropServices.DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern System.IntPtr GetConsoleWindow ();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool SetForegroundWindow (System.IntPtr hWnd);

        #endif

        // --------------------------------------------------------------------
        // OnLoad

        protected override void OnLoad (System.EventArgs eventArgs)
        {
            base.OnLoad(eventArgs);

            #if DEBUGGER
            var screenBounds =
                    System.Windows.Forms.Screen.FromControl(this).Bounds;
            Location = new Point(screenBounds.Width - ClientSize.Width, 0);
            #endif

            screenObject = new Screen(this, machineObject);
            machineThread.Start(this);
        }

        // --------------------------------------------------------------------
        // OnKeyDown

        private void OnKeyDown (object sender, KeyEventArgs eventArgs)
        {
            int scanCode = MapVirtualKey(eventArgs.KeyValue, 0);
            int asciiCode = GetAsciiCode(eventArgs.KeyValue);

            int GetAsciiCode (int keyValue)
            {
                var keyState = new byte[256];
                GetKeyboardState(keyState);
                var asciiBuf = new byte[2];
                return ToAscii (keyValue, 0, keyState, asciiBuf, 0)
                            == 1 ? asciiBuf[0] : 0;
            }

            if (scanCode == 0x46 && asciiCode == 0x03)  // control-break
                machineObject.Stop();
            else                                        // any other key
                inputClient.KeyPress(scanCode, asciiCode);
        }

        // --------------------------------------------------------------------
        // OnKeyUp

        private void OnKeyUp (object sender, KeyEventArgs eventArgs)
        {
            int scanCode = MapVirtualKey(eventArgs.KeyValue, 0);
            inputClient.KeyRelease(scanCode);
        }

        // --------------------------------------------------------------------
        // external keyboard functions

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int MapVirtualKey (int uCode, int uMapType);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern int ToAscii (int uVirtKey, int uScanCode,
                                          byte[] lpbKeyState, byte[] lpChar,
                                          int uFlags);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool GetKeyboardState (byte[] lpbKeyState);

        // --------------------------------------------------------------------
        // machine thread entrypoint

        void ThreadMain ()
        {
            machineObject.Init(this);

            #if DEBUGGER

            new com.spaceflint.dbg.Debugger(machineObject);

            #else

            machineObject.Run();
            var mips = machineObject.LastRunCount
                     / machineObject.LastRunTime
                     / 1000000.0;
            MessageBox.Show(
                $"{machineObject.LastRunCount} instructions executed in " +
                $"{machineObject.LastRunTime} seconds,\nMIPS = {mips}");

            #endif

            Application.Exit();
        }

        // --------------------------------------------------------------------
        // IShell.Alert

        void IShell.Alert (string msg, bool abort)
        {
            // this method is invoked by the IMachine object,
            // on the machine thread

            System.Console.Write(msg);
            if (abort)
                MessageBox.Show(msg);

            if (abort)
            {
                System.Console.WriteLine("ABORT REQUEST");
                machineObject.Stop();
            }
        }

        // --------------------------------------------------------------------
        // IShell.ReadFile

        // byte[] IShell.ReadFile (string name) => System.IO.File.ReadAllBytes("dos/" + name);

        // --------------------------------------------------------------------
        // IShell.IInput.Register

        void IShell.IInput.Register (IShell.IInput.Client client)
        {
            inputClient = client;
        }

        // --------------------------------------------------------------------
        // IShell.Video and IShell.Timer

        IShell.IVideo IShell.Video => screenObject;
        IShell.IInput IShell.Input => this;

        // --------------------------------------------------------------------

        private IMachine machineObject;
        private Thread machineThread;
        private Screen screenObject;
        private IShell.IInput.Client inputClient;
    }
}

