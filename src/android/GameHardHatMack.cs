
using System;
using Toast = android.widget.Toast;

namespace com.spaceflint
{

    public sealed class GameHardHatMack : Game
    {

        // --------------------------------------------------------------------
        // File properties

        public override string FileName => "hhm.com";
        public override int    FileSize => 42112;
        public override string FileUrl  =>
            "https://www.abandonwaredos.com/abandonware-game.php?abandonware=Hard+Hat+Mack&gid=2857#iDownload";

        public override string Help =>
            "Swipe to move\nTap to jump\nTap two fingers\nto release hammer";

        // --------------------------------------------------------------------
        // Start

        public override void Start ()
        {
        }

        // --------------------------------------------------------------------
        // Hitpoint

        public override void HitPointReached ()
        {
            // enter menu mode whenever the virtual machine reaches
            // the address below, which occurs during start up, and
            // after every game over animation
            EnterMenu();
        }

        public override int HitPointAddress => (0x1000 << 4) + 0x0B81;

        // --------------------------------------------------------------------
        // EnterMenu

        private void EnterMenu ()
        {
            touchInput.Reset();
            touchInput.OnStop += OnTouchStop_Menu;
            touchInput.OnTap  += OnTouchTap_Menu;

            if (level != 0)
                ShowToast();
        }

        // --------------------------------------------------------------------
        // ShowToast

        private void ShowToast ()
        {
            var text = (java.lang.CharSequence) (object)
                                        $"Level {level + 1} selected";
            if (toast == null)
                toast = Toast.makeText(view.getContext(), text, Toast.LENGTH_SHORT);
            else
                toast.setText(text);
            toast.show();
        }

        // --------------------------------------------------------------------
        // OnTouchStop_Menu

        private void OnTouchStop_Menu ()
        {
            if ((level += 1) == 3)
                level = 0;
            ShowToast();
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Menu

        private void OnTouchTap_Menu (int fingers, bool doubleTap)
        {
            int level = this.level;
            if (level > 0)
            {
                // if requested to start at levels 2 or 3,
                // send key 2(@)=0x03 or or 3(#)=0x04.
                SendScanCode(level + 2, true);
                // we need to wait a bit for key to register in the game
                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        SendScanCode(0x39, true))).AsInterface(), 200);
            }
            else
                SendScanCode(0x39, true);
            EnterGame();
        }

        // --------------------------------------------------------------------
        // EnterGame

        private void EnterGame ()
        {
            touchInput.Reset();
            touchInput.OnMove += OnTouchMove_Game;
            touchInput.OnHold += OnTouchStop_Game;
            touchInput.OnStop += OnTouchStop_Game;
            touchInput.OnTap  += OnTouchTap_Game;
        }

        // --------------------------------------------------------------------
        // OnTouchMove_Game

        private void OnTouchMove_Game (float x, float y)
        {
            int scanCode = 0;

            if (Math.Abs(x) >= Math.Abs(y))
            {
                if (x < 0f)
                    scanCode = 0x4B; // keypad left
                else if (x > 0f)
                    scanCode = 0x4D; // keypad right
            }
            else
            {
                if (y < 0f)
                    scanCode = 0x48; // keypad up
                else if (y > 0f)
                    scanCode = 0x50; // keypad down
            }

            if (scanCode != 0)
                SendScanCode(scanCode);
        }

        // --------------------------------------------------------------------
        // OnTouchStop_Game

        private void OnTouchStop_Game ()
        {
            lastScanCode = 0;
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Game

        private void OnTouchTap_Game (int fingers, bool doubleTap)
        {
            if (doubleTap)
                return;
            int scanCode;
            if (fingers == 1)
                scanCode = 0x39; // spacebar
            else if (fingers > 1)
                scanCode = 0x1C; // enter
            else
                return;
            SendScanCode(scanCode, true);
        }

        // --------------------------------------------------------------------
        // SendScanCode

        private void SendScanCode (int scanCode, bool dontCheck = false)
        {
            if (dontCheck || scanCode != lastScanCode)
            {
                lastScanCode = scanCode;
                inputClient.KeyPress(scanCode, 0);
                inputClient.KeyRelease(scanCode);
            }
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] int lastScanCode;
        [java.attr.RetainType] int level;
        [java.attr.RetainType] Toast toast;

    }
}
