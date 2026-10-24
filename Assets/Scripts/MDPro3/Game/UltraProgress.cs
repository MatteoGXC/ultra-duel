using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;

namespace MDPro3
{
    // Free local progression. It is deliberately separate from friends accounts
    // and is never presented as a competitive online currency or ranking.
    public static class UltraProgress
    {
        [Serializable] public class Profile
        {
            public int wins, xp, gems, claimed;
            public List<string> missionClaims = new List<string>();
            public List<string> ownedCosmetics = new List<string>();
            public List<Match> matches = new List<Match>();
            public string title = "Rookie Duelist";
            public string frame = "Default";
            public List<string> ownedFrames = new List<string> { "Default" };
        }
        [Serializable] public class Match
        {
            public string id, opponent, deck, mode, result, date, replay, account;
            public bool synced;
            public int turns, damageDealt, damageTaken, destroyed, spells, summons, xpBefore, gemsEarned, xpEarned;
            public int Score => (result=="WIN"?1500:0)+(result=="WIN" && damageTaken==0?1000:0)+(damageDealt>=3000?500:0)+(destroyed>0?200:0)+(spells>0?100:0)+(summons>0?100:0);
        }
        static Match match;
        public static Match LastMatch => match;
        public static bool Tracking => match!=null && match.result==null && OcgCore.condition==OcgCore.Condition.Duel && !OcgCore.isObserver && !RoomServant.FromHandTest;
        public static void Damage(int player,int amount){if(!Tracking)return;if(player==0)match.damageTaken+=Math.Max(0,amount);else match.damageDealt+=Math.Max(0,amount);}
        public static void Destroyed(){if(Tracking)match.destroyed++;}
        public static void Spell(){if(Tracking)match.spells++;}
        public static void SpecialSummon(){if(Tracking)match.summons++;}
        static UniTask startRequest;
        static string accountToken;
        const string PathName = "Progress/profile.json";
        static Profile profile;
        static string activeDuel, rewardedDuel;
        public static Profile Data
        {
            get
            {
                if (profile != null) return profile;
                try { profile = JsonConvert.DeserializeObject<Profile>(File.ReadAllText(PathName)); }
                catch { profile = new Profile(); }
                return profile ??= new Profile();
            }
        }
        public static int Tier => Math.Min(20, Data.xp / 200);
        public static int Level => 1 + Data.xp / 500;
        public static event Action Changed;
        public static void BeginDuel()
        {
            activeDuel = Guid.NewGuid().ToString("N");
            match=new Match{id=activeDuel,opponent=OcgCore.name_1,deck=Config.GetConfigDeckName(),
                mode=RoomServant.FromSolo?"Solo":RoomServant.FromUltraHost?"Friend duel":"Duel",date=DateTime.UtcNow.ToString("o"),xpBefore=UltraAccount.XP,account=string.IsNullOrEmpty(UltraServices.Token)?"":UltraServices.PlayerName};
            accountToken=UltraServices.Token;
            startRequest=StartAccountGame().Preserve();
        }
        static async UniTask StartAccountGame()
        {
            if(string.IsNullOrEmpty(accountToken))return;
            try {await UltraServices.Request("/games/start","POST",new{nonce=match.id,deck=match.deck,opponent=match.opponent,mode=match.mode});}
            catch(Exception e){Debug.LogWarning("Account progress: "+e.Message);}
        }
        public static void Finish(int winner,int turns)
        {
            if(match==null || match.result!=null)return;
            match.result=winner==0?"WIN":winner==2?"DRAW":"LOSS";match.turns=turns;
            if(winner==0){Win();Data.gems+=25;match.gemsEarned=25;match.xpEarned=100;}
            Data.matches.Insert(0,match);
            if(Data.matches.Count>200)Data.matches.RemoveAt(200);
            Save();FinishAccountGame(match).Forget();
        }
        static async UniTask FinishAccountGame(Match finished)
        {
            if(string.IsNullOrEmpty(accountToken))return;
            var token=accountToken;
            await startRequest;
            if(token!=UltraServices.Token)return;
            await SyncPending();
        }
        static bool syncBusy;
        public static async UniTask SyncPending()
        {
            if(syncBusy || string.IsNullOrEmpty(UltraServices.Token))return;
            syncBusy=true;string token=UltraServices.Token;
            try
            {
                foreach(var game in Data.matches.Where(g=>!g.synced && g.result!=null && g.account==UltraServices.PlayerName).Reverse().ToArray())
                {
                    if(token!=UltraServices.Token)break;
                    await UltraServices.Request("/games/start","POST",new{nonce=game.id,deck=game.deck,opponent=game.opponent,mode=game.mode});
                    var profile=await UltraServices.Request("/games/finish","POST",new{nonce=game.id,result=game.result,turns=game.turns});
                    if(token!=UltraServices.Token)break;
                    game.synced=true;Save();UltraServices.SetProfile(profile);
                }
            }
            catch(Exception e){Debug.LogWarning("Account rewards queued for retry: "+e.Message);}
            finally{syncBusy=false;}
        }
        public static void AttachReplay(string name)
        {
            if(match==null || match.result==null)return;
            match.replay=name;Save();
        }
        public static bool Win()
        {
            if (string.IsNullOrEmpty(activeDuel) || activeDuel == rewardedDuel) return false;
            rewardedDuel = activeDuel;
            Data.wins++; Data.xp += 100;
            Save();
            return true;
        }
        public static int Claim()
        {
            int count = Math.Max(0, Tier - Data.claimed);
            if (count == 0) return 0;
            Data.gems += count * 50;
            Data.claimed = Tier;
            if (Tier >= 5) Data.title = "Rising Duelist";
            if (Tier >= 10) { Data.title = "Ultra Challenger"; Data.frame = "Neon"; if (!Data.ownedFrames.Contains("Neon")) Data.ownedFrames.Add("Neon"); }
            if (Tier >= 20) { Data.title = "Ultra Champion"; Data.frame = "Champion"; if (!Data.ownedFrames.Contains("Champion")) Data.ownedFrames.Add("Champion"); }
            Save();
            return count;
        }
        public static bool Buy(string item)
        {
            int price = item == "Violet" ? 150 : 250;
            if (item != "Violet" && item != "Emerald") return false;
            if (Data.ownedFrames.Contains(item)) { Data.frame = item; Save(); return true; }
            if (Data.gems < price) return false;
            Data.gems -= price; Data.ownedFrames.Add(item); Data.frame = item; Save(); return true;
        }
        public static void Save()
        {
            Directory.CreateDirectory("Progress");
            string pending = PathName + ".tmp";
            File.WriteAllText(pending, JsonConvert.SerializeObject(Data, Formatting.Indented));
            if (File.Exists(PathName)) File.Replace(pending, PathName, PathName + ".bak");
            else File.Move(pending, PathName);
            Changed?.Invoke();
        }
    }
}
