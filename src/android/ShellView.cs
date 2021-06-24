
using System;
using android.widget;

namespace com.spaceflint
{

    public sealed class ShellView : LinearLayout, java.lang.Runnable, IShell
    {

        // --------------------------------------------------------------------
        // constructor

        public ShellView (Activity activity, ScreenView screenView, Type gameClass)
            : base(activity)
        {
            this.activity = activity;
            this.screenObject = screenView;

            var touchInput = new TouchInput(activity);
            setOnTouchListener(touchInput);

            gameObject = (Game) Activator.CreateInstance(gameClass);
            gameObject.Init(this, touchInput);

            machineStoppedEvent = new android.os.ConditionVariable();
        }

        // --------------------------------------------------------------------
        // java.lang.Runnable.run - machine thread entrypoint

        [java.attr.RetainName]
        public void run ()
        {
            if (machineObject is null)
                InitMachine();

            var screenError = screenObject.WaitForResume();
            if (screenError is not null)
            {
                ((IShell) this).Alert(screenError, true);
            }
            else
            {
                try
                {
                    machineObject.Run();
                }
                catch (Exception e)
                {
                    ((IShell) this).Alert(e.ToString(), true);
                }
            }

            // signal event to wake up the ActivityPause method
            machineStoppedEvent.open();
        }

        // --------------------------------------------------------------------
        // InitMachine

        private void InitMachine ()
        {
            machineObject = new com.spaceflint.x86.Machine();

            machineObject.InitObject = programBytes;
            machineObject.Init(this);
            programBytes = null;

            // set up a hitpoint, a callback that will be invoked by
            // the virtual machine when a specific address is reached

            if (gameObject.HitPointAddress != -1)
            {
                machineObject.HitPoint(gameObject.HitPointAddress, () =>
                {
                    // this callback is invoked on the machine thread,
                    // so post a runnable on the user interface thread,
                    // to invoke the callback on the Game object
                    post( ((java.lang.Runnable.Delegate) (() =>
                        gameObject.HitPointReached())).AsInterface());
                });
            }
        }

        // --------------------------------------------------------------------
        // ActivityResume

        public void ActivityResume ()
        {
            if (! activity.Downloader.ActivityResume())
            {
                if (machineObject is null)
                {
                    programBytes = activity.Downloader.Get(activity, gameObject);
                    if (programBytes is null)
                    {
                        var downloaderView =
                                activity.Downloader.Open(activity, this, gameObject);
                        this.addView(downloaderView);
                        return;
                    }

                    gameObject.Start();

                    new android.app.AlertDialog.Builder(activity)
                        .setPositiveButton(
                                    (java.lang.CharSequence) (object) "OK", null)
                        .setMessage((java.lang.CharSequence) (object)
                                        $"In this game:\n\n{gameObject.Help}")
                        .show();
                }

                (new java.lang.Thread(this)).start();
            }
        }

        // --------------------------------------------------------------------
        // ActivityPause

        public void ActivityPause ()
        {
            if (! activity.Downloader.ActivityPause())
            {
                machineObject?.Stop();
                machineStoppedEvent.block();    // wait for event
                machineStoppedEvent.close();    // reset the event
            }
        }

        // --------------------------------------------------------------------
        // IShell.Alert

        void IShell.Alert (string msg, bool abort)
        {
            // this method is invoked by the IMachine object,
            // on the machine thread

            if (abort)
                machineObject.Stop();

            if (msg != alertText)   // if not already showing the same message
            {
                alertText = msg;

                activity.runOnUiThread(((java.lang.Runnable.Delegate) ( () => {
                    new android.app.AlertDialog.Builder(activity)
                        .setOnDismissListener(
                            ((android.content.DialogInterface.OnDismissListener.Delegate)
                                    ((_) => { alertText = null; })).AsInterface())
                        .setPositiveButton(
                                    (java.lang.CharSequence) (object) "Close", null)
                        .setMessage((java.lang.CharSequence) (object) msg)
                        .show();
                })).AsInterface());
            }
        }

        // --------------------------------------------------------------------
        // IShell.Video

        IShell.IVideo IShell.Video => screenObject;
        IShell.IInput IShell.Input => gameObject;

        // --------------------------------------------------------------------

        private Activity activity;
        private IMachine machineObject;
        private ScreenView screenObject;
        private Game gameObject;
        private byte[] programBytes;
        private android.os.ConditionVariable machineStoppedEvent;
        private string alertText;

    }
}
