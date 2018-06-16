using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;

using someApiSpace;

namespace API.binance
{
    public class Binance : someApi
    {
        public static readonly string apiName = "binance";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        public Binance(string mykey, string mysecret, string myurl = "https://api.binance.com/")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
        }

        private string encryptSignature(string secret, string strForSign)
        {
            string signatureStr = strForSign;
            var hash = new HMACSHA256(System.Text.Encoding.UTF8.GetBytes(secret));
            var comhash = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureStr));
            return BitConverter.ToString(comhash).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Get client-server time difference and put it into $nonceAmendment; 
        /// $nonceAmendment will be used in getNonce()
        /// </summary> 
        public override void synchronizeTime()
        {
            sendRequest("GET", requestForServerTime, "!");
            base.synchronizeTime();
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
            catch (Exception)
            {
                throw new Exception("Incorrect request string");
            }
        }

        /// <summary>
        /// Send request
        /// </summary>    
        /// <param name="method">POST/GET/DELETE</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters string (key1=value1&key2=value2&...); 
        ///     if parameters starts with "!", signature and timestamp will not be added </param>
        public string sendRequest(string method, string endpoint, string parameters)
        {
            method = method.ToUpper();
            string nonce = getNonce();
            if (parameters.StartsWith("!"))
                parameters = parameters.TrimStart('!');
            else
            {
                if (parameters != "") parameters += "&";
                parameters += "timestamp=" + nonce;
                parameters += "&signature=" + encryptSignature(secret, parameters);
            }
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("X-MBX-APIKEY", key);
            switch (method)
            {
                case "POST":
                    sendPostRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint} CONTENT:{parameters}";
                case "GET":
                    sendGetRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint}?{parameters}";
                case "DELETE":
                    sendDeleteRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint} CONTENT:{parameters}";
                default:
                    throw new Exception("Method should be POST/GET/DELETE. ");
            }
        }
    }
}
