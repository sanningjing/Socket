using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using log4net;
using log4net.Repository.Hierarchy;
using Oracle.ManagedDataAccess.Client;

namespace NetIOCP.Data
{
    /// <summary>
    /// 数据库操作
    /// </summary>
    class DBOperation
    {
       // private static Logger logDBOperation = LogManager.GetCurrentClassLogger();
        private static OracleConnection mConnection = null;
        private static DBOperation mDbInstence;
        private static bool bIsOpen = false;

        public static DBOperation GetDbOperation()
        {
            if (mDbInstence == null)
            {
                mDbInstence = new DBOperation();
            }

            if (!bIsOpen)
            {
                if (OpenDB() != 0)
                {
                    Program.Logger.Info("Failed to open database!");
                    return null;
                }
            }

            return mDbInstence;
        }

        public static bool IsOpen
        {
            get { return bIsOpen; }
            set { bIsOpen = value; }
        }

        private static int OpenDB()
        {
            int ret = 0;

            string dbStr = getDBConnString();
            if (dbStr == "")
            {
                Program.Logger.Info("failed to open database!");
                ret = -1;
                return ret;
            }
            if (null == mConnection)
            {
                mConnection = new OracleConnection(dbStr);
            }
            mConnection.Open();

            IsOpen = true;

            return ret;
        }

        public int CloseDB()
        {
            int ret = 0;

            if (mConnection != null && (mConnection.State & ConnectionState.Open) != 0)
            {
                mConnection.Close();
                IsOpen = false;
                mConnection = null;
            }

            return ret;
        }

        #region 绿化系统

