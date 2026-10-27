using System;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MDPro3.UI.ServantUI
{
    public sealed class UltraDeckModal : MonoBehaviour
    {
        public static void Open(SelectionToggle_Deck tile, Deck deck, string format, Color badge, Color ink, UnityAction edit)
        {
            if (tile == null) return;
            var canvas = tile.GetComponentInParent<Canvas>();
            if (canvas == null) { edit?.Invoke(); return; }
            var existing = canvas.transform.Find("Deck detail");
            if (existing != null) Destroy(existing.gameObject);

            var root = new GameObject("Deck detail", typeof(RectTransform));
            root.layer = tile.gameObject.layer;
            var overlay = root.GetComponent<RectTransform>();
            overlay.SetParent(canvas.transform, false);
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            UltraUi.EnsureRenderer(root);
            var shade = root.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, 0f);
            shade.raycastTarget = true;
            var modal = root.AddComponent<UltraDeckModal>();

            var boxObject = new GameObject("Box", typeof(RectTransform));
            boxObject.layer = root.layer;
            var box = boxObject.GetComponent<RectTransform>();
            box.SetParent(overlay, false);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(900f, 520f);
            UltraUi.EnsureRenderer(boxObject);
            var plate = boxObject.AddComponent<UltraChrome>();
            plate.cut = UltraCut.Panel;
            plate.color = UltraTheme.PanelSolid;
            plate.raycastTarget = true;

            var tag = new GameObject("Tag", typeof(RectTransform));
            tag.layer = root.layer;
            var tagRect = tag.GetComponent<RectTransform>();
            tagRect.SetParent(box, false);
            tagRect.anchorMin = tagRect.anchorMax = new Vector2(0f, 1f);
            tagRect.pivot = new Vector2(0f, 1f);
            tagRect.anchoredPosition = new Vector2(52f, -44f);
            tagRect.sizeDelta = new Vector2(Mathf.Max(120f, 18f * format.Length + 36f), 42f);
            UltraUi.EnsureRenderer(tag);
            var tagPlate = tag.AddComponent<UltraChrome>();
            tagPlate.cut = UltraCut.Tag;
            tagPlate.color = badge;
            tagPlate.rim = 0f;
            tagPlate.raycastTarget = false;
            var tagText = Label(tagRect, format, 22f, ink);
            tagText.alignment = TextAlignmentOptions.Center;

            var title = Label(box, tile.deckName ?? "", 64f, UltraTheme.Fg);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(52f, -110f);
            titleRect.sizeDelta = new Vector2(-104f, 80f);
            title.alignment = TextAlignmentOptions.MidlineLeft;
            UltraTheme.Style(title, true, 4f);

            int main = deck != null && deck.Main != null ? deck.Main.Count : 0;
            int extra = deck != null && deck.Extra != null ? deck.Extra.Count : 0;
            var detail = Label(box, main + " cards · " + extra + " extra", 22f, UltraTheme.Mute);
            var detailRect = detail.rectTransform;
            detailRect.anchorMin = new Vector2(0f, 1f);
            detailRect.anchorMax = new Vector2(1f, 1f);
            detailRect.pivot = new Vector2(0f, 1f);
            detailRect.anchoredPosition = new Vector2(52f, -200f);
            detailRect.sizeDelta = new Vector2(-104f, 40f);

            string action = edit == null ? "CLOSE" : "EDIT DECK";
            Button(box, action, new Vector2(52f, 64f), () => { modal.Close(() => edit?.Invoke()); });
            Button(box, "CLOSE", new Vector2(280f, 64f), () => modal.Close(null));

            shade.DOColor(new Color(0f, 0f, 0f, 0.6f), 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
            var group = boxObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            box.localScale = Vector3.one * 0.96f;
            group.DOFade(1f, 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
            box.DOScale(1f, 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        void Close(Action then)
        {
            var box = transform.Find("Box") as RectTransform;
            var shade = GetComponent<Image>();
            var group = box != null ? box.GetComponent<CanvasGroup>() : null;
            if (shade != null) shade.DOColor(new Color(0f, 0f, 0f, 0f), 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
            if (box != null) box.DOScale(0.96f, 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
            if (group != null) group.DOFade(0f, 0.2f).SetEase(UltraMotion.Ease).SetUpdate(true);
            DOVirtual.DelayedCall(0.21f, () =>
            {
                then?.Invoke();
                if (this != null) Destroy(gameObject);
            }, true);
        }

        static TextMeshProUGUI Label(Transform parent, string text, float size, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            UltraUi.EnsureRenderer(go);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            UltraTheme.Style(label, true, 2f);
            UltraTheme.Repair(label, true);
            return label;
        }

        static void Button(RectTransform parent, string text, Vector2 position, UnityAction action)
        {
            var go = new GameObject(text, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(text == "CLOSE" ? 180f : 210f, 64f);
            UltraUi.EnsureRenderer(go);
            var plate = go.AddComponent<UltraChrome>();
            plate.cut = UltraCut.Small;
            plate.color = text == "CLOSE" ? UltraTheme.PanelSolid : UltraTheme.Accent;
            plate.rim = text == "CLOSE" ? 1.5f : 0f;
            plate.raycastTarget = true;
            var label = Label(rect, text, 28f, text == "CLOSE" ? UltraTheme.Fg : UltraTheme.AccentInk);
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;
            var button = go.AddComponent<UnityEngine.UI.Button>();
            button.targetGraphic = plate;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
        }
    }
}
