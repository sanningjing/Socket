using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using System.Configuration;
using log4net;
using log4net.Repository.Hierarchy;

namespace NetIOCP
{
    class Program
    {
        public static ILog Logger;

        public static AsyncSocketServer AsyncSocketSvr;
        public static string FileDirectory;
        static void Main(string[] args)
        {
            DateTime currentTime = DateTime.Now;
            log4net.GlobalContext.Properties["LogDir"] = currentTime.ToString("yyyyMM");
            log4net.GlobalContext.Properties["LogFileName"] = "_SocketAsyncServer" + currentTime.ToString("yyyyMMdd");
            Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            //todo:读配置文件得出的。
             //  Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //   FileDirectory = config.AppSettings.Settings["FileDirectory"].Value;
            //             if (FileDirectory == "")
            //                 FileDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Files");
            //             if (!Directory.Exists(FileDirectory))
            //                 Directory.CreateDirectory(FileDirectory);
            //todo:性能测试
            int port = 0;
          //  if (!(int.TryParse(config.AppSettings.Settings["Port"].Value, out port)))
                port = 8104;
            int parallelNum = 100;
          //  if (!(int.TryParse(config.AppSettings.Settings["ParallelNum"].Value, out parallelNum)))
                parallelNum = 100;//并发数量
            int socketTimeOutMS = 0;
            //if (!(int.TryParse(config.AppSettings.Settings["SocketTimeOutMS"].Value, out socketTimeOutMS)))
                socketTimeOutMS = 5 * 60 * 1000;

             AsyncSocketSvr = new AsyncSocketServer(parallelNum);
             AsyncSocketSvr.SocketTimeOutMS = socketTimeOutMS;
             AsyncSocketSvr.Init();
             IPEndPoint listenPoint = new IPEndPoint(IPAddress.Parse("192.168.10.110"), port);
             AsyncSocketSvr.Start(listenPoint);

            Console.WriteLine("Press any key to terminate the server process....");
     

            //for ring buffer test--begin--OK

    　 //    SMELLsocketTest sst = new SMELLsocketTest();
            //for ring buffer test--end--OK

            Console.ReadKey();


        }
    }
}
