
namespace com.spaceflint
{

    // --------------------------------------------------------------------
    // shell main interface

    public interface IShell
    {
        void Alert (string msg, bool abort = true);

        // byte[] ReadFile (string name);

        IVideo Video { get; }
        IInput Input { get; }

        // --------------------------------------------------------------------
        // shell video interface

        public interface IVideo
        {
            void Mode (Client client);

            // --------------------------------------------------------------------
            // shell video client

            public abstract class Client
            {
                public abstract int Width { get; }
                public abstract int Height { get; }
                public abstract int[] Palette { get; }
                public abstract void Update (java.nio.ByteBuffer buffer);
            }
        }

        // --------------------------------------------------------------------
        // shell input interface

        public interface IInput
        {

            void Register (Client client);

            // --------------------------------------------------------------------
            // shell input client

            public abstract class Client
            {
                public abstract void KeyPress (int scanCode, int asciiCode);

                public abstract void KeyRelease (int scanCode);
            }
        }

    }

}
