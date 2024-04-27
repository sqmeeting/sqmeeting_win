using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace SQMeeting.Model.DataObj
{
    public class FRTCMonitor
    {
        public string deviceName { get; set; }

        public string monitorName { get; set; }

        public int handle { get; set; }

        public int top { get; set; }

        public int left { get; set; }

        public int bottom { get; set; }

        public int right { get; set; }

        public int index { get; set; }

        public bool isPrimary { get; set; }

        public override string ToString()
        {
            return string.Format("Monitor info, deviceName: {0}, monitorName: {1}, handle: {2}, left: {3}, top: {4}, right: {5}, bottom: {6}, index: {7}, isprimary: {8}",
                deviceName, monitorName, handle, left, top, right, bottom, index, isPrimary);
        }
    }
}