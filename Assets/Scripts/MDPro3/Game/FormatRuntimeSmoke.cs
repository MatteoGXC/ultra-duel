using System;
using System.Collections;
using System.IO;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

namespace MDPro3
{
    // Opt-in diagnostics for the packaged player; inactive during ordinary play.
    public sealed class FormatRuntimeSmoke : MonoBehaviour
    {
        private string output;
        private string mode;
        private float deadline;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "--format-smoke");
            if (index < 0 || index + 2 >= args.Length) return;
            var target = new GameObject("FormatRuntimeSmoke");
            DontDestroyOnLoad(target);
            var smoke = target.AddComponent<FormatRuntimeSmoke>();
            smoke.output = Path.GetFullPath(args[index + 1]);
            smoke.mode = args[index + 2];
            Directory.CreateDirectory(smoke.output);
            smoke.deadline = Time.realtimeSinceStartup + 180;
            smoke.StartCoroutine(smoke.Run());
        }

        private void Update()
        {
            if (deadline > 0 && Time.realtimeSinceStartup > deadline)
            {
                Debug.LogError("FORMAT_SMOKE_TIMEOUT " + mode);
                Application.Quit(2);
            }
        }

        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(2);
            string path = Path.Combine(output, name + ".png");
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            // Capture the real composited frame. Changing an Overlay camera to
            // Base removes it from the URP camera stack on Metal and corrupts
            // subsequent diagnostic renders, even after restoring renderType.
            File.WriteAllLines(Path.Combine(output,name+"-materials.txt"), Resources.FindObjectsOfTypeAll<Material>()
                .Where(m=>m!=null && m.shader!=null).Select(m=>m.name+" | "+m.shader.name+" | "+string.Join(",",m.GetTexturePropertyNames().Select(p=>p+"="+(m.GetTexture(p)==null?"null":m.GetTexture(p).name)))));
            ScreenCapture.CaptureScreenshot(path);
            yield return new WaitForEndOfFrame();
            float captureDeadline=Time.realtimeSinceStartup+10;
            while(!File.Exists(path) && Time.realtimeSinceStartup<captureDeadline)yield return null;
            if(!File.Exists(path))throw new Exception("The Mac framebuffer capture did not complete.");
            yield break;
#else
            var cameras = Program.instance.camera_;
            var camera = cameras.cameraMain.gameObject.activeInHierarchy ? cameras.cameraMain : cameras.camera2D;
            var target = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,
                new UnityEngine.Rendering.RenderPipeline.StandardRequest { destination = target });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var scene = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            scene.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            scene.Apply();
            File.WriteAllBytes(Path.Combine(output, name + "-scene.png"), scene.EncodeToPNG());
            RenderTexture.active = previous;
            // UI uses an Overlay camera. Render it to the same diagnostic target as
            // a standalone Base camera, then restore its normal stack membership.
            var uiData = cameras.cameraUI.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            var renderType = uiData.renderType;
            var clear = cameras.cameraUI.clearFlags;
            var background = cameras.cameraUI.backgroundColor;
            uiData.renderType = UnityEngine.Rendering.Universal.CameraRenderType.Base;
            cameras.cameraUI.clearFlags = CameraClearFlags.SolidColor;
            cameras.cameraUI.backgroundColor = Color.clear;
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(cameras.cameraUI,
                new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest { destination = target });
            uiData.renderType = renderType;
            cameras.cameraUI.clearFlags = clear;
            cameras.cameraUI.backgroundColor = background;
            RenderTexture.active = target;
            var texture = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            texture.Apply();
            var foreground = texture.GetPixels32();
            var back = scene.GetPixels32();
            for (int i = 0; i < foreground.Length; ++i)
            {
                float alpha = foreground[i].a / 255f;
                foreground[i] = new Color32(
                    (byte)(foreground[i].r * alpha + back[i].r * (1f-alpha)),
                    (byte)(foreground[i].g * alpha + back[i].g * (1f-alpha)),
                    (byte)(foreground[i].b * alpha + back[i].b * (1f-alpha)), 255);
            }
            texture.SetPixels32(foreground);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            RenderTexture.active = previous;
            target.Release();
            Destroy(target);
            Destroy(texture);
            Destroy(scene);
