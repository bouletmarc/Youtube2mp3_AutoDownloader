using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using BrowserHistoryGatherer;
using Microsoft.Win32;
using CefSharp;
//using CefSharp.Example.Handlers;

namespace Youtube2MP3
{
    public partial class Form1 : Form
    {

        string YoutubeSearchString = "tube.com/watch";
        List<string> URL_List = new List<string>();
        List<string> URL_Done_List = new List<string>();
        string LastDoneURL = "";
        string YT2MP3 = "https://mp3y.download/fr/your-mp3-convert";
        bool IsDownloading = false;
        string DownloadedFilename = "";
        string DownloadsDirectoryPath = "C:\\Users\\" + Environment.UserName + "\\Downloads\\";
        int DownloadCountToday = 0;
        int DownloadCountFailed = 0;


        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        public Form1()
        {
            InitializeComponent();

            if (Directory.Exists(@"C:\Users\boule\Desktop\Musique 2022\"))
            {
                textBox1.Text = "C:\\Users\\boule\\Desktop\\Musique 2022\\";
            }

            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = 10000;

            comboBox1.SelectedIndex = 0;

            //Load URL done
            string ThisFileCheck = Application.StartupPath + "\\URLDone.txt";
            if (File.Exists(ThisFileCheck))
            {
                string[] AllLines = File.ReadAllLines(ThisFileCheck);
                foreach (string Line in AllLines)
                {
                    URL_Done_List.Add(Line);
                }

                if (URL_Done_List.Count >= 1)
                {
                    textBoxURL.Text = URL_Done_List[URL_Done_List.Count - 1];
                    comboBox1.SelectedIndex = 1;
                }

                label4.Text = "Downloaded: " + DownloadCountToday + "/" + URL_Done_List.Count + " (failed:" + DownloadCountFailed + ")";
            }
            else
            {
                File.Create(ThisFileCheck).Dispose();
            }
            //########

            chromiumWebBrowser1.LoadUrl(YT2MP3);
            chromiumWebBrowser1.DownloadHandler = new MyCustomDownloadHandler();

            myTimer.Start();

        }

        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            myTimer.Stop();
            if (comboBox1.SelectedIndex == 0)
            {
                GetURLList();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                textBox2.Text = "";
                URL_List.Clear();
                if (chromiumWebBrowser2.GetMainFrame().Url.Contains(YoutubeSearchString))
                {
                    URL_List.Add(chromiumWebBrowser2.GetMainFrame().Url);
                    AddText("URL: " + chromiumWebBrowser2.GetMainFrame().Url);
                }
                else
                {
                    AddText("Nothing found...");
                }
            }
            CheckThoseURL();

            //Perform Next Click
            if (comboBox1.SelectedIndex == 1)
            {
                SendShiftKeyEvent();
                Thread.Sleep(1000);
                textBoxURL.Text = chromiumWebBrowser2.GetMainFrame().Url;
            }

            myTimer.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveURLDone();
        }

        void SaveURLDone()
        {
            string[] AllLines = new string[URL_Done_List.Count];
            int i = 0;
            foreach (string Line in URL_Done_List)
            {
                AllLines[i] = Line;
                i++;
            }

            string ThisFileCheck = Application.StartupPath + "\\URLDone.txt";
            File.Create(ThisFileCheck).Dispose();
            File.WriteAllLines(ThisFileCheck, AllLines);
        }

        private void CheckThoseURL()
        {
            foreach (string Line in URL_List)
            {
                if (!IsURLDone(Line))
                {
                    //Downloading This URL
                    AddText("Downloading: " + Line);
                    chromiumWebBrowser1.LoadUrl(YT2MP3);
                    Thread.Sleep(1000);
                    while (chromiumWebBrowser1.IsLoading)
                    {
                        Thread.Sleep(1);
                        Application.DoEvents();
                    }
                    LastDoneURL = Line;
                    textBox2.Focus();
                    chromiumWebBrowser1.Focus();
                    chromiumWebBrowser1.GetBrowser().GetHost().SetFocus(true);
                    chromiumWebBrowser1.GetBrowser().GetHost().SendFocusEvent(true);
                    Thread.Sleep(50);
                    SendKeyEvent(Keys.Tab);
                    SendKeyEvent(Keys.Tab);
                    SendKeyEvent(Keys.Tab);
                    foreach (char ThisChar in LastDoneURL)
                    {
                        SendKeyCharEvent((Keys)ThisChar);
                    }
                    SendKeyEvent(Keys.Enter);

                    textBox2.AppendText("Waiting til ready");
                    int WaitingTime = 0;
                    while ((BrowserWidth() == 1338 || BrowserWidth() <= 720) && WaitingTime < 120)
                    {
                        textBox2.AppendText(".");
                        Thread.Sleep(1000);
                        Application.DoEvents();
                        WaitingTime++;
                    }
                    textBox2.AppendText(Environment.NewLine);

                    if (WaitingTime == 120)
                    {
                        AddText("Retrying file conversion...");
                        DownloadCountFailed++;
                        label4.Text = "Downloaded: " + DownloadCountToday + "/" + URL_Done_List.Count + " (failed:" + DownloadCountFailed + ")";
                        return;
                    }

                    //Click the download button
                    IsDownloading = true;
                    DownloadedFilename = "";
                    IEnumerable<Process> childfirst = ParentProcessUtilities.GetChildProcesses();
                    int ProcessChildCount = GetProcChildCount();
                    var script = @"
                                    document.getElementsByClassName('btn-primary')[0].click();
                                ";
                    chromiumWebBrowser1.ExecuteScriptAsyncWhenPageLoaded(script);
                    //Thread.Sleep(1000);

                    //Check for ads and close then, retry
                    textBox2.AppendText("Waiting til opened");
                    int SecondsWaited = 0;
                    while (GetProcChildCount() == ProcessChildCount && SecondsWaited < 6)
                    {
                        textBox2.AppendText(".");
                        Thread.Sleep(1000);
                        Application.DoEvents();
                        SecondsWaited++;
                    }
                    textBox2.AppendText(Environment.NewLine);


                    //Close Process
                    IEnumerable<Process> childnew = ParentProcessUtilities.GetChildProcesses();
                    AddText("Closing new processes..");
                    bool ClosedAds = false;
                    foreach (Process procnew in childnew)
                    {
                        bool found = false;
                        foreach (Process procfirst in childfirst)
                        {
                            if (procfirst.Id == procnew.Id) found = true;
                        }

                        if (!found && !ClosedAds)
                        {
                            //if (procnew.ProcessName != Process.GetCurrentProcess().ProcessName)
                            //{
                                Process.GetProcessById(procnew.Id).CloseMainWindow();
                                Process.GetProcessById(procnew.Id).Dispose();
                                Process.GetProcessById(procnew.Id).Close();
                                Process.GetProcessById(procnew.Id).Kill();
                                Process.GetCurrentProcess().CloseMainWindow();
                                AddText("Closed process PID: " + procnew.Id);
                                Console.WriteLine("Closed process PID: " + procnew.Id);
                                ClosedAds = true;
                            //}
                        }
                    }

                    //Restart Download
                    chromiumWebBrowser1.ExecuteScriptAsyncWhenPageLoaded(script);

                    int SecondsWaitedDownload = 0;
                    while (IsDownloading && SecondsWaitedDownload < 1200)
                    {
                        LoadLogs();
                        Thread.Sleep(100);
                        Application.DoEvents();
                    }

                    if (SecondsWaitedDownload >= 1200)
                    {
                        AddText("ERROR: Download not detected after 2mins!");
                        DownloadCountFailed++;
                        label4.Text = "Downloaded: " + DownloadCountToday + "/" + URL_Done_List.Count + " (failed:" + DownloadCountFailed + ")";
                        return;
                    }

                    if (DownloadedFilename != "")
                    {
                        //Cut and Paste the file to 'music' folder
                        byte[] AllByte = File.ReadAllBytes(DownloadsDirectoryPath + DownloadedFilename);
                        File.Create(@textBox1.Text + DownloadedFilename).Dispose();
                        File.WriteAllBytes(@textBox1.Text + DownloadedFilename, AllByte);
                        File.Delete(DownloadsDirectoryPath + DownloadedFilename);
                        AddText("File transfered!");
                        textBox3.AppendText(DownloadedFilename + Environment.NewLine);
                    }
                    else
                    {
                        AddText("ERROR: Downloaded filename is Null!");
                        DownloadCountFailed++;
                        label4.Text = "Downloaded: " + DownloadCountToday + "/" + URL_Done_List.Count + " (failed:" + DownloadCountFailed + ")";
                        return;
                    }

                    //Finished, add to Done list
                    URL_Done_List.Add(Line);
                    DownloadCountToday++;
                    label4.Text = "Downloaded: " + DownloadCountToday + "/" + URL_Done_List.Count + " (failed:" + DownloadCountFailed + ")";
                    SaveURLDone();
                }
            }
            URL_List.Clear();
        }


        void LoadLogs()
        {
            try
            {
                string SavePath = Application.StartupPath + "\\DownloadLogs.txt";
                if (File.Exists(SavePath))
                {
                    string[] AllText = File.ReadAllLines(SavePath);
                    foreach (string Line in AllText)
                    {
                        if (Line != string.Empty)
                        {
                            AddText(Line);
                        }
                        if (Line.Contains("Suggested FileName"))
                        {
                            DownloadedFilename = Line.Split(' ')[2];
                        }
                        if (Line.Contains("download has been finished"))
                        {
                            IsDownloading = false;
                        }
                        if (Line.Contains("(100%)")) //Current Download Speed: 0 mB/s (100%)
                        {
                            IsDownloading = false;
                            Thread.Sleep(1000);
                        }
                    }

                    File.WriteAllText(SavePath, "");
                }
            }
            catch
            {
                //AddText("Unable to read download logs. Retrying..");
            }
        }

        public int GetProcChildCount()
        {
            IEnumerable<Process> child = ParentProcessUtilities.GetChildProcesses();
            int ProcessChildCount = 0;
            foreach (Process proc in child)
            {
                ProcessChildCount++;
            }
            return ProcessChildCount;
        }

        public int BrowserWidth()
        {
            var task = chromiumWebBrowser1.EvaluateScriptAsync("(function() { var body = document.body, html = document.documentElement; return  Math.max( body.scrollHeight, body.offsetHeight, html.clientHeight, html.scrollHeight, html.offsetHeight ); })();");

            task.ContinueWith(t =>
            {
                if (!t.IsFaulted)
                {
                    var response = t.Result;
                    var EvaluateJavaScriptResult = response.Success ? (response.Result ?? "null") : response.Message;
                }
            });

            if (task.Result.Result == null) { return 0; }
            return Convert.ToInt32(task.Result.Result.ToString());
        }

        private bool IsURLDone(string ThisURL)
        {
            if (ThisURL.Contains("&"))
            {
                ThisURL = ThisURL.Split('&')[0];
            }

            foreach (string Line in URL_Done_List)
            {
                string CheckLine = Line;
                if (CheckLine.Contains("&"))
                {
                    CheckLine = CheckLine.Split('&')[0];
                }

                if (ThisURL == CheckLine) return true;
            }
            return false;
        }

        void SendKeyEvent(Keys ThisKey)
        {
            KeyEvent k = new KeyEvent();
            k.WindowsKeyCode = (int)ThisKey;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.KeyDown;
            chromiumWebBrowser1.GetBrowser().GetHost().SendKeyEvent(k);
            Thread.Sleep(10);
            k.Type = KeyEventType.KeyUp;
            chromiumWebBrowser1.GetBrowser().GetHost().SendKeyEvent(k);
            Thread.Sleep(10);
        }

        void SendKeyCharEvent(Keys ThisKey)
        {
            KeyEvent k = new KeyEvent();
            k.WindowsKeyCode = (int)ThisKey;
            k.FocusOnEditableField = true;
            k.IsSystemKey = false;
            k.Type = KeyEventType.Char;
            chromiumWebBrowser1.GetBrowser().GetHost().SendKeyEvent(k);
            Thread.Sleep(10);
        }

        void SendShiftKeyEvent()
        {
            AddText("Next video...");
            chromiumWebBrowser2.Focus();
            chromiumWebBrowser2.GetBrowser().GetHost().SetFocus(true);
            chromiumWebBrowser2.GetBrowser().GetHost().SendFocusEvent(true);
            SendKeys.Send("+{n}");
            Thread.Sleep(10);
        }

        private void chromiumWebBrowser1_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {
            //Console.WriteLine(chromiumWebBrowser1.IsLoading);
            if (!chromiumWebBrowser1.IsLoading)
            {
                //Console.WriteLine("performed tab key");
                /*chromiumWebBrowser1.GetBrowser().GetHost().SendFocusEvent(true);
                Thread.Sleep(50);

                SendKeyEvent(Keys.Tab);
                SendKeyEvent(Keys.Tab);
                SendKeyEvent(Keys.Tab);
                SendKeyCharEvent(Keys.T);*/
            }
        }

        void AddText(string ThisText)
        {
            textBox2.AppendText(ThisText + Environment.NewLine);
        }

        void GetURLList()
        {
            URL_List.Clear();
            textBox2.Text = "";

            //1Minute ago
            DateTime TestDt = DateTime.Now - new TimeSpan(0, 5, 0);
            var history = BrowserHistory.GetHistory(Browser.Firefox, TestDt);
            int donecount = 0;

            foreach (var entry in history)
            {
                if (entry.Uri.ToString().Contains(YoutubeSearchString))
                {
                    URL_List.Add(entry.Uri.ToString());
                    AddText("Found URL: " + entry.Uri.ToString());
                }
                //Console.WriteLine(entry.Uri);
                //Console.WriteLine(entry.Title);
                //Console.WriteLine(entry.LastVisitTime);

                donecount++;
                if (donecount > 20) return;
            }

            if (history.Count == 0)
            {
                AddText("Nothing found...");
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox2.Text = "";
            AddText("Chanded mode: " + comboBox1.Text);
            if (comboBox1.SelectedIndex == 0)
            {
                myTimer.Stop();
                textBoxURL.Enabled = false;
                this.Size = new Size(512, 597);
                chromiumWebBrowser2.Visible = false;
                textBox3.Visible = false;
                myTimer.Start();
            }
            else if (comboBox1.SelectedIndex == 1)
            {
                myTimer.Stop();
                textBoxURL.Enabled = true;
                this.Size = new Size(982, 597);
                chromiumWebBrowser2.Visible = true;
                textBox3.Visible = true;
                if (textBoxURL.Text != string.Empty)
                {
                    chromiumWebBrowser2.LoadUrl(textBoxURL.Text);
                }
                else
                {
                    chromiumWebBrowser2.LoadUrl("https://www.youtube.com/");
                }
                myTimer.Start();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (textBoxURL.Text != string.Empty)
            {
                chromiumWebBrowser2.LoadUrl(textBoxURL.Text);
            }
        }

        //#######################################################
        class MyCustomDownloadHandler : IDownloadHandler
        {
            public event EventHandler<DownloadItem> OnBeforeDownloadFired;

            public event EventHandler<DownloadItem> OnDownloadUpdatedFired;

            void SaveLogs(string ThisText)
            {
                string SavePath = Application.StartupPath + "\\DownloadLogs.txt";
                bool Done = false;
                while (!Done)
                {
                    try
                    {
                        if (!File.Exists(SavePath)) File.Create(SavePath).Dispose();
                        File.AppendAllText(SavePath, ThisText + Environment.NewLine);
                        Done = true;
                    }
                    catch { }
                }
            }

            public void OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
            {
                if (downloadItem.IsValid)
                {
                    SaveLogs("== File information ========================");
                    SaveLogs("File URL: " + downloadItem.Url);
                    SaveLogs("Suggested FileName: " + downloadItem.SuggestedFileName);
                    //SaveLogs("MimeType: " + downloadItem.MimeType);
                    //SaveLogs("Content Disposition: " + downloadItem.ContentDisposition);
                    SaveLogs("Total Size: " + (downloadItem.TotalBytes / 1000000));
                    SaveLogs("============================================");
                }

                OnBeforeDownloadFired?.Invoke(this, downloadItem);

                if (!callback.IsDisposed)
                {
                    using (callback)
                    {
                        string DownloadsDirectoryPath = "C:\\Users\\" + Environment.UserName + "\\Downloads\\";

                        callback.Continue(
                            Path.Combine(
                                DownloadsDirectoryPath,
                                downloadItem.SuggestedFileName
                            ),
                            showDialog: false
                        );
                    }
                }
            }

            public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
            {
                OnDownloadUpdatedFired?.Invoke(this, downloadItem);

                if (downloadItem.IsValid)
                {
                    // Show progress of the download
                    if (downloadItem.IsInProgress && (downloadItem.PercentComplete != 0))
                    {
                        SaveLogs("Current Download Speed: " + (downloadItem.CurrentSpeed / 1000000) + " mB/s (" + downloadItem.PercentComplete + "%)");
                    }

                    if (downloadItem.IsComplete)
                    {
                        SaveLogs("The download has been finished !");
                    }
                }
            }
        }
    }
}
