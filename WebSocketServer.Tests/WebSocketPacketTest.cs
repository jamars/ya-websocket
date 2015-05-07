using WebSocketServer.RFC6455;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace WebSocketServer.Tests
{
    
    
    /// <summary>
    ///This is a test class for WebSocketPacketTest and is intended
    ///to contain all WebSocketPacketTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WebSocketPacketTest
    {


        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for ExtractPayload
        ///</summary>
        [TestMethod()]
        [DeploymentItem("WebSocketServer.dll")]
        public void ExtractPayloadRfc6455MaskedHelloTest()
        {
            WebSocketPacket_Accessor target = new WebSocketPacket_Accessor(new byte[] { 0x81, 0x85, 0x37, 0xfa, 0x21, 0x3d, 0x7f, 0x9f, 0x4d, 0x51, 0x58 }); // TODO: Initialize to an appropriate value
            target.ExtractPayload();
            Assert.IsNotNull(target.payload);
            Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(target.payload));
        }

        /// <summary>
        ///A test for ExtractPayload
        ///</summary>
        [TestMethod()]
        [DeploymentItem("WebSocketServer.dll")]
        public void ExtractPayloadRfc6455UnMaskedHelloTest()
        {
            WebSocketPacket_Accessor target = new WebSocketPacket_Accessor(new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f }); // TODO: Initialize to an appropriate value
            target.ExtractPayload();
            Assert.IsNotNull(target.payload);
            Assert.AreEqual("Hello", System.Text.Encoding.UTF8.GetString(target.payload));
        }

        /// <summary>
        ///A test for SetPayload
        ///</summary>
        [TestMethod()]
        [DeploymentItem("WebSocketServer.dll")]
        public void SetPayloadTest()
        {
            WebSocketPacket_Accessor target = new WebSocketPacket_Accessor(); // TODO: Initialize to an appropriate value
            target.IsLastPacket = true;
            target.IsMasked = false;
            target.OpCode = OpCode.Text;
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello"); ; // TODO: Initialize to an appropriate value
            target.SetPayload(data);
            byte[] expected = new byte[] { 0x81, 0x05, 0x48, 0x65, 0x6c, 0x6c, 0x6f };
            Assert.AreEqual(expected.Length, target._packet.Length);
            bool equals = true;
            for (int i = 0; i < target._packet.Length; i++)
                equals = equals && (expected[i] == target._packet[i]);
            Assert.IsTrue(equals);
        }

        /// <summary>
        ///A test for SetPayload
        ///</summary>
        [TestMethod()]
        [DeploymentItem("WebSocketServer.dll")]
        public void SetPayloadTestMasked()
        {
            WebSocketPacket_Accessor target = new WebSocketPacket_Accessor(); // TODO: Initialize to an appropriate value
            target.IsLastPacket = true;
            target.IsMasked = true;
            target.OpCode = OpCode.Text;
            byte[] data = System.Text.Encoding.UTF8.GetBytes("Hello"); ; // TODO: Initialize to an appropriate value
            target.SetPayload(data);
            //
            WebSocketPacket_Accessor target1 = new WebSocketPacket_Accessor(target._packet);
            target1.ExtractPayload();
            //
            Assert.AreEqual(target1._packet.Length, target._packet.Length);
            bool equals = true;
            for (int i = 0; i < target1._packet.Length; i++)
                equals = equals && (target1._packet[i] == target._packet[i]);
            Assert.IsTrue(equals);
            Assert.AreEqual(target1.Payload.Length, target.Payload.Length);
            equals = true;
            for (int i = 0; i < target1.Payload.Length; i++)
                equals = equals && (target.Payload[i] == target1.Payload[i]);
            Assert.IsTrue(equals);
            equals = true;
            for (int i = 0; i < target1.MaskedPayload.Length; i++)
                equals = equals && (target1.MaskedPayload[i] == target.MaskedPayload[i]);
            Assert.IsTrue(equals);
        }
    }
}
