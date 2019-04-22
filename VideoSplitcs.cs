using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Wemew.Program.Assets.utility
{
    class VideoSplitcs
    {
        public class SplitFile
        {
#if DEBUG
            private static string FFMpegExecute { get; } = @"F:\ffmpeg\FFMpeg\ffmpeg.exe";
#else
        private static string FFMpegExecute { get; } = "ffmpeg.exe";
#endif
            private static string DurationExpression { get; } = "Duration: +([0-9]{2}:[0-9]{2}:[0-9]{2}.[0-9]{0,4})+,";

            public static void Split(string filename, string directory)
            {
                var pro = new ProcessExtension();

                var durationArgs = $"-i \"{filename}\"";
                var durationResult = pro.Execute(FFMpegExecute, durationArgs);

                if (Regex.IsMatch(durationResult, DurationExpression))
                {
                    var durationMatch = Regex.Match(durationResult, DurationExpression);
                    var durationString = durationMatch.Groups[1].Value;
                    var duration = TimeSpan.Parse(durationString);
                    var currentTime = TimeSpan.Zero;

                    var index = 1;
                    var splitSpanc = new TimeSpan(0, 3, 0);

                    while (currentTime < duration)
                    {
                        var time = duration.Subtract(currentTime);

                        if (time > splitSpanc)
                        {
                            var splitArgs = $"-ss {currentTime} -i \"{filename}\" -c copy -t {currentTime.Add(splitSpanc)} \"{directory}\\Video_{index++}.mkv\"";

                            pro.InvokeShell(FFMpegExecute, splitArgs);

                            currentTime = currentTime.Add(splitSpanc);
                        }
                        else
                        {
                            var splitArgs = $"-ss {currentTime} -i \"{filename}\" -c copy -t {time} \"{directory}\\Video{index++}.mkv\"";

                            pro.InvokeShell(FFMpegExecute, splitArgs);

                            currentTime = currentTime.Add(time);
                        }
                    }
                }
                else
                {
                    throw new Exception($"FileName:{filename} Read File Struct Error");
                }
            }
        }

        public class ProcessExtension
        {
            public void InvokeShell(string filename, string args)
            {
                var psi = new ProcessStartInfo(filename, args);

                psi.UseShellExecute = false;   // 是否使用外壳程序 
                psi.CreateNoWindow = true;   //是否在新窗口中启动该进程的值 
                psi.RedirectStandardInput = true;  // 重定向输入流 
                psi.RedirectStandardOutput = true;  //重定向输出流 
                psi.RedirectStandardError = true;  //重定向错误流 

                //启动
                using (var proc = Process.Start(psi))
                {
                    var blockSize = 1024;
                    var blockBytes = new char[blockSize];
                    var len = 0;

                    //错误信息
                    using (var sr = proc.StandardError)
                    {
                        while ((len = sr.Read(blockBytes, 0, blockSize)) > 0)
                        {
                            var line = new string(blockBytes);

                            Console.WriteLine($"{line}");
                        }
                    }

                    //开始读取
                    using (var sr = proc.StandardOutput)
                    {
                        while ((len = sr.Read(blockBytes, 0, blockSize)) > 0)
                        {
                            var line = new string(blockBytes);

                            Console.WriteLine($"{line}");
                        }
                    }

                    if (!proc.HasExited)
                    {
                        proc.Kill();
                    }
                }
            }

            public string Execute(string filename, string args)
            {
                var psi = new ProcessStartInfo(filename, args);

                psi.UseShellExecute = false;   // 是否使用外壳程序 
                psi.CreateNoWindow = true;   //是否在新窗口中启动该进程的值 
                psi.RedirectStandardInput = true;  // 重定向输入流 
                psi.RedirectStandardOutput = true;  //重定向输出流 
                psi.RedirectStandardError = true;  //重定向错误流 

                //启动
                using (var proc = Process.Start(psi))
                {
                    //错误信息
                    using (var sr = proc.StandardError)
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
        }
    }
}
