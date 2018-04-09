using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VideoRecorder;

namespace NVSGVideoRecordServer
{
    class Program
    {
        static VLCRTSPController vrcl;
        static bool isVLCExit;
        static bool isRecordExit;
        static int CurrentMaxPort;
        static string CurrentIpAddressStr;
        static TcpServer KunGeserver;
        static string Splitetag = "^";
        static void Main(string[] args)
        {
            KunGeserver = new TcpServer(20, 4096);
            KunGeserver.DataReceived += server_DataReceived;
            KunGeserver.Open(4002);


            isVLCExit = false;
            CurrentIpAddressStr = "";
            CurrentMaxPort = 0;
            isRecordExit = false;

            //   Console.WriteLine(" 本机 IP 是 " + GetClientLocalIPv4Address());

            
            Console.WriteLine("本服务ip "  + GetClientLocalIPv4Address());
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            // GameExitCheck();
            

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        static void server_DataReceived(object sender, DataReceivedEventArgs e)
        {

            // There  might be more data, so store the data received so far.
            string content = System.Text.Encoding.UTF8.GetString(e.Data, 0, e.RealDataSize);
            List<string> ResultList = AnalyzeDateFromClient(content);

            foreach (string str in ResultList)
            {
                MessageDealCenter(str);
            }

        }

        /// <summary>
        /// 分析客户端发来的数据里面有几条
        /// 并根据 <EOF> 作为判断依据，返回结果集
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static List<string> AnalyzeDateFromClient(string str)
        {
            List<string> resList = new List<string>();

            int EOFIndex = str.IndexOf("<EOF>");

            if (EOFIndex <= -1)
            {
                //-1 说明没有EOF结尾
                return resList;
            }

            //int EOFCount = str.ToCharArray().Count(x => x == '<EOF>');
            int EOFCount = Regex.Matches(str, @"<EOF>").Count;

            for (int i = 0; i < EOFCount; i++)
            {
                int index_EOF = str.IndexOf("<EOF>");
                string FirstConnect = str.Substring(0, index_EOF);
                resList.Add(FirstConnect);

                str = str.Substring(index_EOF + 5, str.Length - index_EOF - 5);
            }


            return resList;
        }

        #region 消息处理中心

        /// <summary>
        /// 消息处理中心
        /// 只处理单条的数据
        /// </summary>
        /// <param name="content"></param>
        static void MessageDealCenter(string content)
        {
            

            // All the data has been read from the 
            // client. Display it on the console.
            Console.WriteLine("\r\n\r\n=========================================================================");
            Console.WriteLine("Time : {0} , Read {1} bytes from socket. \n Data : {2}",DateTime.Now.ToLongTimeString(), content.Length, content);
            Console.WriteLine("=========================================================================\r\n\r\n");

            string message = content.Replace("<EOF>", "");
            string[] splits = message.Split(Splitetag.ToCharArray());
            if (splits[0] == "CreatePlayer")
            {
                DealWith_CreatePlayer(splits);
            }
            else if (splits[0] == "UpdatePersons") //最好成 SynchroClients
            {
                DeleaWith_UpdatePersons(splits);
            }
            else if (splits[0] == "UpdateObjPositions") //实时更新物体的位置
            {
                DealWith_UpdateObjPositions(splits);
            }
            else if (splits[0] == "CreateBullet")
            {
                DealWith_CreateBullet(splits);
            }
            else if (splits[0] == "stopPlayer") //某个客户端退出
            {
                DealWith_stopPlayer(splits);
            }
            else if (splits[0] == "Command")
            {
                if (splits[1] == "stop")
                {
                    GameExitCheck();
                }
                else if (splits[1] == "StartVideoSteaming")
                {

                }
                else if (splits[1] == "PauseSteaming")
                {

                }
                else if (splits[1] == "GameExit")
                {
                    //程序退出时候调用的释放
                    GameExitCheck();
                }
            }

        }

        /// <summary>
        /// CreatePlayer 消息处理
        /// </summary>
        /// <param name="splits"></param>
        static void DealWith_CreatePlayer(string[] splits)
        {
            string res = getStringbyInital(splits);
            SendDataToAllClients(res);
        }

        /// <summary>
        /// UpdatePersons
        /// </summary>
        /// <param name="splits"></param>
        static void DeleaWith_UpdatePersons(string[] splits)
        {
            string res = getStringbyInital(splits);

            BoardCastToAllClients(res);

        }

        /// <summary>
        /// UpdateObjPositions
        /// </summary>
        /// <param name="splits"></param>
        static void DealWith_UpdateObjPositions(string[] splits)
        {
            string res = getStringbyInital(splits);
            BoardCastToAllClients(res);
        }

        /// <summary>
        /// CreateBullet
        /// </summary>
        /// <param name="splits"></param>
        static void DealWith_CreateBullet(string[] splits)
        {
            string res = getStringbyInital(splits);
            BoardCastToAllClients(res);
        }

        /// <summary>
        /// stopPlayer
        /// </summary>
        /// <param name="splits"></param>
        static void DealWith_stopPlayer(string[] splits)
        {
            string res = getStringbyInital(splits);
            BoardCastToAllClients(res);
        }

        /// <summary>
        /// 将字符串按照之前的方式重新拼接
        /// </summary>
        /// <param name="splits"></param>
        /// <returns></returns>
        static string getStringbyInital(string[] splits)
        {
            string res = "";
            foreach (string s in splits)
            {
                res += s + Splitetag;
            }

            res = res.Substring(0, res.Length - 1);
            return res;
        }

        #endregion

        #region 消息分发中心

        /// <summary>
        /// 将消息发送给所有的客户端，进行新建玩家
        /// </summary>
        /// <param name="str"></param>
        static void SendDataToAllClients(string str)
        {
            BoardCastToAllClients(str);

            //新建完让第一个链接的人同步
            string updateStr = "Command" + Splitetag + "UpdateAllClients<EOF>";
            byte[] updateByte = Encoding.UTF8.GetBytes(updateStr);
            KunGeserver.Connections[0].Client.Send(updateByte);

        }

        /// <summary>
        ///  将消息广播给所有的客户端
        /// </summary>
        /// <param name="str"></param>
        static void BoardCastToAllClients(string str)
        {
            str += "<EOF>";
            byte[] data = Encoding.UTF8.GetBytes(str);

            foreach (TcpConnection tc in KunGeserver.Connections)
            {
                tc.Client.Send(data);
            }
        }

        #endregion

        #region Local function

        static void GameExitCheck()
        {
            if (!isVLCExit)
            {
                vrcl.ApplicationExitFuntion();
                isVLCExit = true;
                Console.WriteLine(" --------------------------- VLC Game Exit ----------------");
            }

            if (!isRecordExit)
            {
                GeneralVideoSDK.Mp4Recording_Release();
                isRecordExit = true;
                Console.WriteLine("======================================= Record server get Command : stop ===================");
            }
            Console.WriteLine(" ================  Everything is exit ====================");
        }

        static void ShowConnectState(int k)
        {
            if (k == 0)
            {
                Console.WriteLine("录像服务初始化成功");
            }
            else if (k == -1)
            {
                Console.WriteLine("录像服务初始化失败");
            }
            else
            {
                Console.WriteLine("初始化录像服务发生未知错误");
            }

        }

        /// <summary>  
        /// 获取客户端内网IPv4地址  
        /// </summary>  
        /// <returns>客户端内网IPv4地址</returns>  
        static string GetClientLocalIPv4Address()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList.FirstOrDefault<IPAddress>(a => a.AddressFamily.ToString().Equals("InterNetwork")).ToString();
        }



        #endregion



        #region TestModel

        static void ShowAllClients()
        {
            string str = "";
            foreach (TcpConnection tc in KunGeserver.Connections)
            {
                str += "\r\n IP : " + tc.Client.RemoteEndPoint + " --- ";
                tc.Client.Send(Encoding.UTF8.GetBytes(" Hello IP " + tc.Client.RemoteEndPoint));
            }
            Console.WriteLine(str);
        }



        #endregion

    }
}
