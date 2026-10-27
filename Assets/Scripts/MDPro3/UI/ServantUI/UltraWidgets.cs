using System;
using System.Collections;
using DG.Tweening;
using MDPro3.UI.Popup;
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
            if (r.width < 2f || r.height < 2f) return;
            // Facing 0 points left: a shaft plus a head, not a bare chevron.
            float rot = facing == 1 ? 180f : facing == 2 ? 90f : facing == 3 ? -90f : 0f;
            Vector2 U(float x, float y)
            {
                var p = Spin(new Vector2(x - 0.5f, y - 0.5f), rot);
                return r.center + new Vector2(p.x * r.width, p.y * r.height);
            }
            Quad(vh, U(0.40f, 0.42f), U(0.84f, 0.42f), U(0.84f, 0.58f), U(0.40f, 0.58f), color);
            Tri(vh, U(0.14f, 0.50f), U(0.50f, 0.80f), U(0.50f, 0.20f), color);
        }
        static void Quad(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Vector2 d, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero);
            vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero);
            vh.AddVert(d, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
        static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int i = vh.currentVertCount;
            vh.AddVert(a, tint, Vector2.zero);
            vh.AddVert(b, tint, Vector2.zero);
            vh.AddVert(c, tint, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
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
        public bool rule;
        UltraChrome chrome;
        TextMeshProUGUI label;
        Button button;
        RectTransform nudge;
        Vector2 nudgeRest, selfRest;
        Tween move, press, labelMove;
        bool selfReady;
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
            if (tier == UltraTier.Hero && rule) EnsureStripe();
            if (tier == UltraTier.Hero && rule) EnsureUnderline();
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
            labelMove?.Kill();
            pressScale = 1f;
            if (nudge != null) nudge.anchoredPosition = nudgeRest;
            if (selfReady) ((RectTransform)transform).anchoredPosition = selfRest;
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
        public void OnPointerDown(PointerEventData eventData) { down = true; Press(0.96f, UltraMotion.Press); Apply(); }
        public void OnPointerUp(PointerEventData eventData) { down = false; Press(1f, UltraMotion.Fast); Apply(); }
        public void OnSelect(BaseEventData eventData) { selected = true; Slide(true); Apply(); }
        public void OnDeselect(BaseEventData eventData) { selected = false; if (!hover) Slide(false); Apply(); }

        void Slide(bool on)
        {
            var plate = (RectTransform)transform;
            if (!selfReady) { selfRest = plate.anchoredPosition; selfReady = true; }
            move?.Kill();
            float lift = tier == UltraTier.Hero ? 3f : 2f;
            move = plate.DOAnchorPos(selfRest + (on ? new Vector2(0f, lift) : Vector2.zero), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            if (nudge == null || tier != UltraTier.Hero) return;
            labelMove?.Kill();
            labelMove = nudge.DOAnchorPos(nudgeRest + (on ? new Vector2(6f, 0f) : Vector2.zero), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }

        void Press(float scale, float time)
        {
            press?.Kill();
            press = DOTween.To(() => pressScale, value => { pressScale = value; transform.localScale = Vector3.one * value; }, scale, time)
                .SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
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
                if (underline != null) underline.gameObject.SetActive(false);
                if (tier == UltraTier.Primary)
                {
                    chrome.cut = UltraCut.Small;
                    chrome.color = Color.Lerp(UltraTheme.PanelSolid, UltraTheme.Accent, 0.62f);
                    chrome.rim = 1.5f;
                    chrome.rimColor = UltraTheme.Accent;
                    chrome.followThemeRim = false;
                    if (label != null) label.color = UltraTheme.AccentInk;
                    if (stripe != null) stripe.color = UltraTheme.Accent;
                    chrome.SetVerticesDirty();
                    return;
                }
                chrome.color = UltraTheme.PanelSolid;
                chrome.rim = 1f;
                chrome.rimColor = UltraTheme.Line;
                chrome.followThemeRim = false;
                if (label != null) label.color = UltraTheme.Mute;
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
                        underline.gameObject.SetActive(rule);
                        underline.color = UltraTheme.Highlight;
                        PlaceUnderline(underline.rectTransform, hot);
                    }
                    break;
                case UltraTier.Primary:
                    chrome.cut = UltraCut.Small;
                    chrome.color = hot ? Color.Lerp(UltraTheme.Accent, Color.white, 0.22f) : UltraTheme.Accent;
                    chrome.rim = 0f;
                    if (label != null) label.color = UltraTheme.AccentInk;
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

    // Horizontal accent-to-highlight fill used by the DUEL press sweep.
    public sealed class UltraAccentGradient : UltraMesh
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            UltraTheme.Changed += Refresh;
        }
        protected override void OnDisable()
        {
            UltraTheme.Changed -= Refresh;
            base.OnDisable();
        }
        void Refresh() { SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(r.xMin, r.yMin), UltraTheme.Accent, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMin), UltraTheme.Highlight, Vector2.zero);
            vh.AddVert(new Vector3(r.xMax, r.yMax), UltraTheme.Highlight, Vector2.zero);
            vh.AddVert(new Vector3(r.xMin, r.yMax), UltraTheme.Accent, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    public sealed class UltraMenuItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
    {
        public bool hero;
        public float FillCoverage => fill == null ? 0f : fill.localScale.x;
        TextMeshProUGUI label;
        RectTransform motion, tick, rule, wash, fill, sweep, accentBar;
        Image brighten, glow;
        CanvasGroup fillGroup, sweepGroup, glowGroup;
        Button button;
        bool hover, down, filling, fading, ruleOpen;
        Tween nudge, press, ruleTween, fillTween, releaseDelay, brightTween, liftTween, sweepTween, glowTween;
        Vector2 rest;
        float pressScale = 1f;

        public void Bind(Button target, string title, bool heroItem)
        {
            if (target == null) return;
            hero = heroItem;
            button = target;
            button.transition = Selectable.Transition.None;
            UltraUi.EnsureRenderer(gameObject);
            var hit = gameObject.GetComponent<Image>();
            if (hit == null) hit = gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            button.targetGraphic = hit;
            motion = Stretch("Motion");
            motion.gameObject.AddComponent<RectMask2D>();
            if (hero) BuildDuelLayers();
            wash = Child("Wash", 0f, 0f, 1f, 1f);
            var washImage = wash != null ? wash.GetComponent<Image>() : null;
            if (washImage != null) { washImage.color = new Color(0, 0, 0, 0); washImage.raycastTarget = false; }
            accentBar = Child("Accent", 0f, 0f, 0f, 1f);
            if (accentBar != null)
            {
                accentBar.sizeDelta = new Vector2(hero ? 10f : 0f, 0f);
                var barImage = accentBar.GetComponent<Image>();
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
            rule = Child("Rule", 0f, 0f, 0f, 0f);
            if (rule != null)
            {
                rule.anchorMin = new Vector2(0f, 0f);
                rule.anchorMax = new Vector2(0f, 0f);
                rule.pivot = new Vector2(0f, 0f);
                rule.anchoredPosition = new Vector2(44f, hero ? 14f : 4f);
                rule.sizeDelta = new Vector2(hero ? 180f : 0f, hero ? 5f : 4f);
                var ruleImage = rule.GetComponent<Image>();
                if (ruleImage != null)
                {
                    ruleImage.color = hero ? UltraTheme.Highlight : UltraTheme.Accent;
                    ruleImage.raycastTarget = false;
                }
            }
            if (hero) PlayIdle();
            if (hero && brighten != null) brighten.transform.SetAsLastSibling();
            if (rule != null) rule.SetAsLastSibling();
            UltraTheme.Changed += Paint;
            Paint();
        }

        void BuildDuelLayers()
        {
            var host = new GameObject("Duel fill", typeof(RectTransform));
            host.layer = gameObject.layer;
            fill = host.GetComponent<RectTransform>();
            fill.SetParent(motion, false);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.localScale = new Vector3(0f, 1f, 1f);
            UltraUi.EnsureRenderer(host);
            var gradient = host.AddComponent<UltraAccentGradient>();
            gradient.raycastTarget = false;
            fillGroup = host.AddComponent<CanvasGroup>();
            fillGroup.alpha = 0f;
            fillGroup.blocksRaycasts = false;

            var band = new GameObject("Sweep", typeof(RectTransform));
            band.layer = gameObject.layer;
            sweep = band.GetComponent<RectTransform>();
            sweep.SetParent(motion, false);
            sweep.anchorMin = new Vector2(0f, 0f);
            sweep.anchorMax = new Vector2(0f, 1f);
            sweep.pivot = new Vector2(0.5f, 0.5f);
            sweep.sizeDelta = new Vector2(220f, 0f);
            sweep.anchoredPosition = new Vector2(860f, 0f);
            UltraUi.EnsureRenderer(band);
            var sweepImage = band.AddComponent<Image>();
            sweepImage.sprite = SoftBand();
            sweepImage.color = new Color(1f, 1f, 1f, 0.22f);
            sweepImage.raycastTarget = false;
            sweepGroup = band.AddComponent<CanvasGroup>();
            sweepGroup.blocksRaycasts = false;

            var haze = new GameObject("Glow", typeof(RectTransform));
            haze.layer = gameObject.layer;
            var glowRect = haze.GetComponent<RectTransform>();
            glowRect.SetParent(motion, false);
            glowRect.anchorMin = new Vector2(0.08f, 0.2f);
            glowRect.anchorMax = new Vector2(0.62f, 0.85f);
            glowRect.offsetMin = glowRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(haze);
            glow = haze.AddComponent<Image>();
            glow.sprite = SoftBlob();
            glow.color = UltraTheme.Accent;
            glow.raycastTarget = false;
            glowGroup = haze.AddComponent<CanvasGroup>();
            glowGroup.alpha = 0.15f;
            glowGroup.blocksRaycasts = false;

            var bright = new GameObject("Brighten", typeof(RectTransform));
            bright.layer = gameObject.layer;
            var brightRect = bright.GetComponent<RectTransform>();
            brightRect.SetParent(motion, false);
            brightRect.anchorMin = Vector2.zero;
            brightRect.anchorMax = Vector2.one;
            brightRect.offsetMin = brightRect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(bright);
            brighten = bright.AddComponent<Image>();
            brighten.color = new Color(1f, 1f, 1f, 0f);
            brighten.raycastTarget = false;
        }

        void PlayIdle()
        {
            if (sweep != null)
            {
                sweep.anchoredPosition = new Vector2(860f, 0f);
                sweepTween?.Kill();
                sweepTween = DOTween.Sequence().SetUpdate(true).SetLoops(-1).SetLink(gameObject)
                    .Append(sweep.DOAnchorPosX(-280f, 1.76f).SetEase(Ease.InOutSine))
                    .AppendInterval(1.44f)
                    .AppendCallback(() => { if (sweep != null) sweep.anchoredPosition = new Vector2(860f, 0f); });
            }
            if (glowGroup != null)
            {
                glowGroup.alpha = 0.12f;
                glowTween = glowGroup.DOFade(0.7f, 1.2f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetLink(gameObject);
            }
        }

        static Sprite bandSprite, blobSprite;
        static Sprite SoftBand()
        {
            if (bandSprite != null) return bandSprite;
            var tex = new Texture2D(64, 4, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            for (int x = 0; x < 64; x++)
            {
                float t = Mathf.Abs(x / 63f - 0.5f) * 2f;
                float a = Mathf.Clamp01(1f - t);
                a *= a;
                var c = new Color(1f, 1f, 1f, a);
                for (int y = 0; y < 4; y++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            bandSprite = Sprite.Create(tex, new Rect(0, 0, 64, 4), new Vector2(0.5f, 0.5f));
            return bandSprite;
        }
        static Sprite SoftBlob()
        {
            if (blobSprite != null) return blobSprite;
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            var center = new Vector2((n - 1) * 0.5f, (n - 1) * 0.5f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center) / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            blobSprite = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
            return blobSprite;
        }

        RectTransform Stretch(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
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
                go.transform.SetParent(motion != null ? motion : transform, false);
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
            go.transform.SetParent(motion != null ? motion : transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(go);
            go.AddComponent<Image>();
            return rect;
        }

        void LateUpdate()
        {
            // The first hover sample can run before layout, locking the bar at its idle width.
            if (!hover || rule == null) return;
            if (ruleTween != null && ruleTween.IsActive()) return;
            if (Mathf.Abs(rule.sizeDelta.x - RuleWidth()) > 6f) Paint();
        }
        void OnDestroy()
        {
            UltraTheme.Changed -= Paint;
            releaseDelay?.Kill();
            fillTween?.Kill();
            sweepTween?.Kill();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            hover = true;
            if (hero) { Lift(4f); Brighten(true); }
            else Nudge(12f);
            Paint();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            hover = false;
            down = false;
            if (hero) { Lift(0f); Brighten(false); PressMotion(1f, UltraMotion.Fast); }
            else { Nudge(0f); PressLabel(1f); }
            Paint();
        }
        public void OnPointerDown(PointerEventData eventData)
        {
            down = true;
            if (hero) BeginFill();
            else PressLabel(0.96f);
            Paint();
        }
        public void OnPointerUp(PointerEventData eventData)
        {
            down = false;
            if (hero) { PressMotion(1f, UltraMotion.Fast); EndFill(); }
            else PressLabel(1f);
            Paint();
        }
        public void OnSelect(BaseEventData eventData)
        {
            hover = true;
            if (hero) { Lift(4f); Brighten(true); }
            else Nudge(12f);
            Paint();
        }
        public void OnDeselect(BaseEventData eventData)
        {
            hover = false;
            if (hero) { Lift(0f); Brighten(false); }
            else Nudge(0f);
            Paint();
        }

        void Nudge(float x)
        {
            if (label == null) return;
            nudge?.Kill();
            nudge = label.rectTransform.DOAnchorPos(rest + new Vector2(x, 0f), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void PressLabel(float scale)
        {
            if (label == null) return;
            press?.Kill();
            press = label.rectTransform.DOScale(scale, scale < 0.99f ? UltraMotion.Press : UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void PressMotion(float scale, float time)
        {
            if (motion == null) return;
            press?.Kill();
            press = DOTween.To(() => pressScale, value =>
            {
                pressScale = value;
                motion.localScale = new Vector3(value, value, 1f);
            }, scale, time).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void Lift(float y)
        {
            if (motion == null) return;
            liftTween?.Kill();
            liftTween = motion.DOAnchorPos(new Vector2(0f, y), UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void Brighten(bool on)
        {
            if (brighten == null) return;
            brightTween?.Kill();
            brightTween = brighten.DOFade(on ? 0.18f : 0f, UltraMotion.Fast).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
        }
        void BeginFill()
        {
            releaseDelay?.Kill();
            bool restart = !filling || fading;
            filling = true;
            fading = false;
            if (fillGroup != null) fillGroup.alpha = 1f;
            if (restart && fill != null)
            {
                fillTween?.Kill();
                fill.localScale = new Vector3(0f, 1f, 1f);
                fillTween = fill.DOScaleX(1f, 0.3f).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
            }
            PressMotion(0.96f, UltraMotion.Press);
            glowTween?.Pause();
            if (glowGroup != null) glowGroup.alpha = 0f;
        }
        void EndFill()
        {
            if (!filling) return;
            releaseDelay?.Kill();
            releaseDelay = DOVirtual.DelayedCall(0.26f, () =>
            {
                fading = true;
                Paint();
                if (fillGroup == null) { filling = false; fading = false; return; }
                fillGroup.DOFade(0f, 0.32f).SetEase(Ease.Linear).SetUpdate(true).SetLink(gameObject).OnComplete(() =>
                {
                    filling = false;
                    fading = false;
                    if (fill != null) fill.localScale = new Vector3(0f, 1f, 1f);
                    if (glowGroup != null) glowGroup.alpha = 0.2f;
                    glowTween?.Play();
                    Paint();
                });
            }).SetUpdate(true).SetLink(gameObject);
        }
        float RuleWidth()
        {
            float width = ((RectTransform)transform).rect.width;
            if (width < 80f) width = hero ? 680f : 160f;
            if (!hero) return hover ? 160f : 0f;
            return hover ? Mathf.Max(180f, width - 44f) : 180f;
        }
        void Paint()
        {
            if (label == null) return;
            bool ink = hero && filling && !fading;
            label.color = ink ? UltraTheme.Ink : UltraTheme.Fg;
            if (accentBar != null)
            {
                var barImage = accentBar.GetComponent<Image>();
                if (barImage != null) barImage.color = UltraTheme.Accent;
                if (hero)
                {
                    accentBar.anchorMax = new Vector2(0f, 1f);
                    accentBar.sizeDelta = new Vector2(hover ? 16f : 10f, 0f);
                }
            }
            if (tick != null)
            {
                var image = tick.GetComponent<Image>();
                if (image != null) image.color = hover ? UltraTheme.Accent : UltraTheme.Mute;
                tick.anchorMin = new Vector2(0f, hover ? 0.2f : 0.28f);
                tick.anchorMax = new Vector2(0f, hover ? 0.8f : 0.72f);
                tick.offsetMin = tick.offsetMax = Vector2.zero;
                tick.sizeDelta = new Vector2(6f, 0f);
                tick.anchoredPosition = new Vector2(hover ? 4f : 0f, 0f);
            }
            var washImage = wash != null ? wash.GetComponent<Image>() : null;
            if (washImage != null) washImage.color = new Color(0, 0, 0, 0);
            var ruleImage = rule != null ? rule.GetComponent<Image>() : null;
            if (rule != null && ruleImage != null)
            {
                var inkColor = hero ? UltraTheme.Highlight : UltraTheme.Accent;
                float alpha = ink ? 0f : 1f;
                ruleImage.color = new Color(inkColor.r, inkColor.g, inkColor.b, alpha);
                bool open = hover;
                float target = RuleWidth();
                bool settled = ruleTween == null || !ruleTween.IsActive();
                if (open != ruleOpen || (settled && Mathf.Abs(rule.sizeDelta.x - target) > 6f))
                {
                    ruleOpen = open;
                    float height = hero ? 5f : 4f;
                    ruleTween?.Kill();
                    ruleTween = DOTween.To(() => rule.sizeDelta.x, value => rule.sizeDelta = new Vector2(value, height), target, hero ? 0.25f : UltraMotion.Med)
                        .SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
                }
            }
            if (glow != null) glow.color = UltraTheme.Accent;
            if (sweepGroup != null) sweepGroup.alpha = filling && !fading ? 0f : 1f;
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
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Move(2f, UltraMotion.Fast); Paint(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; down = false; Move(0f, UltraMotion.Fast); Press(1f); Paint(); }
        public void OnPointerDown(PointerEventData eventData) { down = true; Press(0.92f); Paint(); }
        public void OnPointerUp(PointerEventData eventData) { down = false; Press(1f); Paint(); }

        void Move(float y, float time)
        {
            if (box == null) return;
            lift?.Kill();
            lift = box.rectTransform.DOAnchorPos(rest + new Vector2(0f, y), time).SetEase(UltraMotion.Ease).SetUpdate(true).SetLink(gameObject);
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
            Color hot = Color.Lerp(UltraTheme.Hex("2A2D35"), UltraTheme.Accent, 0.22f);
            if (Luma(hot) < 0.12f) hot = Color.Lerp(hot, UltraTheme.Hex("3C4250"), 0.65f);
            Color pressed = Color.Lerp(hot, Color.white, 0.08f);
            Color plate = down ? pressed : hover ? hot : idle;
            Color edge = hover || down ? UltraTheme.Accent : UltraTheme.Line;
            var glyph = box.GetComponentInChildren<UltraNavIcon>();
            if (glyph != null) glyph.color = Color.white;
            if (caption != null) caption.color = hover || down ? Color.white : UltraTheme.Fg;
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
            var host = root.GetComponent<UltraSkinHost>();
            if (host == null) host = root.AddComponent<UltraSkinHost>();
            if (root.GetComponent<UltraFontRepair>() == null) root.AddComponent<UltraFontRepair>();
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
                bool heading = name is "TextTitle" or "TitleText" or "Text_ServantTitle";
                if (heading) UltraTheme.Style(label, true, 4f);
                else if (name is "FrameText" or "SubTitle") UltraTheme.StyleEmphasis(label, 2f);
                else UltraTheme.Repair(label, label.fontSize >= 28f);
            }
            if (GetComponent<MainMenuUI>() == null) DecorateServantTitle();
        }

        void DecorateServantTitle()
        {
            string subtitle = ServantSubtitle();
            foreach (var label in GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.gameObject.name is not ("TextTitle" or "TitleText" or "Text_ServantTitle")) continue;
                UltraHeader.Mark(label, subtitle);
            }
        }

        string ServantSubtitle()
        {
            if (GetComponent<DeckSelectorUI>() != null)
                return MDPro3.Servant.DeckSelector.condition == MDPro3.Servant.DeckSelector.Condition.ForEdit ? "Deck builder" : "Choose a deck";
            if (GetComponent<SoloSelectorUI>() != null) return "Duel the AI";
            if (GetComponent<AppearanceUI>() != null) return "Appearance";
            if (GetComponent<SettingServantUI>() != null) return "Settings";
            if (GetComponent<ReplaySelectorUI>() != null) return "Replays";
            if (GetComponent<PuzzleSelectorUI>() != null) return "Puzzles";
            if (GetComponent<CharacterSelectorUI>() != null) return "Characters";
            if (GetComponent<DeckEditorUI>() != null) return "Deck editor";
            if (GetComponent<DeckBrowserUI>() != null) return "Online decks";
            if (GetComponent<RoomServantUI>() != null) return "Duel room";
            if (GetComponent<OnlineServantUI>() != null) return "Online duel";
            return null;
        }
    }

    public static class UltraHeader
    {
        public static void Mark(TMP_Text title, string subtitle)
        {
            if (title == null) return;
            if (title.transform.Find("Title rule") == null)
            {
                var ruleObject = new GameObject("Title rule", typeof(RectTransform));
                ruleObject.layer = title.gameObject.layer;
                var rule = ruleObject.GetComponent<RectTransform>();
                rule.SetParent(title.transform, false);
                rule.anchorMin = rule.anchorMax = new Vector2(0f, 0f);
                rule.pivot = new Vector2(0f, 1f);
                rule.anchoredPosition = new Vector2(0f, -4f);
                rule.sizeDelta = new Vector2(84f, 5f);
                UltraUi.EnsureRenderer(ruleObject);
                var ink = ruleObject.AddComponent<Image>();
                ink.raycastTarget = false;
                ink.color = UltraTheme.Highlight;
                ruleObject.AddComponent<UltraHighlightFill>();
            }
            if (string.IsNullOrEmpty(subtitle)) return;
            var existing = title.transform.Find("Ultra subtitle");
            TextMeshProUGUI label;
            if (existing == null)
            {
                var subtitleObject = new GameObject("Ultra subtitle", typeof(RectTransform));
                subtitleObject.layer = title.gameObject.layer;
                var rect = subtitleObject.GetComponent<RectTransform>();
                rect.SetParent(title.transform, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(2f, -16f);
                rect.sizeDelta = new Vector2(640f, 28f);
                UltraUi.EnsureRenderer(subtitleObject);
                label = subtitleObject.AddComponent<TextMeshProUGUI>();
                label.fontSize = 18f;
                label.alignment = TextAlignmentOptions.MidlineLeft;
                label.raycastTarget = false;
                label.overflowMode = TextOverflowModes.Overflow;
            }
            else label = existing.GetComponent<TextMeshProUGUI>();
            if (label == null) return;
            label.text = subtitle;
            UltraTheme.Style(label, true, 1f);
            label.fontSize = 22f;
            label.fontStyle = FontStyles.Bold;
            label.color = UltraTheme.Fg;
        }
    }

    public sealed class UltraHighlightFill : MonoBehaviour
    {
        Graphic graphic;
        void Awake() { graphic = GetComponent<Graphic>(); }
        void OnEnable() { UltraTheme.Changed += Paint; Paint(); }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void Paint() { if (graphic != null) graphic.color = UltraTheme.Highlight; }
    }

    // Deck tiles and other prefabs are spawned after the first skin pass. Keep repairing null faces.
    public sealed class UltraFontRepair : MonoBehaviour
    {
        float next;
        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.2f;
            var labels = GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null || label.font != null) continue;
                bool display = label.fontSize >= 28f || label.gameObject.name is "TextTitle" or "TitleText" or "TextDeckNumValue";
                UltraTheme.Repair(label, display);
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

    public sealed class UltraDiamond : UltraMesh
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 c = r.center;
            float rx = r.width * 0.46f, ry = r.height * 0.46f;
            Vector2 top = c + new Vector2(0f, ry);
            Vector2 right = c + new Vector2(rx, 0f);
            Vector2 bottom = c + new Vector2(0f, -ry);
            Vector2 left = c + new Vector2(-rx, 0f);
            Stroke(vh, top, right);
            Stroke(vh, right, bottom);
            Stroke(vh, bottom, left);
            Stroke(vh, left, top);
        }
        void Stroke(VertexHelper vh, Vector2 a, Vector2 b)
        {
            float width = Mathf.Min(rectTransform.rect.width, rectTransform.rect.height) * 0.012f;
            var n = Vector2.Perpendicular((b - a).normalized) * Mathf.Max(1.5f, width);
            int i = vh.currentVertCount;
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddVert(a - n, color, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2);
            vh.AddTriangle(i, i + 2, i + 3);
        }
    }

    public sealed class UltraOnAccentText : MonoBehaviour
    {
        TMP_Text text;
        void Awake() { text = GetComponent<TMP_Text>(); }
        void OnEnable() { UltraTheme.Changed += Paint; Paint(); }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void Paint() { if (text != null) text.color = UltraTheme.AccentInk; }
    }

    public static class UltraSoloChrome
    {
        public static void Apply(SoloSelectorUI solo)
        {
            if (solo == null) return;
            var settings = Find(solo.transform, "Settings");
            if (settings != null)
            {
                for (int i = 0; i < settings.childCount; i++)
                    Plate(settings.GetChild(i) as RectTransform, UltraCut.Small);
            }
            var overview = Find(solo.transform, "TextOverview");
            if (overview != null && overview.parent is RectTransform box)
                Plate(box, UltraCut.Panel);
            var play = Find(solo.transform, "ButtonPlay");
            if (play != null) PaintPlay(play);
            foreach (var label in solo.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label == null || label.font != null) continue;
                UltraTheme.Repair(label, label.fontSize >= 28f);
            }
        }

        static void PaintPlay(Transform play)
        {
            foreach (var graphic in play.GetComponentsInChildren<Graphic>(true))
            {
                string n = graphic.gameObject.name;
                if (n is "Out" or "Hover" or "HoverOn" or "HoverOff" or "SelectCursor" or "Button")
                    graphic.enabled = false;
            }
            var plate = play.Find("Ultra play plate") as RectTransform;
            if (plate == null)
            {
                var host = new GameObject("Ultra play plate", typeof(RectTransform));
                host.layer = play.gameObject.layer;
                plate = host.GetComponent<RectTransform>();
                plate.SetParent(play, false);
                plate.anchorMin = Vector2.zero;
                plate.anchorMax = Vector2.one;
                plate.offsetMin = plate.offsetMax = Vector2.zero;
                var ignore = host.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
                UltraUi.EnsureRenderer(host);
                var chrome = host.AddComponent<UltraChrome>();
                chrome.cut = UltraCut.Panel;
                chrome.raycastTarget = true;
                chrome.rim = 0f;
            }
            plate.SetAsFirstSibling();
            var fill = plate.GetComponent<UltraChrome>();
            if (fill != null)
            {
                fill.color = UltraTheme.Accent;
                fill.cut = UltraCut.Panel;
                fill.raycastTarget = true;
            }
            TMP_Text caption = null;
            foreach (var label in play.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label.gameObject.name == "ButtonText" || label.text.Trim().Equals("PLAY", StringComparison.OrdinalIgnoreCase))
                    caption = label;
            }
            if (caption == null) return;
            caption.gameObject.SetActive(true);
            caption.enabled = true;
            caption.text = "PLAY";
            caption.fontSize = 32f;
            caption.alignment = TextAlignmentOptions.Center;
            caption.color = UltraTheme.AccentInk;
            UltraTheme.Style(caption, true, 8f);
            caption.rectTransform.SetAsLastSibling();
        }

        public static void Plate(RectTransform row, UltraCut cut)
        {
            if (row == null || row.Find("Ultra row plate") != null) return;
            var host = new GameObject("Ultra row plate", typeof(RectTransform));
            host.layer = row.gameObject.layer;
            var rect = host.GetComponent<RectTransform>();
            rect.SetParent(row, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0f, 2f);
            rect.offsetMax = new Vector2(0f, -2f);
            var ignore = host.AddComponent<LayoutElement>();
            ignore.ignoreLayout = true;
            UltraUi.EnsureRenderer(host);
            var chrome = host.AddComponent<UltraChrome>();
            chrome.color = UltraTheme.PanelSolid;
            chrome.cut = cut;
            chrome.rim = 1.5f;
            chrome.followThemeRim = true;
            chrome.raycastTarget = false;
        }

        static Transform Find(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }
    }

    public sealed class UltraSoloRepaint : MonoBehaviour
    {
        float next;
        void OnEnable() { UltraTheme.Changed += Paint; Paint(); }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.35f;
            Paint();
        }
        void Paint() { UltraSoloChrome.Apply(GetComponent<SoloSelectorUI>()); }
    }

    public static class UltraEditorChrome
    {
        static readonly string[] Panels = { "BG", "Background", "DeckView", "CardListArea", "CollectionAreaCenter", "TabArea", "Header", "CardCollectionView", "CardDetailView" };

        public static void Apply(Component host)
        {
            if (host == null) return;
            var root = host.transform;
            foreach (string name in Panels)
            {
                var panel = Find(root, name);
                if (panel == null) continue;
                TintPanel(panel.GetComponent<Image>());
                for (int i = 0; i < panel.childCount; i++)
                {
                    var child = panel.GetChild(i);
                    if (child.name is "BG" or "Background" or "Base" or "Frame")
                        TintPanel(child.GetComponent<Image>());
                }
            }
            var tabs = Find(root, "TabArea");
            if (tabs != null)
            {
                foreach (var toggle in tabs.GetComponentsInChildren<SelectionToggle>(true))
                    PaintTab(toggle);
            }
            var header = Find(root, "Header");
            if (header != null && header.Find("Ultra header rule") == null)
            {
                var rule = new GameObject("Ultra header rule", typeof(RectTransform));
                rule.layer = header.gameObject.layer;
                var rect = rule.GetComponent<RectTransform>();
                rect.SetParent(header, false);
                rect.anchorMin = new Vector2(0.02f, 0f);
                rect.anchorMax = new Vector2(0.18f, 0f);
                rect.pivot = new Vector2(0f, 0f);
                rect.sizeDelta = new Vector2(0f, 4f);
                rect.anchoredPosition = new Vector2(0f, 2f);
                var ignore = rule.AddComponent<LayoutElement>();
                ignore.ignoreLayout = true;
                UltraUi.EnsureRenderer(rule);
                var ink = rule.AddComponent<Image>();
                ink.color = UltraTheme.Highlight;
                ink.raycastTarget = false;
                rule.AddComponent<UltraHighlightFill>();
            }
            foreach (var image in host.GetComponentsInChildren<Image>(true))
            {
                if (image is UltraChrome || image.GetComponentInParent<SelectionButton>() != null) continue;
                var rect = image.rectTransform.rect;
                if (rect.width < 200f || rect.height < 80f) continue;
                var c = image.color;
                if (c.a < 0.4f) continue;
                if (c.b > c.r + 0.06f && c.b > 0.15f)
                    image.color = new Color(UltraTheme.PanelSolid.r, UltraTheme.PanelSolid.g, UltraTheme.PanelSolid.b, c.a);
            }
            foreach (var button in host.GetComponentsInChildren<SelectionButton>(true))
            {
                if (button.GetType().Name.Contains("Card")) continue;
                var caption = button.GetComponentInChildren<TMP_Text>(true);
                if (caption == null || caption.gameObject.name != "ButtonText") continue;
                UltraTheme.Style(caption, true, 8f);
                caption.color = UltraTheme.IsDanger(caption.text) ? UltraTheme.Danger : UltraTheme.Fg;
            }
        }

        static void TintPanel(Image image)
        {
            if (image == null || image is UltraChrome) return;
            if (image.GetComponentInParent<SelectionButton>() != null) return;
            image.color = image.gameObject.name is "BG" or "Background"
                ? UltraTheme.Ink
                : new Color(UltraTheme.PanelSolid.r, UltraTheme.PanelSolid.g, UltraTheme.PanelSolid.b, 0.96f);
        }

        static void PaintTab(SelectionToggle toggle)
        {
            bool on = toggle.isOn;
            foreach (var image in toggle.GetComponentsInChildren<Image>(true))
            {
                if (image is UltraChrome) continue;
                var rect = image.rectTransform.rect;
                if (rect.width > 0f && rect.width < 28f && rect.height < 28f) continue;
                string n = image.gameObject.name;
                if (n.Contains("Icon") || n.Contains("Cursor")) continue;
                image.color = on ? UltraTheme.Accent : UltraTheme.PanelSolid;
            }
            foreach (var label in toggle.GetComponentsInChildren<TMP_Text>(true))
            {
                UltraTheme.Style(label, true, 8f);
                label.color = on ? UltraTheme.AccentInk : UltraTheme.Fg;
            }
        }

        static Transform Find(Transform root, string name)
        {
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
                if (all[i].name == name) return all[i];
            return null;
        }
    }

    public sealed class UltraEditorRepaint : MonoBehaviour
    {
        float next;
        void OnEnable() { UltraTheme.Changed += Paint; }
        void OnDisable() { UltraTheme.Changed -= Paint; }
        void LateUpdate()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + 0.4f;
            Paint();
        }
        void Paint() { UltraEditorChrome.Apply(this); }
    }

    // The format picker is a stock popup. Repaint it while it is open so the lime selection pill cannot come back.
    public sealed class UltraFormatChrome : MonoBehaviour
    {
        bool ruled;
        void OnEnable() { StartCoroutine(PaintLoop()); }
        IEnumerator PaintLoop()
        {
            while (isActiveAndEnabled)
            {
                Paint();
                yield return new WaitForEndOfFrame();
            }
        }
        void Paint()
        {
            EnsureHeader();
            foreach (var image in GetComponentsInChildren<Image>(true))
            {
                if (image is UltraChrome) continue;
                if (image.GetComponentInParent<SelectionToggle_PopupSelectionItem>() != null) continue;
                var c = image.color;
                if (Lime(c))
                {
                    image.sprite = null;
                    image.color = UltraTheme.Accent;
                    continue;
                }
                var rect = image.rectTransform.rect;
                if (rect.width > 80f && rect.height > 24f && c.a > 0.35f && c.b > c.r + 0.08f && c.b > 0.18f)
                    image.color = new Color(UltraTheme.PanelSolid.r, UltraTheme.PanelSolid.g, UltraTheme.PanelSolid.b, c.a);
            }
            var current = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            foreach (var item in GetComponentsInChildren<SelectionToggle_PopupSelectionItem>(true))
            {
                bool on = item.isOn || (current != null && (current.transform == item.transform || current.transform.IsChildOf(item.transform)));
                foreach (var image in item.GetComponentsInChildren<Image>(true))
                {
                    if (image is UltraChrome) continue;
                    string n = image.gameObject.name;
                    bool plate = n is "Out" or "Base" or "Background" or "Image";
                    bool ornament = n is "SelectCursor" or "Cursor" or "Checkmark" or "Hover" or "Over" or "Corner";
                    if (ornament || Lime(image.color))
                    {
                        image.sprite = null;
                        image.color = on ? UltraTheme.Accent : Color.clear;
                        continue;
                    }
                    if (plate)
                        image.color = on ? UltraTheme.Accent : UltraTheme.PanelSolid;
                }
                var label = item.GetComponentInChildren<TMP_Text>(true);
                if (label == null) continue;
                label.color = on ? UltraTheme.AccentInk : UltraTheme.Fg;
                var ink = label.GetComponent<ColorContainerGraphic>();
                if (ink != null) ink.baseColor = label.color;
            }
        }
        static bool Lime(Color c)
        {
            return c.a > 0.2f && c.g > 0.45f && c.g > c.r + 0.12f && c.g > c.b + 0.05f;
        }
        void EnsureHeader()
        {
            if (ruled) return;
            Transform title = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
                if (t.name == "TitleArea") { title = t; break; }
            if (title == null) return;
            ruled = true;
            var plateObject = new GameObject("Ultra format header", typeof(RectTransform));
            plateObject.layer = gameObject.layer;
            var plate = plateObject.GetComponent<RectTransform>();
            plate.SetParent(title, false);
            plate.SetAsFirstSibling();
            plate.anchorMin = Vector2.zero;
            plate.anchorMax = Vector2.one;
            plate.offsetMin = plate.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(plateObject);
            var chrome = plateObject.AddComponent<UltraChrome>();
            chrome.cut = UltraCut.Panel;
            chrome.color = UltraTheme.PanelSolid;
            chrome.raycastTarget = false;
            var ruleObject = new GameObject("Ultra header rule", typeof(RectTransform));
            ruleObject.layer = gameObject.layer;
            var rule = ruleObject.GetComponent<RectTransform>();
            rule.SetParent(title, false);
            rule.anchorMin = new Vector2(0.06f, 0f);
            rule.anchorMax = new Vector2(0.28f, 0f);
            rule.pivot = new Vector2(0f, 0f);
            rule.sizeDelta = new Vector2(0f, 4f);
            rule.anchoredPosition = new Vector2(0f, 6f);
            UltraUi.EnsureRenderer(ruleObject);
            var ink = ruleObject.AddComponent<Image>();
            ink.color = UltraTheme.Accent;
            ink.raycastTarget = false;
            ruleObject.AddComponent<UltraAccentFill>();
        }
    }
}
