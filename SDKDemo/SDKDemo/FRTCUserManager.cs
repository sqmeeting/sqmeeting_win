using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using SDKDemo.Model.DataObj;
using SDKDemo.MvvMMessages;
using SDKDemo;

namespace SDKDemo.Model
{
    public enum UserRole
    {
        Normal,
        MeetingOperator,
        SystemAdmin
    }

    public enum SignInErrorCode
    {
        SignIn_Failed_PWD_Error_General = 0x00003000,
        SignIn_Failed_PWD_Error_Freezed = 0x00003001,
        SignIn_Failed_PWD_Error_Locked = 0x00003002,
        SignIn_Failed_PWD_Error = 0x00003003,
    }
    public class FRTCUserManager
    {
        const string salt = "49d88eb34f77fc9e81cbdc5190c7efdc";
        public FRTCUserManager()
        {
            _userToken = ConfigurationManager.AppSettings["FRTCUserToken"];
            _selfInstance = this;
        }

        private FRTCUserData _userData = null;
        public FRTCUserData UserData
        {
            get
            {
                return _userData;
            }
        }

        private UserMeetingRoomList _userMeetingRoomList = null;
        public UserMeetingRoomList UserMeetingRoomList
        {
            get
            {
                return _userMeetingRoomList;
            }
        }

        private UserInfoList _searchedUserList = null;
        public UserInfoList SearchedUserList
        {
            get
            {
                return _searchedUserList;
            }
        }


        private string _userToken = string.Empty;

        private bool _isUserSignIn = false;
        public bool IsUserSignIn
        {
            get
            {
                return _isUserSignIn;
            }
            private set
            {
                if (_isUserSignIn != value)
                {
                    _isUserSignIn = value;
                }
            }
        }

        private string _userName = string.Empty;
        public string UserName
        {
            get
            {
                return _userName;
            }
            private set
            {
                if (_userName != value)
                {
                    _userName = value;
                }
            }
        }

        private UserRole _role = UserRole.Normal;
        public UserRole Role
        {
            get
            {
                return _role;
            }
            private set
            {
                if (_role != value)
                {
                    _role = value;
                }
            }
        }

        private static FRTCUserManager _selfInstance;

