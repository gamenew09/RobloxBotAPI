using HtmlAgilityPack;
using Newtonsoft.Json;
using RobloxBotAPI.Event;
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
using WebBrowserReadyState = System.Windows.Forms.WebBrowserReadyState;
using System.Reflection;
using Awesomium.Windows.Forms;
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

        private CookieAwareWebClient _WebClient;

        // This would get passed to all requests as a
        private String _LastCSRFToken;

        internal RobloxBot()
        {
            _WebClient = new CookieAwareWebClient();
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

        int maxRequestFriendshipIter = 0;

        public async Task<GenericResult_t> Unfriend(int userId)
        {
            if (maxRequestFriendshipIter > MAX_TRIES_WITH_CSRF_TOKEN)
            {
                maxRequestFriendshipIter = 0;
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
            Console.WriteLine("Data: {0}", postdata);

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
                    maxSignoutIters = 0;
                    return JsonConvert.DeserializeObject<GenericResult_t>(result);
                }
            }
            catch (WebException ex)
            {
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
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
            Console.WriteLine("RequestFriendship {0}", String.Format(ROBLOX_API_URL, "user/request-friendship"));
            HttpWebRequest request = GetWebRequest(new Uri(String.Format(ROBLOX_API_URL, "user/request-friendship")));
            NameValueCollection outgoingQueryString = HttpUtility.ParseQueryString(String.Empty);
            outgoingQueryString.Add("recipientUserId", String.Format("{0}", userId));
            string postdata = outgoingQueryString.ToString();
            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] data = ascii.GetBytes(postdata.ToString());
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = data.Length;
            request.Expect = "application/json";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/50.0.2661.102 Safari/537.36";
            Console.WriteLine(request.UserAgent);
            Console.WriteLine("RequestFriendship made {0}", postdata);
            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(data, 0, data.Length);
            postStream.Flush();
            postStream.Close();

            try
            {
                Console.WriteLine("RequestFriendship sending");
                using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                {
                    Console.WriteLine("RequestFriendship gotten");
                    Stream dataStream = resp.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    string result = await reader.ReadToEndAsync();
                    Console.WriteLine("RequestFriendship Result: {0}", result);
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
                Console.WriteLine(result);
                if (((HttpWebResponse)ex.Response).StatusCode == (HttpStatusCode)403)
                {
                    maxRequestFriendshipIter++;
                    try
                    {
                        _LastCSRFToken = ex.Response.Headers.Get("X-Csrf-Token");
                        Console.WriteLine(_LastCSRFToken);
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

                reader.Close();
                dataStream.Close();
                return realResult;
            }
        }

        public bool EventsEnabled
        {
            get;
            private set;
        }

        private int _RefreshRate;

        Thread eventThread;

        /// <summary>
        /// Detects events every [number] seconds. Please do not set this to a low number otherwise ROBLOX will not like it and slow down.
        /// NOTE: It will not update during a Thread.Sleep, so it'll wait the Thread.Sleep amount then next sleep it'll pull from this value.
        /// </summary>
        public int RefreshRate
        {
            get { return _RefreshRate; }
            set { _RefreshRate = value; }
        }

        public void DisableEvents()
        {
            if (!EventsEnabled)
                return;
            EventsEnabled = false;
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
                    Console.WriteLine(view.IsJavascriptEnabled);
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


        public void EnableEvents()
        {
            if (EventsEnabled)
                return;
            EventsEnabled = true;

            eventThread = new Thread(() =>
            {
                while(EventsEnabled)
                {

                    Thread.Sleep(RefreshRate * 1000);
                }
            });
            eventThread.Start();
        }

        /// <summary>
        /// Get's a captcha for a user.
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
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
                Console.WriteLine("Signout tasked completed in Dispose: {0}", task.Result);
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
        public async Task<MessageRecievedEvent[]> GetMessages()
        {
            /*
            HttpWebRequest request;
            if (String.IsNullOrWhiteSpace(_LastCSRFToken))
                request = _WebClient.GetWebRequest(new Uri("https://www.roblox.com/my/messages/#!/inbox"));
            else
                request = _WebClient.GetWebRequest(new Uri("https://www.roblox.com/my/messages/#!/inbox"), _LastCSRFToken);
            request.ContentLength = 0;
            request.Method = "GET";
            /

            bool saved = false;

            HtmlDocument doc = new HtmlDocument();
            string result = await CreateWebBrowser("https://www.roblox.com/my/messages/#!/inbox", _WebClient.CookieContainer,
                new Func<WebView, bool>((view) =>
                {
                    if (!saved)
                    {
                        saved = true;
                        using (StreamWriter writer = new StreamWriter("test.html")) // Debug Writing, to see if it actually works.
                        {
                            writer.Write(view.HTML);
                        }
                    }
                    doc.LoadHtml(view.HTML);
                    
                    foreach (HtmlNode na in doc.DocumentNode.SelectNodes("//div[@ng-switch-default]"))
                    {
                        int i = 0;
                        try
                        {
                            Console.WriteLine(i);
                            Console.WriteLine(na.Descendants("div").Count());
                            Console.WriteLine("--");
                            if (na.Descendants("div").Count() > 1)
                            {
                                return true;
                            }
                            i++;
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    return false;
                }));

            using(StreamWriter writer = new StreamWriter("test.html")) // Debug Writing, to see if it actually works.
            {
                writer.Write(result);
            }
            
            doc.LoadHtml(result);
            HtmlNode n = null;
            foreach (HtmlNode na in doc.DocumentNode.SelectNodes("//div[@ng-switch-default]"))
            {
                int i = 0;
                try
                {
                    if (na.Descendants("div").Count() > 1)
                    {
                        n = na;
                        break;
                    }
                    i++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            IEnumerable<HtmlNode> findclasses = n.Descendants("div");
            /*
             * .Where(d =>
                d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("roblox-message-row")
            )
             /
            Console.WriteLine(doc.DocumentNode.SelectNodes("//div[@id='MessagesInbox']").Count);
            List<MessageRecievedEvent> messages = new List<MessageRecievedEvent>();
            foreach(HtmlNode node in findclasses)
            {
                
                try
                {
                    String subject = node.Descendants("div").Where(d =>
                        d.Attributes.Contains("class") && d.Attributes["class"].Value.Contains("subject")
                    ).First().InnerText;

                    String body = node.Descendants("div").Where(d =>
                        d.Attributes.Contains("ng-bind-html") && d.Attributes["ng-bind-html"].Value.Contains("message.Body | htmlToPlaintext")
                    ).First().InnerText;

                    int userId = int.Parse(node.Descendants("a").Where(d =>
                        d.Attributes.Contains("rbx-avatar")
                    ).First().GetAttributeValue("href", "").Replace("https://www.roblox.com/users/", "").Replace("/profile/", ""));

                    messages.Add(new MessageRecievedEvent(body, subject, userId));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            return messages.ToArray();

            
        }
     */
#endregion
}
