using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace RobloxBotAPI.JsonResult
{
    public struct LoginResult_t
    {

        [JsonIgnore]
        public int ReturnCode;

        public int userId;

        public string message;

    }

    public class LoginResult
    {

        private LoginResult_t _Result;
        public LoginResult(LoginResult_t res)
        {
            _Result = res;
        }

        public int ReturnCode
        {
            get { return _Result.ReturnCode; ; }
        }

        public RobloxBot Bot
        {
            get;
            internal set;
        }

        /// <summary>
        /// The UserId that /login/v1 sent, will not have a value if the request errored.
        /// </summary>
        public int UserId
        {
            get { return _Result.userId; }
        }

        public LoginFailureReason FailureReason
        {
            get 
            {
                switch(_Result.message)
                {
                    case "Captcha":
                        return LoginFailureReason.Captcha;
                    case "Credentials":
                        return LoginFailureReason.Credentials;
                    case "Privileged":
                        return LoginFailureReason.Privileged;
                    case "TwoStepVerification":
                        return LoginFailureReason.TwoStepVerification;
                }
                switch(ReturnCode)
                {
                    case 404:
                        return LoginFailureReason.DisabledEndpoint;
                    case 500:
                        return LoginFailureReason.InternalError;
                    case 503:
                        return LoginFailureReason.Unavailable;
                }
                return LoginFailureReason.Success;
            }
        }
    }

    public enum LoginFailureReason
    {
        Success,
        Captcha,
        Credentials,
        Privileged,
        TwoStepVerification,
        DisabledEndpoint,
        InternalError,
        Unavailable
    }
}
