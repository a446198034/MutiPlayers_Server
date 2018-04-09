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
    /// TCP服务器连接类
    /// </summary>
    public class TcpClient
    {
        private string _serverIp = string.Empty;
        private int _serverPort = -1;
        private IPEndPoint remoteEP = null;
        private Socket _serverSocket = null;
        private System.Timers.Timer timer = null;
        private DateTime _lastCheckTime;
        private int _heartBeatSeconds = 60;
        private long _sentBytesCount = 0;
        public long SentBytesCount { get { return _sentBytesCount; } }
        private long _receivedBytesCount = 0;
        public long ReceivedBytesCount { get { return _receivedBytesCount; } }
        public string IP { get { return _serverIp; } }
        public int Port { get { return _serverPort; } }

        public event EventHandler<DataReceivedEventArgs> DataReceived = delegate { };
        public event EventHandler OnConnected = delegate { };
        public event EventHandler OnTimeout = delegate { };
        public event EventHandler OnClosed = delegate { };

        public bool IsConnected { get { return _serverSocket != null && _serverSocket.Connected; } }//{ get { return _isSocketConnected; } }

        public TcpClient(string serverIp, int serverPort)
        {
            _serverIp = serverIp;
            _serverPort = serverPort;

            remoteEP = new IPEndPoint(IPAddress.Parse(_serverIp), _serverPort);

            timer = new System.Timers.Timer();
            timer.AutoReset = true;
            timer.Interval = 2000;
            timer.Elapsed += new System.Timers.ElapsedEventHandler(timer_Elapsed);
        }

        bool _isBindPort = false;
        public TcpClient(string serverIp, int serverPort, int heartBeatSeconds, bool isBindPort)
            : this(serverIp, serverPort)
        {
            _heartBeatSeconds = heartBeatSeconds;
            _isBindPort = isBindPort;
        }

        void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Connect(_serverIp, _serverPort);
        }

        /// <summary>
        /// 连接远程主机
        /// </summary>
        /// <param name="remotehostorip">远程主机名或IP址</param>
        /// <param name="remoteport">通信端口</param>
        private void Connect(string remotehostorip, int remoteport)
        {
            try
            {
                //解析远程主机
                IPAddress resolvedIP = null;
                if (IPAddress.TryParse(remotehostorip, out resolvedIP))
                    Connect(resolvedIP, remoteport);
                else
                    Dns.BeginGetHostEntry(remotehostorip, new AsyncCallback(DoConnectCallback), remoteport);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 连接回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void DoConnectCallback(IAsyncResult ar)
        {
            try
            {
                int port = (int)ar.AsyncState;
                IPHostEntry resolved = null;
                try
                {
                    resolved = Dns.EndGetHostEntry(ar);
                }
                catch (Exception ex)
                {
                    resolved = null;
                    throw ex;
                }
                if ((resolved == null) || (resolved.AddressList.Length == 0))
                {
                    string name = resolved == null ? "\"" + resolved.HostName + "\"" : "";
                    throw new Exception("主机名：" + name + "无法解析.");
                }
                Connect(resolved.AddressList[0], port);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Start()
        {
            Connect(_serverIp, _serverPort);
            _lastCheckTime = DateTime.Now;
            timer.Enabled = true;
        }

        public void Stop()
        {
            timer.Enabled = false;
            CloseSocket();
        }

        /// <summary>
        /// 连接中心系统平台
        /// </summary>
        /// <param name="remoteip">远程主机IP地址</param>
        /// <param name="remoteport">通信端口</param>
        private void Connect(IPAddress remoteip, int remoteport)
        {
            try
            {
                if (_serverSocket != null)
                {
                    // This is how you can determine whether a socket is still connected.  
                    if (_serverSocket.Connected)
                    {
                        if (DateTime.Now.Subtract(_lastCheckTime).TotalSeconds >= _heartBeatSeconds)
                        {
                            _lastCheckTime = DateTime.Now;
                            //发送连接测试
                            OnTimeout(this, null);
                        }
                        return;
                    }
                    else
                    {
                        CloseSocket();
                    }
                }
                _serverSocket = new Socket(remoteEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //uint dummy = 0;
                //byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                //BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                //BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                //BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
                //_serverSocket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);             
                LingerOption linger = new LingerOption(true, 0);
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, linger);
                _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, false);
                if (_isBindPort)
                {
                    _serverSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    _serverSocket.Bind(new IPEndPoint(IPAddress.Any, _serverPort));
                }
                _serverSocket.BeginConnect(remoteEP, new AsyncCallback(ConnectCallback), _serverSocket);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /// <summary>
        /// 连接回调函数
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                Socket socket = (Socket)ar.AsyncState;
                socket.EndConnect(ar);
                //发送认证内容到GpsServer程序
                OnConnected(this, null);
                UserTokenObject userToken = new UserTokenObject();
                userToken.AssociatedID = socket.RemoteEndPoint.ToString();
                userToken.WorkSocket = socket;
                Receive(userToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        /// <summary>
        /// 接收数据
        /// </summary>
        /// <remarks>读取从中心平台系统转发过来的数据</remarks>
        private void Receive(UserTokenObject userToken)
        {
            try
            {
                SocketError error;
                _serverSocket.BeginReceive(userToken.Buffer, 0, UserTokenObject.BufferSize, SocketFlags.None, out error, new AsyncCallback(ReceiveCallback), userToken);
                if (error != SocketError.Success)
                {
                    CloseSocket();
                    Console.WriteLine("Receive: " + error.ToString());
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
                SocketError error;
                UserTokenObject userToken = (UserTokenObject)ar.AsyncState;
                int bytesRead = _serverSocket.EndReceive(ar, out error);
                if (bytesRead > 0 && error == SocketError.Success)
                {
                    Interlocked.Add(ref _receivedBytesCount, bytesRead);
                    DataReceivedEventArgs e = new DataReceivedEventArgs();
                    e.UserToken = userToken;
                    e.RealDataSize = bytesRead;
                    e.Data = new byte[bytesRead];
                    Array.Copy(userToken.Buffer, 0, e.Data, 0, bytesRead);
                    DataReceived(this, e);
                    Receive(userToken);
                }
                else
                {
                    CloseSocket();
                    Console.WriteLine("ReceiveCallback: " + error.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public void Send(string data)
        {
            data = data + "<EOF>";
            Send(System.Text.Encoding.GetEncoding("GBK").GetBytes(data));
        }

        public void Send(byte[] data)
        {
            try
            {
                SocketError error;
                IAsyncResult ar = _serverSocket.BeginSend(data, 0, data.Length, SocketFlags.None, out error, new AsyncCallback(SendCallback), _serverSocket);
                if (error != SocketError.Success)
                {
                    CloseSocket();
                    Console.WriteLine("Send: " + error.ToString());
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
                    CloseSocket();
                    Console.WriteLine("SendCallback: " + error.ToString());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void CloseSocket()
        {
            if (_serverSocket != null)
            {
                _serverSocket.Close();
                _serverSocket = null;
                if (OnClosed != null)
                {
                    OnClosed(this, null);
                }
            }
        }




        public static bool operator ==(TcpClient left, TcpClient right)
        {
            if ((object)left == null) return (object)right == null;
            if ((object)right == null) return (object)left == null;
            return left.IP == right.IP && left.Port == right.Port;
        }

        public static bool operator !=(TcpClient left, TcpClient right)
        {
            return !(left == right);
        }

        public override int GetHashCode()
        {
            return _serverIp.GetHashCode() + _serverPort.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is TcpClient)) return false;
            return this == obj as TcpClient;
        }

    }

    public class UserTokenObject
    {
        public static int BufferSize = 4096;
        public UserTokenObject()
        {
            this.Buffer = new byte[BufferSize];
        }

        public byte[] Buffer { get; private set; }
        public Socket WorkSocket { get; set; }
        public string AssociatedID = string.Empty;
    }
}
