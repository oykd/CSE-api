using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;

using someApiSpace;

namespace API.huobi
{
    public class Huobi : someApi
    {
        public static readonly string apiName = "huobi";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        //Huobi special parameters
        public string host = String.Empty;
        public string signatureMethod = "HmacSHA256";
        public int signatureVersion = 2;

        public Huobi(string mykey, string mysecret, string myurl = "https://api.huobi.pro/", string myhost = "api.huobi.pro")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
            host = myhost;
        }

        private static string encryptSignature(string text, string secretKey)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return Convert.ToBase64String(hashmessage);
            }
        }

        private string GetHuobiCommonParameters()
        {
            return $"AccessKeyId={key}&SignatureMethod={signatureMethod}&SignatureVersion={signatureVersion}&Timestamp={DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss")}";
        }

        private string GetSignatureStr(string method, string host, string resourcePath, string parameters)
        {
            StringBuilder result = new StringBuilder();
            result.Append(method.ToUpper()).Append("\n").Append(host).Append("\n").Append(resourcePath).Append("\n");
            SortedDictionary<string, string> sortedDictionary = new SortedDictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in parameters.Split('&'))
                sortedDictionary.Add(item.Split('=').First(), item.Split('=').Last());
            foreach (var item in sortedDictionary)
                result.Append(item.Key).Append("=").Append(item.Value).Append("&");
            return urlEncode(encryptSignature(result.ToString().TrimEnd('&'), secret));
        }

        /// <summary>
        /// Send request by one string: "METHOD ENDPOINT PARAMETERS"
        /// </summary>    
        /// <param name="requestString">Request string :)</param>
        public string sendReq(string requestString)
        {
            try
            {
                string[] tmp = requestString.Trim(new char[] { '\t', ' ' }).Split(' ');
                return sendRequest(tmp[0], tmp[1], (tmp.Count() >= 3) ? tmp[2] : String.Empty);
            }
            catch (Exception) { throw new Exception("Incorrect request string"); }
        }

        /// <summary>
        /// Send request
        /// </summary>    
        /// <param name="method">POST/GET</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters string (key1=value1&key2=value2&...)</param>
        public string sendRequest(string method, string endpoint, string parameters)
        {
            method = method.ToUpper();
            string signatureResult;
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/39.0.2171.71 Safari/537.36");
            switch (method)
            {
                case "GET":
                    if (parameters != String.Empty) parameters += "&";
                    parameters = sortParametersString(parameters + GetHuobiCommonParameters(), true);
                    signatureResult = GetSignatureStr(method, host, endpoint, parameters);
                    parameters += "&Signature=" + signatureResult;
                    sendGetRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint}?{parameters}";
                case "POST":
                    string snout = GetHuobiCommonParameters();
                    //byte[] bytes = Encoding.Default.GetBytes(snout);
                    //snout = Encoding.UTF8.GetString(bytes);
                    snout = sortParametersString(snout, true);
                    signatureResult = GetSignatureStr(method, host, endpoint, snout);
                    snout += "&Signature=" + signatureResult;
                    sendSpecialPostRequest($"https://{host}{endpoint}?{snout}", parameters);
                    return $"{method} https://{host}{endpoint}?{snout} CONTENT:{parameters}";
                default:
                    throw new Exception("Method should be POST/GET. ");
            }
        }
    }
}
