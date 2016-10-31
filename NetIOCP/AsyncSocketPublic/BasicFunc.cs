using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace NetIOCP.AsyncSocketPublic
{
    /// <summary>
    /// 公共资源函数
    /// </summary>
    class BasicFunc
    {
        public static bool IsFileInUse(string fileName)
        {
            bool isUse = true;
            FileStream fs = null;

            try
            {
                fs = new FileStream(fileName,FileMode.Open,FileAccess.Read,FileShare.None);
                isUse = false;
            }
            catch (Exception ex)
            {
                isUse = true;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
            return isUse;
        }

        public static string MD5String(string value)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            byte[] data = Encoding.Default.GetBytes(value);

            byte[] md5Data = md5.ComputeHash(data);
            md5.Clear();
            string result = "";
            for (int i = 0; i < md5Data.Length; i++)
            {
                result += md5Data[i].ToString("x").PadLeft(2, '0');
            }
            return result;
        }
    }
}
