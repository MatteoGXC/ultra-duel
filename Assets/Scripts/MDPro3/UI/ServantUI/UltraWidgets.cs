using System;
using DG.Tweening;
using MDPro3.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YgomSystem.UI;
using Tween = DG.Tweening.Tween;

namespace MDPro3.UI.ServantUI
{
    public static class UltraUi
    {
        public static void EnsureRenderer(GameObject go)
        {
            if (go != null && go.GetComponent<CanvasRenderer>() == null)
                go.AddComponent<CanvasRenderer>();
        }

        public static T AddGraphic<T>(GameObject go) where T : Graphic
        {
            if (go == null) return null;
            EnsureRenderer(go);
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        // TMP is itself a Graphic. Never attach it beside another Graphic.
        public static TextMeshProUGUI Text(GameObject host, string childName = "Caption")
        {
            if (host == null) return null;
            var own = host.GetComponent<TextMeshProUGUI>();
            if (own != null) return own;
            if (host.GetComponent<Graphic>() != null)
            {
                for (int i = 0; i < host.transform.childCount; i++)
                {
                    var child = host.transform.GetChild(i);
                    var existing = child.GetComponent<TextMeshProUGUI>();
                    if (existing != null && child.GetComponent<Image>() == null) return existing;
                }
                var nested = new GameObject(childName);
                nested.layer = host.layer;
                nested.transform.SetParent(host.transform, false);
                var nestedRect = nested.AddComponent<RectTransform>();
                nestedRect.anchorMin = Vector2.zero;
                nestedRect.anchorMax = Vector2.one;
                nestedRect.offsetMin = nestedRect.offsetMax = Vector2.zero;
                host = nested;
            }
            EnsureRenderer(host);
            return host.AddComponent<TextMeshProUGUI>();
        }
    }

