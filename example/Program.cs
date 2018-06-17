using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;

using API.kucoin;
using API.binance;
using API.gate;
using API.huobi;

using API.hitbtc;

namespace example
{

    #region unnecessary helpers
    // Auxiliary functions for work with strings (not about API)
    // could be deleted
    static class helpers
    {
        public static Int64 getIFS(this string s, string after)
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

        public static string buildString(this string[] arr, string jumper = "", int first = 0, int last = -1)
        {
            StringBuilder r = new StringBuilder();
            if (last == -1) last = arr.Count() - 1;
            for (int i = first; i <= last; i++)
            {
                r.Append(arr[i]);
                if (i != last) r.Append(jumper);
            }
            return r.ToString();
        }

        public static string[] spacedFromJson(this string someJson)
        {
            List<string> r = new List<string>();
            string t = "", tb = "";
            for (int i = 0; i < someJson.Length; i++)
            {
                if (("{}[]").Contains(someJson[i]) && r.Count > 0)
                {
                    r.Add(tb + t);
                    t = "";
                }
                t = t + someJson[i];
                if ((",{[").Contains(someJson[i])) 
                {
                    r.Add(tb + t);
                    t = "";
                }
                if (("{[").Contains(someJson[i])) tb = tb + "\t";
                if (("}]").Contains(someJson[i]) && tb.Length > 0) tb = tb.Substring(0, tb.Length - 1);
            }
            r.Add(tb + t);
            return r.ToArray();
        }

        public enum way : byte { straight, reverse };
        public static double getInnerFloat(this string s, string after = "", way w = way.straight)
        {
            if (!s.Contains(after)) return 0;
            int z = 1;
            string arg = String.Empty;
            string exp = "0";
            bool key = false;
            int sx = (after != "") ? ((w == way.straight) ? s.IndexOf(after) : s.LastIndexOf(after)) : ((w == way.straight) ? 0 : s.Length - 1);
            int d = (w == way.straight) ? 1 : -1;
            int start = (w == way.straight) ? sx + after.Length : sx;
            int finish = (w == way.straight) ? s.Length : -1;
            for (int i = start; i != finish; i += d)
            {
                if ((Char.IsDigit(s[i])) || (s[i] == '.'))
                {
                    key = true;
                    if (w == way.straight && i > start && s[i - 1] == '-') arg = "-" + arg;
                    arg = (w == way.straight) ? arg + s[i] : s[i] + arg;
                }
                else if (s[i] == '-' && w == way.reverse)
                {
                    if (arg != String.Empty) arg = "-" + arg;
                }
                else if (s[i] == 'E' && w == way.reverse)
                {
                    exp = arg;
                    arg = String.Empty;
                }
                else if (s[i] == 'E')
                {
                    exp = "";
                    int k = 0;
                    if (s[i + 1] == '-')
                    {
                        z = -1;
                        k = 1;
                    }
                    for (int j = i + 1 + k; j < s.Length; j++)
                        if (Char.IsDigit(s[j])) exp += s[j]; else break;
                    break;
                }
                else if (key) break;
            }
            if (arg == String.Empty) arg = "0";
            return Convert.ToDouble(arg.Replace('.', ',')) * Math.Pow(10, Convert.ToInt32(exp) * z);
        }

