using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VideoLibrary;

namespace YT_Downloader
{
    public partial class Form1 : Form
    {
        combineDialog cd;
        helpForm hf;
        aboutApp aa;
        public string command;
        public Form1()
        {
            InitializeComponent();
            cd = new combineDialog();
            
        }
        public delegate void SetValueCallback(int value);
        public delegate void SetStateCallback(bool state);
        public void setValue(int value)
        {
            if (progressBar1.InvokeRequired)
            {
                SetValueCallback d = new SetValueCallback(setValue);
                Invoke(d, new object[] { value });
            }
            else
            {
                progressBar1.Value = value;
            }
        }

        public void setState(bool state)
        {
            if (button1.InvokeRequired)
            {
                SetStateCallback d = new SetStateCallback(setState);
                Invoke(d,new object[] { state});
            }
            else
            {
                button1.Enabled = state;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var youtube = YouTube.Default;
            if (radioButton1.Checked)
            {
                saveFileDialog1.Filter = "Pliki MP4 (*.mp4)|*.mp4|Wszystkie pliki (*.*)|*.*";
                if(saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    short resolution = 360;
                    switch (comboBox1.SelectedItem)
                    {
                        case "144p":
                            resolution = 144;
                            break;
                        case "240p":
                            resolution = 240;
                            break;
                        case "360p":
                            resolution = 360;
                            break;
                        case "480p":
                            resolution = 480;
                            break;
                        case "720p":
                            resolution = 720;
                            break;
                        case "1080p":
                            resolution = 1080;
                            break;
                    }
                    try
                    {
                        IEnumerable<YouTubeVideo> videos = await youtube.GetAllVideosAsync(textBox1.Text);
                        try
                        {
                            YouTubeVideo video = videos.First(x => x.Resolution == resolution && x.Format == VideoFormat.Mp4 && x.AudioBitrate != -1);
                            setState(false);
                            new Thread(async () =>
                            {
                                var client = new HttpClient();
                                client.Timeout = TimeSpan.FromDays(1);
                                double? totalByte = 0;
                                using (Stream output = File.OpenWrite(saveFileDialog1.FileName))
                                {
                                    using (var request = new HttpRequestMessage(HttpMethod.Head, video.Uri))
                                    {
                                        totalByte = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
                                    }
                                    using (var input = await client.GetStreamAsync(video.Uri))
                                    {
                                        byte[] buffer = new byte[16 * 1024];
                                        int read;
                                        double totalRead = 0;
                                        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                                        {
                                            output.Write(buffer, 0, read);
                                            totalRead += read;
                                            int percentage = (int)((totalRead / totalByte) * 100);
                                            setValue(percentage);
                                        }
                                        setValue(0);
                                        MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        client.Dispose();
                                        setState(true);
                                    }
                                }
                            }
                                ).Start();
                        }
                        catch (InvalidOperationException ex)
                        {
                            if (cd.ShowDialog() == DialogResult.Yes)
                            {
                                try
                                {
                                    YouTubeVideo video = videos.First(x => x.Resolution == resolution && x.Format == VideoFormat.Mp4);
                                    YouTubeVideo audio = videos.First(x => x.AudioFormat == AudioFormat.Aac && x.AdaptiveKind == AdaptiveKind.Audio);
                                    ManualResetEvent syncEvent = new ManualResetEvent(false);
                                    new Thread(async () =>
                                    {
                                        setState(false);
                                        var client = new HttpClient();
                                        client.Timeout = TimeSpan.FromDays(1);
                                        double? totalByte = 0;
                                        using (Stream output = File.OpenWrite(saveFileDialog1.FileName + ".mp4"))
                                        {
                                            using (var request = new HttpRequestMessage(HttpMethod.Head, video.Uri))
                                            {
                                                totalByte = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
                                            }
                                            using (var input = await client.GetStreamAsync(video.Uri))
                                            {
                                                byte[] buffer = new byte[16 * 1024];
                                                int read;
                                                double totalRead = 0;
                                                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                                                {
                                                    output.Write(buffer, 0, read);
                                                    totalRead += read;
                                                    int percentage = (int)((totalRead / totalByte) * 100);
                                                    setValue(percentage);
                                                }
                                                setValue(0);
                                                MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                client.Dispose();
                                            }
                                            syncEvent.Set();
                                        }
                                    }
                                ).Start();
                                    new Thread(async () =>
                                    {
                                        syncEvent.WaitOne();
                                        var client = new HttpClient();
                                        client.Timeout = TimeSpan.FromDays(1);
                                        double? totalByte = 0;
                                        using (Stream output = File.OpenWrite(saveFileDialog1.FileName + ".m4a"))
                                        {
                                            using (var request = new HttpRequestMessage(HttpMethod.Head, audio.Uri))
                                            {
                                                totalByte = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
                                            }
                                            using (var input = await client.GetStreamAsync(audio.Uri))
                                            {
                                                byte[] buffer = new byte[16 * 1024];
                                                int read;
                                                double totalRead = 0;
                                                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                                                {
                                                    output.Write(buffer, 0, read);
                                                    totalRead += read;
                                                    int percentage = (int)((totalRead / totalByte) * 100);
                                                    setValue(percentage);
                                                }
                                                setValue(0);
                                                MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                                client.Dispose();

                                                setState(true);

                                            }
                                        }
                                        if (MessageBox.Show("Oba pliki zostały pobrane. Połączyć?", "Połączyć?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                                        {
                                            string directoryPath = Path.Combine(Environment.CurrentDirectory, "ffmpeg");
                                            command = $"\"{directoryPath}\" -i \"{saveFileDialog1.FileName + ".mp4"}\" -i \"{saveFileDialog1.FileName + ".m4a"}\" -c copy -map 0:v:0 -map 1:a:0 \"{saveFileDialog1.FileName}\"";
                                            File.WriteAllText("temp.bat", command, new UTF8Encoding(false));
                                            ProcessStartInfo startInfo = new ProcessStartInfo();
                                            startInfo.FileName = "cmd.exe";
                                            startInfo.CreateNoWindow = true;
                                            startInfo.RedirectStandardInput = true;
                                            startInfo.RedirectStandardOutput = true;
                                            startInfo.UseShellExecute = false;
                                            using (Process process = Process.Start(startInfo))
                                            {
                                                process.StandardInput.Write("chcp 65001" + Environment.NewLine + "temp.bat" + Environment.NewLine);
                                                process.StandardInput.Flush();
                                                process.StandardInput.Close();
                                                process.WaitForExit();
                                            }
                                            File.Delete("temp.bat");
                                        }
                                    }
                                    ).Start();

                                }
                                catch (Exception exc)
                                { MessageBox.Show(exc.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else if(radioButton2.Checked)
            {
                saveFileDialog1.Filter = "Pliki AAC (*.m4a)|*.m4a|Wszystkie pliki (*.*)|*.*";
                if(saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    IEnumerable<YouTubeVideo> videos = await youtube.GetAllVideosAsync(textBox1.Text);
                    YouTubeVideo audio = videos.First(x => x.AudioFormat == AudioFormat.Aac && x.AdaptiveKind == AdaptiveKind.Audio);
                    try
                    {
                        new Thread(async () =>
                        {
                            setState(false);
                            var client = new HttpClient();
                            client.Timeout = TimeSpan.FromDays(1);
                            double? totalByte = 0;
                            using (Stream output = File.OpenWrite(saveFileDialog1.FileName))
                            {
                                using (var request = new HttpRequestMessage(HttpMethod.Head, audio.Uri))
                                {
                                    totalByte = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).Result.Content.Headers.ContentLength;
                                }
                                using (var input = await client.GetStreamAsync(audio.Uri))
                                {
                                    byte[] buffer = new byte[16 * 1024];
                                    int read;
                                    double totalRead = 0;
                                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                                    {
                                        output.Write(buffer, 0, read);
                                        totalRead += read;
                                        int percentage = (int)((totalRead / totalByte) * 100);
                                        setValue(percentage);
                                    }
                                    setValue(0);
                                    MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                    client.Dispose();
                                    setState(true);

                                }
                            }
                        }).Start();
                    }
                    catch (Exception exce)
                    {
                        MessageBox.Show(exce.Message, "Błąd", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void pomocToolStripMenuItem_Click(object sender, EventArgs e)
        {
            hf = new helpForm(this);
            hf.Show();
        }

        private void oProgramieToolStripMenuItem_Click(object sender, EventArgs e)
        {
            aa = new aboutApp();
            aa.Show();
        }
    }
}
