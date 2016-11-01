using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIOCP.AsyncSocketCore
{
    /// <summary>
    /// 守护进程
    /// 定时检查客户端是否掉线：
    /// 一定时间间隔没有数据上传则为掉线
    /// 使用timer实现
    /// </summary>
    class DaemonThread:Object
    {
        private System.Timers.Timer m_MonitorTimer = null;//设置时间间隔30S执行一次
        private const int m_MonitorTimerInterval = 30 * 1000;//30S//间隔30S执行一次函数

        private AsyncSocketServer m_asyncSocketServer;

        public DaemonThread(AsyncSocketServer asyncSocketServer)
        {
            m_asyncSocketServer = asyncSocketServer;
         //   MonitorTimerStart();
        }

        /// <summary>
        /// 设备离线状态监测定时器初始化
        /// </summary>
        public void MonitorTimerStart()
        {
            if (m_MonitorTimer == null)
            {
                m_MonitorTimer = new System.Timers.Timer();
                m_MonitorTimer.Interval = m_MonitorTimerInterval;
                m_MonitorTimer.Elapsed += new System.Timers.ElapsedEventHandler(MonitorSocketStart);
                m_MonitorTimer.AutoReset = true;
                m_MonitorTimer.Enabled = true;
            }
        }

        public void MonitorSocketStart(object source, System.Timers.ElapsedEventArgs e)
        {

            //轮巡SOCKET管理类，查数据接收超时
            AsyncSocketUserToken[] ListSoc = null;
            m_asyncSocketServer.AsyncSocketUserTokenList.CopyList(ref ListSoc);

            for (int i = 0; i < ListSoc.Length; i++)
            {
                try
                {
                    if ((DateTime.Now - ListSoc[i].ActiveDateTime).TotalMilliseconds > m_asyncSocketServer.SocketTimeOutMS)
                    {
                        //   Console.WriteLine("{0} is time out!!!", ListSoc[i].ConnectSocket.ToString());
                        //   //todo 服务器主动断掉这个链接，其他操作待添加
                        lock (ListSoc[i])
                        {
                            //断开SOCKET连接
                            m_asyncSocketServer.CloseClientSocket(ListSoc[i]);
                        }

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
               
            }

        }

        public bool MonitorSocketStop()
        {
            try
            {
                m_MonitorTimer.Stop();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }

        }
    }
}
