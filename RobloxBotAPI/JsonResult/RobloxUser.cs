using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{
    public class RobloxUser
    {

        private RobloxUser_t _User;

        internal RobloxUser(RobloxUser_t user)
        {
            _User = user;
        }

        public int ID
        {
            get { return _User.ID; }
        }

        public string Username
        {
            get { return _User.Username; }
        }

        public BuildersClubType Membership
        {
            get { return _User.MemberShip; }
        }

        

        public async Task<bool> CanManage(int assetId)
        {
            try
            {
                Dictionary<string, object> dict = await RobloxAPIHelper.GetJsonObject<Dictionary<string, object>>(String.Format("{0}users/{1}/canmanage/{2}", RobloxBot.ROBLOX_API_URL, _User.ID, assetId));
                object outa, outb;
            
                if (dict.TryGetValue("Success", out outa))
                {
                    if (dict.TryGetValue("CanManage", out outb) && ((bool)outa))
                    {
                        return (bool)outb;
                    }
                }
            }
            catch { }
            return false;
        }
        
        // http://www.roblox.com/Thumbs/BCOverlay.ashx?username=Shedletsky
    }

    public enum BuildersClubType
    {
        None,
        Classic,
        Turbo,
        Outrageous
    }

    internal struct RobloxUser_t
    {
        [JsonProperty("Id")]
        internal int ID;
        internal BuildersClubType MemberShip;
        internal String Username;
    }
}
