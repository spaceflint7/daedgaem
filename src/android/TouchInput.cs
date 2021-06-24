
using System;
using android.view;

namespace com.spaceflint
{

    public sealed class TouchInput : View.OnTouchListener
    {

        // --------------------------------------------------------------------
        // callback delegates

        public delegate void MoveDelegate (float x, float y);
        public delegate void StopDelegate ();
        public delegate void TapDelegate (int fingers, bool doubleTap);

        // OnMove reports finger movement, but only if the primary finger
        // moved at least some minimum distance (1/4 of an inch)
        public MoveDelegate OnMove;

        // OnHold is reported if OnMove was reported during the gesture,
        // and indicates that the primary finger is no longer moving
        public StopDelegate OnHold;

        // OnStop is reported if OnMove was reported during the gesture,
        // and indicates that the primary finger has been lifted.
        public StopDelegate OnStop;

        // OnTap is reported only if OnMove was never reported for the
        // gesture, and indicates a tap (or doubletap) with no movement
        public TapDelegate OnTap;

        // screen coordinates for last callback, in range 0..1
        public float X => previousX / width;
        public float Y => previousY / height;

        // --------------------------------------------------------------------
        // constructor

        public TouchInput (Activity activity)
        {
            // calculate minimum distance as one quarter of an inch in pixels.
            // this requires Android API 17.
            var metrics = new android.util.DisplayMetrics();
            activity.getWindowManager().getDefaultDisplay().getRealMetrics(metrics);
            minimumDistance = (int) ((metrics.xdpi + metrics.ydpi) * 0.5f / 4f);
            if (minimumDistance < 5)
                minimumDistance = 5;
        }

        // --------------------------------------------------------------------
        // Reset

        public void Reset ()
        {
            OnMove = null;
            OnHold = null;
            OnStop = null;
            OnTap = null;
        }

        // --------------------------------------------------------------------
        // onTouch

        [java.attr.RetainName]
        public bool onTouch (View view, MotionEvent motionEvent)
        {
            int action = motionEvent.getActionMasked();
            var time = android.os.SystemClock.uptimeMillis();

            width = view.getWidth();
            height = view.getHeight();

            if (    action == MotionEvent.ACTION_DOWN
                 || action == MotionEvent.ACTION_POINTER_DOWN
                 || action == MotionEvent.ACTION_MOVE)
            {
                if (primaryId == -1)
                {
                    HandleInitialPress(motionEvent, time);
                }
                else
                {
                    bool trackOngoingPress =
                            HandleOngoingPress(motionEvent, time);

                    if (trackOngoingPress)
                    {
                        TrackOngoingPress(view);
                    }
                }
            }
            else if (primaryId != -1)
            {
                bool cancel = (    action != MotionEvent.ACTION_UP
                                && action != MotionEvent.ACTION_POINTER_UP);

                HandleRelease(motionEvent, time, cancel);
            }

            return true;
        }

        // --------------------------------------------------------------------
        // HandleInitialPress

        private void HandleInitialPress (MotionEvent motionEvent, long time)
        {
            // we got motion while we are not tracking any finger,
            // so take this as an initial press event, and reset variables

            primaryId = motionEvent.getPointerId(0);
            previousX = motionEvent.getX(0);
            previousY = motionEvent.getY(0);
            secondFinger = motionEvent.getPointerCount() > 1;
            anyMovement = false;
            lastMoveTime = 0;
        }

        // --------------------------------------------------------------------
        // HandleOngoingPress

        private bool HandleOngoingPress (MotionEvent motionEvent, long time)
        {
            // we got a motion event, and we have to make sure that it
            // contains information for the finger we are already tracking

            bool trackOngoing = false;

            int pointerIndex = motionEvent.findPointerIndex(primaryId);
            if (pointerIndex != -1)
            {
                float x = motionEvent.getX(pointerIndex);
                float y = motionEvent.getY(pointerIndex);

                float deltaX = x - previousX;
                float deltaY = y - previousY;

                // check for some minimal movement on the x and y axes,
                // otherwise discard the movement on that axis

                if (Math.Abs(deltaX) >= minimumDistance)
                    previousX = x;
                else
                    deltaX = 0f;

                if (Math.Abs(deltaY) >= minimumDistance)
                    previousY = y;
                else
                    deltaY = 0f;

                // if there was at least a minimal movement, then report
                // an OnMove event

                var onMove = OnMove;
                if (deltaX != 0f || deltaY != 0f)
                {
                    trackOngoing = true;
                    anyMovement = true;
                    lastMoveTime = time;

                    if (onMove != null)
                        OnMove(deltaX / width, deltaY / height);
                }
            }

            if (motionEvent.getPointerCount() > 1)
                secondFinger = true;

            // the return value specifies whether to keep tracking the
            // movement to see if it becomes a finger hold gesture.
            return trackOngoing;
        }

