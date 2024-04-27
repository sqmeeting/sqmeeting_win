using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class JoinFRTCMeetingMsg
    {
        public string confAlias { get; set; }
        public string displayName { get; set; }
        public int callRate { get; set; }
        public bool preMuteMic { get; set; }
        public bool preMuteCamera { get; set; }
        public string userToken { get; set; }
        public string passCode { get; set; }
        public IntPtr meetingVideoWndHWND { get; set; }
        public bool isSelfOwnedMeeting { get; set; }

        public bool isVoiceOnlyMeeting { get; set; }

        public bool isPlainTextURLJoin { get; set; }
        public string serverAddress { get; set; }
    }
}
