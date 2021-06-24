
#if ! ANDROID

// provide minimal implementation for some Java stuff which
// is not available when compiling without the Android libs.

namespace java.attr
{
    // we use the Bluebonnet attribute [java.attr.RetainType]
    // in performance critical code

    public class RetainType : System.Attribute
    {
    }
}

namespace java.nio
{
    // we use ByteBuffer in video client code (x86/Cga.cs)
    // and the video shell (windows/Screen.cs)

    public abstract class ByteBuffer
    {
        public abstract ByteBuffer put(int index, sbyte value);
        public abstract ByteBuffer putInt(int index, int value);
        public abstract ByteBuffer putLong(int index, long value);
    }
}

#endif
