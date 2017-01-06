using CefSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMM
{
    class DownloadHandler : IDownloadHandler
    {
        private string chemin;
        private DownloadFinish download;

        public DownloadHandler(string chemin)
        {
            this.chemin = chemin;
        }

        public DownloadHandler(string chemin, DownloadFinish download)
        {
            this.chemin = chemin;
            this.download = download;
        }

        public event EventHandler<DownloadItem> OnBeforeDownloadFired;

        public event EventHandler<DownloadItem> OnDownloadUpdatedFired;
        

        public void OnBeforeDownload(IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
        {
            var handler = OnBeforeDownloadFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }
            

            if (!callback.IsDisposed)
            {
                using (callback)
                {
                    downloadItem.FullPath = chemin + "\\" + downloadItem.SuggestedFileName;
                    callback.Continue(downloadItem.FullPath, showDialog: false);
                }
            }
        }

        public void OnDownloadUpdated(IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
        {
            var handler = OnDownloadUpdatedFired;
            if (handler != null)
            {
                handler(this, downloadItem);
            }
            if (downloadItem.IsComplete)
            {
                browser.FocusedFrame.ExecuteJavaScriptAsync("close();");
                System.Diagnostics.Debug.WriteLine("Credit Agricol " + downloadItem.FullPath);

                download.isFinish(downloadItem.FullPath);
            }
        }
    }
}