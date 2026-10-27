using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using YgomSystem.UI;
using Tween = DG.Tweening.Tween;

namespace MDPro3.UI.ServantUI
{
    public enum UltraTier { Secondary, Primary, Hero, Danger, Disabled }

    // Fixed neutrals from the hybrid spec. Accent and highlight follow the wallpaper.
    public static class UltraTheme
    {
        public static readonly Color Ink = Hex("0E0F12");
        public static readonly Color Panel = new Color(22f / 255f, 24f / 255f, 29f / 255f, 0.86f);
        public static readonly Color PanelSolid = Hex("16181D");
        public static readonly Color PanelHover = Hex("262A32");
        public static readonly Color Line = new Color(236f / 255f, 230f / 255f, 218f / 255f, 0.16f);
        public static readonly Color Fg = Hex("ECE6DA");
        public static readonly Color Mute = Hex("9096A0");
        public static readonly Color Danger = Hex("FF4B55");
        public static readonly Color DangerPress = Hex("C8242E");
        public static Color Accent { get; private set; } = Hex("4C9BE8");
        public static Color Highlight { get; private set; } = Hex("F25C2A");
        public static event Action Changed;
        static Color accentTarget = Hex("4C9BE8");
        static Color highlightTarget = Hex("F25C2A");
        static bool hasTarget;
        static Tween paletteTween;

        public static Color AccentInk
        {
            get
            {
                float luma = 0.2126f * Accent.r + 0.7152f * Accent.g + 0.0722f * Accent.b;
                return luma > 0.62f ? Ink : Color.white;
            }
        }

        static TMP_FontAsset displayFont, bodyFont, emphasisFont;
        static bool fontsReady;

        public static TMP_FontAsset Display => Ensure() ? displayFont : null;
        public static TMP_FontAsset Body => Ensure() ? bodyFont : null;

        public static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString("#" + html, out var color);
            return color;
        }

        public static void ApplyPalette(float hue, float second, bool hasSecond)
        {
            float h = Mathf.Repeat(hue, 360f);
            Color accent, highlight;
            // Approved reference pairs. Nearby hues keep their own sampled colors.
            if (Mathf.Abs(Mathf.DeltaAngle(h, 292f)) <= 16f)
            {
                accent = Hex("C04BDA");
                highlight = Hex("E0263F");
            }
            else if (Mathf.Abs(Mathf.DeltaAngle(h, 70f)) <= 14f)
            {
                accent = Hex("C6E043");
                highlight = Hex("FF6A2B");
            }
            else
            {
                accent = Color.HSVToRGB(h / 360f, 0.62f, 0.86f);
                float shift = hasSecond && Mathf.Abs(Mathf.DeltaAngle(h, second)) > 26f ? second : h + 36f;
                highlight = Color.HSVToRGB(Mathf.Repeat(shift, 360f) / 360f, 0.78f, 0.95f);
            }
            if (hasTarget && Approximately(accent, accentTarget) && Approximately(highlight, highlightTarget)) return;
            hasTarget = true;
            accentTarget = accent;
            highlightTarget = highlight;
            Color fromAccent = Accent;
            Color fromHighlight = Highlight;
            paletteTween?.Kill();
            float blend = 0f;
            // --acc / --hl crossfade across the UI in 400ms. Wallpaper motion is left alone.
            paletteTween = DOTween.To(() => blend, value =>
            {
                blend = value;
                float eased = UltraMotion.Ease.Evaluate(value);
                Accent = Color.Lerp(fromAccent, accent, eased);
                Highlight = Color.Lerp(fromHighlight, highlight, eased);
                Changed?.Invoke();
            }, 1f, 0.4f).SetEase(Ease.Linear).SetUpdate(true);
        }

