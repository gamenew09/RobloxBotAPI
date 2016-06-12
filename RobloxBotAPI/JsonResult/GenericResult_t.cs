using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{
    public struct GenericResult_t
    {
        [JsonIgnore]
        public GenericResultEnum ResultEnum;

        [JsonProperty("success")]
        public bool Success;
        [JsonProperty("message")]
        public string Message;

        public GenericResult_t(GenericResultEnum res)
        {
            ResultEnum = res;
            Success = (res != GenericResultEnum.Success) ? false : true;
            Message = "";
        }
    }

    public enum GenericResultEnum
    {
        Success = 200,
        InvalidXSRFToken = 403,
        InternalServerError = 500,
        Unknown = -1
    }
}
