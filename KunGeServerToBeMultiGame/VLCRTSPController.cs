using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NVSGVideoRecordServer
{
    class VLCRTSPController
    {
        #region 私有属性

        const string DllFIleName_X32 = @"\Plugins\VLCRtspDLL\VlcApiLib.dll";//dll路径

        List<VLCSteamer> VLCSteamList;
        int vlcPort;
        #endregion


        public VLCRTSPController()
        {
            vlcPort = 554;
            VLCSteamList = new List<VLCSteamer>();
            Console.WriteLine("VLCSteaming : VLC 推流服务开启，端口从 " + vlcPort + "开始");
        }


        #region 公有方法

        /// <summary>
        /// 初始化VLC实例
        /// </summary>
        /// <param name="SteamVideoPath"></param>
        public void initVLCMediaPlayer(List<string> SteamVideoPath)
        {

            for (int i = 0; i < SteamVideoPath.Count; i++)
            {
                CreateVLCInstance(SteamVideoPath[i],i);
                vlcPort++;
            }

        }

        /// <summary>
        /// 开始推流
        /// </summary>
        public void SteamVideo()
        {
            if (VLCSteamList.Count == 0)
            {
                Console.WriteLine("VLCSteaming : VlcSteamList count is zero");
            }
            else
            {
                Console.WriteLine("VLCSteaming :  Start Steaming");
                for (int i = 0; i < VLCSteamList.Count; i++)
                {
                    VLCSteamer vs = VLCSteamList[i];
                    int res = VLC_PlayMedia(vs.VLCSteamHandle,vs.Media_Name);
                    Console.WriteLine("VLCSteaming :  Media PLay Result " + getFinalResStr(res));
                }
            }
        }

        /// <summary>
        /// 暂停推流
        /// </summary>
        public void PauseVideo()
        {
            for (int i = 0; i < VLCSteamList.Count; i++)
            {
                VLCSteamer vs = VLCSteamList[i];
                int res = VLC_PauseMedia(vs.VLCSteamHandle,vs.Media_Name);
                Console.WriteLine("VLCSteaming :  Pause Video " + getFinalResStr(res) + " Handdle : " + vs.VLCSteamHandle);
            }

        }

        /// <summary>
        /// 程序退出时调用的VLC实例释放的方法
        /// </summary>
        public void ApplicationExitFuntion()
        {
            for (int i = 0; i < VLCSteamList.Count; i++)
            {
                VLCSteamer vs = VLCSteamList[i];
                VLC_StopMedia(vs.VLCSteamHandle, vs.Media_Name);
                VLC_Release(vs.VLCSteamHandle);
            }
        }

        #endregion


        #region 本地方法

        void CreateVLCInstance(string Videopath, int index)
        {
            VLCSteamer vs = new VLCSteamer();
            IntPtr vlcHandle = VLC_Init();
            vs.VLCSteamHandle = vlcHandle;
            string ChanelHao = getNVRChanel();
            vs.Media_Sout = "#rtp{sdp=rtsp://admin:a1234567@:" + vlcPort + "/h264/ch" + ChanelHao + "/main/av_stream} --sout-keep";
            vs.Media_Name = "VLCMediaName" + index.ToString();
            vs.Media_URL = Videopath;

            VLCSteamList.Add(vs);

            int Boardres = VLC_Add_Broadcast(vs.VLCSteamHandle,vs.Media_Name,vs.Media_URL,vs.Media_Sout);
           

          //  int PlayRes = VLC_PlayMedia(vs.VLCSteamHandle,vs.Media_Name);
            Console.WriteLine("VLCSteaming :   Port :" + vlcPort + " Handle : " + vs.VLCSteamHandle +  " Add BoardRess Result : " + getFinalResStr(Boardres));
        }

        string getFinalResStr(int res)
        {
            return res == -1? "失败" : "成功";
        }

        string getNVRChanel()
        {
            int Chanel = vlcPort - 554 + 33;
            string res = Chanel < 10 ? "0" + Chanel.ToString() : Chanel.ToString();
            return res;
        }

        #endregion


        #region VLC Control

        public IntPtr VLC_Init()
        {
            return LibVlc_Init();
        }

        public int VLC_Add_Broadcast(IntPtr vlcIPtr, string m_media_name, string m_media_url, string sout)
        {
            int res = 0;
            try
            {
                res = LibVlc_Add_Broadcast(vlcIPtr, System.Text.Encoding.UTF8.GetBytes(m_media_name),
                System.Text.Encoding.UTF8.GetBytes(m_media_url),
                System.Text.Encoding.UTF8.GetBytes(sout));
            }
            catch (Exception ex)
            {
                Console.WriteLine("VLCSteaming :  AddBroad Error " + ex); 
            }
            return res;
        }

        public int VLC_PlayMedia(IntPtr vlcPtr, string m_mediaName)
        {
            int res = 0;
            try
            {
                res = LibVlc_Play_Media(vlcPtr, System.Text.Encoding.UTF8.GetBytes(m_mediaName));
            }
            catch (Exception ex)
            {
                Console.WriteLine("VLCSteaming :  Vlc_play Exception " + ex);
            }
            return res;
        }

        public int VLC_PauseMedia(IntPtr vlcPtr, string m_mediaName)
        {
            int res = 0;
            try
            {
                res = LibVlc_Pause_Media(vlcPtr, System.Text.Encoding.UTF8.GetBytes(m_mediaName));
            }
            catch (Exception ex)
            {
                Console.WriteLine("VLCSteaming :  Vlc_Pause Exception " + ex);
            }
            return res;
        }

        public void VLC_StopMedia(IntPtr vlcPtr, string m_mediaName)
        {
            try
            {
                LibVlc_Stop_Media(vlcPtr, System.Text.Encoding.UTF8.GetBytes(m_mediaName));
            }
            catch (Exception ex)
            {
                Console.WriteLine("VLCSteaming :  VLC_Stop Exception " + ex);

            }
        }

        public void VLC_Release(IntPtr vlcPtr)
        {
            try
            {
                LibVlc_Release(vlcPtr);
            }
            catch (Exception ex)
            {
                Console.WriteLine("VLCSteaming :  VLC_release Exception " + ex);
            }
        }


        #endregion


        #region Dll Import

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Init", CallingConvention = CallingConvention.Cdecl)]
        static extern IntPtr LibVlc_Init();

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Add_Broadcast", CallingConvention = CallingConvention.Cdecl)]
        static extern int LibVlc_Add_Broadcast(IntPtr pVlcHanlde,
                                                     [MarshalAs(UnmanagedType.LPArray)] byte[] pMediaName,
                                                     [MarshalAs(UnmanagedType.LPArray)] byte[] pUrlPath,
                                                     [MarshalAs(UnmanagedType.LPArray)] byte[] pCmdSout);

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Play_Media", CallingConvention = CallingConvention.Cdecl)]
        static extern int LibVlc_Play_Media(IntPtr ppp, [MarshalAs(UnmanagedType.LPArray)] byte[] pMediaName);

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Pause_Media", CallingConvention = CallingConvention.Cdecl)]
        static extern int LibVlc_Pause_Media(IntPtr ppp, [MarshalAs(UnmanagedType.LPArray)] byte[] pMediaName);

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Release", CallingConvention = CallingConvention.Cdecl)]
        static extern int LibVlc_Release(IntPtr tppp);

        [DllImport(DllFIleName_X32, EntryPoint = "LibVlc_Stop_Media", CallingConvention = CallingConvention.Cdecl)]
        static extern void LibVlc_Stop_Media(IntPtr ppp, [MarshalAs(UnmanagedType.LPArray)] byte[] pMediaName);


        #endregion

    }
}
