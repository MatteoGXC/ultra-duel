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
    [RequireComponent(typeof(CanvasRenderer))]
    public class UltraHomeShape : UltraMesh
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
        static Color Lime => UltraTheme.Accent;
        static Color Muted => UltraTheme.Mute;
        RectTransform ultraHome, overlay, modal;
        TextMeshProUGUI profileLabel, gemLabel, passLabel, updateLabel, friendLabel, modalStatus;
        Image profileAccent;
        Button ownerAdminButton;
        readonly List<GameObject> hiddenHome = new List<GameObject>();
        bool actionBusy;
        string modalPage;

        void OnDestroy()
        {
            if(matchmakingActive)CancelMatchmaking().Forget();
            UltraServices.Changed -= RefreshUltraHome;
            UltraProgress.Changed -= RefreshUltraHome;
            UltraTheme.Changed -= RefreshUltraHome;
        }
        Color FrameColor()
        {
            return UltraProgress.Data.frame switch {
                "Violet" => new Color(.72f,.35f,1), "Emerald" => new Color(.15f,1,.65f),
                "Neon" => new Color(.15f,.85f,1), "Champion" => new Color(1,.78f,.22f), _ => Lime };
        }
        RectTransform Px(string name, Transform parent, float left, float top, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
            return rect;
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
            TryHome("scrim", () =>
            {
                var scrim = HomeRect("Home scrim", ultraHome, 0, 0, 1, 1).gameObject.AddComponent<UltraScrim>();
                scrim.strong = false; scrim.raycastTarget = false;
            });
            TryHome("profile", () =>
            {
                var profile = Px("Profile", ultraHome, 64, 36, 560, 132);
                HomePanel(profile, UltraTheme.Panel);
                profileAccent = Px("Profile frame", profile, 0, 28, 6, 56).gameObject.AddComponent<Image>();
                profileAccent.raycastTarget = false;
                profileAccent.gameObject.AddComponent<UltraAccentFill>();
                var avatar = profile.gameObject.AddComponent<UltraProfileHeader>(); avatar.Initialize();
                profileLabel = HomeText(profile, "", 40, UltraTheme.Fg, .30f, .46f, .66f, .40f);
                UltraTheme.Style(profileLabel, true, 2f);
                var profileClick = profile.gameObject.AddComponent<Button>(); profileClick.onClick.AddListener(OpenProfile);
                profileClick.targetGraphic = profile.GetComponent<Image>();
                profile.GetComponent<Image>().raycastTarget = true;
            });
            TryHome("wallet", () =>
            {
                var wallet = Px("Gem wallet", ultraHome, 648, 58, 300, 80);
                var walletFrame = wallet.gameObject.AddComponent<UltraChrome>();
                walletFrame.color = UltraTheme.Panel; walletFrame.cut = UltraCut.Panel; walletFrame.rim = 1.5f; walletFrame.followThemeRim = true; walletFrame.raycastTarget = false;
                var gem = Px("Gem", wallet, 18, 14, 52, 52); gem.gameObject.AddComponent<UltraGem>().raycastTarget = false;
                gemLabel = HomeText(wallet, "", 36, UltraTheme.Fg, .26f, .12f, .52f, .76f);
                UltraTheme.Style(gemLabel, true, 2f);
                SmallButton(wallet, "+", OpenShop, .78f, .22f, .16f, .56f, 28, UltraTier.Primary);
            });
            TryHome("notices", () => { TopButton("NOTICES", OpenNotifications, 394, 0); BuildNotificationsHome(); });
            TryHome("missions icon", () => TopButton("MISSIONS", OpenMissions, 284, 1));
            TryHome("friends icon", () => TopButton("FRIENDS", OpenFriends, 174, 2));
            TryHome("menu icon", () => TopButton("MENU", OpenMenu, 64, 3));
            RectTransform menu = null;
            TryHome("menu", () =>
            {
                menu = Px("Menu", ultraHome, 48, 184, 680, 560);
                var layout = menu.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.childControlWidth = layout.childControlHeight = true;
                layout.childForceExpandWidth = true; layout.childForceExpandHeight = false;
                layout.spacing = 0f;
            });
            TryHome("DUEL", () => defaultSelectable = NavButton(menu, "DUEL", OpenDuel, true));
            TryHome("DECK", () => NavButton(menu, "DECK", OnDeck, false));
            TryHome("SOLO", () => NavButton(menu, "SOLO", OnSolo, false));
            TryHome("SHOP", () => NavButton(menu, "SHOP", OpenShop, false));
            TryHome("missions", () =>
            {
                var missions = Px("Missions", ultraHome, 64, 772, 560, 146);
                HomePanel(missions, UltraTheme.Panel);
                var missionEdge = Px("Mission edge", missions, 0, 0, 6, 56); missionEdge.gameObject.AddComponent<Image>().raycastTarget = false; missionEdge.gameObject.AddComponent<UltraAccentFill>();
                var missionKicker = HomeText(missions, "MISSIONS", 16, Lime, .06f, .68f, .55f, .22f);
                UltraTheme.StyleEmphasis(missionKicker, 14f);
                if (missionKicker != null) missionKicker.gameObject.AddComponent<UltraAccentText>();
                homeStatus = HomeText(missions, "", 30, UltraTheme.Fg, .06f, .16f, .62f, .50f);
                UltraTheme.Style(homeStatus, true, 2f);
                SmallButton(missions, "VIEW", OpenMissions, .68f, .58f, .26f, .28f, 18, UltraTier.Secondary);
            });
            TryHome("pass", () =>
            {
                var pass = Px("Duel Pass", ultraHome, 64, 934, 560, 96);
                HomePanel(pass, UltraTheme.Panel);
                var passEdge = Px("Pass edge", pass, 0, 0, 6, 48); passEdge.gameObject.AddComponent<Image>().raycastTarget = false; passEdge.gameObject.AddComponent<UltraAccentFill>();
                passLabel = HomeText(pass, "", 32, UltraTheme.Fg, .06f, .16f, .62f, .70f);
                UltraTheme.Style(passLabel, true, 2f);
                pass.GetComponent<Image>().raycastTarget = true;
                var passButton = pass.gameObject.AddComponent<Button>(); passButton.targetGraphic = pass.GetComponent<Image>(); passButton.onClick.AddListener(OpenPass);
                var claim = Px("Claim", pass, 430, 22, 100, 52);
                var claimChrome = claim.gameObject.AddComponent<UltraChrome>(); claimChrome.cut = UltraCut.Small; claimChrome.raycastTarget = true;
                var claimButton = claim.gameObject.AddComponent<Button>(); claimButton.targetGraphic = claimChrome; claimButton.onClick.AddListener(OpenPass);
                var claimLabel = HomeText(claim, "▼", 28, Color.white, 0, 0, 1, 1);
                if (claimLabel != null) claimLabel.alignment = TextAlignmentOptions.Center;
                claim.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Hero);
            });
            TryHome("version", () =>
            {
                friendLabel = HomeText(ultraHome, "", 17, Muted, 0, 0, 0, 0);
                updateLabel = HomeText(ultraHome, "", 16, Muted, 0, 0, 0, 0);
                var version = Px("Version", ultraHome, 66, 0, 240, 28);
                version.anchorMin = version.anchorMax = new Vector2(0f, 0f); version.pivot = new Vector2(0f, 0f); version.anchoredPosition = new Vector2(66f, 16f);
                var versionLabel = HomeText(version, "v" + UltraServices.Version, 16, Muted, 0, 0, 1, 1);
                UltraTheme.StyleEmphasis(versionLabel, 10f);
                if (friendLabel != null) friendLabel.gameObject.SetActive(false);
                if (updateLabel != null) updateLabel.gameObject.SetActive(false);
            });
            TryHome("admin", () =>
            {
                var admin = Px("Admin slot", ultraHome, 0, 0, 180, 56);
                admin.anchorMin = admin.anchorMax = new Vector2(1f, 0f); admin.pivot = new Vector2(1f, 0f); admin.anchoredPosition = new Vector2(-64f, 28f);
                ownerAdminButton = SmallButton(admin, "ADMIN", OpenAdmin, 0, 0, 1, 1, 20, UltraTier.Secondary);
                ownerAdminButton.gameObject.SetActive(false);
            });
            TryHome("services", () =>
            {
                UltraServices.Initialize();
                UltraServices.Changed += RefreshUltraHome;
                UltraProgress.Changed += RefreshUltraHome;
                UltraTheme.Changed += RefreshUltraHome;
                RefreshUltraHome();
                UltraServices.CheckUpdates();
                var keys = ultraHome.gameObject.AddComponent<UltraHomeKeyboard>(); keys.open = OpenAdmin;
                if(!Array.Exists(Environment.GetCommandLineArgs(), x => x == "--format-smoke")) ShowGameStart();
            });
        }
        void TryHome(string part, Action build)
        {
            try { build(); }
            catch (Exception exception) { Debug.LogError("Ultra home " + part + " failed.\n" + exception); }
        }
        void TopButton(string title, UnityAction action, float right, int icon)
        {
            var rect = Px(title, ultraHome, 0, 48, 100, 112);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-right, -48f);
            var hit = rect.gameObject.AddComponent<Image>(); hit.color = Color.clear; hit.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = hit; button.transition = Selectable.Transition.None; button.onClick.AddListener(action);
            var plate = Px("Plate", rect, 12, 0, 76, 76);
            var chrome = plate.gameObject.AddComponent<UltraChrome>(); chrome.cut = UltraCut.Small; chrome.color = UltraTheme.Panel; chrome.raycastTarget = false;
            var glyph = Px("Icon", plate, 18, 18, 40, 40).gameObject.AddComponent<UltraNavIcon>(); glyph.kind = icon; glyph.raycastTarget = false; glyph.color = UltraTheme.Fg;
            var caption = HomeText(rect, title, 15, UltraTheme.Fg, 0f, 0f, 1f, .28f);
            if (caption != null) caption.alignment = TextAlignmentOptions.Center;
            var iconButton = rect.gameObject.AddComponent<UltraIconButton>(); iconButton.Bind(chrome, caption);
        }
        Button NavButton(Transform menu, string title, UnityAction action, bool hero)
        {
            if (menu == null) return null;
            var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(menu, false);
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = hero ? 210f : 100f;
            element.minHeight = element.preferredHeight;
            var button = rect.gameObject.AddComponent<Button>();
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<UltraMenuItem>().Bind(button, title, hero);
            return button;
        }
        Button SmallButton(Transform parent, string text, UnityAction action, float x, float y, float w, float h, float size = 18, UltraTier tier = UltraTier.Secondary)
        {
            var rect = HomeRect(text, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<UltraChrome>();
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = HomeText(rect, text, size, UltraTheme.Fg, .06f, .08f, .88f, .84f);
            if (label != null) label.alignment = TextAlignmentOptions.Center;
            if (tier == UltraTier.Secondary && UltraTheme.IsDanger(text)) tier = UltraTier.Danger;
            var themed = rect.gameObject.AddComponent<UltraThemedButton>();
            themed.Configure(tier);
            button.onClick.AddListener(action);
            return button;
        }
        void RefreshUltraHome()
        {
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            if (string.IsNullOrWhiteSpace(name)) name="Player";
            if (profileLabel!=null) profileLabel.text=name;
            if (profileAccent!=null) profileAccent.enabled=false;
            if (gemLabel!=null) gemLabel.text=UltraAccount.Gems.ToString("N0");
            if (passLabel!=null) passLabel.text="DUEL PASS   "+UltraAccount.Tier+" / 20\n<size=65%>"+(UltraAccount.Tier>UltraAccount.Claimed?"Rewards ready to claim":"WIN DUELS · EARN REWARDS")+"</size>";
            if (homeStatus!=null) homeStatus.text="Win duels · Earn rewards\n<size=70%>Next grade: "+(UltraAccount.Tier>=20?"Complete":(200-UltraAccount.XP%200)+" XP")+"</size>";
            if (updateLabel!=null) updateLabel.text="UPDATES  /  "+UltraServices.UpdateStatus+"\n<size=80%>New pass & shop · Friends & profiles · Solo fixes</size>";
            RefreshNotificationsHome();
            var state=UltraServices.State;
            if(ownerAdminButton!=null)ownerAdminButton.gameObject.SetActive(UltraAdminAccess.IsOwner && (overlay==null || modalPage=="friends" || modalPage=="friend"));
            if (friendLabel!=null) friendLabel.text=state==null?"FRIENDS  /  Sign in to connect and host duels":
                "FRIENDS  /  "+(state["friends"] as JArray)?.Count+" added   ·   "+(state["requests"] as JArray)?.Count+" requests";
        }
        void CloseModal()
        {
            if(matchmakingActive)CancelMatchmaking().Forget();
            if(overlay!=null){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
            foreach(var child in hiddenHome)if(child!=null)child.SetActive(true);hiddenHome.Clear();
            if(Program.instance.currentServant==Program.instance.menu){Program.instance.ui_.wallpaper.gameObject.SetActive(true);Program.instance.ui_.wallpaper.alpha=1;}
            overlay=modal=null;modalStatus=null;modalPage=null;
            RefreshNotificationsHome();
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
                shade.color=Color.clear;
                var scrim=HomeRect("Page scrim",overlay,0,0,1,1).gameObject.AddComponent<UltraScrim>();
                scrim.strong=page!="start";scrim.raycastTarget=false;
            }
            modal=HomeRect(title,overlay,.04f,.055f,.92f,.89f);
            HomePanel(modal,drawer ? new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.96f):Color.clear);
            if(drawer){modal.anchorMin=new Vector2(.62f,0);modal.anchorMax=Vector2.one;modal.offsetMin=modal.offsetMax=Vector2.zero;shade.color=new Color(0,0,0,.55f);}
            if(drawer){var strip=HomeRect("Drawer accent",modal,0,.78f,.008f,.22f);HomePanel(strip,UltraTheme.Accent);strip.gameObject.AddComponent<UltraAccentFill>();}
            HomePanel(HomeRect("Top rule",modal,.04f,.84f,.92f,.002f),UltraTheme.Line);
            var heading=HomeText(modal,title,64,UltraTheme.Fg,drawer?.16f:.075f,.875f,drawer?.78f:.70f,.09f);
            UltraTheme.Style(heading,true,4f);
            var sub=HomeText(modal,subtitle,18,Muted,.075f,.80f,.84f,.05f);
            UltraTheme.StyleEmphasis(sub,8f);
            SquareBack(modal,CloseModal,drawer?.03f:.012f,.875f,drawer?.09f:.045f,.075f);
            modal.gameObject.AddComponent<UltraPageTransition>();
            modalStatus=HomeText(modal,"",16,Lime,.045f,.02f,.90f,.04f);
            modalStatus.gameObject.AddComponent<UltraAccentText>();
            return modal;
        }
        TMP_InputField Input(Transform parent,string placeholder,float y,bool password=false)
        {
            var rect=HomeRect(placeholder,parent,.055f,y,.89f,.09f);HomePanel(rect,UltraTheme.PanelSolid);
            rect.GetComponent<Image>().raycastTarget=true;
            var field=rect.gameObject.AddComponent<TMP_InputField>();
            var text=HomeText(rect,"",24,UltraTheme.Fg,.035f,.08f,.93f,.84f);UltraTheme.Style(text,false);
            var hint=HomeText(rect,placeholder,23,Muted,.035f,.08f,.93f,.84f);UltraTheme.Style(hint,false);
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
            var card=HomeRect("Access terminal",modal,.25f,.13f,.50f,.66f);HomePanel(card,new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.94f));
            HomeText(card,"ULTRA DUEL",39,Color.white,.06f,.78f,.88f,.13f).alignment=TextAlignmentOptions.Center;
            HomePanel(HomeRect("Access rule",card,.06f,.74f,.88f,.004f),Muted);
            var name=Input(card,"Username (3–24 characters)",.57f);
            var password=Input(card,"Password (8 characters minimum)",.39f,true);
            SmallButton(card,"SIGN IN",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,false);password.text="";ShowFriends();}),.055f,.15f,.42f,.13f,23,UltraTier.Hero);
            SmallButton(card,"CREATE ACCOUNT",()=>Run(async()=>{await UltraServices.SignIn(name.text,password.text,true);password.text="";ShowFriends();}),.525f,.15f,.42f,.13f,23);
            HomeText(modal,"Connect with friends, see who is online, and join private TCG or Genesys rooms.",20,Muted,.055f,.13f,.89f,.11f);
        }
        RectTransform ScrollArea()
        {
            var viewport=HomeRect("List viewport",modal,.045f,.135f,.91f,.50f);
            var image=viewport.gameObject.AddComponent<Image>();image.color=new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.45f);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content=HomeRect("List content",viewport,0,1,1,0);content.pivot=new Vector2(.5f,1);
            var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.viewport=viewport;scroll.content=content;scroll.horizontal=false;scroll.scrollSensitivity=30;scroll.movementType=ScrollRect.MovementType.Clamped;
            return content;
        }
        RectTransform Row(RectTransform content,int index,string title,string detail)
        {
            var row=HomeRect("Row",content,0,1,1,0);row.pivot=new Vector2(.5f,1);row.sizeDelta=new Vector2(0,70);row.anchoredPosition=new Vector2(0,-index*76);
            HomePanel(row,UltraTheme.Panel);
            HomeText(row,title,23,Color.white,.025f,.43f,.67f,.48f);
            HomeText(row,detail,17,Muted,.025f,.08f,.67f,.35f);
            content.sizeDelta=new Vector2(0,(index+1)*76);
            return row;
        }
        void ShowFriends()
        {
            OpenModal("FRIENDS",UltraServices.PlayerName+" / Follow & duel","friends");
            HomeText(modal,"FOLLOW  /  "+((UltraServices.State?["friends"] as JArray)?.Count??0),20,Color.white,.045f,.735f,.90f,.04f);
            var add=Input(modal,"Enter friend's name",.62f);
            add.GetComponent<RectTransform>().anchorMax=new Vector2(.73f,.71f);
            SmallButton(modal,"+",()=>Run(async()=>{await UltraServices.Request("/friends/request","POST",new{name=add.text});await UltraServices.Refresh();ShowFriends();}),.76f,.62f,.19f,.09f,28,UltraTier.Primary);
            var content=ScrollArea();int index=0;
            var listViewport=(RectTransform)content.parent;listViewport.anchorMax=new Vector2(.955f,.58f);
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
            SmallButton(modal,"ONLINE DUEL",OpenOnlineDuel,.08f,.52f,.84f,.20f,40,UltraTier.Hero);
            SmallButton(modal,"FRIEND DUELS",()=>OpenFriendDuels("1v1"),.08f,.34f,.40f,.14f,26);
            SmallButton(modal,"DUOS 2v2",()=>OpenFriendDuels("2v2"),.52f,.34f,.40f,.14f,26);
            SmallButton(modal,"SOLO",()=>{CloseModal();Program.instance.ShiftToServant(Program.instance.solo);},.08f,.16f,.84f,.13f,26,UltraTier.Primary);
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
            var xpFill=HomeRect("XP fill",modal,.06f,.615f,.88f*(UltraAccount.Tier>=20?1:(UltraAccount.XP%200)/200f),.012f);HomePanel(xpFill,UltraTheme.Accent);xpFill.gameObject.AddComponent<UltraAccentFill>();
            HomeText(modal,"FREE\nPASS",20,Color.white,.045f,.40f,.105f,.12f);
            for(int col=0;col<6 && first+col<=20;col++)
            {
                int tier=first+col;float x=.17f+col*.128f;
                var cell=HomeRect("Grade "+tier,modal,x,.27f,.12f,.31f);
                bool claimed=tier<=UltraAccount.Claimed,available=tier<=UltraAccount.Tier;
                HomePanel(cell,UltraTheme.PanelSolid);
                var border=cell.GetComponent<UltraChrome>();border.rim=available && !claimed?2:1;border.followThemeRim=false;border.rimColor=available && !claimed?UltraTheme.Accent:UltraTheme.Line;
                var header=HomeRect("Grade header",cell,0,.82f,1,.18f);
                HomePanel(header,claimed?UltraTheme.PanelHover:available?UltraTheme.Accent:UltraTheme.PanelSolid);
                if(available && !claimed)header.gameObject.AddComponent<UltraAccentFill>();
                HomeText(cell,tier.ToString(),24,available && !claimed?UltraTheme.AccentInk:UltraTheme.Fg,.1f,.83f,.8f,.16f).alignment=TextAlignmentOptions.Center;
                HomeRect("Gem reward",cell,.14f,.43f,.72f,.36f).gameObject.AddComponent<UltraGem>();
                var amount=HomeText(cell,"50 GEMS",22,Color.white,.06f,.275f,.88f,.13f);amount.alignment=TextAlignmentOptions.Center;amount.fontStyle=FontStyles.Bold;
                string milestone=tier==5?"+ DUELIST TITLE":tier==10?"+ NEON FRAME":tier==20?"+ CHAMPION FRAME":"FREE REWARD";
                HomeText(cell,milestone,12,Muted,.04f,.15f,.92f,.095f).alignment=TextAlignmentOptions.Center;
                HomePanel(HomeRect("Reward state",cell,.06f,.04f,.88f,.075f),claimed?new Color(.12f,.17f,.22f):new Color(.025f,.04f,.07f));
                HomeText(cell,claimed?"CLAIMED":available?"READY TO CLAIM":"LOCKED",11,available && !claimed?Lime:Muted,.07f,.045f,.86f,.065f).alignment=TextAlignmentOptions.Center;
                var reveal=cell.gameObject.AddComponent<CanvasGroup>();reveal.alpha=0;
                reveal.DOFade(1,.25f).SetDelay(col*.045f).SetUpdate(true).SetLink(cell.gameObject);
            }
            SmallButton(modal,"<",()=>{passPage=Math.Max(0,passPage-1);OpenPass();},.045f,.16f,.09f,.07f,28);
            SmallButton(modal,">",()=>{passPage=Math.Min(3,passPage+1);OpenPass();},.86f,.16f,.09f,.07f,28);
            HomeText(modal,(passPage+1)+" / 4",16,Muted,.44f,.225f,.12f,.035f).alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"CLAIM REWARDS",()=>Run(async()=>{await UltraAccount.Claim();OpenPass();}),.30f,.12f,.40f,.09f,24,UltraTier.Hero).interactable=UltraAccount.Tier>UltraAccount.Claimed;
            modalStatus.rectTransform.anchorMin=new Vector2(.045f,.035f);modalStatus.rectTransform.anchorMax=new Vector2(.945f,.075f);
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
        void OpenBackups()
        {
            OpenModal("DECK BACKUPS",UltraDeckCloud.Status,"backups");
            HomeText(modal,"Signed-in decks are backed up to your Ultra Duel account.\nSign in after reinstalling to restore them.\nLocal backups also protect offline decks, settings and replays.",24,Color.white,.06f,.35f,.88f,.37f).alignment=TextAlignmentOptions.Center;
            SmallButton(modal,"SYNC DECKS",()=>SyncBackups().Forget(),.08f,.16f,.40f,.12f);
            SmallButton(modal,"LOCAL BACKUPS",()=>{if(!string.IsNullOrEmpty(UltraSaveSafety.Location))Application.OpenURL(new Uri(UltraSaveSafety.Location+System.IO.Path.DirectorySeparatorChar).AbsoluteUri);},.52f,.16f,.40f,.12f);
        }
        async UniTaskVoid SyncBackups(){await UltraDeckCloud.Sync(true);OpenBackups();}
        void OpenMenu()
        {
            OpenModal("MENU","Ultra Duel  /  v"+UltraServices.Version,"menu");
            SmallButton(modal,"SETTINGS",()=>{CloseModal();OnSetting();},.055f,.61f,.42f,.10f);
            SmallButton(modal,"WALLPAPER",OpenWallpaper,.525f,.61f,.42f,.10f);
            SmallButton(modal,"MATCH HISTORY",OpenHistory,.055f,.46f,.42f,.10f);
            SmallButton(modal,"PUZZLES",()=>{CloseModal();OnPuzzle();},.525f,.46f,.42f,.10f);
            SmallButton(modal,"CUT-INS",()=>{CloseModal();OnCutin();},.055f,.31f,.42f,.10f);
            SmallButton(modal,"UPDATES",OpenUpdates,.525f,.31f,.42f,.10f);
            SmallButton(modal,"GITHUB",()=>Application.OpenURL(UltraServices.Repository),.055f,.16f,.42f,.10f);
            SmallButton(modal,"EXIT",()=>{CloseModal();OnExit();},.525f,.16f,.42f,.10f);
            SmallButton(modal,"GUI SCALE  "+UltraGuiScale.Percent+"%",OpenGuiScale,.055f,.025f,.42f,.08f);
            SmallButton(modal,"SAVE BACKUPS",OpenBackups,.525f,.025f,.42f,.08f);
        }
    }
}
