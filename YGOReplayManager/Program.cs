using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YGOReplayManager
{
    class Program
    {
        static void Main(string[] args)
        {
            //Usage
            if (args.Length < 3 || args[0] == "--help" || args[0] == "-h")
            {
                OutputUsage();
                return;
            }
            //Default args
            string userName = args[0];
            string password = args[1];
            string serverLink = args[2];
            if (!serverLink.StartsWith("https://") && !serverLink.StartsWith("http://"))
            {
                serverLink = "http://" + serverLink;
            }
            string port = args.Length > 3 ? args[3] : "7211";

            //Parse args
            DateTime startTime = DateTime.ParseExact("1970/01/01,00:00:00", "yyyy/MM/dd,HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
            DateTime endTime = DateTime.ParseExact("2099/12/31,23:59:59", "yyyy/MM/dd,HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
            if (args.Length > 4)
            {
                if (!DateTime.TryParseExact(args[4], "yyyy/MM/dd,HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None,out startTime))
                {
                    OutputUsage();
                    return;
                }
            }
            if (args.Length > 5)
            {
                if (!DateTime.TryParseExact(args[5], "yyyy/MM/dd,HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out endTime))
                {
                    OutputUsage();
                    return;
                }
            }

            //Get JSON
            string replayJson = null;
            try
            {
                string link = $"{serverLink}:{port}/api/duellog?username={userName}&pass={password}";
                replayJson = HttpGet(link);
            }
            catch
            {
                OutputError("Can't reach server");
                return;
            }

            if (string.IsNullOrEmpty(replayJson))
            {
                OutputError("Wrong Json format");
                return;
            }

            if (replayJson == "[{name:'密码错误'}]")
            {
                OutputError("Wrong password");
                return;
            }
            JArray output = new JArray();
            try
            {
                JArray json = JArray.Parse(replayJson);
                
                foreach (JObject log in json) //A duel log, includes time,name,roomid...
                {
                    DateTime dt = DateTime.ParseExact(log["time"].ToString(), "yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.None);
                    if (dt < startTime || dt > endTime) //Not in specified time
                    {
                        continue;
                    }
                    if (!log["name"].ToString().StartsWith("M#")) //Not Match duel
                    {
                        continue;
                    }
                    JObject outlog = new JObject();
                    int index = 1;
                    foreach (JObject player in log["players"])
                    {
                        outlog.Add($"player{index}", GetRealName(player["name"]));
                        outlog.Add($"score{index}", GetScore(player["name"]));
                        index++;
                    }
                    output.Add(outlog);
                }
            }
            catch (Exception ex)
            {
                OutputError($"Wrong Json format:{ex.ToString()}");
                return;
            }
            Console.Write(output.ToString());
        }
        /// <summary>
        /// Get player's score from the "name" JToken(actually a string)
        /// </summary>
        /// <param name="jToken">The JToken to parse</param>
        /// <returns>The player's score</returns>
        private static JToken GetScore(JToken jToken)
        {
            string[] split = jToken.ToString().Split('('); //Score:1 LP:8000 Cards:7)
            int index = split.Length - 1;
            string[] propertySplit = split[index].Split(' ');
            string[] scoreSplit = propertySplit[0].Split(':');
            return int.Parse(scoreSplit[1]);
        }

        /// <summary>
        /// Get player's real name from the "name" JToken(actually a string)
        /// </summary>
        /// <param name="jToken">The JToken to parse</param>
        /// <returns>The player's real name</returns>
        private static JToken GetRealName(JToken jToken)
        {
            string[] split = jToken.ToString().Split('(');
            if (split.Length > 2)
            {
                int len = split.Length - 1;
                for (int i = 1; i < len; i++)
                {
                    if (split[i].StartsWith("IP:")) //repaired by Nanahira; improved by JoyJ
                    {
                        break;
                    }
                    split[0] += "(";
                    split[0] += split[i];
                }
            }
            return split[0].Trim();
        }
        /// <summary>
        /// Output error json
        /// </summary>
        /// <param name="error">Error message</param>
        private static void OutputError(string error)
        {
            JArray arr = new JArray();
            JObject jobj = new JObject();
            jobj.Add("error", error);
            arr.Add(jobj);
            Console.WriteLine(arr.ToString());
        }
        /// <summary>
        /// Output the usage of the program
        /// </summary>
        private static void OutputUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("yrpmanager username password serverlink [port=7211] [starttime=1970/01/01,00:00:00] [endtime=2099/12/31,23:59:59]");
            Console.WriteLine("If you're using this program on a old type srvpro(no username), just input anything for the username and it'll works well.");
        }

        public static string HttpGet(string Url)
        {
            //取文件
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();

            return retString;
        }
    }
}
