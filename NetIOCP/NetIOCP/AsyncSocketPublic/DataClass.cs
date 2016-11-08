using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetIOCP.AsyncSocketPublic
{
    /// <summary>
    /// 数据值类
    /// </summary>
    public class DataClass
    {
    }
    /// <summary>
    /// 环卫气体值
    /// </summary>
    public class DataClassHW
    {
        public int ID;
        public double dNH3Value;
    }

    /// <summary>
    /// 土壤类传感器值
    /// </summary>
    public class DataClassLH
    {
        public int ID;
        public double dSoilTempture;
        public double dSoilMoisture;
        public double dSunshine;
        public double dAirTemperature;
        public double dAirMoisture;
        public double dSoilMoisture30;
        public double dSoilPH;
    }
}
