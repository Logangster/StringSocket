﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;

namespace CustomNetworking
{
    //Author: Logan Gore, Nov/16/2014

    /// <summary> 
    /// A StringSocket is a wrapper around a Socket.  It provides methods that
    /// asynchronously read lines of text (strings terminated by newlines) and 
    /// write strings. (As opposed to Sockets, which read and write raw bytes.)  
    ///
    /// StringSockets are thread safe.  This means that two or more threads may
    /// invoke methods on a shared StringSocket without restriction.  The
    /// StringSocket takes care of the synchonization.
    /// 
    /// Each StringSocket contains a Socket object that is provided by the client.  
    /// A StringSocket will work properly only if the client refrains from calling
    /// the contained Socket's read and write methods.
    /// 
    /// If we have an open Socket s, we can create a StringSocket by doing
    /// 
    ///    StringSocket ss = new StringSocket(s, new UTF8Encoding());
    /// 
    /// We can write a string to the StringSocket by doing
    /// 
    ///    ss.BeginSend("Hello world", callback, payload);
    ///    
    /// where callback is a SendCallback (see below) and payload is an arbitrary object.
    /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
    /// successfully written the string to the underlying Socket, or failed in the 
    /// attempt, it invokes the callback.  The parameters to the callback are a
    /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
    /// the Exception that caused the send attempt to fail.
    /// 
    /// We can read a string from the StringSocket by doing
    /// 
    ///     ss.BeginReceive(callback, payload)
    ///     
    /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
    /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
    /// string of text terminated by a newline character from the underlying Socket, or
    /// failed in the attempt, it invokes the callback.  The parameters to the callback are
    /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
    /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
    /// it is the requested string (with the newline removed).  If the Exception is non-null, 
    /// it is the Exception that caused the send attempt to fail.
    /// </summary>

    public class StringSocket
    {
        // These delegates describe the callbacks that are used for sending and receiving strings.
        public delegate void SendCallback(Exception e, object payload);
        public delegate void ReceiveCallback(String s, Exception e, object payload);

        /// <summary>
        /// Keeps track of send requests
        /// </summary>
        private Queue<Request> sendQueue;
        /// <summary>
        /// Keeps track of receive requests
        /// </summary>
        private Queue<Request> receiveQueue;

        //Socket and encoding that is sent to the class
        private Socket client;
        private Encoding encoding;

        /// <summary>
        /// Keeps track of the most current string received
        /// </summary>
        private string currentString = "";

        //Locks
        private Object sendLock = new Object();
        private Object receiveLock = new Object();
        private Object sendCallbackLock = new Object();
        private object receiveCallbackLock = new Object();
        private object receiveQueueLock = new Object();

        /// <summary>
        /// Creates a StringSocket from a regular Socket, which should already be connected.  
        /// The read and write methods of the regular Socket must not be called after the
        /// LineSocket is created.  Otherwise, the StringSocket will not behave properly.  
        /// The encoding to use to convert between raw bytes and strings is also provided.
        /// </summary>
        public StringSocket(Socket s, Encoding e)
        {
            sendQueue = new Queue<Request>();
            client = s;
            encoding = e;
            receiveQueue = new Queue<Request>();
        }

        /// <summary>
        /// We can write a string to a StringSocket ss by doing
        /// 
        ///    ss.BeginSend("Hello world", callback, payload);
        ///    
        /// where callback is a SendCallback (see below) and payload is an arbitrary object.
        /// This is a non-blocking, asynchronous operation.  When the StringSocket has 
        /// successfully written the string to the underlying Socket, or failed in the 
        /// attempt, it invokes the callback.  The parameters to the callback are a
        /// (possibly null) Exception and the payload.  If the Exception is non-null, it is
        /// the Exception that caused the send attempt to fail. 
        /// 
        /// This method is non-blocking.  This means that it does not wait until the string
        /// has been sent before returning.  Instead, it arranges for the string to be sent
        /// and then returns.  When the send is completed (at some time in the future), the
        /// callback is called on another thread.
        /// 
        /// This method is thread safe.  This means that multiple threads can call BeginSend
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginSend must take care of synchronization instead.  On a given StringSocket, each
        /// string arriving via a BeginSend method call must be sent (in its entirety) before
        /// a later arriving string can be sent.
        /// </summary>
        public void BeginSend(String s, SendCallback callback, object payload)
        {
            lock (sendLock)
            {
                //Create a new request and add it to the queue
                Request newRequest = new Request(encoding.GetBytes(s), callback, payload);
                sendQueue.Enqueue(newRequest);

                Request currentRequest = sendQueue.Dequeue();

                //Begin sending with the MessageBuffer attached to the request class instance
                byte[] currentRequestMessage = currentRequest.MessageBuffer;
                client.BeginSend(currentRequestMessage, 0, currentRequestMessage.Length, SocketFlags.None, BeginSendCallback, currentRequest);
            }
        }

        private void BeginSendCallback(IAsyncResult ar)
        {
            //Retreive request from results
            Request currentRequest = (Request)ar.AsyncState;

            //Figure out how much data was sent
            currentRequest.Count = currentRequest.Count + client.EndSend(ar);
            string message = encoding.GetString(currentRequest.MessageBuffer);

            //If not all the data was sent, send the rest
            if (currentRequest.Count < message.Length)
                client.BeginSend(currentRequest.MessageBuffer, currentRequest.Count - 1, message.Length - 1, SocketFlags.None, BeginSendCallback, currentRequest);
            else
            {
                ThreadPool.QueueUserWorkItem(ignored =>
                currentRequest.SendingCallback(null, currentRequest.Payload));
            }
        }

