using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.Model.DataObj
{
    public class MeetingScheduleInfo
    {
        //instant/reservation
        public string meeting_type { get; set; }
        public string meeting_name { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_description { get; set; }
        //utc time stamp
        public string schedule_start_time { get; set; }
        public string schedule_end_time { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_room_id { get; set; }

        //128K/512K/1024K/2048K/3072K/4096K/6144K/8192K, default 4096K.
        [DataMember(IsRequired = false)]
        public string call_rate_type { get; set; }

        // -1 means any time
        [DataMember(IsRequired = false)]
        public int time_to_join { get; set; }

        //"meeting_password" : null     :: random password
        //"meeting_password" : ""       :: no password
        //"meeting_password" : "123456" :: specify a 6 numbers password
        [DataMember(IsRequired = false)]
        public string meeting_password { get; set; }

        [DataMember(IsRequired = false)]
        public string[] invited_users { get; set; }

        // DISABLE/ENABLE
        [DataMember(IsRequired = false)]
        public string mute_upon_entry { get; set; }

        [DataMember(IsRequired = false)]
        public bool guest_dial_in { get; set; }

        [DataMember(IsRequired = false)]
        public bool watermark { get; set; }

        [DataMember(IsRequired = false)]
        public string watermark_type { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_url { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrence_type { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceInterval { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceStartTime { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceEndTime { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceStartDay { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceEndDay { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfWeek { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfMonth { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceId { get; set; }
    }
    public class ScheduledMeetingUserDetail
    {
        public string user_id { get; set; }
        public string username { get; set; }
    }

    public class MeetingScheduleResult
    {
        //instant/reservation/recurrence
        public string meeting_type { get; set; }
        public string meeting_name { get; set; }
        public string meeting_number { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_description { get; set; }
        //utc time stamp
        public string schedule_start_time { get; set; }
        public string schedule_end_time { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_room_id { get; set; }

        //128K/512K/1024K/2048K/3072K/4096K/6144K/8192K, default 4096K.
        [DataMember(IsRequired = false)]
        public string call_rate_type { get; set; }

        //-1 means anytime
        [DataMember(IsRequired = false)]
        public int time_to_join { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_password { get; set; }

        [DataMember(IsRequired = false)]
        public ScheduledMeetingUserDetail[] invited_users_details { get; set; }

        // DISABLE/ENABLE
        [DataMember(IsRequired = false)]
        public string mute_upon_entry { get; set; }

        [DataMember(IsRequired = false)]
        public bool guest_dial_in { get; set; }

        [DataMember(IsRequired = false)]
        public bool watermark { get; set; }

        [DataMember(IsRequired = false)]
        public string watermark_type { get; set; }

        [DataMember(IsRequired = false)]
        public string owner_id { get; set; }

        [DataMember(IsRequired = false)]
        public string owner_name { get; set; }

        [DataMember(IsRequired = true)]
        public string reservation_id { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrence_gid { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrence_type { get; set; }

        [DataMember(IsRequired = false)]
        public int recurrenceInterval { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfWeek { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfMonth { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceStartTime { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceEndTime { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceStartDay { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceEndDay { get; set; }

        [DataMember(IsRequired =false)]
        public MeetingScheduleResult[] recurrenceReservationList { get; set; }

        [DataMember(IsRequired = false)]
        public string[] participantUsers { get; set; }

        [DataMember(IsRequired = false)]
        public string meetingInfoKey { get; set; }

        [DataMember(IsRequired = false)]
        public string groupInfoKey { get; set; }

        [DataMember(IsRequired = false)]
        public string qrcode_string { get; set; }

        [DataMember(IsRequired = false)]
        public string meeting_url { get; set; }

        [DataMember(IsRequired = false)]
        public string groupMeetingUrl { get; set; }
    }

    public class ScheduledMeetingList
    {
        public int total_page_num { get; set; }
        public int total_size { get; set; }
        public MeetingScheduleResult[] meeting_schedules { get; set; }
    }

    public class RecurringMeetingGroup
    {
        [DataMember(IsRequired = false)]
        public int total_page_num { get; set; }

        [DataMember(IsRequired = false)]
        public int total_size { get; set; }

        [DataMember(IsRequired = false)]
        public string recurrenceType { get; set; }

        [DataMember(IsRequired = false)]
        public int recurrenceInterval { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfWeek { get; set; }

        [DataMember(IsRequired = false)]
        public int[] recurrenceDaysOfMonth { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceStartTime { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceEndTime { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceStartDay { get; set; }

        [DataMember(IsRequired = false)]
        public long? recurrenceEndDay { get; set; }
        public MeetingScheduleResult[] meeting_schedules { get; set; }
    }
}