        public DataTable queryLHSensor()
        {
            DataTable dt = null;
            try
            {
                if (mConnection == null || IsOpen == false)
                {
                    Console.WriteLine("Please open database first.");
                    return dt;
                }

                dt = new DataTable();
                OracleCommand cmd = new OracleCommand("select TERMINAL_ID,TERMINAL_TYPE,INSTALLATION_DATE,TREE_NUM,TREE_NAME from LH_DEVICEINFO", mConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(ex.Message);
            }

            return dt;
        }

        #endregion

        #region 环卫系统
        /// <summary>
        /// 获取数据库存储的信息:HW_TOILET
        /// </summary>
        /// <returns>DataTable</returns>
        public DataTable queryHWGLSensor()
        {
            DataTable dt = null;

            if (mConnection == null || IsOpen == false)
            {
                // Console.WriteLine("Please open database first.");
                Program.Logger.Info("Please open database first.");
                return dt;
            }

            try
            {
                dt = new DataTable();
                //查询字段还是有问题
                OracleCommand cmd = new OracleCommand("select TOILET_NAME,DEVICE_ID,DEVICE_ID_GIRL from HW_TOILET", mConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(ex.Message);
            }


            return dt;
        }
        #endregion



        public DataTable querySensor()
        {
            DataTable dt = null;

            if (mConnection == null || IsOpen == false)
            {
                Console.WriteLine("Please open database first.");
                return dt;
            }

            dt = new DataTable();

            try
            {
                OracleCommand cmd = new OracleCommand("select ID,SENSORETYPE_ID,SENSORE_SERIAL,SENSORE_STATUS,THRESHOLD_VALUE,THRESHOLD_INTERVAL,SENSOR_IP from TB_SENSORE", mConnection);
                //    OracleCommand cmd = new OracleCommand("select ID,SENSORETYPE_ID,SENSORE_SERIAL,SENSORE_STATUS,THRESHOLD_VALUE,THRESHOLD_INTERVAL,SENSOR_IP from TB_SENSORE", mConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(ex.Message);
            }


            return dt;
        }


        public DataTable queryWarnValue()
        {
            DataTable dt = null;

            if (mConnection == null || IsOpen == false)
            {
                Console.WriteLine("Please open database first.");
                return dt;
            }

            dt = new DataTable();

            try
            {
                OracleCommand cmd = new OracleCommand("select ID,SENSORETYPE_ID,SENSORE_SERIAL,SENSORE_STATUS,THRESHOLD_VALUE,THRESHOLD_INTERVAL,SENSOR_IP from TB_SENSORE", mConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                adapter.Fill(dt);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(ex.Message);
            }

            return dt;
        }

        /// <summary>
        ///暂时没用
        /// </summary>
        /// <param name="cmdString"></param>
        /// <returns></returns>
        public DataTable QueryData(string cmdString)
        {
            DataTable dt = null;
            if (mConnection == null || IsOpen == false)
            {
                Console.WriteLine("Please open database first.");
                OracleCommand cmd = new OracleCommand(cmdString, mConnection);
                OracleDataAdapter adapter = new OracleDataAdapter(cmd);
                adapter.Fill(dt);
                return dt;
            }
            return dt;
        }

        /// <summary>
        /// 插入实时数据, 并且返回当前插入行的自增ID，供插入警报表时用
        /// </summary>
        /// <param name="sensorID"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public long insertRealtimeData(int sensorID, List<object> dataList, string unitStr)
        {
            if (mConnection == null || IsOpen == false)
            {
                Console.WriteLine("Please open database first.");
                return -1;
            }
            long iInsertID = -1;
            double mainData = -1;//设备主参数值，液位计为液位值，流速计为流速值。
            double flowData = -1;//通过流速计计算的流量值
            double depthData = -1;//流速计获取的水深
            double temperatureData = 0;//通过流速计获取的水温

            mainData = Convert.ToDouble(dataList[0]);
            if (dataList.Count == 4)
            {
                //流速计上报数据，包含流速值、流量值、水深值、水温值
                flowData = Convert.ToDouble(dataList[1]);
                depthData = Convert.ToDouble(dataList[2]);
                temperatureData = Convert.ToDouble(dataList[3]);
            }

            string strGetId = "select TB_TRACKVALUE$SEQ.nextval from dual";
            OracleCommand cmdGetId = new OracleCommand(strGetId, mConnection);

            OracleDataReader dataReader = cmdGetId.ExecuteReader();
            if (dataReader.Read())
            {
                iInsertID = Convert.ToInt32(dataReader[0].ToString());
            }
            else
            {
                Console.WriteLine("failed to get seq num!");

            }
            dataReader.Close();

            if (iInsertID == -1)
            {
                return iInsertID;
            }

            string dateStr = DateTime.Now.ToString();
            string str =
                "Insert into TB_TRACKVALUE(ID,SENSORE_ID,TRACK_VALUE,TRACK_UNIT,TRACK_TIME,FLOW,DEPTH,TEMPERATURE) values('" + iInsertID + "','" +
                sensorID + "','" + mainData + "','" +
                unitStr + "'," + "To_Date('" + dateStr + "', 'yyyy-mm-dd hh24:mi:ss'),'" + flowData + "','" + depthData + "','" + temperatureData + "')";

            OracleCommand cmd = new OracleCommand(str, mConnection);
            cmd.ExecuteNonQuery();

            return iInsertID;
        }

        public bool insertWarnData(long trackValueID, string warnLevel, string warnStatus, double data, int userID)
        {
            bool bRes = false;
            string dt = DateTime.Now.ToString();

            string str =
                "Insert into tb_warning(ID,TRACKVALUE_ID,WARNING_LEVEL,WARNING_STATUS,USER_ID,WARNING_CREATETIME,WARNING_UPDATETIME) values(tb_warning$seq.nextval,'" +
                trackValueID + "','" + warnLevel + "','" +
                warnStatus + "','" + userID + "',To_Date('" + dt + "', 'yyyy-mm-dd hh24:mi:ss')" + ",To_Date('" + dt + "', 'yyyy-mm-dd hh24:mi:ss'))";

            OracleCommand cmd = new OracleCommand(str, mConnection);
            if (cmd.ExecuteNonQuery() > 0)
            {
                bRes = true;
            }
            else
            {
                Console.WriteLine("Failed to insert warn data into database!");
            }
            return bRes;
        }

        private string getWarningID()
        {
            string id = "";
            DataTable dt = null;
            string queryCmd = "select TRACKVALUE_ID from tb_warning";

            if (mConnection == null || IsOpen == false)
            {
                Console.WriteLine("Please open database first.");
                return id;
            }

            OracleCommand cmd = new OracleCommand(queryCmd, mConnection);
            OracleDataAdapter adapter = new OracleDataAdapter(cmd);
            adapter.Fill(dt);


            return id;
        }

        #region SMELL sensor
        /// <summary>
        /// 气体SMELL传感器，采集数据写入数据库
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool updateSmellData(int id, Dictionary<string, double> dict)
        {
            bool res = false;
            //todo:根据数据库字段，待添加

            double NH3 = 0;
            if (dict.ContainsKey("NH3Value"))
            {
                NH3 = dict["NH3Value"];
            }
            string str = "";

            //查找数据库，是男厕所还是女厕所
            DataTable sensorDT = queryHWGLSensor();

            if (sensorDT == null)
            {
                // Console.WriteLine("Failed to get sensor info");
                Program.Logger.Info("Failed to get sensor info");
                return false;
            }

            bool orman = true;
            //将传感器ID装入
            for (int i = 0; i < sensorDT.Rows.Count; i++)
            {
                int ssID = Convert.ToInt32(sensorDT.Rows[i][1].ToString());
                int ssIDGirl = Convert.ToInt32(sensorDT.Rows[i][2].ToString());
                string dt = System.DateTime.Now.ToString();
                //一行存在两个ID，男厕与女厕
                if (id == ssID)
                {
                    //男厕所 
                    str = "update HW_TOILET set NH3=" + NH3 + ",UPDATE_TIME = sysdate" + " where DEVICE_ID=" + id;
                    orman = true;
                    break;

                }
                if (id == ssIDGirl)
                {
                    //女厕所
                    str = "update HW_TOILET set NH3_GIRL=" + NH3 + ",UPDATE_TIME = sysdate" + " where DEVICE_ID_GIRL=" + id;
                    orman = false;
                    break;
                }

//                 Sensor sensor = new Smell();
//                 Sensor sensor_girl = new Smell();
// 
//                 sensor.SensorID = ssID;
//                 sensor.MenFlag = true;
//                 sensor_girl.MenFlag = false;
//                 sensor_girl.SensorID = ssIDGirl;

            }
            ///向数据库插入。
            insertSmellData(id, dict, orman);

            if ("" != str)
            {
                OracleCommand cmd = new OracleCommand(str, mConnection);
                try
                {
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        res = true;
                    }
                    else
                        Program.Logger.Info("cmd.ExecuteNonQuery() failed!!!");
                }
                catch (Exception ex)
                {
                    // Console.WriteLine("Failed to insert data into database!");
                    Program.Logger.Debug("Failed to insert data into database!");
                    //Console.WriteLine(ex.ToString());
                }
            }
            else
            {
                //  Console.WriteLine("数据库命令字为空");
                Program.Logger.Info("数据库命令字为空");
            }

            return res;
        }

        /// <summary>
        /// 获取数据库插入命令
        /// </summary>
        /// <param name="dt1"></param>
        /// <param name="orman"></param>
        /// <param name="id"></param>
        /// <returns></returns>
        public string GetInsertCmd(DataTable dt1, bool orman, int id, double NH3Value)
        {
            int ID = Convert.ToInt32(dt1.Rows[0][0]);
            string TOILET_NUM = dt1.Rows[0][1].ToString();
            string TOILET_NAME = dt1.Rows[0][2].ToString();
            string WORK_UNIT_ID = dt1.Rows[0][3].ToString();
            string WORK_UNIT_NAME = dt1.Rows[0][4].ToString();
            string DEVICE_ADDRESS = dt1.Rows[0][5].ToString();
            //    int O2 = Convert.ToInt32(dt1.Rows[0][6]);
            //   int CH4 = Convert.ToInt32(dt1.Rows[0][7]);
            //   int CO = Convert.ToInt32(dt1.Rows[0][8]);
            int O2 = 0;
            int CH4 = 0;
            int CO = 0;

            String cmdString = "";
            DateTime save_date = System.DateTime.Now;
            if (orman)
            {
                //男厕所
                cmdString =
              "insert into HW_HISTOILET (ID, TOILET_NUMBER,TOILET_NAME,DEVICE_ID,WORK_UNIT_ID,WORK_UNIT_NAME,DEVICE_ADDRESS,O2,CH4,CO，NH3,UPDATE_TIME)" +
              " values ('" +
              ID + "' , '" + TOILET_NUM + "', '" + TOILET_NAME + "', '" + id + "', '" + WORK_UNIT_ID + "','" + WORK_UNIT_NAME + "', '" + DEVICE_ADDRESS + "','" +
              O2 + "','" + CH4 + "','" + CO + "'," + NH3Value + "'," + "To_Date('" + save_date + "','yyyy-mm-dd hh24:mi:ss'))";
            }
            else
            {
                //女厕所 the same to 男厕所
                cmdString =
              "insert into HW_HISTOILET (ID, TOILET_NUMBER,TOILET_NAME,DEVICE_ID,WORK_UNIT_ID,WORK_UNIT_NAME,DEVICE_ADDRESS,O2,CH4,CO，NH3,UPDATE_TIME)" +
              " values ('" +
              ID + "' , '" + TOILET_NUM + "', '" + TOILET_NAME + "', '" + id + "', '" + WORK_UNIT_ID + "','" + WORK_UNIT_NAME + "', '" + DEVICE_ADDRESS + "'," +
              O2 + "," + CH4 + "," + CO + "," + NH3Value + "," + "To_Date('" + save_date + "','yyyy-mm-dd hh24:mi:ss'))";
            }

            return cmdString;
        }

        /// <summary>
        /// 根据实时表，查询历史表需要的字段
        /// </summary>
        /// <param name="id"></param>
        /// <param name="orman"></param>
        /// <returns></returns>
        public DataTable QuerySmellData(int id, bool orman)
        {
            DataTable dtGet = new DataTable();
            string cmdString = "";
            if (orman)
            {
                //男厕所
                cmdString = "select ID,TOILET_NUM,TOILET_NAME,WORK_UNIT_ID,WORK_UNIT_NAME,DEVICE_ADDRESS,O2,CH4,CO from HW_TOILET WHERE DEVICE_ID=" + id;
            }
            else
            {
                //女厕所
                cmdString = "select ID,TOILET_NUMBER,TOILET_NAME,WORK_UNIT_ID,WORK_UNIT_NAME,DEVICE_ADDRESS,O2,CH4,CO from HW_TOILET WHERE DEVICE_ID_GIRL=" + id;
            }

            OracleCommand cmd1 = new OracleCommand(cmdString, mConnection);

            OracleDataAdapter adapter1 = new OracleDataAdapter(cmd1);

            try
            {
                adapter1.Fill(dtGet);
            }
            catch (Exception ex)
            {
                Program.Logger.Debug(ex.Message);
            }

            return dtGet;
        }

        /// <summary>
        /// 向指定数据库插入值
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool insertSmellData(int id, Dictionary<string, double> dict, bool orman)
        {
            bool res = false;
            double NH3 = 0;
            if (dict.ContainsKey("NH3Value"))
            {
                NH3 = dict["NH3Value"];//传感器数值
            }
            //todo:根据数据库字段，待添加
            //新数据库数据插入准备
            DataTable dt1 = new DataTable();
            dt1 = QuerySmellData(id, orman);

            string insertCmd = GetInsertCmd(dt1, orman, id, NH3);

            OracleCommand cmd = new OracleCommand(insertCmd, mConnection);

            try
            {
                if (cmd.ExecuteNonQuery() > 0)
                {
                    res = true;
                }
                else
                {
                    Program.Logger.Info("cmd.ExecuteNonQuery() > 0" + "failed!!!");
                }
            }
            catch (Exception ex)
            {
                //   Console.WriteLine("Failed to insert warn data into database!");
                //   Console.WriteLine(ex.ToString());
                Program.Logger.Debug(ex.Message);
            }

            return res;
        }

        #endregion

        #region SOIL sensor

        /// <summary>
        /// 土壤SOIL传感器，采集数据写入数据库
        /// </summary>
        /// <param name="id"></param>
        /// <param name="dict"></param>
        /// <returns></returns>
        public bool updateSoilData(int id, Dictionary<string, double> dict)
        {
            bool res = false;

            string dt = DateTime.Now.ToString();
            double soil_tempe = 0;
            double soil_moi = 0;
            double sunshine = 0;
            double air_tempe = 0;
            double air_moi = 0;

            //
            double soil_moi30 = 0;
            double soil_ph = 0;

            if (dict.ContainsKey("SOIL_TEMPERATURE"))
            {
                soil_tempe = dict["SOIL_TEMPERATURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE"))
            {
                soil_moi = dict["SOIL_MOISTURE"];
            }
            if (dict.ContainsKey("SUNSHINE"))
            {
                sunshine = dict["SUNSHINE"];
            }
            if (dict.ContainsKey("AIR_TEMPERATURE"))
            {
                air_tempe = dict["AIR_TEMPERATURE"];
            }
            if (dict.ContainsKey("AIR_MOISTURE"))
            {
                air_moi = dict["AIR_MOISTURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE30"))
            {
                soil_moi30 = dict["SOIL_MOISTURE30"];
            }

            if (dict.ContainsKey("SOIL_PH"))
            {
                soil_ph = dict["SOIL_PH"];
            }

            string str =
                "update LH_TREEADMINISTRATION set SOIL_TEMPERATURE=" + soil_tempe + ",SOIL_MOISTURE=" + soil_moi + ",SUNSHINE=" + sunshine +
                ",AIR_TEMPERATURE=" + air_tempe + ",AIR_MOISTURE=" + air_moi + ",SOIL_MOISTURE30=" + soil_moi30 + ",SOIL_PH=" + soil_ph + ",UPDATE_TIME=sysdate" + " where ID=" + id;

            //             string str =
            //                "update LH_TREEADMINISTRATION set SOIL_TEMPERATURE=" + soil_tempe + ",SOIL_MOISTURE=" + 0 + ",SUNSHINE=" + 0 +
            //                ",AIR_TEMPERATURE=" + 0 + ",AIR_MOISTURE=" + 0 +  " where ID=" + id;

            OracleCommand cmd = new OracleCommand(str, mConnection);

            try
            {
                if (cmd.ExecuteNonQuery() > 0)
                {
                    res = true;
                }
                else
                    Program.Logger.Info("cmd.ExecuteNonQuery() failed!!!");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine("Failed to insert warn data into database!");
                Console.WriteLine(ex.ToString());
            }

            res = insertSoilData(id, dict);//向数据库插入数据

            return res;
        }

        /// <summary>
        /// 向土壤数据库插入数据
        /// </summary>
        /// <returns></returns>
        public bool insertSoilData(int id, Dictionary<string, double> dict)
        {
            bool res = false;

            double soil_tempe = 0;
            double soil_moi = 0;
            double sunshine = 0;
            double air_tempe = 0;
            double air_moi = 0;
            double soil_moi30 = 0;
            double soil_ph = 0;
            if (dict.ContainsKey("SOIL_TEMPERATURE"))
            {
                soil_tempe = dict["SOIL_TEMPERATURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE"))
            {
                soil_moi = dict["SOIL_MOISTURE"];
            }
            if (dict.ContainsKey("SUNSHINE"))
            {
                sunshine = dict["SUNSHINE"];
            }
            if (dict.ContainsKey("AIR_TEMPERATURE"))
            {
                air_tempe = dict["AIR_TEMPERATURE"];
            }
            if (dict.ContainsKey("AIR_MOISTURE"))
            {
                air_moi = dict["AIR_MOISTURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE30"))
            {
                soil_moi30 = dict["SOIL_MOISTURE30"];
            }

            if (dict.ContainsKey("SOIL_PH"))
            {
                soil_ph = dict["SOIL_PH"];
            }

            //新数据库数据插入准备
            ///以下为插入数据程序：
            DataTable dt1 = new DataTable();
            string cmdString =
                "select ID,NUM from LH_TREEADMINISTRATION ";

            try
            {
                OracleCommand cmd1 = new OracleCommand(cmdString, mConnection);

                OracleDataAdapter adapter1 = new OracleDataAdapter(cmd1);

                adapter1.Fill(dt1);

                string num = "";

                for (int i = 0; i < dt1.Rows.Count; i++)
                {
                    string ssType = dt1.Rows[i][0].ToString();

                    if (ssType.Equals("1"))//
                    {
                        num = dt1.Rows[i][1].ToString();
                        break;
                    }
                }
                int test = 0;

                double treenum = Convert.ToDouble(num);

                DateTime save_date = System.DateTime.Now;
                double id0 = 1;
                //  num = "000015";
                ///
                string str1 =
                   "insert into LH_TREEDEVICEHIS (ID, TREE_NUMBER,DEVICE_NUMBER,SUNSHINE,AIR_TEMPERATURE,AIR_MOISTURE,SOIL_TEMPERATURE,SOIL_MOISTURE,SOIL_MOISTURE30,SOIL_PH,SAVE_DATE)" +
                   " values ('" +
                   id0 + "' , '" + num + "', '" + id + "', '" + sunshine + "', '" + air_tempe + "','" + air_moi + "', '" + soil_tempe + "','" +
                   soil_moi + "','" + soil_moi30 + "','" + soil_ph + "'," + "To_Date('" + save_date + "','yyyy-mm-dd hh24:mi:ss'))";

                OracleCommand cmd2 = new OracleCommand(str1, mConnection);
                if (cmd2.ExecuteNonQuery() > 0)
                {
                    res = true;
                }
                else
                {
                    Program.Logger.Info("Failed to insert warn data into database!");
                    //  Console.WriteLine("Failed to insert warn data into database!");
                }
            }
            catch (Exception ex)
            {
                Program.Logger.Error(ex.Message);
            }




            return res;
        }

        /// <summary>
        /// 向TREEDEVICEHIS数据表中插入新数据///暂时不用
        /// </summary>
        /// <returns>bool</returns>
        public bool InsertIntoOracle(string tablename, string cmdSting, Dictionary<string, double> dict)
        {
            bool res = false;

            double soil_tempe = 0;
            double soil_moi = 0;
            double sunshine = 0;
            double air_tempe = 0;
            double air_moi = 0;

            //
            double soil_moi30 = 0;
            double soil_ph = 0;

            if (dict.ContainsKey("SOIL_TEMPERATURE"))
            {
                soil_tempe = dict["SOIL_TEMPERATURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE"))
            {
                soil_moi = dict["SOIL_MOISTURE"];
            }
            if (dict.ContainsKey("SUNSHINE"))
            {
                sunshine = dict["SUNSHINE"];
            }
            if (dict.ContainsKey("AIR_TEMPERATURE"))
            {
                air_tempe = dict["AIR_TEMPERATURE"];
            }
            if (dict.ContainsKey("AIR_MOISTURE"))
            {
                air_moi = dict["AIR_MOISTURE"];
            }
            if (dict.ContainsKey("SOIL_MOISTURE30"))
            {
                soil_moi30 = dict["SOIL_MOISTURE30"];
            }

            if (dict.ContainsKey("SOIL_PH"))
            {
                soil_ph = dict["SOIL_PH"];
            }

            //             string str =
            //                 "update LH_TREEADMINISTRATION set SOIL_TEMPERATURE=" + soil_tempe + ",SOIL_MOISTURE=" + soil_moi + ",SUNSHINE=" + sunshine +
            //                 ",AIR_TEMPERATURE=" + air_tempe + ",AIR_MOISTURE=" + air_moi + ",SOIL_MOISTURE30=" + soil_moi30 + ",SOIL_PH=" + soil_ph + " where ID=" + id;

            //    OracleCommand cmd = new OracleCommand(str, mConnection);
            //             if (cmd.ExecuteNonQuery() > 0)
            //             {
            //                 res = true;
            //             }
            //             else
            //             {
            //                 Console.WriteLine("Failed to insert warn data into database!");
            //             }

            return res;
        }

        #endregion







        /// <summary>
        /// 从配置文件中取得连接信息生成数据库连接字符串,选择是哪种数据库链接
        /// </summary>
        /// <returns>如果成功返回连接字符串，失败返回空字符串</returns>
        public static string getDBConnString()
        {
            string strDataBaseConnect = "";                                                   //连接字符串
            string strDBConfig = "";
            strDBConfig = Application.StartupPath + @"\..\..\ini\DBConnInfo.xml";
            Dictionary<string, string> dicDBConnInfo;                                           //连接信息字典
            string host = null;                                                                 //主机地址
            string port = null;                                                                 //端口
            string serverName = null;                                                           //服务器名称
            string userID = null;                                                               //用户ID
            string password = null;                                                             //密码
            StringBuilder connstring = new StringBuilder();

            dicDBConnInfo = GetDBConnInfo("DBORACLE", strDBConfig);                                                //从数据库配置文件"..\config\DBConfig\DBConnInfo.xml"文件中的
                                                                                                                   //"DBCONN"节点的子节点"DB"取得连接信息生成字典，如果文件不存在返回空值
            if (dicDBConnInfo != null)
            {
                foreach (KeyValuePair<string, string> kvp in dicDBConnInfo)
                {
                    switch (kvp.Key)
                    {
                        case "HOST":
                            host = kvp.Value;
                            break;
                        case "PORT":
                            port = kvp.Value;
                            break;
                        case "SERVICE_NAME":
                            serverName = kvp.Value;
                            break;
                        case "USER_ID":
                            userID = kvp.Value;
                            break;
                        case "PASSWORD":
                            password = kvp.Value;
                            break;
                    }
                }

                strDataBaseConnect = String.Format("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT = {1})))(CONNECT_DATA=(SERVICE_NAME={2})));User ID={3};Password={4}", host, port, serverName, userID, password);
            }
            return strDataBaseConnect;
        }

        /// <summary>
        /// 从配置文件中取得连接信息，生成连接信息数据字典
        /// </summary>
        /// <param name="strDBName">存储连接字符串的XML子节点的名称</param>
        /// <returns>如果成功生成数据字典，如果失败返回NULL</returns>
        public static Dictionary<string, string> GetDBConnInfo(string strDBName, String strDBConfig)
        {
            XmlTextReader xmlReader = null;
            XmlDocument xmlDocument = null;
            XmlNode xmlNode = null;
            Dictionary<string, string> dicConnInfo;
            String sxmlArrName;

            xmlDocument = new XmlDocument();

            try
            {
                if (System.IO.File.Exists(strDBConfig) == false)
                {
                    MessageBox.Show(strDBConfig + "Does not exist.");
                    return null;
                }
                else
                {
                    xmlReader = new XmlTextReader(strDBConfig);
                    dicConnInfo = new Dictionary<string, string>();

                    while (xmlReader.Read())
                    {
                        if (xmlReader.IsStartElement())
                        {
                            xmlDocument.Load(xmlReader);
                            XmlNodeList nodeList = xmlDocument.SelectSingleNode("DBCONN").ChildNodes;
                            foreach (XmlNode node in nodeList)
                            {
                                if (node.Name == strDBName)
                                    xmlNode = node;
                            }
                            if (xmlNode != null && xmlNode.Attributes.Count > 0)
                            {
                                foreach (XmlAttribute xmlAttr in xmlNode.Attributes)
                                {
                                    sxmlArrName = xmlAttr.Name;
                                    if (dicConnInfo.ContainsKey(sxmlArrName) == false)
                                    {
                                        dicConnInfo.Add(sxmlArrName, xmlNode.Attributes[sxmlArrName].Value.ToString());
                                    }
                                }
                            }
                        }
                    }
                }
                xmlReader.Close();

                return dicConnInfo;
            }
            catch (Exception e)
            {
                return null;

            }
        }

    }
}
