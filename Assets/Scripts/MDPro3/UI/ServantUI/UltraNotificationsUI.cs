using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class MainMenuUI
    {
        RectTransform noticeBadge, noticePreview, noticeContent;
        TextMeshProUGUI noticeCount, noticeSummary, noticeHeading;
        string renderedNotifications;
        int previewNotification;
        JObject notificationFixture; // Used only by the opt-in packaged UI verification.
        JObject NotificationState => notificationFixture??UltraServices.State;
        JArray NotificationRows => NotificationState?["notifications"] as JArray ?? new JArray();
        int UnreadNotifications => NotificationRows.Count(n=>(int?)n["seen"]!=1);

        void BuildNotificationsHome()
        {
            var envelope=(RectTransform)ultraHome.Find("NOTICES");
            noticeBadge=HomeRect("Unread notifications",envelope,.62f,.73f,.30f,.28f);
            var badge=noticeBadge.gameObject.AddComponent<Image>();badge.color=UltraTheme.Highlight;badge.raycastTarget=false;
            noticeCount=HomeText(noticeBadge,"",14,Color.white,0,0,1,1);noticeCount.alignment=TextAlignmentOptions.Center;
            noticePreview=Px("Home notification",ultraHome,0,176,420,108);
            noticePreview.anchorMin=noticePreview.anchorMax=new Vector2(1f,1f);noticePreview.pivot=new Vector2(1f,1f);noticePreview.anchoredPosition=new Vector2(-64f,-176f);
            HomePanel(noticePreview,UltraTheme.Panel);
            var edge=Px("Notice accent",noticePreview,0,16,6,76);edge.gameObject.AddComponent<Image>().raycastTarget=false;edge.gameObject.AddComponent<UltraAccentFill>();
            noticeHeading=HomeText(noticePreview,"NOTIFICATIONS",13,Lime,.06f,.62f,.88f,.28f);
            noticeHeading.gameObject.AddComponent<UltraAccentText>();UltraTheme.StyleEmphasis(noticeHeading,8f);
            noticeSummary=HomeText(noticePreview,"",18,UltraTheme.Fg,.06f,.10f,.88f,.48f);UltraTheme.Style(noticeSummary,false);
            var button=noticePreview.gameObject.AddComponent<Button>();button.targetGraphic=noticePreview.GetComponent<Image>();button.onClick.AddListener(OpenNotifications);
            noticePreview.GetComponent<Image>().raycastTarget=true;
            noticePreview.gameObject.AddComponent<UltraButtonMotion>();
        }
        static string NoticeTitle(JToken row)
        {
            string name=(string)row["name"]??"A friend";
            return (string)row["kind"] switch {
                "duel-invite"=>name+" invited you to duel",
                "invite-sent"=>"Duel invitation sent to "+name,
                "friend-request"=>name+" sent a friend request",
                "request-sent"=>"Friend request sent to "+name,
                "friend-accepted"=>name+" accepted your friend request",
                _=>"Friend activity" };
        }
        void RefreshNotificationsHome()
        {
            if(noticeBadge==null)return;
            int unread=UnreadNotifications;
            var badgeImage=noticeBadge.GetComponent<Image>();if(badgeImage!=null)badgeImage.color=UltraTheme.Highlight;
            noticeBadge.gameObject.SetActive(unread>0);
            noticeCount.text=unread>99?"99+":unread.ToString();
            var latest=NotificationRows.FirstOrDefault(n=>(int?)n["seen"]!=1)??NotificationRows.FirstOrDefault();
            bool modalHidesHome=overlay!=null && modalPage!="friends" && modalPage!="friend";
            // Modals restore children through CloseModal; do not reactivate a home
            // card over a full-screen page while presence updates arrive.
            if(!modalHidesHome)noticePreview.gameObject.SetActive(latest!=null);
            if(latest!=null)
            {
                noticeHeading.text=unread>0?"NOTIFICATIONS  /  "+unread+" NEW":"RECENT ACTIVITY";
                noticeSummary.text=NoticeTitle(latest);
                int id=(int)latest["id"];
                if(id!=previewNotification && noticePreview.gameObject.activeInHierarchy)
                {
                    var fade=noticePreview.GetComponent<CanvasGroup>();
                    if(fade==null)fade=noticePreview.gameObject.AddComponent<CanvasGroup>();
                    fade.DOKill();fade.alpha=0;fade.DOFade(1,.3f).SetUpdate(true).SetLink(noticePreview.gameObject);
                }
                previewNotification=id;
            }
            if(modalPage=="notifications")RenderNotifications();
        }
        void OpenNotifications()
        {
            if(notificationFixture==null && string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            OpenModal("NOTIFICATIONS",UnreadNotifications+" unread / Invitations and friend activity","notifications");
            noticeContent=ScrollArea();renderedNotifications=null;RenderNotifications();
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();}),.045f,.025f,.25f,.065f,18);
            SmallButton(modal,"UPDATES",OpenUpdates,.37f,.025f,.25f,.065f,18);
            SmallButton(modal,"MARK ALL READ",()=>Run(async()=>{
                int through=NotificationRows.Select(n=>(int)n["id"]).DefaultIfEmpty(0).Max();
                if(notificationFixture!=null){foreach(var row in NotificationRows)row["seen"]=1;RefreshNotificationsHome();}
                else if(through>0)await UltraServices.ReadNotifications(through);
            }),.70f,.025f,.25f,.065f,18);
            if(notificationFixture==null)Run(async()=>{await UltraServices.Refresh();});
        }
        void RenderNotifications()
        {
            if(noticeContent==null || modalPage!="notifications")return;
            string snapshot=(DateTimeOffset.UtcNow.ToUnixTimeSeconds()/30)+NotificationRows.ToString(Formatting.None)+(NotificationState?["requests"]?.ToString(Formatting.None)??"")+(NotificationState?["rooms"]?.ToString(Formatting.None)??"");
            if(snapshot==renderedNotifications)return;
            renderedNotifications=snapshot;
            foreach(Transform child in noticeContent){child.gameObject.SetActive(false);Destroy(child.gameObject);}
            var heading=modal.GetComponentsInChildren<TextMeshProUGUI>().FirstOrDefault(t=>t.text.EndsWith(" unread / Invitations and friend activity"));
            if(heading!=null)heading.text=UnreadNotifications+" unread / Invitations and friend activity";
            int index=0;long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach(var notification in NotificationRows)
            {
                string kind=(string)notification["kind"],room=(string)notification["room"];
                int peer=(int)notification["peer"];
                long age=Math.Max(0,now-(long)notification["created"]),expires=(long?)notification["expires"]??0;
                bool expired=expires>0 && expires<=now;
                string when=age<60?"Just now":age<3600?age/60+"m ago":age<86400?age/3600+"h ago":age/86400+"d ago";
                var row=Row(noticeContent,index++,NoticeTitle(notification),when+(expired?" / Invitation expired":""));
                if((int?)notification["seen"]!=1)HomePanel(HomeRect("Unread edge",row,0,.08f,.006f,.84f),Lime);
                if(kind=="friend-request" && (NotificationState?["requests"] as JArray)?.Any(p=>(int)p["id"]==peer)==true)
                    SmallButton(row,"ACCEPT",()=>Run(async()=>{await UltraServices.Request("/friends/accept","POST",new{id=peer});await UltraServices.Refresh();}),.75f,.2f,.22f,.6f,18);
                else if((kind=="duel-invite" || kind=="invite-sent") && !expired &&
                    (NotificationState?["rooms"] as JArray)?.Any(r=>(string)r["id"]==room)==true)
                    SmallButton(row,kind=="duel-invite"?"JOIN":"VIEW ROOM",()=>{if(kind=="duel-invite")JoinFriendRoom(room);else OpenFriendTables(room);},.75f,.2f,.22f,.6f,17);
                else SmallButton(row,"FRIENDS",ShowFriends,.75f,.2f,.22f,.6f,17);
            }
            if(index==0)Row(noticeContent,0,"You're all caught up","Duel invitations and friend activity will appear here.");
        }
    }
}