    // Runtime-created meshes must own a CanvasRenderer before Graphic.OnEnable.
    // RequireComponent on Graphic itself is not applied to these project scripts.
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class UltraMesh : MaskableGraphic
    {
        protected override void OnEnable()
        {
            UltraUi.EnsureRenderer(gameObject);
            base.OnEnable();
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraScrim : UltraMesh
    {
        public bool strong = true;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float left = strong ? 0.62f : 0.48f;
            float mid = strong ? 0.28f : 0.12f;
            Color a = new Color(0, 0, 0, left), b = new Color(0, 0, 0, mid), c = new Color(0, 0, 0, strong ? 0.38f : 0.2f);
            Add(vh, r.xMin, r.xMin + r.width * 0.34f, r.yMin, r.yMax, a, b);
            Add(vh, r.xMin + r.width * 0.34f, r.xMax, r.yMin, r.yMax, b, c);
        }
        static void Add(VertexHelper vh, float x0, float x1, float y0, float y1, Color left, Color right)
        {
            int n = vh.currentVertCount;
            vh.AddVert(new Vector3(x0, y0), left, Vector2.zero);
            vh.AddVert(new Vector3(x1, y0), right, Vector2.zero);
            vh.AddVert(new Vector3(x1, y1), right, Vector2.zero);
            vh.AddVert(new Vector3(x0, y1), left, Vector2.zero);
            vh.AddTriangle(n, n + 1, n + 2);
            vh.AddTriangle(n, n + 2, n + 3);
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UltraChevron : UltraMesh
    {
        public int facing;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 a = new Vector2(0.16f, 0.28f);
            Vector2 b = new Vector2(-0.18f, 0f);
            Vector2 c = new Vector2(0.16f, -0.28f);
            float rot = facing == 1 ? 180f : facing == 2 ? 90f : facing == 3 ? -90f : 0f;
            a = Spin(a, rot); b = Spin(b, rot); c = Spin(c, rot);
            a = r.center + Vector2.Scale(a, r.size);
            b = r.center + Vector2.Scale(b, r.size);
            c = r.center + Vector2.Scale(c, r.size);
            float width = Mathf.Min(r.width, r.height) * 0.11f;
            Stroke(vh, a, b, width, color);
            Stroke(vh, b, c, width, color);
        }
        static Vector2 Spin(Vector2 p, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
            return new Vector2(p.x * cos - p.y * sin, p.x * sin + p.y * cos);
        }
        static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var n = Vector2.Perpendicular((b - a).normalized) * width;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, tint, Vector2.zero);
            vh.AddVert(b + n, tint, Vector2.zero);
            vh.AddVert(b - n, tint, Vector2.zero);
            vh.AddVert(a - n, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    // Stroke icons for menu cards. Color is the stroke, so an accent tint stays a glyph.
    public sealed class UltraLineIcon : UltraMesh
    {
        public int kind;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (r.width < 2f || r.height < 2f) return;
            float w = Mathf.Min(r.width, r.height) * 0.07f;
            Vector2 P(float x, float y) => new Vector2(r.xMin + x * r.width, r.yMin + y * r.height);
            void L(float x0, float y0, float x1, float y1) => Line(vh, P(x0, y0), P(x1, y1), w, color);
            void Ring(float cx, float cy, float radius, int steps)
            {
                for (int i = 0; i < steps; i++)
                {
                    float a = i * Mathf.PI * 2f / steps;
                    float b = (i + 1) * Mathf.PI * 2f / steps;
                    L(cx + Mathf.Cos(a) * radius, cy + Mathf.Sin(a) * radius, cx + Mathf.Cos(b) * radius, cy + Mathf.Sin(b) * radius);
                }
            }
            switch (kind)
            {
                case 1:
                    L(.18f, .22f, .82f, .22f); L(.82f, .22f, .82f, .78f); L(.82f, .78f, .18f, .78f); L(.18f, .78f, .18f, .22f);
                    L(.22f, .38f, .42f, .58f); L(.42f, .58f, .58f, .36f); L(.58f, .36f, .78f, .62f);
                    Ring(.32f, .64f, .07f, 8);
                    break;
                case 2:
                    Ring(.5f, .5f, .32f, 16); Ring(.5f, .5f, .16f, 12); Ring(.5f, .5f, .04f, 6);
                    break;
                case 3:
                    L(.62f, .84f, .38f, .54f); L(.38f, .54f, .58f, .54f); L(.58f, .54f, .34f, .16f);
                    break;
                case 4:
                    L(.16f, .68f, .84f, .68f); L(.16f, .32f, .84f, .32f); Ring(.36f, .68f, .08f, 8); Ring(.64f, .32f, .08f, 8);
                    break;
                case 5:
                    L(.28f, .28f, .48f, .28f); L(.48f, .28f, .48f, .48f); L(.48f, .48f, .72f, .48f);
                    L(.72f, .48f, .72f, .72f); L(.72f, .72f, .52f, .72f); L(.52f, .72f, .28f, .48f); L(.28f, .48f, .28f, .28f);
                    break;
                case 6:
                    Ring(.5f, .5f, .28f, 14);
                    L(.5f, .72f, .34f, .42f); L(.34f, .42f, .5f, .28f); L(.5f, .28f, .66f, .42f); L(.66f, .42f, .5f, .72f);
                    break;
                case 7:
                    L(.22f, .22f, .78f, .22f); L(.78f, .22f, .78f, .78f); L(.78f, .78f, .22f, .78f); L(.22f, .78f, .22f, .22f);
                    L(.36f, .62f, .64f, .62f); L(.36f, .62f, .36f, .40f);
                    break;
                case 8:
                    Ring(.38f, .62f, .16f, 10); Ring(.62f, .40f, .18f, 10); L(.48f, .28f, .74f, .22f);
                    break;
                default:
                    Ring(.5f, .5f, .18f, 12);
                    L(.5f, .5f, .5f, .86f); L(.5f, .5f, .86f, .5f); L(.5f, .5f, .5f, .14f); L(.5f, .5f, .14f, .5f);
                    L(.5f, .5f, .76f, .76f); L(.5f, .5f, .24f, .24f); L(.5f, .5f, .76f, .24f); L(.5f, .5f, .24f, .76f);
                    break;
            }
        }
        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color tint)
        {
            var n = Vector2.Perpendicular((b - a).normalized) * width;
            if (float.IsNaN(n.x)) return;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, tint, Vector2.zero);
            vh.AddVert(b + n, tint, Vector2.zero);
            vh.AddVert(b - n, tint, Vector2.zero);
            vh.AddVert(a - n, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    public sealed class UltraThemedButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public UltraTier tier = UltraTier.Secondary;
        public bool selectedVisual;
        UltraChrome chrome;
        TextMeshProUGUI label;
        Button button;
        RectTransform nudge;
        Vector2 nudgeRest;
        Tween move, press;
        bool hover, down, selected, ready, wasBlocked;
        float pressScale = 1f;

        public void Configure(UltraTier value)
        {
            tier = value;
            chrome = GetComponent<UltraChrome>();
            button = GetComponent<Button>();
            label = GetComponentInChildren<TextMeshProUGUI>(true);
            if (chrome != null)
            {
                chrome.cut = tier == UltraTier.Hero ? UltraCut.Panel : UltraCut.Small;
                chrome.raycastTarget = true;
            }
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                if (chrome != null) button.targetGraphic = chrome;
            }
            if (label != null)
            {
                nudge = label.rectTransform;
                nudgeRest = nudge.anchoredPosition;
                UltraTheme.Style(label, true, tier == UltraTier.Hero ? 4f : 10f);
            }
            if (tier == UltraTier.Hero)
            {
                EnsureUnderline();
                EnsureStripe();
            }
            ready = true;
            Apply();
        }

        public void SetSelected(bool on)
        {
            selectedVisual = on;
            Apply();
        }

        void OnEnable()
        {
            UltraTheme.Changed += Apply;
            if (ready) Apply();
        }
        void OnDisable()
        {
            UltraTheme.Changed -= Apply;
            move?.Kill();
            press?.Kill();
            pressScale = 1f;
            if (nudge != null) nudge.anchoredPosition = nudgeRest;
            transform.localScale = Vector3.one;
        }

        void Update()
        {
            if (!ready || button == null) return;
            bool blocked = !button.interactable;
            if (blocked == wasBlocked) return;
            wasBlocked = blocked;
            Apply();
        }

        public void OnPointerEnter(PointerEventData eventData) { hover = true; Slide(true); Apply(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; down = false; Slide(false); Apply(); }
        public void OnPointerDown(PointerEventData eventData) { down = true; Press(0.95f, 0.07f); Apply(); }
        public void OnPointerUp(PointerEventData eventData) { down = false; Press(1f, 0.14f); Apply(); }
        public void OnSelect(BaseEventData eventData) { selected = true; Slide(true); Apply(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; if (!hover) Slide(false); Apply(); }

        void Slide(bool on)
        {
            if (nudge == null) return;
            move?.Kill();
            var target = nudgeRest + (on ? new Vector2(tier == UltraTier.Hero ? 6f : 0f, tier == UltraTier.Hero ? 0f : 2f) : Vector2.zero);
            move = nudge.DOAnchorPos(target, on ? 0.16f : 0.14f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        void Press(float scale, float time)
        {
            press?.Kill();
            press = DOTween.To(() => pressScale, value => { pressScale = value; transform.localScale = Vector3.one * value; }, scale, time)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        void EnsureUnderline()
        {
            if (transform.Find("Hero underline") != null) return;
            var bar = new GameObject("Hero underline");
            bar.layer = gameObject.layer;
            bar.transform.SetParent(transform, false);
            var rect = bar.AddComponent<RectTransform>();
            UltraUi.EnsureRenderer(bar);
            rect.anchorMin = new Vector2(0.08f, 0.18f);
            rect.anchorMax = new Vector2(0.38f, 0.18f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(0f, -2f);
            rect.offsetMax = new Vector2(0f, 2f);
            var image = bar.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = UltraTheme.Accent;
        }

        void EnsureStripe()
        {
            if (transform.Find("Hero stripe") != null) return;
            var bar = new GameObject("Hero stripe");
            bar.layer = gameObject.layer;
            bar.transform.SetParent(transform, false);
            var rect = bar.AddComponent<RectTransform>();
            UltraUi.EnsureRenderer(bar);
            rect.anchorMin = new Vector2(0f, 0.22f);
            rect.anchorMax = new Vector2(0f, 0.78f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(5f, 0f);
            rect.anchoredPosition = new Vector2(16f, 0f);
            var image = bar.AddComponent<Image>();
            image.raycastTarget = false;
            image.color = UltraTheme.Accent;
        }

        static void PlaceUnderline(RectTransform rect, bool hot)
        {
            rect.anchorMin = new Vector2(0.08f, 0.18f);
            rect.anchorMax = new Vector2(hot ? 0.90f : 0.38f, 0.18f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, 4f);
        }

        void Apply()
        {
            if (!ready || chrome == null) return;
            bool blocked = tier == UltraTier.Disabled || (button != null && !button.interactable);
            var underline = transform.Find("Hero underline")?.GetComponent<Image>();
            var stripe = transform.Find("Hero stripe")?.GetComponent<Image>();
            if (blocked)
            {
                chrome.color = UltraTheme.PanelSolid;
                chrome.rim = 1f;
                chrome.rimColor = UltraTheme.Line;
                chrome.followThemeRim = false;
                if (label != null) label.color = UltraTheme.Mute;
                if (underline != null) underline.color = new Color(UltraTheme.Mute.r, UltraTheme.Mute.g, UltraTheme.Mute.b, 0.45f);
                if (stripe != null) stripe.color = UltraTheme.Mute;
                chrome.SetVerticesDirty();
                return;
            }
            bool hot = hover || selected || selectedVisual;
            switch (tier)
            {
                case UltraTier.Hero:
                    chrome.cut = UltraCut.Panel;
                    chrome.color = down ? new Color(0.07f, 0.08f, 0.10f, 1f) : hot ? UltraTheme.PanelHover : UltraTheme.PanelSolid;
                    chrome.rim = hot || down ? 2f : 1.5f;
                    chrome.rimColor = hot || down ? UltraTheme.Accent : UltraTheme.Line;
                    chrome.followThemeRim = false;
                    if (label != null) label.color = UltraTheme.Fg;
                    if (stripe != null) stripe.color = UltraTheme.Accent;
                    if (underline != null)
                    {
                        underline.color = UltraTheme.Accent;
                        PlaceUnderline(underline.rectTransform, hot);
                    }
                    break;
                case UltraTier.Primary:
                    chrome.cut = UltraCut.Small;
                    chrome.color = hot ? Color.white : UltraTheme.Fg;
                    chrome.rim = 0f;
                    if (label != null) label.color = UltraTheme.Ink;
                    break;
                case UltraTier.Danger:
                    chrome.cut = UltraCut.Small;
                    chrome.color = down ? new Color(0.10f, 0.05f, 0.06f, 1f) : hot ? new Color(0.20f, 0.09f, 0.11f, 1f) : UltraTheme.PanelSolid;
                    chrome.rim = 2f;
                    chrome.rimColor = UltraTheme.Danger;
                    chrome.followThemeRim = false;
                    if (label != null) label.color = UltraTheme.Danger;
                    break;
                default:
                    chrome.cut = UltraCut.Small;
                    chrome.color = hot ? UltraTheme.PanelHover : UltraTheme.Panel;
                    chrome.rim = hot ? 2f : 1.5f;
                    chrome.rimColor = hot ? UltraTheme.Accent : UltraTheme.Line;
                    chrome.followThemeRim = !hot;
                    if (label != null) label.color = UltraTheme.Fg;
                    break;
            }
            chrome.SetVerticesDirty();
        }
    }

    public sealed class UltraMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public bool hero;
        TextMeshProUGUI label;
        RectTransform tick, rule, wash;
        Button button;
        bool hover, down;
        Tween nudge, press, ruleTween;
        Vector2 rest;

        public void Bind(Button target, string title, bool heroItem)
        {
            if (target == null) return;
            hero = heroItem;
            button = target;
            button.transition = Selectable.Transition.None;
            UltraUi.EnsureRenderer(gameObject);
            var hit = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            button.targetGraphic = hit;
            wash = Child("Wash", 0f, 0f, 1f, 1f);
            var washImage = wash != null ? wash.GetComponent<Image>() : null;
            if (washImage != null) { washImage.color = new Color(0, 0, 0, 0); washImage.raycastTarget = false; }
            var bar = Child("Accent", 0f, 0f, 0f, 1f);
            if (bar != null)
            {
                bar.sizeDelta = new Vector2(hero ? 10f : 0f, 0f);
                var barImage = bar.GetComponent<Image>();
                if (barImage != null) { barImage.color = UltraTheme.Accent; barImage.raycastTarget = false; }
            }
            tick = Child("Tick", 0f, 0.28f, 0f, 0.44f);
            if (tick != null)
            {
                tick.sizeDelta = new Vector2(6f, 0f);
                var tickImage = tick.GetComponent<Image>();
                if (tickImage != null) { tickImage.color = UltraTheme.Mute; tickImage.raycastTarget = false; }
                tick.gameObject.SetActive(!hero);
            }
            label = TextLabel(title);
            if (label == null) return;
            rest = label.rectTransform.anchoredPosition;
            if (hero)
            {
                rule = Child("Rule", 0f, 0f, 0f, 0f);
                if (rule != null)
                {
                    rule.anchorMin = new Vector2(0f, 0f);
                    rule.anchorMax = new Vector2(0f, 0f);
                    rule.pivot = new Vector2(0f, 0f);
                    rule.anchoredPosition = new Vector2(44f, 18f);
                    rule.sizeDelta = new Vector2(180f, 5f);
                    var ruleImage = rule.GetComponent<Image>();
                    if (ruleImage != null) { ruleImage.color = UltraTheme.Highlight; ruleImage.raycastTarget = false; }
                }
            }
            UltraTheme.Changed += Paint;
            Paint();
        }

        TextMeshProUGUI TextLabel(string title)
        {
            var existing = GetComponentInChildren<TextMeshProUGUI>(true);
            TextMeshProUGUI text = existing != null && existing.gameObject != gameObject && existing.GetComponent<Image>() == null
                ? existing
                : null;
            if (text == null)
            {
                var go = new GameObject("Label");
                go.layer = gameObject.layer;
                go.transform.SetParent(transform, false);
                var rect = go.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(44f, hero ? 18f : 0f);
                rect.offsetMax = new Vector2(-8f, hero ? -8f : 0f);
                text = UltraUi.Text(go);
            }
            if (text == null) return null;
            text.text = title ?? "";
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.fontSize = hero ? 168f : 86f;
            text.enableAutoSizing = true;
            text.fontSizeMin = hero ? 96f : 48f;
            text.fontSizeMax = hero ? 168f : 86f;
            text.color = UltraTheme.Fg;
            text.raycastTarget = false;
            UltraTheme.Style(text, true, 4f);
            return text;
        }

        RectTransform Child(string name, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(go);
            go.AddComponent<Image>();
            return rect;
        }

        void OnDestroy() { UltraTheme.Changed -= Paint; }
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Nudge(hero ? 6f : 10f); Paint(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; down = false; Nudge(0f); Press(1f); Paint(); }
        public void OnPointerDown(PointerEventData eventData) { down = true; Press(0.96f); Paint(); }
        public void OnPointerUp(PointerEventData eventData) { down = false; Press(1f); Paint(); }
        public void OnSelect(BaseEventData eventData) { hover = true; Nudge(hero ? 6f : 10f); Paint(); }
        public void OnDeselect(BaseEventData eventData) { hover = false; Nudge(0f); Paint(); }

        void Nudge(float x)
        {
            if (label == null) return;
            nudge?.Kill();
            nudge = label.rectTransform.DOAnchorPos(rest + new Vector2(x, 0f), 0.16f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Press(float scale)
        {
            if (label == null) return;
            press?.Kill();
            press = label.rectTransform.DOScale(scale, 0.07f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Paint()
        {
            if (label == null) return;
            label.color = down ? UltraTheme.Highlight : UltraTheme.Fg;
            var bar = transform.Find("Accent")?.GetComponent<Image>();
            if (bar != null) bar.color = UltraTheme.Accent;
            if (tick != null)
            {
                var image = tick.GetComponent<Image>();
                if (image != null) image.color = hover ? UltraTheme.Accent : UltraTheme.Mute;
                tick.anchorMin = new Vector2(0f, hover ? 0.2f : 0.28f);
                tick.anchorMax = new Vector2(0f, hover ? 0.8f : 0.72f);
                tick.offsetMin = tick.offsetMax = Vector2.zero;
                tick.sizeDelta = new Vector2(6f, 0f);
            }
            var washImage = wash != null ? wash.GetComponent<Image>() : null;
            if (washImage != null) washImage.color = hero && hover ? new Color(UltraTheme.Panel.r, UltraTheme.Panel.g, UltraTheme.Panel.b, 0.55f) : new Color(0, 0, 0, 0);
            var ruleImage = rule != null ? rule.GetComponent<Image>() : null;
            if (rule != null && ruleImage != null)
            {
                ruleImage.color = UltraTheme.Highlight;
                ruleTween?.Kill();
                ruleTween = rule.DOSizeDelta(new Vector2(hover ? 300f : 180f, 5f), 0.18f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
            }
            var accent = transform.Find("Accent") as RectTransform;
            if (accent != null && hero)
            {
                accent.anchorMax = new Vector2(0f, 1f);
                accent.sizeDelta = new Vector2(hover ? 16f : 10f, 0f);
            }
        }
    }

    public sealed class UltraIconButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Image box;
        TextMeshProUGUI caption;
        bool hover, down, booted;
        Tween lift, press, tint;
        Vector2 rest;

        public void Bind(Image plate, TextMeshProUGUI label)
        {
            box = plate;
            caption = label;
            if (box == null) return;
            rest = box.rectTransform.anchoredPosition;
            var button = GetComponent<Button>();
            if (button != null)
            {
                button.transition = Selectable.Transition.None;
                var colors = button.colors;
                colors.normalColor = colors.highlightedColor = colors.pressedColor = colors.selectedColor = Color.white;
                colors.fadeDuration = 0.16f;
                button.colors = colors;
            }
            if (caption != null) UltraTheme.Style(caption, true, 12f);
            var glyph = box.GetComponentInChildren<UltraNavIcon>();
            if (glyph != null) glyph.color = Color.white;
            UltraTheme.Changed += Paint;
            Paint();
        }
        void OnDestroy() { UltraTheme.Changed -= Paint; }
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Move(3f, 0.16f); Paint(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; down = false; Move(0f, 0.14f); Press(1f); Paint(); }
        public void OnPointerDown(PointerEventData eventData) { down = true; Press(0.92f); Paint(); }
        public void OnPointerUp(PointerEventData eventData) { down = false; Press(1f); Paint(); }

        void Move(float y, float time)
        {
            if (box == null) return;
            lift?.Kill();
            lift = box.rectTransform.DOAnchorPos(rest + new Vector2(0f, y), time).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Press(float scale)
        {
            if (box == null) return;
            press?.Kill();
            press = box.rectTransform.DOScale(scale, 0.07f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Paint()
        {
            if (box == null) return;
            Color idle = UltraTheme.PanelSolid;
            Color hot = Color.Lerp(UltraTheme.PanelHover, UltraTheme.Accent, 0.42f);
            if (Luma(hot) < 0.16f) hot = Color.Lerp(hot, UltraTheme.Fg, 0.35f);
            Color pressed = Color.Lerp(hot, UltraTheme.PanelSolid, 0.45f);
            Color plate = down ? pressed : hover ? hot : idle;
            Color edge = hover || down ? UltraTheme.Accent : UltraTheme.Line;
            var glyph = box.GetComponentInChildren<UltraNavIcon>();
            if (glyph != null) glyph.color = Color.white;
            float accentLuma = Luma(UltraTheme.Accent);
            if (caption != null) caption.color = hover ? (accentLuma > 0.28f ? UltraTheme.Accent : UltraTheme.Highlight) : UltraTheme.Fg;
            var chrome = box.GetComponent<UltraChrome>();
            if (!booted)
            {
                booted = true;
                ApplyPlate(plate, edge, chrome);
                return;
            }
            Color from = box.color;
            Color fromEdge = chrome != null ? chrome.rimColor : edge;
            tint?.Kill();
            tint = DOTween.To(() => 0f, t => ApplyPlate(Color.Lerp(from, plate, t), Color.Lerp(fromEdge, edge, t), chrome), 1f, 0.17f)
                .SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }

        void ApplyPlate(Color plate, Color edge, UltraChrome chrome)
        {
            box.color = plate;
            if (chrome == null) return;
            chrome.color = plate;
            chrome.rim = 2f;
            chrome.rimColor = edge;
            chrome.followThemeRim = false;
            chrome.SetVerticesDirty();
        }

        static float Luma(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }

    public sealed class UltraAccentText : MonoBehaviour
    {
        TMP_Text text;
        void Awake() { text = GetComponent<TMP_Text>(); }
        void OnEnable() { UltraTheme.Changed += Paint; Paint(); }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void Paint() { if (text != null) text.color = UltraTheme.Accent; }
    }

    public sealed class UltraAccentFill : MonoBehaviour
    {
        Graphic graphic;
        void Awake() { graphic = GetComponent<Graphic>(); }
        void OnEnable() { UltraTheme.Changed += Paint; Paint(); }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void Paint() { if (graphic != null) graphic.color = UltraTheme.Accent; }
    }

    public static class UltraScreenSkin
    {
        public static void Apply(GameObject root)
        {
            if (root == null) return;
            var host = root.GetComponent<UltraSkinHost>() ?? root.AddComponent<UltraSkinHost>();
            host.Paint();
        }
    }

    public sealed class UltraSkinHost : MonoBehaviour
    {
        void OnEnable() { UltraTheme.Changed += Paint; }
        void OnDisable() { UltraTheme.Changed -= Paint; }

        public void Paint()
        {
            foreach (var button in GetComponentsInChildren<SelectionButton>(true))
            {
                if (button.GetType().Name.Contains("Card")) continue;
                bool danger = UltraTheme.IsDanger(button.GetButtonText());
                foreach (var container in button.GetComponentsInChildren<ColorContainerGraphic>(true))
                {
                    if (container.GetComponentInParent<SelectionButton>() != button) continue;
                    string role = container.gameObject.name;
                    bool isCaption = container.GetComponent<TMP_Text>() != null;
                    if (!isCaption && role != "Out" && role != "Hover" && role != "SelectCursor" && role != "Button") continue;
                    UltraTheme.TintChrome(container, role, danger);
                    bool active = button.GetSelectable() == null || button.GetSelectable().interactable;
                    container.SetColor(ColorContainer.SelectMode.Unselected, ColorContainer.StatusMode.Normal, active);
                }
                var buttonText = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (buttonText != null && buttonText.gameObject.name == "ButtonText")
                {
                    UltraTheme.Style(buttonText, true, 8f);
                    if (danger) buttonText.color = UltraTheme.Danger;
                }
                if (button.GetSelectable() != null) button.SetInteractable(button.GetSelectable().interactable);
            }
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                string name = label.gameObject.name;
                if (name is "TextTitle" or "TitleText") UltraTheme.Style(label, true, 4f);
                else if (name is "FrameText" or "SubTitle") UltraTheme.StyleEmphasis(label, 2f);
            }
        }
    }

    public sealed class UltraPerfReadout : MonoBehaviour
    {
        TMP_Text label;
        float smooth;
        void Awake() { label = GetComponent<TMP_Text>(); }
        void Update()
        {
            if (label == null) return;
            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            smooth = smooth <= 0f ? dt : Mathf.Lerp(smooth, dt, 0.08f);
            int fps = Mathf.RoundToInt(1f / smooth);
            string cpu = "N/A", gpu = "N/A";
            FrameTimingManager.CaptureFrameTimings();
            var sample = new FrameTiming[1];
            if (FrameTimingManager.GetLatestTimings(1, sample) > 0)
            {
                if (sample[0].cpuFrameTime > 0.01) cpu = Mathf.Clamp(Mathf.RoundToInt((float)sample[0].cpuFrameTime / 16.7f * 100f), 1, 100) + "%";
                if (sample[0].gpuFrameTime > 0.01) gpu = Mathf.Clamp(Mathf.RoundToInt((float)sample[0].gpuFrameTime / 16.7f * 100f), 1, 100) + "%";
            }
            label.text = "FPS " + fps + "    |    GPU " + gpu + "    |    CPU " + cpu + "    |    LAT N/A";
        }
    }
}