        public static readonly char[] quotes = { '"', '\'' };
        public static readonly char[] commonTrim = { ' ', '\t', '\n' };
        public static readonly char[] jsonValueBegin = { ':' };
        public static readonly char[] jsonValueEnd = { ',', '}' };
        public static readonly char jsonArrayBegin = '[';
        public static readonly char jsonArrayEnd = ']';
        public static int countChar(this string s, char c)
        {
            int r = 0;
            for (int i = 0; i < s.Length; i++)
                if (s[i] == c) r++;
            return r;
        }
        public static string getJsonValueAfterKeys(this string someJson, string[] keys, bool useQuotes = true)
        {
            if (keys.Count() == 1 && keys[0] == String.Empty || keys.Count() == 0) return someJson;
            int pos = 0;
            foreach (var key in keys)
            {
                switch (useQuotes)
                {
                    case true:
                        int t = -1;
                        foreach (var sym in quotes)
                        {
                            t = someJson.IndexOf("" + sym + key + sym, pos);
                            if (t >= 0)
                            {
                                t += ("" + sym + key + sym).Length;
                                break;
                            }
                        }
                        pos = t;
                        break;
                    case false:
                        pos = someJson.IndexOf(key, pos);
                        if (pos >= 0) pos += key.Length;
                        break;
                }
                if (pos < 0) return String.Empty;
            }

            StringBuilder r = new StringBuilder();
            bool flag = false;
            bool inArray = false;
            string brackets = "";
            char q = ' ';
            for (int i = pos; i < someJson.Length; i++)
            {
                //quotes
                if (quotes.Contains(someJson[i]))
                    if (q == ' ') q = someJson[i]; else if (q == someJson[i]) q = ' ';
                //in array?
                if (q == ' ' && (someJson[i] == jsonArrayBegin || someJson[i] == jsonArrayEnd)) brackets += someJson[i];
                inArray = (brackets.countChar('[') > brackets.countChar(']')) ? true : false;
                //Start/Finish/Append
                if (q == ' ' && jsonValueBegin.Contains(someJson[i]) && !inArray) flag = true;
                else if (q == ' ' && jsonValueEnd.Contains(someJson[i]) && !inArray) break;
                else if (flag) r.Append(someJson[i]);
            }
            return r.ToString().Trim(commonTrim);
        }
    }
    #endregion

    class Program
    {
        #region Thread Example
        // Thread-Loop pattern: Refresh BTC price from various Stock Exchanges simultaneously
        private static bool isSomeStockWaiting(dynamic[] stockExchanges)
        {
            bool flag = false;
            foreach (var stock in stockExchanges)
                flag |= stock.waitingResponse;
            return flag;
        }

