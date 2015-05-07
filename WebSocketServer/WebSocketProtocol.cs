using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebSocketServer.RFC6455
{
    public delegate void OnResponseReadyDelegate(WebSocketProtocol protocol, OpCode opCode, byte[] payload);

    public abstract class WebSocketProtocol
    {
        protected string _protocolName;
        public string Name
        {
            get { return _protocolName; }
        }

        public event OnResponseReadyDelegate MessageReady;

        protected WebSocketClientConnection _connection = null;
        
        //Do not allow default constructors on derived classes!
        private WebSocketProtocol() { }

        protected WebSocketProtocol(string protocolName, RFC6455.WebSocketClientConnection connection)
        {
            _protocolName = protocolName;
            _connection = connection;
        }

        //Note to self: RFC6455 allows for payload arrays with 64bits int length, .net arrays have indexes of 32 bits int...
        public abstract void ProcessIncomingPacket(RFC6455.OpCode opCode, WebSocketPacket packet);

        protected virtual void OnMessageReady(OpCode opCode, byte[] payload)
        {
            if (MessageReady != null)
                MessageReady(this, opCode, payload);
        }
    }
}
