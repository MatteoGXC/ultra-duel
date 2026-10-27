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
        UltraCountPop gemCount;
        bool homeIntroPlayed;
        bool launchGate;
        RectTransform accessCard, accessRule, accessWelcome;
        Button accessContinue, accessSwitch, accessSignIn, accessCreate;
        TMP_InputField accessUser, accessPass;
        bool accessRemembered;
        Image profileAccent;
        Button ownerAdminButton;
        readonly List<GameObject> hiddenHome = new List<GameObject>();
        bool actionBusy;
        bool replacingModal;
        string modalPage;
        int shopPage;
        string shopCategory = "Mate";

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
                var avatar = profile.gameObject.AddComponent<UltraProfileHeader>(); avatar.Initialize();
                profileLabel = HomeText(profile, "", 34, UltraTheme.Fg, .215f, .54f, .74f, .34f);
                UltraTheme.Style(profileLabel, true, 1f);
                var profileClick = profile.gameObject.AddComponent<Button>(); profileClick.onClick.AddListener(OpenProfile);
                profileClick.targetGraphic = profile.GetComponent<Image>();
                profile.GetComponent<Image>().raycastTarget = true;
            });
            TryHome("wallet", () =>
            {
                var wallet = Px("Gem wallet", ultraHome, 648, 58, 250, 80);
                gemLabel = FillGemPlate(wallet);
                if (gemLabel != null) gemCount = gemLabel.gameObject.AddComponent<UltraCountPop>();
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
                var claim = Px("Claim", pass, 490, 22, 52, 52);
                var claimChrome = claim.gameObject.AddComponent<UltraChrome>();
                claimChrome.cut = UltraCut.Small; claimChrome.color = UltraTheme.Accent; claimChrome.rim = 0f; claimChrome.followThemeRim = false; claimChrome.raycastTarget = true;
                claim.gameObject.AddComponent<UltraAccentFill>();
                var mark = HomeRect("Chevron", claim, .22f, .22f, .56f, .56f);
                var chevron = mark.gameObject.AddComponent<UltraChevron>();
                chevron.facing = 2; chevron.color = Color.white; chevron.raycastTarget = false;
                var claimButton = claim.gameObject.AddComponent<Button>();
                claimButton.targetGraphic = claimChrome; claimButton.transition = Selectable.Transition.None; claimButton.onClick.AddListener(OpenPass);
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
                if(!Array.Exists(Environment.GetCommandLineArgs(), x => x == "--format-smoke")) OpenLaunchLogin();
                else PlayHomeIntro();
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
            themed.rule = tier == UltraTier.Hero && text == "GAME START";
            themed.Configure(tier);
            button.onClick.AddListener(action);
            return button;
        }
        TextMeshProUGUI FillGemPlate(RectTransform plate)
        {
            var frame = plate.GetComponent<UltraChrome>();
            if (frame == null) frame = plate.gameObject.AddComponent<UltraChrome>();
            frame.color = UltraTheme.Panel;
            frame.cut = UltraCut.Panel;
            frame.rim = 1.5f;
            frame.followThemeRim = true;
            frame.raycastTarget = false;
            var gem = HomeRect("Gem", plate, .06f, .16f, .22f, .68f);
            gem.gameObject.AddComponent<UltraGem>().raycastTarget = false;
            var label = HomeText(plate, "", 36, UltraTheme.Fg, .32f, .10f, .62f, .80f);
            UltraTheme.Style(label, true, 2f);
            return label;
        }
        void SectionLabel(Transform parent, string text, float x, float y)
        {
            var tick = HomeRect(text + " mark", parent, x, y, .004f, .02f);
            var bar = tick.gameObject.AddComponent<Image>();
            bar.color = UltraTheme.Accent;
            bar.raycastTarget = false;
            tick.gameObject.AddComponent<UltraAccentFill>();
            var label = HomeText(parent, text, 14, Muted, x + .012f, y - .002f, .24f, .026f);
            if (label != null) UltraTheme.StyleEmphasis(label, 12f);
        }
        Button MenuCard(string title, UnityAction action, int icon, float x, float y, float w, float h, string badge = null)
        {
            var rect = HomeRect(title, modal, x, y, w, h);
            var chrome = rect.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Panel;
            chrome.color = UltraTheme.PanelSolid;
            chrome.rim = 1.5f;
            chrome.followThemeRim = true;
            chrome.raycastTarget = true;
            var edge = HomeRect("Card accent", rect, 0f, .2f, .012f, .6f);
            var edgeImage = edge.gameObject.AddComponent<Image>();
            edgeImage.color = UltraTheme.Accent;
            edgeImage.raycastTarget = false;
            edge.gameObject.AddComponent<UltraAccentFill>();
            var well = HomeRect("Icon well", rect, .045f, .18f, .16f, .64f);
            var wellChrome = well.gameObject.AddComponent<UltraChrome>();
            wellChrome.cut = UltraCut.Small;
            wellChrome.color = UltraTheme.Ink;
            wellChrome.rim = 0f;
            wellChrome.raycastTarget = false;
            var mark = HomeRect("Icon", well, .18f, .18f, .64f, .64f);
            var glyph = mark.gameObject.AddComponent<UltraLineIcon>();
            glyph.kind = icon;
            glyph.color = UltraTheme.Accent;
            glyph.raycastTarget = false;
            glyph.SetVerticesDirty();
            mark.gameObject.AddComponent<UltraAccentFill>();
            var label = HomeText(rect, title, 22, UltraTheme.Fg, .22f, .18f, badge == null ? .72f : .46f, .64f);
            if (label != null) label.alignment = TextAlignmentOptions.MidlineLeft;
            if (badge != null)
            {
                var pill = HomeRect("Badge", rect, .70f, .26f, .24f, .48f);
                var pillChrome = pill.gameObject.AddComponent<UltraChrome>();
                pillChrome.cut = UltraCut.Flat;
                pillChrome.color = UltraTheme.Accent;
                pillChrome.rim = 0f;
                pillChrome.followThemeRim = false;
                pillChrome.raycastTarget = false;
                pill.gameObject.AddComponent<UltraAccentFill>();
                var pillText = HomeText(pill, badge, 16, UltraTheme.AccentInk, 0f, 0f, 1f, 1f);
                if (pillText != null)
                {
                    pillText.alignment = TextAlignmentOptions.Center;
                    pillText.color = UltraTheme.AccentInk;
                }
            }
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Secondary);
            return button;
        }
        Button PlusButton(Transform parent, UnityAction action, float anchorX, float anchorY)
        {
            var rect = new GameObject("Add friend", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(anchorX, anchorY);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(76f, 76f);
            var chrome = rect.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Small;
            chrome.color = UltraTheme.Fg;
            chrome.rim = 0f;
            chrome.followThemeRim = false;
            chrome.raycastTarget = true;
            var horizontal = HomeRect("Plus horizontal", rect, .28f, .42f, .44f, .16f);
            var h = horizontal.gameObject.AddComponent<Image>();
            h.color = UltraTheme.Ink;
            h.raycastTarget = false;
            var vertical = HomeRect("Plus vertical", rect, .42f, .28f, .16f, .44f);
            var v = vertical.gameObject.AddComponent<Image>();
            v.color = UltraTheme.Ink;
            v.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Primary);
            return button;
        }
        Button RowChevron(Transform parent, UnityAction action)
        {
            var rect = new GameObject("Open", typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-14f, 0f);
            rect.sizeDelta = new Vector2(48f, 48f);
            var chrome = rect.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Small;
            chrome.color = UltraTheme.PanelSolid;
            chrome.rim = 1.5f;
            chrome.followThemeRim = true;
            chrome.raycastTarget = true;
            var mark = HomeRect("Chevron", rect, .28f, .28f, .44f, .44f);
            var chevron = mark.gameObject.AddComponent<UltraChevron>();
            chevron.facing = 1;
            chevron.color = UltraTheme.Fg;
            chevron.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Secondary);
            return button;
        }
        void RefreshUltraHome()
        {
            string name=string.IsNullOrEmpty(UltraServices.PlayerName)?Config.Get("DuelPlayerName0","Player"):UltraServices.PlayerName;
            if (string.IsNullOrWhiteSpace(name)) name="Player";
            if (profileLabel!=null) profileLabel.text=name;
            if (profileAccent!=null) profileAccent.enabled=false;
            if (gemCount!=null) gemCount.Set(UltraAccount.Gems, false);
            else if (gemLabel!=null) gemLabel.text=UltraAccount.Gems.ToString("N0");
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
            bool fromStart = modalPage=="start";
            bool enterHome = launchGate && !replacingModal;
            if(enterHome) launchGate=false;
            bool homeWasHidden = hiddenHome.Count>0;
            var dying = overlay;
            var page = modal;
            foreach(var child in hiddenHome)if(child!=null)child.SetActive(true);hiddenHome.Clear();
            overlay=modal=null;modalStatus=null;modalPage=null;
            if(!replacingModal) ApplyWallpaper();
            if(dying!=null)
            {
                var group = dying.GetComponent<CanvasGroup>();
                if (group == null) group = dying.gameObject.AddComponent<CanvasGroup>();
                group.blocksRaycasts=false;
                var rect = page!=null ? page : dying;
                rect.DOAnchorPos(rect.anchoredPosition+new Vector2(50f,0f),.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
                group.DOFade(0f,.25f).SetEase(UltraMotion.Ease).SetUpdate(true).OnComplete(()=>{ if(dying!=null) Destroy(dying.gameObject); });
            }
            if(fromStart || enterHome) PlayHomeIntro();
            else if(homeWasHidden) SlideHome(-70f,.25f);
            RefreshNotificationsHome();
        }
        void PlayHomeIntro()
        {
            if(homeIntroPlayed || ultraHome==null) return;
            homeIntroPlayed=true;
            string[] top = { "Profile", "Gem wallet", "NOTICES", "MISSIONS", "FRIENDS", "MENU" };
            for(int i=0;i<top.Length;i++)
            {
                var piece = ultraHome.Find(top[i]) as RectTransform;
                if(piece!=null) Slide(piece, new Vector2(0f,140f), .32f, i*0.04f);
            }
            var menu = ultraHome.Find("Menu");
            string[] rows = { "DUEL", "DECK", "SOLO", "SHOP" };
            for(int i=0;i<rows.Length;i++)
            {
                var item = menu!=null ? menu.Find(rows[i]) : null;
                var motion = item!=null ? item.Find("Motion") as RectTransform : null;
                if(motion!=null) Slide(motion, new Vector2(-70f,0f), .25f, 0.22f+i*0.04f);
            }
            var missions = ultraHome.Find("Missions") as RectTransform;
            var pass = ultraHome.Find("Duel Pass") as RectTransform;
            if(missions!=null) Slide(missions, new Vector2(-70f,0f), .25f, 0.22f+4*0.04f);
            if(pass!=null) Slide(pass, new Vector2(-70f,0f), .25f, 0.22f+5*0.04f);
            SlideFeatured();
            var header = ultraHome.GetComponentInChildren<UltraProfileHeader>(true);
            header?.BeginIntro();
            gemCount?.HoldZero();
            DOVirtual.DelayedCall(0.65f, ()=>gemCount?.Set(UltraAccount.Gems, true), true);
        }
        void SlideHome(float fromX, float time)
        {
            if(ultraHome==null) return;
            foreach(Transform child in ultraHome)
            {
                if(!child.gameObject.activeSelf) continue;
                if(child.name=="Modal overlay") continue;
                Slide(child as RectTransform, new Vector2(fromX, 0f), time, 0f);
            }
        }
        void Slide(RectTransform rect, Vector2 from, float time, float delay)
        {
            if(rect==null) return;
            var end = rect.anchoredPosition;
            rect.anchoredPosition = end+from;
            var group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha=0f;
            rect.DOAnchorPos(end, time).SetDelay(delay).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(rect.gameObject);
            group.DOFade(1f, time).SetDelay(delay).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(rect.gameObject);
        }
        void SlideFeatured()
        {
            var wall = Program.instance!=null && Program.instance.ui_!=null ? Program.instance.ui_.wallpaper : null;
            if(wall==null) return;
            var rect = wall.GetComponent<RectTransform>();
            if(rect==null) return;
            var end = rect.anchoredPosition;
            rect.anchoredPosition = end+new Vector2(90f, 0f);
            wall.alpha=0f;
            rect.DOAnchorPos(end, 0.6f).SetEase(UltraMotion.Ease).SetUpdate(true);
            wall.DOFade(1f, 0.4f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }
        void ApplyWallpaper()
        {
            if(Program.instance==null || Program.instance.ui_==null) return;
            bool home=Program.instance.currentServant==Program.instance.menu;
            bool show=home && (string.IsNullOrEmpty(modalPage) || modalPage=="start");
            if(show) UIManager.ShowWallpaper(0.25f);
            else UIManager.HideWallpaper(0.2f);
        }
        RectTransform OpenModal(string title,string subtitle,string page,bool replay=true)
        {
            replacingModal=true;
            CloseModal();
            replacingModal=false;
            modalPage=page;
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
            if(drawer){var strip=HomeRect("Drawer accent",modal,0,0,.005f,1f);var stripImage=strip.gameObject.AddComponent<Image>();stripImage.color=UltraTheme.Accent;stripImage.raycastTarget=false;strip.gameObject.AddComponent<UltraAccentFill>();}
            float hx=drawer?.16f:.075f;
            var heading=HomeText(modal,title,64,UltraTheme.Fg,hx,.875f,drawer?.78f:.70f,.09f);
            UltraTheme.Style(heading,true,4f);
            var titleRule=HomeRect("Title underline",modal,hx,.852f,drawer?.12f:.055f,.008f);
            var titleInk=titleRule.gameObject.AddComponent<Image>();titleInk.color=UltraTheme.Accent;titleInk.raycastTarget=false;titleRule.gameObject.AddComponent<UltraAccentFill>();
            var sub=HomeText(modal,subtitle,18,Muted,hx,.785f,.80f,.05f);
            UltraTheme.StyleEmphasis(sub,8f);
            SquareBack(modal,CloseModal,drawer?.03f:.012f,.875f,drawer?.09f:.045f,.075f);
            if(replay) modal.gameObject.AddComponent<UltraPageTransition>();
            modalStatus=HomeText(modal,"",16,Lime,.045f,.02f,.90f,.04f);
            modalStatus.gameObject.AddComponent<UltraAccentText>();
            if(modal.GetComponent<CanvasGroup>()==null)modal.gameObject.AddComponent<CanvasGroup>();
            ApplyWallpaper();
            return modal;
        }
        Button ArrowButton(Transform parent, int facing, UnityAction action, float x, float y, float w, float h)
        {
            var rect = HomeRect(facing == 0 ? "Previous" : "Next", parent, x, y, w, h);
            var chrome = rect.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Small;
            chrome.color = UltraTheme.PanelSolid;
            chrome.rim = 1.5f;
            chrome.followThemeRim = true;
            chrome.raycastTarget = true;
            var mark = HomeRect("Arrow", rect, .28f, .22f, .44f, .56f);
            var arrow = mark.gameObject.AddComponent<UltraChevron>();
            arrow.facing = facing;
            arrow.color = UltraTheme.Fg;
            arrow.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            rect.gameObject.AddComponent<UltraThemedButton>().Configure(UltraTier.Secondary);
            return button;
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
        void OpenLaunchLogin()
        {
            ShowAccess(true);
        }
        void OpenLogin()
        {
            ShowAccess(false);
        }
        void ShowAccess(bool launch)
        {
            replacingModal=true;
            CloseModal();
            replacingModal=false;
            launchGate=launch;
            modalPage="login";
            overlay=HomeRect("Modal overlay",ultraHome,0,0,1,1);
            var shade=overlay.gameObject.AddComponent<Image>();shade.color=Color.clear;shade.raycastTarget=true;
            foreach(Transform child in ultraHome)if(child!=overlay && child.gameObject.activeSelf){hiddenHome.Add(child.gameObject);child.gameObject.SetActive(false);}
            var scrim=HomeRect("Page scrim",overlay,0,0,1,1).gameObject.AddComponent<UltraScrim>();
            scrim.strong=true;scrim.raycastTarget=false;
            if(!launch) SquareBack(overlay,CloseModal,.028f,.90f,.05f,.08f);
            accessCard=HomeRect("Access terminal",overlay,.5f,.5f,0f,0f);
            accessCard.anchorMin=accessCard.anchorMax=new Vector2(.5f,.5f);
            accessCard.pivot=new Vector2(.5f,.5f);
            accessCard.anchoredPosition=Vector2.zero;
            HomePanel(accessCard,new Color(UltraTheme.Ink.r,UltraTheme.Ink.g,UltraTheme.Ink.b,.98f));
            modal=accessCard;
            var wordHost=Px("Wordmark",accessCard,32f,28f,456f,48f);
            var word=HomeText(wordHost,"ULTRA DUEL",40,Color.white,0f,0f,1f,1f);
            if(word!=null)
            {
                word.alignment=TextAlignmentOptions.Center;
                word.enableAutoSizing=false;
                word.fontSize=40f;
                UltraTheme.Style(word,true,6f);
                word.fontSize=40f;
            }
            accessRule=Px("Wordmark underline",accessCard,240f,84f,40f,4f);
            accessRule.anchorMin=accessRule.anchorMax=new Vector2(.5f,1f);
            accessRule.pivot=new Vector2(.5f,1f);
            var ruleInk=accessRule.gameObject.AddComponent<Image>();ruleInk.color=UltraTheme.Accent;ruleInk.raycastTarget=false;
            accessRule.gameObject.AddComponent<UltraAccentFill>();
            accessRemembered=!string.IsNullOrEmpty(UltraServices.Token);
            accessWelcome=null;
            if(accessRemembered)
            {
                string who=string.IsNullOrEmpty(UltraServices.PlayerName)?"Duelist":UltraServices.PlayerName;
                accessWelcome=Px("Welcome row",accessCard,32f,120f,456f,40f);
                var disc=Px("Profile picture",accessWelcome,0f,2f,36f,36f);
                UltraUi.EnsureRenderer(disc.gameObject);
                var maskGraphic=disc.gameObject.AddComponent<UltraCircleGraphic>();
                maskGraphic.color=UltraTheme.PanelSolid;maskGraphic.raycastTarget=false;
                var mask=disc.gameObject.AddComponent<Mask>();mask.showMaskGraphic=true;
                var face=Px("Face",disc,0f,0f,36f,36f);
                face.anchorMin=Vector2.zero;face.anchorMax=Vector2.one;face.offsetMin=face.offsetMax=Vector2.zero;
                var portrait=face.gameObject.AddComponent<Image>();portrait.preserveAspect=true;portrait.raycastTarget=false;
                LoadProfileIcon(portrait,ProfileIconId()).Forget();
                var hello=HomeText(accessWelcome,"Welcome back, "+who,18,UltraTheme.Fg,.12f,0f,.86f,1f);
                if(hello!=null){hello.enableAutoSizing=false;hello.fontSize=18f;hello.alignment=TextAlignmentOptions.MidlineLeft;UltraTheme.Style(hello,false);}
            }
            accessUser=AccessField("Username (3–24 characters)",false);
            accessPass=AccessField("Password (8 characters minimum)",true);
            if(accessRemembered && accessUser!=null && !string.IsNullOrEmpty(UltraServices.PlayerName)) accessUser.text=UltraServices.PlayerName;
            UnityAction signIn=()=>Run(async()=>{await UltraServices.SignIn(accessUser.text,accessPass.text,false);accessPass.text="";ShowFriends();});
            UnityAction create=()=>Run(async()=>{await UltraServices.SignIn(accessUser.text,accessPass.text,true);accessPass.text="";ShowFriends();});
            accessContinue=AccessButton("CONTINUE",CloseModal,UltraTier.Primary,22f);
            accessSwitch=AccessLink("Use a different account",()=>LayoutAccess(true));
            accessSignIn=AccessButton("SIGN IN",signIn,UltraTier.Primary,20f);
            accessCreate=AccessButton("CREATE ACCOUNT",create,UltraTier.Secondary,18f);
            var statusHost=Px("Access status",overlay,0f,0f,520f,28f);
            statusHost.anchorMin=statusHost.anchorMax=new Vector2(.5f,.5f);
            statusHost.pivot=new Vector2(.5f,1f);
            modalStatus=HomeText(statusHost,"",16,Lime,0f,0f,1f,1f);
            if(modalStatus!=null){modalStatus.alignment=TextAlignmentOptions.Center;modalStatus.gameObject.AddComponent<UltraAccentText>();}
            if(accessCard.GetComponent<CanvasGroup>()==null)accessCard.gameObject.AddComponent<CanvasGroup>();
            LayoutAccess(false);
            ApplyWallpaper();
        }
        void LayoutAccess(bool credentials)
        {
            if(accessCard==null) return;
            const float pad=32f, inner=456f;
            float y=28f;
            var word=accessCard.Find("Wordmark") as RectTransform;
            if(word!=null) Place(word,pad,y,inner,48f);
            y+=48f+8f;
            if(accessRule!=null)
            {
                accessRule.anchorMin=accessRule.anchorMax=new Vector2(.5f,1f);
                accessRule.pivot=new Vector2(.5f,1f);
                accessRule.anchoredPosition=new Vector2(0f,-y);
                accessRule.sizeDelta=new Vector2(40f,4f);
            }
            y+=4f+16f;
            if(accessWelcome!=null)
            {
                accessWelcome.gameObject.SetActive(accessRemembered);
                if(accessRemembered)
                {
                    string who=string.IsNullOrEmpty(UltraServices.PlayerName)?"Duelist":UltraServices.PlayerName;
                    float textW=Mathf.Clamp(who.Length*10f+150f,180f,380f);
                    float rowW=36f+12f+textW;
                    Place(accessWelcome,(520f-rowW)*0.5f,y,rowW,40f);
                    var hello=accessWelcome.Find("Label") as RectTransform;
                    if(hello!=null) Place(hello,48f,0f,textW,40f);
                    y+=40f+20f;
                }
            }
            bool quick=accessRemembered && !credentials;
            if(accessContinue!=null) accessContinue.gameObject.SetActive(quick);
            if(accessSwitch!=null) accessSwitch.gameObject.SetActive(quick);
            if(accessUser!=null) accessUser.gameObject.SetActive(!quick);
            if(accessPass!=null) accessPass.gameObject.SetActive(!quick);
            if(accessSignIn!=null) accessSignIn.gameObject.SetActive(!quick);
            if(accessCreate!=null) accessCreate.gameObject.SetActive(!quick);
            if(quick)
            {
                Place(accessContinue.transform as RectTransform,pad,y,inner,56f);
                y+=56f+12f;
                Place(accessSwitch.transform as RectTransform,pad,y,inner,28f);
                y+=28f+20f;
            }
            else
            {
                Place(accessUser.transform as RectTransform,pad,y,inner,52f);
                y+=52f+16f;
                Place(accessPass.transform as RectTransform,pad,y,inner,52f);
                y+=52f+16f;
                Place(accessSignIn.transform as RectTransform,pad,y,inner,52f);
                y+=52f+16f;
                Place(accessCreate.transform as RectTransform,pad,y,inner,52f);
                y+=52f+16f;
            }
            accessCard.sizeDelta=new Vector2(520f,y);
            if(modalStatus!=null)
            {
                var status=modalStatus.rectTransform.parent as RectTransform;
                if(status!=null)
                {
                    status.anchorMin=status.anchorMax=new Vector2(.5f,.5f);
                    status.pivot=new Vector2(.5f,1f);
                    status.anchoredPosition=new Vector2(0f,-(y*0.5f+10f));
                    status.sizeDelta=new Vector2(520f,28f);
                }
            }
        }
        static void Place(RectTransform rect,float left,float top,float width,float height)
        {
            if(rect==null) return;
            rect.anchorMin=rect.anchorMax=new Vector2(0f,1f);
            rect.pivot=new Vector2(0f,1f);
            rect.anchoredPosition=new Vector2(left,-top);
            rect.sizeDelta=new Vector2(width,height);
        }
        TMP_InputField AccessField(string placeholder,bool password)
        {
            var rect=Px(placeholder,accessCard,32f,0f,456f,52f);
            HomePanel(rect,UltraTheme.PanelSolid);
            rect.GetComponent<Image>().raycastTarget=true;
            var field=rect.gameObject.AddComponent<TMP_InputField>();
            var text=HomeText(rect,"",22,UltraTheme.Fg,.04f,.12f,.92f,.76f);UltraTheme.Style(text,false);
            var hint=HomeText(rect,placeholder,20,Muted,.04f,.12f,.92f,.76f);UltraTheme.Style(hint,false);
            if(text!=null) text.enableAutoSizing=false;
            if(hint!=null) hint.enableAutoSizing=false;
            field.textViewport=rect;field.textComponent=text;field.placeholder=hint;
            field.characterLimit=password?128:24;
            field.contentType=password?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;
            return field;
        }
        Button AccessButton(string text,UnityAction action,UltraTier tier,float size)
        {
            var rect=Px(text,accessCard,32f,0f,456f,52f);
            var image=rect.gameObject.AddComponent<UltraChrome>();image.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=image;
            var label=HomeText(rect,text,size,UltraTheme.Fg,.06f,.08f,.88f,.84f);
            if(label!=null){label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;label.fontSize=size;}
            var themed=rect.gameObject.AddComponent<UltraThemedButton>();themed.Configure(tier);
            button.onClick.AddListener(action);
            return button;
        }
        Button AccessLink(string text,UnityAction action)
        {
            var rect=Px(text,accessCard,32f,0f,456f,28f);
            var hit=rect.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=hit;button.transition=Selectable.Transition.None;
            var label=HomeText(rect,text,16,UltraTheme.Accent,0f,0f,1f,1f);
            if(label!=null){label.alignment=TextAlignmentOptions.Center;label.enableAutoSizing=false;label.fontSize=16f;label.color=UltraTheme.Accent;UltraTheme.Style(label,false);}
            button.onClick.AddListener(action);
            return button;
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
            var follow=HomeText(modal,"FOLLOW  /  "+((UltraServices.State?["friends"] as JArray)?.Count??0),18,UltraTheme.Fg,.055f,.735f,.80f,.04f);
            UltraTheme.StyleEmphasis(follow,10f);
            var add=Input(modal,"Enter friend's name",.62f);
            add.GetComponent<RectTransform>().anchorMax=new Vector2(.78f,.71f);
            PlusButton(modal,()=>Run(async()=>{await UltraServices.Request("/friends/request","POST",new{name=add.text});await UltraServices.Refresh();ShowFriends();}),.955f,.665f);
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
                var row=Row(content,index++,(string)person["name"],(string)person["activity"]??(online?"Online":"Offline"));
                HomePanel(HomeRect("Online edge",row,0,0,.008f,1),online?Lime:Muted);
                var icon=HomeRect("Friend mate",row,.025f,.12f,.13f,.76f).gameObject.AddComponent<Image>();icon.preserveAspect=true;
                LoadMatePortrait(icon,(int?)person["mate"]??1000001).Forget();
                var dot=HomeRect("Presence",row,.20f,.62f,0f,0f);
                dot.anchorMin=dot.anchorMax=new Vector2(.20f,.68f);
                dot.sizeDelta=new Vector2(8f,8f);
                var presence=dot.gameObject.AddComponent<UltraCircleGraphic>();
                presence.color=online?UltraTheme.Accent:UltraTheme.Mute;
                presence.raycastTarget=false;
                foreach(var label in row.GetComponentsInChildren<TextMeshProUGUI>()){var r=label.rectTransform;r.anchorMin=new Vector2(.24f,r.anchorMin.y);r.anchorMax=new Vector2(.78f,r.anchorMax.y);}
                RowChevron(row,()=>OpenFriend(person));
            }
            foreach(var person in state?["sent"] as JArray ?? new JArray())
            {
                int id=(int)person["id"];var row=Row(content,index++,(string)person["name"],"Request sent");
                SmallButton(row,"X",()=>RemoveFriend(id),.84f,.20f,.13f,.60f,18);
            }
            if(index==0)Row(content,0,"Find your friends","Add their Ultra Duel username above.");
            SmallButton(modal,"REFRESH",()=>Run(async()=>{await UltraServices.Refresh();ShowFriends();}),.055f,.03f,.36f,.075f,18);
            SmallButton(modal,"SIGN OUT",()=>Run(async()=>{await UltraServices.SignOut();OpenLogin();}),.58f,.03f,.36f,.075f,18);
        }
        void RemoveFriend(int id) {Run(async()=>{await UltraServices.Request("/friends/"+id,"DELETE");await UltraServices.Refresh();ShowFriends();});}
        void OpenDuel()
        {
            OpenModal("DUEL","Choose how you want to play","duel-modes");WideModal();
            ModeCard("ONLINE DUEL","01","RANKED  ·  CASUAL","ONLINE DUEL",OpenOnlineDuel,.045f,.12f,.50f,.62f,true);
            ModeCard("FRIEND DUELS","02","","FRIEND DUELS",()=>OpenFriendDuels("1v1"),.57f,.546f,.385f,.194f,false);
            ModeCard("DUOS 2v2","03","","DUOS 2v2",()=>OpenFriendDuels("2v2"),.57f,.333f,.385f,.194f,false);
            ModeCard("SOLO","04","","SOLO",()=>{CloseModal();Program.instance.ShiftToServant(Program.instance.solo);},.57f,.12f,.385f,.194f,false);
        }
        Button ModeCard(string name, string index, string eyebrow, string title, UnityAction action, float x, float y, float w, float h, bool hero)
        {
            var card = HomeRect(name, modal, x, y, w, h);
            var chrome = card.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Panel;
            chrome.color = hero ? UltraTheme.Accent : UltraTheme.PanelSolid;
            chrome.rim = hero ? 0f : 1.5f;
            chrome.followThemeRim = !hero;
            chrome.raycastTarget = true;
            if (hero)
            {
                card.gameObject.AddComponent<UltraAccentFill>();
                var motif = HomeRect("Diamond", card, .58f, .18f, .34f, .62f);
                var diamond = motif.gameObject.AddComponent<UltraDiamond>();
                diamond.color = new Color(1f, 1f, 1f, 0.16f);
                diamond.raycastTarget = false;
                var echo = HomeRect("Diamond echo", card, .68f, .08f, .22f, .40f);
                var small = echo.gameObject.AddComponent<UltraDiamond>();
                small.color = new Color(1f, 1f, 1f, 0.10f);
                small.raycastTarget = false;
            }
            else
            {
                var stripe = HomeRect("Stripe", card, 0f, .16f, .012f, .68f);
                var bar = stripe.gameObject.AddComponent<Image>();
                bar.color = UltraTheme.Accent;
                bar.raycastTarget = false;
                stripe.gameObject.AddComponent<UltraAccentFill>();
            }
            var indexLabel = HomeText(card, index, hero ? 42 : 28, hero ? Color.white : UltraTheme.Mute, .06f, .72f, .40f, .20f);
            UltraTheme.Style(indexLabel, true, 6f);
            if (hero && indexLabel != null) indexLabel.gameObject.AddComponent<UltraOnAccentText>();
            if (!string.IsNullOrEmpty(eyebrow))
            {
                var brow = HomeText(card, eyebrow, 18, hero ? Color.white : UltraTheme.Mute, .06f, .60f, .70f, .12f);
                UltraTheme.StyleEmphasis(brow, 10f);
                if (hero && brow != null) brow.gameObject.AddComponent<UltraOnAccentText>();
            }
            var label = HomeText(card, title, hero ? 78 : 40, hero ? Color.white : UltraTheme.Fg, .06f, .08f, .68f, hero ? .28f : .42f);
            if (label != null)
            {
                label.alignment = TextAlignmentOptions.BottomLeft;
                UltraTheme.Style(label, true, 2f);
                if (hero) label.gameObject.AddComponent<UltraOnAccentText>();
            }
            var mark = new GameObject("Go", typeof(RectTransform)).GetComponent<RectTransform>();
            mark.gameObject.layer = gameObject.layer;
            mark.SetParent(card, false);
            mark.anchorMin = mark.anchorMax = new Vector2(1f, 0f);
            mark.pivot = new Vector2(1f, 0f);
            mark.anchoredPosition = new Vector2(-18f, 18f);
            mark.sizeDelta = new Vector2(hero ? 64f : 52f, hero ? 64f : 52f);
            var plate = mark.gameObject.AddComponent<UltraChrome>();
            plate.cut = UltraCut.Small;
            plate.color = hero ? UltraTheme.Ink : UltraTheme.PanelSolid;
            plate.rim = hero ? 0f : 1.5f;
            plate.rimColor = UltraTheme.Accent;
            plate.followThemeRim = false;
            plate.raycastTarget = false;
            var chevronHost = HomeRect("Chevron", mark, .26f, .26f, .48f, .48f);
            var chevron = chevronHost.gameObject.AddComponent<UltraChevron>();
            chevron.facing = 1;
            chevron.color = Color.white;
            chevron.raycastTarget = false;
            var button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            var motion = card.gameObject.AddComponent<UltraButtonMotion>();
            motion.liftPixels = 4f;
            motion.pressScale = 0.98f;
            return button;
        }
        public void OpenFriendDuels(string mode="1v1")
        {
            friendRoomMode=mode;
            if(string.IsNullOrEmpty(UltraServices.Token)){OpenLogin();return;}
            ShowRooms();Run(async()=>{await UltraServices.Refresh();if(modalPage=="rooms")ShowRooms(false);});
        }
        void ShowRooms(bool replay = true) { BuildRoomBrowser(replay); }
        void OpenPass()
        {
            OpenModal("DUEL PASS","Earn rewards by winning duels · No paid tier","pass");
            WideModal();int first=passPage*6+1;
            var head=HomeRect("Pass header",modal,.045f,.60f,.91f,.16f);
            HomePanel(head,UltraTheme.Panel);
            var headStripe=HomeRect("Stripe",head,0f,.18f,.008f,.64f);
            var headBar=headStripe.gameObject.AddComponent<Image>();headBar.color=UltraTheme.Accent;headBar.raycastTarget=false;headStripe.gameObject.AddComponent<UltraAccentFill>();
            var gradeKicker=HomeText(head,"GRADE",14,UltraTheme.Mute,.04f,.58f,.22f,.28f);UltraTheme.StyleEmphasis(gradeKicker,8f);
            var gradeValue=HomeText(head,UltraAccount.Tier+" / 20",36,UltraTheme.Fg,.04f,.10f,.24f,.46f);UltraTheme.Style(gradeValue,true,2f);
            var nextKicker=HomeText(head,"NEXT",14,UltraTheme.Mute,.74f,.58f,.22f,.28f);UltraTheme.StyleEmphasis(nextKicker,8f);if(nextKicker!=null)nextKicker.alignment=TextAlignmentOptions.MidlineRight;
            var nextValue=HomeText(head,UltraAccount.Tier>=20?"COMPLETE":(200-UltraAccount.XP%200)+" XP",28,UltraTheme.Fg,.62f,.10f,.34f,.46f);UltraTheme.Style(nextValue,true,2f);if(nextValue!=null)nextValue.alignment=TextAlignmentOptions.MidlineRight;
            var track=HomeRect("XP track",head,.30f,.22f,.40f,.28f);
            UltraUi.EnsureRenderer(track.gameObject);
            var passBar=track.gameObject.AddComponent<UltraAngularBar>();
            passBar.raycastTarget=false;
            passBar.amount=UltraAccount.Tier>=20?1f:(UltraAccount.XP%200)/200f;
            var rail=HomeRect("Free pass track",modal,.045f,.18f,.11f,.38f);
            HomePanel(rail,UltraTheme.PanelSolid);
            var railLabel=HomeText(rail,"FREE\nPASS",20,Color.white,.08f,.22f,.84f,.56f);
            if(railLabel!=null){railLabel.alignment=TextAlignmentOptions.Center;UltraTheme.Style(railLabel,true,4f);}
            for(int col=0;col<6 && first+col<=20;col++)
            {
                int tier=first+col;float x=.175f+col*.125f;
                var cell=HomeRect("Grade "+tier,modal,x,.18f,.115f,.38f);
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
            PassPager("<",0,()=>{passPage=Math.Max(0,passPage-1);OpenPass();},.045f);
            PassPager(">",1,()=>{passPage=Math.Min(3,passPage+1);OpenPass();},.16f);
            HomeText(modal,(passPage+1)+" / 4",16,Muted,.24f,.035f,.12f,.07f).alignment=TextAlignmentOptions.MidlineLeft;
            SmallButton(modal,"CLAIM REWARDS",()=>Run(async()=>{await UltraAccount.Claim();OpenPass();}),.55f,.03f,.40f,.09f,24,UltraTier.Primary).interactable=UltraAccount.Tier>UltraAccount.Claimed;
            modalStatus.rectTransform.anchorMin=new Vector2(.045f,.125f);modalStatus.rectTransform.anchorMax=new Vector2(.50f,.165f);
            modalStatus.text=UltraAccount.Profile==null?"Offline progress / Saved on this PC":"Account rewards / Saved on Ultra Duel";
        }
        void PassPager(string name, int facing, UnityAction action, float x)
        {
            var rect = HomeRect(name, modal, x, .03f, .055f, .085f);
            var chrome = rect.gameObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Small;
            chrome.color = UltraTheme.PanelSolid;
            chrome.rim = 1.5f;
            chrome.followThemeRim = true;
            chrome.raycastTarget = true;
            var mark = HomeRect("Chevron", rect, .28f, .28f, .44f, .44f);
            var chevron = mark.gameObject.AddComponent<UltraChevron>();
            chevron.facing = facing;
            chevron.color = UltraTheme.Fg;
            chevron.raycastTarget = false;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = chrome;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
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
            var perf=HomeText(modal,"FPS N/A    |    GPU N/A    |    CPU N/A    |    LAT N/A",14,Muted,.34f,.905f,.62f,.035f);
            if(perf!=null)
            {
                perf.alignment=TextAlignmentOptions.MidlineRight;
                UltraTheme.StyleEmphasis(perf,4f);
                perf.gameObject.AddComponent<UltraPerfReadout>();
            }
            SectionLabel(modal,"DISPLAY",.04f,.80f);
            MenuCard("SETTINGS",()=>{CloseModal();OnSetting();},0,.04f,.615f,.30f,.145f);
            MenuCard("WALLPAPER",OpenWallpaper,1,.355f,.615f,.30f,.145f);
            MenuCard("CUT-INS",()=>{CloseModal();OnCutin();},3,.04f,.455f,.30f,.145f);
            MenuCard("GUI SCALE",OpenGuiScale,4,.355f,.455f,.30f,.145f,UltraGuiScale.Percent+"%");
            SectionLabel(modal,"PLAY",.675f,.80f);
            MenuCard("MATCH HISTORY",OpenHistory,2,.675f,.585f,.285f,.17f);
            MenuCard("PUZZLES",()=>{CloseModal();OnPuzzle();},5,.675f,.40f,.285f,.17f);
            SectionLabel(modal,"SYSTEM",.04f,.385f);
            MenuCard("CHECK UPDATES",OpenUpdates,6,.04f,.22f,.30f,.145f);
            MenuCard("SAVE BACKUPS",OpenBackups,7,.355f,.22f,.30f,.145f);
            MenuCard("GITHUB",()=>Application.OpenURL(UltraServices.Repository),8,.675f,.22f,.285f,.145f);
            SectionLabel(modal,"SESSION",.04f,.155f);
            float sessionW=(.30f+.015f+.30f-.015f)*.5f;
            SmallButton(modal,"SIGN OUT",()=>Run(async()=>{await UltraServices.SignOut();OpenLogin();}),.04f,.03f,sessionW,.10f,22);
            SmallButton(modal,"EXIT",()=>{CloseModal();OnExit();},.04f+sessionW+.015f,.03f,sessionW,.10f,22,UltraTier.Danger);
        }
    }
}