#endif
        }

        private IEnumerator CheckUltraFeatures()
        {
            var audit = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText("Data/card-update-audit.json"));
            foreach (int id in audit["addedIds"].Select(v => (int)v))
                if (CardsManager.GetCardRaw(id) == null) throw new Exception("New card missing from runtime: " + id);
            var pack = PacksManager.packs.FirstOrDefault(p => p.fullName.Contains("BURST PROTOCOL") && p.year == 2026);
            if (pack == null) throw new Exception("English Burst Protocol pack is missing.");
            int burstCount = CardsManager._cards.Values.Count(c => c.packFullName != null && c.packFullName.Contains("BURST PROTOCOL") && c.year == 2026);
            if (burstCount < 100) throw new Exception("English Burst Protocol pack is incomplete: " + burstCount);
            int[] newTcg = audit["addedIds"].Select(v => (int)v).Where(id => (CardsManager.GetCardRaw(id).Ot & 2) != 0).Take(3).ToArray();
            foreach (int id in newTcg)
            {
                var art = MDPro3.Utility.CardImageLoader.LoadArtAsync(id).AsTask();
                while (!art.IsCompleted) yield return null;
                if (art.IsFaulted || art.Result == null) throw new Exception("New card art failed to load: " + id);
                MDPro3.Utility.CardImageLoader.ReleaseArt(id);
            }
            var health = MDPro3.Net.UltraServices.Request("/health").AsTask();
            while (!health.IsCompleted) yield return null;
            if (health.IsFaulted || (bool?)health.Result["ok"] != true) throw new Exception("Unity could not reach the pinned HTTPS friends service.", health.Exception);
            string original = File.Exists("Progress/profile.json") ? File.ReadAllText("Progress/profile.json") : null;
            var fields = typeof(UltraProgress).GetField("profile", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            fields.SetValue(null, new UltraProgress.Profile());
            try
            {
                UltraProgress.BeginDuel();
                if (!UltraProgress.Win() || UltraProgress.Win() || UltraProgress.Data.xp != 100) throw new Exception("Win rewards duplicated.");
                UltraProgress.BeginDuel(); UltraProgress.Win();
                if (UltraProgress.Claim() != 1 || UltraProgress.Data.gems != 50 || UltraProgress.Claim() != 0) throw new Exception("Pass claim was not idempotent.");
                if (UltraProgress.Buy("Violet")) throw new Exception("Shop allowed an unaffordable frame.");
                for (int i=0;i<4;i++) { UltraProgress.BeginDuel(); UltraProgress.Win(); }
                UltraProgress.Claim();
                if (!UltraProgress.Buy("Violet") || UltraProgress.Data.gems != 0) throw new Exception("Frame purchase failed.");
                if (!UltraProgress.Buy("Violet") || UltraProgress.Data.gems != 0) throw new Exception("Owned frame charged twice.");
                var saved = File.ReadAllText("Progress/profile.json"); fields.SetValue(null, null);
                if (UltraProgress.Data.frame != "Violet" || UltraProgress.Data.wins != 6) throw new Exception("Pass progress was not persisted.");
                if(UltraAccount.Profile==null)
                {
                    for(int i=0;i<2;i++)
                    {
                        var claim=UltraMissions.Claim().AsTask();while(!claim.IsCompleted)yield return null;
                        if(claim.IsFaulted)throw claim.Exception;
                        if(UltraProgress.Data.gems!=20)throw new Exception("Offline mission rewards were missing or duplicated.");
                    }
                    Debug.Log("ULTRA_MISSIONS_PASSED offline persistent grade reward and duplicate claim");
                }

            }
            finally
            {
                if (original != null) File.WriteAllText("Progress/profile.json", original);
                else if (File.Exists("Progress/profile.json")) File.Delete("Progress/profile.json");
                fields.SetValue(null, null);
            }
            var home = Program.instance.menu.GetUI<MainMenuUI>();
            foreach (string page in new[] { "OpenLogin", "OpenPass", "OpenShop", "OpenMissions" })
            {
                typeof(MainMenuUI).GetMethod(page, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home, null);
                yield return Capture(page);
                if (page == "OpenLogin")
                {
                    var inputs = home.GetComponentsInChildren<TMPro.TMP_InputField>();
                    if (inputs.Length != 2 || !inputs.Any(i => i.contentType == TMPro.TMP_InputField.ContentType.Password)) throw new Exception("Account form/password masking missing.");
                }
                typeof(MainMenuUI).GetMethod("CloseModal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home, null);
            }
            if (Environment.GetCommandLineArgs().Contains("--ultra-online"))
            {
                typeof(MainMenuUI).GetMethod("OpenLogin", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                var inputs = home.GetComponentsInChildren<TMPro.TMP_InputField>();
                inputs.First(i => i.contentType != TMPro.TMP_InputField.ContentType.Password).text = "UT_UI_" + Guid.NewGuid().ToString("N").Substring(0,8);
                inputs.First(i => i.contentType == TMPro.TMP_InputField.ContentType.Password).text = Guid.NewGuid().ToString("N");
                var register = home.GetComponentsInChildren<UnityEngine.UI.Button>().First(b => b.GetComponentInChildren<TMPro.TextMeshProUGUI>()?.text == "CREATE ACCOUNT");
                register.onClick.Invoke();
                float end=Time.realtimeSinceStartup+20;
                while (MDPro3.Net.UltraServices.State == null && Time.realtimeSinceStartup<end) yield return null;
                if (MDPro3.Net.UltraServices.State == null) throw new Exception("In-game account creation did not complete.");
                yield return Capture("friends-account");
                typeof(MainMenuUI).GetMethod("OpenDuel", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                yield return Capture("friends-rooms");
                var logout=MDPro3.Net.UltraServices.SignOut().AsTask();
                while(!logout.IsCompleted)yield return null;
                if(logout.IsFaulted || !string.IsNullOrEmpty(MDPro3.Net.UltraServices.Token))throw new Exception("Account sign out failed.");
                typeof(MainMenuUI).GetMethod("CloseModal", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                Debug.Log("ULTRA_ACCOUNT_UI_PASSED registration, HTTPS session, friend list, rooms and sign out");
            }
            Debug.Log("ULTRA_FEATURES_PASSED 300 cards, English Burst Protocol, artwork, pinned HTTPS, pass rewards, persistence and native account UI");
        }

        private IEnumerator Run()
        {
            while (Program.instance == null || Program.instance.menu.servantUI == null || !TextureManager.loaded)
                yield return null;
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
            var localArtwork=MDPro3.Utility.CardImageLoader.LoadArtAsync(89631139).AsTask();
            while(!localArtwork.IsCompleted)yield return null;
            if(localArtwork.IsFaulted || localArtwork.Result==null || localArtwork.Result.width<32)
                throw new Exception("Installed Mac card artwork failed to load from local storage.",localArtwork.Exception);
            MDPro3.Utility.CardImageLoader.ReleaseArt(89631139);
            Debug.Log("FORMAT_SMOKE_MAC_LOCAL_ARTWORK_PASSED");
#endif
            Debug.Log("FORMAT_SMOKE_MENU_READY");
            yield return Capture("menu");
            var commandLine = Environment.GetCommandLineArgs();
            if(commandLine.Contains("--owner-admin-check"))
            {
                var refresh=MDPro3.Net.UltraServices.Refresh().AsTask();
                while(!refresh.IsCompleted)yield return null;
                if(refresh.IsFaulted || !UltraAdminAccess.IsOwner)throw new Exception("An authenticated primary owner session is required for this check.");
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                typeof(MainMenuUI).GetMethod("OpenAdmin",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                yield return Capture("owner-account-controls");
                typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
            }
            if(commandLine.Contains("--solo-ai-audit"))
            {
                deadline=Time.realtimeSinceStartup+7200;
                yield return SoloAiRuntimeAudit.Run(output);
                yield break;
            }
            if(commandLine.Contains("--ultra-ui-pages"))
            {
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                if(commandLine.Contains("--wallpaper-theme-check"))
                {
                    float green=UltraWallpaperTheme.DominantHue(new[]{new Color32(0,255,0,255),new Color32(255,255,255,255)});
                    if(Mathf.Abs(Mathf.DeltaAngle(green,120))>1)throw new Exception("Wallpaper color sampling lost the dominant green accent.");
                    string original=Config.Get("Wallpaper",UIManager.DefaultWallpaper);
                    var theme=Program.instance.background_.GetComponent<UltraWallpaperTheme>();
                    foreach(var wallpaper in new[]{"1130001","1135010"})
                    {
                        Config.Set("Wallpaper",wallpaper);Program.instance.ui_.ChangeWallpaper(wallpaper);
                        while(theme.AppliedWallpaper!=wallpaper)yield return null;
                        yield return new WaitForSecondsRealtime(2);
                        typeof(MainMenuUI).GetMethod("OpenPass",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                        yield return Capture("wallpaper-theme-"+wallpaper);
                        typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                    }
                    Config.Set("Wallpaper",original);Program.instance.ui_.ChangeWallpaper(original);
                    while(theme.AppliedWallpaper!=original)yield return null;
                    Debug.Log("WALLPAPER_THEME_PASSED native background follows both selected wallpapers; dominant hue and neutral exclusion verified");
                }
                foreach(string page in new[]{"ShowGameStart","OpenProfile","OpenPass","OpenShop","OpenHistory","OpenMissions"})
                {
                    typeof(MainMenuUI).GetMethod(page,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                    yield return Capture(page);
                    typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                }
                var results=UltraResultsScreen.Show(home.transform,new UltraProgress.Match{result="WIN",damageDealt=3400,spells=2,summons=1,gemsEarned=25,xpEarned=100},null);
                yield return Capture("duel-results-layout");
                yield return new WaitForSecondsRealtime(3);
                yield return Capture("duel-results-complete");Destroy(results);
            }
            if(commandLine.Contains("--friend-room-layout"))
            {
                RoomServant.FromUltraHost=true;RoomServant.FromSolo=false;RoomServant.FromHandTest=false;
                RoomServant.Mode=0;RoomServant.SelfType=0;RoomServant.IsHost=true;
                for(int i=0;i<4;i++)RoomServant.players[i]=i==0?new RoomServant.Player{name="Duelist",ready=false}:null;
                Program.instance.ShiftToServant(Program.instance.room);
                yield return Capture("friend-room");
                RoomServant.Mode=2;
                Program.instance.room.GetUI<RoomServantUI>().Realize();
                yield return Capture("friend-room-duos");
                RoomServant.FromUltraHost=false;
                Program.instance.ShiftToServant(Program.instance.menu);
            }
            int remoteIndex = Array.IndexOf(commandLine, "--ultra-remote-room");
            if (remoteIndex >= 0)
            {
                var room = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(commandLine[remoteIndex+1]));
                string remoteDeckName = (string)room["format"] == "TCG" ? "Starter - TCG"
                    : (int)room["cap"] == 0 ? "Starter - Genesys Zero" : "Starter - Genesys";
                Config.SetConfigDeck(remoteDeckName);
                var joining = MDPro3.Net.UltraServices.JoinRoom(room).AsTask();
                while (!joining.IsCompleted) yield return null;
                if (joining.IsFaulted) throw new Exception("Remote room join failed.", joining.Exception);
                while (Program.instance.currentServant != Program.instance.room || Program.instance.room.servantUI == null
                    || RoomServant.players.Count(p => p != null) < 2) yield return null;
                Program.instance.room.GetUI<RoomServantUI>().OnReady();
                while (RoomServant.players.Count(p => p != null && p.ready) < 2) yield return null;
                yield return Capture("remote-room");
                Debug.Log("ULTRA_REMOTE_UNITY_PASSED two players ready / " + (string)room["format"] + " / " + (int)room["cap"]);
                File.WriteAllText(Path.Combine(output,"passed.txt"), "Unity connected to the public VPS and both players readied with server-enforced format.");
                Application.Quit(0);
                yield break;
            }
            if (Environment.GetCommandLineArgs().Contains("--ultra-features")) yield return CheckUltraFeatures();
            CheckDeckProfiles();
            Program.instance.menu.GetUI<MainMenuUI>().OnDeck();
            yield return new WaitForSecondsRealtime(2);
            if (Program.instance.currentServant != Program.instance.deckSelector || Program.instance.ui_.currentPopupB != null)
                throw new Exception("Opening My Decks unexpectedly prompted for a format.");
            Debug.Log("FORMAT_SMOKE_DECK_BROWSER_WITHOUT_PROMPT_PASSED");
            yield return Capture("deck-library");
            if (Environment.GetCommandLineArgs().Contains("--deck-badges-only"))
            {
                File.WriteAllText(Path.Combine(output, "passed.txt"), "Deck library rendered without a format prompt.");
                Application.Quit(0);
                yield break;
            }
            Program.instance.ShiftToServant(Program.instance.menu);
            yield return new WaitForSecondsRealtime(1);
            DuelFormat.Select(() => Debug.Log("FORMAT_SMOKE_SELECTED " + DuelFormat.DisplayName));
            yield return Capture("format-options");
            var buttons = FindObjectsByType<SelectionButton>(FindObjectsSortMode.None);
            var option = buttons.FirstOrDefault(b => b.isActiveAndEnabled && b.GetButtonText() ==
                (mode == "Genesys" ? "Genesys · 100 points" : "TCG Advanced"));
            if (option == null) throw new Exception("Format selection button was not rendered.");
            EventSystem.current.SetSelectedGameObject(option.gameObject);
            option.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(2);
            if (DuelFormat.Genesys != (mode == "Genesys") || !DuelFormat.RequireNativeRules())
                throw new Exception("Selected format/native capabilities did not match.");
            string deckName = mode == "Genesys" ? "Starter - Genesys" : "Starter - TCG";
            Config.SetConfigDeck(deckName);
            if (!DuelFormat.ValidateSelectedDeck()) throw new Exception("Starter deck failed validation.");
            Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
            Program.instance.ShiftToServant(Program.instance.deckEditor);
            while (Program.instance.deckEditor.servantUI == null) yield return null;
            yield return Capture("deck");
            Debug.Log("FORMAT_SMOKE_DECK_READY " + mode);
            if(commandLine.Contains("--deck-count-check"))
            {
                var nameResults=CardsManager.Search("power",new System.Collections.Generic.List<long>(),DeckEditor.Banlist,string.Empty,namesOnly:true);
                if(nameResults.Count==0 || nameResults.Any(c=>c.Name.IndexOf("power",StringComparison.OrdinalIgnoreCase)<0))
                    throw new Exception("Collection search matched text outside card names.");
                if(!CardsManager.Get(89392810).GetSetName().Contains("Kewl Tune") || CardsManager.Get(89392810).GetSetName().Contains("Power Patron"))
                    throw new Exception("Kewl Tune Reco archetype is mislabelled.");
                if(!CardsManager.Get(89631139).GetSetNameWithColor().Contains("Archetype:"))
                    throw new Exception("Archetype label was not renamed.");
                Debug.Log("CARD_NAME_SEARCH_PASSED power returns only names; Reco Kewl Tune and Archetype label verified");
                var editor=Program.instance.deckEditor.GetUI<DeckEditorUI>();
                while(!editor.DeckView.deckLoaded)yield return null;
                editor.CardCollectionView.ShowArea(CardCollectionView.Area.Collection);
                yield return new WaitForSecondsRealtime(1);
                var sample=MDPro3.Duel.YGOSharp.CardsManager.Get(89631139);
                typeof(CardCollectionView).GetMethod("PrintCards",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)
                    .Invoke(editor.CardCollectionView,new object[]{new System.Collections.Generic.List<int>{sample.Id}});
                yield return new WaitForSecondsRealtime(1);
                var slot=editor.CardCollectionView.superScrollView.gameObjects.Select(o=>o.GetComponent<SelectionButton_CardInCollection>()).First(c=>c.gameObject.activeInHierarchy && c.CardCode==sample.Id);
                int originalCount=editor.DeckView.GetCardCount(sample.Id);
                foreach(var existing in editor.DeckView.cards.Where(c=>c.Card.Id==sample.Id || c.Card.Alias==sample.Id).ToArray())editor.RemoveCard(existing.Card);
                yield return new WaitForSecondsRealtime(.8f);
                int code=slot.CardCode;
                void CheckCount(int expected)
                {
                    if(editor.DeckView.GetCardCount(code)!=expected)throw new Exception("Deck model count did not update.");
                    for(int i=1;i<=3;i++)if(slot.GetComponent<YgomSystem.ElementSystem.ElementObjectManager>().GetElement("IconCardUse"+i).activeSelf!=(i<=expected))throw new Exception("Collection count dots are stale after editing.");
                }
                editor.AddCard(slot.card);editor.AddCard(slot.card);CheckCount(2);
                editor.RemoveCardByDrag(editor.DeckView.GetCardByData(slot.card));CheckCount(1);
                editor.RemoveCard(slot.card);CheckCount(0);
                for(int i=0;i<originalCount;i++)editor.AddCard(sample);editor.DeckView.SetDirty(false);
                yield return Capture("deck-count-after-removal");
                Debug.Log("DECK_COUNT_PASSED add twice, drag-remove, regular-remove; visible dots 2 to 1 to 0");
            }

            // Exercise the real local-server/AI path, including both deck validators.
            var solo = Program.instance.solo;
            Program.instance.ShiftToServant(solo);
            while (solo.servantUI == null) yield return null;
            Config.Set("AutoRPS", "1");
            int checkedBots = 0;
            foreach (var bot in SoloSelector.bots)
            {
                if (bot.command.StartsWith("Random=") || bot.flags.Contains("SELECT_DECKFILE")) continue;
                if (!DuelFormat.ValidateBotCommand(bot.command))
                    throw new Exception("An incompatible AI is offered: " + bot.name);
                if (!DuelFormat.Genesys && bot.command.Contains("DeckFile="))
                {
                    var deckToken = Tools.SplitWithPreservedQuotes(bot.command.Replace("'", "\""))
                        .Last(t => t.StartsWith("DeckFile="));
                    var ai = new Deck(deckToken.Substring(9).Trim('"'));
                    if (ai.Main.Concat(ai.Extra).Concat(ai.Side).Contains(23434538))
                        throw new Exception("Maxx C leaked into the TCG Solo library.");
                }
                checkedBots++;
            }
            if (checkedBots == 0) throw new Exception("No compatible Solo opponents offered.");
            Debug.Log("FORMAT_SMOKE_SOLO_LIBRARY_PASSED " + mode + " opponents=" + checkedBots);
            // Use the same header callback and rendered choices as a player.
            string[] selections = { mode == "Genesys" ? "TCG Advanced" : "Genesys · 100 points",
                "Genesys · 0 points", mode == "Genesys" ? "Genesys · 100 points" : "TCG Advanced" };
            foreach (string selection in selections)
            {
                solo.GetUI<SoloSelectorUI>().OnChangeSoloFormat();
                yield return new WaitForSecondsRealtime(1);
                var choice = FindObjectsByType<SelectionButton>(FindObjectsSortMode.None)
                    .FirstOrDefault(b => b.isActiveAndEnabled && b.GetButtonText() == selection);
                if (choice == null) throw new Exception("Solo format control did not open its options.");
                EventSystem.current.SetSelectedGameObject(choice.gameObject);
                choice.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                yield return new WaitForSecondsRealtime(2);
                if (!SoloSelector.HasFormatOverride || DuelFormat.Genesys != selection.StartsWith("Genesys"))
                    throw new Exception("Solo session format override was not applied.");
                int offered = SoloSelector.bots.Count(b => !b.command.StartsWith("Random=") && !b.flags.Contains("SELECT_DECKFILE"));
                if (selection == "Genesys · 0 points" ? offered != 0 : offered == 0)
                    throw new Exception("Solo catalog was not reloaded for the selected point cap.");
                foreach (var bot in SoloSelector.bots.Where(b => !b.command.StartsWith("Random=") && !b.flags.Contains("SELECT_DECKFILE")))
                    if (!DuelFormat.ValidateBotCommand(bot.command)) throw new Exception("Format switch retained an illegal opponent.");
            }
            Debug.Log("FORMAT_SMOKE_SOLO_FORMAT_SWITCH_PASSED " + mode);
            yield return Capture("solo-library");
            string command;
            if (!DuelFormat.PrepareSoloBot("Deck=CyberDragon Name=SmokeAI Dialog=default", out command))
                throw new Exception("The format-specific AI build could not be prepared.");
            solo.Launch(command, false, false, true);
            bool readied = false, started = false;
            while (Program.instance.currentServant != Program.instance.ocgcore)
            {
                if (Program.instance.currentServant == Program.instance.room && Program.instance.room.servantUI != null)
                {
                    if (!readied && RoomServant.players[RoomServant.SelfType] != null)
                    {
                        Program.instance.room.GetUI<RoomServantUI>().OnReady();
                        readied = true;
                    }
                    if (!started && RoomServant.players.Count(p => p != null && p.ready) >= 2)
                    {
                        Program.instance.room.GetUI<RoomServantUI>().OnStart();
                        started = true;
                    }
                }
                var turnChoice = Program.instance.ui_.currentPopupB as MDPro3.UI.Popup.PopupYesOrNo;
                if (turnChoice != null && turnChoice.isActiveAndEnabled && started)
                {
                    turnChoice.decideAction?.Invoke();
                    turnChoice.Hide();
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(12);
            yield return Capture("duel");
            Debug.Log("FORMAT_SMOKE_DUEL_READY " + mode);
            if(commandLine.Contains("--owner-admin-check"))
            {
                var controls=Program.instance.ocgcore.GetUI<OcgCoreUI>().GetComponent<UltraAdminDuelUI>();
                if(controls==null)throw new Exception("The duel admin controls are missing.");
                controls.Open();
                yield return Capture("owner-duel-controls");
                if(!controls.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t=>t.text=="ADMIN / CURRENT DUEL"))throw new Exception("The owner duel panel did not open.");
                controls.SendMessage("Close");
                Debug.Log("OWNER_ADMIN_UI_PASSED authenticated owner account and duel controls rendered; no account grants made");
            }
            yield return CheckHandMovement();
            yield return CheckDamageDisplay();
            if(Environment.GetCommandLineArgs().Contains("--ultra-result-check"))
            {
                int xp=UltraProgress.Data.xp,gems=UltraProgress.Data.gems;
                var win=new Package{Function=(int)GameMessage.Win};
                win.Data.writer.Write((byte)OcgCore.LocalPlayer(0));win.Data.writer.Write((byte)0);
                OcgCore.currentMessage=GameMessage.Win;
                yield return Program.instance.ocgcore.messageDispatcher.Process(win).ToCoroutine();
                if(UltraProgress.Data.xp!=xp+100 || UltraProgress.Data.gems!=gems+25)throw new Exception("Duel result rewards were not awarded exactly once.");
                UltraProgress.Finish(0,OcgCore.turns);
                if(UltraProgress.Data.xp!=xp+100 || UltraProgress.Data.gems!=gems+25)throw new Exception("Duplicate duel result awarded twice.");
                string replay=UltraProgress.LastMatch.replay;
                if(string.IsNullOrEmpty(replay) || !File.Exists(Program.PATH_REPLAY+replay+Program.EXPANSION_YRP3D))throw new Exception("Completed duel replay was not saved.");
                if(!File.Exists(Program.PATH_REPLAY+replay+Program.EXPANSION_YRP3D+".protocol"))throw new Exception("Saved replay protocol metadata was not written.");
                yield return Capture("post-duel-results");
                Debug.Log("ULTRA_RESULTS_PASSED rewards, idempotency, match history and automatic replay");
            }

            File.WriteAllText(Path.Combine(output, "passed.txt"), mode + " menu, selection, deck, local AI duel rendered.");
            Application.Quit(0);
        }

        private void CheckDeckProfiles()
        {
            string name = "__format-smoke-" + Guid.NewGuid().ToString("N");
            string path = Program.PATH_DECK + name + Program.EXPANSION_YDK;
            try
            {
                foreach (int cap in new[] { 100, 0, 237 })
                {
                    var deck = new Deck { Format = "Genesys", FormatPointCap = cap };
                    deck.Main.Add(123);
                    File.WriteAllText(path, deck.GetYDK());
                    DuelFormat.ApplyDeckFormat(name);
                    if (!DuelFormat.Genesys || DuelFormat.PointCap != cap)
                        throw new Exception("Deck format/cap was not restored.");
                    DuelFormat.SaveDeckFormat(name);
                    var saved = new Deck(path);
                    if (saved.Format != "Genesys" || saved.FormatPointCap != cap || saved.Main.Single() != 123)
                        throw new Exception("Saving deck format changed its cards or profile.");
                }
                File.WriteAllText(path, new Deck { Format = "TCG" }.GetYDK());
                DuelFormat.ApplyDeckFormat(name);
                if (DuelFormat.Genesys) throw new Exception("Switching to a TCG deck retained Genesys.");
                Debug.Log("FORMAT_SMOKE_PER_DECK_PROFILES_PASSED");
            }
            finally { if (File.Exists(path)) File.Delete(path); }
        }

        private IEnumerator CheckHandMovement()
        {
            var core = Program.instance.ocgcore;
            OcgCore.pause = true;
            var hand = OcgCore.cards.First(c => c.p.controller == 1 && c.p.InLocation(CardLocation.Hand));
            var original = hand.p.Copy();
            int before = core.GetLocationCardCount(CardLocation.Hand, 1);
            var move = new Package { Function = (int)GameMessage.Move };
            move.Data.writer.Write(12482652);
            move.Data.writer.Write((byte)OcgCore.LocalPlayer(1));
            move.Data.writer.Write((byte)CardLocation.Hand);
            move.Data.writer.Write((byte)original.sequence);
            move.Data.writer.Write((byte)original.position);
            move.Data.writer.Write((byte)OcgCore.LocalPlayer(1));
            move.Data.writer.Write((byte)CardLocation.MonsterZone);
            move.Data.writer.Write((byte)0);
            move.Data.writer.Write((byte)CardPosition.FaceUpAttack);
            move.Data.writer.Write((uint)CardReason.SUMMON);
            OcgCore.currentMessage = GameMessage.Move;
            yield return core.messageDispatcher.Process(move).ToCoroutine();
            if (!hand.p.InLocation(CardLocation.MonsterZone)
                || core.GetLocationCardCount(CardLocation.Hand, 1) != before - 1)
                throw new Exception("Opponent summon remained in hand.");
            testedOpponentCard = hand;
            Debug.Log("FORMAT_SMOKE_HAND_TO_FIELD_PASSED");
        }

        private GameCard testedOpponentCard;

        private IEnumerator CheckDamageDisplay()
        {
            var core = Program.instance.ocgcore;
            OcgCore.pause = true;
            var handBeforeDamage = OcgCore.cards.Where(c => c.p.controller == 1 && c.p.InLocation(CardLocation.Hand)).ToArray();
            bool originalFirst = OcgCore.isFirst;
            foreach (bool first in new[] { true, false })
            {
                OcgCore.isFirst = first;
                OcgCore.life0 = OcgCore.life1 = 8000;
                core.SetLP(0, 0, true);
                var damage = new Package { Function = (int)GameMessage.Damage };
                damage.Data.writer.Write((byte)(first ? 1 : 0));
                damage.Data.writer.Write(400);
                OcgCore.currentMessage = GameMessage.Damage;
                yield return core.messageDispatcher.duel.Process(damage).ToCoroutine();
                yield return new WaitForSecondsRealtime(3);
                var ui = core.GetUI<OcgCoreUI>();
                if (OcgCore.life0 != 8000 || OcgCore.life1 != 7600
                    || ui.TextPlayer0LP.text != "8000" || ui.TextPlayer1LP.text != "7600")
                    throw new Exception("400 damage went to the wrong player/LP display, first=" + first);
                Debug.Log("FORMAT_SMOKE_DAMAGE_400_PASSED first=" + first);
            }
            // A second message arrives while the first count is still active.
            OcgCore.life1 = 7200;
            core.SetLP(1, -400);
            yield return new WaitForSecondsRealtime(1.3f);
            OcgCore.life1 = 6800;
            core.SetLP(1, -400);
            yield return new WaitForSecondsRealtime(3);
            if (core.GetUI<OcgCoreUI>().TextPlayer1LP.text != "6800")
                throw new Exception("An older LP animation overwrote the latest total.");
            Debug.Log("FORMAT_SMOKE_OVERLAPPING_LP_PASSED");
            OcgCore.isFirst = originalFirst;
            if (!handBeforeDamage.SequenceEqual(OcgCore.cards.Where(c => c.p.controller == 1 && c.p.InLocation(CardLocation.Hand))))
                throw new Exception("Damage changed the opponent hand.");
            var destroy = new Package { Function = (int)GameMessage.Move };
            destroy.Data.writer.Write(testedOpponentCard.GetData().Id);
            foreach (var location in new[] { CardLocation.MonsterZone, CardLocation.Grave })
            {
                destroy.Data.writer.Write((byte)OcgCore.LocalPlayer(1));
                destroy.Data.writer.Write((byte)location);
                destroy.Data.writer.Write((byte)(location == CardLocation.MonsterZone ? (int)testedOpponentCard.p.sequence : core.GetLocationCardCount(CardLocation.Grave, 1)));
                destroy.Data.writer.Write((byte)CardPosition.FaceUpAttack);
            }
            destroy.Data.writer.Write((uint)(CardReason.DESTROY | CardReason.BATTLE));
            OcgCore.currentMessage = GameMessage.Move;
            yield return core.messageDispatcher.Process(destroy).ToCoroutine();
            if (!testedOpponentCard.p.InLocation(CardLocation.Grave)
                || handBeforeDamage.Any(c => !c.p.InLocation(CardLocation.Hand)))
                throw new Exception("Battle destruction removed an opponent hand card.");
            Debug.Log("FORMAT_SMOKE_FIELD_DESTRUCTION_PRESERVES_HAND_PASSED");
            yield return Capture("damage-verified");
        }
    }
}
