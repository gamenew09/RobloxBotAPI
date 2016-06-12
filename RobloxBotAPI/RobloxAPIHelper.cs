using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI
{
    internal static class RobloxAPIHelper
    {

        internal static async Task<T> GetJsonObject<T>(string url, CookieContainer collection = null)
        {
            // http://www.roblox.com/api/groups/1/RoleSets/
            CookieAwareWebClient client;
            if (collection != null)
                client = new CookieAwareWebClient(collection);
            else
                client = new CookieAwareWebClient();
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
                T val = await JsonConvert.DeserializeObjectAsync<T>(result);
                return val;
            }
        }

    }
}
