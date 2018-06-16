using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;

using someApiSpace;

namespace API.kucoin
{
    public class Kucoin : someApi
    {
        public static readonly string apiName = "kucoin";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        public Kucoin(string mykey, string mysecret, string myurl = "https://api.kucoin.com/")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
        }

        private string encryptSignature(string secret, string strForSign)
        {
            string signatureStr = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(strForSign));
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
            sendRequest("GET", requestForServerTime, String.Empty);
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
        /// <param name="method">POST/GET</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="parameters">Parameters string (key1=value1&key2=value2&...)</param>
        public string sendRequest(string method, string endpoint, string parameters)
        {
            method = method.ToUpper();
            string nonce = getNonce();
            string strForSign = endpoint + "/" + nonce + "/" + parameters;
            string signatureResult = encryptSignature(secret, strForSign);
            Dictionary<string, string> headers = new Dictionary<string, string>();
            headers.Add("KC-API-KEY", key);
            headers.Add("KC-API-NONCE", nonce);
            headers.Add("KC-API-SIGNATURE", signatureResult);
            headers.Add("Accept-Language", lang); //"zh_CN" - chinese, "en_US" - english
            switch (method)
            {
                case "POST":
                    sendPostRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint} CONTENT:{parameters}";
                case "GET":
                    sendGetRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint}?{parameters}";
                default:
                    throw new Exception("Method should be POST/GET. ");
            }
        }
    }
}


