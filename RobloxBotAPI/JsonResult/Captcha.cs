using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{
    public class Captcha
    {

        public string ChallengeKey
        {
            get;
            internal set;
        }

        public string Image
        {
            get;
            internal set;
        }

    }
}
