using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Web; // -> link
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;

// * by onyokneesdog
// Last edit:  15.06.2018
namespace someApiSpace
{
    public class someApi : IDisposable
    {
        public string apiUrl = "";              // base address

        public int timeout = 30;                // timeout for request in seconds

        public string responseData = "";        // response string
                                                // starts with "!", if something goes wrong

        public bool waitingResponse = false;    // indicating flag for waiting response                                                
                                                // prevent the next request, if the previous one did not receive a response
                                                // unnecessary, could be easily deleted

        public string lang = String.Empty;      // It's just here, and I dont remember why
                                                // could be deleted

        public void Dispose() { }

        public delegate void ResponseReceivedDelegate(string response);
        public event ResponseReceivedDelegate OnResponseReceived = delegate { };

        public delegate void ResponseTimeoutDelegate(string response);
        public event ResponseTimeoutDelegate OnResponseTimeout = delegate { };

        #region Synchro Time
        // Get client-server time diffrence and put it into $nonceAmendment
        // Could be usefull rarely.. 
        /* 
         * Child class function should be like this:
            public override void synchronizeTime()
            {
                sendRequest(requestForServerTime, "", "GET");
                base.synchronizeTime();
            }
        */
        public Int64 nonceAmendment = 0;
        public string requestForServerTime = String.Empty;
        public string servertimeKey = String.Empty;
        private Thread synchronizeTimeThread = null;

        public virtual void synchronizeTime()
        {
            synchronizeTimeThread = new Thread(synchronizeTimeThreadLoop);
            synchronizeTimeThread.Priority = ThreadPriority.Highest;
            responseData = "";
            synchronizeTimeThread.Start();
        }

        private void synchronizeTimeThreadLoop()
        {
            try
            {
                while (waitingResponse)
                    Thread.Sleep(1);
                nonceAmendment = getIFS(responseData, servertimeKey) - Convert.ToInt64(getNonce());
                responseData = "Client-server time diffrence: " + nonceAmendment;
            }
            catch (ThreadAbortException)
            {
                responseData = "!Synchronization failed";
                synchronizeTimeThread = null;
            }
            finally
            {
                synchronizeTimeThread = null;
            }
        }
        #endregion

        #region Helpers

        /// <summary>
        /// Get nonce in milliseconds
        /// </summary>
        public string getNonce()
        {
            Int64 unixTimestamp = (Int64)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds + Convert.ToInt64(nonceAmendment);
            return unixTimestamp.ToString();
        }

        /// <summary>
        /// Get integer-type number from string after inner key-string
        /// </summary>
        /// <param name="s">Initial string</param>
        /// <param name="after">Key-string</param>
        private Int64 getIFS(string s, string after)
        {
            string r = string.Empty;
            bool key = false;
            for (int i = s.IndexOf(after) + after.Length; i < s.Length; i++)
                if (Char.IsDigit(s[i]))
                {
                    key = true;
                    r += s[i];
                }
                else if (key) break;
            if (r == "") r = "0";
            return Convert.ToInt64(r);
        }

        /// <summary>
        /// Sort parameters-string ("key1=value1&key2=value2&...") by inner keys and urlencode values (if indicated)
        /// </summary>
        /// <param name="s">Initial string</param>
        /// <param name="urlencode"> Urlencode value or not </param>
        public string sortParametersString(string s, bool urlencode = false)
        {
            var sortedDictionary = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in s.Split('&'))
            {
                var parameter = item.Split('=');
                if (urlencode)
                    sortedDictionary.Add(parameter.First(), urlEncode(parameter.Last()));
                else
                    sortedDictionary.Add(parameter.First(), parameter.Last());
            }
            var result = new StringBuilder();
            foreach (var item in sortedDictionary)
                result.Append(item.Key).Append("=").Append(item.Value).Append("&");
            return result.ToString().TrimEnd('&');
        }

