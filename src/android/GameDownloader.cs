
using System;
using System.Collections.Generic;
using android.view;
using android.webkit;

namespace com.spaceflint
{

    public sealed class GameDownloader
    {

        // --------------------------------------------------------------------
        // Open

        public View Open (Activity activity, ShellView shellView, Game gameObject)
        {
            var monitor = new Monitor(activity, shellView, gameObject);
            webView = new WebView(activity);
            webView.setDownloadListener(monitor);
            webView.setWebViewClient(monitor);
            webView.getSettings().setJavaScriptEnabled(true);
            webView.loadUrl(gameObject.FileUrl);
            ActivityResume();

            activity.runOnUiThread(((java.lang.Runnable.Delegate) ( () => {
                new android.app.AlertDialog.Builder(activity)
                    .setPositiveButton(
                                (java.lang.CharSequence) (object) "OK", null)
                    .setMessage((java.lang.CharSequence) (object) (
                        "Games are not distributed\n"
                      + "with this app.\n\n"
                      + "Follow the Download links\n"
                      + "to download the game."))
                    .show();
            })).AsInterface());

            return webView;
        }

        // --------------------------------------------------------------------
        // Close

        public void Close ()
        {
            if (webView is not null)
            {
                if (webView.getParent() is ViewGroup parent)
                    parent.removeView(webView);

                webView = null;
            }
        }

        // --------------------------------------------------------------------
        // Get

        public byte[] Get (Activity activity, Game gameObject)
        {
            if (! ProgramFiles.TryGetValue(gameObject.FileName, out var bytes))
            {
                try
                {
                    var stream = activity.openFileInput(gameObject.FileName);
                    if (stream.available() == gameObject.FileSize)
                    {
                        var bytesTemp = new byte[gameObject.FileSize];
                        stream.read((sbyte[]) (object) bytesTemp);

                        ProgramFiles.Add(gameObject.FileName, bytesTemp);
                        bytes = bytesTemp;
                    }
                }
                catch {}
            }
            return bytes;
        }

        // --------------------------------------------------------------------
        // ActivityPause

        public bool ActivityPause ()
        {
            if (webView is null)
                return false;
            webView?.pauseTimers();
            webView?.onPause();
            return true;
        }

        // --------------------------------------------------------------------
        // ActivityResume

        public bool ActivityResume ()
        {
            if (webView is null)
                return false;
            webView?.onResume();
            webView?.resumeTimers();
            return true;
        }

        // --------------------------------------------------------------------

        Dictionary<string, byte[]> ProgramFiles = new();
        WebView webView;

        // --------------------------------------------------------------------

        private class Monitor : WebViewClient, DownloadListener, java.lang.Runnable
        {

            // --------------------------------------------------------------------
            // constructor

            public Monitor (Activity activity, ShellView shellView, Game gameObject)
            {
                this.activity = activity;
                this.shellView = shellView;
                this.gameObject = gameObject;
            }

            // --------------------------------------------------------------------
            // android.webkit.WebViewClient.shouldOverrideUrlLoading

            [java.attr.RetainName]
            public override bool shouldOverrideUrlLoading (WebView view, string url)
                => false;

            // --------------------------------------------------------------------
            // android.webkit.DownloadListener.onDownloadStart

            [java.attr.RetainName]
            public void onDownloadStart (string url, string userAgent,
                                         string contentDisposition,
                                         string mimetype, long contentLength)
            {
                if (downloadUrl is null)
                {
                    downloadUrl = url;
                    cookies = CookieManager.getInstance().getCookie(url);
                    (new java.lang.Thread(this)).start();
                }
            }

            // --------------------------------------------------------------------
            // java.lang.Runnable.run

            [java.attr.RetainName]
            public void run ()
            {
                var inputStream = OpenStream();
                if (inputStream is not null)
                {
                    var outputStream = ReadStream(inputStream);
                    if (outputStream is not null)
                    {
                        activity.runOnUiThread(((java.lang.Runnable.Delegate)
                            (() => WriteStream(outputStream))).AsInterface());
                        return;
                    }
                }

                Error();
            }

            // --------------------------------------------------------------------
            // OpenStream

            private java.io.InputStream OpenStream ()
            {
                var conn = new java.net.URL(downloadUrl).openConnection();
                conn.addRequestProperty("Cookie", cookies);
                conn.connect();

                var zip = new java.util.zip.ZipInputStream(conn.getInputStream());
                for (;;)
                {
                    var entry = zip.getNextEntry();
                    if (entry is null)
                        break;

                    if (entry.getSize() == gameObject.FileSize)
                    {
                        var fileName = entry.getName();
                        int idx = fileName.LastIndexOf('/');
                        if (idx != -1)
                            fileName = fileName.Substring(idx + 1);

                        if (fileName == gameObject.FileName)
                            return zip;
                    }
                }
                return null;
            }

            // --------------------------------------------------------------------
            // ReadStream

            private java.io.ByteArrayOutputStream ReadStream (java.io.InputStream inputStream)
            {
                var outputStream = new java.io.ByteArrayOutputStream();
                var tmp = new sbyte[4096];
                for (;;)
                {
                    int n = inputStream.read(tmp, 0, 4096);
                    if (n > 0)
                        outputStream.write(tmp, 0, n);
                    else
                        break;
                }

                if (outputStream.size() != gameObject.FileSize)
                    outputStream = null;

                return outputStream;
            }

            // --------------------------------------------------------------------
            // WriteStream

            private void WriteStream (java.io.ByteArrayOutputStream outputStream)
            {
                var fileStream = activity.openFileOutput(gameObject.FileName, 0);
                outputStream.writeTo(fileStream);
                fileStream.close();

                activity.Downloader.ProgramFiles.Add(
                    gameObject.FileName,
                    (byte[]) (object) outputStream.toByteArray());

                activity.Downloader.Close();
                shellView.ActivityResume();
            }

            // --------------------------------------------------------------------
            // Error

            private void Error ()
            {
                activity.runOnUiThread(((java.lang.Runnable.Delegate) ( () => {

                    var stream = activity.getAssets().open($"Error.png");
                    var imageView = new android.widget.ImageView(activity);
                    imageView.setImageBitmap(
                            android.graphics.BitmapFactory.decodeStream(stream));
                    imageView.setScaleType(android.widget.ImageView.ScaleType.FIT_XY);

                    new android.app.AlertDialog.Builder(activity)
                        .setOnDismissListener(
                            ((android.content.DialogInterface.OnDismissListener.Delegate)
                                    ((_) => activity.onBackPressed())).AsInterface())
                        .setView(imageView)
                        .show();
                })).AsInterface());
            }

            // --------------------------------------------------------------------

            private Activity activity;
            private ShellView shellView;
            private Game gameObject;
            private string downloadUrl;
            private string cookies;

        }

    }
}
