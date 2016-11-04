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
                    if ((bCheckResult[0] == receiveBuffer.Buffer[37]) && (bCheckResult[1] == receiveBuffer.Buffer[36]))
                    {
                        bool paseResult = ProcessPacket(receiveBuffer.Buffer, 0, packetLength);
                    }
                    else
                    {
                        Program.Logger.Debug("数据帧尾有问题，抛弃一帧");
                        Console.WriteLine(System.DateTime.Now+"数据帧尾有问题，抛弃一帧");
                    }


                }

                //一帧数据全部清除
                receiveBuffer.Clear(packetLength);
            }
            return true;
       }

        public override bool  ProcessPacket(byte[] buffer, int offset, int count)
        {
            bool pResult = false;
            byte bId = buffer[0];//设备ID
            byte bFunCode = buffer[1];//功能码
            int  bDataLen =(buffer[2]<<8)|buffer[3] ;//从第4字节到数据结束的总字节数

            if (32!=bDataLen)
            {
                Program.Logger.Debug("数据长度出错。。。放弃本帧数据");
                Console.WriteLine("数据长度出错。。。放弃本帧数据");
                return false;
            }

            //字节高低位。高前低后
            double lData1 =((buffer[4]<<8)|buffer[5])*0.1;//CO2:数据库无此字段
            double lData2= ((buffer[6]<<8)|buffer[7])*0.1;//AIR_TEMPERATURE
            double lData3 = ((buffer[8] << 8) | buffer[9]) * 0.1;//AIR_MOISTURE
            double lData4 = ((buffer[10] << 8) | buffer[11]) * 10;//SUNSHINE
            double lData5 = ((buffer[12] << 8) | buffer[13]) * 0.01;//SOIL_PH
            double lData6 = ((buffer[14] << 8) | buffer[15]) * 0.1;//SOIL_TEMPERATURE
            double lData7 = ((buffer[16] << 8) | buffer[17]) * 0.1;//SOIL_MOISTURE//20cm
            double lData8 = ((buffer[18] << 8) | buffer[19]) * 0.1;//SOIL_MOISTURE30

            string strOut = "CO2:" + lData1 + " 环境温度：" + lData2 + " 环境湿度：" + lData3 + " 光照：" + lData4 + " 土壤PH：" + lData5 + " 土壤温度：" + lData6 + " 土壤湿度：" + lData7 + " 土壤湿度30：" + lData8;
            Console.WriteLine(strOut);

            //Convert.ToDouble();

            return pResult;
        }
    }
}
