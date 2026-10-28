using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3
{
    public abstract class CardBuilder : MonoBehaviour
    {
        [Header("Common")]
        [SerializeField] protected Text textDescription;
        [SerializeField] protected Text textDescriptionPendulum;
        [SerializeField] protected Text textAuther;
        [SerializeField] protected TextMeshProUGUI tmpCardName;
        [SerializeField] protected TextMeshProUGUI tmpCardType;
        [SerializeField] protected TextMeshProUGUI tmpAttrRuby;

        [SerializeField] protected Image imageFrame;
        [SerializeField] protected RawImage imageOverFrame;
        [SerializeField] protected Image imageAttr;
        [SerializeField] protected Text textLScale;
        [SerializeField] protected Text textRScale;
        [SerializeField] protected Text textPassword;

        protected string currentFontLanguage;

        protected virtual CardRenderer.CardStyle CardStyle => CardRenderer.CardStyle.OCG_TCG;
        protected virtual float CardNameLabelMaxWidth => 520f;
        protected virtual float[] FontSizeSimplifiedChinese => new float[] { 50f, 40f };
        protected virtual float[] FontSizeTraditionalChinese => new float[] { 55f, 40f };
        protected virtual float[] FontSizeKorean => new float[] { 50f, 40f };
        protected virtual float[] FontSizeJapanese => new float[] { 55f, 40f };
        protected virtual float[] FontSizeEnglish => new float[] { 63f, 43f };

        private RectTransform _tmpCardNameRT;
        private RectTransform TmpCardNameRT =>
            _tmpCardNameRT = _tmpCardNameRT != null ? _tmpCardNameRT : tmpCardName.GetComponent<RectTransform>();
        private ContentSizeFitter _tmpCardNameCSF;
        private ContentSizeFitter TmpCardNameCSF =>
            _tmpCardNameCSF = _tmpCardNameCSF != null ? _tmpCardNameCSF : tmpCardName.GetComponent<ContentSizeFitter>();

        protected virtual void SwitchLanguage(string language = null)
        {
            language ??= Language.GetCardConfig();

            if (currentFontLanguage == language)
                return;
            currentFontLanguage = language;

            if(language == Language.SimplifiedChinese)
            {
                tmpCardName.fontSize = FontSizeSimplifiedChinese[0];
                tmpCardType.fontSize = FontSizeSimplifiedChinese[1];
                SetFonts(CardRenderer.fontChineseSimplified, CardRenderer.tmpFontChineseSimplified);
            }
            else if(language == Language.TraditionalChinese)
            {
                tmpCardName.fontSize = FontSizeTraditionalChinese[0];
                tmpCardType.fontSize = FontSizeTraditionalChinese[1];
                SetFonts(CardRenderer.fontChineseTraditional, CardRenderer.tmpFontChineseTraditional);
            }
            else if(language == Language.Korean)
            {
                tmpCardName.fontSize = FontSizeKorean[0];
                tmpCardType.fontSize = FontSizeKorean[1];
                SetFonts(CardRenderer.fontKorean, CardRenderer.tmpFontKorean);
            }
            else if(language == Language.Japanese)
            {
                tmpCardName.fontSize = FontSizeJapanese[0];
                tmpCardType.fontSize = FontSizeJapanese[1];
                SetFonts(CardRenderer.fontJapanese, CardRenderer.tmpFontJapanese);
            }
            else
            {
                tmpCardName.fontSize = FontSizeEnglish[0];
                tmpCardType.fontSize = FontSizeEnglish[1];
                SetFonts(CardRenderer.fontEnglish, CardRenderer.tmpFontEnglish);
            }

            if (Language.UseLatin(language))
                tmpCardName.fontStyle = FontStyles.SmallCaps;
            else
                tmpCardName.fontStyle = FontStyles.Normal;
        }

        protected virtual void SetFonts(Font font, TMP_FontAsset tmpFont)
        {
            if (tmpFont == null) tmpFont = CardRenderer.EnsureFace();
            if (font != null)
            {
                textDescription.font = font;
                textDescriptionPendulum.font = font;
                textAuther.font = font;
            }
            if (tmpFont == null) return;
            tmpCardName.font = tmpFont;
            tmpCardType.font = tmpFont;
            tmpAttrRuby.font = tmpFont;
        }

        protected virtual void ShowNameOnlyParts()
        {
            imageFrame.gameObject.SetActive(false);
            imageOverFrame.gameObject.SetActive(false);
            imageAttr.gameObject.SetActive(false);

            textDescriptionPendulum.text = string.Empty;
            textDescription.text = string.Empty;
            tmpCardType.text = string.Empty;
            textLScale.text = string.Empty;
            textRScale.text = string.Empty;
        }

        public virtual void SetCardName(Card data, string language)
        {
            SwitchLanguage(language);

            tmpCardName.text = data.Name;
            tmpCardName.color = Color.white;
            TmpCardNameRT.localScale = Vector3.one;
            TmpCardNameCSF.SetLayoutHorizontal();
            var nameWidth = TmpCardNameRT.rect.width;
            if(nameWidth > CardNameLabelMaxWidth)
                TmpCardNameRT.localScale = new Vector3(CardNameLabelMaxWidth / nameWidth, 1f, 1f);
            tmpAttrRuby.text = GetAttributeRubyText(data, language);

            ShowNameOnlyParts();
        }

        public virtual void SetCard(Card data, string language, Texture art, Texture2D overFrame = null)
        {
            SetCardName(data, language);
            SetAllArtPartsOff();
            if (Settings.Data.CardRenderPassword)
                textPassword.text = data.Id.ToString("D8");
            else
                textPassword.text = string.Empty;
            tmpCardName.color = Color.black;
            tmpCardType.color = Color.black;
            textPassword.color = Color.black;
            textAuther.color = Color.black;

            imageFrame.gameObject.SetActive(true);
            imageAttr.gameObject.SetActive(true);
            imageFrame.sprite = TextureManager.container.GetCardFrame(data, CardStyle);
            if(overFrame != null)
            {
                imageOverFrame.gameObject.SetActive(true);
                var descMask = TextureManager.container.GetDescMask(CardStyle, data.HasType(CardType.Pendulum));
                var maskedOF = TextureProcessor.ApplyMaskToAlpha(overFrame, descMask, invertMask: true);
#if UNITY_EDITOR
                maskedOF.alphaIsTransparency = true;
#endif
                imageOverFrame.texture = maskedOF;
            }
        }

        protected Texture2D GetDescMask(bool isPendulum)
        {
            if (isPendulum)
                return TextureManager.container.rd_CardDescColumnMask;
            else
                return TextureManager.container.CardDescColumnMask;
        }

        public virtual RawImage GetArtPartForVideo(bool isPendulum)
        {
            return null;
        }

        public virtual void SetAllArtPartsOff()
        {
        }

        #region IDS_SYS

        private static Dictionary<string, string> idsSysText;
        private static readonly Dictionary<string, Dictionary<string, string>> cachedIdsDics = new();

        private static void LoadIDS(string language)
        {
            if (cachedIdsDics.TryGetValue(language, out var cachedDic))
            {
                idsSysText = cachedDic;
                return;
            }
            idsSysText = new();
            var path = $"{Program.PATH_LOCALES}{language}/IDS/IDS_SYS.txt";
            if (!File.Exists(path))
            {
                cachedIdsDics[language] = idsSysText;
                return;
            }
            var text = File.ReadAllText(path);
            var lines = text.Replace("\r", string.Empty).Split('\n');

            string currentKey = null;
            string currentValue = null;

            foreach (var line in lines)
            {
                var match = Regex.Match(line, @"(?<=\[IDS_SYS\.).*?(?=\])");
                if (match.Success)
                {
                    if (currentValue != null)
                        idsSysText[currentKey] = currentValue;
                    currentKey = match.Value;
                }
                else
                    currentValue = line;
            }

            if (currentKey != null && currentValue != null)
                idsSysText[currentKey] = currentValue;
            cachedIdsDics[language] = idsSysText;
        }

        private static string GetIdsSysText(string key, string language)
        {
            LoadIDS(language);
            if (idsSysText.TryGetValue(key, out var value))
                return value;
            return string.Empty;
        }

        protected static string GetAttributeRubyText(Card data, string language)
        {
            if (data.HasType(CardType.Spell))
                return GetIdsSysText("ATTR_MAGIC_RUBY", language);
            else if (data.HasType(CardType.Trap))
                return GetIdsSysText("ATTR_TRAP_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Light))
                return GetIdsSysText("ATTR_LIGHT_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Dark))
                return GetIdsSysText("ATTR_DARK_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Water))
                return GetIdsSysText("ATTR_WATER_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Fire))
                return GetIdsSysText("ATTR_FIRE_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Earth))
                return GetIdsSysText("ATTR_EARTH_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Wind))
                return GetIdsSysText("ATTR_WIND_RUBY", language);
            else if (data.IsAttribute(CardAttribute.Divine))
                return GetIdsSysText("ATTR_GOD_RUBY", language);
            else
                return string.Empty;
        }

        #endregion

        #region Text Process

        private const string BIG_SLASH = "／";
        private const string SMALL_SLASH = " / ";

        protected static string TextForRender(string description, Card data)
        {
            if (string.IsNullOrEmpty(description))
                return string.Empty;

            if (data.IsRushDuelCard())
            {
                int removeLineCount = 1;
                if (data.HasType(CardType.Maximum))
                    removeLineCount = 2;
                description = Tools.RemoveFirstLines(description, removeLineCount);
            }

            var language = data.isPre ? Language.GetPrereleaseConfig() : Language.GetCardConfig();

            if (!data.IsRushDuelCard())
            {
                description = description.Replace("\t\r\n", "\f\f\f");
                description = description.Replace("\r\n●", "●●●");
                description = description.Replace("\r", string.Empty);
                description = description.Replace("\n", string.Empty);
                description = description.Replace("\f\f\f", Program.STRING_LINE_BREAK);
                description = description.Replace("●●●", $"{Program.STRING_LINE_BREAK}●");
            }

            if (!Language.UseLatin(language))
                description = description.Replace(Program.STRING_SLASH, BIG_SLASH);
            else
                description = description.Replace(Program.STRING_SLASH, SMALL_SLASH);

            if (!Language.UseLatin(language))
                description = description.Replace(" ", "\u00A0");
            description = description.Replace($"{Program.STRING_LINE_BREAK}{Program.STRING_LINE_BREAK}", Program.STRING_LINE_BREAK);
            return description;
        }

        protected static List<string> GetAuthorFromDescription(string description)
        {
            var lines = description.Split(Program.STRING_LINE_BREAK);
            var returnValue = new List<string>();

            StringBuilder beforeDiySymbol = new StringBuilder();
            bool foundDIY = false;

            foreach (var line in lines)
            {
                if (!foundDIY && line.StartsWith(Settings.Data.DiySymbol))
                {
                    var beforeDiySymbolText = beforeDiySymbol.ToString();
                    returnValue.Add(beforeDiySymbolText);
                    returnValue.Add(line);
                    foundDIY = true;
                }
                else if (!foundDIY && !string.IsNullOrEmpty(line))
                {
                    beforeDiySymbol.Append(line);
                }

                if (foundDIY)
                    break;
            }

            if (!foundDIY)
            {
                returnValue.Add(description);
                returnValue.Add(string.Empty);
            }

            return returnValue;
        }

        #endregion


    }
}
