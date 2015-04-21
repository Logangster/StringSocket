using CustomNetworking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;

namespace StringSocketTester
{


    /// <summary>
    ///This is a test class for StringSocketTest and is intended
    ///to contain all StringSocketTest Unit Tests
    ///</summary>
    [TestClass()]
    public class StringSocketTest
    {

        #region test_non_ASCII
        /// <author>Matthew Madden</author>
        /// <timecreated>11/11/14</timecreated>
        /// <summary>
        /// This method tests whether non-ASCII (multi-byte) characters are 
        /// passed through the String Socket intact, based on the encoding provided. 
        /// UTF-8 encoding can encode/decode any valid Unicode character.
        ///</summary>
        [TestMethod()]
        public void Test_non_ASCII()
        {
            new TestClass_non_ASCII().run(4100);
        }

        public class TestClass_non_ASCII
        {
            private ManualResetEvent mre1;
            private String msg;
            private object p1;
            StringSocket sendSocket, receiveSocket;

            // Timeout
            private static int timeout = 2000;

            public void run(int port)
            {
                TcpListener server = null;
                TcpClient client = null;


                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;
                    sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    mre1 = new ManualResetEvent(false);

                    receiveSocket.BeginReceive(CompletedReceive, 1);
                    sendSocket.BeginSend("Hêllø Ψórlđ!\n", (e, o) => { }, null);

                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting");
                    // this will fail if the String Socket does not handle non-ASCII characters
                    Assert.AreEqual("Hêllø Ψórlđ!", msg);
                    System.Diagnostics.Debug.WriteLine(msg);
                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    sendSocket.Close();
                    receiveSocket.Close();
                    server.Stop();
                    client.Close();
                }
            }

            //callback
            private void CompletedReceive(String s, Exception o, object payload)
            {
                msg = s;
                p1 = payload;
                mre1.Set();
            }
        }
        #endregion
        /// <summary>
        /// Authors: Greg Smith and Jase Bleazard
        /// Attempts sending the newline character by itself. The sockets should
        /// still send and receive a blank String, "".
        /// </summary>
        [TestMethod()]
        public void SendAndReceiveEmpty()
        {
            new SendAndReceiveEmptyClass().run(4006);
        }

