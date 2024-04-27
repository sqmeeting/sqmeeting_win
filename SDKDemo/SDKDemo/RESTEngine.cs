using SDKDemo.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SDKDemo.Model
{
    public class RESTRequestObj
    {
        public RESTRequestObj()
        {
            RequestURL = string.Empty;
            SpecifiedHeader = new List<KeyValuePair<string, string>>();
            ReqParams = new List<KeyValuePair<string, string>>();
            Body = string.Empty;
        }

        public RESTRequestObj(string url) : this()
        {
            if (url != null)
                RequestURL = url;
        }

        public RESTRequestObj(string url, List<KeyValuePair<string, string>> ReqParams) : this(url)
        {
            if (ReqParams != null)
                this.ReqParams = ReqParams;
        }

        public RESTRequestObj(string url, List<KeyValuePair<string, string>> ReqParams, string ReqBody) : this(url, ReqParams)
        {
            if (ReqBody != null)
                this.Body = ReqBody;
        }

        public RESTRequestObj(string url, List<KeyValuePair<string, string>> ReqParams, string ReqBody, List<KeyValuePair<string, string>> Headers) : this(url, ReqParams, ReqBody)
        {
            if (Headers != null)
                this.SpecifiedHeader = Headers;
        }

        public async Task<RESTResponseObj> SendPostRequest()
        {
            this.ReqParams.Add(new KeyValuePair<string, string>("client_id", FRTCUIUtils.GetFRTCDeviceUUID()));
            return await RESTEngine.DoPost(this);
        }

        public async Task<RESTResponseObj> SendGetRequest()
        {
            this.ReqParams.Add(new KeyValuePair<string, string>("client_id", FRTCUIUtils.GetFRTCDeviceUUID()));
            return await RESTEngine.DoGet(this);
        }

        public async Task<RESTResponseObj> SendPutRequest()
        {
            this.ReqParams.Add(new KeyValuePair<string, string>("client_id", FRTCUIUtils.GetFRTCDeviceUUID()));
            return await RESTEngine.DoPut(this);
        }

        public async Task<RESTResponseObj> SendDeleteRequest()
        {
            this.ReqParams.Add(new KeyValuePair<string, string>("client_id", FRTCUIUtils.GetFRTCDeviceUUID()));
            if (string.IsNullOrEmpty(this.Body))
                return await RESTEngine.DoDelete(this);
            else
                return await RESTEngine.DoDeleteWithBody(this);
        }

        private string _requestURL;
        public string RequestURL
        {
            get { return _requestURL; }
            set { _requestURL = value; }
        }

        private List<KeyValuePair<string, string>> _specifiedHeader;
        public List<KeyValuePair<string, string>> SpecifiedHeader
        {
            get { return _specifiedHeader; }
            set { _specifiedHeader = value; }
        }

        public List<KeyValuePair<string, string>> _reqParams;
        public List<KeyValuePair<string, string>> ReqParams
        {
            get { return _reqParams; }
            set { _reqParams = value; }
        }

        private string _body;
        public string Body
        {
            get { return _body; }
            set { _body = value; }
        }
    }

    public class RESTResponseObj
    {
        public HttpStatusCode StatusCode;
        public Dictionary<string, string> Header;
        public string Body;
        public string ReqUri;
    }
    public static class RESTEngine
    {
        static string _baseUrl = ConfigurationManager.AppSettings["FRTCServerAddress"];

        public static void SetBaseUrl(string BaseUrl)
        {
            _baseUrl = BaseUrl;
        }
        static RESTEngine()
        {
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
        }

        static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }

        private static HttpClient CreateRequestHttpClient(RESTRequestObj requestObj)
        {
            HttpClient client = new HttpClient();

            client.BaseAddress = new Uri("https://" + _baseUrl);

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "FrtcMeeting/" + "3.4.0" + " windows " + Environment.OSVersion.Version.ToString(3));

            foreach (var kv in requestObj.SpecifiedHeader)
            {
                client.DefaultRequestHeaders.Add(kv.Key, kv.Value);
            }

            client.Timeout = TimeSpan.FromSeconds(10);
            return client;
        }
        public static async Task<RESTResponseObj> DoPost(RESTRequestObj requestObj)
        {
            RESTResponseObj responseObj = new RESTResponseObj();
            try
            {
                using (var client = CreateRequestHttpClient(requestObj))
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    string reqStr = CommunicationUtils.MakeURLWithParams(requestObj.RequestURL, requestObj.ReqParams);
                    response = await client.PostAsync(reqStr, new StringContent(requestObj.Body, Encoding.UTF8, "application/json"));

                    responseObj.Body = response.Content.ReadAsStringAsync().Result;
                    responseObj.StatusCode = response.StatusCode;
                    responseObj.ReqUri = reqStr;
                }
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine(ex.Message);
                responseObj.StatusCode = HttpStatusCode.RequestTimeout;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                responseObj.StatusCode = HttpStatusCode.NotFound;
            }

            return responseObj;
        }

        public static async Task<RESTResponseObj> DoGet(RESTRequestObj requestObj)
        {
            RESTResponseObj responseObj = new RESTResponseObj();
            try
            {
                using (var client = CreateRequestHttpClient(requestObj))
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    string reqStr = Utilities.CommunicationUtils.MakeURLWithParams(requestObj.RequestURL, requestObj.ReqParams);
                    response = await client.GetAsync(reqStr);

                    responseObj.Body = response.Content.ReadAsStringAsync().Result; ;
                    responseObj.StatusCode = response.StatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                responseObj.StatusCode = HttpStatusCode.RequestTimeout;
            }

            catch (Exception e)
            {
                responseObj.StatusCode = HttpStatusCode.NotFound;
            }
            return responseObj;
        }

        public static async Task<RESTResponseObj> DoPut(RESTRequestObj requestObj)
        {
            RESTResponseObj responseObj = new RESTResponseObj();
            try
            {
                using (var client = CreateRequestHttpClient(requestObj))
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    string reqStr = Utilities.CommunicationUtils.MakeURLWithParams(requestObj.RequestURL, requestObj.ReqParams);
                    response = await client.PutAsync(reqStr, new StringContent(requestObj.Body, Encoding.UTF8, "application/json"));

                    responseObj.Body = response.Content.ReadAsStringAsync().Result; ;
                    responseObj.StatusCode = response.StatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                responseObj.StatusCode = HttpStatusCode.RequestTimeout;
            }

            catch (Exception e)
            {
                responseObj.StatusCode = HttpStatusCode.NotFound;
            }
            return responseObj;
        }

        public static async Task<RESTResponseObj> DoDelete(RESTRequestObj requestObj)
        {
            RESTResponseObj responseObj = new RESTResponseObj();
            try
            {
                using (var client = CreateRequestHttpClient(requestObj))
                {
                    HttpResponseMessage response = new HttpResponseMessage();
                    string reqStr = Utilities.CommunicationUtils.MakeURLWithParams(requestObj.RequestURL, requestObj.ReqParams);
                    response = await client.DeleteAsync(reqStr);
                    responseObj.Body = response.Content.ReadAsStringAsync().Result; ;
                    responseObj.StatusCode = response.StatusCode;
                }
            }
            catch (HttpRequestException ex)
            {
                responseObj.StatusCode = HttpStatusCode.RequestTimeout;
            }

            catch (Exception e)
            {
                responseObj.StatusCode = HttpStatusCode.NotFound;
            }
            return responseObj;
        }

        public static async Task<RESTResponseObj> DoDeleteWithBody(RESTRequestObj requestObj)
        {
            RESTResponseObj responseObj = new RESTResponseObj();
            try
            {
                string reqStr = Utilities.CommunicationUtils.MakeURLWithParams(requestObj.RequestURL, requestObj.ReqParams);
                HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://" + _baseUrl + reqStr);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.ContentLength = requestObj.Body.Length;
                httpWebRequest.UserAgent = "FrtcMeeting/" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(3) + " windows " + Environment.OSVersion.Version.ToString(3);
                foreach (var kv in requestObj.SpecifiedHeader)
                {
                    httpWebRequest.Headers.Add(kv.Key, kv.Value);
                }

                httpWebRequest.Timeout = 3000;
                httpWebRequest.Method = "DELETE";
                httpWebRequest.Accept = "application/json";
                using (System.IO.Stream reqStream = httpWebRequest.GetRequestStream())
                {
                    byte[] buffer = Encoding.UTF8.GetBytes(requestObj.Body);
                    await reqStream.WriteAsync(buffer, 0, buffer.Count());
                    await reqStream.FlushAsync();
                    reqStream.Close();
                }
                HttpWebResponse response = await httpWebRequest.GetResponseAsync() as HttpWebResponse;
                using (System.IO.Stream resStream = response.GetResponseStream())
                {
                    if (resStream != null && resStream.CanSeek && resStream.CanRead && resStream.Length > 0)
                    {
                        byte[] resBuffer = new byte[resStream.Length];
                        int nReadBytes = resStream.Read(resBuffer, 0, (int)resStream.Length);
                        responseObj.Body = Encoding.UTF8.GetString(resBuffer);
                    }
                    resStream?.Close();
                }
                responseObj.StatusCode = response.StatusCode;

            }
            catch (WebException ex)
            {
                if (ex.Status == WebExceptionStatus.Timeout)
                    responseObj.StatusCode = HttpStatusCode.RequestTimeout;
                else if (ex.Response != null)
                    responseObj.StatusCode = ((HttpWebResponse)ex.Response).StatusCode;
                else
                    responseObj.StatusCode = HttpStatusCode.Unused;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                responseObj.StatusCode = HttpStatusCode.Unused;
            }
            return responseObj;
        }
    }
}
