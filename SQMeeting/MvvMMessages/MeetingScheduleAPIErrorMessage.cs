using SQMeeting.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class MeetingScheduleAPIErrorMessage
    {
        public MeetingScheduleStatusCode StatusCode { get; set; }

        public MeetingScheduleAPIErrorMessage() { }

        public MeetingScheduleAPIErrorMessage(MeetingScheduleStatusCode StatusCode)
        {
            this.StatusCode = StatusCode;
        }
    }
}
