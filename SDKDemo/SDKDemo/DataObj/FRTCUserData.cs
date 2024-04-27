using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SDKDemo.Model.DataObj
{
    public class FRTCUserData
    {
        public string user_token { get; set; }
        public string user_id { get; set; }
        public string username { get; set; }
        public string email { get; set; }

        [DataMember(IsRequired = false)]
        public string firstname { get; set; }

        [DataMember(IsRequired = false)]
        public string lastname { get; set; }

        [DataMember(IsRequired = false)]
        public string real_name { get; set; }

        public string[] role { get; set; }

        public string security_level { get; set; }

        [DataMember(IsRequired = false)]
        public string department { get; set; }

        [DataMember(IsRequired = false)]
        public string mobile { get; set; }

        [DataMember(IsRequired = false)]
        public string errorCode { get; set; }


    }

    public class UserMeetingRoomList
    {
        public int total_page_num { get; set; }
        public int total_size { get; set; }
        public MeetingRoomData[] meeting_rooms { get; set; }
    }

    public class MeetingRoomData
    {
        public string meeting_room_id { get; set; }
        public string meeting_number { get; set; }
        public string meetingroom_name { get; set; }
        public string meeting_password { get; set; }
        public string owner_id { get; set; }
        public string owner_name { get; set; }
        public string creator_id { get; set; }
        public string creator_name { get; set; }
        public string created_time { get; set; }
    }

    public class UserMeetingHistory
    {
        public MeetingHistoryData[] frtc_meeting_history { get; set; }
    }

    public class MeetingHistoryData : IComparer<MeetingHistoryData>
    {
        public string user_id { get; set; }
        public string meeting_number { get; set; }
        public string display_name { get; set; }
        public string meeting_pwd { get; set; }
        public string meeting_name { get; set; }
        public string begin_time { get; set; }
        public string end_time { get; set; }
        public string owner_id { get; set; }
        public string owner_name { get; set; }
        public string uuid { get; set; }

        public int Compare(MeetingHistoryData x, MeetingHistoryData y)
        {
            if (x.uuid == y.uuid)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
    }

    public class UserInfo
    {
        public string user_id { get; set; }
        public string username { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string real_name { get; set; }
    }

    public class UserInfoList
    {
        public UserInfo[] users { get; set; }
        public int total_page_num { get; set; }
        public int total_size { get; set; }
    }

}