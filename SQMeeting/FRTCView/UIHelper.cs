using SQMeeting.Properties;
using SQMeeting.ViewModel;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Documents;

namespace SQMeeting.FRTCView
{
    public class UIHelper
    {
        public static string GetMeetingInvitationText(string InvitorName, string MeetingName, string StartTime, string MeetingNumber, string Password, bool isRecurrence, string EndTime = "", string JoinURL = "")
        {
            StringBuilder bd = new StringBuilder();
            if (string.IsNullOrEmpty(InvitorName))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITING_YOU);
            }
            else
            {
                bd.Append(InvitorName + " " + Properties.Resources.FRTC_MEETING_SDKAPP_INVITING_YOU_LOWER);
            }
            bd.Append(Environment.NewLine);
            //bd.Append(Environment.NewLine);
            bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_TOPIC + " ");
            bd.Append(MeetingName);
            bd.Append(Environment.NewLine);
            //bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(StartTime))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_START_TIME + " ");
                bd.Append(StartTime);
                bd.Append(Environment.NewLine);
            }
            //bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(EndTime))
            {
                //No end time currently
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_END_TIME + " ");
                bd.Append(EndTime);
                bd.Append(Environment.NewLine);
                //bd.Append(Environment.NewLine);
            }

            //bd.Append(Environment.NewLine);
            bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGID_COLON + " ");
            bd.Append(MeetingNumber);
            bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(Password))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGPASSCODE + ": ");
                bd.Append(Password);
                bd.Append(Environment.NewLine);
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITATION_WITHPWD);
            }
            else
            {
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITATION);
            }
            if (!string.IsNullOrEmpty(JoinURL))
            {
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_INVITE_URL + Environment.NewLine);
                bd.Append(JoinURL);
            }
            return bd.ToString();
        }

        public static string DayOfWeekIntegerToFriendlyName(int dayOfWeekInt, int startFrom = 1, bool sundayIsFirst = true)
        {
            string ret = string.Empty;
            int d = dayOfWeekInt - startFrom;
            if (d == 0)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_SUN_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_MON_LONG;
            }
            else if (d == 1)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_MON_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_TUE_LONG;
            }
            else if (d == 2)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_TUE_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_WED_LONG;
            }
            else if (d == 3)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_WED_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_THU_LONG;
            }
            else if (d == 4)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_THU_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_FRI_LONG;
            }
            else if (d == 5)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_FRI_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_SAT_LONG;
            }
            else if (d == 6)
            {
                ret = sundayIsFirst ? Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_SAT_LONG : Properties.Resources.FRTC_SDKAPP_SCHEDULE_DAY_SUN_LONG;
            }
            return ret;
        }

        public static string GetMeetingInvitationText(string InvitorName, ScheduledMeetingDislpayData data)
        {
            StringBuilder bd = new StringBuilder();
            if (string.IsNullOrEmpty(InvitorName))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITING_YOU);
            }
            else
            {
                bd.Append(InvitorName + " " + Properties.Resources.FRTC_MEETING_SDKAPP_INVITING_YOU_LOWER);
            }
            bd.Append(Environment.NewLine);
            //bd.Append(Environment.NewLine);
            bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_TOPIC + " ");
            bd.Append(data.MeetingName);
            bd.Append(Environment.NewLine);
            //bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(data.BeginTime))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_START_TIME + " ");
                string strBegin = string.Empty;
                DateTime begintime = GetUTCDateTimeFromUTCTimestamp(long.Parse(data.BeginTime)).ToLocalTime();

                strBegin = begintime.ToString("yyyy-MM-dd HH:mm");
                bd.Append(strBegin);
                bd.Append(Environment.NewLine);
            }
            //bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(data.EndTime))
            {
                //No end time currently
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETING_END_TIME + " ");
                string strEnd = string.Empty;
                DateTime endtime = GetUTCDateTimeFromUTCTimestamp(long.Parse(data.EndTime)).ToLocalTime();

                strEnd = endtime.ToString("yyyy-MM-dd HH:mm");

                bd.Append(strEnd);
                bd.Append(Environment.NewLine);
                //bd.Append(Environment.NewLine);
            }

            if (data.IsRecurringMeeting)
            {
                bd.Append(Resources.FRTC_SDKAPP_RECURRENCE + ": ");
                long recurringBeginDate = data.RecurringBeginDate;
                long recurringEndDate = data.RecurringEndDate;
                string period = GetUTCDateTimeFromUTCTimestamp(recurringBeginDate).ToLocalTime().ToString("yyyy-MM-dd") + " - " + GetUTCDateTimeFromUTCTimestamp(recurringEndDate).ToLocalTime().ToString("yyyy-MM-dd");

                long begin_time =
                    (data.RecurringMeetingGroup != null && data.RecurringMeetingGroup.Count() > 0) ?
                    data.RecurringMeetingGroup[0].recurrenceStartTime.Value
                    : long.Parse(data.BeginTime);


                long end_time =
                    (data.RecurringMeetingGroup != null && data.RecurringMeetingGroup.Count() > 0) ?
                    data.RecurringMeetingGroup[0].recurrenceEndTime.Value
                    : long.Parse(data.EndTime);

                period += ",";// + GetUTCDateTimeFromUTCTimestamp(begin_time).ToLocalTime().ToString("HH:mm") + " - " + GetUTCDateTimeFromUTCTimestamp(end_time).ToLocalTime().ToString("HH:mm") + ", ";
                if (data.RecurringType == MeetingRecurring.Daily)
                {
                    period += Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + " " + data.RecurringFrequency.ToString() + " " + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_DAY;
                }
                else if (data.RecurringType == MeetingRecurring.Weekly)
                {
                    period += Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + " " + data.RecurringFrequency.ToString() + " " + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_WEEK;
                    string days = string.Empty;
                    var daysList = data.RecurringDaysOfWeek.ToList();
                    daysList.Sort();
                    daysList.ForEach((d) => days += DayOfWeekIntegerToFriendlyName(d) + Resources.FRTC_SDKAPP_STR_SEPERATE);
                    period += " (" + days.TrimEnd(Resources.FRTC_SDKAPP_STR_SEPERATE[0]) + ")";
                }
                else if (data.RecurringType == MeetingRecurring.Monthly)
                {
                    period += Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_EVERY + " " + data.RecurringFrequency.ToString() + " " + Resources.FRTC_SDKAPP_MEASURE_WORD + Resources.FRTC_SDKAPP_SCHEDULE_RECURRING_MONTH;
                    string days = string.Empty;
                    var daysList = data.RecurringDaysOfMonth.ToList();
                    daysList.Sort();
                    daysList.ForEach((d) => days += d.ToString() + Resources.FRTC_SDKAPP_STR_SEPERATE);
                    period += " (" + days.TrimEnd(Resources.FRTC_SDKAPP_STR_SEPERATE[0]) + Resources.FRTC_SDKAPP_SCHEDULE_DAY_IN_MONTH + ")";
                }
                bd.Append(period);
                bd.Append(Environment.NewLine);
            }

            //bd.Append(Environment.NewLine);
            bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGID_COLON + " ");
            bd.Append(data.MeetingNumber);
            bd.Append(Environment.NewLine);
            if (!string.IsNullOrEmpty(data.Password))
            {
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_MEETINGPASSCODE + ": ");
                bd.Append(data.Password);
                bd.Append(Environment.NewLine);
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITATION_WITHPWD);
            }
            else
            {
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_SDKAPP_INVITATION);
            }
            if (!data.IsRecurringMeeting && !string.IsNullOrEmpty(data.MeetingUrl))
            {
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_INVITE_URL + Environment.NewLine);
                bd.Append(data.MeetingUrl);
            }
            else if (data.IsRecurringMeeting && !string.IsNullOrEmpty(data.GroupMeetingUrl))
            {
                bd.Append(Environment.NewLine);
                bd.Append(Properties.Resources.FRTC_MEETING_INVITE_URL + Environment.NewLine);
                bd.Append(data.GroupMeetingUrl);
            }
            return bd.ToString();
        }

        public static DateTime GetUTCDateTimeFromUTCTimestamp(long timestamp, bool ignoreSeconds = false)
        {
            var ret = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromMilliseconds(timestamp));
            if (ignoreSeconds)
            {
                ret = ret.Subtract(TimeSpan.FromSeconds(ret.Second)).Subtract(TimeSpan.FromMilliseconds(ret.Millisecond));
            }
            return ret;
        }

        public static string GetResourceCultureName(string culture = "")
        {
            string cultureName = string.IsNullOrEmpty(culture) ? Thread.CurrentThread.CurrentUICulture.Name : culture;
            string resourceCultureName = "en-US";
            switch (cultureName)
            {
                case "ar":
                case "ar-001":
                case "ar-AE":
                case "ar-BH":
                case "ar-DJ":
                case "ar-DZ":
                case "ar-EG":
                case "ar-ER":
                case "ar-IL":
                case "ar-IQ":
                case "ar-JO":
                case "ar-KM":
                case "ar-KW":
                case "ar-LB":
                case "ar-LY":
                case "ar-MA":
                case "ar-MR":
                case "ar-OM":
                case "ar-PS":
                case "ar-QA":
                case "ar-SA":
                case "ar-SD":
                case "ar-SO":
                case "ar-SS":
                case "ar-SY":
                case "ar-TD":
                case "ar-TN":
                case "ar-YE":
                    //resourceCultureName = "ar";
                    break;

                case "zh":
                case "zh-CN":
                case "zh-Hans":
                case "zh-Hans-HK":
                case "zh-Hans-MO":
                case "zh-SG":
                case "zh-CHS":
                    resourceCultureName = "zh-CHS";
                    break;

                case "zh-Hant":
                case "zh-HK":
                case "zh-MO":
                case "zh-TW":
                case "zh-CHT":
                    resourceCultureName = "zh-CHT";
                    break;

                case "de":
                case "de-AT":
                case "de-BE":
                case "de-CH":
                case "de-DE":
                case "de-LI":
                case "de-LU":
                    //resourceCultureName = "de";
                    break;

                case "en-GB":
                    //resourceCultureName = "en-GB";
                    break;

                case "en-US":
                    //resourceCultureName = "en-US";
                    break;

                case "es":
                case "es-419":
                case "es-AR":
                case "es-BO":
                case "es-CL":
                case "es-CO":
                case "es-CR":
                case "es-CU":
                case "es-DO":
                case "es-EC":
                case "es-ES":
                case "es-GQ":
                case "es-GT":
                case "es-HN":
                case "es-MX":
                case "es-NI":
                case "es-PA":
                case "es-PE":
                case "es-PH":
                case "es-PR":
                case "es-PY":
                case "es-SV":
                case "es-US":
                case "es-UY":
                case "es-VE":
                    //resourceCultureName = "es";
                    break;

                case "fr":
                case "fr-029":
                case "fr-BE":
                case "fr-BF":
                case "fr-BI":
                case "fr-BJ":
                case "fr-BL":
                case "fr-CA":
                case "fr-CD":
                case "fr-CF":
                case "fr-CG":
                case "fr-CH":
                case "fr-CI":
                case "fr-CM":
                case "fr-DJ":
                case "fr-DZ":
                case "fr-FR":
                case "fr-GA":
                case "fr-GF":
                case "fr-GN":
                case "fr-GP":
                case "fr-GQ":
                case "fr-HT":
                case "fr-KM":
                case "fr-LU":
                case "fr-MA":
                case "fr-MC":
                case "fr-MF":
                case "fr-MG":
                case "fr-ML":
                case "fr-MQ":
                case "fr-MR":
                case "fr-MU":
                case "fr-NC":
                case "fr-NE":
                case "fr-PF":
                case "fr-PM":
                case "fr-RE":
                case "fr-RW":
                case "fr-SC":
                case "fr-SN":
                case "fr-SY":
                case "fr-TD":
                case "fr-TG":
                case "fr-TN":
                case "fr-VU":
                case "fr-WF":
                case "fr-YT":
                    //resourceCultureName = "fr";
                    break;

                case "hu":
                case "hu-HU":
                    //resourceCultureName = "hu";
                    break;

                case "it":
                case "it-CH":
                case "it-IT":
                case "it-SM":
                    //resourceCultureName = "it";
                    break;

                case "ja":
                case "ja-JP":
                    //resourceCultureName = "ja";
                    break;

                case "ko":
                case "ko-KP":
                case "ko-KR":
                    //resourceCultureName = "ko";
                    break;

                case "nb":
                case "nb-NO":
                case "nb-SJ":
                case "nn":
                case "nn-NO":
                case "no":
                case "no-NO":// ??
                    //resourceCultureName = "no";
                    break;

                case "pl":
                case "pl-PL":
                    //resourceCultureName = "pl";
                    break;

                case "pt":
                case "pt-AO":
                case "pt-BR":
                case "pt-CV":
                case "pt-GW":
                case "pt-MO":
                case "pt-MZ":
                case "pt-PT":
                case "pt-ST":
                case "pt-TL":
                    //resourceCultureName = "pt-BR";
                    break;

                case "ru":
                case "ru-BY":
                case "ru-KG":
                case "ru-MD":
                case "ru-RU":
                case "ru-UA":
                    //resourceCultureName = "ru";
                    break;

                default:
                    resourceCultureName = "en-US";
                    break;

            }
            return resourceCultureName;
        }
    }

    public static class DateTimeHelper
    {
        public static DateTime Tomorrow
        {
            get { return DateTime.Today.AddDays(1); }
        }

        public static DateTime DayAfterTomorrow
        {
            get { return DateTime.Today.AddDays(1); }
        }

        public static DateTime TodayInNextYear
        {
            get { return DateTime.Today.AddYears(1).AddDays(-1); }
        }
    }
}
