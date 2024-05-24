using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TelegramLite.Core
{
    public class TcpClientNew : TcpClient
    {
        public TcpClientNew(IPEndPoint localEP) : base(localEP)
        {

        }
        public TcpClientNew()
        {

        }

        public new bool Active
        {
            get { return base.Active; }
        }
    }
}
