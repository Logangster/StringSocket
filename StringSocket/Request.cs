using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CustomNetworking
{
    class Request
    {
        /// <summary>
        /// Used as a buffer for sending messages
        /// </summary>
        public byte[] MessageBuffer { get; private set; }

        /// <summary>
        /// Used to save the callback for sending messages
        /// </summary>
        public StringSocket.SendCallback SendingCallback { get; private set; }

        /// <summary>
        /// Which object sent/received the data
        /// </summary>
        public object Payload { get; private set; }

        /// <summary>
        /// Used to save the callback fro receiving messages
        /// </summary>
        public StringSocket.ReceiveCallback receivingCallback { get; private set; }

        /// <summary>
        /// Keeps track of the count of data sent/recieved
        /// </summary>
        public int Count { get; set; }

        public Request(byte[] messageBuffer, StringSocket.SendCallback callback, object payload)
        {
            this.MessageBuffer = messageBuffer;
            this.SendingCallback = callback;
            this.Payload = payload;
            this.Count = 0;
        }

        public Request(byte[] message, StringSocket.ReceiveCallback callback, object payload)
        {
            this.MessageBuffer = message;
            this.receivingCallback = callback;
            this.Payload = payload;
            this.Count = 0;
        }
    }
}
