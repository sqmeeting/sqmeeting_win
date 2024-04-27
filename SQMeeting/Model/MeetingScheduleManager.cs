using SQMeeting.Model.DataObj;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using SQMeeting.MvvMMessages;

namespace SQMeeting.Model
{
    public enum MeetingScheduleStatusCode
    {
        Schedule_Meeting_Success,
        Get_Scheduled_Meeting_List_Success,
        Get_Scheduled_Meeting_Detail_Success,
        Get_Recurring_Meeting_Group_Success,
        Update_Scheduled_Meeting_Success,
        Update_Scheduled_Recurring_Success,
        Delete_Scheduled_Meeting_Success,
        Add_Meeting_To_My_List_Success,
        Remove_Meeting_From_My_List_Success,
        Schedule_Meeting_General_Error,
        Get_Scheduled_Meeting_List_General_Error,
        Get_Scheduled_Meeting_Detail_General_Error,
        Get_Recurring_Meeting_Group_General_Error,
        Update_Scheduled_Meeting_General_Error,
        Update_Scheduled_Recurring_General_Error,
        Delete_Scheduled_Meeting_General_Error,
        Add_Meeting_To_My_List_General_Error,
        Remove_Meeting_From_My_List_General_Error,
        SessionToken_Invalid
    }
    public class MeetingScheduleManager
    {
        private string _userToken = string.Empty;

        public MeetingScheduleManager()
        {
            _userToken = ConfigurationManager.AppSettings["FRTCUserToken"];
            Messenger.Default.Register<FRTCAPIResultMessage>(this, new Action<FRTCAPIResultMessage>((msg) =>
            {
                if (msg != null && (msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS || msg.Result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN))
                    this._userToken = CommonServiceLocator.ServiceLocator.Current.GetInstance<FRTCUserManager>().UserData.user_token;
            }));
        }

        public async void SetupMeeting(string MeetingName)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

            string reqBody = "{ \"meeting_type\":\"instant\", \"meeting_name\":\"" + MeetingName + "\"}";

            RESTRequestObj req = new RESTRequestObj("/api/v1/meeting_schedule", meetingParams, reqBody);
            var responseObj = await req.SendPostRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Schedule_Meeting_Success : MeetingScheduleStatusCode.Schedule_Meeting_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void ScheduleMeeting(MeetingScheduleInfo scheduleInfo)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

