using Mono.WebBrowser;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class SelectionToggle_Solo : SelectionToggle_ScrollRectItem
    {

        #region Elements

        private const string LABEL_TXT_TITLE = "Title";
        private TextMeshProUGUI m_Title;
        private TextMeshProUGUI Title =>
            m_Title = m_Title != null ? m_Title
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_TITLE);

        private const string LABEL_ART = "Image";
        private ArtRawImageHandler m_Art;
        private ArtRawImageHandler Art =>
            m_Art = m_Art != null ? m_Art
            : Manager.GetElement<ArtRawImageHandler>(LABEL_ART);

        private const string LABEL_GO_NUMBADGE = "NumBadge";
        private GameObject m_NumBadge;
        private GameObject NumBadge =>
            m_NumBadge = m_NumBadge != null ? m_NumBadge
            : Manager.GetElement(LABEL_GO_NUMBADGE);

        private const string LABEL_GO_TEXTCLEAR = "TextClear";
        private GameObject m_TextClear;
        private GameObject TextClear =>
            m_TextClear = m_TextClear != null ? m_TextClear
            : Manager.GetElement(LABEL_GO_TEXTCLEAR);

        #endregion

        public SoloSelector.BotInfo botInfo;
        private bool isDiyDeck;
        private RawImage bannerImage;
        private TextMeshProUGUI bannerSubtitle;

        private void PaintBanner()
        {
            if (bannerImage == null)
            {
                // The native selection animation moves Offset by 48 pixels. Keep
                // artwork, captions and frame in that same coordinate system.
                var bannerParent = Manager.GetElement<RectTransform>("Offset");
                var background = new GameObject("Arena backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                background.layer = gameObject.layer;
                var bgRect = background.GetComponent<RectTransform>();
                bgRect.SetParent(bannerParent, false);
                bgRect.SetAsFirstSibling();
                Stretch(bgRect, Vector2.zero, Vector2.one, 4);
                var fill = background.GetComponent<Image>();
                fill.color = new Color(.025f, .045f, .075f, 1);
                fill.raycastTarget = false;
                bannerImage = Art.GetComponent<RawImage>();
                bannerImage.rectTransform.SetParent(bannerParent, false);
                Stretch(bannerImage.rectTransform, new Vector2(.18f, 0), Vector2.one, 5);
                bannerImage.rectTransform.SetSiblingIndex(1);
                bannerImage.raycastTarget = false;
                var shade = new GameObject("Arena fade", typeof(RectTransform), typeof(CanvasRenderer), typeof(SoloBannerFade));
                shade.layer = gameObject.layer;
                var shadeRect = shade.GetComponent<RectTransform>();
                shadeRect.SetParent(bannerParent, false);
                Stretch(shadeRect, Vector2.zero, Vector2.one, 5);
                shadeRect.SetSiblingIndex(2);
                shade.GetComponent<SoloBannerFade>().raycastTarget = false;
                Title.rectTransform.SetParent(bannerParent, false);
                Stretch(Title.rectTransform, new Vector2(0, .34f), new Vector2(.76f, .88f), 0);
                Title.rectTransform.offsetMin = new Vector2(24, 0);
                Title.rectTransform.offsetMax = new Vector2(-12, 0);
                UltraTheme.Style(Title, true, 1);
                Title.alignment = TextAlignmentOptions.MidlineLeft;
                Title.enableAutoSizing = true;
                Title.fontSizeMin = 30;
                Title.fontSizeMax = 42;
                Title.color = Color.white;
                Title.raycastTarget = false;
                var caption = new GameObject("Arena category", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                caption.layer = gameObject.layer;
                var captionRect = caption.GetComponent<RectTransform>();
                captionRect.SetParent(bannerParent, false);
                Stretch(captionRect, new Vector2(0, .13f), new Vector2(.80f, .33f), 0);
                captionRect.offsetMin = new Vector2(25, 0);
                captionRect.offsetMax = new Vector2(-12, 0);
                bannerSubtitle = caption.GetComponent<TextMeshProUGUI>();
                UltraTheme.Style(bannerSubtitle, false, 1.2f);
                bannerSubtitle.fontSize = 17;
                bannerSubtitle.color = new Color(.65f, .83f, .94f);
                bannerSubtitle.raycastTarget = false;
                // The original title strip and full-color gate image obscure the new banner.
                foreach (var graphic in GetComponentsInChildren<Graphic>(true))
                    if (graphic.gameObject.name == "Base" || graphic.gameObject.name == "TextTitle") graphic.enabled = false;
            }
            Title.rectTransform.SetAsLastSibling();
            bannerSubtitle.text = botInfo.subtitle ?? "SOLO CHALLENGE";
        }

        private static void Stretch(RectTransform rect, Vector2 min, Vector2 max, float inset)
        {
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            rect.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            if (bannerImage == null || bannerImage.texture == null) return;
            var size = bannerImage.rectTransform.rect.size;
            if (size.x <= 0 || size.y <= 0) return;
            float imageAspect = (float)bannerImage.texture.width / bannerImage.texture.height;
            float viewAspect = size.x / size.y;
            // Fill by cropping, never stretch card art into a strip.
            float width = Mathf.Min(1, viewAspect / imageAspect);
            float height = Mathf.Min(1, imageAspect / viewAspect);
            // Most monster faces sit above the center of the square art. These
            // three fusion bosses need a higher focal point to avoid chest-only crops.
            float focusY = botInfo.main0 == 33760966 ? .88f
                : botInfo.main0 == 24915933 ? .84f
                : botInfo.main0 == 79755671 ? .94f : .60f;
            bannerImage.uvRect = new Rect((1-width)*.5f, (1-height)*focusY, width, height);
        }

        public override void Refresh()
        {
            base.Refresh();
            Title.text = botInfo.name;
            isDiyDeck = botInfo.command.Contains("Lucky");
            Art.SetArt(botInfo.main0);
            PaintBanner();

            NumBadge.SetActive(false);
            TextClear.SetActive(false);
        }

        protected override void CallToggleOnEvent()
        {
            base.CallToggleOnEvent();
            Program.instance.solo.GetUI<SoloSelectorUI>().superScrollView.selected = index;
            Program.instance.solo.GetUI<SoloSelectorUI>().SetOverview(botInfo.desc, isDiyDeck, botInfo.source);
            Program.instance.solo.lastSoloItem = this;
        }

        protected override void CallSubmitEvent()
        {
            base.CallSubmitEvent();
            if (SoloSelector.condition == SoloSelector.Condition.ForSolo)
                Program.instance.solo.StartAIForSolo(index, isDiyDeck);
            else
                Program.instance.solo.StartAIForRoom(index, isDiyDeck);
        }

        public void PublicSubmit()
        {
            CallSubmitEvent();
        }

        protected override void OnNavigation(AxisEventData eventData)
        {
            base.OnNavigation(eventData);

            if (eventData.moveDir == MoveDirection.Right)
            {
                UserInput.NextSelectionIsAxis = true;
                Program.instance.solo.GetUI<SoloSelectorUI>().SelectOnRight();
            }
        }
    }
    public sealed class SoloBannerFade : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            float[] stops = { 0, .28f, .63f, 1 };
            float[] opacity = { .98f, .90f, .45f, .05f };
            for (int i=0; i<stops.Length; i++)
            {
                var c = new Color(.025f, .045f, .075f, opacity[i]);
                float x = Mathf.Lerp(r.xMin, r.xMax, stops[i]);
                vh.AddVert(new Vector3(x, r.yMin), c, Vector2.zero);
                vh.AddVert(new Vector3(x, r.yMax), c, Vector2.up);
                if (i==0) continue;
                int k=i*2; vh.AddTriangle(k-2,k-1,k); vh.AddTriangle(k,k-1,k+1);
            }
        }
    }
}