        // --------------------------------------------------------------------
        // TrackOngoingPress

        private void TrackOngoingPress (View view, bool firstCall = true)
        {
            if (firstCall)
            {
                // called from onTouch to schedule the check for later.
                // if already scheduled, do nothing.

                if (trackOngoingScheduled)
                    return;
            }
            else
            {
                // we reach here for a delayed invocation via postDelayed.
                // first, indicate that we are no longer scheduled.
                trackOngoingScheduled = false;

                // if we are no longer tracking any fingers, stop tracking
                if (primaryId == -1)
                    return;

                // if too long has passed since the last move action,
                // report an OnHold event, i.e. a finger hold gesture,
                // then stop tracking

                var currentTime = android.os.SystemClock.uptimeMillis();
                if (currentTime - lastMoveTime >= MaxTrackingTimeInMs)
                {
                    var onHold = OnHold;
                    if (onHold != null)
                        onHold();

                    lastMoveTime = 0;
                    return;
                }
            }

            // otherwise, schedule tracking to occur in a short time,
            // and indicate that we are scheduled for later.

            view.postDelayed( ((java.lang.Runnable.Delegate) (() =>
                        TrackOngoingPress(view, false))).AsInterface(),
                        MaxTrackingTimeInMs);
            trackOngoingScheduled = true;
        }

        // --------------------------------------------------------------------
        // HandleRelease

        private void HandleRelease (MotionEvent motionEvent, long time, bool cancel)
        {
            int primaryId = this.primaryId;
            this.primaryId = -1;

            bool callOnStop = cancel;
            if (! callOnStop)
            {
                // we report an OnStop event, i.e. the end of the gesture,
                // but only (1) if the motion release event includes the
                // finger we are tracking, and (2) if at least one OnMove
                // event was reported (see also HandleOngoingPress)

                if (motionEvent.findPointerIndex(primaryId) != -1)
                    callOnStop = anyMovement;
            }

            if (callOnStop)
            {
                var onStop = OnStop;
                if (onStop != null)
                    onStop();
            }
            else
            {
                var onTap = OnTap;
                if (onTap != null)
                {
                    int fingers = secondFinger ? 2 : 1;
                    bool doubleTap = (time - lastTapTime < MaxDoubleTapTimeInMs);
                    onTap(fingers, doubleTap);
                }
                lastTapTime = time;
            }
        }

        // --------------------------------------------------------------------
        // fields

        private const int MaxTrackingTimeInMs = 120;
        private const int MaxDoubleTapTimeInMs = 250;
        // minimum distance before we recognize a move gesture
        [java.attr.RetainType] private readonly int minimumDistance = -1;

        // more than one finger may be touching, but we track only the first
        [java.attr.RetainType] private int primaryId = -1;
        // if a second finger participates in the gesture
        [java.attr.RetainType] private bool secondFinger;
        // if any non-minimal movement occurred since initial touch
        [java.attr.RetainType] private bool anyMovement;
        // time of last non-minimal movement since the initial touch
        [java.attr.RetainType] private long lastMoveTime;
        // time of last non-minimal movement since the initial touch
        [java.attr.RetainType] private long lastTapTime;
        // x and y values of previous touch
        [java.attr.RetainType] private float previousX;
        [java.attr.RetainType] private float previousY;
        // screen width and height values
        [java.attr.RetainType] private float width;
        [java.attr.RetainType] private float height;
        // if a call to TrackOngoingPress has already been posted
        [java.attr.RetainType] private bool trackOngoingScheduled;
    }

}
