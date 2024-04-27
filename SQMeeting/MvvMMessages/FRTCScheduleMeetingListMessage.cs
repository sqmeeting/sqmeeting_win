using SQMeeting.Model.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class FRTCScheduleMeetingListMessage
    {
        public ScheduledMeetingList MeetingListObj { get; set; }

        public FRTCScheduleMeetingListMessage()
        {
            MeetingListObj = new ScheduledMeetingList();
        }

        public FRTCScheduleMeetingListMessage(ScheduledMeetingList MeetingList)
        {
            MeetingListObj = MeetingList;
        }
    }

    public class FRTCRecurringMeetingGroupMessage
    {
        public RecurringMeetingGroup RecurringMeetingGroup { get; set; }

        public FRTCRecurringMeetingGroupMessage()
        {
            RecurringMeetingGroup = new RecurringMeetingGroup();
        }

        public FRTCRecurringMeetingGroupMessage(RecurringMeetingGroup recurringMeetingGroup)
        {
            RecurringMeetingGroup = recurringMeetingGroup;
        }
    }
}
