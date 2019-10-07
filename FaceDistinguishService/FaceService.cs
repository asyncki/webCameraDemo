using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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
                        worker.StartWorkII(ConfigurationManager.AppSettings["logpath"],
                            ConfigurationManager.AppSettings["camera_IP"],
                            int.Parse(ConfigurationManager.AppSettings["camera_port"]),
                            ConfigurationManager.AppSettings["userName"],
                            ConfigurationManager.AppSettings["passWord"]);
                        LogHelper.Init.Log("主循环try块正常结束");
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
                        worker.StopWork();
                        GC.Collect();
                        LogHelper.Init.Log("休眠60秒..");
                        Thread.Sleep(60000);
                    }
                }
            });
            Thread thread = new Thread(workThread);
            thread.Start();
        }
        protected override void OnStop()
        {
            worker.StopWork();
            Thread.Sleep(5000);
        }
    }
}
