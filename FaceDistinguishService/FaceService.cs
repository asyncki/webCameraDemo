using FaceDistinguishSDK;
using System;
using System.ServiceProcess;
using System.Threading;

namespace FaceDistinguishService
{
    public partial class FaceService : ServiceBase
    {

        Worker worker = null;
        string lastError = null;

        public FaceService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            ThreadStart workThread = new ThreadStart(() =>
            {
                while (true)
                {
                    LogHelper.Init.Log("主循环启动...");
                    try
                    {
                        worker = new Worker();
                        //worker.StartWork(@"C:\Users\test\Desktop\mylog", "172.27.16.94", 8000, "admin", "12345");
                        worker.StartWork();
                    }
                    catch (Exception e)
                    {
                        if (e.Message.Equals(lastError))
                        {
                            //do nothing
                            LogHelper.Init.Log("相同的错误..");
                        }
                        else
                        {
                            lastError = e.Message;
                            LogHelper.Init.Log("错误：" + e.Message + e.StackTrace);
                        }
                    }
                    try
                    {
                        worker.StopWork();
                        GC.Collect();
                        LogHelper.Init.Log("休眠60秒..");
                        Thread.Sleep(60000);
                    }
                    catch (Exception e)
                    {
                        // 无能为力
                        LogHelper.Init.Log("错误：" + e.Message + e.StackTrace);
                    }
                }
            });
            Thread thread = new Thread(workThread);
            thread.Start();
        }
        protected override void OnStop()
        {
            worker.StopWork();
            LogHelper.Init.close();
            Thread.Sleep(2000);
        }
    }
}
