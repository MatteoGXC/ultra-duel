using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace MDPro3
{
    public static class UltraAccount
    {
        public sealed class Product
        {
            public string key, kind, name;
            public int id, price;
            public int[] parts;
            public string Name {
                get {
                    if (!string.IsNullOrEmpty(name)) return name;
                    var list = kind switch { "Mate"=>Program.items.mates,"Case"=>Program.items.cases,
                        "Protector"=>Program.items.protectors,_=>Program.items.mats };
                    var item=list.Find(i=>i.id==id);
                    return string.IsNullOrWhiteSpace(item.name) || item.name==Items.STRING_NULL ? kind+" "+id : item.name;
                }
            }
        }
        static List<Product> catalog;
        public static List<Product> Catalog => catalog ??= JsonConvert.DeserializeObject<List<Product>>(File.ReadAllText("Data/shop-catalog.json"));
        public static int ProfileMate => int.TryParse(Config.Get("ProfileMate","1000001"),out int id) && CanEquip(id)?id:1000001;
        public static JObject Profile => UltraServices.State?["profile"] as JObject;
        public static int Gems => Profile==null ? UltraProgress.Data.gems : (int)Profile["gems"];
        public static int XP => Profile==null ? UltraProgress.Data.xp : (int)Profile["xp"];
        public static int Tier => Math.Min(20,XP/200);
        public static int Claimed => Profile==null ? UltraProgress.Data.claimed : (int)Profile["claimed"];
        public static int Level => 1+XP/500;
        public static string Rank => Tier>=20?"ULTRA CHAMPION":Tier>=10?"CHALLENGER":Tier>=5?"RISING DUELIST":"ROOKIE";
        public static bool Owns(Product item) => Profile==null ? UltraProgress.Data.ownedCosmetics.Contains(item.key) : (Profile["owned"] as JArray)?.Any(x=>(string)x==item.key)==true;
        public static bool CanEquip(int id)
        {
            if((int?)Profile?["unlock_all"]==1)return true;
            if (id==0 || id%10000==1) return true;
            // Wallpaper and identity artwork stay freely selectable.
            if (id>=1130000 || (id>=1010000 && id<1070000)) return true;
            return Catalog.Any(p=>(p.id==id || (p.parts?.Contains(id)??false)) && Owns(p));
        }
        public static async UniTask Buy(Product item)
        {
            if (Profile!=null)
            {
                var result=await UltraServices.Request("/shop/buy","POST",new{item=item.key});
                UltraServices.SetProfile(result);
            }
            else if (!string.IsNullOrEmpty(UltraServices.Token)) throw new Exception("Connect to the account service before purchasing.");
            else if (!Owns(item))
            {
                if (Gems<item.price) throw new Exception("Not enough gems. Earn rewards through the Duel Pass.");
                UltraProgress.Data.gems-=item.price;
                UltraProgress.Data.ownedCosmetics.Add(item.key);
                UltraProgress.Save();
            }
            if (item.kind=="Frame") {UltraProgress.Data.frame=item.name;UltraProgress.Save();}
        }
        public static async UniTask Claim()
        {
            if (Profile!=null)
            {
                UltraServices.SetProfile(await UltraServices.Request("/pass/claim","POST",new{}));
                if(Tier>=10){UltraProgress.Data.frame=Tier>=20?"Champion":"Neon";UltraProgress.Save();}
            }
            else if (!string.IsNullOrEmpty(UltraServices.Token)) throw new Exception("Connect to claim account rewards.");
            else UltraProgress.Claim();
        }
        public static string Activity
        {
            get {
                if(Program.instance==null)return "Home";
                var p=Program.instance;
                if(p.currentServant==p.ocgcore && OcgCore.condition==OcgCore.Condition.Duel && !OcgCore.duelEnded)return "In duel";
                if(p.currentServant==p.room)return "In room";
                if(p.currentServant==p.deckEditor)return "Deck editing";
                if(p.currentServant==p.solo)return "Solo";
                return "Home";
            }
        }
        public static void ApplyFieldBundle(int id)
        {
            var p=Catalog.Find(i=>i.kind=="Field" && i.id==id);
            int grave=p?.parts?[1]??1100001, stand=p?.parts?[2]??1110001;
            if(Appearance.condition==Appearance.Condition.DeckEditor) {DeckEditor.Deck.Grave=grave;DeckEditor.Deck.Stand=stand;}
            else {Config.Set(Appearance.condition.ToString()+"Grave"+Appearance.player,grave.ToString());Config.Set(Appearance.condition.ToString()+"Stand"+Appearance.player,stand.ToString());}
        }
    }
}
