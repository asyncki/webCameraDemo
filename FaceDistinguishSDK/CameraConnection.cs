using System;
using System.Drawing;
using NeededSDK;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace FaceDistinguishSDK
{
    public class CameraConnection : IDisposable
    {
        private Int32 m_lUserID = -1;
        private bool m_bInitSDK = false;
        private Int32 m_lRealHandle = -1;

        public delegate void NewFaceAlarmHandler(Bitmap bitmap);
        public static event NewFaceAlarmHandler OnNewFaceAlarm;

        private CHCNetSDK.MSGCallBack_V31 mSGCallBack = new CHCNetSDK.MSGCallBack_V31(MSGCallBack_V31);

        public void HeartBeat()
        {
            Console.Write("0");
        }

        public CameraConnection(string logPath, string IP, Int32 port, string userName, string passWord, NewFaceAlarmHandler callBack)
        {
            //初始化
            m_bInitSDK = CHCNetSDK.NET_DVR_Init();
            if (m_bInitSDK)
            {
                CHCNetSDK.NET_DVR_SetLogToFile(3, logPath, true);
                CHCNetSDK.NET_DVR_DEVICEINFO_V30 DeviceInfo = new CHCNetSDK.NET_DVR_DEVICEINFO_V30();

                //登录设备 Login the device
                m_lUserID = CHCNetSDK.NET_DVR_Login_V30(IP, port, userName, passWord, ref DeviceInfo);
                if (m_lUserID < 0)
                {
                    uint err = CHCNetSDK.NET_DVR_GetLastError();
                    //登录失败，输出错误号
                    CHCNetSDK.NET_DVR_Cleanup();
                    throw new Exception("NET_DVR_Login_V30 failed, errorcode: " + err);
                }
                CHCNetSDK.NET_VCA_FACESNAPCFG conf = new CHCNetSDK.NET_VCA_FACESNAPCFG
                {
                    bySnapTime = 3,
                    bySnapInterval = 30,
                    bySnapThreshold = 6,
                    byGenerateRate = 3,
                    bySensitive = 3,
                    byReferenceBright = 40,
                    byMatchType = 1,
                    byMatchThreshold = 3,
                    struPictureParam = new CHCNetSDK.NET_DVR_JPEGPARA
                    {
                        wPicSize = 0xff,
                        wPicQuality = 0
                    },
                    wFaceExposureMinDuration = 30,
                    byFaceExposureMode = 0
                };
                conf.byFaceExposureMode = 0;
                conf.dwValidFaceTime = 1;
                conf.dwUploadInterval = 2;
                conf.dwFaceFilteringTime = 10;
                conf.dwSize = (uint)Marshal.SizeOf(conf);
                IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(conf));
                Marshal.StructureToPtr(conf, ptr, false);
                //if (CHCNetSDK.NET_DVR_SetDVRConfig(m_lUserID, 5002, 1, ptr, conf.dwSize))
                //{
                //    uint err = CHCNetSDK.NET_DVR_GetLastError();
                //    CHCNetSDK.NET_DVR_Logout(m_lUserID);
                //    CHCNetSDK.NET_DVR_Cleanup();
                //    throw new Exception("NET_DVR_SetDVRConfig errorCode: " + err);
                //}
                int lHandle = -1;
                //指定回调函数
                if (CHCNetSDK.NET_DVR_SetDVRMessageCallBack_V31(mSGCallBack, IntPtr.Zero))
                {
                    CHCNetSDK.NET_DVR_SETUPALARM_PARAM struAlarmParam = new CHCNetSDK.NET_DVR_SETUPALARM_PARAM();
                    struAlarmParam.byFaceAlarmDetection = 0;
                    struAlarmParam.dwSize = (uint)Marshal.SizeOf(struAlarmParam);
                    lHandle = CHCNetSDK.NET_DVR_SetupAlarmChan_V41(m_lUserID, ref struAlarmParam);
                    if (lHandle < 0)
                    {
                        CHCNetSDK.NET_DVR_Logout(m_lUserID);
                        CHCNetSDK.NET_DVR_Cleanup();
                        throw new Exception("NET_DVR_SetupAlarmChan_V41 errorCode: " + CHCNetSDK.NET_DVR_GetLastError());
                    }
                    OnNewFaceAlarm += callBack;
                }
                else
                {

                    CHCNetSDK.NET_DVR_Logout(m_lUserID);
                    CHCNetSDK.NET_DVR_Cleanup();
                    throw new Exception("NET_DVR_SetDVRMessageCallBack_V31 errorCode: " + CHCNetSDK.NET_DVR_GetLastError());

                }
            }
            else
            {
                CHCNetSDK.NET_DVR_Cleanup();
                throw new Exception("NET_DVR_Init failed, errorCode: " + CHCNetSDK.NET_DVR_GetLastError());
            }
        }

        private static bool MSGCallBack_V31(int lCommand, ref CHCNetSDK.NET_DVR_ALARMER pAlarmer, IntPtr pAlarmInfo, uint dwBufLen, IntPtr pUser)
        {
            Console.WriteLine("callback!");
            switch (lCommand)
            {
                case CHCNetSDK.COMM_UPLOAD_FACESNAP_RESULT: //人脸侦测报警信息
                    {
                        CHCNetSDK.NET_VCA_FACESNAP_RESULT struFaceDetectionAlarm;
                        struFaceDetectionAlarm = (CHCNetSDK.NET_VCA_FACESNAP_RESULT)Marshal.PtrToStructure(pAlarmInfo, typeof(CHCNetSDK.NET_VCA_FACESNAP_RESULT));
                        int len = Convert.ToInt32(struFaceDetectionAlarm.dwBackgroundPicLen);
                        Console.WriteLine("pic len:" + struFaceDetectionAlarm.dwBackgroundPicLen);
                        if (struFaceDetectionAlarm.dwBackgroundPicLen > 0 && !struFaceDetectionAlarm.pBuffer2.Equals(IntPtr.Zero))
                        {
                            byte[] pic = new byte[len];
                            Marshal.Copy(struFaceDetectionAlarm.pBuffer2, pic, 0, len);
                            Bitmap bb = BytesToBitmap(pic, len);
                            OnNewFaceAlarm(bb);
                        }
                    }
                    break;
                default:
                    Console.WriteLine("其他报警，报警信息类型: " + lCommand);
                    break;
            }

            return true;

        }

        public Bitmap GetShotPicture()
        {
            if (m_lRealHandle >= 0)
            {
                byte[] array = new byte[1024 * 1024];
                uint length = 0;
                CHCNetSDK.NET_DVR_JPEGPARA lpJpegPara = new CHCNetSDK.NET_DVR_JPEGPARA();
                lpJpegPara.wPicQuality = 0; //图像质量 Image quality
                lpJpegPara.wPicSize = 5; //抓图分辨率 Picture size: 2- 4CIF，0xff- Auto(使用当前码流分辨率)，抓图分辨率需要设备支持，更多取值请参考SDK文档
                if (CHCNetSDK.NET_DVR_CaptureJPEGPicture_NEW(m_lUserID, 1, ref lpJpegPara, array, 1024 * 768, ref length))
                {
                    return BytesToBitmap(array, (int)length);
                }
                else
                {
                    CHCNetSDK.NET_DVR_Cleanup();
                    throw new Exception("NET_DVR_CaptureJPEGPicture_NEW failed, errorCode: " + CHCNetSDK.NET_DVR_GetLastError());
                }
            }
            else
            {
                throw new Exception("Please startcapture firstly");
            }
        }

        public static Bitmap BytesToBitmap(byte[] Bytes, int length)
        {
            MemoryStream stream = null;
            try
            {
                stream = new MemoryStream(Bytes, 0, length);
                return new Bitmap(new Bitmap(stream));
            }
            catch (ArgumentNullException ex)
            {
                throw ex;
            }
            catch (ArgumentException ex)
            {
                throw ex;
            }
            finally
            {
                stream.Close();
            }
        }

        public void RealDataCallBack(Int32 lRealHandle, UInt32 dwDataType, ref byte pBuffer, UInt32 dwBufSize, IntPtr pUser)
        {
            //Console.WriteLine("RealDataCallBack");
        }

        public void StartCapture(PictureBox pictureBox)
        {
            if (m_lUserID < 0)
            {
                throw new Exception("Please login the device firstly");
            }

            if (m_lRealHandle < 0)
            {
                CHCNetSDK.NET_DVR_PREVIEWINFO lpPreviewInfo = new CHCNetSDK.NET_DVR_PREVIEWINFO();
                lpPreviewInfo.hPlayWnd = pictureBox.Handle;//预览窗口
                lpPreviewInfo.lChannel = 1;//预te览的设备通道
                lpPreviewInfo.dwStreamType = 0;//码流类型：0-主码流，1-子码流，2-码流3，3-码流4，以此类推
                lpPreviewInfo.dwLinkMode = 1;//连接方式：0- TCP方式，1- UDP方式，2- 多播方式，3- RTP方式，4-RTP/RTSP，5-RSTP/HTTP 
                lpPreviewInfo.bBlocked = false; //0- 非阻塞取流，1- 阻塞取流

                CHCNetSDK.REALDATACALLBACK RealData = new CHCNetSDK.REALDATACALLBACK(RealDataCallBack);//预览实时流回调函数
                IntPtr pUser = new IntPtr();//用户数据

                //打开预览 Start live view 
                m_lRealHandle = CHCNetSDK.NET_DVR_RealPlay_V40(m_lUserID, ref lpPreviewInfo, /*RealData*/null, pUser);
                if (m_lRealHandle < 0)
                {
                    CHCNetSDK.NET_DVR_Cleanup();
                    throw new Exception("NET_DVR_RealPlay_V40 failed, errorcode: " + CHCNetSDK.NET_DVR_GetLastError());
                    //预览失败，输出错误号
                }
                else
                {
                    //预览成功
                    //btnPreview.Text = "Stop Live View";
                    //timer1.Enabled = true;
                }
            }
            else
            {
                throw new Exception("cannot start twice!");
            }
        }

        public void StopCapture()
        {
            if (m_lRealHandle >= 0)
            {
                //停止预览 Stop live view 
                if (!CHCNetSDK.NET_DVR_StopRealPlay(m_lRealHandle))
                {
                    CHCNetSDK.NET_DVR_Cleanup();
                    throw new Exception("NET_DVR_StopRealPlay failed, errorcode: " + CHCNetSDK.NET_DVR_GetLastError());
                }
                m_lRealHandle = -1;
                //btnPreview.Text = "Live View";
            }
            else
            {
                throw new Exception("cannot stop twice");
            }
        }

        public void Dispose()
        {

            CHCNetSDK.NET_DVR_Cleanup();
        }
    }
}
