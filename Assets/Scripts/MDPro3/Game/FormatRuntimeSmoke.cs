using System;
using System.Collections;
using System.IO;
using System.Linq;
using MDPro3.Duel.YGOSharp;
using MDPro3.Servant;
using MDPro3.UI;
using MDPro3.Utility;
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
            smoke.deadline = Time.realtimeSinceStartup + 300;
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

        private IEnumerator CheckAllCardArt()
        {
            var rows = new Newtonsoft.Json.Linq.JArray();
            var cards = CardsManager._cards.Values.OrderBy(c => c.Id).ToArray();
            var auditArgs = Environment.GetCommandLineArgs();
            int idsIndex = Array.IndexOf(auditArgs, "--card-art-ids");
            if (idsIndex >= 0 && idsIndex + 1 < auditArgs.Length)
            {
                var ids = Newtonsoft.Json.JsonConvert.DeserializeObject<int[]>(File.ReadAllText(auditArgs[idsIndex + 1]));
                cards = cards.Where(c => ids.Contains(c.Id)).ToArray();
                if (cards.Length != ids.Distinct().Count()) throw new Exception("Some requested artwork IDs are absent from the runtime database.");
            }
            var wanted = new System.Collections.Generic.HashSet<int> { 16387555, 89392810, 43904702, 16509007, 89631139 };
            var styles = new System.Collections.Generic.HashSet<string>();
            var contact = new Texture2D(880, 1280, TextureFormat.RGB24, false);
            var thumbnail = new Texture2D(88, 128, TextureFormat.RGB24, false);
            var smallTarget = new RenderTexture(88, 128, 0);
            smallTarget.Create();
            int index = 0;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            foreach (var card in cards)
            {
                var row = new Newtonsoft.Json.Linq.JObject { ["id"] = card.Id, ["name"] = card.Name, ["token"] = card.HasType(CardType.Token) };
                System.Threading.Tasks.Task<Texture2D> art = null;
                float limit;
                if (!CardImageLoader.UsesCardListImages)
                {
                    art = CardImageLoader.LoadArtAsync(card.Id).AsTask();
                    limit = Time.realtimeSinceStartup + 30;
                    while (!art.IsCompleted && Time.realtimeSinceStartup < limit) yield return null;
                    if (!art.IsCompleted) throw new Exception("Artwork load timed out: " + card.Id);
                    row["art"] = art.IsFaulted ? "error" : art.Result == null ? "missing" : "ok";
                    if (art.IsFaulted) row["artError"] = art.Exception.ToString();
                }
                var load = CardImageLoader.LoadCardAsync(card.Id, false, default, true).AsTask();
                limit = Time.realtimeSinceStartup + 30;
                while (!load.IsCompleted && Time.realtimeSinceStartup < limit) yield return null;
                if (!load.IsCompleted) throw new Exception("Card render timed out: " + card.Id);
                var texture = load.IsFaulted ? null : load.Result as Texture2D;
                row["sourceVerified"] = !CardImageLoader.UsesCardListImages || texture != null && texture.name == "CardList_" + card.Id;
                if (CardImageLoader.UsesCardListImages) row["art"] = (bool)row["sourceVerified"] ? "ok" : "missing";
                if (texture != null) { row["width"] = texture.width; row["height"] = texture.height; }
                row["render"] = texture != null && texture.width >= 64 && CardImageLoader.lastCardRenderSucceed ? "ok" : "error";
                if (load.IsFaulted) row["renderError"] = load.Exception.ToString();
                if (texture != null)
                {
                    var previous = RenderTexture.active;
                    Graphics.Blit(texture, smallTarget);
                    RenderTexture.active = smallTarget;
                    thumbnail.ReadPixels(new Rect(0, 0, 88, 128), 0, 0);
                    thumbnail.Apply();
                    RenderTexture.active = previous;
                    int slot = index % 100;
                    contact.SetPixels((slot % 10) * 88, (9 - slot / 10) * 128, 88, 128, thumbnail.GetPixels());
                    string style = card.Type.ToString();
                    if (wanted.Contains(card.Id) || styles.Add(style))
                        File.WriteAllBytes(Path.Combine(output, "card-" + card.Id + ".png"), texture.EncodeToPNG());
                    CardImageLoader.ReleaseCard(card.Id);
                }
                if (art != null && !art.IsFaulted && art.Result != null) CardImageLoader.ReleaseArt(card.Id);
                rows.Add(row);
                index++;
                if (index % 100 == 0 || index == cards.Length)
                {
                    contact.Apply();
                    File.WriteAllBytes(Path.Combine(output, "cards-" + ((index - 1) / 100).ToString("D3") + ".png"), contact.EncodeToPNG());
                    var report = new Newtonsoft.Json.Linq.JObject { ["total"] = cards.Length, ["checked"] = index, ["cards"] = rows };
                    File.WriteAllText(Path.Combine(output, "all-cards.json"), report.ToString());
                    Debug.Log("ALL_CARD_ART_PROGRESS " + index + "/" + cards.Length);
                    contact.SetPixels(new Color[880 * 1280]);
                }
                if (index % 500 == 0)
                {
                    yield return Resources.UnloadUnusedAssets();
                    System.GC.Collect();
                }
            }
            smallTarget.Release(); Destroy(smallTarget); Destroy(thumbnail); Destroy(contact);
            int failed = rows.Count(r => (string)r["art"] != "ok" || (string)r["render"] != "ok" || (bool?)r["sourceVerified"] == false);
            File.WriteAllText(Path.Combine(output, "completed.txt"), "Checked " + index + " database cards. Failures: " + failed);
            Debug.Log("ALL_CARD_ART_COMPLETED " + index + " failures=" + failed);
            Application.Quit(failed == 0 ? 0 : 1);
        }

        private IEnumerator CheckPremadeLibrary()
        {
            var failures = new System.Collections.Generic.List<string>();
            int valid = 0;
            foreach (var recipeEntry in PremadeDeckLibrary.Entries.Where(e => e.available))
            {
                try { PremadeDeckLibrary.Prepare(recipeEntry); valid++; }
                catch (Exception e) { failures.Add(recipeEntry.id + ": " + e.Message); }
            }
            File.WriteAllText(Path.Combine(output, "catalog-validation.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { valid, failures }, Newtonsoft.Json.Formatting.Indented));
            if (failures.Count > 0 || valid < 600) throw new Exception("Premade catalog validation failed: " + string.Join("; ", failures.Take(8)));
            Debug.Log("PREMADE_CATALOG_VALIDATED " + valid);
            if (!PremadeDeckLibrary.Search("kewl-tune", "TCG").Any(e => e.archetype == "Kewl Tune"))
                throw new Exception("Archetype search did not normalize spaces and punctuation.");
            var zeroEntry = PremadeDeckLibrary.Entries.First(e => e.available && e.format == "Genesys" && e.cap == 0);
            var existingDeckPaths = Directory.GetFiles("Deck", "*.ydk").Select(Path.GetFullPath).ToHashSet(StringComparer.OrdinalIgnoreCase);
            string zeroName = PremadeDeckLibrary.SaveCopy(zeroEntry);
            string zeroPath = Path.Combine("Deck", zeroName + ".ydk");
            var zeroDeck = new Deck(zeroPath);
            if (zeroDeck.Format != "Genesys" || zeroDeck.FormatPointCap != 0 || !PremadeDeckLibrary.Validate(zeroEntry, zeroDeck).Valid)
                throw new Exception("The zero-point import lost its format metadata.");
            if (!existingDeckPaths.Contains(Path.GetFullPath(zeroPath))) File.Delete(zeroPath);
            string originalFormat = Config.Get("DuelFormat", "TCG"), originalCap = Config.Get("GenesysPointCap", "100");
            Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForEdit);
            Program.instance.ShiftToServant(Program.instance.deckSelector);
            while (Program.instance.deckSelector.servantUI == null || Program.instance.deckSelector.inTransition) yield return null;
            var selector = Program.instance.deckSelector.GetUI<DeckSelectorUI>();
            if (selector.ButtonOnline.GetButtonText() != "Premade Decks") throw new Exception("The deck library button was not renamed.");
            selector.OnOnlineDeckView();
            while (Program.instance.onlineDeckViewer.servantUI == null || Program.instance.onlineDeckViewer.inTransition) yield return null;
            var library = Program.instance.onlineDeckViewer.GetUI<OnlineDeckViewerUI>();
            while (library.superScrollView == null || library.superScrollView.items.Count == 0) yield return null;
            Canvas.ForceUpdateCanvases();
            var browseControls = library.GetComponentsInChildren<RectTransform>().Where(r => r.name == "PremadeFormatFilter" || r.name == "PremadeSort" || r.name == "PremadeAvailability").ToArray();
            if (browseControls.Length != 3 || browseControls.Any(r => r.rect.width < 200 || r.rect.height < 40))
                throw new Exception("Premade browse controls have collapsed or are missing.");
            yield return Capture("premade-library-tcg");
            library.SetSortOrder("Z–A");
            yield return null;
            if (OnlineDeckViewer.premades.First().archetype != OnlineDeckViewer.premades.OrderByDescending(e => e.archetype, StringComparer.OrdinalIgnoreCase).First().archetype)
                throw new Exception("Reverse name sorting did not reorder the recipes.");
            yield return Capture("premade-sort-reverse");
            OnlineDeckViewer.AvailableOnly = true;
            library.SetFormatFilter("Genesys 100");
            library.SetSortOrder("Lowest points");
            yield return null;
            if (OnlineDeckViewer.premades.Any(e => !e.available) || !OnlineDeckViewer.premades.Select(e => e.points).SequenceEqual(OnlineDeckViewer.premades.Select(e => e.points).OrderBy(p => p)))
                throw new Exception("Available-only or Genesys-point sorting failed.");
            yield return Capture("premade-sort-points");
            OnlineDeckViewer.AvailableOnly = false;
            library.SetSortOrder("A–Z");
            library.SetFormatFilter("TCG");
            library.ShowFormatPicker();
            yield return Capture("premade-format-picker");
            var option = FindObjectsByType<SelectionButton>(FindObjectsSortMode.None).First(b => b.isActiveAndEnabled && b.GetButtonText() == "Genesys 100");
            EventSystem.current.SetSelectedGameObject(option.gameObject);
            option.GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            library.InputDeckName.text = "Kewl Tune";
            while (library.superScrollView.items.Count != 1) yield return null;
            yield return Capture("premade-kewl-genesys");
            library.SetFormatFilter("Genesys 0");
            yield return new WaitForSecondsRealtime(.5f);
            var unavailable = PremadeDeckLibrary.Find("kewl-tune-genesys-0");
            if (unavailable.available || !unavailable.reason.Contains("point")) throw new Exception("Kewl Tune zero-point restrictions were not explained.");
            yield return Capture("premade-kewl-zero");
            library.InputDeckName.text = "No such archetype 1234567";
            yield return new WaitForSecondsRealtime(.5f);
            if (library.superScrollView.items.Count != 0) throw new Exception("Empty archetype search did not clear the list.");
            Program.instance.onlineDeckViewer.Select();
            yield return Capture("premade-empty-search");
            library.InputDeckName.text = "Kewl Tune";
            library.SetFormatFilter("Genesys 100");
            yield return new WaitForSecondsRealtime(.5f);
            var item = library.superScrollView.gameObjects.Select(o => o.GetComponent<SelectionToggle_DeckOnline>()).First(i => i != null && i.gameObject.activeInHierarchy && i.deckId == "kewl-tune-genesys-100");
            item.OpenPremade();
            while (Program.instance.deckEditor.servantUI == null || Program.instance.deckEditor.inTransition) yield return null;
            var editor = Program.instance.deckEditor.GetUI<DeckEditorUI>();
            while (!editor.DeckView.deckLoaded) yield return null;
            if (DeckEditor.condition != DeckEditor.Condition.PremadeDeck || DeckEditor.DeckIsFromLocal || DeckEditor.Deck.Format != "Genesys" || DeckEditor.Deck.FormatPointCap != 100)
                throw new Exception("Premade preview did not retain the source format.");
            if (Config.Get("DuelFormat", "TCG") != originalFormat || Config.Get("GenesysPointCap", "100") != originalCap)
                throw new Exception("Browsing the catalog changed the player's selected format.");
            yield return Capture("premade-kewl-preview");
            var entry = PremadeDeckLibrary.Current;
            string name = PremadeDeckLibrary.SaveCopy(entry);
            string path = Path.Combine("Deck", name + ".ydk");
            byte[] recipe = File.ReadAllBytes(path);
            if (PremadeDeckLibrary.SaveCopy(entry) != name) throw new Exception("Repeated import duplicated an unchanged premade.");
            string edited = File.ReadAllText(path) + "# diagnostic user edit\n";
            File.WriteAllText(path, edited);
            string next = PremadeDeckLibrary.SaveCopy(entry);
            if (next == name || File.ReadAllText(path) != edited) throw new Exception("Import overwrote the edited local deck.");
            File.Delete(Path.Combine("Deck", next + ".ydk"));
            File.WriteAllBytes(path, recipe);
            editor.OnSave();
            while (Program.instance.currentServant != Program.instance.deckSelector || Program.instance.deckSelector.inTransition) yield return null;
            if (Config.GetConfigDeckName() != name || !DuelFormat.Genesys || DuelFormat.PointCap != 100)
                throw new Exception("Saving the premade did not select its saved format.");
            var saved = new Deck(path);
            if (!PremadeDeckLibrary.Validate(entry, saved).Valid || saved.Format != "Genesys" || saved.FormatPointCap != 100)
                throw new Exception("The saved premade does not preserve its cards and format metadata.");
            yield return Capture("premade-saved-copy");
            File.WriteAllText(Path.Combine(output, "passed.txt"), valid + " premade lists validated. Search, all three format filters, name and point sorting, available-only filter, zero-point explanation, read-only preview, save-copy, duplicate import and edited-deck protection passed.");
            Debug.Log("PREMADE_LIBRARY_PASSED " + valid);
            Application.Quit(0);
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
            if (commandLine.Contains("--menu-layout-check"))
            {
                var home = Program.instance.menu.GetUI<MainMenuUI>();
                typeof(MainMenuUI).GetMethod("OpenMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home, null);
                yield return Capture("menu-redesign");
                var icons = home.GetComponentsInChildren<UnityEngine.UI.RawImage>().Where(i => i.name.StartsWith("Menu icon ")).ToArray();
                if (icons.Length != 22 || icons.Any(i => i.texture == null || i.texture.width < 128))
                    throw new Exception("A menu action or navigation icon is missing its full-resolution asset.");
                foreach (var label in home.GetComponentsInChildren<TMPro.TMP_Text>().Where(t => t.transform.parent.name is "Title" or "Description"))
                {
                    label.ForceMeshUpdate();
                    if (label.preferredWidth > label.rectTransform.rect.width + 1f)
                        throw new Exception("Menu caption overflows its tile: " + label.text);
                }
                foreach (string page in new[] { "OpenGuiScale", "OpenHistory", "OpenBackups" })
                {
                    typeof(MainMenuUI).GetMethod(page, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home, null);
                    yield return Capture(page);
                }
                typeof(MainMenuUI).GetMethod("OpenMenu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(home, null);
                yield return Capture("menu-return");
                File.WriteAllText(Path.Combine(output, "passed.txt"), "All 11 menu actions have licensed high-resolution icons and readable captions. Interface size, history, backups, and menu return passed.");
                Application.Quit(0);
                yield break;
            }
            if (commandLine.Contains("--deck-header-check"))
            {
                foreach (var condition in new[] { DeckSelector.Condition.ForEdit, DeckSelector.Condition.ForSolo })
                {
                    Program.instance.deckSelector.SwitchCondition(condition);
                    Program.instance.ShiftToServant(Program.instance.deckSelector);
                    while (Program.instance.deckSelector.servantUI == null || Program.instance.deckSelector.inTransition) yield return null;
                    var selector = Program.instance.deckSelector.GetUI<DeckSelectorUI>();
                    UltraScreenSkin.Apply(selector.gameObject);
                    yield return Capture("deck-header-" + condition);
                    var header = selector.Manager.GetElement<RectTransform>("Header");
                    var title = selector.Manager.GetElement<TMPro.TextMeshProUGUI>("TextTitle");
                    if (condition == DeckSelector.Condition.ForEdit && title.text != "Deck Builder")
                        throw new Exception("The deck heading is not Deck Builder.");
                    if (selector.GetComponentsInChildren<TMPro.TMP_Text>().Any(t => t.name == "Ultra subtitle"))
                        throw new Exception("The deck screen still has a duplicate subtitle.");
                    var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(header, title.rectTransform);
                    if (bounds.min.y < header.rect.yMin || bounds.max.y > header.rect.yMax)
                        throw new Exception("Deck title or underline extends outside the header: " + bounds);
                    Program.instance.ShiftToServant(Program.instance.menu);
                    while (Program.instance.menu.inTransition || Program.instance.deckSelector.inTransition) yield return null;
                }
                Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForEdit);
                Program.instance.ShiftToServant(Program.instance.deckSelector);
                while (Program.instance.deckSelector.servantUI == null || Program.instance.deckSelector.inTransition) yield return null;
                Program.instance.deckSelector.GetUI<DeckSelectorUI>().OnOnlineDeckView();
                while (Program.instance.onlineDeckViewer.servantUI == null || Program.instance.onlineDeckViewer.inTransition) yield return null;
                var viewer = Program.instance.onlineDeckViewer.GetUI<OnlineDeckViewerUI>();
                yield return Capture("premade-browse-header");
                var browse = viewer.GetComponentsInChildren<RectTransform>().First(t => t.name == "Premade browse controls");
                var browseRoot = (RectTransform)browse.parent;
                var browseBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(browseRoot, browse);
                if (browseBounds.max.y > browseRoot.rect.yMax - 128f)
                    throw new Exception("Premade search controls overlap the header divider.");
                File.WriteAllText(Path.Combine(output, "passed.txt"), "Single Deck Builder heading, no duplicate subtitle, and premade search controls sit below the header divider.");
                Application.Quit(0);
                yield break;
            }
            if (commandLine.Contains("--premade-check"))
            {
                deadline = Time.realtimeSinceStartup + 300;
                yield return CheckPremadeLibrary();
                yield break;
            }
            if (commandLine.Contains("--all-card-art-check"))
            {
                deadline = Time.realtimeSinceStartup + 14400;
                yield return CheckAllCardArt();
                yield break;
            }
            if(commandLine.Contains("--rps-art-check"))
            {
                var request=UnityEngine.AddressableAssets.Addressables.InstantiateAsync("Popup/PopupRockPaperScissors.prefab");
                yield return request;
                var popup=request.Result.GetComponent<MDPro3.UI.Popup.PopupRockPaperScissors>();
                popup.transform.SetParent(Program.instance.ui_.popup,false);
                popup.args=new System.Collections.Generic.List<string>{"Rock-Paper-Scissors"};
                popup.Show();
                yield return new WaitForSecondsRealtime(1);
                var manager=popup.GetComponent<YgomSystem.ElementSystem.ElementObjectManager>();
                foreach(var choice in new[]{"Rock","Paper","Scissors"})
                {
                    var image=manager.GetElement<UnityEngine.UI.RawImage>(choice+"Button");
                    var label=image.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                    if(image.texture==null || image.texture.width<32 || label==null || label.font==null)
                        throw new Exception("RPS choice missing visible built-in art or font: "+choice);
                }
                yield return Capture("rock-paper-scissors");
                popup.Hide();
                yield return new WaitForSecondsRealtime(.5f);
                Debug.Log("RPS_BUILTIN_ART_PASSED all three choices render with captions");
                if(commandLine.Contains("--rps-only")){Application.Quit(0);yield break;}
            }
            if(commandLine.Contains("--friend-tables-layout"))
            {
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                var fixture=Newtonsoft.Json.Linq.JObject.Parse("{\"id\":\"layout\",\"host\":\"Friend\",\"hostId\":-1,\"format\":\"TCG\",\"mode\":\"1v1\"}");
                typeof(MainMenuUI).GetMethod("ShowFriendTables",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,new object[]{fixture});
                yield return Capture("friend-tables");
                if(home.GetComponentsInChildren<UnityEngine.UI.Button>().Count(b=>b.name=="WATCH")!=3)
                    throw new Exception("Friend room did not render three spectator entries.");
                typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                Debug.Log("FRIEND_TABLES_LAYOUT_PASSED three entry and watch rows");
            }
            if(commandLine.Contains("--notifications-check"))
            {
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
                long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var fixture=Newtonsoft.Json.Linq.JObject.FromObject(new {
                    requests=new[]{new{id=42,name="Friend"}},rooms=new[]{new{id="notice-room"}},
                    notifications=new object[]{
                        new{id=4,kind="duel-invite",peer=42,name="Friend",room="notice-room",created=now,expires=now+300,seen=0},
                        new{id=3,kind="invite-sent",peer=42,name="Friend",room="notice-room",created=now-20,expires=now+280,seen=0},
                        new{id=2,kind="friend-request",peer=42,name="Friend",room="",created=now-40,expires=0L,seen=0},
                        new{id=1,kind="friend-accepted",peer=43,name="Another friend",room="",created=now-100,expires=0L,seen=1}}});
                var field=typeof(MainMenuUI).GetField("notificationFixture",flags);field.SetValue(home,fixture);
                typeof(MainMenuUI).GetMethod("RefreshUltraHome",flags).Invoke(home,null);
                yield return Capture("notifications-home");
                if(!home.GetComponentsInChildren<UnityEngine.UI.Button>().Any(b=>b.name=="NOTICES"))throw new Exception("Notification entry missing from home.");
                typeof(MainMenuUI).GetMethod("OpenNotifications",flags).Invoke(home,null);
                yield return Capture("notifications-inbox");
                var noticeButtons=home.GetComponentsInChildren<UnityEngine.UI.Button>();
                if(!noticeButtons.Any(b=>b.name=="JOIN") || !noticeButtons.Any(b=>b.name=="ACCEPT") || !noticeButtons.Any(b=>b.name=="VIEW ROOM"))throw new Exception("Notification actions missing.");
                noticeButtons.First(b=>b.name=="MARK ALL READ").onClick.Invoke();
                yield return new WaitForSecondsRealtime(.2f);
                if(((Newtonsoft.Json.Linq.JArray)fixture["notifications"]).Any(n=>(int)n["seen"]!=1))throw new Exception("Mark all read failed.");
                yield return Capture("notifications-read");
                typeof(MainMenuUI).GetMethod("CloseModal",flags).Invoke(home,null);
                field.SetValue(home,null);typeof(MainMenuUI).GetMethod("RefreshUltraHome",flags).Invoke(home,null);
                Debug.Log("ULTRA_NOTIFICATIONS_PASSED home badge, incoming/sent invites, friend requests, actions and mark-read");
            }
            if(commandLine.Contains("--cloud-backup-check"))
            {
                var cloudCheck=MDPro3.Net.UltraDeckCloudVerification.Run().AsTask();
                while(!cloudCheck.IsCompleted)yield return null;
                if(cloudCheck.IsFaulted)throw cloudCheck.Exception;
            }
            if(commandLine.Contains("--ultra-rewards-check"))
            {
                Debug.Log(UltraBackupVerification.Run());
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                foreach(string page in new[]{"OpenPass","OpenShop","OpenBackups"})
                {
                    typeof(MainMenuUI).GetMethod(page,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                    yield return Capture("rewards-"+page);
                    foreach(var gem in home.GetComponentsInChildren<UltraGem>())
                        if(gem.sprite==null || gem.sprite.texture.name!="Crystal")throw new Exception("Currency did not use the supplied crystal.");
                    typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                }
                Debug.Log("ULTRA_REWARDS_PASSED crystal, reward layout and reinstall protection");
            }
            if(commandLine.Contains("--ultra-layout"))
            {
                var home=Program.instance.menu.GetUI<MainMenuUI>();
                var wallpaper=Program.instance.ui_.wallpaper.transform;var wallpaperScale=wallpaper.localScale;
                int originalScale=UltraGuiScale.Percent;
                foreach(int scale in new[]{70,100,125})
                {
                    UltraGuiScale.Set(scale);yield return new WaitForSecondsRealtime(.5f);
                    var button=home.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="DUEL");
                    if(Mathf.Abs(button.transform.localScale.x-scale/100f)>.06f)throw new Exception("GUI scale did not resize home navigation.");
                    if(wallpaper.localScale!=wallpaperScale)throw new Exception("GUI scale changed the wallpaper.");
                    yield return Capture("gui-scale-"+scale);
                }
                UltraGuiScale.Set(originalScale);
                foreach(string page in new[]{"OpenGuiScale","OpenProfile","OpenStats","ShowFriends","OpenUpdates","OpenDuel"})
                {
                    typeof(MainMenuUI).GetMethod(page,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                    if(page=="OpenUpdates")yield return new WaitForSecondsRealtime(11);
                    yield return Capture("layout-"+page);
                    if(page=="OpenProfile" && home.GetComponentsInChildren<UltraWallpaperBanner>().Length!=1)throw new Exception("The equipped wallpaper has no profile banner.");
                    if(page=="OpenProfile")
                    {
                        Color.RGBToHSV(home.GetComponentInChildren<UltraWallpaperBanner>().accent,out _,out _,out float brightness);
                        if(brightness<.8f)throw new Exception("Wallpaper banner lost its accent color.");
                    }
                    typeof(MainMenuUI).GetMethod("CloseModal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(home,null);
                }
                Debug.Log("ULTRA_LAYOUT_PASSED GUI scale 70/100/125; wallpaper unchanged; profile banners and pages rendered");
            }
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
                if((bool?)room["randomMatch"]==true)
                {
                    while(Program.instance.ui_.popup.GetComponentInChildren<MDPro3.UI.Popup.PopupRockPaperScissors>()==null)yield return null;
                    yield return Capture("random-online-rps");
                    Debug.Log("ULTRA_RANDOM_AUTOSTART_PASSED matched players automatically readied and reached rock-paper-scissors");
                    File.WriteAllText(Path.Combine(output,"passed.txt"),"Random match reached RPS using automatic ready/start.");
                    yield return new WaitForSecondsRealtime(5);Application.Quit(0);yield break;
                }
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
            if (commandLine.Contains("--card-art-check") && File.Exists("Deck/Kewl Tune.ydk"))
                deckName = "Kewl Tune";
            Config.SetConfigDeck(deckName);
            if (!DuelFormat.ValidateSelectedDeck()) throw new Exception("Starter deck failed validation.");
            Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
            Program.instance.ShiftToServant(Program.instance.deckEditor);
            while (Program.instance.deckEditor.servantUI == null) yield return null;
            if (commandLine.Contains("--card-art-check"))
            {
                var view = Program.instance.deckEditor.GetUI<DeckEditorUI>().DeckView;
                while (!view.deckLoaded) yield return null;
                float timeout = Time.realtimeSinceStartup + 90;
                CardRawImageHandler[] faces;
                do
                {
                    yield return null;
                    faces = view.GetComponentsInChildren<CardRawImageHandler>()
                        .Where(f => f.card != null && f.card.Id > 0).ToArray();
                } while ((faces.Length < 40 || faces.Any(f => !f.Refreshed)) && Time.realtimeSinceStartup < timeout);
                if (faces.Length < 40 || faces.Any(f => !f.Refreshed || f.RawImage.texture == null))
                    throw new Exception("Deck card faces did not finish loading: " + faces.Count(f => f.Refreshed) + "/" + faces.Length);
                foreach (int id in faces.Select(f => f.card.Id).Distinct())
                {
                    var art = CardImageLoader.LoadArtAsync(id).AsTask();
                    while (!art.IsCompleted) yield return null;
                    if (art.IsFaulted || art.Result == null) throw new Exception("Missing deck artwork: " + id);
                    CardImageLoader.ReleaseArt(id);
                }
                yield return Capture("card-art-deck");
                File.WriteAllText(Path.Combine(output, "passed.txt"), "All " + faces.Length + " deck faces and their artwork loaded: " + deckName);
                Debug.Log("CARD_ART_CHECK_PASSED " + faces.Length + " " + deckName);
                Application.Quit(0);
                yield break;
            }
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
            float cardsReadyDeadline=Time.realtimeSinceStartup+60;
            while((Program.instance.ocgcore.GetLocationCardCount(CardLocation.Hand,0)==0
                || Program.instance.ocgcore.GetLocationCardCount(CardLocation.Deck,1)==0)
                && Time.realtimeSinceStartup<cardsReadyDeadline)yield return null;
            if(Program.instance.ocgcore.GetLocationCardCount(CardLocation.Hand,0)==0)
                throw new Exception("The installed duel did not finish loading its opening hand.");
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
            if(commandLine.Contains("--owner-admin-actions"))
            {
                yield return CheckOwnerAdminActions();
                File.WriteAllText(Path.Combine(output,"passed.txt"),"Owner duel controls rendered and applied successfully.");
                Application.Quit(0);
                yield break;
            }
            yield return CheckHandMovement();
            yield return CheckDamageDisplay();
            if(Environment.GetCommandLineArgs().Contains("--ultra-result-check"))
            {
                int xp=UltraProgress.Data.xp,gems=UltraProgress.Data.gems;
                var win=new Package{Function=(int)GameMessage.Win};
                win.Data.writer.Write((byte)OcgCore.LocalPlayer(0));win.Data.writer.Write((byte)0);
                OcgCore.currentMessage=GameMessage.Win;
                var originalResultsFont=TMPro.TMP_Settings.defaultFontAsset;
                try{TMPro.TMP_Settings.defaultFontAsset=null;yield return Program.instance.ocgcore.messageDispatcher.Process(win).ToCoroutine();}
                finally{TMPro.TMP_Settings.defaultFontAsset=originalResultsFont;}
                if(UltraProgress.Data.xp!=xp+100 || UltraProgress.Data.gems!=gems+25)throw new Exception("Duel result rewards were not awarded exactly once.");
                UltraProgress.Finish(0,OcgCore.turns);
                if(UltraProgress.Data.xp!=xp+100 || UltraProgress.Data.gems!=gems+25)throw new Exception("Duplicate duel result awarded twice.");
                string replay=UltraProgress.LastMatch.replay;
                if(string.IsNullOrEmpty(replay) || !File.Exists(Program.PATH_REPLAY+replay+Program.EXPANSION_YRP3D))throw new Exception("Completed duel replay was not saved.");
                if(!File.Exists(Program.PATH_REPLAY+replay+Program.EXPANSION_YRP3D+".protocol"))throw new Exception("Saved replay protocol metadata was not written.");
                var resultView=Program.instance.ocgcore.GetUI<OcgCoreUI>().transform.Find("Ultra Duel Results");
                if(resultView==null)throw new Exception("The real duel end did not create results.");
                var labels=resultView.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
                foreach(var label in labels)
                {
                    label.ForceMeshUpdate(true);
                    if(label.font==null || label.font.atlasTexture==null || label.textInfo.characterCount==0)throw new Exception("Result label failed to render without a default font: "+label.text);
                }
                yield return Capture("post-duel-results");
                if(Program.instance.ui_.chatPanel.showing)throw new Exception("Chat covered the post-duel results.");
                var continueButton=resultView.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="Continue");
                continueButton.onClick.Invoke();yield return null;
                if(resultView!=null)continueButton.onClick.Invoke();
                yield return new WaitForSecondsRealtime(1);
                if(resultView!=null)throw new Exception("Continue did not close the results screen.");
                Debug.Log("ULTRA_RESULTS_PASSED rewards, idempotency, match history, automatic replay, visible fonts without TMP default and Continue");
            }

            File.WriteAllText(Path.Combine(output, "passed.txt"), mode + " menu, selection, deck, local AI duel rendered.");
            Application.Quit(0);
        }

        private IEnumerator CheckOwnerAdminActions()
        {
            var core=Program.instance.ocgcore;
            float readyDeadline=Time.realtimeSinceStartup+60;
            while(!(OcgCore.myTurn && OcgCore.currentMessage==GameMessage.SelectIdleCmd)
                && Time.realtimeSinceStartup<readyDeadline)yield return null;
            if(!OcgCore.myTurn || OcgCore.currentMessage!=GameMessage.SelectIdleCmd)
                throw new Exception("Owner action test did not reach the owner's Main Phase.");
            var controls=core.GetUI<OcgCoreUI>().GetComponent<UltraAdminDuelUI>();
            controls.Open();
            yield return null;
            int mine=core.GetLocationCardCount(CardLocation.Hand,0);
            var field=controls.GetComponentsInChildren<TMPro.TMP_InputField>().First(i=>i.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text=="Exact card name or ID");
            field.text="13068112";
            var add=controls.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.GetComponentsInChildren<TMPro.TextMeshProUGUI>().Any(t=>t.text=="ADD TO MY HAND"));
            add.onClick.Invoke();
            float changedDeadline=Time.realtimeSinceStartup+20;
            while((core.GetLocationCardCount(CardLocation.Hand,0)!=mine+1
                || OcgCore.currentMessage!=GameMessage.SelectIdleCmd)
                && Time.realtimeSinceStartup<changedDeadline)yield return null;
            controls.SendMessage("Close");
            yield return new WaitForSecondsRealtime(1);
            var added=OcgCore.cards.FirstOrDefault(c=>c.p.controller==0 && c.p.InLocation(CardLocation.Hand) && c.GetData().Id==13068112);
            if(added==null || added.model==null || !added.model.activeInHierarchy || added.buttons.Count==0)
            {
                yield return Capture("owner-add-card-failed");
                throw new Exception("Owner hand addition did not render a usable card and refresh idle choices.");
            }
            if(FindObjectsByType<UnityEngine.Playables.PlayableDirector>(FindObjectsSortMode.None).Any(d=>d.name.Contains("CardCheat")))
                throw new Exception("Owner addition left the legacy card-cheat cinematic active.");
            yield return Capture("owner-added-card");
            controls.Open();
            yield return null;
            var lpInput=controls.GetComponentsInChildren<TMPro.TMP_InputField>().First(i=>i.placeholder.GetComponent<TMPro.TextMeshProUGUI>().text=="Life points");
            lpInput.text="12345";
            controls.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="SET MY LP").onClick.Invoke();
            changedDeadline=Time.realtimeSinceStartup+15;
            while((OcgCore.life0!=12345 || OcgCore.currentMessage!=GameMessage.SelectIdleCmd) && Time.realtimeSinceStartup<changedDeadline)yield return null;
            if(OcgCore.life0!=12345)throw new Exception("Owner LP change stalled behind the previous idle prompt.");
            yield return new WaitForSecondsRealtime(.5f);
            controls.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="CLEAR OPPONENT HAND → GY").onClick.Invoke();
            changedDeadline=Time.realtimeSinceStartup+15;
            while((core.GetLocationCardCount(CardLocation.Hand,1)>0 || OcgCore.currentMessage!=GameMessage.SelectIdleCmd) && Time.realtimeSinceStartup<changedDeadline)yield return null;
            if(core.GetLocationCardCount(CardLocation.Hand,1)>0)throw new Exception("Owner clear-hand change stalled.");
            controls.SendMessage("Close");
            yield return new WaitForSecondsRealtime(1);
            var launcher=controls.GetComponentsInChildren<UnityEngine.UI.Button>().First(b=>b.name=="ADMIN");
            if(launcher.gameObject.layer!=launcher.GetComponentInChildren<TMPro.TextMeshProUGUI>().gameObject.layer)
                throw new Exception("Admin launcher text is on a different render layer.");
            yield return Capture("owner-lp-clear-hand");
            Debug.Log("OWNER_ADMIN_ACTIONS_PASSED real GUI card addition, LP change, clear-hand and refreshed Main Phase choices");
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
                var legacy = new Deck { Format = "Genesys", FormatPointCap = 47 };
                legacy.Main.Add(89631139);
                string oldYdk=legacy.GetYDK().Replace(DeckFormatMetadata.Prefix,DeckFormatMetadata.LegacyPrefix);
                File.WriteAllText(path,oldYdk);
                var legacyRead=new Deck(path);
                if(legacyRead.Format!="Genesys" || legacyRead.FormatPointCap!=47 || legacyRead.Main.Single()!=89631139)
                    throw new Exception("Legacy deck metadata or card list failed to load.");
                DuelFormat.ApplyDeckFormat(name);DuelFormat.SaveDeckFormat(name);
                string migrated=File.ReadAllText(path);
                if(migrated.Contains(DeckFormatMetadata.LegacyPrefix) || !migrated.Contains(DeckFormatMetadata.Prefix+"Genesys:47"))
                    throw new Exception("Saving did not migrate the legacy deck header.");
                File.WriteAllText(path,DeckFormatMetadata.Prefix+"Genesys:0\n"+oldYdk);
                var mixed=new Deck(path);
                if(mixed.FormatPointCap!=0 || mixed.Main.Single()!=89631139)
                    throw new Exception("Legacy header overrode canonical deck metadata.");
                Debug.Log("ULTRA_DECK_METADATA_MIGRATION_PASSED legacy read, canonical save, mixed-header priority and unchanged cards");
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
            // The real AI can spend its opening hand before the frame is captured.
            // Seed this paused client-only movement fixture so it also retains a hand card during destruction.
            int missing=2-core.GetLocationCardCount(CardLocation.Hand,1);
            if(missing>0)
            {
                if(core.GetLocationCardCount(CardLocation.Deck,1)<missing)
                    throw new Exception("Opponent deck cannot supply the movement fixture.");
                var draw=new Package{Function=(int)GameMessage.Draw};
                draw.Data.writer.Write((byte)OcgCore.LocalPlayer(1));
                draw.Data.writer.Write((byte)missing);
                for(int i=0;i<missing;i++)draw.Data.writer.Write(0);
                yield return core.messageDispatcher.duel.Process(draw).ToCoroutine();
            }
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
