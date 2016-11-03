using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using NetIOCP.AsyncSocketProtoclCore;
using log4net;
using log4net.Repository.Hierarchy;

namespace NetIOCP.AsyncSocketProtocol
{
    public class HWSmellSensor: BaseSocketProtocol
    {
        public HWSmellSensor(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_socketFlag = "Smell";
        }

       
       
        public override void Close()
        {
            base.Close();
        }

        public ControlSocketCommand StrToCommand(string command)
        {
            if (command.Equals(ProtocolKey.Active, StringComparison.CurrentCultureIgnoreCase))
                return ControlSocketCommand.Active;
            else if (command.Equals(ProtocolKey.Login, StringComparison.CurrentCultureIgnoreCase))
                return ControlSocketCommand.Login;
            else if (command.Equals(ProtocolKey.GetClients, StringComparison.CurrentCultureIgnoreCase))
                return ControlSocketCommand.GetClients;
            else if (command.Equals(ProtocolKey.HeartBeat, StringComparison.CurrentCultureIgnoreCase))
                return ControlSocketCommand.HeartBeat;
            else
                return ControlSocketCommand.None;
        }

        public bool CheckLogined(ControlSocketCommand command)
        {
            if ((command == ControlSocketCommand.Login) | (command == ControlSocketCommand.Active))
                return true;
            else
                return m_logined;
        }

        /// <summary>
        /// 继承自上一级
        /// //接收异步事件返回的数据，用于对数据进行缓存和分包
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>//返回失败，断开连接

