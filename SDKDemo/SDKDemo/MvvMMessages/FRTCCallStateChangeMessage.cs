using SDKDemo;

namespace SDKDemo.MvvMMessages
{
    public class FRTCCallStateChangeMessage
    {
        public FrtcCallState callState { get; set; }
        public FrtcCallReason reason { get; set; }
        public string meetingId { get; set; }
        public string meetingName { get; set; }
    }
}
