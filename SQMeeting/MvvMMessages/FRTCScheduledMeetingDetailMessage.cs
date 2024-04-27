using SQMeeting.Model.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class FRTCScheduledMeetingDetailMessage
    {
        public FRTCScheduledMeetingDetailMessage()
        {

        }

        public FRTCScheduledMeetingDetailMessage(MeetingScheduleResult data)
        {
            MeetingData = data;
        }

        public MeetingScheduleResult MeetingData { get; set; }
    }
}
