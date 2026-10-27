using System;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        string friendRoomMode="1v1";
        void BuildRoomBrowser(bool replay = true)
        {
            OpenModal("ROOM MATCH","","rooms",replay);WideModal();
            var rooms=new JArray();
            foreach(var room in UltraServices.State?["rooms"] as JArray??new JArray())
                if(((string)room["mode"]??"1v1")==friendRoomMode)rooms.Add(room.DeepClone());
            HomeText(modal,"FRIEND ROOMS",25,Color.white,.045f,.76f,.55f,.06f);
            HomeText(modal,rooms.Count+" rooms available",18,Muted,.045f,.70f,.55f,.05f);
            var viewport=HomeRect("Room list",modal,.045f,.16f,.59f,.53f);var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;viewport.gameObject.AddComponent<RectMask2D>();
            var content=HomeRect("Room content",viewport,0,1,1,0);content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            int i=0;
            foreach(var room in rooms)
            {
                string id=(string)room["id"];string host=(string)room["host"];
                var row=HomeRect("Friend room",content,0,1,1,0);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,180);row.anchoredPosition=new Vector2(0,-i++*198);
                HomePanel(row,UltraTheme.Panel);
                var accent=HomeRect("Room accent",row,0,.14f,.01f,.72f);HomePanel(accent,UltraTheme.Accent);accent.gameObject.AddComponent<UltraAccentFill>();
                HomeText(row,host+"'s room",22,UltraTheme.Fg,.04f,.74f,.90f,.22f);
                var avatar=HomeRect("Host mate",row,.035f,.12f,.14f,.56f).gameObject.AddComponent<Image>();avatar.preserveAspect=true;LoadMatePortrait(avatar).Forget();
                HomeText(row,(string)room["format"]=="Genesys"?"GENESYS / "+(int)room["cap"]+" POINTS":"TCG ADVANCED",22,Color.white,.20f,.40f,.50f,.23f);
                HomeText(row,"Private friend duel",17,Muted,.20f,.15f,.50f,.18f);
                SmallButton(row,"TABLES  >",()=>OpenFriendTables(id),.71f,.43f,.25f,.26f,22);
                SmallButton(row,"WATCH",()=>JoinFriendRoom(id,true),.71f,.10f,.25f,.26f,20);
            }
            content.sizeDelta=new Vector2(0,i*198);
            if(i==0)
            {
                HomeText(viewport,"No friend rooms open",32,Color.white,.02f,.58f,.95f,.15f);
                HomeText(viewport,"Create a room and invite a friend, or join one\nfrom an invitation in your Friends list.",23,Muted,.02f,.28f,.95f,.27f);
            }
            HomePanel(HomeRect("Options divider",modal,.665f,.17f,.001f,.64f),new Color(.48f,.53f,.60f,.6f));
            HomeText(modal,"ROOM OPTIONS",23,Color.white,.70f,.76f,.25f,.055f);
            SmallButton(modal,friendRoomMode=="2v2"?"MODE: DUOS 2v2":"MODE: 1v1",()=>{friendRoomMode=friendRoomMode=="1v1"?"2v2":"1v1";ShowRooms(false);},.70f,.85f,.25f,.075f,20);
            SmallButton(modal,DuelFormat.DisplayName,()=>DuelFormat.Select(()=>ShowRooms(false)),.70f,.65f,.25f,.085f,21);
            SmallButton(modal,"CREATE ROOM",()=>Run(async()=>{
                var deck=new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+Config.GetConfigDeckName()+Program.EXPANSION_YDK);
                if(!DuelFormat.Validate(deck,"Your Deck"))return;
                var room=await UltraServices.Request("/rooms","POST",new{format=DuelFormat.Genesys?"Genesys":"TCG",cap=DuelFormat.PointCap,mode=friendRoomMode});
                ShowFriendTables(room);
            }),.70f,.53f,.25f,.085f,24,UltraTier.Hero);
            var deckPlate=HomeRect("Deck plate",modal,.69f,.115f,.27f,.39f);HomePanel(deckPlate,UltraTheme.Panel);
            var deckEdge=HomeRect("Deck accent",deckPlate,0f,.18f,.014f,.64f);HomePanel(deckEdge,UltraTheme.Accent);deckEdge.gameObject.AddComponent<UltraAccentFill>();
            HomeText(deckPlate,"YOUR DECK",17,Muted,.08f,.80f,.84f,.16f);
            var caseImage=HomeRect("Deck case",deckPlate,.28f,.26f,.44f,.52f).gameObject.AddComponent<Image>();caseImage.preserveAspect=true;LoadRoomDeckCase(deckPlate,caseImage).Forget();
            var deckName=HomeText(deckPlate,Config.GetConfigDeckName(),20,Color.white,.06f,.04f,.88f,.18f);if(deckName!=null)deckName.alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();ShowRooms(false);}),.045f,.025f,.20f,.075f,20);
            SmallButton(modal,"FRIENDS",OpenFriends,.27f,.025f,.20f,.075f,20);
            SmallButton(modal,"DUEL MODES",OpenDuel,.70f,.025f,.25f,.075f,20);
        }
        void OpenFriendTables(string id)
        {
            Run(async()=>ShowFriendTables(await UltraServices.Request("/rooms/"+id)));
        }
        void ShowFriendTables(JObject room)
        {
            string id=(string)room["id"];
            OpenModal("ROOM MATCH",(string)room["host"]+"'s room / "+(string)room["format"],"tables");WideModal();
            string mode=(string)room["mode"]??"1v1";
            int seats=mode=="2v2"?4:2;
            int seated=CountSeated(room);
            string seatLine=seated<=0?"Open · 0 / "+seats+" seated":seated+" / "+seats+" seated";
            for(int i=0;i<3;i++)
            {
                int number=i+1;
                var table=HomeRect("Table "+number,modal,.04f,.52f-i*.20f,.62f,.185f);
                HomePanel(table,UltraTheme.Panel);
                var accent=HomeRect("Table accent",table,0,.16f,.012f,.68f);HomePanel(accent,UltraTheme.Accent);accent.gameObject.AddComponent<UltraAccentFill>();
                HomeText(table,"Table "+number,24,UltraTheme.Fg,.04f,.72f,.55f,.22f);
                HomeText(table,(mode=="2v2"?"DUOS 2v2":"1v1")+"  ·  "+seatLine+"  ·  300 seconds",18,Muted,.04f,.46f,.55f,.22f);
                for(int s=0;s<seats;s++)
                {
                    var dot=HomeRect("Seat",table,.045f+s*.04f,.16f,.028f,.18f);
                    UltraUi.EnsureRenderer(dot.gameObject);
                    var pip=dot.gameObject.AddComponent<Image>();
                    pip.color=s<seated?UltraTheme.Accent:new Color(1f,1f,1f,.16f);
                    pip.raycastTarget=false;
                }
                SmallButton(table,"ENTRY",()=>JoinFriendRoom(id,false,number),.50f,.16f,.26f,.68f,24,UltraTier.Primary);
                SmallButton(table,"WATCH",()=>JoinFriendRoom(id,true,number),.78f,.22f,.18f,.56f,20);
            }
            HomeText(modal,"ROOM OPTIONS",24,Color.white,.69f,.78f,.27f,.06f);
            HomeText(modal,"Each table hosts a separate duel.\nChoose WATCH to spectate without\nentering a player seat.",20,Muted,.69f,.43f,.27f,.29f);
            SmallButton(modal,"REFRESH",()=>OpenFriendTables(id),.69f,.31f,.27f,.09f,21);
            if((int?)room["hostId"]==(int?)UltraServices.State?["id"])
                SmallButton(modal,"DISBAND ROOM",()=>Run(async()=>{await UltraServices.Request("/rooms/"+id,"DELETE");await UltraServices.Refresh();ShowRooms();}),.69f,.19f,.27f,.09f,21);
            SmallButton(modal,"ALL FRIEND ROOMS",()=>ShowRooms(),.69f,.07f,.27f,.09f,21);
        }
        static int CountSeated(JObject room)
        {
            if(room==null) return 0;
            foreach(string key in new[]{"seated","playerCount","players","members"})
            {
                var token=room[key];
                if(token==null || token.Type==JTokenType.Null) continue;
                if(token is JArray list) return list.Count;
                if(int.TryParse(token.ToString(), out int count)) return count;
            }
            return 0;
        }
        async UniTask LoadRoomDeckCase(RectTransform plate, Image image)
        {
            string deckName=Config.GetConfigDeckName();
            try
            {
                var deck=new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+deckName+Program.EXPANSION_YDK);
                var sprite=await Program.items.TryLoadItemIconAsync(deck.Case.ToString(),Items.ItemType.Case);
                if(image!=null){image.sprite=sprite;image.enabled=true;image.color=Color.white;}
                UltraDeckBackdrop.Apply(plate,deck,deckName);
            }
            catch(Exception e){Debug.LogWarning(e.Message);}
        }
    }
}
