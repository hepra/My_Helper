using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DaheDownloadDemo
{
    static public class DownloadDemo
    {
        public static void Download(string URL, string path)
        {
            var downloadInfo = new DownloadInfo();
            downloadInfo.saveDir = path;
            downloadInfo.downloadUrlList = new List<string> {
                URL
            };
            downloadInfo.taskCount = 1;
            downloadInfo.IsSupportMultiThreading = true;
            var downloadManager = new DownloadManager(downloadInfo);
            downloadManager.OnDownload += DownloadManager_OnDownload; ;
            downloadManager.OnStart += DownloadManager_OnStart; ;
            downloadManager.OnStop += DownloadManager_OnStop; ;
            downloadManager.OnFinsh += DownloadManager_OnFinsh;
        }

        public static void DownloadManager_OnFinsh()
        {
            //下载完成
        }

        public static void DownloadManager_OnStop()
        {
            //暂停下载
        }

        public static void DownloadManager_OnStart()
        {
            //开始下载
        }

        public static void DownloadManager_OnDownload(long arg1, long arg2)
        {
            //进度显示
            //进度 =  arg1*100/arg2
        }
    }

    public class TaskInfo
    {
        /// <summary>
        /// 请求方法
        /// </summary>
        public string method { get; set; }
        public string downloadUrl { get; set; }
        public string filePath { get; set; }
        /// <summary>
        /// 分片起点
        /// </summary>
        public long fromIndex { get; set; }
        /// <summary>
        /// 分片终点
        /// </summary>
        public long toIndex { get; set; }
        /// <summary>
        /// 分片的总大小
        /// </summary>
        public long count
        {
            get { return this.toIndex - this.fromIndex + 1; }
        }
        public override string ToString()
        {
            return method+"##"+ downloadUrl + "##"+ filePath + "##"+ fromIndex + "##"+ toIndex + "##"+ count + "##";
        }
        public TaskInfo(string taskInfo)
        {
            if(taskInfo.Contains("##"))
            {
                string[] tempList = taskInfo.Split(new string[1] { "##" }, StringSplitOptions.RemoveEmptyEntries);
                this.method = tempList[0];
                this.downloadUrl = tempList[1];
                this.filePath = tempList[2];
                this.fromIndex = long.Parse(tempList[3]);
                this.toIndex = long.Parse(tempList[4]);
            }
           
        }
        public TaskInfo()
        {

        }
    }
    public class DownloadService
    {
        private string downloadUrl = "";//文件下载地址
        private string filePath = "";//文件保存路径
        private string method = "";//方法
        private long fromIndex = 0;//开始下载的位置
        private long toIndex = 0;//结束下载的位置
        private long count = 0;//总大小
        private long size = 524288;//每次下载大小 512kb
        private bool isRun = false;//是否正在进行

        public bool isFinish { get; private set; } = false;//是否已下载完成
        public bool isStopped { get; private set; } = true;//是否已停止


        public event Action OnStart;
        public event Action OnDownload;
        public event Action OnFinsh;

        public long GetDownloadedCount()
        {
            return this.count - this.toIndex + this.fromIndex - 1;
        }

        public void Stop()
        {
            this.isRun = false;
        }
        public bool Start(TaskInfo info, bool isReStart)
        {
            this.downloadUrl = info.downloadUrl;
            this.fromIndex = info.fromIndex;
            this.toIndex = info.toIndex;
            this.method = info.method;
            this.filePath = info.filePath;
            this.count = info.count;
            this.isStopped = false;
            if (File.Exists(this.filePath))
            {
                if (isReStart)
                {
                    File.Delete(this.filePath);
                    File.Create(this.filePath).Close();
                }
            }
            else
            {
                File.Create(this.filePath).Close();
            }
            using (var file = File.Open(this.filePath, FileMode.Open))
            {
                this.fromIndex = info.fromIndex + file.Length;
            }
            if (this.fromIndex >= this.toIndex)
            {
                OnFineshHandler();
                this.isFinish = true;
                this.isStopped = true;
                return false;
            }
            OnStartHandler();
            this.isRun = true;
            new Action(() =>
            {
                WebResponse rsp;
                while (this.fromIndex < this.toIndex && isRun)
                {
                    long to;
                    if (this.fromIndex + this.size >= this.toIndex - 1)
                        to = this.toIndex - 1;
                    else
                        to = this.fromIndex + size;
                    using (rsp = HttpHelper.Download(this.downloadUrl, this.fromIndex, to, this.method))
                    {
                        if (rsp==null)
                        {
                            this.isStopped = true;
                            return;
                        }
                        Save(this.filePath, rsp.GetResponseStream());
                    }
                }
                if (!this.isRun) this.isStopped = true;
                if (this.fromIndex >= this.toIndex)
                {
                    this.isFinish = true;
                    this.isStopped = true;
                    OnFineshHandler();
                }

            }).BeginInvoke(null, null);
            return true;
        }

        private void Save(string filePath, Stream stream)
        {
            try
            {
                using (var writer = File.Open(filePath, FileMode.Append))
                {
                    using (stream)
                    {
                        var repeatTimes = 0;
                        byte[] buffer = new byte[1024];
                        var length = 0;
                        while ((length = stream.Read(buffer, 0, buffer.Length)) > 0 && this.isRun)
                        {
                            writer.Write(buffer, 0, length);
                            this.fromIndex += length;
                            if (repeatTimes % 5 == 0)
                            {
                                OnDownloadHandler();
                            }
                            repeatTimes++;
                        }
                    }
                }
                OnDownloadHandler();
            }
            catch (Exception)
            {
                //异常也不影响
            }
        }

        private void OnStartHandler()
        {
            new Action(() =>
            {
                this.OnStart?.Invoke();
            }).BeginInvoke(null, null);
        }
        private void OnFineshHandler()
        {
            new Action(() =>
            {
                this.OnFinsh?.Invoke();
                this.OnDownload?.Invoke();
            }).BeginInvoke(null, null);
        }
        private void OnDownloadHandler()
        {
            new Action(() =>
            {
                this.OnDownload?.Invoke();
            }).BeginInvoke(null, null);
        }
    }

    public class DownloadInfo
    {
        public override string ToString()
        {
            return taskCount + "|" + tempFileName + "|" + isNewTask + "|" + isReStart + "|" + count + "|" +
                saveDir + "|" + method + "|" + fileName + "|" + downloadUrlList[0] +
                "|" + IsSupportMultiThreading + "|"+ TaskInfoListToString();
        }
        public DownloadInfo(string downloadInfo)
        {
            string[] tempList = downloadInfo.Split(new string[1] { "|" }, StringSplitOptions.RemoveEmptyEntries);
            this.taskCount = int.Parse(tempList[0]);
            this.tempFileName = tempList[1];
            this.isNewTask = bool.Parse(tempList[2]);
            this.isReStart = bool.Parse(tempList[3]);
            this.count = long.Parse(tempList[4]);
            this.saveDir = tempList[5];
            this.method = tempList[6];
            this.fileName =tempList[7];
            this.downloadUrlList =new List<string> {tempList[8]};
            this.IsSupportMultiThreading = bool.Parse(tempList[9]);
            if(tempList.Length>10)
            {
                string[] taskInfolist = tempList[10].Split(new string[1] { "**" }, StringSplitOptions.RemoveEmptyEntries);
                this.TaskInfoList = new List<TaskInfo>();
                for (int i = 0; i < taskInfolist.Length; i++)
                {
                    this.TaskInfoList.Add(new TaskInfo(taskInfolist[i]));
                }
            }
           
        }
        public DownloadInfo()
        {

        }
        private string TaskInfoListToString()
        {
            string temp = "";
            if(TaskInfoList==null)
            {
                return "";
            }
            for (int i = 0; i < TaskInfoList.Count; i++)
            {
                temp+= TaskInfoList[i].ToString()+"**";
            }
            return temp;
        }
        /// <summary>
        /// 子线程数量
        /// </summary>
        public int taskCount { get; set; } = 1;
        /// <summary>
        /// 缓存名，临时保存的文件名
        /// </summary>
        public string tempFileName { get; set; }
        /// <summary>
        /// 是否是新任务，如果不是新任务则通过配置去分配线程
        /// 一开始要设为true,在初始化完成后会被设为true，此时可以对这个 DownloadInfo 进行序列化后保存，进而实现退出程序加载配置继续下载。
        /// </summary>
        public bool isNewTask { get; set; } = true;
        /// <summary>
        /// 是否重新下载
        /// </summary>
        public bool isReStart { get; set; } = false;
        /// <summary>
        /// 任务总大小
        /// </summary>
        public long count { get; set; }
        /// <summary>
        /// 保存的目录
        /// </summary>
        public string saveDir { get; set; }
        /// <summary>
        /// 请求方法
        /// </summary>
        public string method { get; set; } = "get";
        public string fileName { get; set; }
        /// <summary>
        /// 下载地址，
        /// 这里是列表形式，如果同一个文件有不同来源则可以通过不同来源取数据
        /// 来源的有消息需另外判断
        /// </summary>
        public List<string> downloadUrlList { get; set; }
        /// <summary>
        /// 是否支持断点续传
        /// 在任务开始后，如果需要暂停，应先通过这个判断是否支持
        /// 默认设为false
        /// </summary>
        public bool IsSupportMultiThreading { get; set; } = false;
        /// <summary>
        /// 线程任务列表
        /// </summary>
        public List<TaskInfo> TaskInfoList { get; set; }

        
    }
    public class DownloadManager
    {
        private long fromIndex = 0;//开始下载的位置
        private bool isRun = false;//是否正在进行
        private DownloadInfo dlInfo;

        private List<DownloadService> dls = new List<DownloadService>();

        public event Action OnStart;
        public event Action OnStop;
        public event Action<long, long> OnDownload;
        public event Action OnFinsh;

        public DownloadManager(DownloadInfo dlInfo)
        {
            this.dlInfo = dlInfo;
        }
        public void Stop()
        {
            this.isRun = false;
            dls.ForEach(dl => dl.Stop());
            OnStopHandler();
        }

        public void Start()
        {
            this.dlInfo.isReStart = false;
            WorkStart();
        }
        public void ReStart()
        {
            this.dlInfo.isReStart = true;
            WorkStart();
        }

        private void WorkStart()
        {
            new Action(() =>
            {
                if (dlInfo.isReStart)
                {
                    this.Stop();
                }

                while (dls.Where(dl => !dl.isStopped).Count() > 0)
                {
                    if (dlInfo.isReStart) Thread.Sleep(100);
                    else return;
                }

                this.isRun = true;
                OnStartHandler();
                //首次任务或者不支持断点续传的进入
                if (dlInfo.isNewTask || (!dlInfo.isNewTask && !dlInfo.IsSupportMultiThreading))
                {
                    //第一次请求获取一小块数据，根据返回的情况判断是否支持断点续传
                    using (var rsp = HttpHelper.Download(dlInfo.downloadUrlList[0], 0, 0, dlInfo.method))
                    {
                        if (rsp == null)
                        {
                            this.Stop();
                            return;
                        }
                        //获取文件名，如果包含附件名称则取下附件，否则从url获取名称
                        var Disposition = rsp.Headers["Content-Disposition"];
                        if (Disposition != null) dlInfo.fileName = Disposition.Split('=')[1].Replace("\"","").Replace(";","");
                        else dlInfo.fileName = Path.GetFileName(rsp.ResponseUri.AbsolutePath);

                        //默认给流总数
                        dlInfo.count = rsp.ContentLength;
                        //尝试获取 Content-Range 头部，不为空说明支持断点续传
                        var contentRange = rsp.Headers["Content-Range"];
                        if (contentRange != null)
                        {
                            //支持断点续传的话，就取range 这里的总数
                            dlInfo.count = long.Parse(rsp.Headers["Content-Range"]?.Split('/')?[1]);
                            dlInfo.IsSupportMultiThreading = true;

                            //生成一个临时文件名
                            var tempFileName = Convert.ToBase64String(Encoding.UTF8.GetBytes(dlInfo.fileName)).ToUpper();
                            tempFileName = tempFileName.Length > 32 ? tempFileName.Substring(0, 32) : tempFileName;
                            dlInfo.tempFileName = tempFileName + DateTime.Now.ToString("yyyyMMddHHmmssfff");
                            ///创建线程信息
                            ///
                           
                            GetTaskInfo(dlInfo);
                        }
                        else
                        {
                            //不支持断点续传则一开始就直接读完整流
                            Save(GetRealFileName(dlInfo), rsp.GetResponseStream());
                            OnFineshHandler();
                        }
                    }
                    dlInfo.isNewTask = false;
                }
                //如果支持断点续传采用这个
                if (dlInfo.IsSupportMultiThreading)
                {
                    StartTask(dlInfo);

                    //等待合并
                    while (this.dls.Where(td => !td.isFinish).Count() > 0 && this.isRun)
                    {
                        Thread.Sleep(100);
                    }
                    if ((this.dls.Where(td => !td.isFinish).Count() == 0))
                    {

                        CombineFiles(dlInfo);
                        OnFineshHandler();
                    }
                }

            }).BeginInvoke(null, null);
        }
        private void CombineFiles(DownloadInfo dlInfo)
        {
            string realFilePath = GetRealFileName(dlInfo);

            //合并数据
            byte[] buffer = new Byte[2048];
            int length = 0;
            using (var fileStream = File.Open(realFilePath, FileMode.CreateNew))
            {
                for (int i = 0; i < dlInfo.TaskInfoList.Count; i++)
                {
                    var tempFile = dlInfo.TaskInfoList[i].filePath;
                    using (var tempStream = File.Open(tempFile, FileMode.Open))
                    {
                        while ((length = tempStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            fileStream.Write(buffer, 0, length);
                        }
                        tempStream.Flush();
                    }
                    Configer.Write("downloadInfo", "");
                    File.Delete(tempFile);
                }
            }
        }

        private static string GetRealFileName(DownloadInfo dlInfo)
        {
            //创建正式文件名，如果已存在则加数字序号创建，避免覆盖
            var fileIndex = 0;
            var realFilePath = Path.Combine(dlInfo.saveDir, dlInfo.fileName);
            while (File.Exists(realFilePath))
            {
                realFilePath = Path.Combine(dlInfo.saveDir, string.Format("{0}_{1}", fileIndex++, dlInfo.fileName));
            }

            return realFilePath;
        }

        private void StartTask(DownloadInfo dlInfo)
        {
            this.dls = new List<DownloadService>();
            if (dlInfo.TaskInfoList != null)
            {
                foreach (var item in dlInfo.TaskInfoList)
                {
                    var dl = new DownloadService();
                    dl.OnDownload += OnDownloadHandler;
                    dls.Add(dl);
                    dl.Start(item, dlInfo.isReStart);
                }
            }
        }

        private void GetTaskInfo(DownloadInfo dlInfo)
        {
            var pieceSize = (dlInfo.count) / dlInfo.taskCount;
            dlInfo.TaskInfoList = new List<TaskInfo>();
            var rand = new Random();
            var urlIndex = 0;
            for (int i = 0; i <= dlInfo.taskCount + 1; i++)
            {
                var from = (i * pieceSize);

                if (from >= dlInfo.count) break;
                var to = from + pieceSize;
                if (to >= dlInfo.count) to = dlInfo.count;

                dlInfo.TaskInfoList.Add(
                    new TaskInfo
                    {
                        method = dlInfo.method,
                        downloadUrl = dlInfo.downloadUrlList[urlIndex++],
                        filePath = Path.Combine(dlInfo.saveDir, dlInfo.tempFileName + i + ".temp"),
                        fromIndex = from,
                        toIndex = to
                    });
                if (urlIndex >= dlInfo.downloadUrlList.Count) urlIndex = 0;
            }
        }

        /// <summary>
        /// 保存内容
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="stream"></param>
        private void Save(string filePath, Stream stream)
        {
            try
            {
                using (var writer = File.Open(filePath, FileMode.Append))
                {
                    using (stream)
                    {
                        var repeatTimes = 0;
                        byte[] buffer = new byte[1024];
                        var length = 0;
                        while ((length = stream.Read(buffer, 0, buffer.Length)) > 0 && this.isRun)
                        {
                            writer.Write(buffer, 0, length);
                            this.fromIndex += length;
                            if (repeatTimes % 5 == 0)
                            {
                                writer.Flush();//一定大小就刷一次缓冲区
                                OnDownloadHandler();
                              
                            }
                            repeatTimes++;
                        }
                        writer.Flush();
                        OnDownloadHandler();
                    }
                }
            }
            catch (Exception)
            {
                //异常也不影响
            }
        }


        private void OnStartHandler()
        {
            new Action(() =>
            {
                this.OnStart?.Invoke();
            }).BeginInvoke(null, null);
        }
        private void OnStopHandler()
        {
            Configer.Write("downloadInfo", dlInfo.ToString());
            new Action(() =>
            {
                this.OnStop?.Invoke();
            }).BeginInvoke(null, null);
        }
        private void OnFineshHandler()
        {
            new Action(() =>
            {
                for (int i = 0; i < dlInfo.TaskInfoList.Count; i++)
                {
                    var tempFile = dlInfo.TaskInfoList[i].filePath;
                    File.Delete(tempFile);
                }
                this.OnFinsh?.Invoke();
            }).BeginInvoke(null, null);
        }
        private void OnDownloadHandler()
        {
            new Action(() =>
            {
                long current = GetDownloadLength();
                this.OnDownload?.Invoke(current, dlInfo.count);
            }).BeginInvoke(null, null);
        }

        public long GetDownloadLength()
        {
            if (dlInfo.IsSupportMultiThreading) return dls.Sum(dl => dl.GetDownloadedCount());
            else return this.fromIndex;
        }
    }

    public class HttpHelper1
    {

        #region 断点续传

        /// <summary>
        /// 是否暂停
        /// </summary>
        static bool isPause = true;
        /// <summary>
        /// 下载开始位置（也就是已经下载了的位置）
        /// </summary>
        static long rangeBegin = 0;//(当然，这个值也可以存为持久化。如文本、数据库等)

        /// <summary>
        /// 断线续传
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void download断点续传Async(string url, string SavePath)
        {
            isPause = !isPause;
            if (!isPause)//点击下载
            {
                //button3.Text = "暂停";

                await Task.Run(async () =>
                {

                    long downloadSpeed = 0;//下载速度
                    using (HttpClient http = new HttpClient())
                    {
                        var request = new HttpRequestMessage { RequestUri = new Uri(url) };
                        request.Headers.Range = new RangeHeaderValue(rangeBegin, null);//【关键点】全局变量记录已经下载了多少，然后下次从这个位置开始下载。
                        var httpResponseMessage = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                        var contentLength = httpResponseMessage.Content.Headers.ContentLength;//本次请求的内容大小
                        if (httpResponseMessage.Content.Headers.ContentRange != null) //如果为空，则说明服务器不支持断点续传
                        {
                            contentLength = httpResponseMessage.Content.Headers.ContentRange.Length;//服务器上的文件大小
                        }

                        using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync())
                        {
                            var readLength = 1024000;//1000K
                            byte[] bytes = new byte[readLength];
                            int writeLength;
                            var beginSecond = DateTime.Now.Second;//当前时间秒
                            while ((writeLength = stream.Read(bytes, 0, readLength)) > 0 && !isPause)
                            {
                                //使用追加方式打开一个文件流
                                using (FileStream fs = new FileStream(SavePath + "\\temp.rar", FileMode.Append, FileAccess.Write))
                                {
                                    fs.Write(bytes, 0, writeLength);
                                }
                                downloadSpeed += writeLength;
                                rangeBegin += writeLength;
                                // progressBar1.Invoke((Action)(() =>
                                //{
                                //    var endSecond = DateTime.Now.Second;
                                //    if (beginSecond != endSecond)//计算速度
                                //    {
                                //        downloadSpeed = downloadSpeed / (endSecond - beginSecond);
                                //        //label1.Text = "下载速度" + downloadSpeed / 1024 + "KB/S";

                                //        beginSecond = DateTime.Now.Second;
                                //        downloadSpeed = 0;//清空
                                //    }
                                //   // progressBar1.Value = Math.Max((int)((rangeBegin) * 100 / contentLength), 1);
                                //}));
                            }

                            if (rangeBegin == contentLength)
                            {
                                //label1.Invoke((Action)(() =>
                                //{
                                //    label1.Text = "下载完成";
                                //}));
                            }
                        }
                    }
                });
            }
            else//点击暂停
            {
                //button3.Text = "继续下载";
                //label1.Text = "暂停下载";
            }
        }
        #endregion

        #region 多线程下载
        public async void MutliThreadDownloadAsync(string url, int threadCount, string SaveFileName)
        {
            using (HttpClient http = new HttpClient())
            {
                var httpResponseMessage = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                var contentLength = httpResponseMessage.Content.Headers.ContentLength.Value;
                var size = contentLength / threadCount; //这里为了方便，就直接分成10个线程下载。（当然这是不合理的）
                var tasks = new List<Task>();
                for (int i = 0; i < 10; i++)
                {
                    var begin = i * size;
                    var end = begin + size - 1;
                    var task = FileDownload(url, begin, end, i, SaveFileName);
                    tasks.Add(task);
                }
                for (int i = 0; i < 10; i++)
                {
                    await tasks[i];  //当然，这里如有下载异常没有考虑、文件也没有校验。各位自己完善吧。
                    //progressBar1.Value = (i + 1) * 10;
                }
                FileMerge(SaveFileName, "temp.rar");
                //下载完成;
            }
        }

        #endregion

        /// <summary>
        /// 文件下载
        /// （如果你有兴趣，可以没个线程弄个进度条）
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public Task FileDownload(string url, long begin, long end, int index, string SaveFileName)
        {
            var task = Task.Run(async () =>
            {
                using (HttpClient http = new HttpClient())
                {
                    var request = new HttpRequestMessage { RequestUri = new Uri(url) };
                    request.Headers.Range = new RangeHeaderValue(begin, end);//【关键点】全局变量记录已经下载了多少，然后下次从这个位置开始下载。
                    var httpResponseMessage = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                    using (var stream = await httpResponseMessage.Content.ReadAsStreamAsync())
                    {
                        var readLength = 1024000;//1000K
                        byte[] bytes = new byte[readLength];
                        int writeLength;
                        var beginSecond = DateTime.Now.Second;//当前时间秒
                        var filePaht = SaveFileName;
                        if (!Directory.Exists(filePaht))
                            Directory.CreateDirectory(filePaht);

                        try
                        {
                            while ((writeLength = stream.Read(bytes, 0, readLength)) > 0)
                            {
                                //使用追加方式打开一个文件流
                                using (FileStream fs = new FileStream(filePaht + index, FileMode.Append, FileAccess.Write))
                                {
                                    fs.Write(bytes, 0, writeLength);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            //如果出现异常则删掉这个文件
                            File.Delete(filePaht + index);
                        }
                    }
                }
            });

            return task;
        }
        /// <summary>
        /// 合并文件
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool FileMerge(string path, string fileName)
        {
            //这里排序一定要正确，转成数字后排序（字符串会按1 10 11排序，默认10比2小）
            foreach (var filePath in Directory.GetFiles(path).OrderBy(t => int.Parse(Path.GetFileNameWithoutExtension(t))))
            {
                using (FileStream fs = new FileStream(Directory.GetParent(path).FullName + @"\" + fileName, FileMode.Append, FileAccess.Write))
                {
                    byte[] bytes = System.IO.File.ReadAllBytes(filePath);//读取文件到字节数组
                    fs.Write(bytes, 0, bytes.Length);//写入文件
                }
                System.IO.File.Delete(filePath);
            }
            Directory.Delete(path);
            return true;
        }
    }

    public class HttpHelper
    {
        public static void init_Request(ref System.Net.HttpWebRequest request)
        {
            request.Accept = "text/json,*/*;q=0.5";
            request.Headers.Add("Accept-Charset", "utf-8;q=0.7,*;q=0.7");
            request.Headers.Add("Accept-Encoding", "gzip, deflate, x-gzip, identity; q=0.9");
            request.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
            request.Timeout = 8000;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        public static System.Net.HttpWebRequest GetHttpWebRequest(string url)
        {
            HttpWebRequest request = null;
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            return request;
        }
        public static WebResponse Download(string downloadUrl, long from, long to, string method)
        {
            try
            {
                var request = HttpHelper.GetHttpWebRequest(downloadUrl);
                HttpHelper.init_Request(ref request);
                request.Accept = "text/json,*/*;q=0.5";
                request.AddRange(from, to);
                request.Headers.Add("Accept-Charset", "utf-8;q=0.7,*;q=0.7");
                request.Headers.Add("Accept-Encoding", "gzip, deflate, x-gzip, identity; q=0.9");
                request.AutomaticDecompression = System.Net.DecompressionMethods.GZip;
                request.Timeout = 120000;
                request.Method = method;
                request.KeepAlive = false;
                request.ContentType = "application/json; charset=utf-8";
                return request.GetResponse();
            }
            catch (Exception ex)
            {
                return null;
            }
          
        }
        public static string Get(string url, IDictionary<string, string> param)
        {
            var paramBuilder = new List<string>();
            foreach (var item in param)
            {
                paramBuilder.Add(string.Format("{0}={1}", item.Key, item.Value));
            }
            url = string.Format("{0}?{1}", url.TrimEnd('?'), string.Join(",", paramBuilder.ToArray()));
            return Get(url);
        }
        public static string Get(string url)
        {
            try
            {
                var request = GetHttpWebRequest(url);
                if (request != null)
                {
                    string retval = null;
                    init_Request(ref request);
                    using (var Response = request.GetResponse())
                    {
                        using (var reader = new System.IO.StreamReader(Response.GetResponseStream(), System.Text.Encoding.UTF8))
                        {
                            retval = reader.ReadToEnd();
                        }
                    }
                    return retval;
                }
            }
            catch
            {

            }
            return null;
        }
        public static string Post(string url, string data)
        {
            try
            {
                var request = GetHttpWebRequest(url);
                if (request != null)
                {
                    string retval = null;
                    init_Request(ref request);
                    request.Method = "POST";
                    request.ServicePoint.Expect100Continue = false;
                    request.ContentType = "application/json; charset=utf-8";
                    request.Timeout = 800;
                    var bytes = System.Text.UTF8Encoding.UTF8.GetBytes(data);
                    request.ContentLength = bytes.Length;
                    using (var stream = request.GetRequestStream())
                    {
                        stream.Write(bytes, 0, bytes.Length);
                    }
                    using (var response = request.GetResponse())
                    {
                        using (var reader = new System.IO.StreamReader(response.GetResponseStream()))
                        {
                            retval = reader.ReadToEnd();
                        }
                    }
                    return retval;
                }
            }
            catch
            {

            }
            return null;
        }

    }
}