        private static void OnUserAPIResponsed(FRTC_API_RESULT result, string signInData)
        {
            FRTCUserManager self = _selfInstance;
            if (self.IsUserSignIn && (result == FRTC_API_RESULT.SIGNIN_SUCCESS || result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN))//heart beat
            {
                return;
            }
            object dataObj = null;
            if (result == FRTC_API_RESULT.USER_ROOM_SUCCESS)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(signInData)))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserMeetingRoomList));
                        self._userMeetingRoomList = (UserMeetingRoomList)jsonSerializer.ReadObject(ms);
                        ms.Close();
                    }
                }
                catch (Exception e) { }
            }
            else if (result == FRTC_API_RESULT.QUERY_USER_SUCCESS)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(signInData)))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(UserInfoList));
                        dataObj = jsonSerializer.ReadObject(ms);
                        ms.Close();
                    }
                }
                catch (Exception e) { }
            }
            else if (result == FRTC_API_RESULT.API_SESSION_INVALID)
            {

            }

            GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<FRTCAPIResultMessage>(new FRTCAPIResultMessage()
            {
                Result = result,
                ResponseData = signInData == null ? string.Empty : signInData,
                DataObj = dataObj
            });
        }

        private static void OnSignInResult(FRTC_API_RESULT result, string signInData)
        {
            FRTCUserManager self = _selfInstance;
            if (self.IsUserSignIn && (result == FRTC_API_RESULT.SIGNIN_SUCCESS || result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN))//heart beat
            {
                return;
            }
            object dataObj = null;
            if (result == FRTC_API_RESULT.SIGNIN_SUCCESS || result == FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN)
            {
                try
                {
                    using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(signInData)))
                    {
                        DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(FRTCUserData));
                        self._userData = (FRTCUserData)jsonSerializer.ReadObject(ms);
                        ms.Close();
                    }

                    self._userToken = self._userData.user_token;
                    self._userName = self._userData.username;

                    if (self._userData.role != null && self._userData.role.Length > 0)
                    {
                        for (int i = 0; i < self._userData.role.Length; i++)
                        {
                            if (self._userData.role[i] == "SystemAdmin")
                            {
                                self.Role = UserRole.SystemAdmin;
                                break;
                            }
                            else if (self._userData.role[i] == "MeetingOperator")
                            {
                                self.Role = UserRole.MeetingOperator;
                            }
                        }
                    }

                    try
                    {
                        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        config.AppSettings.Settings["FRTCUserToken"].Value = self._userToken;
                        config.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                        config = null;
                    }
                    catch (ConfigurationErrorsException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }

                self.IsUserSignIn = true;

                if (string.IsNullOrEmpty(self.UserName))
                {
                    self.UserName = ConfigurationManager.AppSettings["FRTCUserName"];
                }
                else
                {
                    try
                    {
                        Configuration configItem = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        configItem.AppSettings.Settings["FRTCUserName"].Value = self.UserName;
                        configItem.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                    }
                    catch (ConfigurationErrorsException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else if (result == FRTC_API_RESULT.USER_NOT_EXIST)
            {
                self.UserName = string.Empty;
                self._userToken = string.Empty;
            }
            else if (result == FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD
                || result == FRTC_API_RESULT.SIGNIN_FAILED_INVALID_TOKEN
                || result == FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR
                || result == FRTC_API_RESULT.SIGNOUT_SUCCESS)
            {
                self.IsUserSignIn = false;
                self._userToken = string.Empty;
                if (result == FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD)
                {
                    FRTC_API_RESULT failedRet = FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD;
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(signInData)))
                        {
                            DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(FRTCUserData));
                            self._userData = (FRTCUserData)jsonSerializer.ReadObject(ms);
                            ms.Close();
                        }

                        string errStr = self._userData.errorCode;
                        if (errStr.StartsWith("0x"))
                        {
                            errStr = errStr.Substring("0x".Length);
                        }
                        int err = Convert.ToInt32(errStr, 16);
                        //if(int.TryParse(, System.Globalization.NumberStyles.HexNumber, out err))
                        {
                            switch ((SignInErrorCode)err)
                            {
                                case SignInErrorCode.SignIn_Failed_PWD_Error_General:
                                    failedRet = FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR;
                                    break;
                                case SignInErrorCode.SignIn_Failed_PWD_Error_Freezed:
                                    failedRet = FRTC_API_RESULT.SIGNIN_FAILED_PWD_FREEZED;
                                    break;
                                case SignInErrorCode.SignIn_Failed_PWD_Error_Locked:
                                    failedRet = FRTC_API_RESULT.SIGNIN_FAILED_PWD_LOCKED;
                                    break;
                                case SignInErrorCode.SignIn_Failed_PWD_Error:
                                    failedRet = FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD;
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    catch { }
                    GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<FRTCAPIResultMessage>(new FRTCAPIResultMessage()
                    {
                        Result = failedRet,
                        ResponseData = signInData == null ? string.Empty : signInData,
                        DataObj = dataObj
                    });
                    return;
                }
                else if (result == FRTC_API_RESULT.SIGNOUT_SUCCESS)
                {
                    try
                    {
                        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        config.AppSettings.Settings["FRTCUserName"].Value = string.Empty;
                        config.AppSettings.Settings["FRTCUserToken"].Value = string.Empty;
                        config.Save(ConfigurationSaveMode.Modified, true);
                        ConfigurationManager.RefreshSection("appSettings");
                        config = null;
                    }
                    catch (ConfigurationErrorsException e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    self._userToken = string.Empty;
                    self.Role = UserRole.Normal;
                }
            }

            else
            {
                self.IsUserSignIn = false;
                self._userToken = string.Empty;
            }
            try
            {
                GalaSoft.MvvmLight.Messaging.Messenger.Default.Send<FRTCAPIResultMessage>(new FRTCAPIResultMessage()
                {
                    Result = result,
                    ResponseData = signInData == null ? string.Empty : signInData,
                    DataObj = dataObj
                });
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public async void FRTCResetUserPassword(string OldPwd, string NewPwd)
        {

            string cypheredOld = Utilities.CommunicationUtils.GetSecret(OldPwd, salt);
            string cypheredNew = Utilities.CommunicationUtils.GetSecret(NewPwd, salt);

            List<KeyValuePair<string, string>> reqParams = new List<KeyValuePair<string, string>>();
            reqParams.Add(new KeyValuePair<string, string>("token", _userToken));


            string reqBody = "{ \"secret_old\":\"" + cypheredOld + "\", \"secret_new\":\"" + cypheredNew + "\"}";
            RESTRequestObj req = new RESTRequestObj("/api/v1/user/password", reqParams, reqBody);

            var responseObj = await req.SendPutRequest();


            FRTC_API_RESULT ret = FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR;
            switch (responseObj.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    ret = FRTC_API_RESULT.PASSWORD_RESET_SUCCESS;
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                case System.Net.HttpStatusCode.BadRequest:
                    ret = FRTC_API_RESULT.PASSWORD_RESET_FAILED;
                    break;
                default:
                    break;
            }
            OnUserAPIResponsed(ret, responseObj.Body);

        }

        public async void SignIn(string UserName, string Password)
        {
            string cyphered = Utilities.CommunicationUtils.GetSecret(Password, salt);
            string reqBody = "{ \"username\":\"" + UserName + "\", \"secret\":\"" + cyphered + "\"}";

            RESTRequestObj req = new RESTRequestObj("/api/v1/user/sign_in", null, reqBody);

            var responseObj = await req.SendPostRequest();

            FRTC_API_RESULT ret = FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR;
            switch (responseObj.StatusCode)
            {
                case System.Net.HttpStatusCode.OK:
                    ret = FRTC_API_RESULT.SIGNIN_SUCCESS;
                    break;
                case System.Net.HttpStatusCode.Unauthorized:
                    ret = FRTC_API_RESULT.SIGNIN_FAILED_INVALID_PASSWORD;
                    break;
                case System.Net.HttpStatusCode.NotFound:
                case System.Net.HttpStatusCode.RequestTimeout:
                    ret = FRTC_API_RESULT.CONNECTION_FAILED;
                    break;
                default:
                    break;
            }
            OnSignInResult(ret, responseObj.Body);
        }

        public async void SignInViaToken()
        {
            List<KeyValuePair<string, string>> signInParams = new List<KeyValuePair<string, string>>();
            signInParams.Add(new KeyValuePair<string, string>("token", _userToken));

            RESTRequestObj req = new RESTRequestObj("/api/v1/user/info", signInParams);

            try
            {
                var responseObj = await req.SendGetRequest();


                FRTC_API_RESULT ret = FRTC_API_RESULT.SIGNIN_FAILED_UNKNOWN_ERROR;
                switch (responseObj.StatusCode)
                {
                    case System.Net.HttpStatusCode.OK:
                        ret = FRTC_API_RESULT.SIGNIN_SUCCESS_TOKEN;
                        break;
                    case System.Net.HttpStatusCode.Unauthorized:
                        ret = FRTC_API_RESULT.SIGNIN_FAILED_INVALID_TOKEN;
                        break;
                    case System.Net.HttpStatusCode.NotFound:
                    case System.Net.HttpStatusCode.RequestTimeout:
                        ret = FRTC_API_RESULT.CONNECTION_FAILED;
                        break;
                    default:
                        break;
                }
                OnSignInResult(ret, responseObj.Body);
            }
            catch (Exception ex)
            {
            }
        }

        public async void SignOut()
        {
            List<KeyValuePair<string, string>> signOutParams = new List<KeyValuePair<string, string>>();
            signOutParams.Add(new KeyValuePair<string, string>("token", _userToken));
            RESTRequestObj req = new RESTRequestObj("/api/v1/user/sign_out", signOutParams);
            var responseObj = await req.SendPostRequest();


            OnSignInResult(FRTC_API_RESULT.SIGNOUT_SUCCESS, string.Empty);
        }
        public async void GetUserMeetingRoom()
        {
            try
            {
                if (string.IsNullOrEmpty(_userToken))
                {
                    return;
                }
                List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
                meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));

                RESTRequestObj req = new RESTRequestObj("/api/v1/meeting_room", meetingParams);
                var responseObj = await req.SendGetRequest();

                if (responseObj.StatusCode == System.Net.HttpStatusCode.OK)
                    OnUserAPIResponsed(FRTC_API_RESULT.USER_ROOM_SUCCESS, responseObj.Body);
                else if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    OnUserAPIResponsed(FRTC_API_RESULT.API_SESSION_INVALID, responseObj.Body);
                else
                    OnUserAPIResponsed(FRTC_API_RESULT.USER_ROOM_FAILED, responseObj.Body);
            }
            catch { }
        }

        public async void QueryUsers(int pageNum, int pageSize, string filter)
        {
            try
            {
                if (string.IsNullOrEmpty(_userToken))
                {
                    return;
                }
                List<KeyValuePair<string, string>> meetingParams = new List<KeyValuePair<string, string>>();
                meetingParams.Add(new KeyValuePair<string, string>("token", _userToken));
                meetingParams.Add(new KeyValuePair<string, string>("page_num", pageNum.ToString()));
                meetingParams.Add(new KeyValuePair<string, string>("page_size", pageSize.ToString()));
                meetingParams.Add(new KeyValuePair<string, string>("filter", filter));

                RESTRequestObj req = new RESTRequestObj("/api/v1/user/public/users", meetingParams);
                var responseObj = await req.SendGetRequest();

                if (responseObj.StatusCode == System.Net.HttpStatusCode.OK)
                    OnUserAPIResponsed(FRTC_API_RESULT.QUERY_USER_SUCCESS, responseObj.Body);
                else if (responseObj.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    OnUserAPIResponsed(FRTC_API_RESULT.API_SESSION_INVALID, responseObj.Body);
                else
                    OnUserAPIResponsed(FRTC_API_RESULT.QUERY_USER_FAILED, responseObj.Body);
            }
            catch { }
        }
    }
}
