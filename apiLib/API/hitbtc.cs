using System;
using System.Collections.Generic;
using System.Linq;

using someApiSpace;

namespace API.hitbtc
{
    public class Hitbtc : someApi
    {
        public static readonly string apiName = "";
        private readonly string key = String.Empty;
        private readonly string secret = String.Empty;

        public Hitbtc(string mykey, string mysecret, string myurl = "https://api.hitbtc.com/")
        {
            key = mykey;
            secret = mysecret;
            apiUrl = myurl;
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
        /// <param name="method">GET/AUTH-POST/AUTH-GET/AUTH-PUT/AUTH-DELETE</param>
        /// <param name="endpoint">Endpoint</param>
        /// <param name="data">json string like: {"symbol":"ethbtc","side":"sell","quantity":0.063,"price":0.046016}</param>
        public string sendRequest(string method, string endpoint, string data)
        {
            method = method.ToUpper();
            if (method != "AUTH-POST" && method != "AUTH-GET" && method != "AUTH-PUT" && method != "AUTH-DELETE" && method != "GET")
                throw new Exception("Method should be AUTH-POST/AUTH-GET/AUTH-PUT/AUTH-DELETE/GET. ");
            bool auth = (method.StartsWith("AUTH")) ? true : false;
            method = method.Split('-').Last();
            Dictionary<string, string> headers = new Dictionary<string, string>();
            // + Adding some headers to request +
            if (auth)
            {
                sendAuthRequest(method, endpoint, data, key, secret);
                return $"{method} {apiUrl}{endpoint} CONTENT:{data}";
            }
            else
            {
                sendGetRequest(endpoint, String.Empty, headers);
                return $"{method} {apiUrl}{endpoint}";
            }
        }
    }
}
