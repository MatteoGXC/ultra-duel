using System;
using System.Linq;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        int matchmakingGeneration;
        bool matchmakingActive;
        static int ProfileIconId()=>int.TryParse(Config.Get("DuelFace0","1010001"),out int id) && id>=1010000 && id<1030000?id:1010001;
        void OpenGuiScale()
        {
            OpenModal("GUI SCALE","Adjust button sizes on every page. Your wallpaper stays the same size.","gui-scale");WideModal();
            HomeText(modal,UltraGuiScale.Percent+"%",54,Color.white,.35f,.54f,.30f,.12f).alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"−",()=>{UltraGuiScale.Set(UltraGuiScale.Percent-5);OpenGuiScale();},.20f,.40f,.14f,.10f,35);
            SmallButton(modal,"+",()=>{UltraGuiScale.Set(UltraGuiScale.Percent+5);OpenGuiScale();},.66f,.40f,.14f,.10f,35);
            SmallButton(modal,"RESET TO 100%",()=>{UltraGuiScale.Set(100);OpenGuiScale();},.36f,.27f,.28f,.09f,22);
            HomeText(modal,"70% — 125%  /  Saved automatically",19,Muted,.20f,.17f,.60f,.06f).alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"GAME SETTINGS",()=>{CloseModal();OnSetting();},.32f,.055f,.36f,.08f,22);
        }
        async UniTask LoadReleaseNotes()
        {
            var page=modal;
            string notes="v"+UltraServices.Version+"\nGUI scale, random online duels, updated profiles and themed banners.\nFriend tables, spectating, duel timers and Beyond the Brave cards.";
            string heading="INSTALLED VERSION";
            try
            {
                var release=await UltraServices.PublishedRelease();
                notes=(string)release["body"]??notes;heading=(string)release["name"]??"LATEST RELEASE";
            }
            catch(Exception){heading="OFFLINE / INSTALLED VERSION";}
            if(page==null || modal!=page || modalPage!="updates")return;
            var loading=page.Find("Release notes loading");if(loading!=null)Destroy(loading.gameObject);
            var viewport=HomeRect("Release notes viewport",page,.055f,.25f,.89f,.49f);viewport.gameObject.AddComponent<RectMask2D>();
            var hit=viewport.gameObject.AddComponent<Image>();hit.color=new Color(.025f,.035f,.05f,.70f);
            var content=HomeRect("Published release notes",viewport,.025f,1,.95f,0);content.pivot=new Vector2(.5f,1);
            notes=Regex.Replace(notes,@"\[([^\]]+)\]\([^\)]+\)","$1").Replace("**","").Replace("`","");
            notes=Regex.Replace(notes,@"(?m)^#{1,6}\s*","");
            var text=HomeText(content,heading+"\n\n"+notes,21,Color.white,0,0,1,1);text.richText=false;
            Canvas.ForceUpdateCanvases();content.sizeDelta=new Vector2(0,Mathf.Max(viewport.rect.height,text.GetPreferredValues(text.text,Mathf.Max(100,viewport.rect.width*.95f),0).y+30));
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=35;scroll.movementType=ScrollRect.MovementType.Clamped;
            SmallButton(page,"VIEW RELEASE",()=>Application.OpenURL(UltraServices.Repository+"/releases"),.055f,.025f,.35f,.075f,20);
        }
        void OpenOnlineDuel()
        {
            if(string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            DuelFormat.Select(()=>BeginOnlineDuel());
        }
        void BeginOnlineDuel()
        {
            var deck=new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+Config.GetConfigDeckName()+Program.EXPANSION_YDK);
            if(!DuelFormat.Validate(deck,"Your Deck"))return;
            OpenModal("ONLINE DUEL","Ultra Duel / Random opponent / "+(DuelFormat.Genesys?"Genesys":"TCG"),"matchmaking");WideModal();
            HomeText(modal,"WAITING FOR OPPONENT",38,Color.white,.10f,.46f,.80f,.15f).alignment=TextAlignmentOptions.Center;
            HomeText(modal,"Searching for another player using the same format…",22,Muted,.10f,.32f,.80f,.10f).alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"CANCEL SEARCH",CloseModal,.30f,.16f,.40f,.09f,24);
            matchmakingActive=true;int generation=++matchmakingGeneration;PollMatchmaking(generation).Forget();
        }
        async UniTask PollMatchmaking(int generation)
        {
            try
            {
                while(this!=null && matchmakingActive && generation==matchmakingGeneration)
                {
                    var result=await UltraServices.Request("/matchmaking","POST",new{format=DuelFormat.Genesys?"Genesys":"TCG",cap=DuelFormat.PointCap,accept=true});
                    if(this==null || !matchmakingActive || generation!=matchmakingGeneration)return;
                    if((string)result["status"]=="ready")
                    {
                        matchmakingActive=false;CloseModal();await UltraServices.JoinRoom((Newtonsoft.Json.Linq.JObject)result["room"]);
                        await UltraServices.Request("/matchmaking","DELETE");return;
                    }
                    if(modalStatus!=null)modalStatus.text=(string)result["status"]=="confirming"?"Opponent found / Confirming connection…":"Searching…";
                    await UniTask.Delay(3000,ignoreTimeScale:true);
                }
            }
            catch(Exception e){if(this!=null && generation==matchmakingGeneration){matchmakingActive=false;if(modalStatus!=null)modalStatus.text=e.Message;}}
        }
        async UniTaskVoid CancelMatchmaking()
        {
            matchmakingActive=false;++matchmakingGeneration;
            try{await UltraServices.Request("/matchmaking","DELETE");}catch(Exception){/* An abandoned search expires on the server. */}
        }
    }
}
