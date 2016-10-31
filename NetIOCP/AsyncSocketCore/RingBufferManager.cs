using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIOCP.AsyncSocketCore
{
    public class RingBufferManager
    {
        public byte[] Buffer { get; set; }//存放内在的数组
        public int DataCount { get; set; }//写入数据大小
        public int DataStart { get; set; }//数据起始索引
        public int DataEnd { get; set; }//数据结束索引

        public RingBufferManager(int bufferSize)
        {
            DataCount = 0;
            DataStart = 0;
            DataEnd = 0;
            Buffer = new byte[bufferSize];//缓冲区大小
        }

        public byte this[int index]
        {
            get
            {
                if (index >= DataCount) throw new Exception("Ring buffer Manager error!overflow");
                if (DataStart + index < Buffer.Length)
                {
                    return Buffer[DataStart + index];
                }
                else
                {
                    return Buffer[(DataStart+index)-Buffer.Length];
                }
            }
        }

        public int GetDataCount()//获取当前写入的字节数
        {
            return DataCount;
        }

        public int GetReserveCount()//获取剩余的字节数
        {
            return Buffer.Length - DataCount;
        }

        public void Clear()
        {
            DataCount = 0;
        }

        public void Clear(int count)//清空指定大小的数据
        {
            if (count >= DataCount)//如果需要清空的数据大于现有的数据，则全部清空
            {
                DataCount = 0;
                DataStart = 0;
                DataEnd = 0;
            }
            else
            {
                if (DataStart + count >= Buffer.Length)
                {
                    DataStart = (DataStart + count) - Buffer.Length;
                }
                else
                    DataStart += count;
                DataCount -= count;
            }
        }

        public void WriteBuffer(byte[] buffer, int offset, int count)
        {
            Console.WriteLine("存数据到缓冲区");
            Int32 reserveCount = Buffer.Length - DataCount;
            if (reserveCount >= count) //可用空间够用
            {
                if (DataEnd + count < Buffer.Length) //the end of ring buffer is enough for new data
                {
                    Array.Copy(buffer, offset, Buffer, DataEnd, count);
                    DataEnd += count;
                    DataCount += count;

                    Console.WriteLine(System.DateTime.Now.ToString()+"WriteBuffer:"+count.ToString()+"  Total:"+DataCount.ToString());
                }
                else //the end of ring buffer is not enough for new data
                {
                    System.Diagnostics.Debug.WriteLine("缓存重新开始、、、");
                    Int32 overflowIndexLength = (DataEnd + count) - Buffer.Length;//超出的长度
                    Int32 endPushIndexLength = count - overflowIndexLength; //填充在末尾的数据长度，先将缓冲区填满

                    Array.Copy(buffer, offset, Buffer, DataEnd, endPushIndexLength);

                    DataEnd = 0;//起始位置、从头开始填剩余没有写进缓冲区的部分
                    offset += endPushIndexLength;//数据源偏移字节数（前面的已经写入了缓冲区）
                    DataCount += endPushIndexLength;//填充完了，更新TOTAL DATACOUNT
                    if (overflowIndexLength != 0)
                    {
                        Array.Copy(buffer, offset, Buffer, DataEnd, overflowIndexLength);//环形
                    }

                    DataEnd += overflowIndexLength;//结束索引//更新ring buffer dateend
                    DataCount += overflowIndexLength;//update ring buffer total datacount
                }
            }
            else
            {
                //todo:缓存益处处理：这里不作处理  
            }
        }

        public void ReadBuffer(byte[] targetBytes, Int32 offset, Int32 count)
        {
            if (count > DataCount) throw new Exception("ring buffer read error!readlength is longer than data length");
            Int32 tempDataStart = DataStart;
            if (DataStart + count < Buffer.Length)//环形缓冲区直接读取
            {
                Array.Copy(Buffer, DataStart, targetBytes, offset, count);
            }
            else
            {
                Int32 overflowIndexLength=(DataStart+count)-Buffer.Length;//索引超出ring buffer长度
                Int32 endPushIndexLength = count - overflowIndexLength;//需要的数据存在the end of  ring buffer
                Array.Copy(Buffer,DataStart,targetBytes,offset,endPushIndexLength);//先将末尾的数据拷出来
                offset += endPushIndexLength;
                if (overflowIndexLength!=0)
                {
                    Array.Copy(Buffer,0,targetBytes,offset,overflowIndexLength);//再将前面的数据拷出来
                }
            }
        }

        public void WriteBuffer(byte[] buffer)
        {
            WriteBuffer(buffer,0,buffer.Length);
        }

    }
}