            string reqBody = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MeetingScheduleInfo));
                serializer.WriteObject(ms, scheduleInfo);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    reqBody = sr.ReadToEnd();
                }
            }
            string apiPath = "/api/v1/meeting_schedule";
            if(scheduleInfo.meeting_type.ToLower() == "recurrence")
            {
                apiPath = "/api/v1/meeting_schedule/recurrence";
            }
            RESTRequestObj req = new RESTRequestObj(apiPath, meetingParams, reqBody);

            LogTool.LogHelper.Debug("Schedule a new meeting, api path is {0}, request body is {1}", apiPath, reqBody);

            var responseObj = await req.SendPostRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Schedule_Meeting_Success : MeetingScheduleStatusCode.Schedule_Meeting_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void UpdateScheduledMeeting(string reservation_id, MeetingScheduleInfo scheduleInfo)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

            string reqBody = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MeetingScheduleInfo));
                serializer.WriteObject(ms, scheduleInfo);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    reqBody = sr.ReadToEnd();
                }
            }

            string reqUrl = "/api/v1/meeting_schedule/" + reservation_id;
            RESTRequestObj req = new RESTRequestObj(reqUrl, meetingParams, reqBody);

            LogTool.LogHelper.Debug("Update a scheduled meeting, api path is {0}, request body is {1}", reqUrl, reqBody);

            var responseObj = await req.SendPostRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Update_Scheduled_Meeting_Success : MeetingScheduleStatusCode.Update_Scheduled_Meeting_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            string retData = reservation_id;
            if (scheduleInfo.meeting_type == "recurrence")
                retData = scheduleInfo.recurrenceId;
            OnScheduleMeetingResponsed(ret, retData);
        }

        public async void DeleteScheduledMeeting(string reservation_id, bool deleteGroup = false)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            if(deleteGroup)
            {
                meetingParams.Add(new KeyValuePair<string, string>("deleteGroup", "true"));
                LogTool.LogHelper.Debug("Delete a recurrence meeting group, set deleteGroup to true");
            }

            string reqUrl = "/api/v1/meeting_schedule/" + reservation_id;
            RESTRequestObj req = new RESTRequestObj(reqUrl, meetingParams);

            var responseObj = await req.SendDeleteRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Delete_Scheduled_Meeting_Success : MeetingScheduleStatusCode.Delete_Scheduled_Meeting_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, reservation_id);
        }

        public async void GetScheduledMeetingDetail(string reservation_id)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

            string reqUrl = "/api/v1/meeting_schedule/" + reservation_id;
            RESTRequestObj req = new RESTRequestObj(reqUrl, meetingParams);
            var responseObj = await req.SendGetRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_Success : MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void GetRecurringMeetingGroup(string group_id, int pageNume, int pageSize, string filter)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            meetingParams.Add(new KeyValuePair<string, string>("page_num", pageNume.ToString()));
            meetingParams.Add(new KeyValuePair<string, string>("page_size", pageSize.ToString()));
            meetingParams.Add(new KeyValuePair<string, string>("sort", "startTime"));
            //meetingParams.Add(new KeyValuePair<string, string>("filter", filter));

            string reqUrl = "/api/v1/meeting_schedule/recurrence/" + group_id;
            RESTRequestObj req = new RESTRequestObj(reqUrl, meetingParams);
            var responseObj = await req.SendGetRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Get_Recurring_Meeting_Group_Success : MeetingScheduleStatusCode.Get_Recurring_Meeting_Group_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;

            LogTool.LogHelper.Debug("Get recurrence meeting group: {0}", responseObj.Body);

            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void UpdateRecurringMeetingGroup(string reservation_id, MeetingScheduleInfo scheduleInfo)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            //meetingParams.Add(new KeyValuePair<string, string>("filter", filter));

            string reqBody = string.Empty;
            using (MemoryStream ms = new MemoryStream())
            {
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(MeetingScheduleInfo));
                serializer.WriteObject(ms, scheduleInfo);
                ms.Position = 0;
                using (StreamReader sr = new StreamReader(ms))
                {
                    reqBody = sr.ReadToEnd();
                }
            }

            string reqUrl = "/api/v1/meeting_schedule/recurrence/" + reservation_id;
            RESTRequestObj req = new RESTRequestObj(reqUrl, meetingParams, reqBody);

            LogTool.LogHelper.Debug("Update a recurrence meeting, apipath is {0}, reqBody is {1}", reqUrl, reqBody);


            var responseObj = await req.SendPostRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Update_Scheduled_Recurring_Success : MeetingScheduleStatusCode.Update_Scheduled_Recurring_General_Error;

            LogTool.LogHelper.Debug("Update a recurrence meeting got ret");
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, reservation_id);
        }

        public async void GetScheduledMeetingList(int pageNume, int pageSize, string filter)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

            //recurring meeting aggregation made page turing fail, use only 1 page
            //meetingParams.Add(new KeyValuePair<string, string>("page_num", pageNume.ToString()));
            //meetingParams.Add(new KeyValuePair<string, string>("page_size", pageSize.ToString()));
            meetingParams.Add(new KeyValuePair<string, string>("page_num", "1"));
            meetingParams.Add(new KeyValuePair<string, string>("page_size", "500"));


            meetingParams.Add(new KeyValuePair<string, string>("sort", "startTime"));
            //meetingParams.Add(new KeyValuePair<string, string>("filter", filter.ToString()));

            RESTRequestObj req = new RESTRequestObj("/api/v1/meeting_schedule", meetingParams);
            var responseObj = await req.SendGetRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Get_Scheduled_Meeting_List_Success : MeetingScheduleStatusCode.Get_Scheduled_Meeting_List_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;

            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void AddMeetingToList(string identifier)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            RESTRequestObj req = new RESTRequestObj("/api/v1/meeting_list/add/" + identifier, meetingParams);
            var responseObj = await req.SendPostRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Add_Meeting_To_My_List_Success : MeetingScheduleStatusCode.Add_Meeting_To_My_List_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        public async void RemoveMeetingFromList(string identifier)
        {
            if (string.IsNullOrEmpty(_userToken))
            {
                return;
            }
            List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
            meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            RESTRequestObj req = new RESTRequestObj("/api/v1/meeting_list/remove/" + identifier, meetingParams);
            var responseObj = await req.SendDeleteRequest();
            MeetingScheduleStatusCode ret = responseObj.StatusCode ==
                System.Net.HttpStatusCode.OK ? MeetingScheduleStatusCode.Remove_Meeting_From_My_List_Success : MeetingScheduleStatusCode.Remove_Meeting_From_My_List_General_Error;
            if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                ret = MeetingScheduleStatusCode.SessionToken_Invalid;
            OnScheduleMeetingResponsed(ret, responseObj.Body);
        }

        private static void OnScheduleMeetingResponsed(MeetingScheduleStatusCode result, string meetingData)
        {
            switch (result)
            {
                case MeetingScheduleStatusCode.Schedule_Meeting_Success:
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_Success:
                    if(string.IsNullOrEmpty(meetingData))
                    {
                        return;
                    }
                    MeetingScheduleResult meetingDataObj = null;
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(meetingData)))
                    {
                        try
                        {
                            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(MeetingScheduleResult));
                            meetingDataObj = (MeetingScheduleResult)jsonSerializer.ReadObject(ms);
                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }
                        finally
                        {
                            ms.Close();
                        }
                    }
                    if (meetingDataObj != null)
                    {
                        if (result == MeetingScheduleStatusCode.Schedule_Meeting_Success)
                            Messenger.Default.Send<FRTCMeetingScheduledMessage>(new FRTCMeetingScheduledMessage(meetingDataObj));
                        else if (result == MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_Success)
                            Messenger.Default.Send<FRTCScheduledMeetingDetailMessage>(new FRTCScheduledMeetingDetailMessage(meetingDataObj));
                    }
                    break;
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_List_Success:
                    if (string.IsNullOrEmpty(meetingData))
                    {
                        return;
                    }
                    ScheduledMeetingList meetingList = null;
                    if (meetingData != null)
                    {
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(meetingData)))
                        {
                            try
                            {
                                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(ScheduledMeetingList));
                                meetingList = (ScheduledMeetingList)jsonSerializer.ReadObject(ms);
                                if (meetingList != null)
                                {
                                    Messenger.Default.Send<FRTCScheduleMeetingListMessage>(new FRTCScheduleMeetingListMessage(meetingList));
                                    LogTool.LogHelper.Debug("Get meetng list count {0}", meetingList.meeting_schedules.Count());
                                }
                            }
                            catch (Exception ex)
                            {
                                LogTool.LogHelper.Exception(ex);
                            }
                            finally
                            {
                                if(ms != null)
                                    ms.Close();
                            }
                        }
                    }
                    break;
                case MeetingScheduleStatusCode.Get_Recurring_Meeting_Group_Success:
                    RecurringMeetingGroup recurringMeetingGroup = null;
                    if (string.IsNullOrEmpty(meetingData))
                    {
                        return;
                    }
                    if (meetingData != null)
                    {
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(meetingData)))
                        {
                            try
                            {
                                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(RecurringMeetingGroup));
                                recurringMeetingGroup = (RecurringMeetingGroup)jsonSerializer.ReadObject(ms);
                                if (recurringMeetingGroup != null)
                                    Messenger.Default.Send<FRTCRecurringMeetingGroupMessage>(new FRTCRecurringMeetingGroupMessage(recurringMeetingGroup));
                            }
                            catch (Exception ex)
                            {
                                LogTool.LogHelper.Exception(ex);
                            }
                            finally
                            {
                                if (ms != null)
                                    ms.Close();
                            }
                        }
                    }
                    break;
                case MeetingScheduleStatusCode.Update_Scheduled_Meeting_Success:
                    Messenger.Default.Send<FRTCEditMeetingSuccessMsg>(new FRTCEditMeetingSuccessMsg("update_scheduled_meeting_succeess", meetingData));
                    break;
                case MeetingScheduleStatusCode.Update_Scheduled_Recurring_Success:
                    Messenger.Default.Send<FRTCEditMeetingSuccessMsg>(new FRTCEditMeetingSuccessMsg("update_scheduled_recurring_succeess", meetingData));
                    break;
                case MeetingScheduleStatusCode.Delete_Scheduled_Meeting_Success:
                    Messenger.Default.Send<FRTCEditMeetingSuccessMsg>(new FRTCEditMeetingSuccessMsg("delete_scheduled_meeting_success", meetingData));
                    break;
                case MeetingScheduleStatusCode.Add_Meeting_To_My_List_Success:
                    Messenger.Default.Send<FRTCEditMeetingSuccessMsg>(new FRTCEditMeetingSuccessMsg("add_meeting_to_list_success", meetingData));
                    break;
                case MeetingScheduleStatusCode.Remove_Meeting_From_My_List_Success:
                    Messenger.Default.Send<FRTCEditMeetingSuccessMsg>(new FRTCEditMeetingSuccessMsg("remove_meeting_from_list_success", meetingData));
                    break;
                case MeetingScheduleStatusCode.Schedule_Meeting_General_Error:
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_List_General_Error:
                case MeetingScheduleStatusCode.Get_Scheduled_Meeting_Detail_General_Error:
                case MeetingScheduleStatusCode.Update_Scheduled_Meeting_General_Error:
                case MeetingScheduleStatusCode.Update_Scheduled_Recurring_General_Error:
                case MeetingScheduleStatusCode.Delete_Scheduled_Meeting_General_Error:
                case MeetingScheduleStatusCode.Add_Meeting_To_My_List_General_Error:
                case MeetingScheduleStatusCode.Remove_Meeting_From_My_List_General_Error:
                case MeetingScheduleStatusCode.SessionToken_Invalid:
                    LogTool.LogHelper.Error("Schedule meeting api error {0}", result);
                    Messenger.Default.Send<MeetingScheduleAPIErrorMessage>(new MeetingScheduleAPIErrorMessage(result));
                    break;
                default:
                    break;
            }
        }

    }
}
