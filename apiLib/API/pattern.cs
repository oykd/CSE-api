using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Security.Cryptography;

using someApiSpace;

// Child-class pattern 
namespace API.sample
{
    class Sample : someApi
    {
        public static readonly string apiName = "";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        public Sample(string mykey, string mysecret, string myurl = "https://sample/")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
        }

        // Typical encryption
        private static string encryptSignature(string text, string secretKey)
        {
            using (var hmacsha256 = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey)))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return Convert.ToBase64String(hashmessage);
            }
        }

        // Build & encrypt signature
        private string GetSignatureStr()
        {
            var result = new StringBuilder();
            // + Building signature string + 
            return urlEncode(encryptSignature(result.ToString(), secret));
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
                for (int i = 3; i < tmp.Count(); i++)
                    tmp[2] += (i < tmp.Count() - 1) ? tmp[i] + " " : tmp[i];
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
            Dictionary<string, string> headers = new Dictionary<string, string>();
            // + Adding some headers to request +
            switch (method)
            {
                case "GET":
                    // + Parameters manipulations +
                    // + Signature calculation +
                    sendGetRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint}?{parameters}";
                case "POST":
                    // + Parameters manipulations +
                    // + Signature calculation +
                    sendPostRequest(endpoint, parameters, headers);
                    return $"{method} {apiUrl}{endpoint} CONTENT:{parameters}";
                default:
                    throw new Exception("Method should be POST/GET. ");
            }
        }
    }
}
