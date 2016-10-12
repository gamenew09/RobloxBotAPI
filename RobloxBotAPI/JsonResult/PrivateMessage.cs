using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{

    internal struct Sender_t
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int BuildersClubStatus { get; set; }
    }

    internal struct Recipient_t
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int BuildersClubStatus { get; set; }
    }

    internal struct Message_t
    {
        public int Id { get; set; }
        public Sender_t Sender { get; set; }
        public string SenderAbsoluteUrl { get; set; }
        public string RecipientAbsoluteUrl { get; set; }
        public string AbuseReportAbsoluteUrl { get; set; }
        public Recipient_t Recipient { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public RobloxThumbnail_t SenderThumbnail { get; set; }
        public RobloxThumbnail_t RecipientThumbnail { get; set; }
        public string Created { get; set; }
        public string Updated { get; set; }
        public bool IsRead { get; set; }
        public bool IsSystemMessage { get; set; }
        public bool IsReportAbuseDisplayed { get; set; }
    }

    internal struct PrivateMessages_t
    {
        public int PageNumber { get; set; }
        public int TotalPages { get; set; }
        public List<Message_t> Collection { get; set; }
        public int TotalCollectionSize { get; set; }
    }

    public struct RobloxThumbnail_t
    {
        public bool Final { get; set; }
        public string Url { get; set; }
        public string RetryUrl { get; set; }
    }

    public class PrivateMessage
    {

        private Message_t _PM;
        internal PrivateMessage(Message_t pm)
        {
            _PM = pm;
        }

        public int Id { get { return _PM.Id; } }
        public int Sender { get { return _PM.Sender.UserId; } }
        public string SenderAbsoluteUrl { get { return _PM.SenderAbsoluteUrl; } }
        public string RecipientAbsoluteUrl { get { return _PM.RecipientAbsoluteUrl; } }
        public string AbuseReportAbsoluteUrl { get { return _PM.AbuseReportAbsoluteUrl; } }
        public int Recipient { get { return _PM.Recipient.UserId; } }
        public string Subject { get { return _PM.Subject; } }
        public string Body { get { return _PM.Body; } }
        public RobloxThumbnail_t SenderThumbnail { get { return _PM.SenderThumbnail; } }
        public RobloxThumbnail_t RecipientThumbnail { get { return _PM.RecipientThumbnail; } }
        public string Created { get { return _PM.Created; } }
        public string Updated { get { return _PM.Updated; } }
        public bool IsRead { get { return _PM.IsRead; } }
        public bool IsSystemMessage { get { return _PM.IsSystemMessage; } }
        public bool IsReportAbuseDisplayed { get { return _PM.IsReportAbuseDisplayed; } }

    }

}
