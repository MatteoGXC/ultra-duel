using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
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
                art.color = Color.white;
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
            art.color = Color.white;
            var shadeObject = new GameObject("Deck backdrop shade", typeof(RectTransform));
            shadeObject.layer = gameObject.layer;
            shadeRect = shadeObject.GetComponent<RectTransform>();
            shadeRect.SetParent(transform, false);
            shadeRect.anchorMin = Vector2.zero;
            shadeRect.anchorMax = Vector2.one;
            shadeRect.offsetMin = shadeRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(shadeObject);
            var shade = shadeObject.AddComponent<UltraCoverShade>();
            shade.raycastTarget = false;
            Stack();
        }

        void LateUpdate()
        {
            Stack();
            if (art == null || art.texture == null || !art.enabled) return;
            var size = art.rectTransform.rect.size;
            if (size.x < 8f || size.y < 8f || size == fitted) return;
            UltraDeckCover.Fit(art, art.texture);
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
}
