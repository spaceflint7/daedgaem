
using System;

namespace com.spaceflint
{

    public sealed class GamePitStop : Game
    {

        // --------------------------------------------------------------------
        // File properties

        public override string FileName => "pitstop2.exe";
        public override int    FileSize => 93398;
        public override string FileUrl  =>
            "https://www.abandonwaredos.com/abandonware-game.php?abandonware=Pitstop+2&gid=2581#iDownload";

        public override string Help => "Swipe to move";

        // --------------------------------------------------------------------
        // Start

        public override void Start ()
        {
            HitPointReached();
        }

        // --------------------------------------------------------------------
        // Hitpoint

        public override void HitPointReached ()
        {
            touchInput.Reset();

            // send key releases for any keys still held
            OnTouchStop_Game();

            // wait for intro animation to complete before sending keys
            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                    IntroAnimationDone())).AsInterface(), 3000);
        }

        public override int HitPointAddress => (0x1010 << 4) + 0x0253;

        // --------------------------------------------------------------------
        // IntroAnimationDone

        private void IntroAnimationDone ()
        {
            touchInput.OnTap += (_,_) =>
            {
                touchInput.Reset();

                // send space to advance past intro screen
                inputClient.KeyPress(0x39, 0x20);   // space

                if (! sentDisplayResponse)
                {
                    sentDisplayResponse = true;
                    view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                            SendDisplayResponse(0x39))).AsInterface(), 50);
                }
                else
                {
                    view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                            SendOnePlayer(0x39))).AsInterface(), 50);
                }
            };
        }

        // --------------------------------------------------------------------
        // SendDisplayResponse

        private void SendDisplayResponse (int scanCodeToRelease)
        {
            // first, release whichever key was last sent
            inputClient.KeyRelease(scanCodeToRelease);

            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
            {
                // then, send '2' (@) to select RGB mode (vs composite)
                inputClient.KeyPress(0x03, '2');

                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        SendOnePlayer(0x03))).AsInterface(), 50);

            })).AsInterface(), 50);
        }

        // --------------------------------------------------------------------
        // SendOnePlayer

        private void SendOnePlayer (int scanCodeToRelease)
        {
            // first, release whichever key was last sent
            inputClient.KeyRelease(scanCodeToRelease);

            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
            {
                // then, send '1' (!) to one player mode (vs two players)
                inputClient.KeyPress(0x02, '1');

                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                {
                    // release the '1' key
                    inputClient.KeyRelease(0x02);

                    touchInput.OnTap += SendLevel;

                })).AsInterface(), 50);
            })).AsInterface(), 50);
        }

        // --------------------------------------------------------------------
        // SendLevel

        private void SendLevel (int fingers, bool doubleTap)
        {
            if (touchInput.X >= 0.2f && touchInput.X <= 0.8f)
            {
                int level;
                if (touchInput.Y >= 0.43f && touchInput.Y <= 0.49f)
                    level = 1;
                else if (touchInput.Y >= 0.5f && touchInput.Y <= 0.56f)
                    level = 2;
                else if (touchInput.Y >= 0.59f && touchInput.Y <= 0.64f)
                    level = 3;
                else
                    return;

                touchInput.Reset();

                // send '1' or '2' or '3' to select level of play
                inputClient.KeyPress(0x01 + level, '0' + level);

                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                {
                    // release whichever key was pressed
                    inputClient.KeyRelease(0x01 + level);

                    view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                    {
                        // send Enter in response to player name
                        inputClient.KeyPress(0x1C, 0x0D);

                        view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        {
                            // release Enter key that was pressed
                            inputClient.KeyRelease(0x1C);

                            touchInput.OnTap += SendTrack;

                        })).AsInterface(), 50);

                    })).AsInterface(), 50);

                })).AsInterface(), 50);
            }
        }

        // --------------------------------------------------------------------
        // SendTrack

        private void SendTrack (int fingers, bool doubleTap)
        {
            if (touchInput.X >= 0.2f && touchInput.X <= 0.8f)
            {
                int track;
                if (touchInput.Y >= 0.39f && touchInput.Y <= 0.44f)
                    track = 1;
                else if (touchInput.Y >= 0.47f && touchInput.Y <= 0.52f)
                    track = 2;
                else if (touchInput.Y >= 0.54f && touchInput.Y <= 0.60f)
                    track = 3;
                else if (touchInput.Y >= 0.63f && touchInput.Y <= 0.68f)
                    track = 4;
                else
                    return;

                touchInput.Reset();

                // send '1' or '2' or '3' or '4' to select track
                inputClient.KeyPress(0x01 + track, '0' + track);

                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                {
                    // release whichever key was pressed
                    inputClient.KeyRelease(0x01 + track);

                    touchInput.OnTap += SendLaps;

                })).AsInterface(), 50);
            }
        }

        // --------------------------------------------------------------------
        // SendLaps

        private void SendLaps (int fingers, bool doubleTap)
        {
            if (touchInput.X >= 0.2f && touchInput.X <= 0.8f)
            {
                int laps;
                if (touchInput.Y >= 0.43f && touchInput.Y <= 0.49f)
                    laps = 3;
                else if (touchInput.Y >= 0.5f && touchInput.Y <= 0.56f)
                    laps = 6;
                else if (touchInput.Y >= 0.59f && touchInput.Y <= 0.64f)
                    laps = 9;
                else
                    return;

                touchInput.Reset();

                // send '3' or '6' or '9' to select number of laps
                inputClient.KeyPress(0x01 + laps, '0' + laps);

                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                {
                    // release whichever key was pressed
                    inputClient.KeyRelease(0x01 + laps);

                    // activate game play control
                    touchInput.OnMove += OnTouchMove_Game;
                    touchInput.OnStop += OnTouchStop_Game;
                    touchInput.OnTap  += OnTouchTap_Game;

                })).AsInterface(), 50);
            }
        }

        // --------------------------------------------------------------------
        // OnTouchMove_Game

        private void OnTouchMove_Game (float x, float y)
        {
            if (Math.Abs(x) >= Math.Abs(y))
            {
                if (x < 0)
                {
                    if (rightPressed)
                    {
                        rightPressed = false;
                        inputClient.KeyRelease(0x4D);       // keypad right
                    }
                    if (! leftPressed)
                    {
                        leftPressed = true;
                        inputClient.KeyPress(0x4B, 0x34);   // keypad left
                    }
                }
                else
                {
                    if (leftPressed)
                    {
                        leftPressed = false;
                        inputClient.KeyRelease(0x4B);       // keypad left
                    }
                    if (! rightPressed)
                    {
                        rightPressed = true;
                        inputClient.KeyPress(0x4D, 0x36);   // keypad right
                    }
                }
            }
            else
            {
                if (y < 0)
                {
                    if (downPressed)
                    {
                        downPressed = false;
                        inputClient.KeyRelease(0x50);       // keypad down
                    }
                    if (! upPressed)
                    {
                        upPressed = true;
                        inputClient.KeyPress(0x48, 0x38);   // keypad up
                    }
                }
                else
                {
                    if (upPressed)
                    {
                        upPressed = false;
                        inputClient.KeyRelease(0x48);       // keypad up
                    }
                    if (! downPressed)
                    {
                        downPressed = true;
                        inputClient.KeyPress(0x50, 0x32);   // keypad down
                    }
                }
            }
        }

        // --------------------------------------------------------------------
        // OnTouchStop_Game

        private void OnTouchStop_Game ()
        {
            if (leftPressed)
            {
                leftPressed = false;
                inputClient.KeyRelease(0x4B);   // keypad left
            }

            if (rightPressed)
            {
                rightPressed = false;
                inputClient.KeyRelease(0x4D);   // keypad right
            }

            if (upPressed)
            {
                upPressed = false;
                inputClient.KeyRelease(0x48);   // keypad up
            }

            if (downPressed)
            {
                downPressed = false;
                inputClient.KeyRelease(0x50);   // keypad down
            }
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Game

        private void OnTouchTap_Game (int fingers, bool doubleTap)
        {
            // send and release enter key
            inputClient.KeyPress(0x1C, 0x0D);

            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                    inputClient.KeyRelease(0x1C))).AsInterface(), 50);
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] bool sentDisplayResponse;
        [java.attr.RetainType] bool leftPressed;
        [java.attr.RetainType] bool rightPressed;
        [java.attr.RetainType] bool upPressed;
        [java.attr.RetainType] bool downPressed;

    }
}
