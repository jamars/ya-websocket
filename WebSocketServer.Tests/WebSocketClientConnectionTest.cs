using WebSocketServer.RFC6455;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace WebSocketServer.Tests
{
    
    
    /// <summary>
    ///This is a test class for WebSocketClientConnectionTest and is intended
    ///to contain all WebSocketClientConnectionTest Unit Tests
    ///</summary>
    [TestClass()]
    public class WebSocketClientConnectionTest
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
        ///A test for ExtractMessageHeaders
        ///</summary>
        [TestMethod()]
        [DeploymentItem("WebSocketServer.dll")]
        public void ExtractMessageHeadersTest()
        {
            WebSocketClientConnection_Accessor target = new WebSocketClientConnection_Accessor();
            string message =
                          "GET /chat HTTP/1.1" + "\r\n" +
                          "Host: server.example.com" + "\r\n" +
                          "Upgrade: websocket" + "\r\n" +
                          "Connection: Upgrade" + "\r\n" +
                          "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" + "\r\n" +
                          @"Origin: http://example.com" + "\r\n" +
                          "Sec-WebSocket-Protocol: chat, superchat" + "\r\n" +
                          "Sec-WebSocket-Version: 13";
            Dictionary<string, string> expected = new Dictionary<string,string>(); // TODO: Initialize to an appropriate value
            expected["HOST"] = "server.example.com";
            expected["UPGRADE"] = "websocket";
            expected["CONNECTION"] = "Upgrade";
            expected["Sec-WebSocket-Key"] = "dGhlIHNhbXBsZSBub25jZQ==";
            expected["ORIGIN"] = @"http://example.com";
            expected["Sec-WebSocket-Protocol"] = "chat, superchat";
            expected["Sec-WebSocket-Version"] = "13";
            Dictionary<string, string> actual;
            actual = target.ExtractMessageHeaders(message);
            bool equals = true;
            foreach (string key in actual.Keys)
                equals = equals && expected.ContainsKey(key);
            foreach (string val in actual.Values)
                equals = equals && actual.ContainsValue(val);
            Assert.IsTrue(equals);
        }
    }
}
