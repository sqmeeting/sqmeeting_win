using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class MediaDeviceResetMessage
    {
        public string DeviceType { get; set; }
        public string DeviceId { get; set; }
        public MediaDeviceResetMessage(string type, string id)
        {
            DeviceType = type;
            DeviceId = id;
        }
    }
}