        static bool Approximately(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f;

        public static bool IsDanger(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            string value = text.ToUpperInvariant();
            return value.Contains("EXIT") || value.Contains("SIGN OUT") || value.Contains("SURRENDER")
                || value.Contains("LEAVE TABLE") || value.Contains("LEAVE MATCH") || value.Contains("REMOVE FRIEND") || value.Contains("DISBAND")
                || text.Contains("投降");
        }

        public static void Style(TMP_Text label, bool display, float tracking = 0f)
        {
            if (label == null || !Ensure()) return;
            var face = display ? displayFont : bodyFont;
            if (face == null) face = emphasisFont;
            if (face == null) return;
            var field = label.GetComponentInParent<TMP_InputField>();
            if (field != null)
            {
                // Setting the face while the label is parented makes the input field rebuild its mesh and throw.
                var parent = label.transform.parent;
                int sibling = label.transform.GetSiblingIndex();
                label.transform.SetParent(null, false);
                try
                {
                    label.font = face;
                    label.fontStyle = FontStyles.Normal;
                    label.characterSpacing = tracking;
                }
                catch (Exception) { }
                if (parent != null)
                {
                    label.transform.SetParent(parent, false);
                    label.transform.SetSiblingIndex(sibling);
                }
                if (field.fontAsset == null && label.font != null)
                {
                    try { field.fontAsset = label.font; }
                    catch (Exception) { }
                }
                return;
            }
            var previous = label.font;
            label.font = face;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = tracking;
            Remember(face, previous);
        }

        public static void StyleEmphasis(TMP_Text label, float tracking = 0f)
        {
            if (label == null || !Ensure() || emphasisFont == null) { Style(label, false, tracking); return; }
            var previous = label.font;
            label.font = emphasisFont;
            label.fontStyle = FontStyles.Normal;
            label.characterSpacing = tracking;
            Remember(emphasisFont, previous);
        }

        // Prefab labels keep their words and their color. A missing font asset draws nothing,
        // so only a null face is replaced. An existing face and an existing string are left alone.
        public static bool Repair(TMP_Text label, bool display = false)
        {
            if (label == null || label.font != null) return label != null && label.font != null;
            // Input fields regenerate their text from fontAsset. Setting the child face directly throws inside TMP.
            var field = label.GetComponentInParent<TMP_InputField>();
            if (field != null)
            {
                Style(label, display, 0f);
                return label.font != null || field.fontAsset != null;
            }
            try { Style(label, display, 0f); }
            catch (Exception) { }
            if (label.font == null && TMP_Settings.defaultFontAsset != null)
            {
                try { label.font = TMP_Settings.defaultFontAsset; }
                catch (Exception) { }
            }
            return label.font != null;
        }

        static bool Ensure()
        {
            if (fontsReady) return displayFont != null || bodyFont != null;
            fontsReady = true;
            displayFont = Make("BarlowCondensed-ExtraBold");
            bodyFont = Make("Inter-Regular");
            emphasisFont = Make("Inter-SemiBold");
            return displayFont != null || bodyFont != null;
        }

        static TMP_FontAsset Make(string resource)
        {
            var font = Resources.Load<Font>("Ultra/Fonts/" + resource);
            if (font == null) return null;
            var asset = TMP_FontAsset.CreateFontAsset(font);
            if (asset == null) return null;
            asset.name = resource;
            if (asset.fallbackFontAssetTable == null) asset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            return asset;
        }

        static void Remember(TMP_FontAsset face, TMP_FontAsset previous)
        {
            if (previous == null || previous == face) return;
            face.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
            if (!face.fallbackFontAssetTable.Contains(previous)) face.fallbackFontAssetTable.Add(previous);
        }

        static readonly FieldInfo[] tintFields = ResolveTintFields();
        static FieldInfo[] ResolveTintFields()
        {
            var type = typeof(ColorContainer);
            string[] names = { "colorUnselected", "colorSelected", "colorButtonDown", "colorButtonEnter", "colorButtonInactive",
                "intensityUnselected", "intensitySelected", "intensityButtonDown", "intensityButtonEnter", "intensityButtonInactive" };
            var fields = new FieldInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
                fields[i] = type.GetField(names[i], BindingFlags.Instance | BindingFlags.NonPublic);
            return fields;
        }

        public static void TintChrome(ColorContainer container, string role, bool danger)
        {
            if (container == null || tintFields[0] == null) return;
            var ink = container.GetComponent<TMP_Text>() != null;
            Color normal, enter, down, selected, inactive;
            if (ink)
            {
                normal = danger ? Danger : Fg;
                enter = danger ? Color.white : Accent;
                down = danger ? Color.white : Highlight;
                selected = danger ? Danger : Accent;
                inactive = Mute;
            }
            else if (role == "Hover")
            {
                normal = new Color(Accent.r, Accent.g, Accent.b, 0f);
                enter = Accent;
                down = Highlight;
                selected = Accent;
                inactive = new Color(Mute.r, Mute.g, Mute.b, 0.35f);
            }
            else
            {
                normal = PanelSolid;
                enter = PanelHover;
                down = Hex("1A1D24");
                selected = PanelSolid;
                inactive = new Color(1f, 1f, 1f, 0.06f);
            }
            container.baseColor = Color.white;
            tintFields[0].SetValue(container, normal);
            tintFields[1].SetValue(container, selected);
            tintFields[2].SetValue(container, down);
            tintFields[3].SetValue(container, enter);
            tintFields[4].SetValue(container, inactive);
            for (int i = 5; i < tintFields.Length; i++) if (tintFields[i] != null) tintFields[i].SetValue(container, 1f);
        }
    }
}
