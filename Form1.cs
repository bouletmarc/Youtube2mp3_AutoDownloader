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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolBar;
using System.Runtime.InteropServices.ComTypes;

namespace Youtube2MP3
{
    public partial class Form1 : Form
    {

        string YoutubeSearchString = "tube.com/watch";
        List<string> URL_List = new List<string>();
        List<string> URL_Done_List = new List<string>();

        List<string> SongNames_List = new List<string>();
        List<string> SongNames_Done_List = new List<string>();
        string LastDoneURL = "";
        string YT2MP3 = "https://mp3y.download/fr/your-mp3-convert";
        bool IsDownloading = false;
        string DownloadedFilename = "";
        public string DownloadsDirectoryPath = "C:\\Users\\" + Environment.UserName + "\\Downloads\\";
        int DownloadCountToday = 0;
        int DownloadCountFailed = 0;
        bool IsRunning = false;
        int IntervalInSecondsBetweenNextSong = 5;
        bool RetryDownloadFailed = false;
        int RetryFailedCount = 0;
        bool IsLoadingSettings = true;


        static System.Windows.Forms.Timer myTimer = new System.Windows.Forms.Timer();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern short VkKeyScan(char ch);

        
        //####################################
        /*[DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;*/
        //####################################

        // Get a handle to an application window.
        [DllImport("USER32.DLL", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Activate an application window.
        [DllImport("USER32.DLL")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
        //####################################

        public Form1()
        {
            InitializeComponent();

            myTimer.Tick += new EventHandler(TimerEventProcessor);
            myTimer.Interval = IntervalInSecondsBetweenNextSong * 1000;

            textBox2.Text = "";
            textBox3.Visible = true;

            //Load URL done
            string ThisFileCheck = Application.StartupPath + "\\URLDone.txt";
            if (File.Exists(ThisFileCheck))
            {
                string[] AllLines = File.ReadAllLines(ThisFileCheck);
                foreach (string Line in AllLines)
                {
                    URL_Done_List.Add(removeListfromURL(Line));
                }

                SetDownloadCounts();

                AddText("Found " + labelTotal.Text + " already done downloads links!");
            }
            else
            {
                File.Create(ThisFileCheck).Dispose();
            }

            //Load name song list
            ThisFileCheck = Application.StartupPath + "\\SongNamesDone.txt";
            if (File.Exists(ThisFileCheck))
            {
                LoadMP3DoneList();
                AddText("Found " + SongNames_Done_List.Count + " already done song names!");
            }

            //Load Settings
            ThisFileCheck = Application.StartupPath + "\\Setting.txt";
            if (File.Exists(ThisFileCheck))
            {
                string[] AllLines = File.ReadAllLines(ThisFileCheck);
                if (AllLines.Length > 0)
                {
                    textBox1.Text = AllLines[0];
                }
                if (AllLines.Length > 1) 
                {
                    textBox4.Text = AllLines[1];
                    DownloadsDirectoryPath = textBox4.Text;
                }
                if (AllLines.Length > 2)
                {
                    numericUpDown1.Value = int.Parse(AllLines[2]);
                }
            }
            else
            {
                SaveSettings();
            }
            //########

            chromiumWebBrowser1.LoadUrl(YT2MP3);
            chromiumWebBrowser1.DownloadHandler = new MyCustomDownloadHandler();


            //myTimer.Start();
            CheckLocations();
            IsFirefoxRunning();

            IsLoadingSettings = false;
        }

        private string removeListfromURL(string ThisURLLi)
        {
            string returnURL = ThisURLLi;
            if (ThisURLLi.Contains("&list="))
            {
                returnURL = ThisURLLi.Substring(0, ThisURLLi.IndexOf("&list="));
            }
            if (returnURL == "https://www.youtube.com/watch?v=")
            {
                returnURL = ThisURLLi;
            }
            return returnURL;
        }

        private void SaveSettings()
        {
            string ThisFileCheck = Application.StartupPath + "\\Setting.txt";
            File.Create(ThisFileCheck).Dispose();
            File.WriteAllText(ThisFileCheck, textBox1.Text + Environment.NewLine + textBox4.Text + Environment.NewLine + numericUpDown1.Value.ToString());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (!IsRunning)
            {
                AddText("Bot will start in " + IntervalInSecondsBetweenNextSong.ToString() + " seconds!");
                myTimer.Start();
                button2.Text = "STOP";
                IsRunning = true;
            }
            else
            {
                myTimer.Stop();
                button2.Text = "START";
                IsRunning = false;

                if (RetryFailedCount > 0)
                {
                    RetryFailedCount = 0;
                }
            }
        }

        /*public void DoMouseClick()
        {
            //Call the imported function with the cursor's current position
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }*/

        private void CheckLocations()
        {
            if (textBox1.Text == "" || textBox4.Text == "")
            {
                button2.Enabled = false;
                AddText("Select path's before running bot...");
            }
            else
            {
                button2.Enabled = true;
            }
        }

        private void IsFirefoxRunning()
        {
            IntPtr ThisHandlee = FindWindow("MozillaWindowClass", null);
            if (ThisHandlee == IntPtr.Zero)
            {
                AddText("Firefox is not running...");
            }
        }

        private void FocusFirefox()
        {
            IntPtr ThisHandlee = FindWindow("MozillaWindowClass", null);
            if (ThisHandlee == IntPtr.Zero)
            {
                AddText("Firefox is not running...");
                return;
            }

            SetForegroundWindow(ThisHandlee);

            Thread.Sleep(250);
            UnfocusAddressbar();
        }

        private void UnfocusFirefox()
        {
            IntPtr ThisHandlee = this.Handle;
            if (ThisHandlee == IntPtr.Zero)
            {
                AddText("Youtube2MP3 is not running...");
                return;
            }

            SetForegroundWindow(ThisHandlee);

            Application.DoEvents();
            Thread.Sleep(250);
        }

        private void UnfocusAddressbar()
        {
            //make it pop the search bar (Ctrl+F), then close it
            SendKeys.Send("^{f}");
            Thread.Sleep(250);
            SendKeys.Send("{ESC}");
            Thread.Sleep(250);
        }

        private string getFirefoxURL()
        {
            FocusFirefox();

            SendKeys.Send(" ");   //send SPACE (in case the video is stopped and not playing)
            Thread.Sleep(250);
            SendKeys.Send("{F6}");  //send F6 (firefox shortcut to focus address bar)
            Thread.Sleep(250);
            SendKeys.Send("^{c}");  //send Ctrl+C (copy to clipboard shortcut)
            Thread.Sleep(250);
            string BuffStr = Clipboard.GetText();   //get clipboard data
            UnfocusFirefox();

            return BuffStr;
        }

        private string getFirefoxTitle()
        {
            string Tittle = "";
            try
            {
                System.Diagnostics.Process[] p = System.Diagnostics.Process.GetProcessesByName("firefox");
                if (p.Length > 0)
                {
                    for (int i = 0; i < p.Length; i++)
                    {
                        if (p[i].MainWindowTitle != "")
                        {
                            //loud-sometimes-all-the-time-official-video-ft-charlotte-cardin.mp3
                            //loud_-_sometimes,_all_the_time_(official_video)_ft._charlotte_cardin

                            string bufff = p[i].MainWindowTitle.Replace(" - YouTube — Mozilla Firefox", "");
                            Tittle = FixSongString(bufff);
                        }
                    }
                }
            }
            catch { }

            return Tittle;
        }

        private string FixSongString(string ThisStr)
        {
            string Fixed = ThisStr.ToLower().Replace(" ", "-");
            Fixed = string.Concat(Fixed.Split(Path.GetInvalidFileNameChars()));
            Fixed = Fixed.Replace("---", "-").Replace(",", "").Replace(".", "").Replace("(", "").Replace(")", "").Replace("[", "").Replace("]", "");
            return Fixed;
        }

        private void TimerEventProcessor(Object myObject, EventArgs myEventArgs)
        {
            myTimer.Stop();

            textBox2.Text = "";
            URL_List.Clear();
            SongNames_List.Clear();
            string BufferURL = getFirefoxURL();
            if (BufferURL.Contains(YoutubeSearchString))
            {
                BufferURL = removeListfromURL(BufferURL);
                URL_List.Add(BufferURL);
                string FireTitle = getFirefoxTitle();
                SongNames_List.Add(FireTitle);
                AddText("URL: " + BufferURL + " (" + FireTitle + ")");
            }
            else
            {
                AddText("Nothing found from URL link...");

                AddText("link:" + BufferURL);
            }

            CheckThoseURL();
            if (!IsRunning) return;

            //Perform Next Click
            if (!RetryDownloadFailed)
            {
                AddText("Next video...");
                chromiumWebBrowser1.LoadUrl(YT2MP3);
                FocusFirefox();
                SendKeys.Send("+{n}");  //send Shift+N (youtube playlist shortcut for next video)
                Thread.Sleep(2000);
                SendKeys.Send("{F5}");  //send F5 (firefox shortcut to refresh page, update the url link)
                Thread.Sleep(100);
                UnfocusFirefox();
                Thread.Sleep(100);

                RetryFailedCount = 0;
            }

            AddText("Wating " + IntervalInSecondsBetweenNextSong.ToString() + " seconds!");
            myTimer.Start();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveURLDone();
            SaveMP3DoneList();
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
            RetryDownloadFailed = false;

            for (int Loop = 0; Loop < URL_List.Count; Loop++)
            {
                string Line = URL_List[Loop];
                string Line2 = SongNames_List[Loop];

                if (!IsURLDone(Line) && !IsSongNameSame(Line2))
                {
                    //Downloading This URL
                    AddText("Downloading: " + Line + " (" + Line2 + ")");
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

                    textBox2.AppendText("Waiting til download is ready");
                    int WaitingTime = 0;
                    int ConvertionWidth = 0;
                    int WidthCheck = 0;
                    int ConversionTime = 0;
                    while ((BrowserWidth() == 1338 || BrowserWidth() <= 720) && WaitingTime < numericUpDown1.Value)
                    {
                        //#########
                        int ThissWidth = BrowserWidth();
                        if (WidthCheck == 6)
                        {
                            ConvertionWidth = ThissWidth;
                        }
                        if (WidthCheck > 6)
                        {
                            if (ThissWidth == ConvertionWidth)
                            {
                                WaitingTime--;

                                //if convertion time is above 2min, perform reset
                                if (ConversionTime > 120)
                                {
                                    WaitingTime = (int)numericUpDown1.Value;
                                }
                            }
                        }
                        WidthCheck++;
                        //#########

                        textBox2.AppendText(".");
                        Thread.Sleep(1000);
                        Application.DoEvents();
                        WaitingTime++;
                        ConversionTime++;

                        //close bot faster
                        if (!IsRunning) WaitingTime = (int) numericUpDown1.Value;
                    }

                    textBox2.AppendText(Environment.NewLine);
                    if (!IsRunning) return;

                    if (WaitingTime == numericUpDown1.Value)
                    {
                        //DownloadCountFailed++;
                        //SetDownloadCounts();

                        RetryFailedCount++;
                        if (RetryFailedCount <= 1)
                        {
                            AddText("Taking too long or something went wrong converting file, retrying file conversion...");
                            RetryDownloadFailed = true;
                            chromiumWebBrowser1.LoadUrl(YT2MP3);
                        }
                        else
                        {
                            RetryFailedCount = 0;
                            DownloadCountFailed++;
                            SetDownloadCounts();
                            AddText("Download failed after 3times, trying new video...");

                        }
                        return;
                    }

                    //###
                    if (RetryFailedCount > 0)
                    {
                        RetryFailedCount = 0;
                    }
                    //###
                    AddText("Took " + ConversionTime.ToString() + " seconds to convert the video to mp3");

                    //Click the download button
                    IsDownloading = true;
                    DownloadedFilename = "";
                    IEnumerable<Process> childfirst = ParentProcessUtilities.GetChildProcesses();
                    int ProcessChildCount = GetProcChildCount();
                    var script = @"
                                    document.getElementsByClassName('btn-primary')[0].click();
                                ";
                    textBox2.AppendText("Downloading..." + Environment.NewLine);
                    chromiumWebBrowser1.ExecuteScriptAsyncWhenPageLoaded(script);
                    //Thread.Sleep(1000);

                    //Check for ads and close then, retry
                    textBox2.AppendText("Waiting til ads are opened");
                    int SecondsWaited = 0;
                    while (GetProcChildCount() == ProcessChildCount && SecondsWaited < 6)
                    {
                        textBox2.AppendText(".");
                        Thread.Sleep(1000);
                        Application.DoEvents();
                        SecondsWaited++;

                        //close bot faster
                        if (!IsRunning) SecondsWaited = 6;
                    }

                    textBox2.AppendText(Environment.NewLine);
                    if (!IsRunning) return;

                    //Close Process
                    IEnumerable<Process> childnew = ParentProcessUtilities.GetChildProcesses();
                    AddText("Closing new processes(ads)..");
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
                            try
                            {
                                Process.GetProcessById(procnew.Id).CloseMainWindow();
                                Process.GetProcessById(procnew.Id).Dispose();
                                Process.GetProcessById(procnew.Id).Close();
                                Process.GetProcessById(procnew.Id).Kill();
                                Process.GetCurrentProcess().CloseMainWindow();
                                AddText("Closed process PID: " + procnew.Id);
                                ClosedAds = true;
                            }
                            catch { }
                        }
                    }

                    //Restart Download
                    Thread.Sleep(100);
                    textBox2.AppendText("Starting download..." + Environment.NewLine);
                    chromiumWebBrowser1.ExecuteScriptAsyncWhenPageLoaded(script);

                    int SecondsWaitedDownload = 0;
                    while (IsDownloading && SecondsWaitedDownload < (numericUpDown1.Value * 10))
                    {
                        LoadLogs();
                        Thread.Sleep(100);
                        Application.DoEvents();

                        //close bot faster
                        if (!IsRunning) SecondsWaitedDownload = (int) (numericUpDown1.Value * 10);
                    }

                    if (!IsRunning) return;

                    if (SecondsWaitedDownload >= (numericUpDown1.Value * 10))
                    {
                        AddText("ERROR: Download not detected after " + (numericUpDown1.Value * 10) + " secends!");
                        DownloadCountFailed++;
                        SetDownloadCounts();
                        return;
                    }

                    if (DownloadedFilename != "")
                    {
                        if (textBox1.Text != "") 
                        {
                            //Cut and Paste the file to 'music' folder
                            byte[] AllByte = File.ReadAllBytes(DownloadsDirectoryPath + DownloadedFilename);
                            File.Create(@textBox1.Text + DownloadedFilename).Dispose();
                            File.WriteAllBytes(@textBox1.Text + DownloadedFilename, AllByte);
                            File.Delete(DownloadsDirectoryPath + DownloadedFilename);
                            AddText("File transfered to save location path!");
                        }
                        textBox3.AppendText(DownloadedFilename + Environment.NewLine);
                    }
                    else
                    {
                        AddText("ERROR: Downloaded filename is Null!");
                        DownloadCountFailed++;
                        SetDownloadCounts();
                        return;
                    }

                    //Finished, add to Done list
                    URL_Done_List.Add(Line);
                    SongNames_Done_List.Add(Line2);
                    DownloadCountToday++;
                    SetDownloadCounts();
                    SaveURLDone();
                    SaveMP3DoneList();
                }
                else
                {
                    AddText("This video has already been downloaded!");
                }
            }
            URL_List.Clear();
        }

        private void SetDownloadCounts()
        {
            labelTotal.Text = URL_Done_List.Count.ToString();
            labelToday.Text = DownloadCountToday.ToString();
            labelFailed.Text = DownloadCountFailed.ToString();
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

        private bool IsSongNameSame(string ThisSongName)
        {
            foreach (string Line in SongNames_Done_List)
            {
                if (Line == ThisSongName) return true;

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

        private void chromiumWebBrowser1_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e)
        {

        }

        void AddText(string ThisText)
        {
            textBox2.AppendText(ThisText + Environment.NewLine);
        }

        private void textBox1_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox1.Text = folderBrowserDialog1.SelectedPath + "\\";
                CheckLocations();
                SaveSettings();
            }
        }

        private void textBox4_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBox4.Text = folderBrowserDialog1.SelectedPath + "\\";
                DownloadsDirectoryPath = textBox4.Text;
                CheckLocations();
                SaveSettings();
            }
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            if (!IsLoadingSettings)
            {
                SaveSettings();
            }
        }

