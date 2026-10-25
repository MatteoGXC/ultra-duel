using System;
using DG.Tweening;
using MDPro3.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YgomSystem.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraScrim : MaskableGraphic
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

    public sealed class UltraChevron : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            Vector2 a = r.center + new Vector2(r.width * 0.16f, r.height * 0.28f);
            Vector2 b = r.center + new Vector2(-r.width * 0.18f, 0f);
            Vector2 c = r.center + new Vector2(r.width * 0.16f, -r.height * 0.28f);
            Stroke(vh, a, b, Mathf.Min(r.width, r.height) * 0.09f);
            Stroke(vh, b, c, Mathf.Min(r.width, r.height) * 0.09f);
        }
        static void Stroke(VertexHelper vh, Vector2 a, Vector2 b, float width)
        {
            var n = Vector2.Perpendicular((b - a).normalized) * width;
            int i = vh.currentVertCount;
            vh.AddVert(a + n, color, Vector2.zero);
            vh.AddVert(b + n, color, Vector2.zero);
            vh.AddVert(b - n, color, Vector2.zero);
            vh.AddVert(a - n, color, Vector2.zero);
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
            if (tier == UltraTier.Hero) EnsureUnderline();
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
            var bar = new GameObject("Hero underline", typeof(RectTransform), typeof(Image));
            bar.transform.SetParent(transform, false);
            bar.layer = gameObject.layer;
            var rect = (RectTransform)bar.transform;
            rect.anchorMin = new Vector2(0.12f, 0.16f);
            rect.anchorMax = new Vector2(0.12f, 0.16f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(90f, 5f);
            var image = bar.GetComponent<Image>();
            image.raycastTarget = false;
            image.color = UltraTheme.Highlight;
        }

        void Apply()
        {
            if (!ready || chrome == null) return;
            bool blocked = tier == UltraTier.Disabled || (button != null && !button.interactable);
            var underline = transform.Find("Hero underline")?.GetComponent<Image>();
            if (blocked)
            {
                chrome.color = new Color(1f, 1f, 1f, 0.06f);
                chrome.rim = 1f;
                chrome.rimColor = UltraTheme.Line;
                chrome.followThemeRim = false;
                if (label != null) label.color = UltraTheme.Mute;
                chrome.SetVerticesDirty();
                return;
            }
            bool hot = hover || selected || selectedVisual;
            switch (tier)
            {
                case UltraTier.Hero:
                    chrome.cut = UltraCut.Panel;
                    chrome.color = hot ? Color.Lerp(UltraTheme.Accent, Color.white, 0.12f) : UltraTheme.Accent;
                    chrome.rim = 0f;
                    if (label != null) label.color = UltraTheme.AccentInk;
                    if (underline != null)
                    {
                        underline.color = UltraTheme.Highlight;
                        var rect = underline.rectTransform;
                        rect.sizeDelta = new Vector2(hot ? Mathf.Max(180f, rectTransform().x - 72f) : 90f, 5f);
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
                    chrome.color = down ? UltraTheme.DangerPress : hot ? UltraTheme.Danger : new Color(UltraTheme.Danger.r, UltraTheme.Danger.g, UltraTheme.Danger.b, 0.08f);
                    chrome.rim = 2f;
                    chrome.rimColor = UltraTheme.Danger;
                    chrome.followThemeRim = false;
                    if (label != null) label.color = hot ? Color.white : UltraTheme.Danger;
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

        Vector2 rectTransform() => ((RectTransform)transform).rect.size;
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
            hero = heroItem;
            button = target;
            button.transition = Selectable.Transition.None;
            var hit = gameObject.GetComponent<Image>() ?? gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            hit.raycastTarget = true;
            button.targetGraphic = hit;
            wash = Child("Wash", 0f, 0f, 1f, 1f);
            wash.GetComponent<Image>().color = new Color(0, 0, 0, 0);
            wash.GetComponent<Image>().raycastTarget = false;
            var bar = Child("Accent", 0f, 0f, 0f, 1f);
            bar.sizeDelta = new Vector2(hero ? 10f : 0f, 0f);
            bar.GetComponent<Image>().color = UltraTheme.Accent;
            bar.GetComponent<Image>().raycastTarget = false;
            tick = Child("Tick", 0f, 0.28f, 0f, 0.44f);
            tick.sizeDelta = new Vector2(6f, 0f);
            tick.GetComponent<Image>().color = UltraTheme.Mute;
            tick.GetComponent<Image>().raycastTarget = false;
            tick.gameObject.SetActive(!hero);
            var text = Child("Label", 0f, 0f, 1f, 1f);
            text.offsetMin = new Vector2(44f, hero ? 18f : 0f);
            text.offsetMax = new Vector2(-8f, hero ? -8f : 0f);
            text.GetComponent<Image>().color = Color.clear;
            text.GetComponent<Image>().raycastTarget = false;
            label = text.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = title;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.fontSize = hero ? 168f : 86f;
            label.enableAutoSizing = true;
            label.fontSizeMin = hero ? 96f : 48f;
            label.fontSizeMax = hero ? 168f : 86f;
            label.color = UltraTheme.Fg;
            label.raycastTarget = false;
            UltraTheme.Style(label, true, 4f);
            rest = label.rectTransform.anchoredPosition;
            if (hero)
            {
                rule = Child("Rule", 0f, 0f, 0f, 0f);
                rule.anchorMin = new Vector2(0f, 0f);
                rule.anchorMax = new Vector2(0f, 0f);
                rule.pivot = new Vector2(0f, 0f);
                rule.anchoredPosition = new Vector2(44f, 18f);
                rule.sizeDelta = new Vector2(180f, 5f);
                rule.GetComponent<Image>().color = UltraTheme.Highlight;
                rule.GetComponent<Image>().raycastTarget = false;
            }
            UltraTheme.Changed += Paint;
            Paint();
        }

        RectTransform Child(string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.gameObject.layer = gameObject.layer;
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
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
                image.color = hover ? UltraTheme.Accent : UltraTheme.Mute;
                tick.anchorMin = new Vector2(0f, hover ? 0.2f : 0.28f);
                tick.anchorMax = new Vector2(0f, hover ? 0.8f : 0.72f);
                tick.offsetMin = tick.offsetMax = Vector2.zero;
                tick.sizeDelta = new Vector2(6f, 0f);
            }
            if (wash != null) wash.GetComponent<Image>().color = hero && hover ? new Color(UltraTheme.Panel.r, UltraTheme.Panel.g, UltraTheme.Panel.b, 0.55f) : new Color(0, 0, 0, 0);
            if (rule != null)
            {
                rule.GetComponent<Image>().color = UltraTheme.Highlight;
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
        bool hover;
        Tween lift, press;
        Vector2 rest;

        public void Bind(Image plate, TextMeshProUGUI label)
        {
            box = plate;
            caption = label;
            rest = box.rectTransform.anchoredPosition;
            UltraTheme.Style(caption, true, 12f);
            UltraTheme.Changed += Paint;
            Paint();
        }
        void OnDestroy() { UltraTheme.Changed -= Paint; }
        public void OnPointerEnter(PointerEventData eventData) { hover = true; Move(3f, 0.16f); Paint(); }
        public void OnPointerExit(PointerEventData eventData) { hover = false; Move(0f, 0.14f); Press(1f); Paint(); }
        public void OnPointerDown(PointerEventData eventData) { Press(0.92f); }
        public void OnPointerUp(PointerEventData eventData) { Press(hover ? 1f : 1f); }

        void Move(float y, float time)
        {
            lift?.Kill();
            lift = box.rectTransform.DOAnchorPos(rest + new Vector2(0f, y), time).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Press(float scale)
        {
            press?.Kill();
            press = box.rectTransform.DOScale(scale, 0.07f).SetEase(Ease.OutCubic).SetUpdate(true).SetLink(gameObject);
        }
        void Paint()
        {
            if (box == null) return;
            box.color = hover ? UltraTheme.Accent : UltraTheme.Panel;
            var chrome = box.GetComponent<UltraChrome>();
            if (chrome != null)
            {
                chrome.color = box.color;
                chrome.rimColor = hover ? UltraTheme.Accent : UltraTheme.Line;
                chrome.SetVerticesDirty();
            }
            if (caption != null) caption.color = hover ? UltraTheme.Accent : UltraTheme.Fg;
            var glyph = box.GetComponentInChildren<UltraNavIcon>();
            if (glyph != null) glyph.color = hover ? UltraTheme.AccentInk : UltraTheme.Fg;
        }
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
                    bool caption = container.GetComponent<TMP_Text>() != null;
                    if (!caption && role is not ("Out" or "Hover" or "SelectCursor" or "Button")) continue;
                    UltraTheme.TintChrome(container, role, danger);
                    bool active = button.GetSelectable() == null || button.GetSelectable().interactable;
                    container.SetColor(ColorContainer.SelectMode.Unselected, ColorContainer.StatusMode.Normal, active);
                }
                var caption = button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (caption != null && caption.gameObject.name == "ButtonText")
                {
                    UltraTheme.Style(caption, true, 8f);
                    if (danger) caption.color = UltraTheme.Danger;
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
}
