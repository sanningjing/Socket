using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using NetIOCP.AsyncSocketProtoclCore;

namespace NetIOCP.AsyncSocketProtocol
{
    /// <summary>
    /// 土壤传感器
    /// </summary>
    class LHSoilSensor:BaseSocketProtocol
    {
        public LHSoilSensor(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_socketFlag = "SOIL";
        }

        /// <summary>
        /// 接收到的数据进行处理:该传感器没有帧头帧尾，一帧一处理。
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override bool ProcessReceive(byte[] buffer, int offset, int count)
        {
            m_activeDT = DateTime.UtcNow;
            DynamicBufferManager receiveBuffer = m_asyncSocketUserToken.ReceiveBuffer;

           

            //写进缓冲区
            receiveBuffer.WriteBuffer(buffer, offset, count);
            bool result = true;
            int preCount = 0;//
            int packetLength = 0;//2字结或者

            if (NetByteOrder)
                packetLength = System.Net.IPAddress.NetworkToHostOrder(packetLength); //把网络字节顺序转为本地字节顺序

            while (receiveBuffer.DataCount > 0)//0
            {
                //心跳或者其他包
                packetLength = receiveBuffer.DataCount;//全部取出

                if (packetLength == 1)
                {
                    receiveBuffer.Buffer[0] = 0xFE;//
                    ///心跳包，
                    Console.WriteLine("心跳包");
                }
                if (packetLength == 38)
                {
                    byte[] forCheck = new byte[36];
                    byte[] bCheckResult = new byte[2];
                    Array.Copy(receiveBuffer.Buffer,0,forCheck,0,36);
                    bCheckResult = CheckSumCRC(forCheck);
                    //校验OK
                    if ((bCheckResult[0] == receiveBuffer.Buffer[36]) && (bCheckResult[1] == receiveBuffer.Buffer[37]))
                    {
                        bool paseResult = ProcessPacket(receiveBuffer.Buffer, 0, packetLength);
                    }
                    else
                    {
                        
                    }


                }




                for (int i = 0; i < receiveBuffer.DataCount; i++)
                {

                    if (0xA8 == receiveBuffer.Buffer[i] && 0x81 == receiveBuffer.Buffer[i + 1])
                    {
                        // Console.WriteLine("receive A881");
                        //checksum
                        byte[] forCheck = new byte[19];
                        Array.Copy(receiveBuffer.Buffer, preCount, forCheck, 0, 19);
                        byte checkSumResult = CheckSum(forCheck);
                     
                        

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

                


                if (receiveBuffer.DataCount >= packetLength + preCount) //收到的数据达到最小包长度
                {
                    result = ProcessPacket(receiveBuffer.Buffer, preCount, packetLength);//已经校验过的包
                    if (result)
                        receiveBuffer.Clear(packetLength + preCount); //从缓存中清理已经处理的数据长度
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

        public override bool  ProcessPacket(byte[] buffer, int offset, int count)
        {
            bool pResult = false;
            byte bId = buffer[0];//设备ID
            byte bFunCode = buffer[1];//功能码
            byte bDataLen = buffer[2];//从第4字节到数据结束的总字节数

            if (32!=bDataLen)
            {
                Program.Logger.Debug("数据长度出错。。。放弃本帧数据");
                Console.WriteLine("数据长度出错。。。放弃本帧数据");
                return false;
            }

            double lData1 = 0;
            Convert.ToDouble();


            return pResult;
        }
    }
}