        private void SaveMP3ListToDoneFiles(List<string> MP3List)
        {
            int IdenticalCount = 0;
            int AddedCount = 0;

            for (int i = 0; i < MP3List.Count; i++)
            {
                bool FoundSame = false;

                for (int k = 0; k < SongNames_Done_List.Count; k++)
                {
                    if (SongNames_Done_List[k] == MP3List[i])
                    {
                        FoundSame = true;
                        k = SongNames_Done_List.Count;
                    }
                }

                if (!FoundSame)
                {
                    SongNames_Done_List.Add(MP3List[i]);
                    AddedCount++;
                }
                else
                {
                    IdenticalCount++;
                }
            }

            AddText("Added " + AddedCount + " done song names!");
            AddText(IdenticalCount + " song names were identicals!");

            //save dones
            SaveMP3DoneList();
        }

        private void SaveMP3DoneList()
        {
            string ThisFileCheck = Application.StartupPath + "\\SongNamesDone.txt";
            File.Create(ThisFileCheck).Dispose();

            if (SongNames_Done_List.Count > 0)
            {
                string[] ListBuffer = new string[SongNames_Done_List.Count];
                for (int i = 0; i < SongNames_Done_List.Count; i++)
                {
                    ListBuffer[i] = SongNames_Done_List[i];
                }

                File.WriteAllLines(ThisFileCheck, ListBuffer);
            }

            LoadMP3DoneList();
        }

        private void LoadMP3DoneList()
        {
            SongNames_Done_List.Clear();
            string ThisFileCheck = Application.StartupPath + "\\SongNamesDone.txt";
            if (File.Exists(ThisFileCheck))
            {
                string[] AllLines = File.ReadAllLines(ThisFileCheck);
                foreach (string Line in AllLines)
                {
                    if (Line != "")
                    {
                        SongNames_Done_List.Add(FixSongString(Line));
                    }
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                string[] AllFiles = Directory.GetFiles(folderBrowserDialog1.SelectedPath + "\\");

                if (AllFiles.Length > 0)
                {
                    List<string> AllMP3Done = new List<string>();

                    for (int i = 0; i < AllFiles.Length; i++)
                    {
                        string filename = Path.GetFileName(AllFiles[i]);
                        if (filename.ToLower().Contains(".mp3"))
                        {
                            AllMP3Done.Add(filename.Replace(".mp3", "").Replace(".MP3", ""));
                        }
                    }

                    SaveMP3ListToDoneFiles(AllMP3Done);
                }
                else
                {
                    AddText("0 files found!");
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            AddText("Downloaded files cleared!");
            SongNames_Done_List.Clear();
            SaveMP3DoneList();
        }
    }
}
