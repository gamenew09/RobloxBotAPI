using HtmlAgilityPack;
using Newtonsoft.Json;
using RobloxBotAPI.JsonResult;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Reflection;
using Awesomium.Core;

namespace RobloxBotAPI
{

    /// <summary>
    /// Roblox Bot Class, allows you to act as a user.
    /// NOTE: Some calls might be called twice or more (which could cause an infinite recursion loop) because of a XSRF error since we don't get a token until we actually call an api with support for it.
    /// </summary>
    public class RobloxBot : IDisposable
    {

        public const String ROBLOX_API_URL = "https://api.roblox.com/{0}";
        public const int MAX_TRIES_WITH_CSRF_TOKEN = 10;

        private ObjectCache<CurrencyBalance_t> _CurrencyCache = new ObjectCache<CurrencyBalance_t>(false);

        public int Robux
        {
            get 
            {
                _CurrencyCache.TryUpdate();
                return _CurrencyCache.Object.robux;
            }
        }

        public async void ForceUpdateRobuxCache()
        {
            await Task.Run(() =>
                {
                    _CurrencyCache.Update();
                });
        }

        private CookieAwareWebClient _WebClient;

        // This would get passed to all requests as a
        private String _LastCSRFToken;

        internal RobloxBot()
        {
            _WebClient = new CookieAwareWebClient();
            _CurrencyCache.CacheLength = 60 * 2f;
            _CurrencyCache.UpdateCache += (sender, ev) =>
                {
                    HttpWebRequest request = GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "currency/balance")));
                    request.Method = "GET";
                    request.ContentLength = 0;
                    request.Expect = "application/json";

