using System;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public partial class RoomServantUI
    {
        RectTransform friendRoom;
        TextMeshProUGUI tableLabel, membersLabel, deckLabel;
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
            var t=R("Label",p,x,y,w,h).gameObject.AddComponent<TextMeshProUGUI>();t.text=s;t.font=TMP_Settings.defaultFontAsset;
            t.fontSize=size;t.color=Color.white;t.raycastTarget=false;t.enableAutoSizing=true;t.fontSizeMin=size*.7f;t.fontSizeMax=size;return t;
        }
        Button B(Transform p,string s,UnityEngine.Events.UnityAction action,float x,float y,float w,float h)
        {
            var r=R(s,p,x,y,w,h);var image=P(r,new Color(.05f,.10f,.15f));image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            var t=T(r,s,23,.04f,.1f,.92f,.8f);t.alignment=TextAlignmentOptions.Center;
            var colors=b.colors;colors.highlightedColor=colors.selectedColor=new Color(.7f,1,.15f);b.colors=colors;return b;
        }
        void RefreshFriendRoom()
        {
            if(!RoomServant.FromUltraHost){if(friendRoom!=null)friendRoom.gameObject.SetActive(false);return;}
            if(friendRoom==null)
            {
                friendRoom=R("Ultra Friend Room",Root,0,0,1,1);P(friendRoom,new Color(.015f,.025f,.055f,.98f)).raycastTarget=true;
                var diagonal=R("Diagonal",friendRoom,.34f,0,.65f,1).gameObject.AddComponent<UltraHomeShape>();diagonal.slash=true;diagonal.color=new Color(.09f,.12f,.23f);diagonal.raycastTarget=false;
                B(friendRoom,"<",()=>Program.instance.room.OnExit(),.025f,.90f,.045f,.065f);
                T(friendRoom,"ROOM MATCH",33,.09f,.90f,.35f,.065f);
                membersLabel=T(friendRoom,"",22,.51f,.90f,.44f,.065f);
                var table=R("Table 1",friendRoom,.08f,.49f,.56f,.32f);P(table,new Color(.18f,.20f,.27f));
                P(R("Table header",table,0,.85f,1,.15f),new Color(.65f,.68f,.73f));
                T(table,"TABLE 1",19,.025f,.85f,.8f,.15f).color=Color.black;
                tableAvatars=new Image[2];
                for(int i=0;i<2;i++)tableAvatars[i]=P(R("Player "+i,table,.04f+i*.72f,.34f,.20f,.43f),Color.white);
                tableLabel=T(table,"",22,.04f,.04f,.92f,.25f);
                beginButton=B(table,"BEGIN DUEL",OnStart,.28f,.39f,.44f,.30f);
                readyButton=B(friendRoom,"READY",OnReady,.08f,.34f,.26f,.095f);
                B(friendRoom,"CHOOSE DECK",OnSelectDeck,.38f,.34f,.26f,.095f);
                B(friendRoom,"INVITE FRIENDS",InviteFromRoom,.70f,.67f,.25f,.085f);
                B(friendRoom,"LEAVE ROOM",()=>Program.instance.room.OnExit(),.70f,.55f,.25f,.085f);
                var deckPanel=R("Deck preview",friendRoom,.70f,.17f,.25f,.29f);P(deckPanel,new Color(.08f,.10f,.18f));
                T(deckPanel,"SELECTED DECK",18,.06f,.76f,.88f,.16f);
                deckLabel=T(deckPanel,"",27,.06f,.20f,.88f,.45f);
                T(friendRoom,"Private friend room / Both players must be ready",20,.08f,.20f,.56f,.07f);
            }
            friendRoom.gameObject.SetActive(true);friendRoom.SetAsLastSibling();
            int count=0;string names="";
            for(int i=0;i<2;i++)
            {
                var p=RoomServant.players[i];if(p!=null && !string.IsNullOrEmpty(p.name)){count++;names+=p.name+(p.ready?"  READY":"  NOT READY");}
                else names+="Waiting for player";
                if(i==0)names+="                 VS                 ";
                if(i<roomPlayers.Count){tableAvatars[i].sprite=roomPlayers[i].GetAvatar().sprite;tableAvatars[i].preserveAspect=true;}
            }
            tableLabel.text=names;membersLabel.text=DuelFormat.DisplayName+" / "+count+" / 2 Members";
            deckLabel.text=Config.GetConfigDeckName();
            beginButton.interactable=RoomServant.IsHost && count==2 && RoomServant.players[0].ready && RoomServant.players[1].ready;
            readyButton.interactable=RoomServant.SelfType<2;
            readyButton.GetComponentInChildren<TextMeshProUGUI>().text=RoomServant.SelfType<2 && RoomServant.players[RoomServant.SelfType]?.ready==true?"CANCEL READY":"READY";
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
            try{await UltraServices.Request("/invites","POST",new{id,room=UltraServices.CurrentRoomId});MessageManager.Cast("Invitation sent to "+name);Destroy(picker.gameObject);}
            catch(Exception e){MessageManager.Cast(e.Message);}
        }
    }
}
