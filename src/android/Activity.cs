
using System;

namespace com.spaceflint
{

    public sealed class Activity : android.app.Activity
    {

        // --------------------------------------------------------------------
        // onCreate

        protected override void onCreate (android.os.Bundle savedInstanceState)
        {
            base.onCreate(savedInstanceState);

            // set screen flags

            if (android.os.Build.VERSION.SDK_INT >= 28)
            {
                var layoutParams = getWindow().getAttributes();
                layoutParams.layoutInDisplayCutoutMode =
                                android.view.WindowManager.LayoutParams
                                    .LAYOUT_IN_DISPLAY_CUTOUT_MODE_SHORT_EDGES;
                getWindow().setAttributes(layoutParams);
            }

            getWindow().addFlags(
                    android.view.WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON);

            // set content views

            setContentView(screenView = new ScreenView(this));

            if (GameSelectView.GameTypeFromSavedInstance(savedInstanceState)
                    is Type gameTypeToRestore)
            {
                OnGameSelect(gameTypeToRestore);
            }
            else
            {
                SetCurrentView(new GameSelectView(this, OnGameSelect));
            }
        }

        // --------------------------------------------------------------------
        // onWindowFocusChanged

        public override void onWindowFocusChanged (bool hasFocus)
        {
            base.onWindowFocusChanged(hasFocus);

            if (hasFocus)
            {
                getWindow().getDecorView().setSystemUiVisibility(
                          android.view.View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                        | android.view.View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                        | android.view.View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                        | android.view.View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                        | android.view.View.SYSTEM_UI_FLAG_FULLSCREEN
                        | android.view.View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY);
            }
        }

        // --------------------------------------------------------------------
        // onResume

        protected override void onResume ()
        {
            screenView.onResume();
            shellView?.ActivityResume();
            base.onResume();
        }

        // --------------------------------------------------------------------
        // onPause

        protected override void onPause ()
        {
            screenView.onPause();
            shellView?.ActivityPause();
            base.onPause();
        }

        // --------------------------------------------------------------------
        // onBackPressed

        public override void onBackPressed ()
        {
            if (shellView is null)
            {
                finish();
            }
            else
            {
                shellView.ActivityPause();
                Downloader?.Close();

                shellView = null;
                screenView.Reset();

                SetCurrentView(new GameSelectView(this, OnGameSelect));
            }
        }

        // --------------------------------------------------------------------
        // OnGameSelect

        private void OnGameSelect (Type gameClass)
        {
            bool calledFromOnCreate = currentView is null;
            this.gameClass = gameClass;

            if (Downloader is null)
                Downloader = new GameDownloader();

            SetCurrentView(shellView = new ShellView(this, screenView, gameClass));

            // if we are called from onCreate, Android will call onResume
            if (! calledFromOnCreate)
                shellView.ActivityResume();
        }

        // --------------------------------------------------------------------
        // SetCurrentView

        private void SetCurrentView (android.view.View view)
        {
            if (currentView is not null)
            {
                ((android.view.ViewGroup) currentView.getParent()).removeViewAt(1);
            }

            addContentView(view, new android.view.ViewGroup.LayoutParams(
                android.view.ViewGroup.LayoutParams.MATCH_PARENT,
                android.view.ViewGroup.LayoutParams.MATCH_PARENT));

            currentView = view;
        }

        // --------------------------------------------------------------------

        public GameDownloader Downloader;

        ScreenView screenView;
        ShellView shellView;
        Type gameClass;
        android.view.View currentView;

    }
}
