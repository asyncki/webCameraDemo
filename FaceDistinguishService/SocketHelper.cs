using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace FaceDistinguishService
{
    public class SocketHelper
    {
        Socket client;

        public delegate void ReadHandler(string info);

        public event ReadHandler OnReadFinish;

        byte[] receiveBuffer;

        object sync = new object();

        bool isClosed = true;

        public bool IsClosed { get => isClosed; set => isClosed = value; }

        IPEndPoint iPEndPoint;

        public SocketHelper(string ip, int port)
        {
            IPAddress ipAddress = IPAddress.Parse(ip);
            iPEndPoint = new IPEndPoint(ipAddress, port);
            Start();
        }

        public void Start()
        {
            if (IsClosed == false)
            {
                Stop();
            }
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            // 设置 TCP 心跳，空闲 60 秒检查一次，失败后每 5 秒检查一次
            byte[] inOptionValues = new byte[4 * 3];
            BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)60000).CopyTo(inOptionValues, 4);
            BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, 8);
            client.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
            try
            {
                client.Connect(iPEndPoint); //配置服务器IP与端口
                LogHelper.Init.Log("服务器连接成功！");
                receiveBuffer = new byte[1024];
                IsClosed = false;
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("服务器连接失败！" + e.Message);
                Stop();
            }
        }

        public void Stop()
        {
            if (IsClosed == false)
            {
                try
                {
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    client.Dispose();//素质三连
                    IsClosed = true;
                }
                catch (Exception e)
                {
                    LogHelper.Init.Log("sockethelper stop error: " + e.Message);
                }

            }
        }

        public void StartReceive()
        {
            try
            {
                client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, new object());
            }
            catch
            {
                Stop();
            }
        }

        public void ReceiveCallback(IAsyncResult re)
        {
            try
            {
                int len = client.EndReceive(re);
                if (len > 1)
                {
                    string info = Encoding.UTF8.GetString(receiveBuffer, 0, len);
                    if (info.Length > 1)
                    {
                        OnReadFinish(info);
                    }
                }
                if (!client.Connected) Start();
                client.BeginReceive(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ReceiveCallback, new object());
            }
            catch (Exception e)
            {
                LogHelper.Init.Log("ReceiveCallback error:" + e.Message);
                Stop();
                while (IsClosed)
                {
                    LogHelper.Init.Log("重新连接服务器");
                    Start();
                    // 重连等待1秒
                    if (IsClosed) Thread.Sleep(1000);
                    else StartReceive();
                }
            }

        }

        public void Write(string str)
        {
            try
            {
                lock (sync)
                {
                    int length = str.Length;
                    if (length > 1)
                    {
                        LogHelper.Init.Log("写入：" + length);
                        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(str);
                        client.Send(buffer, buffer.Length, SocketFlags.None);
                    }
                }
            }
            catch
            {
                Stop();
            }
        }
    }
}
