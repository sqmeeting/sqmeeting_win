using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SQMeeting.Model.DataObj;

namespace SQMeeting.Model
{
    public class MeetingHistoryManager
    {
        List<MeetingHistoryData> _history = new List<MeetingHistoryData>();

        public MeetingHistoryManager()
        {
            //_history_list_lock = new ReaderWriterLockSlim();
            LoadHistoryData();
        }

        public async void AddHisotryRecord(MeetingHistoryData history)
        {
            _history.Insert(0, history);
            await UpdateHistoryData();
        }

        public async void UpdateLastMeetingEndTime()
        {
            if (_history.Count > 0)
            {
                _history.First().end_time = DateTime.Now.ToString("G");
                await UpdateHistoryData();
            }
        }
        public async void UpdateLastMeetingOwnerName(string Name)
        {
            if (_history.Count > 0)
            {
                _history.First().owner_name = Name;
                await UpdateHistoryData();
            }
        }

        public async void DeleteHisotryRecord(MeetingHistoryData history)
        {
            _history.Remove(history);
            await UpdateHistoryData();
        }

        public async void DeleteHisotryRecordByID(string uuid)
        {
            _history.RemoveAt(_history.FindIndex((p) => { return p.uuid == uuid; }));
            await UpdateHistoryData();
        }

        public async void DeleteAllHisotry()
        {
            _history.Clear();
            await UpdateHistoryData();
        }

        public List<MeetingHistoryData> GetMeetingHistoryByUserId(string userId)
        {
            List<MeetingHistoryData> ret = _history.FindAll((p) => { return p.user_id == userId; });
            return ret;
        }

        private void LoadHistoryData()
        {
            try
            {
                if (!File.Exists("meeting_history.json"))
                {
                    File.Create("meeting_history.json");
                    return;
                }
                using (FileStream f = File.OpenRead("meeting_history.json"))
                {
                    using (StreamReader sr = new StreamReader(f))
                    {
                        string hisStr = sr.ReadToEnd();
                        if (string.IsNullOrEmpty(hisStr))
                            return;
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(hisStr)))
                        {
                            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserMeetingHistory));
                            UserMeetingHistory his = (UserMeetingHistory)jsonSerializer.ReadObject(ms);
                            this._history = his.frtc_meeting_history.ToList();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
        }
    
        private async Task UpdateHistoryData()
        {
            try
            {
                using (FileStream fs = new FileStream("meeting_history.json", FileMode.Create))
                {
                    if (this._history.Count > 0)
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserMeetingHistory));
                        jsonSerializer.WriteObject(fs, new UserMeetingHistory() { frtc_meeting_history = this._history.ToArray() });
                        await fs.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogTool.LogHelper.Exception(ex);
            }
        }
    
    }
}
