using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;
using System.ServiceProcess;
using System.Timers;

namespace MyRemoteService
{
    public partial class MyRemoteService : ServiceBase
    {
        private string RootPath = AppDomain.CurrentDomain.BaseDirectory;

        public MyRemoteService()
        {
            InitializeComponent();
            // 日志
            string eventSource = "MyRemoteSource";
            string eventLog = "MyRemoteFile";

            eventLog1 = new EventLog();
            if (!EventLog.SourceExists(eventSource))
            {
                EventLog.CreateEventSource(eventSource, eventLog);
            }
            eventLog1.Source = eventSource;
            eventLog1.Log = eventLog;
        }

        protected override void OnStart(string[] args)
        {
            // 每分钟定时执行 OnTimer
            Timer timer = new Timer();
            timer.Interval = 60000; // 60 seconds
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            eventLog1.WriteEntry("MyRemoteService Start!");
        }

        protected override void OnStop()
        {
            eventLog1.WriteEntry("MyRemoteService Stop!");
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            bool isRun = IsRunFrp();
            if (!isRun)
            {
                RunFrp();
            }
        }

        private bool IsRunFrp()
        {
            Process[] proc = Process.GetProcessesByName("frpc");
            return proc.Length > 0;
        }

        private void RunFrp()
        {
            string frpPath = System.IO.Path.Combine(RootPath, "Frp\\frpc.exe");
            string iniPath = System.IO.Path.Combine(RootPath, "Frp\\frpc.ini");
            string cmd = $"{frpPath} -c {iniPath}";
            RunCmd(cmd);
        }

        private string RunCmd(string cmd)
        {
            //不管命令是否成功均执行exit命令，否则当调用ReadToEnd()方法时，会处于假死状态
            cmd = cmd.Trim().TrimEnd('&') + "&exit";
            string result = string.Empty;
            Process process = new Process();
            try
            {
                process.StartInfo.FileName = "cmd.exe";
                process.StartInfo.UseShellExecute = false;       //是否使用操作系统shell启动
                process.StartInfo.RedirectStandardInput = true;  //接受来自调用程序的输入信息
                process.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
                process.StartInfo.RedirectStandardError = true;  //重定向标准错误输出
                process.StartInfo.CreateNoWindow = true;         //不显示程序窗口
                process.Start();                                 //启动程序
                process.StandardInput.WriteLine(cmd);            //向cmd窗口写入命令
                process.StandardInput.AutoFlush = true;
                result = process.StandardOutput.ReadToEnd();     //获取cmd窗口的输出信息
                process.WaitForExit();                           //等待程序执行完退出进程
                process.Close();
            }
            catch (Exception)
            {
                result = string.Empty;
            }
            finally
            {
                process.Dispose();
            }
            return result;
        }
    }
}
