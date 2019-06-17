using System;
using System.Threading;
using System.Configuration;


namespace ConsoleApp1
{
    class Program
    {

        static void Main(string[] args)
        {
            Worker worker=null;
            string lastError=null;
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
                        else {
                            lastError = e.Message;
                            LogHelper.Init.Log("错误：" + e.Message + e.StackTrace);
                        }
                        worker.StopWork();
                        GC.Collect();
                        LogHelper.Init.Log("休眠6秒..");
                        Thread.Sleep(6000);
                    }
                }
            });
            Thread thread = new Thread(workThread);
            thread.Start();
            string cmd;
            do
            {
                cmd = Console.ReadLine();
            }
            while (cmd != "exit");
            if (worker != null)
            {
                worker.StopFlag = true;
            }
        }
    }
}
