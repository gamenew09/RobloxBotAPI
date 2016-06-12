using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RobloxBotAPI.JsonResult;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI
{
    public static class RobloxAPI
    {

        static HttpWebRequest GetWebRequest(string api)
        {
            CookieAwareWebClient client = new CookieAwareWebClient();
            string url = String.Format(RobloxBot.ROBLOX_API_URL, api);
            HttpWebRequest request = client.GetWebRequest(new Uri(url));
            request.ContentLength = 0;
            return request;
        }

        public static async Task<GroupRank[]> GetGroupRanks(int groupId)
        {
            // http://www.roblox.com/api/groups/1/RoleSets/
            CookieAwareWebClient client = new CookieAwareWebClient();
            string url = String.Format("http://www.roblox.com/api/groups/{0}/RoleSets/", groupId);
            HttpWebRequest request = client.GetWebRequest(new Uri(url));
            request.Method = "GET";
            request.ContentLength = 0;

            using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = await reader.ReadToEndAsync();
                dataStream.Dispose();
                reader.Dispose();
                GroupRank[] ranks = JsonConvert.DeserializeObject<List<GroupRank>>(result).ToArray();
                GroupRank[] ranksF = new GroupRank[ranks.Length];
                for(int i = 0; i < ranks.Length; i++)
                {
                    GroupRank rank = ranks[i];
                    rank.GroupID = groupId;
                    ranksF[i] = rank;
                }
                return ranksF;
            }
        }

        public static async Task<RobloxUser> GetUser(int userId)
        {
            RobloxUser_t user = new RobloxUser_t();

            HttpWebRequest req = GetWebRequest(String.Format("users/{0}", userId));

            using(HttpWebResponse resp = (HttpWebResponse)await req.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = await reader.ReadToEndAsync();
                user = JsonConvert.DeserializeObject<RobloxUser_t>(result);
            }

            return new RobloxUser(user);
        }

        public static async Task<GroupRank> GetUserRankInGroup(int userId, int groupId)
        {
            CookieAwareWebClient client = new CookieAwareWebClient();
            string url = String.Format("http://www.roblox.com/Game/LuaWebService/HandleSocialRequest.ashx?method=GetGroupRank&playerid={0}&groupid={1}", userId, groupId);
            HttpWebRequest request = client.GetWebRequest(new Uri(url));
            request.Method = "GET";
            request.ContentLength = 0;

            using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                int rankNum = int.Parse((await reader.ReadToEndAsync()).Replace("<Value Type=\"integer\">", "").Replace("</Value>", ""));
                dataStream.Dispose();
                reader.Dispose();
                foreach(GroupRank rank in (await GetGroupRanks(groupId)))
                    if (rank.Rank == rankNum)
                        return rank;
            }
            return null;
        }
        

    }

    public class GroupRank
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Rank { get; set; }

        public int GroupID { get; set; }
    }


    public class CookieAwareWebClient
    {
        public CookieContainer CookieContainer { get; set; }

        public CookieAwareWebClient()
            : this(new CookieContainer())
        {
        }

        public CookieAwareWebClient(CookieContainer cookies)
        {
            this.CookieContainer = cookies;
        }

        public HttpWebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(address);
            (request as HttpWebRequest).CookieContainer = this.CookieContainer;
            HttpWebRequest httpRequest = (HttpWebRequest)request;
            httpRequest.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            return httpRequest;
        }

        /// <summary>
        /// Get's the web request, with including a csrf-token just in case.
        /// </summary>
        /// <param name="address">The API to call.</param>
        /// <param name="csrfToken">The csrf token to validate, server side.</param>
        /// <returns>The request to use for calling the api.</returns>
        public HttpWebRequest GetWebRequest(Uri address, string csrfToken)
        {
            HttpWebRequest request = GetWebRequest(address);
            request.Headers.Add("X-Csrf-Token", csrfToken);
            return request;
        }
    }
}
