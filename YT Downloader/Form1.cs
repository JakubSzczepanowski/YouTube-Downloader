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
        private DownloadEngine downloadEngine;
        private CancellationTokenSource cancellationTokenSource;
        public Form1()
        {
            InitializeComponent();
            cd = new combineDialog();
            downloadEngine = new DownloadEngine();
            downloadEngine.OnProgressChange += changeProgressBarValue;
            downloadEngine.OnDownloadStateChange += changeButtonDisabledState;
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void changeProgressBarValue(object sender, DownloadEngine.ProgressBarArgs e)
        {
            if (progressBar1.InvokeRequired)
            {
                Action action = delegate { changeProgressBarValue(sender, e); };
                progressBar1.Invoke(action);
            }
            else
            {
                progressBar1.Value = e.NewProgress;
            }
        }

        private void changeButtonDisabledState(object sender, DownloadEngine.DownloadStateArgs e)
        {
            if(button1.InvokeRequired)
            {
                Action action = delegate { changeButtonDisabledState(sender, e); };
                button1.Invoke(action);
            }
            else
            {
                button1.Enabled = !e.NewDownloadState;
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
                        YouTubeVideo video = videos.FirstOrDefault(x => x.Resolution == resolution && x.Format == VideoFormat.Mp4 && x.AudioBitrate != -1);
                        if(video == null && cd.ShowDialog() == DialogResult.Yes)
                        {
                            video = videos.FirstOrDefault(x => x.Resolution == resolution && x.Format == VideoFormat.Mp4);
                            YouTubeVideo audio = videos.FirstOrDefault(x => x.AudioFormat == AudioFormat.Aac && x.AdaptiveKind == AdaptiveKind.Audio);

                            if(video == null || audio == null)
                            {
                                MessageBox.Show("Nie znaleziono ścieżek", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                return;
                            }

                            try
                            {
                                await Task.Run(() => downloadEngine.DownloadFile(saveFileDialog1.FileName + ".mp4", video.Uri, cancellationTokenSource.Token), cancellationTokenSource.Token);

                                await Task.Run(() => downloadEngine.DownloadFile(saveFileDialog1.FileName + ".m4a", video.Uri, cancellationTokenSource.Token), cancellationTokenSource.Token);
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
                            catch (AggregateException exception)
                            {
                                var exceptions = new List<string>();
                                exception.Handle(each =>
                                {
                                    exceptions.Add(each.Message);
                                    return true;
                                });
                                MessageBox.Show("Wystąpiły następujące wyjątki: " + string.Join(Environment.NewLine, exceptions), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                        else
                        {
                            try
                            {
                                await Task.Run(() => downloadEngine.DownloadFile(saveFileDialog1.FileName + ".mp4", video.Uri, cancellationTokenSource.Token), cancellationTokenSource.Token);
                                MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                            catch (AggregateException exception)
                            {
                                var exceptions = new List<string>();
                                exception.Handle(each =>
                                {
                                    exceptions.Add(each.Message);
                                    return true;
                                });
                                MessageBox.Show("Wystąpiły następujące wyjątki: " + string.Join(Environment.NewLine, exceptions), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                IEnumerable<YouTubeVideo> videos = await youtube.GetAllVideosAsync(textBox1.Text);
                saveFileDialog1.Filter = "Pliki AAC (*.m4a)|*.m4a|Wszystkie pliki (*.*)|*.*";
                if(saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    YouTubeVideo audio = videos.FirstOrDefault(x => x.AudioFormat == AudioFormat.Aac && x.AdaptiveKind == AdaptiveKind.Audio);
                    try
                    {
                        try
                        {
                            await Task.Run(() => downloadEngine.DownloadFile(saveFileDialog1.FileName + ".m4a", audio.Uri, cancellationTokenSource.Token), cancellationTokenSource.Token);
                            MessageBox.Show("Pobieranie zakończone", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        catch (AggregateException exception)
                        {
                            var exceptions = new List<string>();
                            exception.Handle(each =>
                            {
                                exceptions.Add(each.Message);
                                return true;
                            });
                            MessageBox.Show("Wystąpiły następujące wyjątki: " + string.Join(Environment.NewLine, exceptions), "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
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

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            cancellationTokenSource.Cancel();
        }
    }
}
