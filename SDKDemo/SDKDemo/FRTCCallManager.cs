using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GalaSoft.MvvmLight.Messaging;
using SDKDemo.MvvMMessages;
using Newtonsoft.Json.Linq;
using System.Net;
using SDKDemo;

namespace SDKDemo.Model
{
    public class FRTCCallManager
    {
        public IntPtr m_meetingVideoWndHWND = IntPtr.Zero;
        private string _userToken = string.Empty;

        private static FRTCCallManager _selfInstance;

        public FrtcCallState CurrentCallState { get; private set; }


        public FRTCCallManager()
        {
            CurrentCallState = FrtcCallState.DISCONNECTED;
            _selfInstance = this;
        }

        public static OnFRTCCallStateChangeCallback callStateChangecallback = new OnFRTCCallStateChangeCallback(OnFrtcCallStateChanged);
        public static OnMeetingPasswordCallback passCodeCallback = new OnMeetingPasswordCallback(OnFrtcPassCodeRequested);

        private static void OnFrtcCallStateChanged(FrtcCallState state, FrtcCallReason result, string meetingID, string meetingName)
        {
            if (state == FrtcCallState.CONNECTED && state == _selfInstance.CurrentCallState)
            {
                return;
            }
            switch (state)
            {
                case FrtcCallState.CONNECTED:
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            FRTCCallStateChangeMessage msg = new FRTCCallStateChangeMessage()
                            { callState = state, reason = result, meetingId = meetingID, meetingName = meetingName };
                            Messenger.Default.Send<FRTCCallStateChangeMessage>(msg);
                        });
                    }
                    break;
                case FrtcCallState.CONNECTING:
                    {
                        App.Current.Dispatcher.Invoke(() =>
                        {
                            if (result == FrtcCallReason.CALL_PASSWORD_FAILED)
                            {
                                Messenger.Default.Send<FRTCMeetingPasswordMessage>(new FRTCMeetingPasswordMessage() { reason = "" });
                            }
                            else
                            {
                                FRTCCallStateChangeMessage msg = new FRTCCallStateChangeMessage()
                                { callState = state, reason = result, meetingId = meetingID, meetingName = meetingName };
                                Messenger.Default.Send<FRTCCallStateChangeMessage>(msg);
                            }
                        });
                    }
                    break;
                case FrtcCallState.DISCONNECTING:
                case FrtcCallState.DISCONNECTED:
                    try
                    {
                        App.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            try
                            {
                                FRTCCallStateChangeMessage msg = new FRTCCallStateChangeMessage()
                                { callState = state, reason = result };
                                if ((int)result == 49)
                                {
                                    return;
                                }
                                else
                                    Messenger.Default.Send<FRTCCallStateChangeMessage>(msg);
                            }
                            catch { }
                        }), null);
                    }
                    catch { }
                    break;
                default:
                    break;
            }
            _selfInstance.CurrentCallState = state;
        }

        private static void OnFrtcPassCodeRequested(bool isResponse, string reason)
        {
            Messenger.Default.Send<FRTCMeetingPasswordMessage>(new FRTCMeetingPasswordMessage() { reason = reason });
        }

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public void SetAPIBaseUrl(string BaseUrl)
        {
            RESTEngine.SetBaseUrl(BaseUrl);
        }

        public void JoinMeeting(string meetingID, string guestName, int callRate, bool preMuteCamera, bool preMuteMic, string token, string passCode)
        {
            FrtcCallParam param = new FrtcCallParam();

            param.serverAddress = Marshal.StringToHGlobalAnsi(ConfigurationManager.AppSettings["FRTCServerAddress"]);
            param.callNumberStr = Marshal.StringToHGlobalAnsi(meetingID);
            param.callRateStr = Marshal.StringToHGlobalAnsi(callRate.ToString());
            param.displayNameStr = Marshal.StringToHGlobalAnsi(guestName);
            param.muteAudio = preMuteMic;
            param.muteVideo = (callRate > 0 && callRate <= 64) ? true : preMuteCamera;
            param.isAudioOnly = (callRate > 0 && callRate <= 64);
            string strLayout = ConfigurationManager.AppSettings["VideoLayout"];
            if (string.IsNullOrEmpty(strLayout))
            {
                strLayout = "0";
            }
            param.layout = (FrtcLayout)Enum.Parse(typeof(FrtcLayout), strLayout, true);
            if (!string.IsNullOrEmpty(token))
                param.userToken = Marshal.StringToHGlobalAnsi(token);
            if (!string.IsNullOrEmpty(passCode))
                param.meetingPasswd = Marshal.StringToHGlobalAnsi(passCode);

            FRTCSDK.frtc_call_join(param);
            return;
        }

        public void JoinMeetingPlainTextURL(string serverAddr, string meetingID, string guestName, int callRate, bool preMuteCamera, bool preMuteMic, string token, string passCode)
        {
            FrtcCallParam param = new FrtcCallParam();

            param.serverAddress = Marshal.StringToHGlobalAnsi(serverAddr);
            param.callNumberStr = Marshal.StringToHGlobalAnsi(meetingID);
            param.callRateStr = Marshal.StringToHGlobalAnsi(callRate.ToString());
            param.displayNameStr = Marshal.StringToHGlobalAnsi(guestName);
            param.muteAudio = preMuteMic;
            param.muteVideo = (callRate > 0 && callRate <= 64) ? true : preMuteCamera;
            param.isAudioOnly = (callRate > 0 && callRate <= 64);
            string strLayout = ConfigurationManager.AppSettings["VideoLayout"];
            if (string.IsNullOrEmpty(strLayout))
            {
                strLayout = "0";
            }
            param.layout = (FrtcLayout)Enum.Parse(typeof(FrtcLayout), strLayout, true);
            if (!string.IsNullOrEmpty(token))
                param.userToken = Marshal.StringToHGlobalAnsi(token);
            if (!string.IsNullOrEmpty(passCode))
                param.meetingPasswd = Marshal.StringToHGlobalAnsi(passCode);

            FRTCSDK.frtc_call_join(param);
            return;
        }
        public void ReconnectMeeting()
        {
            FrtcCallParam param = new FrtcCallParam();
            FRTCSDK.frtc_call_join(param);
        }
        public void SendMeetingPWD(string passCode)
        {
            FRTCSDK.frtc_call_password_send(passCode);
        }

        public void DropCall()
        {
            FRTCSDK.frtc_call_leave();
        }

        public async void EndCall(string meetingNumber)
        {
            List<KeyValuePair<string, string>> endParams = new List<KeyValuePair<string, string>>();
            endParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + meetingNumber + "/stop";
            RESTRequestObj req = new RESTRequestObj(url, endParams);
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "");

        }
        public async void MuteAll(string MeetingNumber, bool AllowSelfUnmute)
        {
            List<KeyValuePair<string, string>> muteAllParams = new List<KeyValuePair<string, string>>();
            muteAllParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/mute_all";
            RESTRequestObj req = new RESTRequestObj(url, muteAllParams);
            req.Body = "{\"allow_self_unmute\":" + AllowSelfUnmute.ToString().ToLower() + "}";
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "mute_all");

        }

        public async void UnmuteAll(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> unmuteAllParams = new List<KeyValuePair<string, string>>();
            unmuteAllParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/unmute_all";
            RESTRequestObj req = new RESTRequestObj(url, unmuteAllParams);
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "unmute_all");

        }

        public async void MuteUsers(string MeetingNumber, IEnumerable<string> UserId, bool AllowSelfUnmute)
        {
            List<KeyValuePair<string, string>> muteParams = new List<KeyValuePair<string, string>>();
            muteParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/mute";
            RESTRequestObj req = new RESTRequestObj(url, muteParams);
            JObject jObj = new JObject();
            //jObj.Add("allow_self_unmute", JToken.FromObject(AllowSelfUnmute));
            jObj.Add("participants", new JArray(UserId.ToArray()));
            req.Body = jObj.ToString();
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "");

        }

        public async void UnmuteUsers(string MeetingNumber, IEnumerable<string> UserId)
        {
            List<KeyValuePair<string, string>> unmuteParams = new List<KeyValuePair<string, string>>();
            unmuteParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/unmute";
            RESTRequestObj req = new RESTRequestObj(url, unmuteParams);
            JObject jObj = new JObject();
            jObj.Add("participants", new JArray(UserId.ToArray()));
            req.Body = jObj.ToString();
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "");

        }

        public async void RenameUser(string MeetingNumber, string UserId, string DisplayName, string token)
        {
            List<KeyValuePair<string, string>> renameParams = new List<KeyValuePair<string, string>>();
            if (!string.IsNullOrEmpty(token))
                renameParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/participant";
            RESTRequestObj req = new RESTRequestObj(url, renameParams);
            JObject jObj = new JObject();
            jObj.Add("client_id", UserId);
            jObj.Add("display_name", DisplayName);
            req.Body = jObj.ToString();
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "rename");

        }

        public async void SendMeetingMessage(string MeetingNumber, string Message, int RepeatTimes, int Position, bool EnableScroll)
        {
            List<KeyValuePair<string, string>> sendMeetingMessageParams = new List<KeyValuePair<string, string>>();
            sendMeetingMessageParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/overlay";
            RESTRequestObj req = new RESTRequestObj(url, sendMeetingMessageParams);
            JObject jObj = new JObject();
            jObj.Add("content", Message);
            jObj.Add("repeat", RepeatTimes);
            jObj.Add("position", Position);
            jObj.Add("enable_scroll", EnableScroll);
            req.Body = jObj.ToString();
            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "start_text_overlay");

        }

        public async void StopMeetingMessage(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> stopOverlayParams = new List<KeyValuePair<string, string>>();
            stopOverlayParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/overlay";
            RESTRequestObj req = new RESTRequestObj(url, stopOverlayParams);
            var responseObj = await req.SendDeleteRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "stop_text_overlay");

        }

        public async void SetLecturer(string MeetingNumber, string LecturerUID)
        {
            List<KeyValuePair<string, string>> setLecturerParams = new List<KeyValuePair<string, string>>();
            setLecturerParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/lecturer";
            RESTRequestObj req = new RESTRequestObj(url, setLecturerParams);
            JObject jObj = new JObject();
            jObj.Add("lecturer", LecturerUID);
            req.Body = jObj.ToString();

            var responseObj = await req.SendPostRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "set_lecturer");

        }

        public async void UnsetLecturer(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> unsetLecturerParams = new List<KeyValuePair<string, string>>();
            unsetLecturerParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/lecturer";
            RESTRequestObj req = new RESTRequestObj(url, unsetLecturerParams);

            var responseObj = await req.SendDeleteRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "unset_lecturer");

        }

        public async void KickOutParticipants(string MeetingNumber, IEnumerable<string> KickOutList, bool KickOutAll)
        {
            List<KeyValuePair<string, string>> kickoutParams = new List<KeyValuePair<string, string>>();
            kickoutParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/disconnect";
            RESTRequestObj req = new RESTRequestObj(url, kickoutParams);
            if (!KickOutAll)
            {
                if (KickOutList != null)
                {
                    JObject jObj = new JObject();
                    jObj.Add("participants", new JArray(KickOutList.ToArray()));
                    req.Body = jObj.ToString();
                }
                else
                {
                    throw new ArgumentNullException("KickOutList", "KickOutList can not be null when KickOutAll is false");
                }
            }

            var responseObj = await req.SendDeleteRequest();

            OnMeetingControlOperationResult(responseObj.StatusCode, "", "kickout");

        }
        public async void StartRecording(string MeetingNumber)
        {
            await Recording(MeetingNumber, true);
        }

        public async void StopRecording(string MeetingNumber)
        {
            await Recording(MeetingNumber, false);
        }

        private async Task Recording(string MeetingNumber, bool start)
        {
            List<KeyValuePair<string, string>> recordingParams = new List<KeyValuePair<string, string>>();
            recordingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/recording";
            RESTRequestObj req = new RESTRequestObj(url, recordingParams);

            JObject jObj = new JObject();
            jObj.Add("meeting_number", MeetingNumber);
            req.Body = jObj.ToString();

            RESTResponseObj responseObj = null;
            if (start)
                responseObj = await req.SendPostRequest();
            else
                responseObj = await req.SendDeleteRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "recording", start);

        }

        public async void StartStreaming(string MeetingNumber, string StreamingPassword)
        {
            await Streaming(MeetingNumber, true, StreamingPassword);
        }

        public async void StopStreaming(string MeetingNumber)
        {
            await Streaming(MeetingNumber, false, string.Empty);
        }
        private async Task Streaming(string MeetingNumber, bool start, string StreamingPassword)
        {
            List<KeyValuePair<string, string>> streamingParams = new List<KeyValuePair<string, string>>();
            streamingParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/live";
            RESTRequestObj req = new RESTRequestObj(url, streamingParams);

            JObject jObj = new JObject();
            jObj.Add("meeting_number", MeetingNumber);
            if (start && !string.IsNullOrEmpty(StreamingPassword))
                jObj.Add("live_password", StreamingPassword);
            req.Body = jObj.ToString();

            RESTResponseObj responseObj = null;
            if (start)
                responseObj = await req.SendPostRequest();
            else
                responseObj = await req.SendDeleteRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "streaming", start);

        }

        public async void ApplyForUnmute(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> requestParams = new List<KeyValuePair<string, string>>();
            string url = "/api/v1/meeting/" + MeetingNumber + "/request_unmute";

            RESTRequestObj req = new RESTRequestObj(url, requestParams);

            RESTResponseObj responseObj = await req.SendPostRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "apply_for_unmute");

        }

        public async void ApproveUnmuteApplication(string MeetingNumber, IEnumerable<string> ApplicantUUIDList)
        {
            List<KeyValuePair<string, string>> requestParams = new List<KeyValuePair<string, string>>();
            requestParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/allow_unmute";
            RESTRequestObj req = new RESTRequestObj(url, requestParams);

            JObject jObj = new JObject();
            JArray jArray = new JArray(ApplicantUUIDList.ToList());
            jObj.Add("participants", jArray);
            req.Body = jObj.ToString();

            RESTResponseObj responseObj = await req.SendPostRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "approve_unmute", ApplicantUUIDList);

        }

        public async void ApproveAllUnmuteApplication(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> requestParams = new List<KeyValuePair<string, string>>();
            requestParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/allow_unmute_all";

            RESTRequestObj req = new RESTRequestObj(url, requestParams);

            RESTResponseObj responseObj = await req.SendPostRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "approve_all_unmute");

        }

        public async void PinVideo(string MeetingNumber, IEnumerable<string> PinClientID)
        {
            List<KeyValuePair<string, string>> requestParams = new List<KeyValuePair<string, string>>();
            requestParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/pin";

            RESTRequestObj req = new RESTRequestObj(url, requestParams);

            JObject jObj = new JObject();
            JArray idArray = new JArray(PinClientID.ToList());
            jObj.Add("participants", idArray);

            req.Body = jObj.ToString();

            RESTResponseObj responseObj = await req.SendPostRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "pin_for_meeting");

        }

        public async void UnpinVideo(string MeetingNumber)
        {
            List<KeyValuePair<string, string>> requestParams = new List<KeyValuePair<string, string>>();
            requestParams.Add(new KeyValuePair<string, string>("token", _userToken));
            string url = "/api/v1/meeting/" + MeetingNumber + "/pin";

            RESTRequestObj req = new RESTRequestObj(url, requestParams);

            RESTResponseObj responseObj = await req.SendDeleteRequest();

            string errorCode = string.Empty;
            if (responseObj.StatusCode != HttpStatusCode.OK)
            {
                try
                {
                    errorCode = JObject.Parse(responseObj.Body).GetValue("errorCode").ToString();
                }
                catch (Exception ex)
                {
                }
            }

            OnMeetingControlOperationResult(responseObj.StatusCode, errorCode, "unpin_for_meeting");

        }


        private void OnMeetingControlOperationResult(HttpStatusCode statusCode, string errorCode, string apiName, object resultParam = null)
        {
            Messenger.Default.Send(new MeetingControlOperationResultMessage(statusCode, errorCode, apiName, resultParam));
        }
    }
}
