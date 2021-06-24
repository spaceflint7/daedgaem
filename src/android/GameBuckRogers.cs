
using System;

namespace com.spaceflint
{

    public sealed class GameBuckRogers : Game
    {

        // --------------------------------------------------------------------
        // File properties

        public override string FileName => "Brpoz.exe";
        public override int    FileSize => 58176;
        public override string FileUrl  =>
            "https://www.abandonwaredos.com/abandonware-game.php?abandonware=Buck+Rogers%3A+Planet+of+Zoom&gid=1168#iDownload";

        public override string Help => "Swipe to move";

        // --------------------------------------------------------------------
        // Start

        public override void Start ()
        {
            touchInput.Reset();
            touchInput.OnMove += OnTouchMove_Game;
            touchInput.OnHold += OnTouchStop_Game;
            touchInput.OnStop += OnTouchStop_Game;
            touchInput.OnTap  += OnTouchTap_Game;
        }

        // --------------------------------------------------------------------
        // Hitpoint
        // below is the address for the intro screen for Buck Rogers,
        // but it is not used in this game runner.
        //public override int HitPointAddress => (0x1010 << 4) + 0x0708;

        // --------------------------------------------------------------------
        // OnTouchMove_Game

        private void OnTouchMove_Game (float x, float y)
        {
            if (Math.Abs(x) >= Math.Abs(y))
            {
                if (x < 0)
                    inputClient.KeyPress(0x4B, 0x34);  // keypad left
                else
                    inputClient.KeyPress(0x4D, 0x36);  // keypad right
            }
            else
            {
                if (y < 0)
                    inputClient.KeyPress(0x48, 0x38);  // keypad up
                else
                    inputClient.KeyPress(0x50, 0x32);  // keypad down
            }
        }

        // --------------------------------------------------------------------
        // OnTouchStop_Game

        private void OnTouchStop_Game ()
        {
            inputClient.KeyPress(0x39, 0x20);   // space
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Game

        private void OnTouchTap_Game (int fingers, bool doubleTap)
        {
            OnTouchStop_Game();
        }

    }
}
