using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using MDPro3.Servant;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public static class UltraAdminAccess
    {
        public static bool IsOwner => !string.IsNullOrEmpty(UltraServices.Token)
            && (bool?)UltraServices.State?["owner"] == true;
    }

    public sealed class UltraAdminDuelUI : MonoBehaviour
    {
        Button launcher;
        RectTransform overlay;
        TextMeshProUGUI status;
        TMP_InputField lifePoints,card;
        bool busy;

        void Start()
        {
            launcher=Button(transform,"ADMIN",()=>Open(),.012f,.93f,.11f,.05f);
            launcher.gameObject.SetActive(false);
        }

        bool CanOpen => UltraAdminAccess.IsOwner && Program.instance.currentServant==Program.instance.ocgcore
            && OcgCore.condition==OcgCore.Condition.Duel && !OcgCore.duelEnded;

        void Update()
        {
            if(launcher!=null && launcher.gameObject.activeSelf!=CanOpen)launcher.gameObject.SetActive(CanOpen);
            if(!CanOpen && overlay!=null)Close();
        }

        public void Open()
        {
            if(!CanOpen)return;
            Close();
            overlay=Rect("Owner duel controls",transform,0,0,1,1);
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.76f);
            var panel=Rect("Admin panel",overlay,.23f,.18f,.54f,.64f);
            var background=panel.gameObject.AddComponent<UltraChrome>();background.color=new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.98f);background.cut=UltraCut.Panel;background.rim=1.5f;
            Text(panel,"ADMIN / CURRENT DUEL",30,.05f,.85f,.75f,.09f);
            Button(panel,"CLOSE",Close,.80f,.86f,.15f,.08f);
            Text(panel,"Actions run only when you press a button. Hand and LP controls require your Main Phase.",18,.05f,.72f,.9f,.12f);
            lifePoints=Input(panel,"Life points",.59f);lifePoints.contentType=TMP_InputField.ContentType.IntegerNumber;lifePoints.text="8000";
            Button(panel,"SET MY LP",()=>SetLife(false),.42f,.59f,.24f,.09f);
            Button(panel,"SET OPPONENT LP",()=>SetLife(true),.68f,.59f,.27f,.09f);
            card=Input(panel,"Exact card name or ID",.45f);
            Button(panel,"ADD TO MY HAND",GiveCard,.56f,.45f,.39f,.09f);
            Button(panel,"CLEAR OPPONENT HAND → GY",()=>Apply("clear-hand",1,0).Forget(),.05f,.30f,.56f,.10f);
            Button(panel,"WIN THIS DUEL",()=>Apply("win",0,0).Forget(),.64f,.30f,.31f,.10f,UltraTier.Danger);
            status=Text(panel,"No changes made.",18,.05f,.055f,.9f,.18f);
        }

        void Close(){if(overlay!=null)Destroy(overlay.gameObject);overlay=null;status=null;}
        void OnDisable()=>Close();

        void SetLife(bool opponent)
        {
            if(!int.TryParse(lifePoints.text,out int value) || value<1 || value>99999999)
            {status.text="Enter life points from 1 to 99,999,999.";return;}
            Apply("set-lp",opponent?1:0,value).Forget();
        }

        void GiveCard()
        {
            string query=card.text.Trim();int id;
            if(!int.TryParse(query,out id))
                id=CardsManager._cards.Values.FirstOrDefault(c=>string.Equals(c.Name,query,StringComparison.OrdinalIgnoreCase))?.Id??0;
            var data=CardsManager.GetCardRaw(id);
            if(data==null || (data.Type&0x4000)!=0){status.text="Enter a valid card's exact name or ID.";return;}
            Apply("give-card",0,id).Forget();
        }

        async UniTaskVoid Apply(string action,int target,int value)
        {
            if(busy || !CanOpen)return;
            busy=true;if(status!=null)status.text="Applying…";
            try
            {
                if(RoomServant.FromUltraHost && !string.IsNullOrEmpty(UltraServices.CurrentRoomId))
                    await UltraServices.Request("/admin/duel","POST",new{room=UltraServices.CurrentRoomId,table=UltraServices.CurrentTable,action,target,value});
                else if(YgoServer.ServerRunning())
                {
                    string actor=Config.Get("DuelPlayerName0",Config.EMPTY_STRING);
                    int operation=action=="win"?1:action=="set-lp"?2:action=="give-card"?3:4;
                    int result=await UniTask.RunOnThreadPool(()=>Dll.admin_duel_action(actor,operation,target,value));
                    if(result!=0)throw new Exception(result==2?"Wait for your Main Phase and finish resolving effects.":result==3?"Invalid value or card, or your hand is full.":"You must be seated in an active supported duel.");
                }
                else throw new Exception("These controls require an Ultra Duel friend room or a local Solo duel.");
                if(status!=null)status.text="Applied: "+action+".";
            }
            catch(Exception error){if(status!=null)status.text=error.Message;}
            finally{busy=false;}
        }

        static RectTransform Rect(string name,Transform parent,float x,float y,float w,float h)
        {
            var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.gameObject.layer=parent.gameObject.layer;
            r.anchorMin=new Vector2(x,y);r.anchorMax=new Vector2(x+w,y+h);r.offsetMin=r.offsetMax=Vector2.zero;return r;
        }
        static TextMeshProUGUI Text(Transform parent,string value,float size,float x,float y,float w,float h)
        {
            var t=UltraUi.Text(Rect("Label",parent,x,y,w,h).gameObject);
            t.font=Program.instance.ui_.tmpFont;t.text=value;t.fontSize=size;t.color=UltraTheme.Fg;t.raycastTarget=false;
            t.enableAutoSizing=true;t.fontSizeMin=size*.7f;t.fontSizeMax=size;t.alignment=TextAlignmentOptions.MidlineLeft;
            UltraTheme.Style(t,size>=26f,size>=26f?4f:0f);return t;
        }
        static Button Button(Transform parent,string label,UnityAction action,float x,float y,float w,float h,UltraTier tier=UltraTier.Secondary)
        {
            var r=Rect(label,parent,x,y,w,h);var image=r.gameObject.AddComponent<UltraChrome>();image.raycastTarget=true;
            var b=r.gameObject.AddComponent<Button>();b.targetGraphic=image;
            Text(r,label,19,.04f,.05f,.92f,.9f).alignment=TextAlignmentOptions.Center;
            if(tier==UltraTier.Secondary && UltraTheme.IsDanger(label))tier=UltraTier.Danger;
            r.gameObject.AddComponent<UltraThemedButton>().Configure(tier);
            b.onClick.AddListener(action);return b;
        }
        static TMP_InputField Input(Transform parent,string hint,float y)
        {
            var r=Rect("Input",parent,.05f,y,hint=="Life points"?.34f:.48f,.09f);
            var image=r.gameObject.AddComponent<UltraChrome>();image.color=UltraTheme.PanelSolid;image.cut=UltraCut.Small;image.raycastTarget=true;
            var input=r.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=image;input.characterLimit=120;
            input.textViewport=Rect("Viewport",r,.035f,.05f,.93f,.9f);
            input.textComponent=Text(input.textViewport,"",20,0,0,1,1);
            var placeholder=Text(input.textViewport,hint,18,0,0,1,1);placeholder.color=new Color(.6f,.63f,.68f);input.placeholder=placeholder;
            return input;
        }
    }
}