        public class SendAndReceiveEmptyClass
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private String s1;
            private object p1;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);

                    // Now send the data.  Hope those receive requests didn't block!
                    sendSocket.BeginSend("\n", (e, o) => { }, null);

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("", s1);

                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }
        }

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
        ///A simple test for BeginSend and BeginReceive
        ///</summary>
        [TestMethod()]
        public void Test1()
        {
            new Test1Class().run(4001);
        }

        public class Test1Class
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 50000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    receiveSocket.BeginReceive(CompletedReceive1, 1);
                    receiveSocket.BeginReceive(CompletedReceive2, 2);

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "Hello world\nThis is a test\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make sure the lines were received properly.
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("Hello world", s1);
                    Assert.AreEqual(1, p1);

                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("This is a test", s2);
                    Assert.AreEqual(2, p2);
                    
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            // This is the callback for the first receive request.  We can't make assertions anywhere
            // but the main thread, so we write the values to member variables so they can be tested
            // on the main thread.
            private void CompletedReceive1(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // This is the callback for the second receive request.
            private void CompletedReceive2(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }


        /// <summary>
        ///A test for empty strings and carriage returns
        ///Tests that \n works for empty strings, \r works for full strings, \r works for empty strings
        ///Also tests to make sure that strings are received in the proper order while doing so
        ///Modifies test case already provided, done by Logan Gore
        ///</summary>
        [TestMethod()]
        public void TestMultipleEmptyStrings()
        {
            new EmptyAndCarriageStrings().run(4001);
        }

        public class EmptyAndCarriageStrings
        {
            // Data that is shared across threads
            private ManualResetEvent mre1;
            private ManualResetEvent mre2;
            private String s1;
            private object p1;
            private String s2;
            private object p2;

            // Timeout used in test case
            private static int timeout = 2000;

            public void run(int port)
            {
                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, port);
                    server.Start();
                    client = new TcpClient("localhost", port);

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    mre1 = new ManualResetEvent(false);
                    mre2 = new ManualResetEvent(false);

                    // Make three receive requests
                    receiveSocket.BeginReceive(ReceiveEmpty, 1);
                    receiveSocket.BeginReceive(ReceiveString, 2);
                    receiveSocket.BeginReceive(ReceiveEmpty, 1);

                    // Send multiple new lines mixing both carriage and newline characters
                    String msg = "\nHelloWorld\r\n\r\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make sure empty string with \n
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("", s1);
                    Assert.AreEqual(1, p1);

                    //Make sure full string with \r
                    Assert.AreEqual(true, mre2.WaitOne(timeout), "Timed out waiting 2");
                    Assert.AreEqual("HelloWorld", s2);
                    Assert.AreEqual(2, p2);

                    //Make sure empty string with \r
                    Assert.AreEqual(true, mre1.WaitOne(timeout), "Timed out waiting 1");
                    Assert.AreEqual("", s1);
                    Assert.AreEqual(1, p1);
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }

            //call back that receives the empty string
            private void ReceiveEmpty(String s, Exception o, object payload)
            {
                s1 = s;
                p1 = payload;
                mre1.Set();
            }

            // call back that receives helloworld, ensuring that carriage returns work with strings
            private void ReceiveString(String s, Exception o, object payload)
            {
                s2 = s;
                p2 = payload;
                mre2.Set();
            }
        }

        /// <summary>
        /// Authors: Jarom Norris and Sarah Cotner
        /// November 2014
        /// University of Utah CS 3500 with Dr. de St. Germain
        /// This is a simple test to make sure that string socket is written to be non-blocking,
        /// regardless of inappropriate callbacks. Uses the functions BlockingTestCallback1 and
        /// BlockingTestCallback2.
        /// </summary>
        [TestMethod()]
        public void JaromAndSarahNonBlockingTest()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 4002);
            server.Start();
            TcpClient client = new TcpClient("localhost", 4002);

            Socket serverSocket = server.AcceptSocket();
            Socket clientSocket = client.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

            sendSocket.BeginSend("This test wa", (e, p) => { }, 1);
            sendSocket.BeginSend("s made by\n", (e, p) => { }, 2);
            sendSocket.BeginSend("Jarom and Sarah!\n", (e, p) => { }, 3);

            receiveSocket.BeginReceive(BlockingTestCallback1, 4);
            receiveSocket.BeginReceive(BlockingTestCallback2, 5);
        }

        public void BlockingTestCallback1(string s, Exception e, object payload)
        {
            while (true)
                Thread.Sleep(500);
        }

        public void BlockingTestCallback2(string s, Exception e, object payload)
        {
            Assert.AreEqual(s, "Jarom and Sarah!");
            Assert.AreEqual(payload, 5);
        }

        /// <summary>
        /// Written by: Kyle Hiroyasu and Drake Bennion
        /// This test is designed to ensure that string sockets will properly wait for strings to be sent and received
        /// The last send also ensures that a message is broken up by newline character but maintains same payload
        /// </summary>
        [TestMethod()]
        public void MessageOrderStressTest()
        {
            int Port = 4000;
            int timeout = 30000;
            TcpListener server = null;
            TcpClient client = null;


            try
            {
                server = new TcpListener(IPAddress.Any, Port);
                server.Start();
                client = new TcpClient("localhost", Port);
                Socket serverSocket = server.AcceptSocket();
                Socket clientSocket = client.Client;

                StringSocket send = new StringSocket(serverSocket, Encoding.UTF8);
                StringSocket receive = new StringSocket(clientSocket, Encoding.UTF8);

                //Messages
                string message1 = "The sky is blue\n";
                string message2 = "The grass is green\n";
                string message3 = "Drakes hat is blue\n";
                string message4 = (new String('h', 1000)) + message1 + message2 + message3;
                string message4s = (new String('h', 1000)) + message1;

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message1, message);
                    Assert.AreEqual(1, o);
                }, 1);

                send.BeginSend(message1, (e, o) => { }, 1);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message2, message);
                    Assert.AreEqual(2, o);
                }, 1);

                send.BeginSend(message2, (e, o) => { }, 2);
                send.BeginSend(message3, (e, o) => { }, 3);
                send.BeginSend(message4, (e, o) => { }, 4);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message3, message);
                    Assert.AreEqual(3, o);
                }, 1);

                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message4s, message);
                    Assert.AreEqual(4, o);
                }, 1);
                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message2, message);
                    Assert.AreEqual(4, o);
                }, 1);
                receive.BeginReceive((message, e, o) =>
                {
                    Assert.AreEqual(message3, message);
                    Assert.AreEqual(4, o);
                }, 1);

            }
            finally
            {
                server.Stop();
                client.Close();
            }
        }

        /// <summary>
        /// Braden Scothern Test Case
        /// This test heavily relies on the original test we were given
        /// But it has been modified to run 5 times meaning that the ports need to be properly closed
        /// It also sends each char as it's own message now by putting a "\n" after each.
        /// This means that more messages are sent possibly resulting in more blocking if not strucutured correctly.
        /// </summary>
        [TestMethod()]
        public void Test_Port_ASCII_JIM2000() //ASCII code for JIM is 747377
        {
            for (int counter = 0; counter < 5; counter++)
            {
                String[] sA = new String[26];
                object[] pA = new object[26];
                String tester = "Hello world This is a test";

                // Create and start a server and client.
                TcpListener server = null;
                TcpClient client = null;

                try
                {
                    server = new TcpListener(IPAddress.Any, ('J' + 'I' + 'M' + 2000));
                    server.Start();
                    client = new TcpClient("localhost", ('J' + 'I' + 'M' + 2000));

                    // Obtain the sockets from the two ends of the connection.  We are using the blocking AcceptSocket()
                    // method here, which is OK for a test case.
                    Socket serverSocket = server.AcceptSocket();
                    Socket clientSocket = client.Client;

                    // Wrap the two ends of the connection into StringSockets
                    StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

                    // This will coordinate communication between the threads of the test cases
                    ManualResetEvent mre1 = new ManualResetEvent(false);
                    ManualResetEvent mre2 = new ManualResetEvent(false);

                    // Make two receive requests
                    for (int i = 0; i < tester.Length; i++)
                    {
                        receiveSocket.BeginReceive((s, o, p) => { sA[i] = s; pA[i] = p; }, i);
                    }

                    // Now send the data.  Hope those receive requests didn't block!
                    String msg = "H\ne\nl\nl\no\n \nw\no\nr\nl\nd\n \nT\nh\ni\ns\n \ni\ns\n \na\n \nt\ne\ns\nt\n";
                    foreach (char c in msg)
                    {
                        sendSocket.BeginSend(c.ToString(), (e, o) => { }, null);
                    }

                    // Make sure the lines were received properly.
                    for (int i = 0; i < tester.Length; i++)
                    {
                        Assert.AreEqual(true, mre1.WaitOne(150), "Timed out waiting for char" + sA[i] + " (" + (i + 1) + ")");
                        Assert.AreEqual(tester[i], sA[i]);
                        Assert.AreEqual(i, pA[i]);
                    }
                }
                finally
                {
                    server.Stop();
                    client.Close();
                }
            }
        }
        /// <summary>
        /// Author: Ryan Farr
        /// A simple test to make sure Close() works
        ///</summary>
        [TestMethod()]
        [ExpectedException(typeof(System.ObjectDisposedException))]
        public void TestCloseBasic()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 4006);
            server.Start();
            TcpClient client = new TcpClient("localhost", 4006);

            Socket serverSocket = server.AcceptSocket();
            Socket clientSocket = client.Client;

            StringSocket sendSocket = new StringSocket(serverSocket, new UTF8Encoding());
            StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());

            sendSocket.Close();
            receiveSocket.Close();

            bool test1 = serverSocket.Available == 0; //Should fail here because socket should be shutdown and closed
        }
    }

}
