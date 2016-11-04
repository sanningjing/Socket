using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using NetIOCP.AsyncSocketPublic;

namespace NetIOCP.AsyncSocketProtoclCore
{
    /// <summary>
    /// 登录控制类
    /// </summary>
    public class BaseSocketProtocol:AsyncSocketInvokeElement
    {
        protected string m_userName;
        public string UserName { get { return m_userName; } }
        protected bool m_logined;
        public bool Logined { get { return m_logined; } }
        protected string m_socketFlag;
        public string SocketFlag { get { return m_socketFlag; } }

        public BaseSocketProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_userName = "";
            m_logined = false;
            m_socketFlag = "";
        }

        public bool DoLogin_Hello()
        {
          
                    m_outgoingDataAssembler.AddSuccess();
                   
                    m_logined = true;
                  //  Program.Logger.InfoFormat("login success",);
                    Console.WriteLine("login success");
                    //todo:lyj
             
            return DoSendResult();
        }

        //登录
        public bool DoLogin()
        {
            string userName = "";
            string password = "";
            //UserName/Password
            if (m_incomingDataParser.GetValue(ProtocolKey.UserName, ref userName) & m_incomingDataParser.GetValue(ProtocolKey.Password, ref password))
            {
                if (password.Equals(BasicFunc.MD5String("admin"), StringComparison.CurrentCultureIgnoreCase))
                {
                    m_outgoingDataAssembler.AddSuccess();
                    m_userName = userName;
                    m_logined = true;
                    Program.Logger.InfoFormat("{0} login success", userName);
                    Console.WriteLine("{0} login success", userName);
                    //todo:lyj
                }
                else
                {
                    m_outgoingDataAssembler.AddFailure(ProtocolCode.UserOrPasswordError, "");
                    Program.Logger.ErrorFormat("{0} login failure,password error", userName);

                    Console.WriteLine("{0} login failure,password error", userName);
                    //todo:lyj
                }
            }
            else
                m_outgoingDataAssembler.AddFailure(ProtocolCode.ParameterError, "");
            return DoSendResult();
        }

        public bool DoActive()
        {
            m_outgoingDataAssembler.AddSuccess();
            return DoSendResult();
        }

    }
}
