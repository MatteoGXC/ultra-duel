using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using MDPro3.Net;
using MDPro3.Servant;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    // A small mesh keeps the home navigation angular at every screen size.
    public class UltraHomeShape : MaskableGraphic
    {
        public bool slash;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear(); var r = rectTransform.rect;
            Vector2[] points = slash
                ? new[] { new Vector2(0, 0), new Vector2(.76f, 0), new Vector2(1, 1), new Vector2(.24f, 1) }
                : new[] { new Vector2(0, 0), new Vector2(.61f, 0), new Vector2(1, 1), new Vector2(0, 1) };
            foreach (var p in points) vh.AddVert(new Vector3(r.xMin + p.x*r.width, r.yMin+p.y*r.height), color, Vector2.zero);
            vh.AddTriangle(0,1,2); vh.AddTriangle(0,2,3);
        }
    }

    public partial class MainMenuUI
    {
        static readonly Color Lime = new Color(.75f, 1f, .10f);
        static readonly Color Muted = new Color(.60f, .70f, .78f);
        RectTransform ultraHome, overlay, modal;
        TextMeshProUGUI profileLabel, gemLabel, passLabel, updateLabel, friendLabel, modalStatus;
        Image profileAccent;
        bool actionBusy;
        string modalPage;

        void OnDestroy()
        {
            UltraServices.Changed -= RefreshUltraHome;
            UltraProgress.Changed -= RefreshUltraHome;
        }
        Color FrameColor()
        {
            return UltraProgress.Data.frame switch {
                "Violet" => new Color(.72f,.35f,1), "Emerald" => new Color(.15f,1,.65f),
                "Neon" => new Color(.15f,.85f,1), "Champion" => new Color(1,.78f,.22f), _ => Lime };
        }
        void BuildUltraHome()
        {
            foreach (Transform child in Root) child.gameObject.SetActive(false);
            ultraHome = HomeRect("Ultra Duel Home", Root, 0, 0, 1, 1);
            HomePanel(ultraHome, new Color(.018f,.029f,.053f,0));
            var diagonal=HomeRect("Midnight diagonal",ultraHome,.23f,0,.77f,1).gameObject.AddComponent<UltraHomeShape>();
            diagonal.slash=true; diagonal.color=new Color(.045f,.085f,.14f,.10f); diagonal.raycastTarget=false;
            var glow=HomeRect("Blue diagonal",ultraHome,.62f,0,.38f,1).gameObject.AddComponent<UltraHomeShape>();
            glow.slash=true; glow.color=new Color(.055f,.15f,.20f,.08f); glow.raycastTarget=false;
            var nav=HomeRect("Angular navigation",ultraHome,0,0,.52f,1).gameObject.AddComponent<UltraHomeShape>();
            nav.color=new Color(.006f,.012f,.021f,.98f); nav.raycastTarget=false;
            // Keep the player’s wallpaper and mate visible through the right side.
            for (int i=0;i<28;i++)
            {
                float x=.38f+((i*37)%59)/100f, y=.12f+((i*19)%71)/100f;
                HomePanel(HomeRect("Light",ultraHome,x,y,.0012f,.002f),new Color(.24f,.61f,.73f,.30f));
            }
            var profile=HomeRect("Profile",ultraHome,.035f,.855f,.265f,.105f);
            HomePanel(profile,new Color(.035f,.055f,.07f));
            profileAccent=HomeRect("Profile frame",profile,0,0,.012f,1).gameObject.AddComponent<Image>();
            profileAccent.raycastTarget=false;
            profileLabel=HomeText(profile,"",22,Color.white,.06f,.12f,.88f,.76f);
            var profileClick=profile.gameObject.AddComponent<Button>(); profileClick.onClick.AddListener(OpenFriends);
            profileClick.targetGraphic=profile.GetComponent<Image>();
            profile.GetComponent<Image>().raycastTarget=true;
            profile.GetComponent<Image>().raycastTarget=true;
            gemLabel=HomeText(ultraHome,"",23,new Color(.70f,.88f,1),.40f,.90f,.12f,.05f);
            SmallButton(ultraHome,"UPDATES",()=>UltraServices.CheckUpdates(true),.54f,.885f,.10f,.065f);
            SmallButton(ultraHome,"MISSIONS",OpenMissions,.65f,.885f,.10f,.065f);
            SmallButton(ultraHome,"FRIENDS",OpenFriends,.76f,.885f,.10f,.065f);
            SmallButton(ultraHome,"MENU",OpenMenu,.87f,.885f,.09f,.065f);
            defaultSelectable=NavButton("DUEL",OpenDuel,.66f,53);
            NavButton("DECK",OnDeck,.565f,36);
            NavButton("SOLO",OnSolo,.475f,36);
            NavButton("SHOP",OpenShop,.385f,36);
            HomeText(ultraHome,"ULTRA DUEL",32,Color.white,.065f,.765f,.29f,.06f).fontStyle=FontStyles.Bold;
            HomeText(ultraHome,"YOUR DECK. YOUR NEXT MOVE.",16,Muted,.065f,.735f,.30f,.03f);
            var info=HomeRect("Home status backdrop",ultraHome,.53f,.065f,.44f,.17f);
            HomePanel(info,new Color(.008f,.02f,.04f,.76f));
            var deckPanel=HomeRect("Selected deck",ultraHome,.045f,.19f,.27f,.15f);
            HomePanel(deckPanel,new Color(.055f,.075f,.12f));
            HomeText(deckPanel,"READY TO PLAY",17,Lime,.055f,.69f,.90f,.22f);
            homeStatus=HomeText(deckPanel,"",23,Color.white,.055f,.13f,.90f,.53f);
            SmallButton(deckPanel,"CHANGE",OnDeck,.69f,.73f,.27f,.20f,14);
            var pass=HomeRect("Duel Pass",ultraHome,.045f,.055f,.27f,.105f);
            HomePanel(pass,new Color(.09f,.085f,.23f));
            passLabel=HomeText(pass,"",23,Color.white,.055f,.12f,.90f,.78f);
            pass.GetComponent<Image>().raycastTarget=true;
            pass.GetComponent<Image>().raycastTarget=true;
            var passButton=pass.gameObject.AddComponent<Button>(); passButton.targetGraphic=pass.GetComponent<Image>(); passButton.onClick.AddListener(OpenPass);
            friendLabel=HomeText(ultraHome,"",17,Muted,.55f,.16f,.41f,.06f);
            updateLabel=HomeText(ultraHome,"",16,Muted,.55f,.09f,.41f,.05f);
            HomeText(ultraHome,"v"+UltraServices.Version,15,Muted,.90f,.025f,.07f,.03f);
            UltraServices.Initialize();
            UltraServices.Changed += RefreshUltraHome;
            UltraProgress.Changed += RefreshUltraHome;
            RefreshUltraHome();
            UltraServices.CheckUpdates();
        }
        Button NavButton(string title,UnityAction action,float y,float size)
        {
            var rect=HomeRect(title,ultraHome,.065f,y,.29f,.075f);
            var image=rect.gameObject.AddComponent<Image>();image.color=Color.white;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=new Color(0,0,0,0);colors.highlightedColor=colors.selectedColor=new Color(.16f,.24f,.22f,.8f);colors.pressedColor=new Color(.24f,.35f,.24f);button.colors=colors;
            HomePanel(HomeRect("Accent",rect,0,.20f,.005f,.60f),Lime);
            HomeText(rect,title,size,Color.white,.025f,0,.94f,1).fontStyle=FontStyles.Bold;
            button.onClick.AddListener(action);return button;
        }
        Button SmallButton(Transform parent,string text,UnityAction action,float x,float y,float w,float h,float size=18)
        {
            var rect=HomeRect(text,parent,x,y,w,h);
            var image=rect.gameObject.AddComponent<Image>();image.color=new Color(.08f,.12f,.17f);
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=colors.selectedColor=new Color(.60f,.85f,.75f);colors.pressedColor=new Color(.40f,.65f,.50f);button.colors=colors;
            var label=HomeText(rect,text,size,Color.white,.05f,.05f,.90f,.90f);label.alignment=TextAlignmentOptions.Center;
            button.onClick.AddListener(action);return button;
        }
        void RefreshUltraHome()
        {
            if (profileLabel==null) return;
            var data=UltraProgress.Data;
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            profileLabel.text=name+"   >\n<size=70%>Lv. "+UltraProgress.Level+"  /  "+data.title+"</size>";
            if (string.IsNullOrWhiteSpace(name)) name="Player";
            profileLabel.text=name+"   >\n<size=70%>Lv. "+UltraProgress.Level+"  /  "+data.title+"</size>";
            profileAccent.color=FrameColor();gemLabel.text="GEMS  "+data.gems;
            passLabel.text="DUEL PASS   "+UltraProgress.Tier+" / 20\n<size=65%>"+(UltraProgress.Tier>data.claimed?"Rewards ready to claim":"WIN DUELS · EARN REWARDS")+"</size>";
            var selectedDeck = new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+Config.GetConfigDeckName()+Program.EXPANSION_YDK);
            homeStatus.text=Config.GetConfigDeckName()+"\n<size=70%>"+(selectedDeck.Format=="Genesys"?"Genesys / "+selectedDeck.FormatPointCap+" points":"TCG Advanced")+"</size>";
            updateLabel.text=UltraServices.UpdateStatus;
            var state=UltraServices.State;
            friendLabel.text=state==null?"FRIENDS  /  Sign in to connect and host duels":
                "FRIENDS  /  "+(state["friends"] as JArray)?.Count+" added   ·   "+(state["requests"] as JArray)?.Count+" requests";
        }
        void CloseModal()
        {
            if(overlay!=null) Destroy(overlay.gameObject);
            overlay=modal=null;modalStatus=null;modalPage=null;
        }
        RectTransform OpenModal(string title,string subtitle,string page)
        {
            CloseModal();modalPage=page;
            overlay=HomeRect("Modal overlay",ultraHome,0,0,1,1);
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.82f);
            modal=HomeRect(title,overlay,.16f,.10f,.68f,.80f);
            HomePanel(modal,new Color(.025f,.045f,.075f,1));
            HomePanel(HomeRect("Top rule",modal,0,.99f,1,.005f),Lime);
            HomeText(modal,title,34,Color.white,.045f,.875f,.79f,.09f).fontStyle=FontStyles.Bold;
            HomeText(modal,subtitle,18,Muted,.045f,.79f,.90f,.07f);
            SmallButton(modal,"CLOSE",CloseModal,.84f,.89f,.12f,.06f,16);
            modalStatus=HomeText(modal,"",16,Lime,.045f,.095f,.90f,.035f);
            return modal;
        }
        TMP_InputField Input(Transform parent,string placeholder,float y,bool password=false)
        {
            var rect=HomeRect(placeholder,parent,.055f,y,.89f,.09f);HomePanel(rect,new Color(.10f,.15f,.19f));
            rect.GetComponent<Image>().raycastTarget=true;
            var field=rect.gameObject.AddComponent<TMP_InputField>();
            var text=HomeText(rect,"",24,Color.white,.035f,.08f,.93f,.84f);
            var hint=HomeText(rect,placeholder,23,Muted,.035f,.08f,.93f,.84f);
            field.textViewport=rect;field.textComponent=text;field.placeholder=hint;
            field.characterLimit=password?128:24;
            field.contentType=password?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;
            return field;
        }
        async UniTaskVoid Run(Func<UniTask> work)
        {
            if(actionBusy)return;actionBusy=true;
            if(modalStatus!=null)modalStatus.text="Working…";
            try { await work(); }
            catch(Exception e) { if(modalStatus!=null)modalStatus.text=e.Message;else MessageManager.Cast(e.Message); }
            finally { actionBusy=false; }
        }
        void OpenFriends()
        {
            UltraServices.Initialize();
            if(string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            ShowFriends();
            Run(async()=>{await UltraServices.Refresh();if(modalPage=="friends")ShowFriends();});
        }
        void OpenLogin()
        {
            OpenModal("FRIENDS","Create an Ultra Duel account, or sign in. Your deck files stay on this PC.","login");
            var name=Input(modal,"Username (3–24 characters)",.61f);
            var password=Input(modal,"Password (8 characters minimum)",.47f,true);
            SmallButton(modal,"SIGN IN",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,false);password.text="";ShowFriends();}),.055f,.29f,.42f,.09f);
            SmallButton(modal,"CREATE ACCOUNT",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,true);password.text="";ShowFriends();}),.525f,.29f,.42f,.09f);
            HomeText(modal,"Connect with friends, see who is online, and join private TCG or Genesys rooms.",20,Muted,.055f,.13f,.89f,.11f);
        }
        RectTransform ScrollArea()
        {
            var viewport=HomeRect("List viewport",modal,.045f,.135f,.91f,.50f);
            var image=viewport.gameObject.AddComponent<Image>();image.color=new Color(.02f,.035f,.06f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=HomeRect("List content",viewport,0,1,1,0);content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=30;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        RectTransform Row(RectTransform content,int index,string title,string detail)
        {
            var row=HomeRect("Row",content,0,1,1,0);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,70);row.anchoredPosition=new Vector2(0,-index*76);
            HomePanel(row,new Color(.065f,.095f,.14f));
            HomeText(row,title,23,Color.white,.025f,.43f,.67f,.48f);
            HomeText(row,detail,17,Muted,.025f,.08f,.67f,.35f);
            content.sizeDelta=new Vector2(0,(index+1)*76);
            return row;
        }
        void ShowFriends()
        {
            OpenModal("FRIENDS",UltraServices.PlayerName+"  /  Friends can join your private rooms.","friends");
            var add=Input(modal,"Add friend by username",.67f);
            add.GetComponent<RectTransform>().anchorMax=new Vector2(.73f,.76f);
            SmallButton(modal,"ADD",()=>Run(async()=>{await UltraServices.Request("/friends/request","POST",new{name=add.text});await UltraServices.Refresh();ShowFriends();}),.76f,.67f,.19f,.09f);
            var content=ScrollArea();int index=0;
            var state=UltraServices.State;
            foreach(var person in state?["requests"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],"Incoming friend request");
                SmallButton(row,"ACCEPT",()=>Run(async()=>{await UltraServices.Request("/friends/accept","POST",new{id});await UltraServices.Refresh();ShowFriends();}),.69f,.20f,.17f,.60f,16);
                SmallButton(row,"X",()=>RemoveFriend(id),.88f,.20f,.09f,.60f,16);
            }
            foreach(var person in state?["friends"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],(bool)person["online"]?"Online":"Offline");
                SmallButton(row,"DUEL",OpenDuel,.69f,.20f,.17f,.60f,16);
                SmallButton(row,"X",()=>UIManager.ShowPopupYesOrNo(new List<string>{"Remove friend","Remove this player from your friends?","Remove","Cancel",Config.STRING_YES},()=>RemoveFriend(id),null),.88f,.20f,.09f,.60f,16);
            }
            foreach(var person in state?["sent"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],"Friend request sent");
                SmallButton(row,"CANCEL",()=>RemoveFriend(id),.72f,.20f,.25f,.60f,16);
            }
            if(index==0)Row(content,index,"Your friends will appear here","Add a friend by their Ultra Duel username.");
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();ShowFriends();}),.045f,.025f,.18f,.065f,16);
            SmallButton(modal,"SIGN OUT",()=>Run(async()=>{await UltraServices.SignOut();OpenLogin();}),.77f,.025f,.18f,.065f,16);
        }
        void RemoveFriend(int id) {Run(async()=>{await UltraServices.Request("/friends/"+id,"DELETE");await UltraServices.Refresh();ShowFriends();});}
        void OpenDuel()
        {
            if(string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            ShowRooms();Run(async()=>{await UltraServices.Refresh();if(modalPage=="rooms")ShowRooms();});
        }
        void ShowRooms()
        {
            OpenModal("DUEL","Hosted on Ultra Duel’s server. Choose your deck and format before creating a room.","rooms");
            SmallButton(modal,DuelFormat.DisplayName,()=>DuelFormat.Select(ShowRooms),.045f,.67f,.42f,.085f,19);
            SmallButton(modal,"CREATE ROOM",()=>Run(async()=>{
                var deck=new MDPro3.Duel.YGOSharp.Deck(Program.PATH_DECK+Config.GetConfigDeckName()+Program.EXPANSION_YDK);
                if(!DuelFormat.Validate(deck,"Your Deck"))return;
                var room=await UltraServices.Request("/rooms","POST",new{format=DuelFormat.Genesys?"Genesys":"TCG",cap=DuelFormat.PointCap});
                CloseModal();await UltraServices.JoinRoom(room);
            }),.51f,.67f,.44f,.085f);
            var content=ScrollArea();int index=0;
            foreach(var room in UltraServices.State?["rooms"] as JArray ?? new JArray())
            {
                string id=(string)room["id"];var row=Row(content,index++,(string)room["host"]+"’s room",(string)room["format"]+((string)room["format"]=="Genesys"?" · "+(int)room["cap"]+" points":" Advanced"));
                SmallButton(row,"JOIN",()=>Run(async()=>{var join=await UltraServices.Request("/rooms/"+id+"/join","POST",new{});CloseModal();await UltraServices.JoinRoom(join);}),.69f,.20f,.17f,.60f,16);
                if((int)room["hostId"]==(int)UltraServices.State["id"])
                    SmallButton(row,"X",()=>Run(async()=>{await UltraServices.Request("/rooms/"+id,"DELETE");await UltraServices.Refresh();ShowRooms();}),.88f,.20f,.09f,.60f,16);
            }
            if(index==0)Row(content,0,"No friend rooms yet","Create a room, then ask a friend to open DUEL and join.");
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();ShowRooms();}),.045f,.025f,.18f,.065f,16);
            SmallButton(modal,"OTHER SERVERS",()=>{CloseModal();OnOnline();},.70f,.025f,.25f,.065f,16);
        }
        void OpenPass()
        {
            var data=UltraProgress.Data;
            OpenModal("DUEL PASS  /  FREE","Earn 100 XP for each Solo or multiplayer win. Two wins unlock each tier. No expiry.","pass");
            HomeText(modal,"TIER "+UltraProgress.Tier+" / 20",37,Color.white,.055f,.64f,.89f,.12f);
            HomePanel(HomeRect("XP track",modal,.055f,.58f,.89f,.018f),new Color(.13f,.17f,.22f));
            HomePanel(HomeRect("XP progress",modal,.055f,.58f,.89f*(UltraProgress.Tier>=20?1:(data.xp%200)/200f),.018f),Lime);
            HomeText(modal,data.wins+" wins  ·  "+data.xp+" XP  ·  "+data.gems+" gems\n\nEvery tier: 50 gems\nTier 5: Rising Duelist title\nTier 10: Ultra Challenger title + Neon frame\nTier 20: Ultra Champion title + Champion frame",23,Color.white,.055f,.22f,.89f,.32f);
            var claim=SmallButton(modal,"CLAIM REWARDS",()=>{int count=UltraProgress.Claim();OpenPass();modalStatus.text=count>0?"Claimed "+count+" tiers. Your rewards are ready.":"Win duels to unlock your next tier.";},.055f,.11f,.54f,.085f);
            claim.interactable=UltraProgress.Tier>data.claimed;
            SmallButton(modal,"SHOP",OpenShop,.65f,.11f,.295f,.085f);
            modalStatus.text="Progress and cosmetic gems are saved on this PC.";
        }
        void OpenMissions()
        {
            OpenModal("MISSIONS","Play Solo or duel a friend to progress your free Duel Pass.","missions");
            HomeText(modal,"WIN DUELS\n\nWin a duel: +100 XP\nNext Duel Pass tier: "+(UltraProgress.Tier>=20?"Complete":(200-UltraProgress.Data.xp%200)+" XP")+"\n\nReplays, spectators and hand tests do not award XP.",28,Color.white,.055f,.29f,.89f,.43f);
            SmallButton(modal,"DUEL PASS",OpenPass,.055f,.13f,.42f,.09f);
            SmallButton(modal,"PLAY SOLO",()=>{CloseModal();OnSolo();},.525f,.13f,.42f,.09f);
        }
        void OpenShop()
        {
            OpenModal("SHOP",UltraProgress.Data.gems+" gems  /  Profile frames purchased with free Duel Pass rewards.","shop");
            HomeText(modal,"VIOLET FRAME  ·  150 gems\n\nEMERALD FRAME  ·  250 gems",29,Color.white,.055f,.39f,.62f,.33f);
            SmallButton(modal,"VIOLET",()=>BuyFrame("Violet"),.055f,.22f,.42f,.10f);
            SmallButton(modal,"EMERALD",()=>BuyFrame("Emerald"),.525f,.22f,.42f,.10f);
            modalStatus.text="Frames appear on your home profile. No paid purchases.";
        }
        void BuyFrame(string frame)
        {
            if(UltraProgress.Data.frame==frame){modalStatus.text="This frame is already equipped.";return;}
            bool bought=UltraProgress.Buy(frame);OpenShop();modalStatus.text=bought?frame+" frame equipped.":"Earn more gems through the Duel Pass.";
        }
        void OpenWallpaper()
        {
            CloseModal();
            var appearance=Program.instance.appearance;
            appearance.SwitchCondition(Appearance.Condition.Duel);
            appearance.returnAction=()=>{appearance.returnAction=null;Program.instance.ShiftToServant(Program.instance.menu);};
            Program.instance.ShiftToServant(appearance);
            SelectWallpaperPage().Forget();
        }
        async UniTask SelectWallpaperPage()
        {
            var appearance=Program.instance.appearance;
            await UniTask.WaitUntil(()=>appearance.servantUI!=null && appearance.servantUI.gameObject.activeInHierarchy);
            await UniTask.Delay(600);
            var page=appearance.GetUI<AppearanceUI>().GetComponentsInChildren<SelectionToggle_AppearanceGenre>(true);
            foreach(var tab in page)
                if(tab.name.Contains("Wallpaper")){tab.SetToggleOn();break;}
        }
        void OpenMenu()
        {
            OpenModal("MENU","Ultra Duel  /  v"+UltraServices.Version,"menu");
            SmallButton(modal,"SETTINGS",()=>{CloseModal();OnSetting();},.055f,.61f,.42f,.10f);
            SmallButton(modal,"WALLPAPER",OpenWallpaper,.525f,.61f,.42f,.10f);
            SmallButton(modal,"REPLAYS",()=>{CloseModal();OnReplay();},.055f,.46f,.42f,.10f);
            SmallButton(modal,"PUZZLES",()=>{CloseModal();OnPuzzle();},.525f,.46f,.42f,.10f);
            SmallButton(modal,"CUT-INS",()=>{CloseModal();OnCutin();},.055f,.31f,.42f,.10f);
            SmallButton(modal,"CHECK UPDATES",()=>UltraServices.CheckUpdates(true),.525f,.31f,.42f,.10f);
            SmallButton(modal,"GITHUB",()=>Application.OpenURL(UltraServices.Repository),.055f,.16f,.42f,.10f);
            SmallButton(modal,"EXIT",()=>{CloseModal();OnExit();},.525f,.16f,.42f,.10f);
        }
    }
}
