using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public class LogHelper
    {

        public static LogHelper Init { get; private set; }

        object sync = new object();

        static LogHelper()
        {
            Init = new LogHelper();
        }

        FileStream logstream;
        StreamWriter sw;
        DateTime today;

        private LogHelper()
        {
            _createSteream();
        }

        private void _createSteream()
        {
            logstream?.Close();
            logstream?.Dispose();
            sw?.Close();
            sw?.Dispose();
            string logpath = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent.FullName + @"\face-log";
            if (Directory.Exists(logpath) == false)//如果不存在就创建file文件夹
            {
                Directory.CreateDirectory(logpath);
            }
            string logfile = logpath + '\\' + DateTime.Now.ToLocalTime().ToString("yyyy-MM-dd") + ".txt";
            today = DateTime.Now.ToLocalTime().Date;
            if (!File.Exists(logfile))
            {
                logstream = new FileStream(logfile, FileMode.Create, FileAccess.Write);//创建写入文件
                sw = new StreamWriter(logstream);
            }
            else
            {
                logstream = new FileStream(logfile, FileMode.Append, FileAccess.Write);
                sw = new StreamWriter(logstream);
            }
        }

        public void Log(string info)
        {
            lock (sync)
            {
                DateTime now = DateTime.Now.ToLocalTime();
                if (now.Date > today.Date)
                {
                    _createSteream();
                    today = now.Date;
                }
                string infoWithTime = now + ":" + info;
                Console.WriteLine();
                Console.WriteLine(infoWithTime);
                sw.WriteLine(infoWithTime);
                sw.Flush();
            }
        }

    }
}
