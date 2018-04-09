using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace VideoRecorder
{
    /// <summary>
    /// TCP通信连接类
    /// </summary>
    public class TcpConnection
    {
        private long _sentBytesCount = 0;
        public long SentBytesCount { get { return _sentBytesCount; } }
        private long _receivedBytesCount = 0;
        public long ReceivedBytesCount { get { return _receivedBytesCount; } }
        Socket _client;
        public Socket Client { get { return _client; } }

        public TcpConnection(Socket socket)
        {
            _isConnected = true;
            _client = socket;
            AssociatedID = _client.RemoteEndPoint.ToString().GetHashCode();
            UserTokenObject userToken = new UserTokenObject();
            userToken.AssociatedID = _client.RemoteEndPoint.ToString();
            userToken.WorkSocket = _client;
            Receive(userToken);
        }

        /// <summary>
        /// 接收数据
        /// </summary>
        /// <remarks>读取从中心平台系统转发过来的数据</remarks>
        private void Receive(UserTokenObject userToken)
        {
            try
            {
                if (_isConnected)
                {
                    SocketError error;
                    _client.BeginReceive(userToken.Buffer, 0, UserTokenObject.BufferSize, SocketFlags.None, out error, new AsyncCallback(ReceiveCallback), userToken);
                    if (error != SocketError.Success)
                    {
                        Close();
                        Console.WriteLine("Receive: " + error.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /// <summary>
        /// 接收数据回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (_isConnected)
                {
                    SocketError error;
                    UserTokenObject userToken = (UserTokenObject)ar.AsyncState;
                    int bytesRead = userToken.WorkSocket.EndReceive(ar, out error);
                    if (bytesRead > 0 && error == SocketError.Success)
                    {
                        try
                        {
                            LatestActivedTime = DateTime.Now;
                            Interlocked.Add(ref _receivedBytesCount, bytesRead);
                            if (DataReceived != null)
                            {
                                DataReceivedEventArgs e = new DataReceivedEventArgs();
                                e.UserToken = userToken;
                                e.RealDataSize = bytesRead;
                                e.Data = new byte[bytesRead];
                                Array.Copy(userToken.Buffer, 0, e.Data, 0, bytesRead);
                                DataReceived(this, e);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        Receive(userToken);
                    }
                    else
                    {
                        Close();
                        Console.WriteLine("ReceiveCallback: " + error.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                if (_isConnected)
                {
                    SocketError error;
                    if (_client != null)
                    {
                        _client.BeginSend(data, 0, data.Length, SocketFlags.None, out error, new AsyncCallback(SendCallback), _client);
                        if (error != SocketError.Success)
                        {
                            Close();
                            Console.WriteLine("Send: " + error.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Close()
        {
            try
            {
                if (_isConnected)
                {
                    _isConnected = false;
                    if (DisConnected != null)
                    {
                        DisConnected(this, null);
                    }
                    if (_client != null)
                    {
                        _client.Close();
                        _client = null;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                if (_isConnected)
                {
                    SocketError error;
                    // Retrieve the socket from the state object.
                    Socket client = (Socket)ar.AsyncState;
                    // Complete sending the data to the remote device.
                    int bytesSent = client.EndSend(ar, out error);
                    if (error == SocketError.Success)
                    {
                        Interlocked.Add(ref _sentBytesCount, bytesSent);
                    }
                    else
                    {
                        Close();
                        Console.WriteLine("SendCallback: " + error.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        bool _isConnected = false;
        public bool IsConnected { get { return _isConnected; } }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public event EventHandler DisConnected;

        public int AssociatedID
        {
            get;
            private set;
        }

        public long SentBytes
        {
            get { return _sentBytesCount; }
        }

        public long ReceivedBytes
        {
            get { return _receivedBytesCount; }
        }

        public EndPoint Remote
        {
            get { return _client.RemoteEndPoint; }
        }

        public DateTime LatestActivedTime
        {
            get;
            set;
        }
    }
}
