using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQMeeting.ViewModel;

namespace SQMeeting.MvvMMessages
{
    public class FRTCViewNavigatorMessage
    {
        public FrtcMeetingViews SourceView;
        public FrtcMeetingViews TargetView;
        public object Param;
    }
}