        static Thread exampleThread;
        private static void exampleThreadLoop()
        {
            try
            {
                Console.WriteLine(" >>> Thread test >>> ");
                Console.WriteLine(" * get BTC price from various Stock Exchanges simultaneously (*10)");
                var binance = new Binance(String.Empty, String.Empty);
                var kucoin = new Kucoin(String.Empty, String.Empty);
                var huobi = new Huobi(String.Empty, String.Empty);
                var gate = new Gate(String.Empty, String.Empty);
                for (int i = 0; i < 10; i++)
                {
                    // Get OrderBooks
                    binance.sendReq("GET /api/v1/depth !symbol=BTCUSDT&limit=50");
                    kucoin.sendReq("GET /v1/open/orders symbol=BTC-USDT");
                    huobi.sendReq("GET /market/depth symbol=btcusdt&type=step1");
                    gate.sendReq("GET 1/orderBook/btc_usdt");
                    while (isSomeStockWaiting(new dynamic[] { binance, kucoin, huobi, gate }))
                        Thread.Sleep(1);
                    // Get best Purchase price for BTC from OrderBooks
                    string result = $"{i}. ";
                    result += $"Binance:{binance.responseData.getJsonValueAfterKeys(new string[] { "asks" }).getInnerFloat().ToString("0.00")} ";
                    result += $"Kucoin:{kucoin.responseData.getJsonValueAfterKeys(new string[] { "data", "SELL" }).getInnerFloat().ToString("0.00")} ";
                    result += $"Huobi:{huobi.responseData.getJsonValueAfterKeys(new string[] { "asks" }).getInnerFloat().ToString("0.00")} ";
                    result += $"Gate:{gate.responseData.getJsonValueAfterKeys(new string[] { "asks" }).getInnerFloat(",", helpers.way.reverse).ToString("0.00")} ";
                    Console.WriteLine(result);
                }
                Console.WriteLine($" >>> End >>> ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                exampleThread = null;
            }
        }
        #endregion

        static void Main(string[] args)
        {
            try
            {
                Console.SetBufferSize(200, 1000);

                #region Console Text
                Console.WriteLine(" >>> Crypto Stock Exchanges console >>> ");
                Console.WriteLine("");
                Console.WriteLine("Set current Api-Key: \"key key-string\"");
                Console.WriteLine("Set current Api-Secret: \"secret secret-string\"");
                Console.WriteLine("Format of api-request: \"apiName method endpoint parameters\"");
                Console.WriteLine("Launch thread example: \"cycle\"");
                Console.WriteLine("Set nonce amendment: \"amendment IntValue\"");
                Console.WriteLine("");
                Console.WriteLine(" Examples: ");

                Console.WriteLine("binance GET /api/v3/ticker/price !symbol=BTCUSDT");
                Console.WriteLine("binance DELETE /api/v3/order symbol=ETHUSDT&orderId=1");
                Console.WriteLine("binance GET /api/v1/depth !symbol=ETHUSDT&limit=50");
                Console.WriteLine("binance POST /api/v3/order symbol=ETHUSDT&timeInForce=GTC&side=BUY&type=LIMIT&quantity=1&price=1");

                Console.WriteLine("gate GET 1/orderBook/eth_usdt");
                Console.WriteLine("gate POST 1/private/buy currencyPair=eth_usdt&amount=1&rate=1");
                Console.WriteLine("gate POST 1/private/balances");

                Console.WriteLine("huobi GET /market/trade symbol=btcusdt");
                Console.WriteLine("huobi GET /v1/order/orders symbol=ethusdt&states=filled");

                Console.WriteLine("kucoin GET /v1/open/currencies");
                Console.WriteLine("kucoin GET /v1/account/USDT/balance");
                Console.WriteLine("kucoin POST /v1/order?symbol=ETH-USDT amount=1&price=1&type=BUY");

                Console.WriteLine("hitbtc GET /api/2/public/orderbook/BTCUSD?limit=5");
                Console.WriteLine("hitbtc AUTH-GET /api/2/trading/balance");
                Console.WriteLine("hitbtc AUTH-POST /api/2/order {\"symbol\":\"ethbtc\",\"side\":\"sell\",\"quantity\":0.063,\"price\":0.046016}");

                Console.WriteLine("");
                Console.WriteLine("Enter empty string for exit");
                #endregion

                string line;

                // Set your api-key & api-secret here (for signature-type requests) or use console
                string key = String.Empty;
                string secret = String.Empty;   

                string response = String.Empty;
                Int64 noncAm = 0;

                do
                {
                    line = Console.ReadLine();
                    var parts = line.Trim(new char[] { '\t', ' ' } ).Split(' ');
                    parts[0] = parts[0].ToLower();
                    string content = parts.buildString(" ", 1);
                    switch (parts[0])
                    {
                        case "amendment":
                            noncAm = Convert.ToInt64(content);
                            break;
                        case "cycle":
                            exampleThread = new Thread(exampleThreadLoop);
                            exampleThread.Priority = ThreadPriority.Highest;
                            exampleThread.Start();
                            break;
                        case "key":
                            key = content;
                            break;
                        case "secret":
                            secret = content;
                            break;
                        case "binance":
                            using (var binance = new Binance(key, secret))
                            {
                                binance.OnResponseReceived += Api_OnResponseReceived;
                                binance.nonceAmendment = noncAm;
                                binance.sendReq(content);
                            }
                            break;
                        case "gate":
                            using (var gate = new Gate(key, secret))
                            {
                                gate.OnResponseReceived += Api_OnResponseReceived;
                                gate.sendReq(content);
                            }
                            break;
                        case "huobi":
                            using (var huobi = new Huobi(key, secret))
                            {
                                huobi.OnResponseReceived += Api_OnResponseReceived;
                                huobi.sendReq(content);
                            }
                            break;
                        case "kucoin":
                            using (var kucoin = new Kucoin(key, secret))
                            {
                                kucoin.OnResponseReceived += Api_OnResponseReceived;
                                kucoin.nonceAmendment = noncAm;
                                kucoin.sendReq(content);
                            }
                            break;
                        case "hitbtc":
                            using (var hitbtc = new Hitbtc(key, secret))
                            {
                                hitbtc.OnResponseReceived += Api_OnResponseReceived;
                                hitbtc.sendReq(content);
                            }
                            break;
                        default:
                            Console.WriteLine("Wrong command");
                            break;
                    }

                } while (line.Length > 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.ReadLine();
            }
        }

        private static void Api_OnResponseReceived(string response)
        {
            //Console.WriteLine(response);
            var lines = response.spacedFromJson();
            Console.WriteLine();
            Console.WriteLine("RESPONSE: ");
            foreach (var item in lines)
                Console.WriteLine(item);
            Console.WriteLine("RESPONSE END");
            Console.WriteLine();
        }
    }
}
