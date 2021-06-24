
using System;
using Toast = android.widget.Toast;

namespace com.spaceflint
{

    public sealed class GameAlleyCat : Game
    {

        // --------------------------------------------------------------------
        // File properties

        public override string FileName => "cat.exe";
        public override int    FileSize => 55067;
        public override string FileUrl  =>
            "https://www.abandonwaredos.com/abandonware-game.php?abandonware=Alley+Cat&gid=45#iDownload";

        public override string Help => "Swipe to move\nTap for special action";

        // --------------------------------------------------------------------
        // Start

        public override void Start ()
        {
            // the game jumps to 1733:0081 after every game over.
            // however, difficulty can only be set before the first game,
            // and the intro screen starts the game on any key press.
            // therefore we don't need a hitpoint mechanism in this game.

            touchInput.Reset();
            touchInput.OnStop += OnTouchStop_Menu;
            touchInput.OnTap  += OnTouchTap_Menu;

            ShowToast();
        }

        // --------------------------------------------------------------------
        // ShowToast

        private void ShowToast ()
        {
            string title = level switch
            {
                3 => "Alley Cat",
                2 => "Tomcat",
                1 => "House Cat",
                _ => "Kitten",
            };
            var text = (java.lang.CharSequence) (object)
                                        $"Level {level + 1} selected: {title}";
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
            if ((level += 1) == 4)
                level = 0;
            ShowToast();
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Menu

        private void OnTouchTap_Menu (int fingers, bool doubleTap)
        {
            StartGame(1, 0);
        }

        // --------------------------------------------------------------------
        // StartGame

        private void StartGame (int step, int scanCode)
        {
            if (step == 1)
            {
                // send Enter to move past the intro animation
                scanCode = 0x1C;
            }
            else
            {
                // for step 2 and beyond, release the last key pressed
                inputClient.KeyRelease(scanCode);
            }

            if (step == 2)
            {
                // send response N for the joystick question
                scanCode = 0x31;
            }

            if (step == 3)
            {
                // send an response to the difficulty level
                scanCode = level switch
                {
                    3 => 0x1E,          // A for Alley Cat
                    2 => 0x14,          // T for Tomcat
                    1 => 0x23,          // H for House Cat
                    _ => 0x25,          // K for Kitten
                };
            }

            if (step == 4)
            {
                // send response to "press any key to start"
                scanCode = 0x1C;
            }

            if (step == 5)
            {
                EnterGame();
            }
            else
            {
                view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        StartGame(step + 1, scanCode))).AsInterface(), 100);
            }

            inputClient.KeyPress(scanCode, 0);
        }

        // --------------------------------------------------------------------
        // EnterGame

        private void EnterGame ()
        {
            touchInput.Reset();
            touchInput.OnMove += OnTouchMove_Game;
            touchInput.OnStop += OnTouchStop_Game;
            touchInput.OnTap  += OnTouchTap_Game;

            SendGameKeys();
        }

        // --------------------------------------------------------------------
        // OnTouchMove_Game

        private void OnTouchMove_Game (float x, float y)
        {
            if (x < 0)
                dirLeftRight = -1;
            else if (x > 0)
                dirLeftRight = 1;

            if (y < 0)
                dirUpDown = -1;
            else if (y > 0)
                dirUpDown = 1;
        }

        // --------------------------------------------------------------------
        // OnTouchStop_Game

        private void OnTouchStop_Game ()
        {
            dirLeftRight = 0;
            dirUpDown = 0;
        }

        // --------------------------------------------------------------------
        // OnTouchTap_Game

        private void OnTouchTap_Game (int fingers, bool doubleTap)
        {
            altPressed = true;
            inputClient.KeyPress(0x38, 0);  // left alt
        }

        // --------------------------------------------------------------------
        // SendGameKeys

        private void SendGameKeys ()
        {
            if (leftPressed && dirLeftRight != -1)
            {
                leftPressed = false;
                inputClient.KeyRelease(0x4B);  // keypad left
            }

            if (rightPressed && dirLeftRight != 1)
            {
                rightPressed = false;
                inputClient.KeyRelease(0x4D);  // keypad right
            }

            if (upPressed && dirUpDown != -1)
            {
                upPressed = false;
                inputClient.KeyRelease(0x48);  // keypad up
            }

            if (downPressed && dirUpDown != 1)
            {
                downPressed = false;
                inputClient.KeyRelease(0x50);  // keypad down
            }

            if (altPressed)
            {
                altPressed = false;
                inputClient.KeyRelease(0x38);  // left alt
            }

            if (dirLeftRight == -1 && (! leftPressed))
            {
                leftPressed = true;
                inputClient.KeyPress(0x4B, 0);  // keypad left
            }

            if (dirLeftRight == 1 && (! rightPressed))
            {
                rightPressed = true;
                inputClient.KeyPress(0x4D, 0);  // keypad right
            }

            if (dirUpDown == -1 && (! upPressed))
            {
                upPressed = true;
                inputClient.KeyPress(0x48, 0);  // keypad up
            }

            if (dirUpDown == 1 && (! downPressed))
            {
                downPressed = true;
                inputClient.KeyPress(0x50, 0);  // keypad down
            }

            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        SendGameKeys())).AsInterface(), 50);
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] Toast toast;
        [java.attr.RetainType] int level;
        [java.attr.RetainType] int dirLeftRight;
        [java.attr.RetainType] int dirUpDown;
        [java.attr.RetainType] bool leftPressed;
        [java.attr.RetainType] bool rightPressed;
        [java.attr.RetainType] bool altPressed;
        [java.attr.RetainType] bool upPressed;
        [java.attr.RetainType] bool downPressed;

    }
}
