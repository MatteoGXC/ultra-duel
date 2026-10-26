using DG.Tweening;
using MDPro3.UI.PropertyOverride;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.UI;

namespace MDPro3.UI.ServantUI
{
    // Deck tiles keep their click wiring. This only replaces the flat frame, the lime plus, and the hover.
    public sealed class UltraDeckFace : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        SelectionToggle_Deck tile;
        RectTransform body, art;
        Vector2 bodyRest, artRest;
        Vector3 artAngle;
        UltraChrome plate;
        UltraDashFrame dash;
        UltraPlusRing plus;
        TextMeshProUGUI caption;
        Tween lift, press, artMove, artSpin;
        float scale = 1f;
        bool hot, captured, artScaled;

        public static void Bind(SelectionToggle_Deck source)
        {
            if (source == null) return;
            var face = source.GetComponent<UltraDeckFace>();
            if (face == null) face = source.gameObject.AddComponent<UltraDeckFace>();
            face.tile = source;
            face.Sync();
        }

        void Sync()
        {
            if (tile == null) return;
            var rect = (RectTransform)transform;
            bool mobile = PropertyOverrider.NeedMobileLayout();
            rect.sizeDelta = mobile ? new Vector2(340f, 460f) : new Vector2(304f, 500f);
            if (body == null)
            {
                var found = FindNamed("Body");
                if (found != null) body = found as RectTransform;
            }
            bool add = tile.index == 0;
            EnsurePlate();
            if (plate != null)
            {
                plate.color = add ? new Color(0f, 0f, 0f, 0f) : UltraTheme.Panel;
                plate.rim = add ? 2f : 1.5f;
                plate.followThemeRim = !add;
                plate.rimColor = add ? new Color(UltraTheme.Fg.r, UltraTheme.Fg.g, UltraTheme.Fg.b, 0.28f) : UltraTheme.Line;
                plate.SetVerticesDirty();
            }
            if (dash != null) dash.gameObject.SetActive(add);
            var badge = transform.Find("Deck format badge");
            if (badge != null && body != null && badge.parent != body) badge.SetParent(body, true);
            EnsureNewDeck(add);
            if (!add) PlaceCase();
        }

        void EnsurePlate()
        {
            if (plate != null || body == null) return;
            var host = new GameObject("Ultra deck plate", typeof(RectTransform));
            host.layer = gameObject.layer;
            var rect = host.GetComponent<RectTransform>();
            rect.SetParent(body, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(host);
            plate = host.AddComponent<UltraChrome>();
            plate.cut = UltraCut.Panel;
            plate.raycastTarget = false;
            var dashObject = new GameObject("Dash", typeof(RectTransform));
            dashObject.layer = gameObject.layer;
            var dashRect = dashObject.GetComponent<RectTransform>();
            dashRect.SetParent(rect, false);
            dashRect.anchorMin = Vector2.zero;
            dashRect.anchorMax = Vector2.one;
            dashRect.offsetMin = dashRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(dashObject);
            dash = dashObject.AddComponent<UltraDashFrame>();
            dash.raycastTarget = false;
        }

        void EnsureNewDeck(bool add)
        {
            if (body == null) return;
            if (plus == null)
            {
                var ring = new GameObject("New deck plus", typeof(RectTransform));
                ring.layer = gameObject.layer;
                var rect = ring.GetComponent<RectTransform>();
                rect.SetParent(body, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.58f);
                rect.sizeDelta = new Vector2(120f, 120f);
                UltraUi.EnsureRenderer(ring);
                plus = ring.AddComponent<UltraPlusRing>();
                plus.raycastTarget = false;
                var words = new GameObject("New deck label", typeof(RectTransform));
                words.layer = gameObject.layer;
                var wordRect = words.GetComponent<RectTransform>();
                wordRect.SetParent(body, false);
                wordRect.anchorMin = new Vector2(0.08f, 0.08f);
                wordRect.anchorMax = new Vector2(0.92f, 0.24f);
                wordRect.offsetMin = wordRect.offsetMax = Vector2.zero;
                UltraUi.EnsureRenderer(words);
                caption = words.AddComponent<TextMeshProUGUI>();
                caption.text = "NEW DECK";
                caption.fontSize = 22f;
                caption.alignment = TextAlignmentOptions.Center;
                caption.raycastTarget = false;
                UltraTheme.Style(caption, true, 20f);
                caption.fontSize = 22f;
                caption.color = UltraTheme.Mute;
            }
            plus.gameObject.SetActive(add);
            if (caption != null) caption.gameObject.SetActive(add);
        }

        void PlaceCase()
        {
            if (artScaled) return;
            var icon = FindNamed("DeckCaseIcon") as RectTransform;
            if (icon == null) return;
            icon.localScale = new Vector3(0.46f, 0.46f, 1f);
            icon.anchoredPosition = new Vector2(0f, 28f);
            var image = FindNamed("DeckImage") as RectTransform;
            if (image != null) art = image;
            artScaled = true;
        }

        void LateUpdate()
        {
            var graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                var graphic = graphics[i];
                if (graphic == null) continue;
                string name = graphic.gameObject.name;
                if (name is "Out" or "Corner" or "Hover" or "SelectCursor" or "Cursor" or "IconAddDeck" or "Base")
                    graphic.canvasRenderer.SetAlpha(0f);
            }
        }

