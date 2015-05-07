using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WebSocketServer.Tests
{
    [TestClass]
    public class WebSocketPacketUnitTest
    {
        [TestMethod]
        public void HandleIncomingPacketTest1()
        {
            byte[] packet = new byte[] { 0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58 };

        }
    }
}
