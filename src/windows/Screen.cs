
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using com.spaceflint.x86;

namespace com.spaceflint
{
    public class Screen : java.nio.ByteBuffer, IShell.IVideo
    {

        // --------------------------------------------------------------------
        // constructor

        public Screen (Form form, IMachine _machine)
        {
            InitBackBuffer(form);

            machine = _machine;

            stopwatch = System.Diagnostics.Stopwatch.StartNew();
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1;
            timer.Tick += TimerTick;
            timer.Start();
        }

        // --------------------------------------------------------------------
        // InitBackBuffer

        private void InitBackBuffer (Form form)
        {
            // create the back buffer with the size of the window
            backBufferRect = form.ClientRectangle;
            backBuffer = BufferedGraphicsManager.Current.Allocate(
                            form.CreateGraphics(), backBufferRect);
            graphics = backBuffer.Graphics;

            // request linear interpolation and disable anti-aliasing
            graphics.InterpolationMode =
                    System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            graphics.PixelOffsetMode =
                    System.Drawing.Drawing2D.PixelOffsetMode.Half;
            graphics.SmoothingMode =
                    System.Drawing.Drawing2D.SmoothingMode.None;
            graphics.TextRenderingHint =
                    System.Drawing.Text.TextRenderingHint.SingleBitPerPixel;

            // the following is required for correct handling of edges,
            // otherwise small images are not scaled correctly.
            imageAttrs = new ImageAttributes();
            imageAttrs.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
        }

        // --------------------------------------------------------------------
        // TimerTick

        private void TimerTick (object _sender, EventArgs _args)
        {
            // invoke timer client to process timer tick

            var currentTime = stopwatch.ElapsedMilliseconds;
            var deltaTime = (int) (currentTime - lastTimerTime);
            lastTimerTime = currentTime;

            // check if enough time has passed since last video update

            if ((timeSinceLastVideo += deltaTime) < videoRate)
                return;
            timeSinceLastVideo = deltaTime > videoRate
                               ? deltaTime - videoRate : 0;

            lock (_lock)
            {
                if (bitmap is not null)
                {
                    var (width, height) = (bitmap.Width, bitmap.Height);

                    var bmpData = bitmap.LockBits(
                                    new Rectangle(0, 0, width, height),
                                    System.Drawing.Imaging.ImageLockMode.ReadWrite,
                                    bitmap.PixelFormat);

                    bitmapScan0 = bmpData.Scan0;
                    bitmapLimit = width * height;
                    videoClient.Update(this);

                    bitmapScan0 = IntPtr.Zero;
                    bitmap.UnlockBits(bmpData);

                    graphics.Clear(Color.Black);
                    graphics.DrawImage(bitmap, backBufferRect, 0, 0, width, height,
                                       GraphicsUnit.Pixel, imageAttrs);
                    backBuffer.Render();
                }
            }
        }

        // --------------------------------------------------------------------
        // ByteBuffer.put

        public override java.nio.ByteBuffer put (int index, sbyte value)
        {
            // this method is called by IShell.Screen.Refresh,
            // to set pixels into the back buffer

            #if DEBUGGER
            if (    index < 0
                 || index > bitmapLimit - sizeof(byte))
            {
                throw new System.IndexOutOfRangeException(
                            $"bad screen byte index: {index}");
            }
            #endif

            System.Runtime.InteropServices.Marshal.WriteByte(
                                            bitmapScan0, index, (byte) value);
            return this;
        }

        // --------------------------------------------------------------------
        // ByteBuffer.putInt

        public override java.nio.ByteBuffer putInt (int index, int value)
        {
            // this method is called by IShell.Screen.Refresh,
            // to set pixels into the back buffer

            #if DEBUGGER
            if (    index < 0
                 || index > bitmapLimit - sizeof(int)
                 || (index & 3) != 0)
            {
                throw new System.IndexOutOfRangeException(
                            $"bad screen int index: {index}");
            }
            #endif

            System.Runtime.InteropServices.Marshal.WriteInt32(
                                            bitmapScan0, index, value);
            return this;
        }

        // --------------------------------------------------------------------
        // ByteBuffer.putLong

        public override java.nio.ByteBuffer putLong (int index, long value)
        {
            // this method is called by IShell.Screen.Refresh,
            // to set pixels into the back buffer

            #if DEBUGGER
            if (    index < 0
                 || index > bitmapLimit - sizeof(long)
                 || (index & 7) != 0)
            {
                throw new System.IndexOutOfRangeException(
                            $"bad screen long index: {index}");
            }
            #endif

            System.Runtime.InteropServices.Marshal.WriteInt64(
                                            bitmapScan0, index, value);
            return this;
        }

        // --------------------------------------------------------------------
        // IShell.IVideo.Mode

        void IShell.IVideo.Mode (IShell.IVideo.Client client)
        {
            lock (_lock)
            {
                if (    bitmap is null
                     || bitmap.Width != client.Width
                     || bitmap.Height != client.Height)
                {
                    if (bitmap is not null)
                        bitmap.Dispose();

                    bitmap = new Bitmap(client.Width, client.Height,
                                        PixelFormat.Format8bppIndexed);
                }

                SetPalette(client.Palette);

                this.videoClient = client;
            }
        }

        // --------------------------------------------------------------------
        // UpdatePalette

        private void SetPalette (int[] clientPalette)
        {
            if (clientPalette is not null)
            {
                var bitmapPalette = bitmap.Palette;
                for (int i = 0; i < clientPalette.Length; i++)
                {
                    bitmapPalette.Entries[i] =
                        Color.FromArgb(   clientPalette[i]
                                        | unchecked ((int) 0xFF000000));
                }
                bitmap.Palette = bitmapPalette;
            }
        }

        // --------------------------------------------------------------------

        private object _lock = new object();

        private ImageAttributes imageAttrs;
        private BufferedGraphics backBuffer;
        private Rectangle backBufferRect;
        private Graphics graphics;

        private IMachine machine;
        private Bitmap bitmap;
        private IShell.IVideo.Client videoClient;
        private IntPtr bitmapScan0;
        private int bitmapLimit;

        private System.Windows.Forms.Timer timer;
        private System.Diagnostics.Stopwatch stopwatch;

        private long lastTimerTime;
        private int timeSinceLastVideo;
        private const int videoRate = 15;   // almost 1/60

    }
}
