using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;       

namespace SQMeeting.MvvMMessages
{
    public class MeetingControlOperationResultMessage
    {
        public HttpStatusCode StatusCode { get; set; }
        public string APIName { get; set; }
        public string ErrorCode { get; set; }

        public object ResultParam { get; set; }

        public MeetingControlOperationResultMessage()
        {

        }

        public MeetingControlOperationResultMessage(HttpStatusCode StatusCode, string ErrorCode, string APIName, object ResultParam = null)
        {
            this.StatusCode = StatusCode;
            this.APIName = APIName;
            this.ErrorCode = ErrorCode;
            this.ResultParam = ResultParam;

        }
    }
}
