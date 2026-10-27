using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI.PropertyOverride;
using MDPro3.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public struct UltraDeckCoverChoice
    {
        public int cardId;
        public string archetype;
        public int copies;
    }

    // Cover art is the deck's own majority archetype, not a fixed illustration.
    public static class UltraDeckArchetype
    {
        public static UltraDeckCoverChoice Choose(Deck deck, string deckName)
        {
            var choice = new UltraDeckCoverChoice();
            if (deck == null) return choice;
            var ids = new List<int>();
            if (deck.Main != null) ids.AddRange(deck.Main);
            if (deck.Extra != null) ids.AddRange(deck.Extra);
            if (deck.Side != null) ids.AddRange(deck.Side);
            var copies = new Dictionary<int, int>();
            foreach (int id in ids)
            {
                if (id <= 0) continue;
                copies.TryGetValue(id, out int count);
                copies[id] = count + 1;
            }
            if (copies.Count == 0) return choice;

            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in copies)
            {
                var card = Lookup(pair.Key);
                if (card == null) continue;
                foreach (string name in Names(card))
                {
                    totals.TryGetValue(name, out int count);
                    totals[name] = count + pair.Value;
                }
            }

            string archetype = "";
            if (totals.Count > 0)
            {
                int best = 0;
                foreach (int count in totals.Values) if (count > best) best = count;
                var tied = new List<string>();
                foreach (var pair in totals) if (pair.Value == best) tied.Add(pair.Key);
                tied.Sort(StringComparer.Ordinal);
                archetype = tied.Count == 1 ? tied[0] : tied[new System.Random(Seed(deck, deckName)).Next(tied.Count)];
            }

            int cardId = 0, bestCopies = -1, bestLevel = -1;
            foreach (var pair in copies)
            {
                var card = Lookup(pair.Key);
                if (card == null) continue;
                if (archetype.Length > 0 && !Has(card, archetype)) continue;
                int level = card.Level;
                if (pair.Value > bestCopies || (pair.Value == bestCopies && level > bestLevel) || (pair.Value == bestCopies && level == bestLevel && pair.Key > cardId))
                {
                    cardId = pair.Key;
                    bestCopies = pair.Value;
                    bestLevel = level;
                }
            }
            choice.cardId = cardId;
            choice.archetype = archetype;
            choice.copies = Mathf.Max(0, bestCopies);
            return choice;
        }

        static int Seed(Deck deck, string deckName)
        {
            string key = !string.IsNullOrEmpty(deck.deckId) ? deck.deckId : deckName ?? "";
            unchecked
            {
                int hash = 17;
                for (int i = 0; i < key.Length; i++) hash = hash * 31 + key[i];
                return hash;
            }
        }

        static Card Lookup(int id)
        {
            var card = CardsManager.GetCardRaw(id);
            if (card != null && card.Id > 0) return card;
            card = CardsManager.Get(id);
            return card != null && card.Id > 0 ? card : null;
        }

        static IEnumerable<string> Names(Card card)
        {
            string raw = card.GetSetName();
            if (string.IsNullOrWhiteSpace(raw) && !string.IsNullOrWhiteSpace(card.strSetName)) raw = card.strSetName;
            if (string.IsNullOrWhiteSpace(raw)) yield break;
            var parts = raw.Split('|');
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string part in parts)
            {
                string name = part.Trim();
                if (name.Length == 0 || !seen.Add(name)) continue;
                yield return name;
            }
        }

        static bool Has(Card card, string archetype)
        {
            foreach (string name in Names(card))
                if (name == archetype) return true;
            return false;
        }
    }

    public sealed class UltraDeckCover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool HasArt => art != null && art.texture != null && art.enabled;
        public string Archetype { get; private set; } = "";
        public int CardId { get; private set; }
        RawImage art;
        RectTransform artRect, shadeRect, body;
        int loaded, requested, loadSerial;
        string shownDeck;
        bool entered, hot, parkedReady;
        Vector2 artRest, parked;

        public void Apply(SelectionToggle_Deck tile, Deck deck)
        {
            if (tile == null || tile.index == 0) return;
            var choice = UltraDeckArchetype.Choose(deck, tile.deckName);
            Archetype = choice.archetype ?? "";
            CardId = choice.cardId;
            Debug.Log("ULTRA_DECK_COVER name=" + tile.deckName + " id=" + (deck != null ? deck.deckId : "") + " archetype=" + (Archetype.Length > 0 ? Archetype : "(fallback)") + " card=" + CardId);
            if (shownDeck != tile.deckName)
            {
                shownDeck = tile.deckName;
                entered = false;
            }
            if (!entered) Enter(tile.index);
            if (CardId > 0 && (requested != CardId || !HasArt))
                BeginLoad(CardId, tile.destroyCancellationToken);
            PlaceBadge(transform);
        }

        void OnEnable()
        {
            if (CardId > 0 && !HasArt)
                BeginLoad(CardId, destroyCancellationToken);
            PlaceBadge(transform);
        }

        void BeginLoad(int id, CancellationToken token)
        {
            requested = id;
            Load(id, ++loadSerial, token).Forget();
        }

        void Enter(int index)
        {
            entered = true;
            var host = Find("Body") as RectTransform;
            if (host == null) return;
            parked = host.anchoredPosition;
            parkedReady = true;
            var group = host.GetComponent<CanvasGroup>();
            if (group == null) group = host.gameObject.AddComponent<CanvasGroup>();
            var end = host.anchoredPosition;
            host.anchoredPosition = end + new Vector2(0f, -40f);
            host.localScale = Vector3.one * 0.96f;
            group.alpha = 0f;
            float delay = 0.12f + Mathf.Max(0, index) * 0.06f;
            host.DOAnchorPos(end, 0.3f).SetDelay(delay).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            host.DOScale(1f, 0.3f).SetDelay(delay).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            group.DOFade(1f, 0.3f).SetDelay(delay).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }

        async UniTaskVoid Load(int id, int serial, CancellationToken token)
        {
            try
            {
                Texture2D texture = null;
                for (int attempt = 0; attempt < 3 && texture == null; attempt++)
                {
                    if (attempt > 0)
                        await UniTask.Delay(160, cancellationToken: token);
                    texture = await CardImageLoader.LoadArtAsync(id, false, token);
                }
                if (this == null || id != CardId || serial != loadSerial)
                {
                    if (texture != null) CardImageLoader.ReleaseArt(id);
                    return;
                }
                if (texture == null)
                {
                    if (requested == id) requested = 0;
                    return;
                }
                if (loaded != 0 && loaded != id) CardImageLoader.ReleaseArt(loaded);
                loaded = id;
                EnsureArt();
                if (art == null) return;
                art.color = Color.white;
                art.texture = texture;
                art.enabled = true;
                Cover(art, texture);
                HideCase();
            }
            catch (OperationCanceledException)
            {
                if (serial == loadSerial && requested == id) requested = 0;
            }
            catch (Exception e)
            {
                if (serial == loadSerial && requested == id) requested = 0;
                Debug.LogWarning("Deck cover: " + e.Message);
            }
        }

        void EnsureArt()
        {
            if (art != null) return;
            body = Find("Body") as RectTransform ?? (RectTransform)transform;
            var host = new GameObject("Deck cover", typeof(RectTransform));
            host.layer = gameObject.layer;
            artRect = host.GetComponent<RectTransform>();
            artRect.SetParent(body, false);
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            artRect.localScale = Vector3.one;
            UltraUi.EnsureRenderer(host);
            art = host.AddComponent<RawImage>();
            art.color = Color.white;
            art.raycastTarget = false;
            var shadeObject = new GameObject("Deck cover shade", typeof(RectTransform));
            shadeObject.layer = gameObject.layer;
            shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(body, false);
            shadeRect.SetSiblingIndex(artRect.GetSiblingIndex() + 1);
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(shadeObject);
            var shade = shadeObject.AddComponent<UltraCoverShade>();
            shade.raycastTarget = false;
            artRest = artRect.anchoredPosition;
        }

        void HideCase()
        {
            foreach (string name in new[] { "DeckCaseIcon", "DeckImage", "CardPos0", "CardPos1", "CardPos2", "IconAddDeck" })
            {
                var part = Find(name);
                if (part != null && part.name != "Deck cover") part.gameObject.SetActive(false);
            }
            PlaceBadge(transform);
        }

        // The tag stays inside the card, over the art. A nested canvas was drawing it above the border.
        public static void PlaceBadge(Transform tile)
        {
            if (tile == null) return;
            Transform badge = null;
            var all = tile.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "Deck format badge") { badge = all[i]; break; }
            if (badge == null) return;
            var ray = badge.GetComponent<GraphicRaycaster>();
            if (ray != null) UnityEngine.Object.Destroy(ray);
            var canvas = badge.GetComponent<Canvas>();
            if (canvas != null) UnityEngine.Object.Destroy(canvas);
            Transform host = tile;
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == "Body") { host = all[i]; break; }
            var rect = badge as RectTransform;
            if (rect == null) return;
            if (rect.parent != host) rect.SetParent(host, false);
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        void LateUpdate()
        {
            StackLayers();
            PlaceBadge(transform);
        }

        // Plate, then art, then the bottom shade, then the format tag. The plate used to sit on top of the picture.
        void StackLayers()
        {
            if (artRect == null || body == null) return;
            Transform plate = null;
            for (int i = 0; i < body.childCount; i++)
                if (body.GetChild(i).name == "Ultra deck plate") { plate = body.GetChild(i); break; }
            int artAt = plate != null ? plate.GetSiblingIndex() + 1 : 0;
            if (artRect.GetSiblingIndex() != artAt) artRect.SetSiblingIndex(artAt);
            if (shadeRect != null)
            {
                int shadeAt = artRect.GetSiblingIndex() + 1;
                if (shadeRect.GetSiblingIndex() != shadeAt) shadeRect.SetSiblingIndex(shadeAt);
            }
        }

        public static void Fit(RawImage image, Texture texture)
        {
            if (image == null || texture == null) return;
            Cover(image, texture);
        }

        // Deck plates crop to the face and upper body: cover, centered, biased to the top of the art.
        public static void FitFace(RawImage image, Texture texture)
        {
            if (image == null || texture == null) return;
            var tile = image.rectTransform;
            float frame = tile.rect.width / Mathf.Max(1f, tile.rect.height);
            float aspect = texture.width / (float)Mathf.Max(1, texture.height);
            float u = 1f, v = 1f, x = 0f, y = 0f;
            if (aspect > frame)
            {
                u = frame / aspect;
                x = (1f - u) * 0.5f;
            }
            else
            {
                v = aspect / Mathf.Max(0.01f, frame);
                y = (1f - v) * 0.9f;
            }
            image.uvRect = new Rect(x, y, u, v);
        }

        static void Cover(RawImage image, Texture texture)
        {
            var tile = image.rectTransform;
            float frame = tile.rect.width / Mathf.Max(1f, tile.rect.height);
            float aspect = texture.width / (float)Mathf.Max(1, texture.height);
            float u = 1f, v = 1f, x = 0f, y = 0f;
            if (aspect > frame)
            {
                u = frame / aspect;
                x = (1f - u) * 0.5f;
            }
            else
            {
                v = aspect / Mathf.Max(0.01f, frame);
                y = (1f - v) * 0.7f;
            }
            image.uvRect = new Rect(x, y, u, v);
        }

        Transform Find(string name)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hot = true;
            if (art != null)
                art.DOColor(new Color(1.08f, 1.08f, 1.08f, 1f), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            if (body != null)
                body.DOScale(1.03f, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            Lift(10f);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hot = false;
            if (art != null)
                art.DOColor(Color.white, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            if (artRect != null)
                artRect.DOAnchorPos(artRest, UltraMotion.Med).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            if (body != null)
            {
                body.DOScale(1f, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                body.DOLocalRotate(Vector3.zero, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                Lift(0f);
            }
        }

        void Lift(float y)
        {
            if (body == null) return;
            if (!parkedReady) parked = body.anchoredPosition;
            body.DOAnchorPos(parked + new Vector2(0f, y), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }

        void OnDestroy()
        {
            if (loaded != 0) CardImageLoader.ReleaseArt(loaded);
        }
    }

    public sealed class UltraCoverShade : UltraMesh
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            // Only the name band. The artwork above it stays untinted.
            float y1 = Mathf.Lerp(r.yMin, r.yMax, 0.26f);
            Band(vh, r.xMin, r.xMax, r.yMin, y1, new Color(0.03f, 0.035f, 0.047f, 0.88f), new Color(0.03f, 0.035f, 0.047f, 0f));
        }

        static void Band(VertexHelper vh, float x0, float x1, float y0, float y1, Color bottom, Color top)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), bottom, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), bottom, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), top, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), top, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    // Archetype art sits behind a chosen deck case. The case and the name stay in front.
    public sealed class UltraDeckBackdrop : MonoBehaviour
    {
        public static readonly Color BackdropInk = new Color(0.65f, 0.65f, 0.65f, 1f);
        RawImage art;
        RectTransform artRect, shadeRect;
        int loaded, requested, serial, misses;
        Vector2 fitted;

        public static void Apply(RectTransform plate, Deck deck, string deckName)
        {
            if (plate == null) return;
            var backdrop = plate.GetComponent<UltraDeckBackdrop>();
            if (backdrop == null) backdrop = plate.gameObject.AddComponent<UltraDeckBackdrop>();
            backdrop.Show(deck, deckName);
        }

        public void Show(Deck deck, string deckName)
        {
            var choice = UltraDeckArchetype.Choose(deck, deckName);
            if (choice.cardId <= 0)
            {
                bool pending = deck != null && ((deck.Main != null && deck.Main.Count > 0) || (deck.Extra != null && deck.Extra.Count > 0));
                if (pending && misses < 8)
                {
                    misses++;
                    Retry(deck, deckName).Forget();
                }
                else Hide();
                return;
            }
            misses = 0;
            if (shadeRect != null) shadeRect.gameObject.SetActive(true);
            if (requested == choice.cardId && art != null && art.texture != null && art.enabled)
            {
                Stack();
                return;
            }
            Load(choice.cardId, ++serial, destroyCancellationToken).Forget();
        }

        async UniTaskVoid Retry(Deck deck, string deckName)
        {
            try
            {
                await UniTask.Delay(300, cancellationToken: destroyCancellationToken);
                if (this != null) Show(deck, deckName);
            }
            catch (OperationCanceledException) { }
        }

        void Hide()
        {
            requested = 0;
            if (art != null) art.enabled = false;
            if (shadeRect != null) shadeRect.gameObject.SetActive(false);
        }

        async UniTaskVoid Load(int id, int ticket, CancellationToken token)
        {
            requested = id;
            try
            {
                Texture2D texture = null;
                for (int attempt = 0; attempt < 3 && texture == null; attempt++)
                {
                    if (attempt > 0) await UniTask.Delay(160, cancellationToken: token);
                    texture = await CardImageLoader.LoadArtAsync(id, false, token);
                }
                if (this == null || ticket != serial)
                {
                    if (texture != null) CardImageLoader.ReleaseArt(id);
                    return;
                }
                if (texture == null)
                {
                    if (requested == id) requested = 0;
                    return;
                }
                if (loaded != 0 && loaded != id) CardImageLoader.ReleaseArt(loaded);
                loaded = id;
                Ensure();
                art.color = BackdropInk;
                art.texture = texture;
                art.enabled = true;
                fitted = Vector2.zero;
                if (shadeRect != null) shadeRect.gameObject.SetActive(true);
                Stack();
            }
            catch (OperationCanceledException)
            {
                if (ticket == serial && requested == id) requested = 0;
            }
            catch (Exception e)
            {
                if (ticket == serial && requested == id) requested = 0;
                Debug.LogWarning("Deck backdrop: " + e.Message);
            }
        }

        void Ensure()
        {
            if (art != null) return;
            var host = new GameObject("Deck backdrop", typeof(RectTransform));
            host.layer = gameObject.layer;
            artRect = host.GetComponent<RectTransform>();
            artRect.SetParent(transform, false);
            artRect.anchorMin = Vector2.zero;
            artRect.anchorMax = Vector2.one;
            artRect.offsetMin = artRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(host);
            art = host.AddComponent<RawImage>();
            art.raycastTarget = false;
            art.color = BackdropInk;
            var shadeObject = new GameObject("Deck backdrop shade", typeof(RectTransform));
            shadeObject.layer = gameObject.layer;
            shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(transform, false);
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(shadeObject);
            var shade = shadeObject.AddComponent<UltraPlateShade>();
            shade.raycastTarget = false;
            Stack();
        }

        void LateUpdate()
        {
            Stack();
            if (art == null || art.texture == null || !art.enabled) return;
            art.color = BackdropInk;
            var size = art.rectTransform.rect.size;
            if (size.x < 8f || size.y < 8f || size == fitted) return;
            UltraDeckCover.FitFace(art, art.texture);
            fitted = size;
        }

        void Stack()
        {
            if (artRect != null && artRect.GetSiblingIndex() != 0) artRect.SetSiblingIndex(0);
            if (shadeRect != null && shadeRect.GetSiblingIndex() != 1) shadeRect.SetSiblingIndex(1);
        }

        void OnDestroy()
        {
            if (loaded != 0) CardImageLoader.ReleaseArt(loaded);
        }
    }

    // Edge vignette plus a solid dark band so the deck name stays readable.
    public sealed class UltraPlateShade : UltraMesh
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 4f || r.height < 4f) return;
            Color edge = new Color(0.02f, 0.024f, 0.038f, 0.72f);
            Color clear = new Color(0.02f, 0.024f, 0.038f, 0f);
            float ix = r.width * 0.18f;
            float iy = r.height * 0.22f;
            Band(vh, r.xMin, r.xMin + ix, r.yMin, r.yMax, edge, clear, true);
            Band(vh, r.xMax - ix, r.xMax, r.yMin, r.yMax, clear, edge, true);
            Band(vh, r.xMin, r.xMax, r.yMax - iy, r.yMax, clear, edge, false);
            float solid = r.height * 0.24f;
            float fadeTop = r.height * 0.46f;
            Color foot = new Color(0.012f, 0.014f, 0.022f, 0.94f);
            Color gone = new Color(0.012f, 0.014f, 0.022f, 0f);
            Band(vh, r.xMin, r.xMax, r.yMin, r.yMin + solid, foot, foot, false);
            Band(vh, r.xMin, r.xMax, r.yMin + solid, r.yMin + fadeTop, foot, gone, false);
        }

        static void Band(VertexHelper vh, float x0, float x1, float y0, float y1, Color a, Color b, bool horizontal)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), horizontal ? a : a, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), horizontal ? b : a, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), horizontal ? b : b, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), horizontal ? a : b, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    // Room Match and the deck selector share one plate: backdrop, small case, name, format tag.
    public static class UltraDeckPlate
    {
        public static void Compose(RectTransform plate, Image caseImage, TMP_Text nameLabel, Deck deck, string deckName)
        {
            if (plate == null) return;
            var layout = plate.GetComponent<UltraDeckPlateLayout>();
            if (layout == null) layout = plate.gameObject.AddComponent<UltraDeckPlateLayout>();
            layout.Bind(caseImage, nameLabel, deck, deckName);
            UltraDeckBackdrop.Apply(plate, deck, deckName);
        }
    }

    public sealed class UltraDeckPlateLayout : MonoBehaviour
    {
        Image caseImage;
        RectTransform shadowRect;
        UltraCircleGraphic shadow;
        TMP_Text nameLabel;
        UltraChrome badge;
        TextMeshProUGUI badgeLabel;
        Deck deck;
        string deckName;
        Vector2 placed;

        public void Bind(Image caseImage, TMP_Text nameLabel, Deck deck, string deckName)
        {
            this.caseImage = caseImage;
            this.nameLabel = nameLabel;
            this.deck = deck;
            this.deckName = deckName;
            placed = Vector2.zero;
            var plate = (RectTransform)transform;
            if (caseImage != null && caseImage.transform.parent != plate)
            {
                caseImage.transform.SetParent(plate, false);
                caseImage.transform.localScale = Vector3.one;
            }
            if (nameLabel == null) nameLabel = CreateName(plate);
            this.nameLabel = nameLabel;
            if (nameLabel.transform.parent != plate) nameLabel.transform.SetParent(plate, false);
            Silence(nameLabel.gameObject);
            if (caseImage != null) Silence(caseImage.gameObject);
            EnsureBadge(plate);
            Apply();
        }

        void LateUpdate()
        {
            var plate = (RectTransform)transform;
            if (plate.rect.height < 8f) return;
            if (plate.rect.size == placed) return;
            placed = plate.rect.size;
            Apply();
        }

        void Apply()
        {
            var plate = (RectTransform)transform;
            float height = plate.rect.height;
            float width = plate.rect.width;
            if (height < 8f || width < 8f) return;
            float caseH = height * 0.45f;
            float caseW = caseH * 0.78f;
            var caseRect = caseImage != null ? caseImage.rectTransform : null;
            if (caseRect != null)
            {
                caseRect.anchorMin = caseRect.anchorMax = new Vector2(0f, 0f);
                caseRect.pivot = new Vector2(0f, 0f);
                caseRect.localScale = Vector3.one;
                caseRect.sizeDelta = new Vector2(caseW, caseH);
                caseRect.anchoredPosition = new Vector2(16f, 18f);
                caseImage.preserveAspect = true;
                caseImage.raycastTarget = false;
                var drop = caseImage.GetComponent<Shadow>();
                if (drop == null) drop = caseImage.gameObject.AddComponent<Shadow>();
                drop.effectColor = new Color(0f, 0f, 0f, 0.55f);
                drop.effectDistance = new Vector2(5f, -7f);
                drop.useGraphicAlpha = true;
                EnsureShadow(plate);
                shadowRect.anchorMin = shadowRect.anchorMax = new Vector2(0f, 0f);
                shadowRect.pivot = new Vector2(0.5f, 0.5f);
                shadowRect.sizeDelta = new Vector2(caseW * 0.92f, Mathf.Max(10f, caseH * 0.16f));
                shadowRect.anchoredPosition = new Vector2(16f + caseW * 0.5f, 12f);
                shadowRect.SetSiblingIndex(Mathf.Max(0, caseRect.GetSiblingIndex()));
                caseRect.SetAsLastSibling();
            }
            if (nameLabel != null)
            {
                string shown = Shown(deckName);
                if (string.IsNullOrEmpty(shown)) shown = nameLabel.text;
                nameLabel.text = shown;
                var rect = nameLabel.rectTransform;
                rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                rect.pivot = new Vector2(1f, 0f);
                float nameW = Mathf.Max(96f, width - 16f - caseW - 36f);
                rect.sizeDelta = new Vector2(nameW, 32f);
                rect.anchoredPosition = new Vector2(-14f, 16f);
                nameLabel.enableAutoSizing = false;
                nameLabel.fontSize = 18f;
                nameLabel.color = Color.white;
                nameLabel.alignment = TextAlignmentOptions.BottomRight;
                nameLabel.textWrappingMode = TextWrappingModes.NoWrap;
                nameLabel.overflowMode = TextOverflowModes.Overflow;
                nameLabel.raycastTarget = false;
                UltraTheme.Style(nameLabel, true, 0.5f);
                nameLabel.fontSize = 18f;
                nameLabel.enableAutoSizing = false;
                nameLabel.fontStyle = FontStyles.Normal;
                rect.SetAsLastSibling();
            }
            PlaceBadge(width);
            if (nameLabel != null) nameLabel.rectTransform.SetAsLastSibling();
            if (badge != null) badge.rectTransform.SetAsLastSibling();
        }

        void EnsureShadow(RectTransform plate)
        {
            if (shadowRect != null) return;
            var host = new GameObject("Case shadow", typeof(RectTransform));
            host.layer = gameObject.layer;
            shadowRect = host.GetComponent<RectTransform>();
            shadowRect.SetParent(plate, false);
            UltraUi.EnsureRenderer(host);
            shadow = host.AddComponent<UltraCircleGraphic>();
            shadow.color = new Color(0f, 0f, 0f, 0.42f);
            shadow.raycastTarget = false;
        }

        void EnsureBadge(RectTransform plate)
        {
            if (badge != null) return;
            var badgeObject = new GameObject("Deck format badge", typeof(RectTransform));
            badgeObject.layer = gameObject.layer;
            var rect = badgeObject.GetComponent<RectTransform>();
            rect.SetParent(plate, false);
            UltraUi.EnsureRenderer(badgeObject);
            badge = badgeObject.AddComponent<UltraChrome>();
            badge.cut = UltraCut.Tag;
            badge.rim = 0f;
            badge.raycastTarget = false;
            badge.followThemeRim = false;
            var text = new GameObject("Label", typeof(RectTransform));
            text.layer = gameObject.layer;
            var textRect = text.GetComponent<RectTransform>();
            textRect.SetParent(rect, false);
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(8f, 2f);
            textRect.offsetMax = new Vector2(-8f, -2f);
            UltraUi.EnsureRenderer(text);
            badgeLabel = text.AddComponent<TextMeshProUGUI>();
            badgeLabel.fontSize = 14f;
            badgeLabel.enableAutoSizing = false;
            badgeLabel.alignment = TextAlignmentOptions.Center;
            badgeLabel.textWrappingMode = TextWrappingModes.NoWrap;
            badgeLabel.raycastTarget = false;
            badgeLabel.overflowMode = TextOverflowModes.Overflow;
        }

        void PlaceBadge(float width)
        {
            if (badge == null) return;
            bool genesys = deck != null && deck.Format == "Genesys";
            int cap = deck != null ? deck.FormatPointCap : 100;
            string label = deck == null ? "" : !genesys ? "TCG" : cap == 0 ? "GENESYS 0" : cap == 100 ? "GENESYS" : "GENESYS " + cap;
            if (string.IsNullOrEmpty(label))
            {
                badge.gameObject.SetActive(false);
                return;
            }
            badge.gameObject.SetActive(true);
            Color ink = cap == 0 && genesys ? UltraTheme.Hex("04201D") : Color.white;
            badge.color = !genesys ? UltraTheme.Hex("2F6BFF") : cap == 0 ? UltraTheme.Hex("14B8A6") : UltraTheme.Hex("8A4DFF");
            badgeLabel.text = label;
            badgeLabel.color = ink;
            badgeLabel.fontSize = 14f;
            UltraTheme.Style(badgeLabel, true, 8f);
            badgeLabel.fontSize = 14f;
            badgeLabel.color = ink;
            var rect = badge.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-10f, -10f);
            rect.sizeDelta = new Vector2(Mathf.Min(width * 0.55f, Mathf.Max(64f, 11f * label.Length + 22f)), 26f);
        }

        TMP_Text CreateName(RectTransform plate)
        {
            var host = new GameObject("Deck name", typeof(RectTransform));
            host.layer = gameObject.layer;
            host.transform.SetParent(plate, false);
            UltraUi.EnsureRenderer(host);
            var label = host.AddComponent<TextMeshProUGUI>();
            label.text = Shown(deckName);
            return label;
        }

        static string Shown(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            int slash = value.LastIndexOfAny(new[] { '/', '\\' });
            if (slash >= 0 && slash < value.Length - 1) value = value.Substring(slash + 1);
            if (value.EndsWith(".ydk", StringComparison.OrdinalIgnoreCase)) value = value.Substring(0, value.Length - 4);
            return value;
        }

        static void Silence(GameObject host)
        {
            var overriders = host.GetComponents<PropertyOverrider>();
            for (int i = 0; i < overriders.Length; i++) overriders[i].enabled = false;
        }
    }
}
