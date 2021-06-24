
using android.opengl;
using GL10 = javax.microedition.khronos.opengles.GL10;
using EGLConfig = javax.microedition.khronos.egl.EGLConfig;

namespace com.spaceflint
{

    public sealed class ScreenView : GLSurfaceView, GLSurfaceView.Renderer,
                                     IShell.IVideo
    {

        // --------------------------------------------------------------------
        // constructor

        public ScreenView (Activity activity)
            : base(activity)
        {
            resumeSignal = new android.os.ConditionVariable();

            setEGLContextClientVersion(3); // OpenGL ES 3.0
            setEGLConfigChooser(/* needDepth */ false);
            setPreserveEGLContextOnPause(true);
            setRenderer(this);

            Reset();
        }

        // --------------------------------------------------------------------
        // Reset

        public void Reset ()
        {
            newClient = new NullClient();
        }

        // --------------------------------------------------------------------
        // onResume

        public override void onResume ()
        {
            resumeSignal.close();
            base.onResume();
        }

        // --------------------------------------------------------------------
        // WaitForResume

        public string WaitForResume ()
        {
            // following the call to onResume() on the activity thread,
            // this method is called on the machine thread to wait for
            // the resume processing to reach onSurfaceChanged(), after
            // optionally going through onSurfaceCreated().

            if (error is null)
            {
                // resume is signalled in onSurfaceChanged()
                if (! resumeSignal.block(5000))
                {
                    if (error is null)
                        error = "timeout in GL resume";
                }
            }
            return error;
        }

        // --------------------------------------------------------------------
        // IShell.IVideo.Mode

        void IShell.IVideo.Mode (IShell.IVideo.Client videoClient)
        {
            // indicate a request for a new client mode, to be detected
            // in onDrawFrame() the next time it runs
            newClient = videoClient;
        }

        // --------------------------------------------------------------------
        // GLSurfaceView.Renderer.onSurfaceCreated

        [java.attr.RetainName]
        public void onSurfaceCreated (GL10 unused, EGLConfig config)
        {
            // this method is called in the renderer thread after the
            // GL context is acquired.  this happens when GLSurfaceView
            // is created, or when the GL context was lost during pause.

            if (error is null)
            {
                // discard any queued errors before creating GL objects
                while (GLES20.glGetError() != 0)
                    ;
                // create shader program
                CreateShaderProgram();
                // create vertex buffer and enable attributes
                if (error is null)
                    CreateVertexBuffer();
                // create palette texture and update sampler
                if (error is null)
                {
                    CreateTexture(paletteTextureUnit,
                                  /* width, height */ 256, 1,
                                  GLES20.GL_RGBA, "palette");
                    if (palette is null)
                        palette = java.nio.ByteBuffer.allocateDirect(256 * 4);
                    else
                        UploadPalette();
                }
                // create videomem texture and update sampler
                if (error is null)
                {
                    var videoClientWidth  = videoClient?.Width  ?? 1;
                    var videoClientHeight = videoClient?.Height ?? 1;

                    CreateTexture(videomemTextureUnit,
                                  videoClientWidth, videoClientHeight,
                                  GLES20.GL_ALPHA, "videomem");
                    // at this point, videomemTextureUnit is active unit
                }
            }
        }

        // --------------------------------------------------------------------
        // GLSurfaceView.Renderer.onSurfaceChanged

        [java.attr.RetainName]
        public void onSurfaceChanged (GL10 unused, int width, int height)
        {
            // this method is called in the renderer thread during resume,
            // even if onSurfaceCreated was not called during resume,
            // so we always signal the resume condition here

            GLES20.glViewport(0, 0, width, height);
            resumeSignal.open();
        }

        // --------------------------------------------------------------------
        // GLSurfaceView.Renderer.onSurfaceChanged

        [java.attr.RetainName]
        public void onDrawFrame (GL10 unused)
        {
            // update video client if changed

            if (newClient is not null)
            {
                UpdateVideoClient();
            }

            // invoke video client to update screen

            if (error is null)
            {
                videoClient.Update(videomem);

                GLES20.glTexSubImage2D(GLES20.GL_TEXTURE_2D, /* level */ 0,
                                       /* xoffset, yoffset */ 0, 0,
                                       videoClient.Width, videoClient.Height,
                                       /* format */ GLES20.GL_ALPHA,
                                       /* type */ GLES20.GL_UNSIGNED_BYTE,
                                       /* data */ videomem);

                GLES20.glDrawArrays(GLES20.GL_TRIANGLES, 0, triangleCount);
            }
        }

        // --------------------------------------------------------------------
        // UpdateVideoClient

        private void UpdateVideoClient ()
        {
            if (error is null)
            {
                var (newWidth, newHeight) = (newClient.Width, newClient.Height);

                var videoClientWidth  = videoClient?.Width  ?? -1;
                var videoClientHeight = videoClient?.Height ?? -1;

                // modify the size of the videomem texture, if the new
                // client mode specifies different width or height

                if (newWidth != videoClientWidth || newHeight != videoClientHeight)
                {
                    videomem = java.nio.ByteBuffer.wrap(
                                        new sbyte[newWidth * newHeight])
                                .order(java.nio.ByteOrder.nativeOrder());

                    GLES20.glTexImage2D(GLES20.GL_TEXTURE_2D, /* level */ 0,
                                        /* internalFormat */ GLES20.GL_ALPHA,
                                        newWidth, newHeight, /* border */ 0,
                                        /* format */ GLES20.GL_ALPHA,
                                        /* type */ GLES20.GL_UNSIGNED_BYTE,
                                        /* pixel data */ null);
                }

                // update the palette texture, if the new client mode
                // specifies a palette array

                var clientPalette = newClient.Palette;
                if (clientPalette is not null)
                {
                    // input palette has red in the high bits, while OpenGL expects
                    // red as the first byte of each RGBA sequence, so fix the order.
                    palette.position(0);
                    for (int i = 0; i < clientPalette.Length; i++)
                    {
                        int rgb = clientPalette[i];
                        palette.put((sbyte) (rgb >> 16));    // red from high bits
                        palette.put((sbyte) (rgb >> 8));     // green from middle bits
                        palette.put((sbyte) rgb);            // blue from low bits
                        palette.put(unchecked ((sbyte) 0xFF));  // alpha bits
                    }

                    UploadPalette();
                }

                videoClient = newClient;
            }

            newClient = null;
        }

        // --------------------------------------------------------------------
        // UploadPalette

        private void UploadPalette ()
        {
            GLES20.glActiveTexture(paletteTextureUnit);

            GLES20.glTexSubImage2D(GLES20.GL_TEXTURE_2D, /* level */ 0,
                                   /* xoffset, yoffset */ 0, 0,
                                   /* width, height */ 256, 1,
                                   /* format */ GLES20.GL_RGBA,
                                   /* type */ GLES20.GL_UNSIGNED_BYTE,
                                   /* data */ palette.position(0));

            GLES20.glActiveTexture(videomemTextureUnit);
        }

        // --------------------------------------------------------------------
        // CreateTexture

        private void CreateTexture (int unit, int width, int height, int fmt,
                                    string samplerName)
        {
            string errText = null;
            GLES20.glActiveTexture(unit);
            int errCode = GLES20.glGetError();
            if (errCode != 0)
                errText = "glActiveTexture";
            else
            {
                int[] id = new int[1];
                GLES20.glGenTextures(1, id, 0);
                int textureId = id[0];

                errCode = GLES20.glGetError();
                if (textureId == 0 || errCode != 0)
                    errText = "glGenTextures";
                else
                {
                    GLES20.glBindTexture(GLES20.GL_TEXTURE_2D, textureId);
                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glBindTexture";
                    else
                    {
                        GLES20.glTexImage2D(GLES20.GL_TEXTURE_2D, /* level */ 0,
                                            /* internalFormat */ fmt,
                                            width, height, /* border */ 0,
                                            /* format */ fmt,
                                            /* type */ GLES20.GL_UNSIGNED_BYTE,
                                            /* pixel data */ null);
                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glTexImage2D";
                        else
                        {
                            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D,
                                                   GLES20.GL_TEXTURE_WRAP_S,
                                                   GLES20.GL_CLAMP_TO_EDGE);
                            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D,
                                                   GLES20.GL_TEXTURE_WRAP_T,
                                                   GLES20.GL_CLAMP_TO_EDGE);
                            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D,
                                                   GLES20.GL_TEXTURE_MIN_FILTER,
                                                   GLES20.GL_NEAREST);
                            GLES20.glTexParameteri(GLES20.GL_TEXTURE_2D,
                                                   GLES20.GL_TEXTURE_MAG_FILTER,
                                                   GLES20.GL_NEAREST);

                            errCode = GLES20.glGetError();
                            if (errCode != 0)
                                errText = "glTexParameteri";
                            else
                            {
                                GLES20.glGetIntegerv(GLES20.GL_CURRENT_PROGRAM, id, 0);
                                errCode = GLES20.glGetError();
                                if (errCode != 0)
                                    errText = "glGet";
                                else
                                {
                                    int location = GLES20.glGetUniformLocation(
                                                                id[0], samplerName);
                                    errCode = GLES20.glGetError();
                                    if (location == -1 || errCode != 0)
                                        errText = "glGetUniformLocation";
                                    else
                                    {
                                        GLES20.glUniform1i(location, unit - GLES20.GL_TEXTURE0);
                                        errCode = GLES20.glGetError();
                                        if (errCode != 0)
                                            errText = "glUniform1i";
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (errCode != 0)
                errText = "GL error " + errCode + ": " + errText;
            if (errText is not null)
                error = "in " + samplerName + " texture: " + errText;
        }

        // --------------------------------------------------------------------
        // CreateShaderProgram

        private void CreateShaderProgram ()
        {
            // this method runs in the renderer thread when GL surface/context
            // is created or re-created after the context was lost.

            int vertexId, fragmentId, programId;
            (vertexId, error) = CompileShader(
                    GLES20.GL_VERTEX_SHADER, "vertex", vertexText);

            if (error is null)
            {
                (fragmentId, error) = CompileShader(
                    GLES20.GL_FRAGMENT_SHADER, "fragment", fragmentText);

                if (error is null)
                {
                    (programId, error) = LinkProgram(vertexId, fragmentId);

                    if (error is null)
                    {
                        GLES20.glUseProgram(programId);
                        int errCode = GLES20.glGetError();
                        if (errCode != 0)
                            error = "GL error " + errCode + ": glUseProgram";
                    }
                }
            }

            //
            // compiler shader text
            //

            static (int, string) CompileShader (int kind, string errKind, string text)
            {
                string errText = null;
                int shaderId = GLES20.glCreateShader(kind);
                int errCode = GLES20.glGetError();
                if (shaderId == 0 || errCode != 0)
                    errText = "glCreateShader";
                else
                {
                    GLES20.glShaderSource(shaderId, text);
                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glShaderSource";
                    else
                    {
                        GLES20.glCompileShader(shaderId);
                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glCompileShader";
                        else
                        {
                            var status = new int[1];
                            GLES20.glGetShaderiv(
                                shaderId, GLES20.GL_COMPILE_STATUS, status, 0);
                            errCode = GLES20.glGetError();
                            if (errCode == 0 && status[0] != 0)
                            {
                                return (shaderId, null); // success
                            }
                            errText = "compile error: "
                                    + GLES20.glGetShaderInfoLog(shaderId);
                        }
                    }
                }
                if (errCode != 0)
                    errText = "GL error " + errCode + ": " + errText;
                errText = "in " + errKind + " shader: " + errText;
                return (0, errText);
            }

            //
            // link compiled shaders into a program
            //

            static (int, string) LinkProgram (int vertexId, int fragmentId)
            {
                string errText = null;
                int programId = GLES20.glCreateProgram();
                int errCode = GLES20.glGetError();
                if (programId == 0 || errCode != 0)
                    errText = "glCreateProgram";
                else
                {
                    GLES20.glAttachShader(programId, vertexId);
                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glAttachShader (vertex)";
                    else
                    {
                        GLES20.glAttachShader(programId, fragmentId);
                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glAttachShader (fragment)";
                        else
                        {
                            GLES20.glLinkProgram(programId);
                            errCode = GLES20.glGetError();
                            if (errCode != 0)
                                errText = "glLinkProgram";
                            else
                            {
                                var status = new int[1];
                                GLES20.glGetProgramiv(
                                    programId, GLES20.GL_LINK_STATUS, status, 0);
                                errCode = GLES20.glGetError();
                                if (errCode == 0 && status[0] != 0)
                                {
                                    return (programId, null); // success
                                }
                                errText = "link error: "
                                        + GLES20.glGetProgramInfoLog(programId);
                            }
                        }
                    }
                }
                if (errCode != 0)
                    errText = "GL error " + errCode + ": " + errText;
                errText = "in shader program: " + errText;
                return (0, errText);
            }
        }

        // --------------------------------------------------------------------
        // CreateVertexBuffer

        private void CreateVertexBuffer ()
        {
            string errText = null;

            int[] id = new int[1];
            GLES20.glGenBuffers(1, id, 0);
            int bufferId = id[0];

            int errCode = GLES20.glGetError();
            if (bufferId == 0 || errCode != 0)
                errText = "glGenBuffers";
            else
            {
                GLES20.glBindBuffer(GLES20.GL_ARRAY_BUFFER, bufferId);
                errCode = GLES20.glGetError();
                if (errCode != 0)
                    errText = "glBindBuffer";
                else
                {
                    var buffer = java.nio.FloatBuffer.wrap(vertexData);
                    GLES20.glBufferData(GLES20.GL_ARRAY_BUFFER,
                                        vertexData.Length * sizeof(float),
                                        buffer, GLES20.GL_STATIC_DRAW);

                    errCode = GLES20.glGetError();
                    if (errCode != 0)
                        errText = "glBufferData";
                    else
                    {
                        GLES20.glVertexAttribPointer(
                            /* index */ 0,
                            /* size */ floatsPerVertex,
                            /* data type */ GLES20.GL_FLOAT,
                            /* normalize data */ false,
                            /* stride */ 0, /* offset */ 0);

                        errCode = GLES20.glGetError();
                        if (errCode != 0)
                            errText = "glVertexAttribPointer";
                        else
                        {
                            GLES20.glEnableVertexAttribArray(0);
                            errCode = GLES20.glGetError();
                            if (errCode != 0)
                                errText = "glEnableVertexAttribArray";

                            else if (vertexData.Length !=
                                            triangleCount * floatsPerVertex)
                            {
                                errCode = -1;
                                errText = "invalid vertex data";
                            }
                        }
                    }
                }
            }

            if (errCode != 0)
                error = "GL error " + errCode + ": " + errText;
        }

        // --------------------------------------------------------------------
        // shader program text

        static string vertexText = @"#version 300 es
layout (location = 0) in vec2 quad;
out vec2 f_uv;
void main() {
    gl_Position = vec4(quad.x, quad.y, 0.0, 1.0);
    f_uv = vec2(quad.x + 1.0, 1.0 - quad.y) / vec2(2.0);
}";

        static string fragmentText = @"#version 300 es
precision mediump float;
in vec2 f_uv;
uniform sampler2D videomem;
uniform sampler2D palette;
out vec4 o_color;
void main() {
    o_color = texelFetch(palette, ivec2(int(texture(videomem, f_uv).a * 255.0), 0), 0);
}";
    // if texelFetch is not available:
    //float index = texture(videomem, f_uv).a * 255.0;
    //o_color = texture(palette, vec2((index + 0.5) / 256.0, 0.5));

        // --------------------------------------------------------------------
        // vertex data

        private const int triangleCount = 6;
        private const int floatsPerVertex = 2;

        static float[] vertexData = new float[] {
            /* top left */      -1f,  1f,
            /* lower right */    1f, -1f,
            /* upper right */    1f,  1f,
            /* top left */      -1f,  1f,
            /* lower right */    1f, -1f,
            /* lower left */    -1f, -1f,
        };

        // --------------------------------------------------------------------

        // texture constants
        private int paletteTextureUnit = GLES20.GL_TEXTURE1;
        private int videomemTextureUnit = GLES20.GL_TEXTURE2;

        private string error;
        private android.os.ConditionVariable resumeSignal;
        private IShell.IVideo.Client videoClient;
        private java.nio.ByteBuffer palette;
        private java.nio.ByteBuffer videomem;
        private volatile IShell.IVideo.Client newClient;

        // --------------------------------------------------------------------
        // NullClient

        private class NullClient : IShell.IVideo.Client
        {
            public override int Width  => 1;
            public override int Height => 1;
            public override int[] Palette => null;
            public override void Update (java.nio.ByteBuffer buffer) { }
        }

    }
}