                    using (HttpWebResponse resp = (HttpWebResponse)request.GetResponse())
                    {
                        Stream dataStream = resp.GetResponseStream();
                        StreamReader reader = new StreamReader(dataStream);
                        string result = reader.ReadToEnd();
                        ev.Object = JsonConvert.DeserializeObject<CurrencyBalance_t>(result);
                    }
                };
        }

        async Task<HttpWebResponse> GetResponse(string api)
        {
            return (HttpWebResponse)await GetWebRequest(api).GetResponseAsync();
        }

        HttpWebRequest GetWebRequest(string api)
        {
            string url = String.Format(ROBLOX_API_URL, api);
            HttpWebRequest request;
            if (String.IsNullOrWhiteSpace(_LastCSRFToken))
                request = _WebClient.GetWebRequest(new Uri(url));
            else
                request = _WebClient.GetWebRequest(new Uri(url), _LastCSRFToken);
            request.ContentLength = 0;
            return request;
        }

        public RobloxPrivateMessage CreatePrivateMessage()
        {
            return new RobloxPrivateMessage(_WebClient);
        }

        int maxSignoutIters = 0;

        /// <summary>
        /// Signs the bot out of the user.
        /// Should never return InvalidXSRFToken, but will return it in the worse case scenario.
        /// </summary>
        /// <returns>The result of the sign out.</returns>
        public async Task<SignoutResult> Signout()
        {
            if (maxSignoutIters >= MAX_TRIES_WITH_CSRF_TOKEN)
            {
                maxSignoutIters = 0;
                _LastCSRFToken = "";
                return SignoutResult.InvalidXSRFToken;
            }
            HttpWebRequest request = GetWebRequest("sign-out/v1");
            request.Method = "POST";
            try
            {
                using(HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    SignoutResult result = (SignoutResult)(int)resp.StatusCode;
                    maxSignoutIters = 0;
                    _LastCSRFToken = "";
                    return result;
                }
            }
            catch(WebException ex)
            {
                if(((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxSignoutIters++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                    }
                    catch { }
                }
                else
                { 
                    _LastCSRFToken = ""; 
                    return SignoutResult.InternalServerError; 
                }
    
            }
            catch
            {
                _LastCSRFToken = "";
                return SignoutResult.Unknown;
            }
            
            return await Signout();
        }

        /*
            try
            {
                _LastCSRFToken = resp.Headers.Get("X-Csrf-Token");
            }
            catch { }
        */

        public async Task<PrivateMessage[]> GetMessages(int messageTab = 0, int pageNumber=0, int pageSize=20)
        {
            List<PrivateMessage> pms = new List<PrivateMessage>();
            HttpWebRequest request = _WebClient.GetWebRequest(new Uri(String.Format("https://www.roblox.com/messages/api/get-messages?messageTab=0&pageNumber=0&pageSize=20", messageTab, pageNumber, pageSize)));
            request.Method = "GET";
            request.ContentLength = 0;
            request.Expect = "application/json";

            using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = await reader.ReadToEndAsync();
                PrivateMessages_t msgs = JsonConvert.DeserializeObject<PrivateMessages_t>(result);
                foreach(Message_t msg in msgs.Collection)
                    pms.Add(new PrivateMessage(msg));
                return pms.ToArray();
            }
        }

        int maxRequestFriendshipIter = 0;
        int maxRequestUnfriend = 0;
        int maxRequestMarkRead = 0;


        public enum MarkAsReadResult
        {
            Success,
            StatusCodeFailure,
            MaxTriesReached,
            Unknown
        }

        public async Task<MarkAsReadResult> MarkAsRead(int messageId)
        {
            // https://www.roblox.com/messages/api/mark-messages-read
            // {"messageIds":[0000]}
            String json = "{\"messageIds\":[" + messageId + "]}";
            Console.WriteLine(json);
            HttpWebRequest request;
            if (String.IsNullOrWhiteSpace(_LastCSRFToken))
                request = _WebClient.GetWebRequest(new Uri("http://www.roblox.com/messages/send"));
            else
                request = _WebClient.GetWebRequest(new Uri("http://www.roblox.com/messages/send"), _LastCSRFToken);

            if (maxRequestMarkRead >= RobloxBot.MAX_TRIES_WITH_CSRF_TOKEN)
            {
                maxRequestMarkRead = 0;
                return MarkAsReadResult.MaxTriesReached;
            }

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(json);

            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = data.Length;

            request.Expect = "application/json";

            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();

            WebHeaderCollection headers = request.Headers;

            for (int i = 0; i < headers.Count; ++i)
            {
                string header = headers.GetKey(i);
                foreach (string value in headers.GetValues(i))
                {
                    Console.WriteLine("{0}: {1}", header, value);
                }
            }

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();
                    
                    maxRequestMarkRead = 0;
                    _LastCSRFToken = "";
                    return MarkAsReadResult.Success;
                }
            }
            catch (WebException ex)
            {
                Console.WriteLine(((HttpWebResponse)ex.Response).StatusCode);
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxRequestMarkRead++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                        Console.WriteLine(_LastCSRFToken);
                    }
                    catch { }
                }
                else
                {
                    headers = ex.Response.Headers;

                    for (int i = 0; i < headers.Count; ++i)
                    {
                        string header = headers.GetKey(i);
                        foreach (string value in headers.GetValues(i))
                        {
                            Console.WriteLine("{0}: {1}", header, value);
                        }
                    }
                    maxRequestMarkRead = 0;
                    using (StreamReader reader = new StreamReader(ex.Response.GetResponseStream()))
                        Console.WriteLine("Raw Response: {0}", reader.ReadToEnd());
                    _LastCSRFToken = "";
                    return MarkAsReadResult.StatusCodeFailure;
                }
            }
            catch
            {
                maxRequestMarkRead = 0;
                _LastCSRFToken = "";
                return MarkAsReadResult.Unknown;
            }

            return await MarkAsRead(messageId);
            
        }

        public async Task<MarkAsReadResult> MarkAsRead(PrivateMessage message)
        {
            return await MarkAsRead(message.Id);
        }

        public async Task<bool> AwardBadge(int userId, int badgeId, int placeId)
        {
            List<PrivateMessage> pms = new List<PrivateMessage>();
            HttpWebRequest request = _WebClient.GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "assets/award-badge")));
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("userId", userId.ToString());
            outgoingQueryString.Add("badgeId", badgeId.ToString());
            outgoingQueryString.Add("placeId", placeId.ToString());
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();

                    Console.WriteLine(result);

                    if (result.Contains("award") && result.Contains("won"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex); }
            return false;
        }

        /// <summary>
        /// Unfriends a user.
        /// </summary>
        /// <param name="userId">The user to unfriend.</param>
        /// <returns>The result of the request.</returns>
        public async Task<GenericResult_t> Unfriend(int userId)
        {
            if (maxRequestUnfriend > MAX_TRIES_WITH_CSRF_TOKEN)
            {
                maxRequestUnfriend = 0;
                return new GenericResult_t(GenericResultEnum.InvalidXSRFToken);
            }
            HttpWebRequest request = GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "user/unfriend")));
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("friendUserId", userId.ToString());
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.Expect = "application/json";

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();
                    maxRequestUnfriend = 0;
                    return JsonConvert.DeserializeObject<GenericResult_t>(result);
                }
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxRequestUnfriend++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                    }
                    catch { }
                }
                else
                {
                    return new GenericResult_t(GenericResultEnum.InternalServerError);
                }
            }
            catch
            {
                return new GenericResult_t(GenericResultEnum.Unknown);
            }

            return await Unfriend(userId);
        }

        HttpWebRequest GetWebRequest(Uri url)
        {
            if (String.IsNullOrWhiteSpace(_LastCSRFToken))
                return _WebClient.GetWebRequest(url);
            else
                return _WebClient.GetWebRequest(url, _LastCSRFToken);
        }

        /// <summary>
        /// Requests a friendship between the bot and another user.
        /// </summary>
        /// <param name="userId">The user to request a friendship with.</param>
        /// <returns>A object that tells the success of the API call.</returns>
        public async Task<GenericResult_t> RequestFriendship(int userId)
        {
            if(maxRequestFriendshipIter > MAX_TRIES_WITH_CSRF_TOKEN)
            {
                _LastCSRFToken = "";
                maxRequestFriendshipIter = 0;
                return new GenericResult_t(GenericResultEnum.InvalidXSRFToken);
            }
            HttpWebRequest request = GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "user/request-friendship")));
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("recipientUserId", userId.ToString());
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.Expect = "application/json";
            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();
                    maxRequestFriendshipIter = 0;
                    _LastCSRFToken = "";
                    return JsonConvert.DeserializeObject<GenericResult_t>(result);
                }
            }
            catch (WebException ex)
            {
                Stream dataStream = ((HttpWebResponse)ex.Response).GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = reader.ReadToEnd();
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxRequestFriendshipIter++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                    }
                    catch { }
                }
                else
                    return new GenericResult_t(GenericResultEnum.InternalServerError);
            }
            catch
            {
                return new GenericResult_t(GenericResultEnum.Unknown);
            }

            return await RequestFriendship(userId);
        }

        /// <summary>
        /// Logs in using a username and password. Please be careful with this, even if we use https you might be in risk of losing your account. Keep that in mind.
        /// 
        /// See:
        /// <see cref="RobloxBotAPI.JsonResult.LoginResult"/>
        /// </summary>
        /// <param name="username">The username to log under.</param>
        /// <param name="password">The password to use in login.</param>
        /// <returns>A asynchronous task to login, which returns <see cref="RobloxBotAPI.JsonResult.LoginResult"/>.</returns>
        public static async Task<LoginResult> Login(string username, string password)
        {
            RobloxBot bot = new RobloxBot();

            HttpWebRequest request = bot._WebClient.GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "login/v1")));
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("username", username);
            outgoingQueryString.Add("password", password);
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.Expect = "application/json";
            
            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();

            using( HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = await reader.ReadToEndAsync();
                LoginResult_t res;
                
                if(!String.IsNullOrWhiteSpace(result))
                    res = JsonConvert.DeserializeObject<LoginResult_t>(result);
                else
                    res = new LoginResult_t();

                res.ReturnCode = (int)resp.StatusCode;

                LoginResult realResult = new LoginResult(res);
                realResult.Bot = bot;

                Thread t = new Thread(() =>
                {
                    bot.ForceUpdateRobuxCache();
                });
                t.Start();

                reader.Close();
                dataStream.Close();
                return realResult;
            }
        }

        // Based off of http://stackoverflow.com/questions/6324810/using-webbrowser-in-a-console-application and http://stackoverflow.com/a/516599
        // Right now this isn't useful since the code snippet I used still didn't even work with it. :(
        async Task<string> CreateWebBrowser(string url, CookieContainer container = null, Func<WebView, bool> ShouldReturnFunc = null)
        {
            bool done = false;
            string result = "";
            SynchronizationContext awesomiumContext = null;
            Thread t2 = new Thread(() =>
            {
                WebCore.Started += (s, e) =>
                {
                    awesomiumContext = SynchronizationContext.Current;
                };
                WebCore.Run();
            });
            t2.Start();
            Thread t = new Thread(() =>
            {
                while (awesomiumContext == null)
                    Thread.Sleep(100);
                awesomiumContext.Post(state =>
                {
                    WebView view = WebCore.CreateWebView(640, 480);
                    if (container != null)
                        foreach (Cookie c in container.GetCookies(new Uri(url)))
                            view.WebSession.SetCookie(new Uri(url), String.Format("{0}={1};", c.Name, c.Value), c.HttpOnly, true); // (yn) Crossing Fingers
                    view.Source = new Uri(url);
                    view.LoadingFrameComplete += (sender, ev) =>
                    {
                        if (view == null || !view.IsLive || !ev.IsMainFrame)
                            return;
                        
                        if (ShouldReturnFunc != null)
                        {
                            while(!ShouldReturnFunc(view))
                            {
                                Thread.Sleep(100);
                            }
                        }
                        result = view.HTML;
                        done = true;
                        view.Dispose();
                    };
                }, null);

                while (!done)
                    Thread.Sleep(100);
            });
            //t.SetApartmentState(ApartmentState.STA);
            t.Start();
            while(done != true)
                await Task.Delay(1);
            return result;
        }

        /// <summary>
        /// Get's a captcha for a user.
        /// Unknown if it works at the moment, use with caution. Might also throw an exception too.
        /// </summary>
        /// <param name="username">The username that was used to log in, this might not be necessary.</param>
        /// <returns>The captcha that was found.</returns>
        public static async Task<Captcha> GetCaptchaForUser(string username)
        {
            Captcha captcha = new Captcha();
            CookieAwareWebClient client = new CookieAwareWebClient();
            HttpWebRequest request = client.GetWebRequest(new Uri("https://www.roblox.com/newlogin"));

            // TODO: Add Post Data.

            using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
            {
                Stream dataStream = resp.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string result = await reader.ReadToEndAsync();
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(result);
                foreach(HtmlNode node in doc.DocumentNode.SelectNodes("//script[@src]"))
                {
                    if (node.Attributes["src"] != null && node.Attributes["src"].Value.StartsWith("https://www.google.com/recaptcha/api/challenge?k="))
                    {
                        captcha.ChallengeKey = node.Attributes["src"].Value.Replace("https://www.google.com/recaptcha/api/challenge?k=", "");
                    }
                }
                HtmlNode n = doc.GetElementbyId("recaptcha_challenge_image");
                captcha.Image = n.Attributes["src"].Value;
            }
            return captcha;
        }


        public void Dispose()
        {
            Thread t = new Thread(() =>
            {
                Task<SignoutResult> task = Signout();
                while (!task.IsCompleted)
                    Thread.Sleep(1);
            });
            t.Start();
        }
    }

    public static class CookieContainerExtensions
    {
        public static CookieCollection GetAllCookies(this CookieContainer container)
        {
            var allCookies = new CookieCollection();
            var domainTableField = container.GetType().GetRuntimeFields().FirstOrDefault(x => x.Name == "m_domainTable");
            var domains = (IDictionary)domainTableField.GetValue(container);

            foreach (var val in domains.Values)
            {
                var type = val.GetType().GetRuntimeFields().First(x => x.Name == "m_list");
                var values = (IDictionary)type.GetValue(val);
                foreach (CookieCollection cookies in values.Values)
                {
                    allCookies.Add(cookies);
                }
            }
            return allCookies;
        }

        public static void CopyCookieContainer(this WebSession session, CookieContainer container)
        {
            foreach (Cookie c in container.GetAllCookies())
                session.SetCookie(new Uri(c.Domain), String.Format("{0}={1};", c.Name, c.Value), c.HttpOnly, true); // (yn) Crossing Fingers
        }
    }

#region Broken Snippets
    /*
     * 
        
     */
#endregion
}
