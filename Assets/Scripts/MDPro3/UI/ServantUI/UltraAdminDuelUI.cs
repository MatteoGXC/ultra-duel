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
        TMP_InputField lifePoints,card,attackPoints;
        TextMeshProUGUI monsterLabel,myMonsterLabel;
        int monsterZone,myMonsterZone;
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
            Program.instance.ui_.chatPanel.Hide();
            overlay=Rect("Owner duel controls",transform,0,0,1,1);
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.76f);
            var panel=Rect("Admin panel",overlay,.12f,.04f,.76f,.92f);
            panel.gameObject.AddComponent<UltraPopupFrame>();
            var background=panel.gameObject.AddComponent<UltraChrome>();background.color=new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.98f);background.cut=UltraCut.Panel;background.rim=1.5f;
            Text(panel,"ADMIN / CURRENT DUEL",30,.05f,.91f,.75f,.07f);
            Button(panel,"CLOSE",Close,.80f,.91f,.15f,.065f);
            Text(panel,"Use a free Main/Battle Phase prompt, after effects resolve. Add cards / LP: your Main Phase.",18,.05f,.84f,.9f,.06f);
            lifePoints=Input(panel,"Life points",.75f);lifePoints.contentType=TMP_InputField.ContentType.IntegerNumber;lifePoints.text="8000";
            Button(panel,"SET MY LP",()=>SetLife(false),.42f,.75f,.24f,.08f);
            Button(panel,"SET OPPONENT LP",()=>SetLife(true),.68f,.75f,.27f,.08f);
            card=Input(panel,"Exact card name or ID",.66f);
            Button(panel,"ADD TO MY HAND",GiveCard,.56f,.66f,.39f,.08f);
            var select=Button(panel,"",CycleMonster,.05f,.57f,.9f,.08f);
            monsterLabel=select.GetComponentInChildren<TextMeshProUGUI>();
            var mySelect=Button(panel,"",()=>{myMonsterZone=(myMonsterZone+1)%7;RefreshMonster();},.05f,.48f,.9f,.08f);
            myMonsterLabel=mySelect.GetComponentInChildren<TextMeshProUGUI>();
            RefreshMonster();
            attackPoints=Input(panel,"Attack points",.39f);attackPoints.contentType=TMP_InputField.ContentType.IntegerNumber;attackPoints.text="0";
            Button(panel,"SET MY ATK",()=>SetAttack(false),.42f,.39f,.24f,.08f);
            Button(panel,"SET OPPONENT ATK",()=>SetAttack(true),.68f,.39f,.27f,.08f);
            Button(panel,"OPPONENT → DEFENSE",()=>Apply("defense",1,monsterZone).Forget(),.05f,.30f,.29f,.08f);
            Button(panel,"ALL OPPONENT → DEFENSE",()=>Apply("defense",1,-1).Forget(),.355f,.30f,.29f,.08f);
            Button(panel,"OPPONENT → 0 ATK",()=>Apply("zero-atk",1,monsterZone).Forget(),.66f,.30f,.29f,.08f);
            Button(panel,"CLEAR OPPONENT HAND → GY",()=>Apply("clear-hand",1,0).Forget(),.05f,.21f,.29f,.08f);
            Button(panel,"CLEAR OPPONENT FIELD → GY",()=>Apply("clear-field",1,0).Forget(),.355f,.21f,.29f,.08f);
            Button(panel,"CLEAR HAND + FIELD → GY",()=>Apply("clear-hand-field",1,0).Forget(),.66f,.21f,.29f,.08f,UltraTier.Danger);
            Button(panel,"END OPPONENT TURN",()=>Apply("skip-turn",1,0).Forget(),.05f,.12f,.44f,.08f,UltraTier.Danger);
            Button(panel,"WIN THIS DUEL",()=>Apply("win",0,0).Forget(),.51f,.12f,.44f,.08f,UltraTier.Danger);
            status=Text(panel,"ATK changes last while face-up on the field. Link monsters cannot enter Defense.",17,.05f,.025f,.9f,.08f);
            UltraGuiScale.FitPopup(panel,UltraGuiScale.Percent/100f);
        }

        void CycleMonster()
        {
            monsterZone=(monsterZone+1)%7;
            RefreshMonster();
        }

        void RefreshMonster()
        {
            SetMonsterLabel(monsterLabel,1,monsterZone,"OPPONENT");
            SetMonsterLabel(myMonsterLabel,0,myMonsterZone,"MY CARD");
        }

        void SetMonsterLabel(TextMeshProUGUI label,int side,int slot,string heading)
        {
            if(label==null)return;
            var selected=OcgCore.cards.FirstOrDefault(c=>c.p.controller==side
                && c.p.location==(uint)CardLocation.MonsterZone && c.p.sequence==slot);
            string zone=slot<5?"Monster zone "+(slot+1):"Extra monster zone "+(slot-4);
            string name=selected==null?"Empty":selected.p.InPosition(CardPosition.FaceDown)?"Face-down monster":selected.GetData().Name;
            label.text=heading+": "+zone+" — "+name+"   [NEXT >]";
        }

        void SetAttack(bool opponent)
        {
            if(!int.TryParse(attackPoints.text,out int value) || value<0 || value>99999999)
            {status.text="Enter ATK from 0 to 99,999,999.";return;}
            Apply("set-atk",opponent?1:0,value).Forget();
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
                    await UltraServices.Request("/admin/duel","POST",new{room=UltraServices.CurrentRoomId,table=UltraServices.CurrentTable,action,target,value,zone=target==0?myMonsterZone:monsterZone});
                else if(YgoServer.ServerRunning())
                {
                    string actor=Config.Get("DuelPlayerName0",Config.EMPTY_STRING);
                    int operation=action switch {
                        "win"=>1,"set-lp"=>2,"give-card"=>3,"clear-hand"=>4,"clear-field"=>5,
                        "clear-hand-field"=>6,"defense"=>7,"zero-atk"=>8,"skip-turn"=>9,
                        "set-atk"=>10,
                        _=>throw new Exception("Unknown admin action.")};
                    int nativeValue=action=="set-atk"?checked(value*8+(target==0?myMonsterZone:monsterZone)):value;
                    int result=await UniTask.RunOnThreadPool(()=>Dll.admin_duel_action(actor,operation,target,nativeValue));
                    if(result!=0)throw new Exception(result==2?"Wait for a free command prompt. Add/LP: your Main Phase. End turn: their turn.":result==3?"Invalid card/value, full hand, or no eligible monster. Links cannot defend; 0 ATK needs a face-up monster.":"You must be seated in an active supported duel.");
                }
                else throw new Exception("These controls require an Ultra Duel friend room or a local Solo duel.");
                if(status!=null)status.text="Applied: "+action+".";
                RefreshMonster();
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
            var r=Rect("Input",parent,.05f,y,hint=="Exact card name or ID"?.48f:.34f,.08f);
            var image=r.gameObject.AddComponent<UltraChrome>();image.color=UltraTheme.PanelSolid;image.cut=UltraCut.Small;image.raycastTarget=true;
            var input=r.gameObject.AddComponent<TMP_InputField>();input.targetGraphic=image;input.characterLimit=120;
            input.textViewport=Rect("Viewport",r,.035f,.05f,.93f,.9f);
            input.textComponent=Text(input.textViewport,"",20,0,0,1,1);
            var placeholder=Text(input.textViewport,hint,18,0,0,1,1);placeholder.color=new Color(.6f,.63f,.68f);input.placeholder=placeholder;
            return input;
        }
    }
}
