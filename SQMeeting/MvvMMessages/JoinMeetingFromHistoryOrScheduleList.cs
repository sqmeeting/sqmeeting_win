using SQMeeting.Model.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class JoinMeetingFromHistoryOrScheduleList
    {
        public MeetingScheduleResult MeetingData { get; set; }

        public JoinMeetingFromHistoryOrScheduleList()
        {
            MeetingData = new MeetingScheduleResult();
        }

        public JoinMeetingFromHistoryOrScheduleList(MeetingScheduleResult Data)
        {
            MeetingData = Data;
        }
    }
}
