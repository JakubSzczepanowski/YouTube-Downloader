using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLibrary;

namespace YT_Downloader
{
    public class DownloadEngine
    {
        private readonly HttpClient client;

        public DownloadEngine()
        {
            client = new HttpClient();
            client.Timeout = TimeSpan.FromDays(1);
        }

        ~DownloadEngine()
        {
            client.Dispose();
        }

        public event EventHandler<ProgressBarArgs> OnProgressChange;

        public event EventHandler<DownloadStateArgs> OnDownloadStateChange;

        public bool IsDownloading
        {
            get
            {
                return _isDownloading;
            }
            set
            {
                if(value != _isDownloading)
                {
                    _isDownloading = value;
                    OnDownloadStateChange?.Invoke(this, new DownloadStateArgs(value));
                }
            }
        }

        public int DownloadingProgress
        {
            get
            {
                return _downloadingProgress;
            }
            set
            {
                if (value != _downloadingProgress)
                {
                    _downloadingProgress = value;
                    OnProgressChange?.Invoke(this, new ProgressBarArgs(value));
                }
            }
        }

        private int _downloadingProgress = 0;
        private bool _isDownloading = false;
        public async Task DownloadFile(string fileName, string uri, CancellationToken cancellationToken)
        {
            IsDownloading = true;
            double? totalByte = 0;
            using (Stream output = File.OpenWrite(fileName))
            {
                using (var request = new HttpRequestMessage(HttpMethod.Head, uri))
                {
                    HttpResponseMessage responseMessage = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    totalByte = responseMessage.Content.Headers.ContentLength;
                }
                using (var input = await client.GetStreamAsync(uri))
                {
                    byte[] buffer = new byte[16 * 1024];
                    int read;
                    double totalRead = 0;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0 && !cancellationToken.IsCancellationRequested)
                    {
                        output.Write(buffer, 0, read);
                        totalRead += read;
                        int percentage = (int)((totalRead / totalByte) * 100);
                        DownloadingProgress = percentage;
                    }
                    DownloadingProgress = 0;
                    IsDownloading = false;
                }
            }
        }

        public class ProgressBarArgs : EventArgs
        {
            public ProgressBarArgs(int newProgress)
            {
                NewProgress = newProgress;
            }
            public int NewProgress { get; set; }
        }

        public class DownloadStateArgs : EventArgs
        {
            public DownloadStateArgs(bool newDownloadState)
            {
                NewDownloadState = newDownloadState;
            }
            public bool NewDownloadState { get; set; }
        }
    }
}
