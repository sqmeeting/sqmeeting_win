using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.MvvMMessages
{
    public class FRTCEditMeetingSuccessMsg
    {
        public string ReservationId { get; set; }

        public string OperationType { get; set; }

        public FRTCEditMeetingSuccessMsg(string operation_type)
        {
            OperationType = operation_type;
        }

        public FRTCEditMeetingSuccessMsg(string operation_type, string reservation_Id)
        {
            OperationType = operation_type;
            ReservationId = reservation_Id;
        }
    }
}