        Transform FindNamed(string name)
        {
            var all = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }

        void Capture()
        {
            if (captured || body == null) return;
            bodyRest = body.anchoredPosition;
            if (art != null)
            {
                artRest = art.anchoredPosition;
                artAngle = art.localEulerAngles;
            }
            captured = true;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hot = true;
            Capture();
            if (body == null) return;
            lift?.Kill();
            lift = body.DOAnchorPos(bodyRest + new Vector2(0f, 6f), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            if (art != null && tile != null && tile.index != 0)
            {
                artMove?.Kill();
                artSpin?.Kill();
                artMove = art.DOAnchorPos(artRest + new Vector2(0f, 6f), UltraMotion.Med).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                artSpin = art.DOLocalRotate(artAngle + new Vector3(0f, 0f, 3f), UltraMotion.Med).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            }
            if (plus != null && tile != null && tile.index == 0) plus.SetHot(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hot = false;
            if (body != null && captured)
            {
                lift?.Kill();
                lift = body.DOAnchorPos(bodyRest, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                Scale(1f);
            }
            if (art != null && captured)
            {
                artMove?.Kill();
                artSpin?.Kill();
                artMove = art.DOAnchorPos(artRest, UltraMotion.Med).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                artSpin = art.DOLocalRotate(artAngle, UltraMotion.Med).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            }
            if (plus != null) plus.SetHot(false);
        }

        public void OnPointerDown(PointerEventData eventData) => Scale(0.97f);
        public void OnPointerUp(PointerEventData eventData) { if (!hot) return; Scale(1f); }

        void Scale(float target)
        {
            if (body == null) return;
            press?.Kill();
            press = DOTween.To(() => scale, value => { scale = value; body.localScale = Vector3.one * value; }, target, target < 0.99f ? UltraMotion.Press : UltraMotion.Fast)
                .SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }

        void OnDisable()
        {
            lift?.Kill();
            press?.Kill();
            artMove?.Kill();
            artSpin?.Kill();
            scale = 1f;
            if (body != null)
            {
                body.localScale = Vector3.one;
                if (captured) body.anchoredPosition = bodyRest;
            }
            hot = false;
        }
    }

    public sealed class UltraDashFrame : UltraMesh
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            const float inset = 14f;
            var box = new Rect(rect.xMin + inset, rect.yMin + inset, Mathf.Max(8f, rect.width - inset * 2f), Mathf.Max(8f, rect.height - inset * 2f));
            var ink = new Color(UltraTheme.Fg.r, UltraTheme.Fg.g, UltraTheme.Fg.b, 0.18f);
            Dash(vh, new Vector2(box.xMin, box.yMax), new Vector2(box.xMax, box.yMax), ink);
            Dash(vh, new Vector2(box.xMax, box.yMax), new Vector2(box.xMax, box.yMin), ink);
            Dash(vh, new Vector2(box.xMax, box.yMin), new Vector2(box.xMin, box.yMin), ink);
            Dash(vh, new Vector2(box.xMin, box.yMin), new Vector2(box.xMin, box.yMax), ink);
        }

        static void Dash(VertexHelper vh, Vector2 a, Vector2 b, Color ink)
        {
            var line = b - a;
            float length = line.magnitude;
            if (length < 4f) return;
            var dir = line / length;
            var normal = new Vector2(-dir.y, dir.x) * 0.75f;
            for (float cursor = 0f; cursor < length; cursor += 14f)
            {
                float end = Mathf.Min(length, cursor + 8f);
                var p0 = a + dir * cursor;
                var p1 = a + dir * end;
                int index = vh.currentVertCount;
                vh.AddVert(p0 + normal, ink, Vector2.zero);
                vh.AddVert(p1 + normal, ink, Vector2.zero);
                vh.AddVert(p1 - normal, ink, Vector2.zero);
                vh.AddVert(p0 - normal, ink, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }
    }

    public sealed class UltraPlusRing : UltraMesh
    {
        float fill;
        Tween tween;

        public void SetHot(bool hot)
        {
            tween?.Kill();
            float target = hot ? 1f : 0f;
            tween = DOTween.To(() => fill, value => { fill = value; SetVerticesDirty(); }, target, UltraMotion.Fast)
                .SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            transform.DOLocalRotate(new Vector3(0f, 0f, hot ? 90f : 0f), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var rect = rectTransform.rect;
            var center = rect.center;
            float radius = Mathf.Min(rect.width, rect.height) * 0.46f;
            var accent = UltraTheme.Accent;
            if (fill > 0.04f)
            {
                var filled = new Color(accent.r, accent.g, accent.b, fill);
                int hub = vh.currentVertCount;
                vh.AddVert(center, filled, Vector2.zero);
                for (int i = 0; i <= 40; i++)
                {
                    float angle = i * Mathf.PI * 2f / 40f;
                    vh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius - 2f), filled, Vector2.zero);
                    if (i > 0) vh.AddTriangle(hub, hub + i, hub + i + 1);
                }
            }
            Stroke(vh, center, radius, 3f, accent);
            var glyph = Color.Lerp(accent, Color.white, fill);
            float arm = radius * 0.46f;
            float thick = Mathf.Max(3f, radius * 0.08f);
            Bar(vh, center, new Vector2(arm * 2f, thick), glyph);
            Bar(vh, center, new Vector2(thick, arm * 2f), glyph);
        }

        static void Stroke(VertexHelper vh, Vector2 center, float radius, float width, Color ink)
        {
            for (int i = 0; i < 40; i++)
            {
                float a0 = i * Mathf.PI * 2f / 40f;
                float a1 = (i + 1) * Mathf.PI * 2f / 40f;
                var d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
                var d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
                int index = vh.currentVertCount;
                vh.AddVert(center + d0 * radius, ink, Vector2.zero);
                vh.AddVert(center + d1 * radius, ink, Vector2.zero);
                vh.AddVert(center + d1 * (radius - width), ink, Vector2.zero);
                vh.AddVert(center + d0 * (radius - width), ink, Vector2.zero);
                vh.AddTriangle(index, index + 1, index + 2);
                vh.AddTriangle(index, index + 2, index + 3);
            }
        }

        static void Bar(VertexHelper vh, Vector2 center, Vector2 size, Color ink)
        {
            int index = vh.currentVertCount;
            vh.AddVert(center + new Vector2(-size.x * 0.5f, -size.y * 0.5f), ink, Vector2.zero);
            vh.AddVert(center + new Vector2(size.x * 0.5f, -size.y * 0.5f), ink, Vector2.zero);
            vh.AddVert(center + new Vector2(size.x * 0.5f, size.y * 0.5f), ink, Vector2.zero);
            vh.AddVert(center + new Vector2(-size.x * 0.5f, size.y * 0.5f), ink, Vector2.zero);
            vh.AddTriangle(index, index + 1, index + 2);
            vh.AddTriangle(index, index + 2, index + 3);
        }
    }
}
