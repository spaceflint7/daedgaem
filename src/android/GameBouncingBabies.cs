
using System;

namespace com.spaceflint
{

    public sealed class GameBouncingBabies : Game
    {

        // --------------------------------------------------------------------
        // File properties

        public override string FileName => "Bbabies.exe";
        public override int    FileSize => 37888;
        public override string FileUrl  =>
            "https://www.abandonwaredos.com/abandonware-game.php?abandonware=Bouncing+Babies&gid=1061#iDownload";

        public override string Help => "Tap to move";

        // --------------------------------------------------------------------
        // Start

        public override void Start ()
        {
            StartNewGame();
        }

        // --------------------------------------------------------------------
        // Hitpoint

        public override void HitPointReached ()
        {
            // enter menu mode whenever the virtual machine reaches
            // the address below, which occurs after game over
            StartNewGame();
        }

        public override int HitPointAddress => (0x1010 << 4) + 0x003B;

        // --------------------------------------------------------------------
        // StartNewGame

        private void StartNewGame()
        {
            touchInput.Reset();
            touchInput.OnTap  += (_,_) =>
            {
                // send spacebar to start the game
                inputClient.KeyPress(0x39, 0x20);
                inputClient.KeyRelease(0x39);

                // switch input to game mode
                touchInput.Reset();
                touchInput.OnHold += SendKey;
                touchInput.OnStop += SendKey;
                touchInput.OnTap  += (_,_) => SendKey();
            };
        }

        // --------------------------------------------------------------------
        // SendKey

        private void SendKey ()
        {
            int area = 0;
            if (touchInput.X < 0.35f)
                area = 1;
            else if (touchInput.X < 0.65f)
                area = 2;
            else if (touchInput.X < 0.85f)
                area = 3;

            if (area != 0)
            {
                // send scan code 02..04 and ascii '1'..'3'
                // according to the X coordinate tapped
                inputClient.KeyPress(area + 1, area + '0');
                inputClient.KeyRelease(area + 1);
            }
        }

    }
}
