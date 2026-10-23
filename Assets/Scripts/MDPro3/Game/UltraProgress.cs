using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace MDPro3
{
    // Free local progression. It is deliberately separate from friends accounts
    // and is never presented as a competitive online currency or ranking.
    public static class UltraProgress
    {
        [Serializable] public class Profile
        {
            public int wins, xp, gems, claimed;
            public string title = "Rookie Duelist";
            public string frame = "Default";
            public List<string> ownedFrames = new List<string> { "Default" };
        }
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
        public static void BeginDuel() { activeDuel = Guid.NewGuid().ToString("N"); }
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
