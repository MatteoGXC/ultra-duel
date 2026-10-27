using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;
using Newtonsoft.Json.Linq;
using TMPro;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
namespace MDPro3.UI.ServantUI
{
    public partial class RoomServantUI
    {
        RectTransform friendRoom;
        RectTransform matchRoom;
        TextMeshProUGUI matchStatus;
        TextMeshProUGUI tableLabel, membersLabel, deckLabel;
        RectTransform roomPopup;
        Image[] tableAvatars;
        Button beginButton, readyButton;
        RectTransform R(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=gameObject.layer;
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        Image P(RectTransform r,Color c){var i=r.gameObject.AddComponent<Image>();i.color=c;i.raycastTarget=false;return i;}
        TextMeshProUGUI T(Transform p,string s,float size,float x,float y,float w,float h)
        {
            var t=UltraUi.Text(R("Label",p,x,y,w,h).gameObject);t.text=s;t.font=Program.instance.ui_.tmpFont;
            t.fontSize=size;t.color=UltraTheme.Fg;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=size*.7f;t.fontSizeMax=size;
            UltraTheme.Style(t,size>=26f,size>=26f?4f:0f);return t;
        }
        Button B(Transform p,string s,UnityEngine.Events.UnityAction action,float x,float y,float w,float h,UltraTier tier=UltraTier.Secondary)
        {
            var r=R(s,p,x,y,w,h);var image=r.gameObject.AddComponent<UltraChrome>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
            var t=T(r,s,22,.06f,.1f,.88f,.8f);t.alignment=TextAlignmentOptions.Center;
            if(tier==UltraTier.Secondary && UltraTheme.IsDanger(s))tier=UltraTier.Danger;
            r.gameObject.AddComponent<UltraThemedButton>().Configure(tier);
            b.onClick.AddListener(action);return b;
        }
        void RefreshFriendRoom()
        {
            if(RoomServant.FromUltraMatch)
            {
                if(matchRoom==null)
                {
                    matchRoom=R("Ultra Online Duel",Root,0,0,1,1);P(matchRoom,Color.clear);
                    T(matchRoom,"ONLINE DUEL",34,.08f,.86f,.84f,.08f);
                    var heading=T(matchRoom,"OPPONENT FOUND",40,.10f,.53f,.80f,.12f);heading.alignment=TextAlignmentOptions.Center;
                    matchStatus=T(matchRoom,"Connecting players…",24,.10f,.38f,.80f,.10f);matchStatus.alignment=TextAlignmentOptions.Center;
                    B(matchRoom,"LEAVE MATCH",()=>Program.instance.room.OnExit(),.35f,.17f,.30f,.08f,UltraTier.Danger);
                }
                foreach(Transform child in Root)child.gameObject.SetActive(child==matchRoom);
                matchRoom.SetAsLastSibling();
                matchStatus.text=string.Join("  VS  ",RoomServant.players.Take(2).Select(p=>p==null?"Connecting…":p.name));
                return;
            }
            if(matchRoom!=null)matchRoom.gameObject.SetActive(false);
            if(!RoomServant.FromUltraHost){if(friendRoom!=null)friendRoom.gameObject.SetActive(false);return;}
            int required=RoomServant.Mode==2?4:2;
            if(friendRoom!=null && tableAvatars.Length!=required){Destroy(friendRoom.gameObject);friendRoom=null;}
            if(friendRoom==null)
            {
                friendRoom=R("Ultra Friend Room",Root,0,0,1,1);P(friendRoom,Color.clear).raycastTarget=true;
                friendRoom.gameObject.AddComponent<UltraPageTransition>();
                var back=R("Back",friendRoom,.012f,.905f,.042f,.065f);
                var backChrome=back.gameObject.AddComponent<UltraChrome>();backChrome.cut=UltraCut.Small;backChrome.color=UltraTheme.Panel;backChrome.raycastTarget=true;
                var chevronHost=R("Chevron",back,.22f,.22f,.56f,.56f);UltraUi.EnsureRenderer(chevronHost.gameObject);var chevron=chevronHost.gameObject.AddComponent<UltraChevron>();chevron.color=UltraTheme.Fg;chevron.raycastTarget=false;
                var backButton=back.gameObject.AddComponent<Button>();backButton.targetGraphic=backChrome;backButton.onClick.AddListener(()=>Program.instance.room.OnExit());
                back.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Secondary);
                T(friendRoom,"Room Match",29,.065f,.905f,.35f,.065f);
                P(R("Header rule",friendRoom,.015f,.885f,.97f,.002f),UltraTheme.Line);
                membersLabel=T(friendRoom,"",19,.48f,.905f,.49f,.06f);
                var table=R("Table 1",friendRoom,.11f,.56f,.50f,.29f);
                var tableChrome=table.gameObject.AddComponent<UltraChrome>();tableChrome.color=UltraTheme.Panel;tableChrome.cut=UltraCut.Panel;tableChrome.raycastTarget=false;
                var tableEdge=R("Table accent",table,0,.18f,.016f,.64f);P(tableEdge,UltraTheme.Accent);tableEdge.gameObject.AddComponent<UltraAccentFill>();
                T(table,"Table "+UltraServices.CurrentTable,21,.04f,.82f,.8f,.14f);
                tableAvatars=new Image[required];
                for(int i=0;i<required;i++)
                {
                    float x=required==4?.035f+i*.25f:.10f+i*.69f;
                    var seat=R("Player "+i,table,x,.36f,required==4?.15f:.13f,.38f);
                    seat.gameObject.AddComponent<AspectRatioFitter>().aspectMode=AspectRatioFitter.AspectMode.HeightControlsWidth;
                    tableAvatars[i]=P(seat,new Color(.8f,.8f,.85f));
                }
                tableLabel=T(table,"",19,.025f,.05f,.95f,.21f);tableLabel.alignment=TextAlignmentOptions.Center;
                readyButton=required==4?B(friendRoom,"ENTRY",OnReady,.11f,.43f,.23f,.075f,UltraTier.Primary):B(table,"ENTRY",OnReady,.31f,.39f,.38f,.27f,UltraTier.Primary);
                beginButton=B(friendRoom,"BEGIN DUEL",OnStart,required==4?.38f:.23f,.43f,required==4?.23f:.26f,.075f,UltraTier.Hero);
                B(friendRoom,"Room Details  >",()=>ShowRoomInfo(false),.65f,.775f,.25f,.065f);
                B(friendRoom,"Room Members  >",()=>ShowRoomInfo(true),.65f,.69f,.25f,.065f);
                B(friendRoom,"Replays  >",()=>{Program.instance.room.OnExit();Program.instance.ShiftToServant(Program.instance.replay);},.65f,.605f,.25f,.065f);
                B(friendRoom,"Invite Friends  >",InviteFromRoom,.65f,.52f,.25f,.065f);
                B(friendRoom,"Leave Table  >",()=>Program.instance.room.OnExit(),.65f,.435f,.25f,.065f,UltraTier.Danger);
                var deckPanel=R("Deck preview",friendRoom,.65f,.145f,.25f,.23f);
                var deckChrome=deckPanel.gameObject.AddComponent<UltraChrome>();deckChrome.color=UltraTheme.Panel;deckChrome.cut=UltraCut.Panel;deckChrome.raycastTarget=true;
                var deckHit=deckPanel.gameObject.AddComponent<Button>();deckPanel.GetComponent<Image>().raycastTarget=true;deckHit.targetGraphic=deckPanel.GetComponent<Image>();deckHit.onClick.AddListener(OnSelectDeck);deckPanel.gameObject.AddComponent<UltraButtonMotion>();
                var deckCase=P(R("Deck case",deckPanel,.36f,.31f,.28f,.60f),Color.white);deckCase.preserveAspect=true;LoadFriendDeckCase(deckCase).Forget();
                deckLabel=T(deckPanel,"",21,.04f,.025f,.92f,.22f);deckLabel.alignment=TextAlignmentOptions.Center;
                T(friendRoom,required==4?"DUOS 2v2 / Seats 1–2 vs 3–4":"Choose ENTRY to ready your deck",20,.11f,.31f,.50f,.055f).alignment=TextAlignmentOptions.Center;
                B(friendRoom,"CHAT",()=>Program.instance.ui_.chatPanel.Switch(),.11f,.145f,.20f,.065f);
            }
            foreach(Transform child in Root)if(child!=friendRoom)child.gameObject.SetActive(false);
            friendRoom.gameObject.SetActive(true);friendRoom.SetAsLastSibling();
            ParkGlobalBack();
            int count=0;string names="";bool allReady=true;
            for(int i=0;i<required;i++)
            {
                var p=RoomServant.players[i];if(p!=null && !string.IsNullOrEmpty(p.name)){count++;names+=p.name+(p.ready?"  READY":"  NOT READY");}
                else {names+="Open seat";if(required!=4)allReady=false;}
                if(p!=null)allReady&=p.ready;
                if(i<required-1)names+=required==4?(i==1?"   VS   ":" / "):"                 VS                 ";
                if(i<roomPlayers.Count){tableAvatars[i].sprite=roomPlayers[i].GetAvatar().sprite;tableAvatars[i].preserveAspect=true;tableAvatars[i].color=p==null?new Color(.28f,.28f,.32f):Color.white;if(i==RoomServant.SelfType && p!=null)LoadLocalFace(tableAvatars[i]).Forget();}
            }
            tableLabel.text=names;membersLabel.text=DuelFormat.DisplayName+" / "+(required==4?"DUOS / ":"")+count+" / "+required+" Members";
            deckLabel.text=Config.GetConfigDeckName();
            bool uneven=required==4 && count==3;
            beginButton.interactable=RoomServant.IsHost && (count==required || uneven) && allReady;
            beginButton.GetComponentInChildren<TextMeshProUGUI>().text=uneven?"BEGIN 2v1":"BEGIN DUEL";
            readyButton.interactable=RoomServant.SelfType<required;
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text=RoomServant.SelfType<required && RoomServant.players[RoomServant.SelfType]?.ready==true?"CANCEL":"ENTRY";
        }
        void LateUpdate()
        {
            if(!RoomServant.FromUltraHost) return;
            bool live=(friendRoom!=null && friendRoom.gameObject.activeInHierarchy) || (matchRoom!=null && matchRoom.gameObject.activeInHierarchy);
            if(live) ParkGlobalBack();
        }
        static void ParkGlobalBack()
        {
            var button=Program.instance!=null && Program.instance.ui_!=null ? Program.instance.ui_.btnExit : null;
            if(button==null) return;
            var rect=button.GetComponent<RectTransform>();
            if(rect==null || rect.anchoredPosition.y>40f) return;
            rect.DOKill();
            UIManager.HideExitButton(0f);
        }
        async UniTask LoadLocalFace(Image image)
        {
            int id=1010001;
            if(int.TryParse(Config.Get("DuelFace0","1010001"), out int parsed) && parsed>=1010000 && parsed<1030000) id=parsed;
            var sprite=await Program.items.TryLoadItemIconAsync(id.ToString(), Items.ItemType.Face);
            if(image!=null && sprite!=null){image.sprite=sprite;image.color=Color.white;}
        }
        void ShowRoomInfo(bool members)
        {
            if(roomPopup!=null)Destroy(roomPopup.gameObject);
            roomPopup=R("Room information",friendRoom,.14f,.20f,.44f,.31f);P(roomPopup,new Color(.04f,.045f,.065f,.98f)).raycastTarget=true;
            string text=members?tableLabel.text:DuelFormat.DisplayName+"\n"+(RoomServant.Mode==2?"Duos 2v2":"Friend duel 1v1")+" / "+RoomServant.StartLp+" LP";
            T(roomPopup,text,22,.04f,.23f,.92f,.69f);
            B(roomPopup,"CLOSE",()=>{Destroy(roomPopup.gameObject);roomPopup=null;},.30f,.035f,.40f,.17f);
        }
        async UniTask LoadFriendDeckCase(Image image)
        {
            try{var deck=new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+Config.GetConfigDeckName()+Program.EXPANSION_YDK);var icon=await Program.items.TryLoadItemIconAsync(deck.Case.ToString(),Items.ItemType.Case);if(image!=null)image.sprite=icon;}
            catch(Exception e){Debug.LogWarning(e.Message);}
        }
        async void InviteFromRoom()
        {
            try
            {
                await UltraServices.Refresh();
                var friends=UltraServices.State?["friends"] as JArray;
                if(friends==null || friends.Count==0){MessageManager.Cast("Add a friend from Home > Friends first.");return;}
                var picker=R("Invite friends",friendRoom,.67f,.27f,.30f,.56f);P(picker,new Color(.025f,.05f,.08f)).raycastTarget=true;
                B(picker,"CLOSE",()=>Destroy(picker.gameObject),.05f,.83f,.90f,.12f);
                int i=0;
                foreach(var friend in friends)
                {
                    if(i>=5)break;int id=(int)friend["id"];string name=(string)friend["name"];
                    B(picker,name,()=>SendRoomInvite(id,name,picker).Forget(),.05f,.66f-i++*.14f,.90f,.12f);
                }
            }catch(Exception e){MessageManager.Cast(e.Message);}
        }
        async UniTask SendRoomInvite(int id,string name,RectTransform picker)
        {
            try{await UltraServices.Request("/invites","POST",new{id,room=UltraServices.CurrentRoomId});MessageManager.Cast("Invitation sent to "+name);Destroy(picker.gameObject);await UltraServices.Refresh();}
            catch(Exception e){MessageManager.Cast(e.Message);}
        }
    }
}
