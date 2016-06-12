using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{
    public enum SignoutResult
    {
        Success = 200,
        InvalidXSRFToken = 403,
        InternalServerError = 500,
        Unknown = -1
    }
}
