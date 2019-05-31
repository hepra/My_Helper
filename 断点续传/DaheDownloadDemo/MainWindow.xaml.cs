using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DaheDownloadDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        DownloadManager downloadManager;
        private void BtnStarDownload_Click(object sender, RoutedEventArgs e)
        {
            var temp = sender as Button;
            switch (temp.Content.ToString())
            {
                case "开始下载":
                    if (tbSave.Text != "" && tbUrl.Text != "")
                    {
                        var downloadInfo = new DownloadInfo();
                        downloadInfo.saveDir = tbSave.Text;
                        downloadInfo.downloadUrlList = new List<string> {
                            tbUrl.Text
                        };
                        downloadInfo.taskCount = 3;

                        downloadInfo.taskCount = 1;
                        downloadInfo.IsSupportMultiThreading = (bool)cbDDXC.IsChecked;
                        if (downloadManager != null)
                        {
                            downloadManager.Stop();
                        }
                        if (!string.IsNullOrEmpty(Configer.Read("downloadInfo")))
                        {
                            downloadInfo = new DownloadInfo(Configer.Read("downloadInfo"));
                              downloadManager = new DownloadManager(downloadInfo);
                            downloadManager.OnDownload += DownloadManager_OnDownload;
                            downloadManager.OnStart += DownloadManager_OnStart;
                            downloadManager.OnStop += DownloadManager_OnStop;
                            downloadManager.OnFinsh += DownloadManager_OnFinsh;
    
                            downloadManager.Start();
                            return;
                        }
                        downloadManager = new DownloadManager(downloadInfo);
                        downloadManager.OnDownload += DownloadManager_OnDownload;
                        downloadManager.OnStart += DownloadManager_OnStart;
                        downloadManager.OnStop += DownloadManager_OnStop;
                        downloadManager.OnFinsh += DownloadManager_OnFinsh;
                    }
                    downloadManager.ReStart();
                    temp.IsEnabled = false;
                    break;
                case "停止下载":
                    downloadManager.Stop();
                    btnStarDownload.IsEnabled = true;
                    break;
                case "暂停下载":
                    downloadManager.Stop();
                    break;
                case "继续下载":
                    downloadManager.Start();
                    break;
            }
        }

        private void DownloadManager_OnFinsh()
        {
            //throw new NotImplementedException();
        }

        private void DownloadManager_OnStop()
        {
            this.Dispatcher.Invoke(() =>
            {
                btnStarDownload.IsEnabled = true;
                progressDownload.Value = 0;
                MessageBox.Show("下载已停止!");
            });
         
         //   throw new NotImplementedException();
        }

        private void DownloadManager_OnStart()
        {
           // throw new NotImplementedException();
        }

        private void DownloadManager_OnDownload(long arg1, long arg2)
        {
            this.Dispatcher.Invoke(() =>
            {
                progressDownload.Value = arg1 * 100.0 / arg2;
            });
            
        }
    }
}
