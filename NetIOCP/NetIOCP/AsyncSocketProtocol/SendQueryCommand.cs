using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using NetIOCP.AsyncSocketProtoclCore;

namespace NetIOCP.AsyncSocketProtocol
{
    class SendQueryCommand : BaseSocketProtocol
    {
        public SendQueryCommand(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_asyncSocketServer = asyncSocketServer;
            MoinitorTimerStart();

        }
        private System.Timers.Timer m_MonitorTimer = null;
        private const int m_MonitorTimerInterval = 60 * 1000;//1分钟发送一次

        private AsyncSocketServer m_asyncSocketServer;

        /// <summary>
        /// 监控定时初始化
        /// </summary>
        public void MoinitorTimerStart()
        {
            m_MonitorTimer = new System.Timers.Timer();
            m_MonitorTimer.Interval = m_MonitorTimerInterval;
            m_MonitorTimer.Elapsed += new System.Timers.ElapsedEventHandler(MonitorSocketStart);
            m_MonitorTimer.AutoReset = true;
            m_MonitorTimer.Enabled = true;
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
                    //SOCKET不为空，则可以发送消息
                    if (ListSoc[i].ConnectSocket != null)
                    {
                        //   Console.WriteLine("{0} is time out!!!", ListSoc[i].ConnectSocket.ToString());
                        //   //todo 服务器主动断掉这个链接，其他操作待添加
                        if (ListSoc[i].AsyncSocketInvokeElement != null)
                        {
                            ///当前端口发送不冲突

                            byte[] bufferUTF8 = str2HexByte(ListSoc[i].StrQueryCommand);

                            AsyncSendBufferManager asyncSendBufferManager = ListSoc[i].SendBuffer;
                            asyncSendBufferManager.StartPacket();
                            // asyncSendBufferManager.DynamicBufferManager.WriteInt(totalLength, false); //写入总大小
                            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(bufferUTF8, 0, bufferUTF8.Length);

                            asyncSendBufferManager.EndPacket();
                            // 
                            bool result = true;
                            Console.WriteLine("ListSoc[i].SendAsyncState = ?" + ListSoc[i].SendAsyncState);
                            //如果发送端口被占有
                            while (ListSoc[i].SendAsyncState)
                            {
                                Thread.Sleep(100);
                                Console.WriteLine("sleep 100");
                            }
                           
                            int packetOffset = 0;
                            int packetCount = 0;
                            if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                            {
                                ListSoc[i].SendAsyncState = true;
                                Console.WriteLine(System.DateTime.Now + "ListSoc[i].SendAsyncState = true;");
                                result = m_asyncSocketServer.SendAsyncEvent(ListSoc[i].ConnectSocket,
                                    ListSoc[i].SendEventArgs,
                                    asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset,
                                    packetCount);
                            }
                      
                        }



                    }
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("##############" + ex);
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

        /// <summary> 
        /// 字符串转16进制字节数组 
        /// </summary> 
        /// <param name="hexString"></param> 
        /// <returns></returns> 
        public byte[] str2HexByte(string hexString)
        {
            hexString = hexString.Replace(" ", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }
    }
}
