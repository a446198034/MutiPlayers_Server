using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoRecorder
{
    /// <summary>
    /// 数据接收完成事件参数
    /// </summary>
    public class DataReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// 接收到的数据字节数
        /// </summary>
        public int RealDataSize { get; set; }
        /// <summary>
        /// 接收到的数据
        /// </summary>
        public byte[] Data { get; set; }

        public UserTokenObject UserToken { get; set; }

        public DataReceivedEventArgs()
        {

        }
    }
}
