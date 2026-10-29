using UnityEngine;
using static YgomSystem.UI.ColorContainer;
using YgomSystem.UI;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;

namespace MDPro3.UI
{
    public class SelectionToggle_DeckOnline : SelectionToggle_Deck
    {
        [HideInInspector] public string deckAuthor;
        [HideInInspector] public string deckId;
        [HideInInspector] public string lastDate;
        [HideInInspector] public int like;
        readonly List<int> previewReferences = new();
        int previewVersion;

        void ReleasePreviews()
        {
            foreach (int id in previewReferences) CardImageLoader.ReleaseCard(id);
            previewReferences.Clear();
        }

        protected override void OnDisable()
        {
            previewVersion++;
            base.OnDisable();
            ReleasePreviews();
        }

        public override void Dispose()
        {
            gameObject.SetActive(false);
            foreach (var component in GetComponentsInChildren<Component>(true))
                if (component != null) DOTween.Kill(component);
            DOTween.Kill(gameObject);
            base.Dispose();
        }

        public override void Refresh()
        {
            previewVersion++;
            ReleasePreviews();
            Manager.GetElement("DeckCaseIcon").SetActive(true);
            Manager.GetElement("TextDeckName").SetActive(true);
            Manager.GetElement("IconAddDeck").SetActive(false);
            Manager.GetElement("SelectedStateToggle").SetActive(false);

            Manager.GetElement<TextMeshProUGUI>("TextDeckName").text = deckName;
            Manager.GetElement<TextMeshProUGUI>("TextDeckAuthor").text = deckAuthor;
            var summary = Manager.GetElement<TextMeshProUGUI>("TextDeckDate");
            summary.text = lastDate;
            summary.enableAutoSizing = true; summary.fontSizeMin = 10; summary.fontSizeMax = 15;
            Manager.GetElement<TextMeshProUGUI>("TextDeckLike").gameObject.SetActive(false);
            foreach (var part in GetComponentsInChildren<RectTransform>(true))
                if (part.name == "LikeGroup") part.gameObject.SetActive(false);
            var entry = PremadeDeckLibrary.Find(deckId);
            var group = GetComponent<CanvasGroup>() ?? gameObject.AddComponent<CanvasGroup>();
            group.alpha = entry != null && entry.available ? 1f : .55f;

            _ = RefreshDeckCaseAsync();

            refreshed = false;
            if (pickuping)
                StartRefresh();
        }

        protected override async UniTask RefreshAsync()
        {
            int version = previewVersion;
            var token = cts != null ? cts.Token : destroyCancellationToken;
            try
            {
                await UniTask.WaitWhile(() => Program.instance.onlineDeckViewer.inTransition, cancellationToken: token);
                int[] ids = { card0, card1, card2 };
                for (int i = 0; i < ids.Length; i++)
                {
                    var image = Manager.GetElement<RawImage>("CardImage" + i);
                    image.texture = null;
                    if (ids[i] == 0) continue;
                    var texture = await CardImageLoader.LoadCardAsync(ids[i], false, token);
                    if (this == null || token.IsCancellationRequested || version != previewVersion)
                    {
                        if (texture != null) CardImageLoader.ReleaseCard(ids[i]);
                        return;
                    }
                    if (texture != null) previewReferences.Add(ids[i]);
                    image.material = null;
                    image.texture = texture;
                }
                refreshed = true;
            }
            catch (System.OperationCanceledException) { }
            catch (System.Exception e) { Debug.LogWarning("Premade cover preview: " + e.Message); }
        }

        protected override async UniTask RefreshDeckCaseAsync()
        {
            try
            {
                await UniTask.WaitWhile(
                    () => Program.instance.deckSelector.inTransition,
                    cancellationToken: destroyCancellationToken);

                int siblingIndex = transform.GetSiblingIndex();
                for (int i = 0; i < siblingIndex; i++)
                    await UniTask.Yield(cancellationToken: destroyCancellationToken);

                if (this == null || gameObject == null)
                    return;

                var sprite = await Program.items.LoadDeckCaseIconAsync(deckCase, "_L_SD");
                if (this == null || gameObject == null || sprite == null)
                    return;

                Manager.GetElement<Image>("DeckImage").sprite = sprite;
            }
            catch (System.OperationCanceledException)
            {
            }
            catch (MissingReferenceException)
            {
            }
        }

        protected override void OnClick()
        {
            AudioManager.PlaySE(SoundLabelClick);
            OpenPremade();
        }

        public void OpenPremade()
        {
            var entry = PremadeDeckLibrary.Find(deckId);
            if (entry == null || !entry.available)
            {
                UIManager.ShowPopupConfirm(new List<string> { deckName + " · " + deckAuthor, entry?.reason ?? "This premade recipe is unavailable." });
                return;
            }
            try
            {
                var deck = PremadeDeckLibrary.Prepare(entry);
                Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.PremadeDeck, entry.DeckName, deck);
                Program.instance.ShiftToServant(Program.instance.deckEditor);
            }
            catch (System.Exception e) { UIManager.ShowPopupConfirm(new List<string> { "Could not open this recipe", e.Message }); }
        }

        protected override void OnSelect(bool playSE)
        {
            HoverOn();
            if (playSE)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
            Program.instance.currentServant.lastSelectable = Selectable;
            Program.instance.onlineDeckViewer.lastSelectedDeckItem = this;
            SetColor(SelectMode.Selected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        public override void ShowPickup(bool forced = false)
        {
            if (forced)
                forcedPickup = true;
            if (pickuping)
                return;
            pickuping = true;

            ApplyShowPickup();
        }

        public override void HidePickup(bool forced = false)
        {
            if (forced)
                forcedPickup = false;
            if (!pickuping)
                return;
            if (forcedPickup)
                return;
            pickuping = false;

            ApplyHidePickup();
        }

        protected override int GetButtonsCount()
        {
            return Program.instance.onlineDeckViewer
                .GetUI<OnlineDeckViewerUI>().superScrollView.items.Count;
        }

        protected override int GetColumnsCount()
        {
            return Program.instance.onlineDeckViewer
                .GetUI<OnlineDeckViewerUI>().superScrollView.GetColumnCount();
        }
    }
}

