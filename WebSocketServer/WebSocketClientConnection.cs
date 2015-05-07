using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace WebSocketServer.RFC6455
{
    public delegate void OnHandshakeNoData(WebSocketClientConnection connection);
    public delegate void OnSocketError(WebSocketClientConnection connection, int errorCode, int win32ErrorCode, SocketError socketError);
    public delegate void OnHandshakeError(WebSocketClientConnection connection, string error);
    public delegate void OnHandshakeResponded(WebSocketClientConnection connection, string response);
    public delegate void OnSocketClosed(WebSocketClientConnection connection);

    //TODO: Add a "time out" parameter to allow closing the client connection (and clean the socket and connection entry) when no data arrives for a specified amout of time (the "time out").
    //TODO: Use setting 'Origin', to allow the server to choose not to connect with "unstrusted" clients.
    //TODO: Allow websocket protocol extensions (future...)
    //TODO: Add support for TLS
    //TODO: ...
    public class WebSocketClientConnection
    {
        protected struct HandshakeHeader
        {
            public string name;
            public bool mandatory;
        }

        protected class SocketState
        {
            public int bufferSize = 4096; //4 * 1024
            public byte[] buffer;
            public Socket socket;
            public int read;
            public List<byte> bufferRead = new List<byte>();

            public SocketState(Socket socket)
            {
                this.socket = socket;
                buffer = new byte[bufferSize];
                read = 0;
            }
        }

        // TODO: avoid wasting memory... stop allocating 4096 buffers for each read...
        //protected class SocketState
        //{
        //    public int bufferSize = 4096; //4 * 1024
        //    public byte[] buffer;
        //    public IList<ArraySegment<byte>> buffers = new List<ArraySegment<byte>>();
        //    public Socket socket;
        //    public int read;
        //    public SocketError SocketError;
        //    public List<byte> bufferRead = new List<byte>();

        //    public SocketState(Socket socket)
        //    {
        //        this.socket = socket;
        //        buffer = new byte[bufferSize];
        //        read = 0;
        //    }
        //}

        protected static UInt64 DefaultMaxPayloadSize = 0xFFFF;

        protected static HandshakeHeader[] OpenHeaders =
            new HandshakeHeader[]
            {
                new HandshakeHeader { name="HOST", mandatory=true },
                new HandshakeHeader { name="UPGRADE", mandatory=true },
                new HandshakeHeader { name="CONNECTION", mandatory=true },
                new HandshakeHeader { name="Sec-WebSocket-Key", mandatory=true },
                new HandshakeHeader { name="Sec-WebSocket-Version", mandatory=true },
                new HandshakeHeader { name="ORIGIN", mandatory=false },
                new HandshakeHeader { name="Sec-WebSocket-Protocol", mandatory=false },
                new HandshakeHeader { name="Sec-WebSocket-Extensions", mandatory=false },
                //In the future add other meaningful headers
            };

        protected static HandshakeHeader[] AckHeaders =
            new HandshakeHeader[]
            {
            };

        protected static string Client_KEY = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        private static string Test_Open =
          "GET /chat HTTP/1.1" + "\r\n" +
          "Host: server.example.com" + "\r\n" +
          "Upgrade: websocket" + "\r\n" +
          "Connection: Upgrade" + "\r\n" +
          "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" + "\r\n" +
          @"Origin: http://example.com" + "\r\n" +
          "Sec-WebSocket-Protocol: chat, superchat" + "\r\n" +
          "Sec-WebSocket-Version: 13" + "\r\n" +
          "Sec-WebSocket-Version: 12,25" + "\r\n";
        //Regex exp = new Regex(@"^Primary\:\s*(?<primary>[.()a-z0-9]*).*$\s+^Secondary\:\s*(?<secondary>[.()a-z0-9]*).*$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);
        //private static string regexp_str = @"^{0}\:\s*(?<value>[.()a-z0-9]*).*$\s";
        //private static string regexp_str = @"^{0}\:\s*(?<value>[.\/\/\\u0000-\\u007F]*).*$\s";

        protected static string Regexp_HandshakeHeaders = @"^{0}\:\s*(?<value>[\s.,+\/\\u0000-\\u00FF]*).*$";

        protected static Dictionary<string, System.Text.RegularExpressions.Regex> parsers = 
            new Dictionary<string, System.Text.RegularExpressions.Regex>();

        //  
        //  Open Handshake example from RFC6455
        //  ===================================
        //
        //  GET /chat HTTP/1.1
        //  Host: server.example.com
        //  Upgrade: websocket
        //  Connection: Upgrade
        //  Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==
        //  Origin: http://example.com
        //  Sec-WebSocket-Protocol: chat, superchat
        //  Sec-WebSocket-Version: 13
        //
        //  Close Handshake example from RFC6455
        //  ====================================
        //
        //  HTTP/1.1 101 Switching Protocols
        //  Upgrade: websocket
        //  Connection: Upgrade
        //  Sec-WebSocket-Accept: s3pPLMBiTxaQ9kYGzzhZRbK+xOo=

        static WebSocketClientConnection()
        {
            foreach (var header in OpenHeaders)
                parsers[header.name] = 
                    new System.Text.RegularExpressions.Regex(
                        string.Format(Regexp_HandshakeHeaders, header.name), 
                        System.Text.RegularExpressions.RegexOptions.Compiled | 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | 
                        System.Text.RegularExpressions.RegexOptions.Singleline);
        }

        public static void TestSHA1()
        {
            System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
            string val = "dGhlIHNhbXBsZSBub25jZQ==" + Client_KEY;
            sha1.ComputeHash(Encoding.ASCII.GetBytes(val));

            Console.WriteLine("HASH => {0}", Convert.ToBase64String(sha1.Hash));

            Console.ReadLine();
        }

        public static Dictionary<string,string> TestOpen()
        {
            Dictionary<string, string> Headers=new Dictionary<string,string>();

            foreach (var header in OpenHeaders)
            {
                System.Text.RegularExpressions.Regex parser = parsers[header.name];
                bool is_match = parser.IsMatch(Test_Open);
                if (is_match)
                {
                    foreach (System.Text.RegularExpressions.Match match in parser.Matches(Test_Open))
                        if (match.Groups["value"].Success)
                            Headers[header.name] = match.Groups["value"].Value;
                }
            }
            //
            return Headers;
        }

        public event OnHandshakeNoData OnHandshakeNoData;
        public event OnSocketError OnSocketError;
        public event OnHandshakeError OnHandshakeError;

        public event OnSocketClosed OnSocketClosed;

        protected UInt64 _payloadSize;
        protected int _handshakeBufferSize = 1024;
        protected Socket _socket = null;
        protected byte[] handshakeBuffer;
        protected WebSocketProtocolFactory _protoFactory;
        protected WebSocketProtocol _protocol = null;

        protected Dictionary<string, string> _handshakeValues = null;

        private WebSocketClientConnection() { }

        public WebSocketClientConnection(Socket socket, WebSocketProtocolFactory protoFactory) :
            this(socket, protoFactory, DefaultMaxPayloadSize)
        {
        }

        public WebSocketClientConnection(Socket socket, WebSocketProtocolFactory protoFactory, UInt64 payloadSize)
        {
            _payloadSize = payloadSize;
            _socket = socket;
            _protoFactory = protoFactory;

            handshakeBuffer = new byte[_handshakeBufferSize];
        }

        /// <exception cref="System.ArgumentNullException"></exception>
        /// <exception cref="System.Net.Sockets.SocketException"></exception>
        /// <exception cref="System.ObjectDisposedException"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException"></exception>
        /// 
        public void Start()
        {
            SocketState handshakeState = new SocketState(_socket);
            _socket.BeginReceive(handshakeState.buffer, 0, handshakeState.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveHandshake), handshakeState);
        }

        //public void Start(byte[] sent, int sentBytes)
        //{
        //    if(sentBytes > 0)
        //        HandleHandshake(sent);
        //    else
        //        _socket.BeginReceive(handshakeBuffer, 0, _handshakeBufferSize, SocketFlags.None, new AsyncCallback(ReceiveHandshake), null);
        //}

        void ReceiveHandshake(IAsyncResult result)
        {
            try
            {
                SocketState state = (SocketState)result.AsyncState;
                bool data_ends = (state.socket.Available <= state.bufferSize);
                int read = state.socket.EndReceive(result);
                state.read += read;
                //if (read == 0) //TODO Connection closed...
                if (read > 0 && data_ends)
                {
                    state.bufferRead.AddRange(state.buffer);
                    //All data is collected, proceed
                    handshakeBuffer = state.bufferRead.ToArray();
                    _handshakeBufferSize = handshakeBuffer.Length;
                    HandleHandshake(handshakeBuffer);
                }
                else
                    if (state.read == 0 && OnHandshakeNoData != null)
                    {
                        try
                        {
                            OnHandshakeNoData(this);
                        }
                        catch { }
                    }
                    else
                    {
                        //Continue to get message
                        state.bufferRead.AddRange(state.buffer);
                        state.buffer = new byte[state.bufferSize];
                        state.socket.BeginReceive(state.buffer, 0, state.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveHandshake), state);
                        //state.socket.BeginReceive(state.buffers, SocketFlags.None, out state.SocketError, new AsyncCallback(ReceiveHandshake), state);
                    }
            }
            catch (SocketException ex)
            {
                if (OnSocketError != null)
                    try
                    {
                        OnSocketError(this, ex.ErrorCode, ex.NativeErrorCode, ex.SocketErrorCode);
                    }
                    catch { }
            }
            catch (ArgumentException ex)
            {
                if (OnHandshakeError != null)
                    try
                    {
                        OnHandshakeError(this, ex.Message);
                    }
                    catch { }
            }
        }

        protected virtual void HandleHandshake(byte[] handshakeBuffer)
        {
            string _handshakeMessage = Encoding.ASCII.GetString(handshakeBuffer);
            _handshakeValues = ExtractMessageHeaders(_handshakeMessage);
            EvaluateOpenHeaders();
            string response = BuildOKResponse();

            //Send Handshake response
            _socket.Send(Encoding.ASCII.GetBytes(response.ToString()), SocketFlags.None);

            //Begin listening for Web Socket messages
            SocketState state = new SocketState(_socket);
            _socket.BeginReceive(state.buffer, 0, state.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveProtocolMessage), state);
        }

        protected virtual Dictionary<string, string> ExtractMessageHeaders(string message)
        {
            Dictionary<string, string> Headers = new Dictionary<string, string>();

            foreach (var header in OpenHeaders)
            {
                System.Text.RegularExpressions.Regex parser = parsers[header.name];
                System.IO.StringReader str_reader = new System.IO.StringReader(message);
                string line;
                while(!string.IsNullOrEmpty(line=str_reader.ReadLine()))
                {
                    bool is_match = parser.IsMatch(line);
                    if (is_match)
                    {
                        foreach (System.Text.RegularExpressions.Match match in parser.Matches(line))
                            if (match.Groups["value"].Success)
                            {
                                //From RFC6455:
                                //The |Sec-WebSocket-Protocol| header field MAY appear multiple times
                                //in an HTTP request (which is logically the same as a single
                                //|Sec-WebSocket-Protocol| header field that contains all values).
                                //However, the |Sec-WebSocket-Protocol| header field MUST NOT appear
                                //more than once in an HTTP response.
                                if ((header.name) == "Sec-WebSocket-Protocol" && Headers.ContainsKey(header.name))
                                    Headers[header.name] = Headers[header.name] + "," + match.Groups["value"].Value;
                                else
                                    Headers[header.name] = match.Groups["value"].Value;
                            }
                    }
                }
            }
            //
            return Headers;
        }

        private void EvaluateOpenHeaders()
        {
            foreach (HandshakeHeader header in OpenHeaders)
            {
                //TODO: Create a sensible exception...
                if (header.mandatory && !_handshakeValues.ContainsKey(header.name))
                    throw new ArgumentException(
                        string.Format("Invalid incoming handshake, missing mandatory field: {0}", header.name));
            }
            //We will only evaluate the basic headers, further tests will follow.
            //Extensions should be handled by derived classes
            string val;
            if(_handshakeValues.TryGetValue("Sec-WebSocket-Version", out val))
            {
                if(val.Contains(','))
                {
                    string[] requested_versions = val.Split(',');
                    if(requested_versions.Length > 0)
                        if(requested_versions.All(item => item.Trim() != "13"))
                            throw new ArgumentException(
                                string.Format("Version is not supported: {0}", _handshakeValues["Sec-WebSocket-Version"]));
                }
                else
                    if(val.Trim() != "13")
                        throw new ArgumentException(
                            string.Format("Version is not supported: {0}", _handshakeValues["Sec-WebSocket-Version"]));
            }

            //
            if(_handshakeValues.TryGetValue("CONNECTION", out val))
                if(val.ToLower().Trim() != "upgrade")
                    throw new ArgumentException("Connection upgrade request invalid");
             
            if (!string.IsNullOrEmpty(_handshakeValues["Sec-WebSocket-Key"]))
            {
                if (GetClientHandShakeKeyByteCount(_handshakeValues["Sec-WebSocket-Key"]) != 16)
                    throw new ArgumentException("Value of \"Sec-WebSocket-Key\" is invalid");
            }
            else
                throw new ArgumentException("Connection handshake must have \"Sec-WebSocket-Key\" header");

            string protocol;
            if (_handshakeValues.TryGetValue("Sec-WebSocket-Protocol", out protocol))
            {
                string[] protocols = protocol.Split(',');
                foreach (var proto_name in protocols)
                    try
                    {
                        CreateProtocol(proto_name.Trim());
                    }
                    catch (ArgumentException) { }
                //
                if (_protocol == null)
                    throw new ArgumentException(string.Format("Server does not support requested protocol(s): {0}", protocols));
            }
            else
            {
                // No advertised protocol... use application's default
                CreateProtocol("default");
            }
            //...
            OnEvaluatingOpenHandshakeHeaders(ref _handshakeValues);
        }

        private void CreateProtocol(string protocol)
        {
            _protocol = null;
            if (!string.IsNullOrEmpty(protocol))
            {
                if (_protoFactory.AvailableProtocols.Any(item => item.ToLower().Trim() == protocol.ToLower().Trim()))
                {
                    _protocol = _protoFactory.Create(protocol.ToLower().Trim(), this);
                    _protocol.MessageReady += proto_MessageReady;
                }
                else
                    throw new ArgumentException(string.Format("Unsupported protocol: {0}", protocol));
            }
        }

        /// <summary>
        /// Derived classes can override this method to be able to process extension/custom headers in Open Handshake message
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="_handshakeValues"></param>
        protected virtual void OnEvaluatingOpenHandshakeHeaders(ref Dictionary<string,string> _handshakeValues)
        {
        }

        private int GetClientHandShakeKeyByteCount(string hashedKey)
        {
            return Convert.FromBase64String(hashedKey).Count();
        }

        private string BuildOKResponse()
        {
            string sentKey = _handshakeValues["Sec-WebSocket-Key"];
            System.Security.Cryptography.SHA1 sha1 = System.Security.Cryptography.SHA1.Create();
            string newKey = sentKey + Client_KEY;
            newKey = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(newKey)));
            string protocol;
            if(!_handshakeValues.TryGetValue("Sec-WebSocket-Protocol", out protocol))
            {
                protocol = null;
            }

            StringBuilder response = new StringBuilder();
            response.Append("HTTP/1.1 101 Switching Protocols\r\n");
            response.Append("Upgrade: websocket\r\n");
            response.Append("Connection: Upgrade\r\n");
            if (!string.IsNullOrEmpty(protocol))
                response.AppendFormat("Sec-WebSocket-Protocol: {0}\r\n", protocol);

            response.AppendFormat("Sec-WebSocket-Accept: {0}\r\n", newKey);
            response.Append("\r\n");

            OnBeforeSendingHandshakeResponse(ref response);

            return response.ToString();
        }

        /// <summary>
        /// Derived classes may override this class to change the response message.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="handshakeHeaders"></param>
        /// <param name="response"></param>
        protected virtual void OnBeforeSendingHandshakeResponse(ref StringBuilder response)
        {
        }

        protected virtual void proto_MessageReady(WebSocketProtocol protocol, OpCode opCode, byte[] payload)
        {
            //TODO... 
            //[Send the response - idea now is for the protocol defs to decide how to frame the packet, but clearly I need to add info such as OpCode]
            switch (opCode)
	        {
		        case OpCode.Continuation:
                 break;
                case OpCode.Text:
                //TODO: Async send? more control?
                 try
                 {
                     WebSocketPacket.FrameTextData(Encoding.UTF8.GetString(payload),
                         (data, _packet) =>
                         {
                             _socket.Send(data);
                         },
                         (data, error) =>
                         {
                             //TODO
                         }).Start();
                 }
                 catch { }
                 break;
                case OpCode.Binary:
                 //TODO: Async send? more control?
                 try
                 {
                     WebSocketPacket.FrameBinaryData(payload,
                         (data, _packet) =>
                         {
                             _socket.Send(data);
                         },
                         (data, error) =>
                         {
                             //TODO
                         }).Start();
                 }
                 catch { }
                 break;
                case OpCode.Close:
                 break;
                case OpCode.Ping:
                 break;
                case OpCode.Pong:
                 break;
                default:
                    OnExtendedOpCodeMessageHandling(protocol, opCode, payload);
                 break;
	        }
        }

        /// <summary>
        /// To handle extensions
        /// </summary>
        /// <param name="protocol"></param>
        /// <param name="original_opCode"></param>
        /// <param name="opCode"></param>
        /// <param name="isFirst"></param>
        /// <param name="isLast"></param>
        /// <param name="payload"></param>
        protected virtual void OnExtendedOpCodeMessageHandling(WebSocketProtocol protocol, OpCode opCode, byte[] payload)
        {
        }

        private void OnPacketError(byte[] sent, string error)
        {
            //TODO... [send an error response according to RFC6455 and/or close the connection]
        }

        //TODO: When OpCode == Close invoke a "ConnectionClosed" event to make the server cleanup the connection from the list.
        //TODO: Create a timeout mechanism to cleanup connections when data received == 0!
        private void ReceiveProtocolMessage(IAsyncResult ar)
        {
            SocketState state = (SocketState)ar.AsyncState;
            bool data_ends = (state.socket.Available <= state.bufferSize);
            int read = state.socket.EndReceive(ar);
            state.bufferRead.AddRange(state.buffer);
            state.read += read;
            //if (read == 0) TODO: Socket closed by client...

            if (read > 0 || data_ends)
            {
                WebSocketPacket.HandlePacket(state.bufferRead.ToArray(), 
                    (payload, ws) => 
                    {
                        //Note to self: Should protocols be responsible for handling control messages such as ping, pong and close? Perhaps not!
                        if (_protocol != null)
                            _protocol.ProcessIncomingPacket(ws.OpCode, ws);
                        else
                        {
                            //TODO: dummy default "protocol" echoes requests?
                        }
                        if (ws.OpCode != OpCode.Close)
                        {
                            //After handling the message and if it was not a "Close" message, try to receive next message...
                            state = new SocketState(state.socket);
                            _socket.BeginReceive(state.buffer, 0, state.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveProtocolMessage), state);
                        }
                        else
                        {

                            try
                            {
                                state.socket.Close();
                            }
                            catch // TODO: Handle this?
                            {
                            }
                            if (OnSocketClosed != null)
                                try
                                {
                                    OnSocketClosed(this);
                                }
                                catch { } //TODO: Handle this?
                        }
                    }, 
                    OnPacketError).Start(); //TODO: Add handlers...
            }
            else if (read > 0)
            {
                state.buffer = new byte[state.bufferSize];
                state.socket.BeginReceive(state.buffer, 0, state.bufferSize, SocketFlags.None, new AsyncCallback(ReceiveProtocolMessage), state);
            }
        }
    }
}
 