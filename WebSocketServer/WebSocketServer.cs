using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace WebSocketServer.RFC6455
{
    public abstract class WebSocketServer
    {
        public static UInt64 DefaultPayloadSize = 0xFFFF;

        //protected static HandshakeHeader[] OpenHeaders =
        //    new HandshakeHeader[]
        //    {
        //        new HandshakeHeader { name="HOST", mandatory=true },
        //        new HandshakeHeader { name="UPGRADE", mandatory=true },
        //        new HandshakeHeader { name="CONNECTION", mandatory=true },
        //        new HandshakeHeader { name="Sec-WebSocket-Key", mandatory=true },
        //        new HandshakeHeader { name="Sec-WebSocket-Version", mandatory=true },
        //        new HandshakeHeader { name="ORIGIN", mandatory=false },
        //        new HandshakeHeader { name="Sec-WebSocket-Protocol", mandatory=false },
        //        new HandshakeHeader { name="Sec-WebSocket-Extensions", mandatory=false },
        //        //In the future add other meaningful headers
        //    };

        //protected static HandshakeHeader[] AckHeaders =
        //    new HandshakeHeader[]
        //    {
        //    };

        //protected static string Handshake_Client_KEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        protected object SyncRoot = new object();

        protected List<WebSocketClientConnection> connections = new List<WebSocketClientConnection>();
        protected string[] _origins;
        protected string[] resource_names;

        protected Socket _serverSocket = null;

        protected WebSocketProtocolFactory _protoFactory = null;

        protected int _port;
        public int Port
        {
            get { return _port; }
        }

        protected string _scheme; //ws or wss(TODO...)
        public string Scheme
        {
            get { return _scheme; }
        }

        protected string _originalUri;

        private WebSocketServer()
        {
        }

        protected WebSocketServer(string uri, WebSocketProtocolFactory protocolFactory)
        {
            Uri _uri = new Uri(uri);
            if (_uri.IsAbsoluteUri)
            {
                _port = _uri.Port == -1 || _uri.Port == 0 ? 8888 : _uri.Port;
                _scheme = _uri.Scheme;
            }
            else
            {
                _port = 8888;
                _scheme = "ws";
            }
            _originalUri = uri;
            _protoFactory = protocolFactory;
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            var ipLocal = new IPEndPoint(IPAddress.Any, _port);
            _serverSocket.Bind(ipLocal);
            _serverSocket.Listen(100);
            _serverSocket.BeginAccept(new AsyncCallback(NewClientConnection), _serverSocket);
            //TODO: Support IPv6...
        }

        protected virtual void NewClientConnection(IAsyncResult ar)
        {
            try
            {
                Socket s = (Socket)ar.AsyncState;
                Socket c = s.EndAccept(ar);

                _serverSocket.BeginAccept(new AsyncCallback(NewClientConnection), _serverSocket);
                
                WebSocketClientConnection connection = new WebSocketClientConnection(c, _protoFactory);
                connection.OnHandshakeNoData += connection_OnNoHandshakeData;
                connection.OnSocketError += connection_OnSocketError;
                connection.OnSocketClosed += connection_OnSocketClosed;
                lock (SyncRoot)
                {
                    connections.Add(connection);
                }
                connection.Start();
            }
            catch { }
        }

        void connection_OnSocketClosed(WebSocketClientConnection connection)
        {
            try
            {
                lock (SyncRoot)
                {
                    connections.RemoveAll(item => item == connection);
                }
            }
            catch { }
        }

        void connection_OnSocketError(WebSocketClientConnection connection, int errorCode, int win32ErrorCode, SocketError socketError)
        {
            try
            {
                lock (SyncRoot)
                {
                    connections.RemoveAll(item => item == connection);
                }
            }
            catch { }
        }

        void connection_OnNoHandshakeData(WebSocketClientConnection connection)
        {
            try
            {
                lock (SyncRoot)
                {
                    connections.RemoveAll(item => item == connection);
                }
            }
            catch { }
        }

        //public virtual void AddProtocol(WebSocketProtocol protocol)
        //{
        //    protocols.Add(protocol.Name, protocol);
        //}
    }
}
