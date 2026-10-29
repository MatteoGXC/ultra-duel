using MDPro3.Duel.YGOSharp;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using MDPro3.Utility;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    [RequireComponent(typeof(RawImage))]
    public class CardRawImageHandler : MonoBehaviour
    {
        #region 字段和属性

        public bool cache;
        public Card card;
        public int protectorCode = 1070001;

        protected bool m_Refreshed;
        public bool Refreshed => m_Refreshed;

        private RawImage m_RawImage;
        public RawImage RawImage =>
            m_RawImage = m_RawImage != null ? m_RawImage
            : GetComponent<RawImage>();

        protected Material normalMat;
        protected Material tempMat;
        private CancellationTokenSource cts;

        private int currentLoadId = 0;
        private int loadedCardId = 0;
        private bool isRenderTexture;

        #endregion

        #region Unity生命周期

        private void Awake()
        {
            SystemEvent.OnVideoCardConfigChange += OnVideoCardConfigChange;
        }

        protected void OnDestroy()
        {
            CancelCurrentLoad();
            Destroy(normalMat);
            Destroy(tempMat);
            ReleaseCard();

            SystemEvent.OnVideoCardConfigChange -= OnVideoCardConfigChange;
        }

        #endregion

        #region 公共API

        public void SetCard(int code)
        {
            if (code <= 0)
            {
                card = null;
                SetProtector(protectorCode);
            }
            else
                SetCard(CardsManager.Get(code));
        }

        public void SetCard(Card data)
        {
            if (data == null)
            {
                ReleaseCard();
                return;
            }

            if (card != null && card.Id == data.Id && m_Refreshed)
                return;

            CancelCurrentLoad();
            card = data;
            if (card != null)
            {
                _ = LoadCardPicAsync();
            }
            else
            {
                ReleaseCard();
            }
        }

        public void CancelCurrentLoad()
        {
            try
            {
                currentLoadId++;
                cts?.Cancel();
                cts?.Dispose();
            }
            finally
            {
                cts = null;
            }
        }

        public void RefreshRarity(int code)
        {
            if (card == null || card.Id != code)
                return;

            if (tempMat != null)
                DestroyImmediate(tempMat);

            if (CardRarity.GetRarity(card.Id) == CardRarity.Rarity.Normal)
            {
                if (normalMat != null)
                {
                    normalMat.SetFloat("_LoadingBlend", 0f);
                    RawImage.material = normalMat;
                }
            }
            else
            {
                _ = LoadMatAsync(0f, default);
            }
        }

        public void SetProtector(int code)
        {
            protectorCode = code;
            _ = LoadProtectorAsync(code);
        }

        #endregion

        #region 私有实现

        private void ReleaseCard()
        {
            if (loadedCardId != 0)
            {
                CardImageLoader.ReleaseCard(loadedCardId);
                loadedCardId = 0;
            }
            RawImage.texture = null;
            m_Refreshed = false;
        }

        private async UniTaskVoid LoadCardPicAsync()
        {
            if (card == null)
                return;

            int targetCardId = card.Id;
            int loadId = ++currentLoadId;
            m_Refreshed = false;

            try
            {
                cts = new CancellationTokenSource();
                var token = cts.Token;

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                if (normalMat == null)
                    normalMat = MaterialLoader.GetCardMaterial(-1);

                normalMat.SetTexture("_LoadingTex", TextureManager.container
                    .GetCardLoadingTexture(CardsManager.Get(card.Id)));
                normalMat.SetFloat("_LoadingBlend", 1f);
                RawImage.material = normalMat;

                if (tempMat != null)
                {
                    Destroy(tempMat);
                    tempMat = null;
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                var cardTex = await CardImageLoader.LoadCardAsync(
                    card.Id,
                    cache,
                    token);
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                {
                    if (cardTex != null)
                        CardImageLoader.ReleaseCard(targetCardId);
                    return;
                }

                if (cardTex == null)
                {
                    Debug.LogError($"Failed to load texture for card {card.Id}");
                    return;
                }

                if (loadedCardId != 0 && loadedCardId != card.Id)
                    CardImageLoader.ReleaseCard(loadedCardId);

                loadedCardId = card.Id;
                RawImage.texture = cardTex;
                isRenderTexture = cardTex is RenderTexture;

                if (CardRarity.GetRarity(card.Id) == CardRarity.Rarity.Normal)
                    await SetMaterialFloatAsync(normalMat, "_LoadingBlend", 0f, 0.1f, token);
                else
                    await LoadMatAsync(0.1f, token);
                RevealCardArt();
                if (card == null || card.Id != targetCardId || loadId != currentLoadId)
                    return;

                m_Refreshed = true;
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load card picture failed: {e.Message}");
                if (RawImage != null && RawImage.texture != null)
                    RevealCardArt();
            }
        }

        void RevealCardArt()
        {
            if (RawImage == null || RawImage.texture == null || normalMat == null) return;
            var current = RawImage.material;
            if (current != null && current.HasProperty("_LoadingBlend") && current.GetFloat("_LoadingBlend") <= 0.05f)
                return;
            normalMat.SetFloat("_LoadingBlend", 0f);
            RawImage.material = normalMat;
        }

        protected async UniTask LoadMatAsync(float fadeTime, CancellationToken token)
        {
            if (card == null) return;

            try
            {
                tempMat = MaterialLoader.GetCardMaterial(card.Id);
                if (tempMat == null)
                {
                    RevealCardArt();
                    return;
                }

                if (cts.Token.IsCancellationRequested || card == null)
                    return;

                tempMat.SetFloat("_LoadingBlend", 1f);
                tempMat.SetTexture("_LoadingTex", normalMat.GetTexture("_LoadingTex"));
                RawImage.material = tempMat;
                await SetMaterialFloatAsync(tempMat, "_LoadingBlend", 0f, fadeTime, token);
            }
            catch (System.OperationCanceledException)
            {
                // 任务被取消，不做处理
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Card finish material failed, showing the plain card: {e.Message}");
                RevealCardArt();
            }
        }

        private async UniTask LoadProtectorAsync(int code)
        {
            m_Refreshed = false;

            CancelCurrentLoad();
            ReleaseCard();
            card = null;

            try
            {
                var material = await ABLoader.LoadProtectorMaterial(
                    code.ToString(),
                    destroyCancellationToken);

                if (material != null)
                {
                    RawImage.material = material;
                    RawImage.texture = null;
                    m_Refreshed = true;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Load protector failed: {e.Message}");
            }
        }

        private async UniTask SetMaterialFloatAsync(Material mat, string propertyName, float endValue, float duration, CancellationToken token)
        {
            if (mat == null)
                return;

            float startValue = mat.GetFloat(propertyName);
            float elapsedTime = 0f;

            while (elapsedTime < duration && !token.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken: token);
                if (mat == null || token.IsCancellationRequested)
                    return;

                float t = elapsedTime / duration;
                float currentValue = Mathf.Lerp(startValue, endValue, t);
                mat.SetFloat(propertyName, currentValue);
                elapsedTime += Time.deltaTime;
            }

            if (mat != null && !token.IsCancellationRequested)
                mat.SetFloat(propertyName, endValue);
        }

        private void OnVideoCardConfigChange()
        {
            var config = Config.GetBool("VideoCard", true);
            if (config && CardImageLoader.CardHasVideoArt(card.Id))
                _ = ReloadAsync();
            else if (!config && isRenderTexture)
                _ = ReloadAsync();
        }

        private async UniTask ReloadAsync()
        {
            await UniTask.Yield();
            CancelCurrentLoad();
            _ = LoadCardPicAsync();
        }

        #endregion
    }
}
