using Cysharp.Threading.Tasks;
using DG.Tweening;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SelectionButton_DeckSelector : SelectionButton
    {
        #region Elements

        private const string LABEL_TEXT_DECK_NAME = "TextDeckName";
        private TextMeshProUGUI m_TextDeckName;
        private TextMeshProUGUI TextDeckName =>
            m_TextDeckName = m_TextDeckName != null ? m_TextDeckName
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TEXT_DECK_NAME);

        private const string LABEL_IMG_DECK = "DeckImage";
        private Image m_ImageDeck;
        private Image ImageDeck =>
            m_ImageDeck = m_ImageDeck != null ? m_ImageDeck
            : Manager.GetElement<Image>(LABEL_IMG_DECK);

        private const string LABEL_RIMG_CARD0 = "CardImage0";
        private RawImage m_ImageCard0;
        private RawImage ImageCard0 =>
            m_ImageCard0 = m_ImageCard0 != null ? m_ImageCard0
            : Manager.GetElement<RawImage>(LABEL_RIMG_CARD0);
        private RectTransform m_CardPos0RT;
        private RectTransform CardPos0RT =>
            m_CardPos0RT = m_CardPos0RT != null ? m_CardPos0RT
            : Manager.GetElement<RectTransform>(LABEL_RIMG_CARD0);

        private const string LABEL_RIMG_CARD1 = "CardImage1";
        private RawImage m_ImageCard1;
        private RawImage ImageCard1 =>
            m_ImageCard1 = m_ImageCard1 != null ? m_ImageCard1
            : Manager.GetElement<RawImage>(LABEL_RIMG_CARD1);
        private RectTransform m_CardPos1RT;
        private RectTransform CardPos1RT =>
            m_CardPos1RT = m_CardPos1RT != null ? m_CardPos1RT
            : Manager.GetElement<RectTransform>(LABEL_RIMG_CARD1);

        private const string LABEL_RIMG_CARD2 = "CardImage2";
        private RawImage m_ImageCard2;
        private RawImage ImageCard2 =>
            m_ImageCard2 = m_ImageCard2 != null ? m_ImageCard2
            : Manager.GetElement<RawImage>(LABEL_RIMG_CARD2);
        private RectTransform m_CardPos2RT;
        private RectTransform CardPos2RT =>
            m_CardPos2RT = m_CardPos2RT != null ? m_CardPos2RT
            : Manager.GetElement<RectTransform>(LABEL_RIMG_CARD2);

        private const string LABEL_CG_CARD_POS0 = "CardPos0";
        private CanvasGroup m_CardPos0;
        private CanvasGroup CardPos0 =>
            m_CardPos0 = m_CardPos0 != null ? m_CardPos0
            : Manager.GetElement<CanvasGroup>(LABEL_CG_CARD_POS0);

        private const string LABEL_CG_CARD_POS1 = "CardPos1";
        private CanvasGroup m_CardPos1;
        private CanvasGroup CardPos1 =>
            m_CardPos1 = m_CardPos1 != null ? m_CardPos1
            : Manager.GetElement<CanvasGroup>(LABEL_CG_CARD_POS1);

        private const string LABEL_CG_CARD_POS2 = "CardPos2";
        private CanvasGroup m_CardPos2;
        private CanvasGroup CardPos2 =>
            m_CardPos2 = m_CardPos2 != null ? m_CardPos2
            : Manager.GetElement<CanvasGroup>(LABEL_CG_CARD_POS2);

        #endregion

        private readonly List<Tweener> pickupTweens = new();
        private readonly List<Tweener> pickdownTweens = new();

        private CancellationTokenSource cts;
        private Deck deck;
        private string deckName;
        private bool refreshed;

        protected override void Awake()
        {
            base.Awake();
            HidePickup();
        }

        private void OnEnable()
        {
            if(!refreshed)
                SetDeck(deck, TextDeckName.text);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DisposeRefreshCts();
        }

        public void SetConfigDeck(string hint)
        {
            var configDeck = Config.GetConfigDeckName();
            var path = Program.PATH_DECK + configDeck + Program.EXPANSION_YDK;
            if (!File.Exists(path))
                SetDeck(null, hint);
            else
                SetDeck(new Deck(path), configDeck);
        }

        public void SetDeck(Deck deck, string deckName)
        {
            this.deck = deck;
            this.deckName = deckName;

            if (gameObject.activeInHierarchy)
                SetDeckInteral();
        }

        private void SetDeckInteral()
        {
            if(deckName.StartsWith("/"))
                TextDeckName.text = Path.GetFileNameWithoutExtension(deckName);
            else
                TextDeckName.text = deckName;
            if(deck == null)
                _ = RefreshAsync();
            else
            {
                _ = RefreshAsync(
                    deck.Case,
                    deck.Protector,
                    deck.Pickup.Count > 0 ? deck.Pickup[0] : 0,
                    deck.Pickup.Count > 1 ? deck.Pickup[1] : 0,
                    deck.Pickup.Count > 2 ? deck.Pickup[2] : 0);
            }
        }

        private async UniTask RefreshAsync(int deckCase = 1080001, int protector = 1070001, int card0 = 0, int card1 = 0, int card2 = 0)
        {
            refreshed = false;

            DisposeRefreshCts();
            var refreshCts = new CancellationTokenSource();
            cts = refreshCts;
            var token = refreshCts.Token;

            try
            {
                await UniTask.WaitUntil(() => Items.initialized, cancellationToken: token);
                await UniTask.WaitWhile(() => TextureManager.container == null, cancellationToken: token);

                ImageDeck.color = Color.clear;
                ImageCard0.color =Color.clear;
                ImageCard1.color = Color.clear;
                ImageCard2.color = Color.clear;

                ImageDeck.sprite = await Program.items.LoadDeckCaseIconAsync(deckCase, "_L_SD");
                ImageDeck.color = Color.white;
                ImageDeck.enabled = true;
                UltraDeckBackdrop.Apply(transform as RectTransform, deck, deckName);

                if (card0 == 0)
                {
                    ImageCard0.texture = null;
                    ImageCard0.material = await ABLoader.LoadProtectorMaterial(protector.ToString(), token);
                }
                else
                {
                    ImageCard0.material = MaterialLoader.GetCardMaterial(card0);
                    ImageCard0.texture = await CardImageLoader.LoadCardAsync(card0, true);
                }
                ImageCard0.color = Color.white;

                if (card1 == 0)
                {
                    ImageCard1.texture = null;
                    ImageCard1.material = await ABLoader.LoadProtectorMaterial(protector.ToString(), token);
                }
                else
                {
                    ImageCard1.material = MaterialLoader.GetCardMaterial(card1);
                    ImageCard1.texture = await CardImageLoader.LoadCardAsync(card1, true);
                }
                ImageCard1.color = Color.white;

                if (card2 == 0)
                {
                    ImageCard2.texture = null;
                    ImageCard2.material = await ABLoader.LoadProtectorMaterial(protector.ToString(), token);
                }
                else
                {
                    ImageCard2.material = MaterialLoader.GetCardMaterial(card2);
                    ImageCard2.texture = await CardImageLoader.LoadCardAsync(card2, true);
                }
                ImageCard2.color = Color.white;

                refreshed = true;
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                if (ReferenceEquals(cts, refreshCts))
                {
                    cts = null;
                    try
                    {
                        refreshCts.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        private void DisposeRefreshCts()
        {
            var refreshCts = cts;
            if (refreshCts == null)
                return;

            cts = null;

            try
            {
                refreshCts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            try
            {
                refreshCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            ShowPickup();
        }

        protected override void CallHoverOffEvent()
        {
            base.CallHoverOffEvent();
            HidePickup();
        }

        private void ShowPickup()
        {
            foreach (var tween in pickdownTweens)
                if (tween.IsActive())
                    tween.Kill();
            pickdownTweens.Clear();

            var tween1 = CardPos0.DOFade(1f, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween1);
            var tween2 = CardPos1.DOFade(1f, 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween2);
            var tween3 = CardPos2.DOFade(1f, 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween3);

            var tween4 = CardPos0RT.DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween4);
            var tween5 = CardPos1RT.DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween5);
            var tween6 = CardPos2RT.DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween6);

            var tween7 = CardPos0RT.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween7);
            var tween8 = CardPos2RT.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween8);
        }

        private void HidePickup()
        {
            foreach (var tween in pickupTweens)
                if (tween.IsActive())
                    tween.Kill();
            pickupTweens.Clear();

            var tween1 = CardPos0.DOFade(0f, 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween1);
            var tween2 = CardPos1.DOFade(0f, 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween2);
            var tween3 = CardPos2.DOFade(0f, 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween3);

            var tween4 = CardPos0RT.DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween4);
            var tween5 = CardPos1RT.DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween5);
            var tween6 = CardPos2RT.DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween6);

            var tween7 = CardPos0RT.DOLocalRotate(new Vector3(0f, 0f, -20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween7);
            var tween8 = CardPos2RT.DOLocalRotate(new Vector3(0f, 0f, 20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween8);
        }

    }
}
