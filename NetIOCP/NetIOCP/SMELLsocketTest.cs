using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;

namespace NetIOCP
{
    /// <summary>
    /// 该类用于测试环形缓冲区
    /// </summary>
    class SMELLsocketTest
    {
        private Socket socketSmell = null;
        private static int MAX_BUFFER_LEN = 1024;
        private static readonly object LockReceiveBuffer = new object();//for lock
        private static RingBufferManager receiveBufferManager = new RingBufferManager(10240);
        
        public SMELLsocketTest()
        {
            socketSmell = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketSmell.Bind(new IPEndPoint(GetIPAddress(), 8104));
            socketSmell.Listen(20);//监听最多20个
            socketSmell.BeginAccept(new AsyncCallback(smellClientAccepted), socketSmell);
           

        }

        public void smellClientAccepted(IAsyncResult ar)
        {
            Console.WriteLine("########## A Smell client connected!");

            var socket = ar.AsyncState as Socket;
            Socket client = socket.EndAccept(ar);

            //实现每隔两秒钟给服务器发一个消息
            //这里我们使用了一个定时器
            var timer = new System.Timers.Timer();
            timer.Interval = 10000D;
            timer.Enabled = true;
            timer.Elapsed += (o, a) =>
            {
                //检测客户端Socket的状态
                if (client.Connected)
                {
                    try
                    {
                        client.Send(Encoding.Unicode.GetBytes("Message from server at " + DateTime.Now.ToString()));
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                else
                {
                    timer.Stop();
                    timer.Enabled = false;
                    Console.WriteLine("Client is disconnected, the timer is stop.");
                }
            };
            timer.Start();


            //接收客户端的消息(这个和在客户端实现的方式是一样的）
            client.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), client);

            //准备接受下一个客户端请求
            socket.BeginAccept(new AsyncCallback(smellClientAccepted), socket);
        }

        public static void ReceiveMessage(IAsyncResult ar)
        {

            try
            {
                var socket = ar.AsyncState as Socket;

                //方法参考：http://msdn.microsoft.com/zh-cn/library/system.net.sockets.socket.endreceive.aspx
                var length = socket.EndReceive(ar);
                //读取出来消息内容
                //将数据扔向环形缓冲区
                if (length<=0)
                {
                    throw new Exception("disconnect");
                }
              
                //存数据到缓冲区
                if (length > 0)
                {
                    lock (LockReceiveBuffer)
                    {
                        while (length+ receiveBufferManager.DataCount>MAX_BUFFER_LEN)//超出缓存区大小
                        {
                            //如果超出缓冲区，则阻止一会，等待空间。
                            Monitor.Wait(LockReceiveBuffer, 10000);
                            Console.WriteLine(System.DateTime.Now.ToString()+"超出缓冲区，阻止一会");
                        }

                        receiveBufferManager.WriteBuffer(buffer,0,length);
                        Monitor.PulseAll(LockReceiveBuffer);
                    }
                }

                byte[] freame_byte = new byte[40];
                //从缓冲区取数据
                lock (LockReceiveBuffer)
                {
                  receiveBufferManager.ReadBuffer(freame_byte,0,40);
                    receiveBufferManager.Clear(40);
                }

                var message = Encoding.Unicode.GetString(freame_byte, 0, freame_byte.Length);
                //显示消息
                Console.WriteLine(message);

                //接收下一个消息(因为这是一个递归的调用，所以这样就可以一直接收消息了）
                socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveMessage), socket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static byte[] buffer = new byte[512];
        public void Init()
        {

        }

        public IPAddress GetIPAddress()
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
