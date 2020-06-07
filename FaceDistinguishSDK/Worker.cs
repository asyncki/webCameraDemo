using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Configuration;
using FaceDistinguishSDK.NetWork;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FaceDistinguishSDK
{
    public class Worker
    {
        public bool StopFlag { get; set; } = false;
        SocketHelper socketHelper;
        Client client;
        CameraConnection camera;
        string startTime;
        string endTime;

        string imgpath;

        TimeSpan start;
        TimeSpan end;
        public Worker()
        {
            client = new Client();
            startTime = ConfigurationManager.AppSettings["start_time"];
            endTime = ConfigurationManager.AppSettings["end_time"];
            LogHelper.Init.Log("工作时间段:" + startTime + "-" + endTime);
            start = DateTime.Parse(startTime).TimeOfDay;
            end = DateTime.Parse(endTime).TimeOfDay;
        }

        public void ReadFinish(string info)
        {
            LogHelper.Init.Log("收到消息:" + info);
        }
        public void StopWork()
        {
            socketHelper?.Stop();
            camera?.Dispose();
        }

        public void StartWork()
        {
            string path = ConfigurationManager.AppSettings["path"];
            string IP = ConfigurationManager.AppSettings["camera_IP"];
            int port = int.Parse(ConfigurationManager.AppSettings["camera_port"]);
            string userName = ConfigurationManager.AppSettings["userName"];
            string passWord = ConfigurationManager.AppSettings["passWord"];
            socketHelper = new SocketHelper(ConfigurationManager.AppSettings["foreign_IP"], int.Parse(ConfigurationManager.AppSettings["foreign_port"]));
            if (socketHelper.IsClosed)
            {
                return;
            }
            imgpath = path + @"\img\";
            socketHelper.OnReadFinish += ReadFinish;
            socketHelper.StartReceive();
            camera = new CameraConnection(path, IP, port, userName, passWord, CallBack);
            LogHelper.Init.Log("摄像连接成功！");
            while (true)
            {
                Thread.Sleep(5000);
                camera.HeartBeat();
            }
        }

        bool busy = false;
        DateTime lastSuccess = DateTime.Now;
        public void CallBack(Bitmap picture)
        {
            try
            {
                TimeSpan nowTimeOfDay = DateTime.Now.TimeOfDay;
                string dateTimeStr = DateTime.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss");
                if (nowTimeOfDay < start || nowTimeOfDay > end)
                {
                    // 如果不在工作时间就返回，不发请求
                    LogHelper.Init.Log("工作时间外的触发。");
                    picture.Save(imgpath + dateTimeStr + "工时外.jpg", ImageFormat.Jpeg);
                    return;
                }
                DateTime startT = DateTime.Now;
                if (startT.Subtract(lastSuccess).TotalMilliseconds < 1000)
                {
                    return;
                }
                if (!busy)
                {
                    busy = true;
                    if (client.CheckFaceIdentity(picture, "test.jpg", new string[] { "manager", "user" }, out string restr))
                    {
                        if (restr.Length > 2 && restr.IndexOf("SDK_IMAGE_FACEDETECT_FAILED") == -1)
                        {
                            LogHelper.Init.Log("人脸识别成功：" + restr);
                            socketHelper.Write(restr);
                            picture.Save(imgpath + dateTimeStr + "成功.jpg", ImageFormat.Jpeg);
                            lastSuccess = DateTime.Now;
                        }
                        else
                        {
                            LogHelper.Init.Log("人脸识别失败：" + restr);
                            picture.Save(imgpath + dateTimeStr + "失败.jpg", ImageFormat.Jpeg);
                        }
                    }
                    else
                    {
                        LogHelper.Init.Log("人脸识别错误：" + restr);
                        picture.Save(imgpath + dateTimeStr + "错误.jpg", ImageFormat.Jpeg);
                    }
                    busy = false;
                }
                else
                {
                    LogHelper.Init.Log("人脸识别遇忙。");
                    picture.Save(imgpath + dateTimeStr + "遇忙.jpg", ImageFormat.Jpeg);
                }
                DateTime afterTime = DateTime.Now;
                TimeSpan timeUse = afterTime.Subtract(startT);
                
                LogHelper.Init.Log("耗时：" + timeUse.TotalMilliseconds+"ms");
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("error in callback:" + e.Message);
            }
        }
    }
}
