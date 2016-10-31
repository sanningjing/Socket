using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetIOCP.AsyncSocketCore;
using NetIOCP.AsyncSocketProtoclCore;

namespace NetIOCP.AsyncSocketProtocol
{
    class LHSoilSocketProtocol:BaseSocketProtocol
    {
        public LHSoilSocketProtocol(AsyncSocketServer asyncSocketServer, AsyncSocketUserToken asyncSocketUserToken)
            : base(asyncSocketServer, asyncSocketUserToken)
        {
            m_socketFlag = "SOIL";
        }
         
    }
}
