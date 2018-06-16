using System;
using System.Collections.Generic;
using System.Linq;

using System.Security.Cryptography;

using someApiSpace;

namespace API.gate
{
    public class Gate : someApi
    {
        public static readonly string apiName = "gate";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        public Gate(string mykey, string mysecret, string myurl = "http://data.gate.io/api2/")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
        }

        private string encryptSignature(string secret, string strForSign)
        {
            string signatureStr = strForSign;
            var hash = new HMACSHA512(System.Text.Encoding.UTF8.GetBytes(secret));
            var comhash = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(signatureStr));
            return BitConverter.ToString(comhash).Replace("-", "").ToLower();
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
            Dictionary<string, string> headers = new Dictionary<string, string>();
            switch (method)
            {
                case "POST":
                    string nonce = getNonce() + "000";
                    parameters += (parameters != String.Empty) ? "&nonce=" + nonce : "nonce=" + nonce;
                    headers.Add("KEY", key);
                    headers.Add("SIGN", encryptSignature(secret, parameters));
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
