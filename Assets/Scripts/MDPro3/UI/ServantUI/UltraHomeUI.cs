using System;
using DG.Tweening;
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
        readonly List<GameObject> hiddenHome = new List<GameObject>();
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
            if(!Config.GetBool("UltraReferenceBackgroundApplied",false))
            {
                Config.Set("Background","2");Config.Set("UltraReferenceBackgroundApplied","1");
                Program.instance.background_.Change(2);
            }
            foreach (Transform child in Root) child.gameObject.SetActive(false);
            ultraHome = HomeRect("Ultra Duel Home", Root, 0, 0, 1, 1);
            HomePanel(ultraHome, new Color(.018f,.029f,.053f,0));
            var nav=HomeRect("Angular navigation",ultraHome,0,0,.52f,1).gameObject.AddComponent<UltraHomeShape>();
            nav.color=Color.clear; nav.raycastTarget=false;
            var profile=HomeRect("Profile",ultraHome,.045f,.855f,.27f,.105f);
            HomePanel(profile,new Color(.015f,.02f,.028f));
            profile.GetComponent<UltraChrome>().rim=5;
            profileAccent=HomeRect("Profile frame",profile,0,0,.012f,1).gameObject.AddComponent<Image>();
            profileAccent.raycastTarget=false;
            var avatar=profile.gameObject.AddComponent<UltraProfileHeader>();avatar.Initialize();
            profileLabel=HomeText(profile,"",25,Color.white,.35f,.49f,.58f,.43f);
            var profileClick=profile.gameObject.AddComponent<Button>(); profileClick.onClick.AddListener(OpenProfile);
            profileClick.targetGraphic=profile.GetComponent<Image>();
            profile.GetComponent<Image>().raycastTarget=true;
            profile.gameObject.AddComponent<UltraButtonMotion>();
            var wallet=HomeRect("Gem wallet",ultraHome,.385f,.90f,.135f,.055f);
            var walletFrame=wallet.gameObject.AddComponent<UltraChrome>();walletFrame.color=new Color(.06f,.08f,.12f,.88f);walletFrame.rim=2;walletFrame.rimColor=new Color(.70f,.75f,.80f);walletFrame.raycastTarget=false;
            HomeRect("Gem",wallet,.055f,.15f,.16f,.70f).gameObject.AddComponent<UltraGem>().raycastTarget=false;
            gemLabel=HomeText(wallet,"",24,Color.white,.26f,.08f,.66f,.84f);gemLabel.alignment=TextAlignmentOptions.MidlineRight;
            TopButton("UPDATES",OpenUpdates,.54f,0);
            TopButton("MISSIONS",OpenMissions,.65f,1);
            TopButton("FRIENDS",OpenFriends,.76f,2);
            TopButton("MENU",OpenMenu,.87f,3);
            defaultSelectable=NavButton("DUEL",OpenDuel,.735f,65);
            NavButton("DECK",OnDeck,.635f,44);
            NavButton("SOLO",OnSolo,.540f,44);
            NavButton("SHOP",OpenShop,.445f,44);
            var deckPanel=HomeRect("Selected deck",ultraHome,.045f,.19f,.27f,.15f);
            HomePanel(deckPanel,new Color(.055f,.075f,.12f));
            HomeText(deckPanel,"MISSIONS",17,Lime,.055f,.69f,.90f,.22f);
            homeStatus=HomeText(deckPanel,"",23,Color.white,.055f,.13f,.90f,.53f);
            SmallButton(deckPanel,"VIEW",OpenMissions,.69f,.73f,.27f,.20f,14);
            var pass=HomeRect("Duel Pass",ultraHome,.045f,.055f,.27f,.105f);
            HomePanel(pass,new Color(.09f,.085f,.23f));
            HomeRect("Duel Pass emblem",pass,.75f,.10f,.23f,.80f).gameObject.AddComponent<UltraPassIcon>().raycastTarget=false;
            passLabel=HomeText(pass,"",23,Color.white,.055f,.12f,.60f,.78f);
            pass.GetComponent<Image>().raycastTarget=true;
            pass.GetComponent<Image>().raycastTarget=true;
            var passButton=pass.gameObject.AddComponent<Button>(); passButton.targetGraphic=pass.GetComponent<Image>(); passButton.onClick.AddListener(OpenPass);
            friendLabel=HomeText(ultraHome,"",17,Muted,0,0,0,0);
            updateLabel=HomeText(ultraHome,"",16,Muted,0,0,0,0);
            HomeText(ultraHome,"v"+UltraServices.Version,15,Muted,.045f,.015f,.10f,.025f);
            friendLabel.gameObject.SetActive(false);updateLabel.gameObject.SetActive(false);
            UltraServices.Initialize();
            UltraServices.Changed += RefreshUltraHome;
            UltraProgress.Changed += RefreshUltraHome;
            RefreshUltraHome();
            UltraServices.CheckUpdates();
            var keys=ultraHome.gameObject.AddComponent<UltraHomeKeyboard>();keys.open=OpenAdmin;
            if(!Array.Exists(Environment.GetCommandLineArgs(),x=>x=="--format-smoke"))ShowGameStart();
        }
        void TopButton(string title,UnityAction action,float x,int icon)
        {
            var rect=HomeRect(title,ultraHome,x,.878f,.09f,.095f);
            var image=rect.gameObject.AddComponent<Image>();image.color=Color.clear;
            var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=image;b.onClick.AddListener(action);
            var glyph=HomeRect("Icon",rect,.34f,.38f,.32f,.55f).gameObject.AddComponent<UltraNavIcon>();glyph.kind=icon;glyph.raycastTarget=false;
            rect.gameObject.AddComponent<UltraButtonMotion>();
            var caption=HomeText(rect,title,15,Color.white,0,0,1,.27f);caption.alignment=TextAlignmentOptions.Center;caption.outlineWidth=.22f;caption.outlineColor=new Color32(0,0,0,240);
        }
        Button NavButton(string title,UnityAction action,float y,float size)
        {
            var rect=HomeRect(title,ultraHome,.065f,y,.29f,.075f);
            var image=rect.gameObject.AddComponent<Image>();image.color=Color.white;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=new Color(0,0,0,0);colors.highlightedColor=colors.selectedColor=new Color(.16f,.24f,.22f,.8f);colors.pressedColor=new Color(.24f,.35f,.24f);button.colors=colors;
            HomePanel(HomeRect("Accent",rect,0,.20f,.005f,.60f),Lime);
            HomeText(rect,title,size,Color.white,.025f,0,.94f,1).fontStyle=FontStyles.Bold;
            button.onClick.AddListener(action);rect.gameObject.AddComponent<UltraButtonMotion>();return button;
        }
        Button SmallButton(Transform parent,string text,UnityAction action,float x,float y,float w,float h,float size=18)
        {
            var rect=HomeRect(text,parent,x,y,w,h);
            var image=rect.gameObject.AddComponent<UltraChrome>();image.color=new Color(.23f,.22f,.22f,.96f);image.rim=0;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=colors.selectedColor=new Color(.60f,.85f,.75f);colors.pressedColor=new Color(.40f,.65f,.50f);button.colors=colors;
            var label=HomeText(rect,text,size,Color.white,.05f,.05f,.90f,.90f);label.alignment=TextAlignmentOptions.Center;
            button.onClick.AddListener(action);rect.gameObject.AddComponent<UltraButtonMotion>();return button;
        }
        void RefreshUltraHome()
        {
            if (profileLabel==null) return;
            var data=UltraProgress.Data;
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            profileLabel.text=name;
            if (string.IsNullOrWhiteSpace(name)) name="Player";
            profileLabel.text=name;
            profileAccent.enabled=false;gemLabel.text=UltraAccount.Gems.ToString("N0");
            passLabel.text="DUEL PASS   "+UltraAccount.Tier+" / 20\n<size=65%>"+(UltraAccount.Tier>UltraAccount.Claimed?"Rewards ready to claim":"WIN DUELS · EARN REWARDS")+"</size>";
            homeStatus.text="Win duels · Earn rewards\n<size=70%>Next grade: "+(UltraAccount.Tier>=20?"Complete":(200-UltraAccount.XP%200)+" XP")+"</size>";
            updateLabel.text="UPDATES  /  "+UltraServices.UpdateStatus+"\n<size=80%>New pass & shop · Friends & profiles · Solo fixes</size>";
            var state=UltraServices.State;
            friendLabel.text=state==null?"FRIENDS  /  Sign in to connect and host duels":
                "FRIENDS  /  "+(state["friends"] as JArray)?.Count+" added   ·   "+(state["requests"] as JArray)?.Count+" requests";
        }
        void CloseModal()
        {
            if(overlay!=null){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
            foreach(var child in hiddenHome)if(child!=null)child.SetActive(true);hiddenHome.Clear();
            if(Program.instance.currentServant==Program.instance.menu){Program.instance.ui_.wallpaper.gameObject.SetActive(true);Program.instance.ui_.wallpaper.alpha=1;}
            overlay=modal=null;modalStatus=null;modalPage=null;
        }
        RectTransform OpenModal(string title,string subtitle,string page)
        {
            CloseModal();modalPage=page;
            overlay=HomeRect("Modal overlay",ultraHome,0,0,1,1);
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=new Color(0,0,0,.45f);
            bool drawer=page=="friends" || page=="friend";
            bool homeVisible=drawer;
            if(!homeVisible)
            {
                foreach(Transform child in ultraHome)if(child!=overlay && child.gameObject.activeSelf){hiddenHome.Add(child.gameObject);child.gameObject.SetActive(false);}
                if(page!="start")Program.instance.ui_.wallpaper.gameObject.SetActive(false);shade.color=Color.clear;
            }
            modal=HomeRect(title,overlay,.04f,.055f,.92f,.89f);
            HomePanel(modal,drawer ? new Color(.012f,.025f,.040f,.99f):Color.clear);
            if(drawer){modal.anchorMin=new Vector2(.64f,0);modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;}
            HomePanel(HomeRect("Top rule",modal,0,.99f,1,.002f),new Color(.62f,.68f,.75f,.65f));
            HomeText(modal,title,34,Color.white,drawer ? .16f:.075f,.885f,drawer ? .79f:.82f,.085f).fontStyle=FontStyles.Bold;
            HomeText(modal,subtitle,18,Muted,.045f,.79f,.90f,.07f);
            RoundBack(modal,CloseModal,drawer ? .035f:.005f,.89f,drawer ? .10f:.055f,.08f);
            modal.gameObject.AddComponent<UltraPageTransition>();
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
            OpenModal("DUELIST ACCESS","Sign in to connect with your friends and keep your account rewards.","login");WideModal();
            var card=HomeRect("Access terminal",modal,.25f,.13f,.50f,.66f);HomePanel(card,new Color(.035f,.04f,.055f,.97f));
            HomeText(card,"ULTRA DUEL",39,Color.white,.06f,.78f,.88f,.13f).alignment=TextAlignmentOptions.Center;
            HomePanel(HomeRect("Access rule",card,.06f,.74f,.88f,.004f),Muted);
            var name=Input(card,"Username (3–24 characters)",.57f);
            var password=Input(card,"Password (8 characters minimum)",.39f,true);
            SmallButton(card,"SIGN IN",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,false);password.text="";ShowFriends();}),.055f,.15f,.42f,.13f,23);
            SmallButton(card,"CREATE ACCOUNT",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,true);password.text="";ShowFriends();}),.525f,.15f,.42f,.13f,23);
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
            OpenModal("FRIENDS",UltraServices.PlayerName+" / Follow & duel","friends");
            HomeText(modal,"FOLLOW  /  "+((UltraServices.State?["friends"] as JArray)?.Count??0),20,Color.white,.045f,.785f,.90f,.045f);
            var add=Input(modal,"Enter friend's name",.69f);
            add.GetComponent<RectTransform>().anchorMax=new Vector2(.73f,.78f);
            SmallButton(modal,"+",()=>Run(async()=>{await UltraServices.Request("/friends/request","POST",new{name=add.text});await UltraServices.Refresh();ShowFriends();}),.76f,.69f,.19f,.09f);
            var content=ScrollArea();int index=0;
            var state=UltraServices.State;
            foreach(var invite in state?["invites"] as JArray ?? new JArray())
            {
                string rid=(string)invite["room"];
                var row=Row(content,index++,(string)invite["name"],"Invited you to duel");
                SmallButton(row,"JOIN",()=>JoinFriendRoom(rid),.72f,.2f,.25f,.6f,16);
            }
            foreach(var person in state?["requests"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],"Friend request");
                SmallButton(row,"ACCEPT",()=>Run(async()=>{await UltraServices.Request("/friends/accept","POST",new{id});await UltraServices.Refresh();ShowFriends();}),.69f,.2f,.28f,.6f,15);
            }
            foreach(var person in state?["friends"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];bool online=(bool)person["online"];
                var row=Row(content,index++,(online?"● ":"○ ")+(string)person["name"],(string)person["activity"]??(online?"Online":"Offline"));
                HomePanel(HomeRect("Online edge",row,0,0,.008f,1),online?Lime:Muted);
                var icon=HomeRect("Friend mate",row,.025f,.12f,.13f,.76f).gameObject.AddComponent<Image>();icon.preserveAspect=true;
                LoadMatePortrait(icon,(int?)person["mate"]??1000001).Forget();
                foreach(var label in row.GetComponentsInChildren<TextMeshProUGUI>()){var r=label.rectTransform;r.anchorMin=new Vector2(.18f,r.anchorMin.y);r.anchorMax=new Vector2(.80f,r.anchorMax.y);}

                SmallButton(row,">",()=>OpenFriend(person),.82f,.20f,.15f,.60f,24);
            }
            foreach(var person in state?["sent"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],"Request sent");
                SmallButton(row,"X",()=>RemoveFriend(id),.84f,.20f,.13f,.60f,18);
            }
            if(index==0)Row(content,0,"Find your friends","Add their Ultra Duel username above.");
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();ShowFriends();}),.045f,.025f,.30f,.065f,16);
            SmallButton(modal,"SIGN OUT",()=>Run(async()=>{await UltraServices.SignOut();OpenLogin();}),.65f,.025f,.30f,.065f,16);
        }
        void RemoveFriend(int id) {Run(async()=>{await UltraServices.Request("/friends/"+id,"DELETE");await UltraServices.Refresh();ShowFriends();});}
        void OpenDuel()
        {
            OpenModal("DUEL","Choose how you want to play","duel-modes");WideModal();
            SmallButton(modal,"FRIEND DUELS  >",()=>OpenFriendDuels("1v1"),.12f,.58f,.76f,.12f,30);
            SmallButton(modal,"DUOS 2v2  >",()=>OpenFriendDuels("2v2"),.12f,.42f,.76f,.12f,30);
            SmallButton(modal,"SOLO  >",()=>{CloseModal();Program.instance.ShiftToServant(Program.instance.solo);},.12f,.26f,.36f,.10f,26);
            SmallButton(modal,"OTHER SERVERS  >",()=>{CloseModal();OnOnline();},.52f,.26f,.36f,.10f,26);
        }
        public void OpenFriendDuels(string mode="1v1")
        {
            friendRoomMode=mode;
            if(string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            ShowRooms();Run(async()=>{await UltraServices.Refresh();if(modalPage=="rooms")ShowRooms();});
        }
        void ShowRooms() { BuildRoomBrowser(); }
        void OpenPass()
        {
            OpenModal("DUEL PASS","FREE PASS / Earn rewards by winning duels · No paid tier","pass");
            WideModal();int first=passPage*6+1;
            StatPanel(modal,"GRADE",UltraAccount.Tier+" / 20",.045f,.65f,.43f,.12f);
            StatPanel(modal,"NEXT GRADE",UltraAccount.Tier>=20?"COMPLETE":(200-UltraAccount.XP%200)+" XP",.50f,.65f,.45f,.12f);
            HomePanel(HomeRect("XP track",modal,.06f,.615f,.88f,.012f),new Color(.17f,.22f,.28f));
            HomePanel(HomeRect("XP fill",modal,.06f,.615f,.88f*(UltraAccount.Tier>=20?1:(UltraAccount.XP%200)/200f),.012f),Lime);
            HomeText(modal,"FREE\nPASS",20,Color.white,.045f,.40f,.105f,.12f);
            for(int col=0;col<6 && first+col<=20;col++)
            {
                int tier=first+col;float x=.17f+col*.128f;
                var cell=HomeRect("Grade "+tier,modal,x,.27f,.12f,.31f);
                HomePanel(cell,new Color(.065f,.10f,.19f));
                HomePanel(HomeRect("Grade header",cell,0,.82f,1,.18f),tier<=UltraAccount.Tier?Lime:new Color(.23f,.27f,.34f));
                HomeText(cell,tier.ToString(),24,tier<=UltraAccount.Tier?Color.black:Color.white,.1f,.83f,.8f,.16f).alignment=TextAlignmentOptions.Center;
                var reward=HomeRect("Gem reward",cell,.25f,.48f,.50f,.26f);
                HomePanel(reward,new Color(.015f,.025f,.045f));
                HomeRect("Gem",reward,.15f,.13f,.70f,.75f).gameObject.AddComponent<UltraGem>().raycastTarget=false;
                HomeText(cell,(tier<=UltraAccount.Claimed?"✓  ":"")+"×50",20,Color.white,.04f,.39f,.92f,.10f).alignment=TextAlignmentOptions.Center;
                string milestone=tier==5?"RISING DUELIST TITLE":tier==10?"NEON FRAME":tier==20?"CHAMPION FRAME":"50 GEMS";
                HomeText(cell,milestone,15,Color.white,.04f,.10f,.92f,.20f).alignment=TextAlignmentOptions.Center;
            }
            SmallButton(modal,"<",()=>{passPage=Math.Max(0,passPage-1);OpenPass();},.045f,.16f,.09f,.07f,28);
            SmallButton(modal,">",()=>{passPage=Math.Min(3,passPage+1);OpenPass();},.86f,.16f,.09f,.07f,28);
            SmallButton(modal,"CLAIM REWARDS",()=>Run(async()=>{await UltraAccount.Claim();OpenPass();}),.30f,.12f,.40f,.09f).interactable=UltraAccount.Tier>UltraAccount.Claimed;
            modalStatus.text=UltraAccount.Profile==null?"Offline progress / Saved on this PC":"Account rewards / Saved on Ultra Duel";
        }
        void OpenShop() { ShowShop("Mate",0); }
        void BuyFrame(string frame)
        {
            var item=UltraAccount.Catalog.Find(p=>p.key=="Frame:"+frame);
            Run(async()=>{await UltraAccount.Buy(item);ShowShop("Frame",0);});
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
            SmallButton(modal,"MATCH HISTORY",OpenHistory,.055f,.46f,.42f,.10f);
            SmallButton(modal,"PUZZLES",()=>{CloseModal();OnPuzzle();},.525f,.46f,.42f,.10f);
            SmallButton(modal,"CUT-INS",()=>{CloseModal();OnCutin();},.055f,.31f,.42f,.10f);
            SmallButton(modal,"CHECK UPDATES",()=>UltraServices.CheckUpdates(true),.525f,.31f,.42f,.10f);
            SmallButton(modal,"GITHUB",()=>Application.OpenURL(UltraServices.Repository),.055f,.16f,.42f,.10f);
            SmallButton(modal,"EXIT",()=>{CloseModal();OnExit();},.525f,.16f,.42f,.10f);
        }
    }
}
