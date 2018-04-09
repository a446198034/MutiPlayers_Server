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
    /// TCP通信服务器类
    /// </summary>
    public class TcpServer
    {
        Dictionary<int, TcpConnection> _connectionsDic;

        int _listenPort;
        private Socket _listenSocket;
        private Semaphore _maxSemaphore;
        private int _connectionCount = 0;
        bool _wantExit = false;

        public TcpServer(int maxConnectionsCount, int bufferSize)
        {
            UserTokenObject.BufferSize = bufferSize;
            _maxSemaphore = new Semaphore(maxConnectionsCount, maxConnectionsCount);
            _connectionsDic = new Dictionary<int, TcpConnection>(maxConnectionsCount);
        }


        public int ConnectionsCount
        {
            get { return _connectionCount; }
        }


        public TcpConnection this[int associatedID]
        {
            get
            {
                TcpConnection connection = null;
                lock (_connectionsDic)
                {
                    if (_connectionsDic.ContainsKey(associatedID))
                    {
                        connection = _connectionsDic[associatedID];
                    }
                }
                return connection;
            }
            set
            {
                lock (_connectionsDic)
                {
                    if (_connectionsDic.ContainsKey(associatedID))
                    {
                        _connectionsDic[associatedID] = value;
                    }
                    else
                    {
                        _connectionsDic.Add(associatedID, value);
                    }
                }
            }
        }

        private void StartAccept()
        {
            if (!_wantExit)
            {
                _maxSemaphore.WaitOne();
                try
                {
                    _listenSocket.BeginAccept(new AsyncCallback(AcceptCallback), _listenSocket);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(" ssss " + ex);
                }
            }
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                Socket listener = (Socket)ar.AsyncState;
                Socket handler = listener.EndAccept(ar);
                Interlocked.Increment(ref _connectionCount);
                //uint dummy = 0;
                //byte[] inOptionValues = new byte[Marshal.SizeOf(dummy) * 3];
                //BitConverter.GetBytes((uint)1).CopyTo(inOptionValues, 0);
                //BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy));
                //BitConverter.GetBytes((uint)5000).CopyTo(inOptionValues, Marshal.SizeOf(dummy) * 2);
                //handler.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);

                Console.WriteLine("client {0} is connected.", handler.RemoteEndPoint.ToString());

                TcpConnection connection = new TcpConnection(handler);
                connection.DisConnected += new EventHandler(connection_DisConnected);
                connection.DataReceived += new EventHandler<DataReceivedEventArgs>(connection_DataReceived);
                lock (_connectionsDic)
                {
                    if (_connectionsDic.ContainsKey(connection.AssociatedID))
                    {
                        _connectionsDic[connection.AssociatedID] = connection;
                    }
                    else
                    {
                        _connectionsDic.Add(connection.AssociatedID, connection);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            StartAccept();
        }

        public event EventHandler<DataReceivedEventArgs> DataReceived;
        public void Open(int port)
        {
            try
            {
                _wantExit = false;
                _listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _listenPort = port;
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);
                _listenSocket.Bind(localEP);
                _listenSocket.Listen(100);
                Console.WriteLine("server is listening...");
                StartAccept();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        void connection_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived(sender, e);
            }
        }

        void connection_DisConnected(object sender, EventArgs e)
        {
            try
            {
                if (DisConnected != null)
                {
                    DisConnected(sender, e);
                }
                TcpConnection connection = (TcpConnection)sender;
                lock (_connectionsDic)
                {
                    if (_connectionsDic.ContainsKey(connection.AssociatedID))
                    {
                        _connectionsDic.Remove(connection.AssociatedID);
                    }
                }
                Interlocked.Decrement(ref _connectionCount);
                _maxSemaphore.Release();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }



        public void Stop()
        {
            try
            {
                _wantExit = true;
                if (_listenSocket != null)
                {
                    try
                    {
                        if (_listenSocket.Connected)
                            _listenSocket.Shutdown(SocketShutdown.Both);
                    }
                    catch
                    { }
                    _listenSocket.Close();
                    _listenSocket = null;
                }
                lock (_connectionsDic)
                {
                    _connectionsDic.Clear();
                }
                _connectionsDic = null;
                _maxSemaphore.Close();

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        public List<TcpConnection> Connections
        {
            get
            {
                TcpConnection[] connections = null;
                lock (_connectionsDic)
                {
                    connections = new TcpConnection[_connectionsDic.Count];
                    _connectionsDic.Values.CopyTo(connections, 0);
                }
                if (connections != null)
                {
                    return new List<TcpConnection>(connections);
                }
                else
                {
                    return null;
                }
            }
        }
        public event EventHandler DisConnected;
    }
}
