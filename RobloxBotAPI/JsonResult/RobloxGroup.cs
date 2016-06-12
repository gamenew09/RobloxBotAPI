using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RobloxBotAPI.JsonResult
{
    public class RobloxGroup
    {

        private int _GroupID;

        public int GroupID
        {
            get { return _GroupID; }
        }

        private GroupRank[] _Ranks;

        public GroupRank[] Ranks
        {
            get 
            {
                if(_Ranks == null)
                {
                    Task<GroupRank[]> task = RobloxAPI.GetGroupRanks(_GroupID);
                    while (!task.IsCompleted)
                        Thread.Sleep(1);
                    _Ranks = task.Result;
                }
                return _Ranks;
            }
        }

        public async void RefreshRanks()
        {
            _Ranks = await RobloxAPI.GetGroupRanks(_GroupID);
        }

        public RobloxGroup(int groupId)
        {
            _GroupID = groupId;
        }

    }
}
