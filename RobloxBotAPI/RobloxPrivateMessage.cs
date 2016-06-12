using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RobloxBotAPI
{
    public class RobloxPrivateMessage
    {

        CookieAwareWebClient _Client;

        internal RobloxPrivateMessage(CookieAwareWebClient client)
        {
            _Client = client;
        }

        public string Subject
        {
            get;
            set;
        }

        public string Body
        {
            get;
            set;
        }

        public int RecipientId
        {
            get;
            set;
        }

        int maxSendIters = 0;

        private String _LastCSRFToken;

        public async Task<SendResult_t> Send()
        {
            HttpWebRequest request;
            if (String.IsNullOrWhiteSpace(_LastCSRFToken))
                request = _Client.GetWebRequest(new Uri("http://www.roblox.com/messages/send"));
            else
                request = _Client.GetWebRequest(new Uri("http://www.roblox.com/messages/send"), _LastCSRFToken);

            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));

            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("subject", Subject);
            outgoingQueryString.Add("body", Body);
            outgoingQueryString.Add("recipientid", RecipientId.ToString());
            outgoingQueryString.Add("cacheBuster", t.TotalSeconds.ToString());
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());

            if (maxSendIters >= RobloxBot.MAX_TRIES_WITH_CSRF_TOKEN)
            {
                maxSendIters = 0;
                return new SendResult_t(SendResult.InvalidXSRFToken);
            }
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            
            request.Expect = "application/json";

            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    SendResult resulte = (SendResult)(int)resp.StatusCode;
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();
                    SendResult_t res = JsonConvert.DeserializeObject<SendResult_t>(result);
                    res.Result = resulte;
                    maxSendIters = 0;
                    return res;
                }
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxSendIters++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                    }
                    catch { }
                }
                else
                    return new SendResult_t(SendResult.InternalServerError);
            }
            catch
            {
                return new SendResult_t(SendResult.Unknown);
            }

            return await Send();
        }

    }

    public struct SendResult_t
    {
        public bool success;
        public string shortMessage;

        [JsonIgnore]
        public SendResult Result;

        public SendResult_t(SendResult res)
        {
            success = false;
            shortMessage = "";

            Result = res;
        }
    }

    public enum SendResult
    {
        Success = 200,
        InvalidXSRFToken = 403,
        InternalServerError = 500,
        Unknown = -1
    }
}