        /// <summary>
        /// Urlencode string
        /// </summary>
        /// <param name="s">Initial string</param>
        public string urlEncode(string s)
        {
            StringBuilder builder = new StringBuilder();
            foreach (char c in s)
                if (HttpUtility.UrlEncode(c.ToString(), Encoding.UTF8).Length > 1)
                    builder.Append(HttpUtility.UrlEncode(c.ToString(), Encoding.UTF8).ToUpper());
                else
                    builder.Append(c);
            return builder.ToString();
        }
        #endregion

        #region Requests
        // Various requests
        // * Some of them are unnecessary and duplicates each other (just examples of different ways to do a same job). 

        /// <summary>
        /// Simple request (HttpWebRequest based)
        /// </summary>    
        /// <param name="uri">URI :)</param>
        public static string simpleRequest(string uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        /// <summary>
        /// Simple async request (HttpWebRequest based)
        /// </summary>    
        /// <param name="uri">URI :)</param>
        public async void simpleAsyncRequest(string uri)
        {
            if (waitingResponse) return;
            waitingResponse = true;
            responseData = "";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Timeout = timeout * 1000;
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                responseData = await reader.ReadToEndAsync();
                waitingResponse = false;
                OnResponseReceived(responseData);
            }
        }

        /// <summary>
        /// Standart post request (async, HttpClient based)
        /// </summary>    
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters (FormUrlEncodedContent)</param>
        /// <param name="headers">Headers</param>
        public async void sendPostRequest(string endpoint, string parameters, Dictionary<string, string> headers)
        {
            try
            {
                if (waitingResponse) return; //previous request not ended
                waitingResponse = true;
                responseData = "";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                using (var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(timeout) })
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    foreach (var item in headers)
                        httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                    var values = new Dictionary<string, string>();
                    foreach (var item in parameters.Split('&'))
                        values.Add(item.Split('=').First(), item.Split('=').Last());
                    var content = new FormUrlEncodedContent(values);
                    using (var response = await httpClient.PostAsync(endpoint, content))
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                        waitingResponse = false;
                        OnResponseReceived(responseData);
                    }
                }
            }
            catch (TaskCanceledException tcex)
            {
                responseData = "!Timeout exception: " + tcex.Message;
                waitingResponse = false;
                OnResponseTimeout(responseData);
            }
            catch (Exception ex)
            {
                responseData = "!System exception: " + ex.Message;
                waitingResponse = false;
            }
        }

        /// <summary>
        /// Get request (async, HttpClient based)
        /// </summary>    
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters (will be part of endpoint)</param>
        /// <param name="headers">Headers</param>
        public async void sendGetRequest(string endpoint, string parameters, Dictionary<string, string> headers)
        {
            try
            {
                if (waitingResponse) return; //previous request not ended
                waitingResponse = true;
                responseData = "";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                using (var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(timeout) })
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    foreach (var item in headers)
                        httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                    endpoint += $"?{parameters}";
                    using (var response = await httpClient.GetAsync(endpoint))
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                        waitingResponse = false;
                        OnResponseReceived(responseData);
                    }
                }
            }
            catch (TaskCanceledException tcex)
            {
                responseData = "!Timeout exception: " + tcex.Message;
                waitingResponse = false;
                OnResponseTimeout(responseData);
            }
            catch (Exception ex)
            {
                responseData = "!System exception: " + ex.Message;
                waitingResponse = false;
            }
        }

        /// <summary>
        /// Delete request (async, HttpClient based)
        /// </summary>    
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters (will be part of endpoint)</param>
        /// <param name="headers">Headers</param>
        public async void sendDeleteRequest(string endpoint, string parameters, Dictionary<string, string> headers)
        {
            try
            {
                if (waitingResponse) return; //previous request not ended
                waitingResponse = true;
                responseData = "";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                using (var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(timeout) })
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    foreach (var item in headers)
                        httpClient.DefaultRequestHeaders.Add(item.Key, item.Value);
                    var values = new Dictionary<string, string>();
                    foreach (var item in parameters.Split('&'))
                        values.Add(item.Split('=').First(), item.Split('=').Last());
                    HttpRequestMessage request = new HttpRequestMessage
                    {
                        Content = new FormUrlEncodedContent(values),
                        Method = HttpMethod.Delete,
                        RequestUri = new Uri(apiUrl.TrimEnd('/') + endpoint)
                    };
                    using (var response = await httpClient.SendAsync(request))
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                        waitingResponse = false;
                        OnResponseReceived(responseData);
                    }
                }
            }
            catch (TaskCanceledException tcex)
            {
                responseData = "!Timeout exception: " + tcex.Message;
                waitingResponse = false;
                OnResponseTimeout(responseData);
            }
            catch (Exception ex)
            {
                responseData = "!System exception: " + ex.Message;
                waitingResponse = false;
            }
        }

        /// <summary>
        /// Special post request (async, HttpWebRequest based)
        /// </summary>    
        /// <param name="url">Full address (with apiUrl)</param>
        /// <param name="parameters">Parameters (convert to json)</param>
        /// <param name="headers">Headers</param>
        public async void sendSpecialPostRequest(string url, string parameters, Dictionary<string, string> headers = null)
        {
            try
            {
                if (waitingResponse) return; //previous request not ended
                waitingResponse = true;
                responseData = "";
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
                string data = String.Empty;
                foreach (var item in parameters.Split('&'))
                    data += $"\"{item.Split('=').First()}\": \"{item.Split('=').Last()}\",";
                data = "{" + data.TrimEnd(',') + "}";
                byte[] dataBytes = Encoding.UTF8.GetBytes(data);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.ContentLength = dataBytes.Length;
                request.ContentType = "application/json";
                request.Method = "POST";
                request.Timeout = timeout * 1000;
                if (headers != null)
                    foreach (var item in headers)
                        request.Headers[item.Key] = item.Value;
                using (Stream requestBody = request.GetRequestStream())
                    await requestBody.WriteAsync(dataBytes, 0, dataBytes.Length);
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    responseData = await reader.ReadToEndAsync();
                    waitingResponse = false;
                    OnResponseReceived(responseData);
                }
            }
            catch (TaskCanceledException tcex)
            {
                responseData = "!Timeout exception: " + tcex.Message;
                waitingResponse = false;
                OnResponseTimeout(responseData);
            }
            catch (Exception ex)
            {
                responseData = "!System exception: " + ex.Message;
                waitingResponse = false;
            }
        }

        public async void sendAuthRequest(string method, string endpoint, string data, string login, string pass)
        {
            try
            {
                if (waitingResponse) return; //previous request not ended
                waitingResponse = true;
                responseData = "";
                using (var httpClient = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromSeconds(timeout) })
                {
                    HttpMethod reqMethod = HttpMethod.Post;
                    switch (method)
                    {
                        case "POST":    reqMethod = HttpMethod.Post;    break;
                        case "GET":     reqMethod = HttpMethod.Get;     break;
                        case "PUT":     reqMethod = HttpMethod.Put;     break;
                        case "DELETE":  reqMethod = HttpMethod.Delete;  break;
                    }
                    var request = new HttpRequestMessage(reqMethod, endpoint);
                    var byteArray = new UTF8Encoding().GetBytes($"{login}:{pass}");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                    if (data != String.Empty)
                        request.Content = new StringContent(data, Encoding.UTF8, "application/json");
                    using (var response = await httpClient.SendAsync(request))
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                        waitingResponse = false;
                        OnResponseReceived(responseData);
                    }
                }
            }
            catch (TaskCanceledException tcex)
            {
                responseData = "!Timeout exception: " + tcex.Message;
                waitingResponse = false;
                OnResponseTimeout(responseData);
            }
            catch (Exception ex)
            {
                responseData = "!System exception: " + ex.Message;
                waitingResponse = false;
            }
        }
        #endregion
    }
}
