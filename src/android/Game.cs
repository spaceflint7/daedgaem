
using System;
using View = android.view.View;

namespace com.spaceflint
{

    public abstract class Game : IShell.IInput
    {

        // --------------------------------------------------------------------
        // constructor

        public void Init (View view, TouchInput touchInput)
        {
            this.view = view;
            this.touchInput = touchInput;
        }

        // --------------------------------------------------------------------
        // Start

        public abstract void Start ();

        // --------------------------------------------------------------------
        // File properties

        public abstract string FileName { get; }
        public abstract int    FileSize { get; }
        public abstract string FileUrl  { get; }

        public abstract string Help     { get; }

        // --------------------------------------------------------------------
        // Hitpoint

        public virtual void HitPointReached ()
        {
        }

        public virtual int HitPointAddress => -1;

        // --------------------------------------------------------------------
        // IShell.IInput.Register

        void IShell.IInput.Register (IShell.IInput.Client client)
        {
            inputClient = client;
        }

        // --------------------------------------------------------------------

        [java.attr.RetainType] protected IShell.IInput.Client inputClient;
        [java.attr.RetainType] protected TouchInput touchInput;
        [java.attr.RetainType] protected View view;

    }
}
