using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.Event
{
    public class MessageRecievedEvent
    {

        internal MessageRecievedEvent(string body, string subject, int senderid)
        {
            Body = body;
            Subject = subject;
            SenderID = senderid;
        }


        public string Body
        {
            get;
            internal set;
        }

        public string Subject
        {
            get;
            internal set;
        }

        public int SenderID
        {
            get;
            internal set;
        }

    }
}
