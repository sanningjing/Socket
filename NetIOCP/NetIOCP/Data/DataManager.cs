using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.Collections;
using System.Threading;
using NetIOCP.AsyncSocketPublic;

namespace NetIOCP.Data
{
    class DataManager
    {
        public static LinkedList<DataClassHW> listHW = new LinkedList<DataClassHW>();
        public static LinkedList<DataClassLH> listLH = new LinkedList<DataClassLH>();

        private Thread tDataProcessListHW = null;
        private Thread tDataProcessListLH = null;

        private Int64 count = 0;
        ///开启线程，写数据库。每类传感器一个线程
        /// 
        public DataManager()
        {
          //  tDataProcessListHW = new Thread(new ThreadStart(DataProcessListHW));
            tDataProcessListLH = new Thread(new ThreadStart(DataProcessListLH));

         //   tDataProcessListHW.Start();
            tDataProcessListLH.Start();
        }

        /// <summary>
        /// HW：环卫气体：将数据存入数据库
        /// </summary>
        private void DataProcessListHW()
        {
            while (true)
            {
                if (0==listHW.Count)//没有数据
                {
                    Thread.Sleep(100);
                    Console.WriteLine("+++++++++++none");
                    continue;
                }

                DataClassHW dchw = (DataClassHW)listHW.Last();

                Console.WriteLine(dchw.dNH3Value);

            }

        }

        /// <summary>
        /// LH：土壤数据存入数据库
        /// </summary>
        private void DataProcessListLH()
        {
            while (true)
            {
                if (0 == listLH.Count)
                {
                    Thread.Sleep(100);
                 //   Console.WriteLine("+++++++++++none");
                    continue;
                }

                DataClassLH dclh = (DataClassLH) listLH.Last();
                string strOut = "CO2:" + 0 + " 环境温度：" + dclh.dAirTemperature + " 环境湿度：" + dclh.dAirMoisture + " 光照：" + dclh.dSunshine + " 土壤PH：" + dclh.dSoilPH + " 土壤温度：" + dclh.dSoilTempture + " 土壤湿度：" + dclh.dSoilMoisture + " 土壤湿度30：" + dclh.dSoilMoisture30;
                Console.WriteLine(System.DateTime.Now+":"+ strOut);

                Dictionary<string, double> dct = new Dictionary<string, double>();
                dct.Add("SOIL_TEMPERATURE", dclh.dSoilTempture);
                dct.Add("SOIL_MOISTURE", dclh.dSoilMoisture);
                dct.Add("SUNSHINE", dclh.dSunshine);
                dct.Add("AIR_TEMPERATURE", dclh.dAirTemperature);
                dct.Add("AIR_MOISTURE", dclh.dAirMoisture);
                dct.Add("SOIL_MOISTURE30", dclh.dSoilMoisture30);
                dct.Add("SOIL_PH", dclh.dSoilPH);

                ///将数据插入数据库
                /// 
                DBOperation db = DBOperation.GetDbOperation();
                if (db != null)
                {
                    count++;
                    if (count > 20000)
                    {
                        count = 0;
                    }
                    db.updateSoilData(dclh.ID, dct);
                    Console.WriteLine(System.DateTime.Now+ ":insert to soil database ok!!:"+count);
                }
                else
                {
                    Program.Logger.Debug("database open failed!!!");
                }
                listLH.RemoveLast();
            }
        }
    }
}
