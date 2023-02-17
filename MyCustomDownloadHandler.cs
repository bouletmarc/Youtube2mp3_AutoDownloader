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

namespace Youtube2MP3
{
    class MyCustomDownloadHandler : IDownloadHandler
    {
        public event EventHandler<DownloadItem> OnBeforeDownloadFired;
        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;
        public event EventHandler<DownloadItem> CanDownloadFired;

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
                    string DownloadPath = "";

                    //Load Save location
                    string ThisFileCheck = Application.StartupPath + "\\Setting.txt";
                    if (File.Exists(ThisFileCheck))
                    {
                        string[] AllLines = File.ReadAllLines(ThisFileCheck);
                        if (AllLines.Length > 1)
                        {
                            DownloadPath = AllLines[1];
                        }
                    }
                    else
                    {
                        SaveLogs("Cannot fint the download folder path!");
                    }

                    callback.Continue(Path.Combine(DownloadPath, downloadItem.SuggestedFileName), showDialog: false);
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

        public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
        {
            return true;
        }
    }
}
