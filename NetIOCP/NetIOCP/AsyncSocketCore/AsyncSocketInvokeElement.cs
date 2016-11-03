using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIOCP.AsyncSocketCore
{

    /// <summary>
    /// //异步Socket调用对象，所有的协议处理都从本类继承
    ///供不同协议的传感器使用。
    /// </summary>
    public class AsyncSocketInvokeElement
    {
        protected AsyncSocketServer m_asyncSocketServer;

        protected AsyncSocketUserToken m_asyncSocketUserToken;
        public AsyncSocketUserToken AsyncSocketUserToken { get { return m_asyncSocketUserToken; } }

        private bool m_netByteOrder;
        public bool NetByteOrder { get { return m_netByteOrder; } set { m_netByteOrder = value; } } //长度是否使用网络字节顺序

        protected IncomingDataParser m_incomingDataParser; //协议解析器，用来解析客户端接收到的命令
        public OutgoingDataAssembler m_outgoingDataAssembler; //协议组装器，用来组织服务端返回的命令

        protected bool m_sendAsync; //标识是否有发送异步事件

        protected DateTime m_connectDT;
        public DateTime ConnectDT { get { return m_connectDT; } }

        protected DateTime m_activeDT;
        public DateTime ActiveDT { get { return m_activeDT; } }

        public AsyncSocketInvokeElement(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
        {
            m_asyncSocketServer = asyncSocketServer;
         //   AsyncSocketUserToken.SendAsyncState = false;
            m_asyncSocketUserToken = asyncSocketUserToken;

            m_netByteOrder = false;

            m_incomingDataParser = new IncomingDataParser();
            m_outgoingDataAssembler = new OutgoingDataAssembler();

            m_sendAsync = false;
        
            m_connectDT = DateTime.UtcNow;
            m_activeDT = DateTime.UtcNow;
        }

        public virtual void Close()
        {
        }

        public virtual bool ProcessReceive(byte[] buffer, int offset, int count) //接收异步事件返回的数据，用于对数据进行缓存和分包
        {
            m_activeDT = DateTime.UtcNow;
            DynamicBufferManager receiveBuffer = m_asyncSocketUserToken.ReceiveBuffer;

            receiveBuffer.WriteBuffer(buffer, offset, count);
            bool result = true;
            while (receiveBuffer.DataCount > sizeof(int))//4
            {
                //按照长度分包
               // int packetLength = BitConverter.ToInt32(receiveBuffer.Buffer, 0); //获取包长度//协议中自带的数据长度
                int packetLength =15;
                if (NetByteOrder)
                    packetLength = System.Net.IPAddress.NetworkToHostOrder(packetLength); //把网络字节顺序转为本地字节顺序


                if ((packetLength > 10 * 1024 * 1024) | (receiveBuffer.DataCount > 10 * 1024 * 1024)) //最大Buffer异常保护
                    return false;

                if ((receiveBuffer.DataCount - sizeof(int)) >= packetLength) //收到的数据达到包长度
                {
                    result = ProcessPacket(receiveBuffer.Buffer, sizeof(int), packetLength);
                    if (result)
                        receiveBuffer.Clear(packetLength + sizeof(int)); //从缓存中清理
                    else
                        return result;
                }
                else
                {
                    return true;
                }
            }
            return true;
        }

        public virtual bool ProcessPacket(byte[] buffer, int offset, int count) //处理分完包后的数据，把命令和数据分开，并对命令进行解析
        {
            if (count < sizeof(int))
                return false;
            int commandLen = BitConverter.ToInt32(buffer, offset); //取出命令长度
            string tmpStr = Encoding.UTF8.GetString(buffer, offset + sizeof(int), commandLen);
            if (!m_incomingDataParser.DecodeProtocolText(tmpStr)) //解析命令
                return false;

            return ProcessCommand(buffer, offset + sizeof(int) + commandLen, count - sizeof(int) - commandLen); //处理命令
        }



        public virtual bool ProcessCommand(byte[] buffer, int offset, int count) //处理具体命令，子类从这个方法继承，buffer是收到的数据
        {
            return true;
        }

        public virtual bool SendCompleted()
        {
            m_activeDT = DateTime.UtcNow;
            m_asyncSocketUserToken.SendAsyncState = false;
            Console.WriteLine("public virtual bool SendCompleted(): m_asyncSocketUserToken.SendAsyncState = false;");
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.ClearFirstPacket(); //清除已发送的包
            int offset = 0;
            int count = 0;
            if (asyncSendBufferManager.GetFirstPacket(ref offset, ref count))
            {
                // m_sendAsync = true;
                m_asyncSocketUserToken.SendAsyncState = true;
                Console.WriteLine("public virtual bool SendCompleted():m_asyncSocketUserToken.SendAsyncState = true;");
                return m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                    asyncSendBufferManager.DynamicBufferManager.Buffer, offset, count);
            }
            else
                return SendCallback();
        }

        //发送回调函数，用于连续下发数据
        public virtual bool SendCallback()
        {
            return true;
        }

        public virtual bool DoSendResultSmell()
        {
            string commandText = m_outgoingDataAssembler.GetProtocolText();
            //byte[] bufferUTF8 = Encoding.UTF8.GetBytes(commandText);
            byte[] bufferUTF8 = str2HexByte(commandText);
            int totalLength = bufferUTF8.Length; //获取总大小
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.StartPacket();
             asyncSendBufferManager.DynamicBufferManager.WriteInt(totalLength, false); //写入总大小
             asyncSendBufferManager.DynamicBufferManager.WriteInt(bufferUTF8.Length, false); //写入命令大小
             asyncSendBufferManager.DynamicBufferManager.WriteBuffer(bufferUTF8); //写入命令内容
             asyncSendBufferManager.EndPacket();

            bool result = true;
            if (!m_asyncSocketUserToken.SendAsyncState)
            {
                int packetOffset = 0;
                int packetCount = 0;
                if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                {
                    m_asyncSocketUserToken.SendAsyncState = true;
                    result = m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                        asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset, packetCount);
                }
            }
            return result;
        }

        public bool DoSendResult()
        {
            string commandText = m_outgoingDataAssembler.GetProtocolText();
            byte[] bufferUTF8 = Encoding.UTF8.GetBytes(commandText);
            int totalLength = sizeof(int) + bufferUTF8.Length; //获取总大小
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.StartPacket();
            asyncSendBufferManager.DynamicBufferManager.WriteInt(totalLength, false); //写入总大小
            asyncSendBufferManager.DynamicBufferManager.WriteInt(bufferUTF8.Length, false); //写入命令大小
            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(bufferUTF8); //写入命令内容
            asyncSendBufferManager.EndPacket();

            bool result = true;
            if (!m_asyncSocketUserToken.SendAsyncState)
            {
                int packetOffset = 0;
                int packetCount = 0;
                if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                {
                    m_asyncSocketUserToken.SendAsyncState = true;
                    result = m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                        asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset, packetCount);
                }
            }
            return result;
        }

        public bool DoSendResult(byte[] buffer, int offset, int count)
        {
            string commandText = m_outgoingDataAssembler.GetProtocolText();
            byte[] bufferUTF8 = Encoding.UTF8.GetBytes(commandText);
            int totalLength = sizeof(int) + bufferUTF8.Length + count; //获取总大小
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.StartPacket();
            asyncSendBufferManager.DynamicBufferManager.WriteInt(totalLength, false); //写入总大小
            asyncSendBufferManager.DynamicBufferManager.WriteInt(bufferUTF8.Length, false); //写入命令大小
            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(bufferUTF8); //写入命令内容
            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(buffer, offset, count); //写入二进制数据
            asyncSendBufferManager.EndPacket();

            bool result = true;
            if (!m_asyncSocketUserToken.SendAsyncState)
            {
                int packetOffset = 0;
                int packetCount = 0;
                if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                {
                    m_asyncSocketUserToken.SendAsyncState = true;
                    result = m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                        asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset, packetCount);
                }
            }
            return result;
        }

        public bool DoSendBuffer(byte[] buffer, int offset, int count) //不是按包格式下发一个内存块，用于日志这类下发协议
        {
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.StartPacket();
            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(buffer, offset, count);
            asyncSendBufferManager.EndPacket();

            bool result = true;
            if (!m_asyncSocketUserToken.SendAsyncState)
            {
                int packetOffset = 0;
                int packetCount = 0;
                if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                {
                    m_asyncSocketUserToken.SendAsyncState = true;
                    result = m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                        asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset, packetCount);
                }
            }
            return result;
        }

        /// <summary>
        /// 心跳包处理
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public virtual bool HeartBeatAnalyse(byte[] byteArray)
        {
            bool bReturn = false;
            return bReturn;

        }

        /// <summary>
        /// 数据包处理
        /// </summary>
        /// <param name="byteArray"></param>
        /// <returns></returns>
        public virtual bool DataPackageAnalyse(byte[] byteArray,int count)
        {
            bool bReturn = false;
            return bReturn;
        }

        /// <summary>
        /// 较验//
        /// </summary>
        /// <param name=byteArray></param>
        /// <returns></returns>
        public virtual byte CheckSum(byte[] byteArray)
        {
            byte bresult = 0;
         //   byte[] byteArray = str2HexByte(cmd);
            int result = 0;
            foreach (byte b in byteArray)
            {
                result += b;
            }

            bresult = Convert.ToByte((result % 256));

            return bresult;

        }

        #region
        ///公共函数
        /// <summary> 
        /// 指定长度，字节数组转16进制字符串 
        /// </summary> 
        /// <param name="bytes"></param> 
        /// <returns></returns> 
        public  string byte2HexStr(byte[] bytes,int count)
        {
            string returnStr = "";
            if (bytes != null)
            {
                for (int i = 0; i < count; i++)
                {
                    returnStr += bytes[i].ToString("X2");
                }
            }
            return returnStr;
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
        #endregion
    }
}