        /// <summary>
        /// 
        /// <para>
        /// We can read a string from the StringSocket by doing
        /// </para>
        /// 
        /// <para>
        ///     ss.BeginReceive(callback, payload)
        /// </para>
        /// 
        /// <para>
        /// where callback is a ReceiveCallback (see below) and payload is an arbitrary object.
        /// This is non-blocking, asynchronous operation.  When the StringSocket has read a
        /// string of text terminated by a newline character from the underlying Socket, or
        /// failed in the attempt, it invokes the callback.  The parameters to the callback are
        /// a (possibly null) string, a (possibly null) Exception, and the payload.  Either the
        /// string or the Exception will be non-null, but nor both.  If the string is non-null, 
        /// it is the requested string (with the newline removed).  If the Exception is non-null, 
        /// it is the Exception that caused the send attempt to fail.
        /// </para>
        /// 
        /// <para>
        /// This method is non-blocking.  This means that it does not wait until a line of text
        /// has been received before returning.  Instead, it arranges for a line to be received
        /// and then returns.  When the line is actually received (at some time in the future), the
        /// callback is called on another thread.
        /// </para>
        /// 
        /// <para>
        /// This method is thread safe.  This means that multiple threads can call BeginReceive
        /// on a shared socket without worrying around synchronization.  The implementation of
        /// BeginReceive must take care of synchronization instead.  On a given StringSocket, each
        /// arriving line of text must be passed to callbacks in the order in which the corresponding
        /// BeginReceive call arrived.
        /// </para>
        /// 
        /// <para>
        /// Note that it is possible for there to be incoming bytes arriving at the underlying Socket
        /// even when there are no pending callbacks.  StringSocket implementations should refrain
        /// from buffering an unbounded number of incoming bytes beyond what is required to service
        /// the pending callbacks.        
        /// </para>
        /// 
        /// <param name="callback"> The function to call upon receiving the data</param>
        /// <param name="payload"> 
        /// The payload is "remembered" so that when the callback is invoked, it can be associated
        /// with a specific Begin Receiver....
        /// </param>  
        /// 
        /// <example>
        ///   Here is how you might use this code:
        ///   <code>
        ///                    client = new TcpClient("localhost", port);
        ///                    Socket       clientSocket = client.Client;
        ///                    StringSocket receiveSocket = new StringSocket(clientSocket, new UTF8Encoding());
        ///                    receiveSocket.BeginReceive(CompletedReceive1, 1);
        /// 
        ///   </code>
        /// </example>
        /// </summary>
        /// 
        /// 
        public void BeginReceive(ReceiveCallback callback, object payload)
        {
            //receiveQueueLock is used anytime the queue is maniuplated 
            lock (receiveQueueLock)
            {
                receiveQueue.Enqueue(new Request(new byte[256], callback, payload));
            }

            //If we still have a currentString to process left over from past receives
            processCurrentString();

            //recieveLock used when BeginReceive method is manipulating data
            lock (receiveLock)
            {
                //If a request was just added, take care of it, if not, don't receive more until the receiveQueue is done
                if (receiveQueue.Count == 1)
                {
                    byte[] buffer = new byte[256];
                    client.BeginReceive(buffer, 0, 256, SocketFlags.None, BeginReceiveCallback, buffer);
                }
            }
        }

        /// <summary>
        /// Called after underlying socket for BeginSend finishes sending
        /// </summary>
        /// <param name="ar">Results</param>
        private void BeginReceiveCallback(IAsyncResult ar)
        {
            byte[] currentRequest = (byte[])ar.AsyncState;
            int count = client.EndReceive(ar);

            //receiveCallbackLock used when the BeginRecieveCallback method is manipulating data
            lock (receiveCallbackLock)
            {
                //Read in the string, add it to the currentString, then process it
                string receivedString = encoding.GetString(currentRequest, 0, count);
                currentString += receivedString;
                processCurrentString();
            }

            //If we have more requests, take care of them
            if (receiveQueue.Count > 0)
            {
                byte[] buffer = new byte[256];
                client.BeginReceive(buffer, 0, 256, SocketFlags.None, BeginReceiveCallback, buffer);
            }
        }

        /// <summary>
        /// Used for processing currentString for the use of keeping track of received data
        /// </summary>
        private void processCurrentString()
        {
            lock (receiveCallbackLock)
            {
                int index;

                //while current string still has a \n
                while (currentString.Contains("\n") && receiveQueue.Count > 0)
                {
                    index = currentString.IndexOf("\n");
                    Request request;

                    //Create a new string from currentString up until the new line marker
                    string newString = currentString.Substring(0, index);

                    lock (receiveQueueLock)
                    {
                        request = receiveQueue.Dequeue();
                    }

                    ThreadPool.QueueUserWorkItem(ignored =>
                    request.receivingCallback(newString, null, request.Payload)
                    );

                    //update currentString to remove newString
                    currentString = currentString.Substring(index + 1);
                }
            }
        }

        /// <summary>
        /// Calling the close method will close the String Socket (and the underlying
        /// standard socket).  The close method  should make sure all 
        ///
        /// Note: ideally the close method should make sure all pending data is sent
        ///       
        /// Note: closing the socket should discard any remaining messages and       
        ///       disable receiving new messages
        /// 
        /// Note: Make sure to shutdown the socket before closing it.
        ///
        /// Note: the socket should not be used after closing.
        /// </summary>
        public void Close()
        {
            client.Shutdown(SocketShutdown.Both);
            client.Close();
        }
    }
}