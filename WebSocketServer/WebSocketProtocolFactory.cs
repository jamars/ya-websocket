using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer.RFC6455
{
    public abstract class WebSocketProtocolFactory
    {
        protected List<string> protocols = new List<string>();
        public List<string> AvailableProtocols
        {
            get { return protocols; }
        }

        public abstract WebSocketProtocol Create(string protocol, WebSocketClientConnection connection);
    }
}
