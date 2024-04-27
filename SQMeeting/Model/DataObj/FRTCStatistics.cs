using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.Model.DataObj
{
    public class FRTCStatistics
    {
        public MediaStatistics mediaStatistics { get; set; }
        public SignalStatistics signalStatistics { get; set; }
    }

    public class MediaStatistics
    {
        public APR[] apr { get; set; }
        public APS[] aps { get; set; }
        public VCR[] vcr { get; set; }
        public VCS[] vcs { get; set; }
        public VPR[] vpr { get; set; }
        public VPS[] vps { get; set; }
    }

    public class APR
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int logicPacketLoss { get; set; }
        public int logicPacketLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
    }

    public class APS
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
        public int roundTripTime { get; set; }
    }

    public class VPR
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
    }

    public class VPS
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
        public int roundTripTime { get; set; }
    }

    public class VCS
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
        public int roundTripTime { get; set; }
    }

    public class VCR
    {
        public string participantName { get; set; }
        public string resolution { get; set; }
        public int frameRate { get; set; }
        public int jitter { get; set; }
        public int packageLoss { get; set; }
        public int packageLossRate { get; set; }
        public int rtpActualBitRate { get; set; }
        public int rtpLogicBitRate { get; set; }
        public int ssrc { get; set; }
        public int packageTotal { get; set; }
        public bool isAlive { get; set; }
    }

    public class SignalStatistics
    {
        public int callRate { get; set; }
    }

    public class MediaStatisticsForUI
    {
        public string Participant { get; set; }
        public string Channel { get; set; }
        public string Format { get; set; }
        public string Rate { get; set; }
        public string UsingRate { get; set; }
        public int FrameRate { get; set; }
        public string PackageLost { get; set; }
        public string Jitter { get; set; }
    }
}
