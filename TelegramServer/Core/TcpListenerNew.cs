using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TelegramServer.Core
{
    public class TcpListenerNew: TcpListener
    {
        public TcpListenerNew(IPEndPoint localEP) : base(localEP)
        {

        }

        public TcpListenerNew(IPAddress localaddr, int port) : base(localaddr, port)
        {

        }

        public new bool Active
        {
            get { return base.Active; }
        }
    }
}
