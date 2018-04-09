using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVSGVideoRecordServer
{
    public class VLCSteamer
    {
        public IntPtr VLCSteamHandle;

        public string Media_Name;

        public string Media_URL;

        public string Media_Sout;

        public VLCSteamer()
        {
            VLCSteamHandle = IntPtr.Zero;
            Media_Name = "";
            Media_URL = "";
            Media_Sout = "";
        }

    }
}
