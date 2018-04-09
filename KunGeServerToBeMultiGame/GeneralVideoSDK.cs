using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NVSGVideoRecordServer
{
    class GeneralVideoSDK
    {

        #region 私有属性 属性定义
        private string DllFIleName_X32 = @"\Plugins\x86\Mp4RecordingSDK.dll";//32 bit dll路径
        private string DllFileName_X64 = @"\Plugins\x86_64\Mp4RecordingSDK.dll";//64 bit dll路径

        /// <summary>
        /// Current platom is 32
        /// </summary>
        private const string DllFIleName = @"\Plugins\x86\Mp4RecordingSDK.dll";//dll路径
        private const string dllTest = @"\Plugins\TestAguiDLL.dll";//dll路径

        /// <summary>
        /// 视频录制存放的地址，一定要开共享
        /// </summary>
        private static string m_czRecordPath = "D:/WorkPlace/UnityWorkPlace/TableTopToolVedioReviewProject/VideoServer/Video/";


        #endregion


        #region 构造方法 GeneralVideoSDK
        /// <summary>
        /// 构造方法
        /// </summary>
        public GeneralVideoSDK()
        {
            //待实现.
        }
        #endregion



        #region 公有属性

        const int MAX_FILE_DIR_NAME_LENTH = 512;

        //设备厂商类型
        public enum RECORD_FACTURE_TYPE
        {
            HK_FACTURE_CAMERA,      //海康厂商的摄像机
            HK_FACTURE_NVR,         //海康厂商的NVR
            DH_FACTURE_CAMERA,      //大华厂商的摄像机
            DH_FACTURE_NVR,         //大华厂商的NVR
            XW_FACTURE_CAMERA,      //雄迈厂商的摄像机
            UNKNOW_FACTURE          //未知厂商.
        }

        //设备信息参数
        public struct MP4_RECORD_DATA
        {
            public RECORD_FACTURE_TYPE emDeviceType;   //设备类型.
            public uint uiSocketType;          //网络类型,UDP方式,0：TCP方式1
            public uint uiMediaType;           //录制流类型, 0-视频，1-音频， 2-音视频全有
            public uint uiRecordSTime;         //设置每个录像文件的时长为秒数， 值范围为10-60分钟之内,超出才系统设定默认值 MAX_RECORD_DEFAULT_TIME
            public uint uiMainOrSubType;       //码流类型，0-主码流 1-子码流 2-第三码流以此类推.
            public uint uiPortNumber;          //RTSP访问的端口号,默认是554
            public uint uiNVRChannel;          //如果是NRV类型需要摄像机的通道号
            public uint uiRecordOldPlan;       //是否启用上一次录像计划0为不启用，1为启用(如果该设备上一次启用过计划)
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 10)]
            public string czEncodingType;            //码流编码类型一般情况下是h264、h265、MPEG-4、mpeg4这几种
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string czCamIPAddress;       //Cam摄像机对应IP地址  目前只支持IPV4 当NVR取流时些值可以为'\0',存储路径按通道号来存存储
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 16)]
            public string czNvrIPAddress;       //Nvr录像机对应IP地址  目前只支持IPV4		 
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_FILE_DIR_NAME_LENTH)]
            public string czRecordPath;     //录像文件存储路径，最好是绝对路径.
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szUserName;                    //摄像机用户名
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string szPassword;                    //摄像机密码
        }



        #endregion

        #region Public Functuion

        /// <summary>
        /// 开启录像服务
        /// 在推流的时候
        /// </summary>
        /// <param name="IPAddStr"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static void StartRecordServer(string IPAddStr, int port)
        {
            int MYComputerIP = 90;
            for (int i = 554; i < port; i++)
            {

                MP4_RECORD_DATA mm = getNormalMP4_RECORD_DATA();
                mm.czRecordPath = m_czRecordPath;
                mm.czCamIPAddress = getCamIPAddress(MYComputerIP);
                mm.czNvrIPAddress = IPAddStr;
                mm.uiNVRChannel = (uint)(i - 554 + 1);
                string ppo = i.ToString();
                mm.uiPortNumber = uint.Parse(ppo);

                int res = Mp4Recording_Open(ref mm);
                Console.WriteLine("                 Port = " + i + " StartRecoedServer Result →→→→→→→→→→→→→→" + getFinalResStr(res));

                MYComputerIP ++;
            }

        }

        static string getFinalResStr(int res)
        {
            return res == -1 ? "Result = " + res + " Faild" :"Result = " + res + "  Success";
        }

        static string getCamIPAddress(int IpAdd)
        {
            string res = "192.168.1.";
            return res + IpAdd;
        }

       

        #endregion

        #region GeneralVideoSDK库倒出方法.

        /// <summary>
        /// 初始化
        /// </summary>
        /// <returns></returns>
        [DllImport(DllFIleName, EntryPoint = "Mp4Recording_Init", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Mp4Recording_Init();
        [DllImport(dllTest, EntryPoint = "add", CallingConvention = CallingConvention.Cdecl)]
        public static extern int add(int x,int y);

        /// <summary>
        /// 释放命令
        /// 在程序退出的时候调用
        /// </summary>
        [DllImport(DllFIleName, EntryPoint = "Mp4Recording_Release", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Mp4Recording_Release();

        /// <summary>
        /// 开始录像
        /// 在推流的时候
        /// </summary>
        /// <param name="mp4R"></param>
        /// <returns></returns>
        [DllImport(DllFIleName, EntryPoint = "Mp4Recording_Open", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Mp4Recording_Open(ref MP4_RECORD_DATA mp4R);

        /// <summary>
        /// 通过通道号关闭
        /// </summary>
        /// <param name="iChannelId"></param>
        [DllImport(DllFIleName, EntryPoint = "Mp4Recording_Close", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Mp4Recording_Close(int iChannelId);


        #endregion

        #region MP4_RECORD_DATA Demo

        static MP4_RECORD_DATA getNormalMP4_RECORD_DATA()
        {
            MP4_RECORD_DATA mm = new MP4_RECORD_DATA();
            mm.emDeviceType = RECORD_FACTURE_TYPE.HK_FACTURE_NVR;
            mm.uiSocketType = 0;
            mm.uiMediaType = 0;
            mm.uiRecordSTime = 1800;
            mm.uiMainOrSubType = 0;
            mm.uiPortNumber = 554;
            mm.uiNVRChannel = 0;
            mm.uiRecordOldPlan = 0;
            mm.czEncodingType = "h264";
            mm.czCamIPAddress = "192.168.1.151";
            mm.czNvrIPAddress = "192.168.1.151";
            mm.czRecordPath = "F:/NVSGClicnt/VideoServer/";
            mm.szUserName = "admin";
            mm.szPassword = "a1234567";
            return mm;
        }


        #endregion

    }
}
