using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using Newtonsoft.Json.Linq;

namespace MDPro3
{
    public static class UltraMissions
    {
        public sealed class Mission { public string id,category,title;public int progress,target,gems;public bool claimed; }
        static string Day=>((DateTimeOffset.UtcNow.ToUnixTimeSeconds())/86400).ToString();
        public static List<Mission> Rows()
        {
            if(UltraAccount.Profile!=null)return (UltraAccount.Profile["missions"] as JArray)?.ToObject<List<Mission>>()??new List<Mission>();
            var games=UltraProgress.Data.matches.Where(g=>string.IsNullOrEmpty(g.account)).ToArray();
            var today=games.Where(g=>DateTime.TryParse(g.date,out var date) && date.ToUniversalTime().Date==DateTime.UtcNow.Date).ToArray();
            var result=new List<Mission>();
            void Add(string id,string category,string title,int progress,int target,int gems)
            {
                UltraProgress.Data.missionClaims??=new List<string>();
                result.Add(new Mission{id=id,category=category,title=title,progress=Math.Min(progress,target),target=target,gems=gems,
                    claimed=UltraProgress.Data.missionClaims.Contains(id+(category=="Daily"?":"+Day:""))});
            }
            Add("daily-play","Daily","Complete a duel",today.Length,1,20);
            Add("daily-win","Daily","Win a duel",today.Count(g=>g.result=="WIN"),1,30);
            foreach(var n in new[]{5,20})
            {
                Add("played-"+n,"Lifetime","Complete duels",games.Length,n,n==5?50:100);
                Add("wins-"+n,"Lifetime","Win duels",games.Count(g=>g.result=="WIN"),n,n==5?50:100);
            }
            foreach(var n in new[]{1,5,10,20})Add("grade-"+n,"Duel Pass","Raise your Duel Pass grade",UltraAccount.Tier,n,n==1?20:n==5?50:n==10?100:200);
            return result;
        }
        public static async UniTask Claim(string id="all")
        {
            if(UltraAccount.Profile!=null){UltraServices.SetProfile(await UltraServices.Request("/missions/claim","POST",new{id}));return;}
            if(!string.IsNullOrEmpty(UltraServices.Token))throw new Exception("Connect to receive account rewards.");
            foreach(var row in Rows().Where(r=>!r.claimed && r.progress>=r.target && (id=="all" || id==r.id)))
            {UltraProgress.Data.missionClaims.Add(row.id+(row.category=="Daily"?":"+Day:""));UltraProgress.Data.gems+=row.gems;}
            UltraProgress.Save();
        }
    }
}
