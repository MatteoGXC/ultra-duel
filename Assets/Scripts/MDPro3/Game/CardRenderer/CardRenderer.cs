using Cysharp.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.Utility;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace MDPro3
{
    public class CardRenderer : MonoBehaviour
    {

        public const string BIG_SLASH = "／";
        public const string SMALL_SLASH = " / ";
        private static int prefabIndex = 0;

        #region Reference

        [Header("CardRenderer")]
        [SerializeField] private RectTransform builder;
        [SerializeField] private Camera renderCamera;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private Material matComposite;
        private Material matCompositeInstance;
        private Texture2D texFrame;
        public RenderTexture renderTexture;
        private ScriptableRenderContext context;

        #endregion

        #region Fonts

        private static bool fontsLoaded;

        public static Font fontChineseSimplified;
        public static Font fontChineseTraditional;
        public static Font fontKorean;
        public static Font fontJapanese;
        public static Font fontEnglish;

        public static TMP_FontAsset tmpFontChineseSimplified;
        public static TMP_FontAsset tmpFontChineseTraditional;
        public static TMP_FontAsset tmpFontKorean;
        public static TMP_FontAsset tmpFontJapanese;
        public static TMP_FontAsset tmpFontEnglish;

        static bool fontsStarted;
        static Task fontsTask;

        public static UniTask FontsReady
        {
            get
            {
                if (!fontsStarted)
                {
                    fontsStarted = true;
                    // Every card awaits this shared initialization, including after it completes.
                    fontsTask = LoadFontsAsync().AsTask();
                }
                return fontsTask.AsUniTask();
            }
        }

        private static async UniTask LoadFontsAsync()
        {
            if (fontsLoaded)
                return;

            fontChineseSimplified = await LoadSourceFont("RenderFontChineseSimplified");
            tmpFontChineseSimplified = await LoadFace("RenderFontChineseSimplified", fontChineseSimplified);
            fontChineseTraditional = await LoadSourceFont("RenderFontChineseTraditional");
            tmpFontChineseTraditional = await LoadFace("RenderFontChineseTraditional", fontChineseTraditional);
            fontKorean = await LoadSourceFont("RenderFontKorean");
            tmpFontKorean = await LoadFace("RenderFontKorean", fontKorean);
            fontJapanese = await LoadSourceFont("RenderFontJapanese");
            tmpFontJapanese = await LoadFace("RenderFontJapanese", fontJapanese);
            fontEnglish = await LoadSourceFont("RenderFontEnglish");
            tmpFontEnglish = await LoadFace("RenderFontEnglish", fontEnglish);

            fontsLoaded = true;
        }

        static async UniTask<Font> LoadSourceFont(string key)
        {
            try { return await Addressables.LoadAssetAsync<Font>(key).ToUniTask(); }
            catch (System.Exception e)
            {
                Debug.LogWarning("Card source font " + key + " did not load. " + e.Message);
                return null;
            }
        }

        // The address exists as a Font, but the TMP asset was never committed. Build the face from that font.
        static async UniTask<TMP_FontAsset> LoadFace(string key, Font source)
        {
            try
            {
                var asset = await Addressables.LoadAssetAsync<TMP_FontAsset>(key).ToUniTask();
                if (asset != null) return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Card face " + key + " has no TMP asset. " + e.Message);
            }
            if (source == null) return EnsureFace();
            var created = TMP_FontAsset.CreateFontAsset(source);
            return created != null ? created : EnsureFace();
        }

        public static TMP_FontAsset EnsureFace()
        {
            if (cardFace != null) return cardFace;
            var font = Resources.Load<Font>("Ultra/Fonts/Inter-Regular");
            if (font == null) return null;
            cardFace = TMP_FontAsset.CreateFontAsset(font);
            if (cardFace != null) cardFace.name = "Card face";
            return cardFace;
        }

        #endregion

        #region Card Builders

        private const string ADDR_STYLE_OCG = "Prefab/CardBuilderOCG.prefab";
        private const string ADDR_STYLE_RUSHDUEL = "Prefab/CardBuilderRushDuel.prefab";

        private static GameObject ocgBuilderPrefab;
        private static GameObject rdBuilderPrefab;
        private static bool buildersLoaded;
        private async UniTask LoadBuildersAsync()
        {
            if (buildersLoaded)
                return;
            ocgBuilderPrefab = await Addressables.LoadAssetAsync<GameObject>(ADDR_STYLE_OCG).ToUniTask();
            rdBuilderPrefab = await Addressables.LoadAssetAsync<GameObject>(ADDR_STYLE_RUSHDUEL).ToUniTask();
            buildersLoaded = true;
        }

        static TMP_FontAsset cardFace;

        static void FinishCardText(CardBuilder builder)
        {
            if (builder == null) return;
            var labels = builder.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                var label = labels[i];
                if (label == null) continue;
                if (label.font == null) label.font = EnsureFace();
                label.outlineWidth = 0f;
                label.ForceMeshUpdate(true);
            }
        }

        private CardBuilder ocgBuilder;
        private CardBuilder rdBuilder;

        private CardBuilder GetCardBuilder(CardStyle style)
        {
            currentStyle = style;
            if (ocgBuilderPrefab == null || rdBuilderPrefab == null)
            {
                Debug.LogError("[CardRenderer]: CardBuilder not loaded.");
                return null;
            }

            if (style == CardStyle.OCG_TCG)
            {
                if(ocgBuilder == null)
                    ocgBuilder = Instantiate(ocgBuilderPrefab, builder, false).GetComponent<CardBuilder>();
                if (rdBuilder != null)
                    rdBuilder.gameObject.SetActive(false);
                ocgBuilder.gameObject.SetActive(true);
                return ocgBuilder;
            }
            else
            {
                if(rdBuilder == null)
                    rdBuilder = Instantiate(rdBuilderPrefab, builder, false).GetComponent<CardBuilder>();
                if (ocgBuilder != null)
                    ocgBuilder.gameObject.SetActive(false);
                rdBuilder.gameObject.SetActive(true);
                return rdBuilder;
            }
        }

        #endregion

        #region Card Style

        public enum CardStyle
        {
            OCG_TCG,
            RUSH_DUEL
        }

        private CardStyle currentStyle;

        public static CardStyle GetCardStyleByCode(int code)
        {
            if (code >= 120000000 && code < 130000000)
                return CardStyle.RUSH_DUEL;
            var config = Config.Get("CardStyle", CardStyle.OCG_TCG.ToString());
            if (config == CardStyle.RUSH_DUEL.ToString())
                return CardStyle.RUSH_DUEL;
            return CardStyle.OCG_TCG;
        }

        #endregion

        #region Render Card 
        private void Awake()
        {
            _ = FontsReady;
            _ = LoadBuildersAsync();

            prefabIndex++;
            transform.position = new Vector3(0f, 200f * prefabIndex, 0f);
        }

        public void SwitchLanguage(string language = null)
        {
            if (!fontsLoaded)
                return;
            language ??= Language.GetCardConfig();

        }

        public static bool NeedRushDuelStyle(int code)
        {
            var config = Config.Get("CardStyle", CardStyle.OCG_TCG.ToString());
            if (config == CardStyle.RUSH_DUEL.ToString())
                return true;
            if(code >= 120000000 && code < 130000000)
                return true;
            return false;
        }

        public void RenderName(int code)
        {
            var data = CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return;
            var language = Language.GetCardConfig();
            if (data.isPre)
                language = Language.GetPrereleaseConfig();

            var builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCardName(data, language);
            FinishCardText(builder);
            renderCamera.Render();
        }

        public bool RenderCard(int code, Texture2D art, Texture2D overFrame)
        {
            var data = CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return false;
            var language = Language.GetCardConfig();
            if (data.isPre)
                language = Language.GetPrereleaseConfig();

            var builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCard(data, language, art, overFrame);
            FinishCardText(builder);
            renderCamera.Render();
            return true;
        }

        #endregion

        #region Video Card

        private int code;

        public static bool CardHasVideoArt(int code)
        {
            if (!Config.GetBool("VideoCard", true))
                return false;
            if (File.Exists(Program.PATH_VIDEO_ART + code.ToString() + Program.EXPANSION_MP4))
                return true;
            return false;
        }

        private static string GetVideoURL(int code)
        {
            string path = Program.PATH_VIDEO_ART + code.ToString() + Program.EXPANSION_MP4;
            path = Tools.FormatPlatformUrl(path);

            return path;
        }

        public async UniTask<Texture> GetVideoCardAsync(int code)
        {
            if (!CardHasVideoArt(code))
                return null;

            this.code = code;

            var data = CardsManager.GetRenderCard(code);
            if (data == null || data.Id == 0)
                return null;
            var language = Language.GetCardConfig();
            if (data.isPre)
                language = Language.GetPrereleaseConfig();

            var builder = GetCardBuilder(GetCardStyleByCode(code));
            builder.SetCard(data, language, null);
            builder.SetAllArtPartsOff();
            FinishCardText(builder);

            renderCamera.Render();
            RenderTexture.active = renderTexture;
            texFrame = new Texture2D(RenderTexture.active.width, RenderTexture.active.height, TextureFormat.RGBA32, true);
            texFrame.ReadPixels(new Rect(0, 0, RenderTexture.active.width, RenderTexture.active.height), 0, 0);
            texFrame.Apply();
            texFrame.name = "Card_" + code;
            Destroy(builder.gameObject);

            videoPlayer.gameObject.SetActive(true);
            videoPlayer.url = GetVideoURL(code);
            videoPlayer.Prepare();
            await UniTask.WaitUntil(() => videoPlayer.isPrepared);

            renderTexture = Instantiate(renderTexture);
            SetMaterial(data);
            return renderTexture;
        }

        private void SetMaterial(Card data)
        {
            matCompositeInstance = Instantiate(matComposite);
            matCompositeInstance.SetTexture("_MainTex", videoPlayer.texture);
            matCompositeInstance.SetTexture("_FrameTex", texFrame);
            matCompositeInstance.SetVector("_VideoRect", GetVideoRect(data));
        }

        private Vector4 GetVideoRect(Card data)
        {
            if (NeedRushDuelStyle(data.Id))
            {
                return new Vector4(0.049f, 0.281f, 0.902f, 0.62f);
            }
            else
            {
                if (data.HasType(CardType.Pendulum))
                    return new Vector4(0.696f, 0.2275f, 0.86f, 0.5918f);
                else
                    return new Vector4(0.1235f, 0.297f, 0.7528f, 0.5175f);
            }
        }

        private void LateUpdate()
        {
            if (matCompositeInstance == null)
                return;
            if (CardImageLoader.GetCardReferenceCount(code) == 0)
            {
                PauseVideo();
                return;
            }

            PlayVideo();
            Graphics.Blit(videoPlayer.targetTexture, renderTexture, matCompositeInstance);
        }

        public void PauseVideo()
        {
            if(!videoPlayer.isPlaying)
                return;
            videoPlayer.Pause();
        }

        public void PlayVideo()
        {
            if(videoPlayer.isPlaying)
                return;
            videoPlayer.Play();
        }

        public void Dispose()
        {
            if(matCompositeInstance != null)
            {
                Destroy(matCompositeInstance);
                Destroy(renderTexture);
                Destroy(texFrame);
            }
            Destroy(gameObject);
        }

        #endregion

    }
}