        public override bool ProcessReceive(byte[] buffer, int offset, int count) 
        {
            m_activeDT = DateTime.UtcNow;
            DynamicBufferManager receiveBuffer = m_asyncSocketUserToken.ReceiveBuffer;

            //写进缓冲区
            receiveBuffer.WriteBuffer(buffer, offset, count);
            bool result = true;
            int preCount = 0;//
            int packetLength = 0;//20心跳包;42数据包
            while (receiveBuffer.DataCount > 0)//0
            {
                //帧头查找A881
                for (int i = 0; i < receiveBuffer.DataCount; i++)
                {
                   
                    if (0xA8 == receiveBuffer.Buffer[i] && 0x81 == receiveBuffer.Buffer[i+1])
                    {
                       // Console.WriteLine("receive A881");
                        //checksum
                        byte[] forCheck = new byte[19];
                        Array.Copy(receiveBuffer.Buffer, preCount, forCheck, 0, 19);
                        byte checkSumResult = CheckSum(forCheck);
                        if (receiveBuffer.Buffer[i + 19] == checkSumResult) //校验正确
                        {
                            if (receiveBuffer.Buffer[i + 17] == 0x02)//heartbeat function code
                            {
                                //只是心跳包
                                packetLength = 20;

                            }
                            else if (0x01 == receiveBuffer.Buffer[i + 17]) //DTU->server function code
                            {
                                //是数据包
                                packetLength = 42;
                            }
                            else//相当于校验不对
                            {
                                i += 20;//校验不对，20个字节。///适用于非大量数据，否则会溢出。
                                preCount += 20;
                                continue;
                            }
                            break;
                        }
                        else
                        {
                            i += 20;//校验不对，20个字节。
                            preCount += 20;
                            continue;
                        }
 
                    }
                    preCount++;
                }

                ///just for test;
     
                if (packetLength == 0)
                {
                    Console.WriteLine("packetLength==0");
                    receiveBuffer.Clear(receiveBuffer.DataCount);//清空缓存
                    return false;//没有正确的数据幀头
                }
               
                if (NetByteOrder)
                    packetLength = System.Net.IPAddress.NetworkToHostOrder(packetLength); //把网络字节顺序转为本地字节顺序


                if (receiveBuffer.DataCount  >= packetLength + preCount) //收到的数据达到最小包长度
                {
                    result = ProcessPacket(receiveBuffer.Buffer, preCount, packetLength);//已经校验过的包
                    if (result)
                        receiveBuffer.Clear(packetLength+preCount); //从缓存中清理已经处理的数据长度
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

        /// <summary>
        /// //处理心跳或者数据
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override bool ProcessPacket(byte[] buffer, int offset, int count) 
        {
            if (count < sizeof(int))
                return false;

            bool rReturn = false;

            if (count==20)
            {
                //心跳包处理
                rReturn=HeartBeatAnalyse(buffer);
                Console.WriteLine("心跳包输出.");
            }
            else if (count==42)
            {
                rReturn = DataPackageAnalyse(buffer);
                Console.WriteLine("数据包");
            }
            else
            {
                Console.WriteLine("需要处理的数据长度有问题，不处理。。。。。datalength="+count.ToString());
            }

            return rReturn;
        }

        public override bool HeartBeatAnalyse(byte[] byteArray)
        {
            ///组帧，心跳响应包。
            byteArray[19] = Convert.ToByte(byteArray[19] + 0x01);
            byteArray[17] = 0x03;

            string socketText = byte2HexStr(byteArray,20);

            // m_outgoingDataAssembler.AddValue(ProtocolKey.Item, socketText);
            m_outgoingDataAssembler.AddValue("", socketText);

            return DoSendResultSmell();

           // return base.HeartBeatAnalyse(byteArray);
        }

        public override bool DataPackageAnalyse(byte[] byteArray)
        {
          //  return base.DataPackageAnalyse(byteArray);
            if (byteArray.Length<=0)
            {
                return false;
            }
            int dataLength = byteArray.Length;

            if ((0x10 == byteArray[dataLength - 1]) && (0x03 == byteArray[dataLength - 2]))
            {
                //数据结尾正确
                //取传感器数据
                byte[] bValue = null;
                byte[] bPoint = null;
                byte[] bUnit = null;
                Array.Copy(byteArray,30,bValue,0,5);
                Array.Copy(byteArray,35,bPoint,0,1);
                Array.Copy();
            }
            else
            {
                string messageOut = "数据帧尾出错：" + byteArray[dataLength-2].ToString()+byteArray[dataLength-1].ToString();
                Program.Logger.Error(messageOut);
                return false;
            }



            return true;
        }

        public override bool DoSendResultSmell()
        {
            string commandText = m_outgoingDataAssembler.GetProtocolText();
            byte[] bufferUTF8 = str2HexByte(commandText);
            int totalLength = bufferUTF8.Length; //获取总大小
            AsyncSendBufferManager asyncSendBufferManager = m_asyncSocketUserToken.SendBuffer;
            asyncSendBufferManager.StartPacket();
           // asyncSendBufferManager.DynamicBufferManager.WriteInt(totalLength, false); //写入总大小
            asyncSendBufferManager.DynamicBufferManager.WriteBuffer(bufferUTF8,0,totalLength);
           
            asyncSendBufferManager.EndPacket();

            bool result = true;
            if (!m_asyncSocketUserToken.SendAsyncState)
            {
                int packetOffset = 0;
                int packetCount = 0;
                if (asyncSendBufferManager.GetFirstPacket(ref packetOffset, ref packetCount))
                {
                    m_asyncSocketUserToken.SendAsyncState = true;
                    m_asyncSocketUserToken.SendAsyncState = true;
                    result = m_asyncSocketServer.SendAsyncEvent(m_asyncSocketUserToken.ConnectSocket, m_asyncSocketUserToken.SendEventArgs,
                        asyncSendBufferManager.DynamicBufferManager.Buffer, packetOffset, packetCount);
                }
            }
            return result;
        }


        //需要响应
        public override bool ProcessCommand(byte[] buffer, int offset, int count) //处理分完包的数据，子类从这个方法继承
        {
            ControlSocketCommand command = StrToCommand(m_incomingDataParser.Command);
            m_outgoingDataAssembler.Clear();
            m_outgoingDataAssembler.AddResponse();
            m_outgoingDataAssembler.AddCommand(m_incomingDataParser.Command);
//             if (!CheckLogined(command)) //检测登录
//             {
//                 m_outgoingDataAssembler.AddFailure(ProtocolCode.UserHasLogined, "");
//                 return DoSendResult();
//             }
            if (command == ControlSocketCommand.Login)
                return DoLogin();
            else if (command == ControlSocketCommand.Active)
                return DoActive();
            else if (command == ControlSocketCommand.GetClients)
                return DoGetClients();
            else if(command == ControlSocketCommand.HeartBeat)//心跳包响应
              return DoHeartBeat();
            else if (command == ControlSocketCommand.DataReceive)//数接收
                return true;
            else
            {
                Program.Logger.Error("Unknow command: " + m_incomingDataParser.Command);

                Console.WriteLine("Unknow command: " + m_incomingDataParser.Command);
                return false;
            }
        }

        public bool DoHeartBeat()
        {
            AsyncSocketUserToken[] userTokenArray = null;
            m_asyncSocketServer.AsyncSocketUserTokenList.CopyList(ref userTokenArray);
            m_outgoingDataAssembler.AddSuccess();
            string socketText = "";
            for (int i = 0; i < userTokenArray.Length; i++)
            {
                try
                {
                    socketText = "A88130303030303033383437303036333403001F";

                    m_outgoingDataAssembler.AddValue(ProtocolKey.Item, socketText);
                }
                catch (Exception E)
                {
                    //                     Program.Logger.ErrorFormat("Get client error, message: {0}", E.Message);
                    //                     Program.Logger.Error(E.StackTrace);

                    Console.WriteLine("Get client error, message: {0}", E.Message);
                }
            }
            return DoSendResultSmell();
          
        }

        public bool DoGetClients()
        {
            AsyncSocketUserToken[] userTokenArray = null;
            m_asyncSocketServer.AsyncSocketUserTokenList.CopyList(ref userTokenArray);
            m_outgoingDataAssembler.AddSuccess();
            string socketText = "";
            for (int i = 0; i < userTokenArray.Length; i++)
            {
                try
                {
                    socketText = userTokenArray[i].ConnectSocket.LocalEndPoint.ToString() + "\t"
                        + userTokenArray[i].ConnectSocket.RemoteEndPoint.ToString() + "\t"
                        + (userTokenArray[i].AsyncSocketInvokeElement as BaseSocketProtocol).SocketFlag + "\t"
                        + (userTokenArray[i].AsyncSocketInvokeElement as BaseSocketProtocol).UserName + "\t"
                        + userTokenArray[i].AsyncSocketInvokeElement.ConnectDT.ToString() + "\t"
                        + userTokenArray[i].AsyncSocketInvokeElement.ActiveDT.ToString();
                    m_outgoingDataAssembler.AddValue(ProtocolKey.Item, socketText);
                }
                catch (Exception E)
                {
                    //                     Program.Logger.ErrorFormat("Get client error, message: {0}", E.Message);
                    //                     Program.Logger.Error(E.StackTrace);

                    Console.WriteLine("Get client error, message: {0}", E.Message);
                }
            }
            return DoSendResultSmell();
        }
    }
}
