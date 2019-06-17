using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using System.Configuration;
using FaceDistinguishSDK;
using FaceDistinguishSDK.NetWork;
namespace ConsoleApp1
{
    public class Worker
    {
        public bool StopFlag { get; set; } = false;
        SocketHelper socketHelper;
        Client client;
        string startTime = "00:00";
        string endTime = "23:59";

        TimeSpan start;
        TimeSpan end;
        public Worker()
        {
            client = new Client();
            startTime = ConfigurationManager.AppSettings["start_time"];
            endTime = ConfigurationManager.AppSettings["end_time"];
            start = DateTime.Parse(startTime).TimeOfDay;
            end = DateTime.Parse(endTime).TimeOfDay;
        }

        private PictureBox _getInitedPictureBox()
        {
            var pictureBox = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)(pictureBox)).BeginInit();
            pictureBox.Size = new Size(800, 600);
            ((System.ComponentModel.ISupportInitialize)(pictureBox)).EndInit();
            return pictureBox;
        }

        public void ReadFinish(string info)
        {
            LogHelper.Init.Log("收到消息:"+info);
        }
        public void StopWork()
        {
            socketHelper?.Stop();
        }

        public void StartWorkII(string logPath, string IP, Int32 port, string userName, string passWord)
        {
            socketHelper = new SocketHelper(ConfigurationManager.AppSettings["foreign_IP"], int.Parse(ConfigurationManager.AppSettings["foreign_port"]));
            if (socketHelper.IsClosed)
            {
                return;
            }
            socketHelper.OnReadFinish += ReadFinish;
            socketHelper.StartReceive();
            CameraConnection camera = new CameraConnection(logPath, IP, port, userName, passWord, CallBack);
            LogHelper.Init.Log("摄像连接成功！");
            while (true) {
                Thread.Sleep(5000);
                camera.HeartBeat();
            }
        }
        bool busy = false;
        public void CallBack(Bitmap picture)
        {
            TimeSpan now = DateTime.Now.TimeOfDay;
            if (now < start || now > end)
            {
                // 如果不在工作时间就返回，不发请求
                return;
            }
            if (!busy)
            {
                busy = true;
                Console.Write(" CallBack ");
                string restr = "[]";
                if (client.CheckFaceIdentity(picture, "test.jpg", new string[] { "manager", "user" }, out restr))
                {
                    if (restr.Length > 2 && restr.IndexOf("SDK_IMAGE_FACEDETECT_FAILED") == -1)
                    {
                        LogHelper.Init.Log("人脸识别成功：" + restr);
                        socketHelper.Write(restr);
                        picture.Save(Environment.CurrentDirectory + "\\img\\" + DateTime.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss") + "成功.jpg", ImageFormat.Jpeg);
                    }
                    else
                    {
                        LogHelper.Init.Log("人脸识别失败：" + restr);
                        picture.Save(Environment.CurrentDirectory + "\\img\\" + DateTime.Now.ToLocalTime().ToString("yyyyMMdd_HHmmss") + "失败.jpg", ImageFormat.Jpeg);
                    }
                }
                else
                {
                    LogHelper.Init.Log("人脸识别错误：:" + restr);
                }
            }
            busy = false;
        }
    }
}
