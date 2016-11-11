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
using NetIOCP.Data;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "App.config", Watch = true)]

namespace NetIOCP
{
    class Program
    {
        public static ILog Logger;

        public static AsyncSocketServer AsyncSocketSvr;
        public static DataManager dm;
        public static string FileDirectory;
        static void Main(string[] args)
        {
            DateTime currentTime = DateTime.Now;
            log4net.GlobalContext.Properties["LogDir"] = currentTime.ToString("yyyyMM");
            log4net.GlobalContext.Properties["LogFileName"] = "_SocketAsyncServer" + currentTime.ToString("yyyyMMdd");
            Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

            //todo:性能测试
            int port = 0;
            port = 8103;
            int parallelNum = 100;
            parallelNum = 100;//并发数量
            int socketTimeOutMS = 0;
            socketTimeOutMS = 5 * 60 * 1000;

            Logger.Debug("for test");
            Logger.Info("for test info");
            Logger.Error("for test error");

            dm = new DataManager();
            
            AsyncSocketSvr = new AsyncSocketServer(parallelNum);
            AsyncSocketSvr.SocketTimeOutMS = socketTimeOutMS;
            AsyncSocketSvr.Init();
            IPEndPoint listenPoint = new IPEndPoint(GetIPAddress(), port);
            AsyncSocketSvr.Start(listenPoint);

            Console.WriteLine("Press any key to terminate the server process....");


            //for ring buffer test--begin--OK
            ///正式环境中，把SMELLsocketTest.cs/RingBufferManager.cs删除
            //    SMELLsocketTest sst = new SMELLsocketTest();
            //for ring buffer test--end--OK

            Console.ReadKey();

        }

        public static IPAddress GetIPAddress()
        {
            IPAddress m_listenIP = null;
            IPAddress[] AddressList = Dns.GetHostByName(Dns.GetHostName()).AddressList;
            if (AddressList.Length == 1)
            {
                m_listenIP = AddressList[0];
            }
            else if (AddressList.Length > 1)//目前此项只针对吴中水利项目，10.38.10.24为云主机，具备双网卡，我们使用第二块网卡
            {
                m_listenIP = AddressList[1];
            }
            return m_listenIP;
        }
    }
}
