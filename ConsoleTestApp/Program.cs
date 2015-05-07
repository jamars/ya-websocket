using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace ConsoleTestApp
{
    class WebSocketProtocolImpl : WebSocketServer.RFC6455.WebSocketProtocol
    {
        public WebSocketProtocolImpl(string protocol, WebSocketServer.RFC6455.WebSocketClientConnection connection)
            : base(protocol, connection)
        {
        }

        public override void ProcessIncomingPacket(WebSocketServer.RFC6455.OpCode opCode, WebSocketServer.RFC6455.WebSocketPacket packet)
        {
            switch (opCode)
            {
                case WebSocketServer.RFC6455.OpCode.Text:
                    string received = Encoding.ASCII.GetString(packet.Payload);
                    Console.WriteLine("Received: {0}", received);

                    //In this example we simply create a basic response and send it back to the browser
                    OnMessageReady(opCode, CreateResponse(received));

                    return;
                case WebSocketServer.RFC6455.OpCode.Close:
                    return;
                default:
                    break;
            }
            throw new NotImplementedException();
        }

        private byte[] CreateResponse(string received)
        {
            //Echo the data back...
            string response = "{ \"Id:\" 5, \"Message\": \"Responding!...\" }";
            return Encoding.UTF8.GetBytes(response);
        }
    }

    class WebSocketProtocolFactoryImpl : WebSocketServer.RFC6455.WebSocketProtocolFactory
    {
        public WebSocketProtocolFactoryImpl()
        {
            protocols.Add("default"); 
        }

        public override WebSocketServer.RFC6455.WebSocketProtocol Create(string protocol, WebSocketServer.RFC6455.WebSocketClientConnection connection)
        {
            return new WebSocketProtocolImpl(protocol, connection);
        }
    }

    class WebSocketServerImpl : WebSocketServer.RFC6455.WebSocketServer
    {
        public WebSocketServerImpl(string uri, WebSocketProtocolFactoryImpl protocolFactory)
            : base(uri, protocolFactory)
        {
        }
    }

    unsafe class Program
    {
        static void Main(string[] args)
        {
            //WS_PACKET* test;
            //byte[] t = new byte[4];
            //unsafe
            //{
            //    byte[] buffer = new byte[255];
            //    fixed (byte* pbuffer = buffer)
            //    {
            //        test = (WSPACKET*)*pbuffer;
            //    }
            //}
            //WebSocketServer.WebSocketConnection.TestSHA1();
            //Dictionary<string, string> headers = WebSocketServer.WebSocketClientConnection.TestOpen();

            WebSocketServerImpl server = new WebSocketServerImpl("ws://localhost:8888", new WebSocketProtocolFactoryImpl());

            Console.ReadLine();
        }
    }
}
