using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQMeeting.Model;

namespace SQMeeting.MvvMMessages
{
    public class FRTCCallStateChangeMessage
    {
        public FrtcCallState callState { get; set; }
        public FrtcCallReason reason { get; set; }
        public string meetingId { get; set; }
        public string meetingName { get; set; }
    }
}
