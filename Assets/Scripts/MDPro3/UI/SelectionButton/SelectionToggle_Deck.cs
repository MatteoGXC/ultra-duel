using UnityEngine;
using static YgomSystem.UI.ColorContainer;
using YgomSystem.UI;
using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using MDPro3.Servant;
using MDPro3.UI.ServantUI;
using MDPro3.Utility;
using Cysharp.Threading.Tasks;

namespace MDPro3.UI
{
    public class SelectionToggle_Deck : SelectionToggle_ScrollRectItem
    {
        [HideInInspector] public string deckName;
        [HideInInspector] public int deckCase;
        [HideInInspector] public int card0;
        [HideInInspector] public int card1;
        [HideInInspector] public int card2;
        [HideInInspector] public string protector;
        [HideInInspector] public bool toggleMode;
        protected bool pickuping;
        protected bool forcedPickup;
        protected IEnumerator enumeratorCase;
        private readonly List<Tweener> pickupTweens = new();
        private readonly List<Tweener> pickdownTweens = new();

        protected override void Awake()
        {
            base.Awake();
            exclusiveToggle = false;
            manuallySetNavigation = false;
            simpleMove = false;
            selectedWhenHover = true;
            ApplyHideToggle();
            ApplyHidePickup(true);
        }

        public override void Refresh()
        {
            if(index == 0)
            {
                Manager.GetElement("DeckCaseIcon").SetActive(false);
                Manager.GetElement("TextDeckName").SetActive(false);
                Manager.GetElement("IconAddDeck").SetActive(true);
                Manager.GetElement("SelectedStateToggle").SetActive(false);

                SoundLabelPointerEnter = string.Empty;
                SoundLabelSelectedGamePad = "SE_MENU_OVERLAP_02";
            }
            else
            {
                Manager.GetElement("DeckCaseIcon").SetActive(true);
                Manager.GetElement("TextDeckName").SetActive(true);
                Manager.GetElement("IconAddDeck").SetActive(false);
                Manager.GetElement("SelectedStateToggle").SetActive(toggleMode);

                SoundLabelPointerEnter = "SE_DECK_CARD_SELECT";
                SoundLabelSelectedGamePad = "SE_DECK_CARD_SELECT";
                Manager.GetElement("IconOn").SetActive(isOn);

                Manager.GetElement<TextMeshProUGUI>("TextDeckName").text = deckName;

                if (forcedPickup && !pickuping)
                {
                    ShowPickup(true);
                }
            }

            _ = RefreshDeckCaseAsync();

            refreshed = false;
            if (pickuping)
                StartRefresh();
        }

        protected virtual void StartRefresh()
        {
            if (gameObject.activeInHierarchy)
            {
                try
                {
                    cts?.Cancel();
                    cts?.Dispose();
                }
                catch { };
                cts = new();
                _ = RefreshAsync();
            }
        }

        protected virtual async UniTask RefreshDeckCaseAsync()
        {
            if (index == 0)
                return;

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

        protected override async UniTask RefreshAsync()
        {
            if (index == 0)
                return;

            await UniTask.WaitWhile(() => Program.instance.deckSelector.inTransition);

            Material pMat = null;
            var cardImage0 = Manager.GetElement<RawImage>("CardImage0");
            if (card0 != 0)
            {
                cardImage0.texture = await CardImageLoader.LoadCardAsync(card0, true);
                //cardImage0.material = await MaterialLoader.LoadCardMaterialAsync(card0, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage0.texture = null;
                cardImage0.material = pMat;
            }

            var cardImage1 = Manager.GetElement<RawImage>("CardImage1");
            if (card1 != 0)
            {
                cardImage1.texture = await CardImageLoader.LoadCardAsync(card1, true);
                //cardImage1.material = await MaterialLoader.LoadCardMaterialAsync(card1, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage1.texture = null;
                cardImage1.material = pMat;
            }

            var cardImage2 = Manager.GetElement<RawImage>("CardImage2");
            if (card2 != 0)
            {
                cardImage2.texture = await CardImageLoader.LoadCardAsync(card2, true);
                //cardImage2.material = await MaterialLoader.LoadCardMaterialAsync(card2, cts.Token);
            }
            else
            {
                if (pMat == null)
                    pMat = await ABLoader.LoadProtectorMaterial(protector, cts.Token);
                cardImage2.texture = null;
                cardImage2.material = pMat;
            }
        }

        protected override void OnClick()
        {
            if(index == 0)
            {
                AudioManager.PlaySE(SoundLabelClick);
                Program.instance.deckSelector.GetUI<DeckSelectorUI>().DeckCreate();
                return;
            }

            if(toggleMode)
            {
                isOn = !isOn;
                if (isOn)
                {
                    Manager.GetElement("IconOn").SetActive(true);
                    Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "1";
                }
                else
                {
                    Manager.GetElement("IconOn").SetActive(false);
                    Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "0";
                }
                AudioManager.PlaySE(isOn ? SoundLabelClickOn : SoundLabelClickOff);
            }
            else
            {
                AudioManager.PlaySE(SoundLabelClick);
                Config.SetConfigDeck(deckName, true);
                if (DeckSelector.condition == DeckSelector.Condition.ForEdit)
                {
                    Program.instance.deckEditor.SwitchCondition(DeckEditor.Condition.EditDeck);
                    Program.instance.ShiftToServant(Program.instance.deckEditor);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.MyCard)
                {
                    Program.instance.ShiftToServant(Program.instance.online);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.ForDuel)
                {
                    Program.instance.ShiftToServant(Program.instance.room);
                }
                else if (DeckSelector.condition == DeckSelector.Condition.ForSolo)
                {
                    if (!SoloSelector.HasFormatOverride)
                        DuelFormat.ApplyDeckFormat(Config.GetConfigDeckName());
                    Program.instance.ShiftToServant(Program.instance.solo);
                }
            }
        }

        protected override void OnSubmit()
        {
            OnClick();
        }

        protected override void OnSelect(bool playSE)
        {
            HoverOn();
            if (playSE)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
            Program.instance.currentServant.lastSelectable = Selectable;
            Program.instance.deckSelector.lastSelectedDeckItem = this;
            foreach (var ccg in transform.GetComponentsInChildren<ColorContainerGraphic>(true))
                ccg.SetColor(SelectMode.Selected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        public virtual void ShowPickup(bool forced = false)
        {
            if (forced)
                forcedPickup = true;
            if (index == 0)
                return;
            if (pickuping)
                return;
            pickuping = true;

            ApplyShowPickup();
        }

        protected virtual void ApplyShowPickup()
        {
            foreach (var tween in pickdownTweens)
                if (tween.IsActive())
                    tween.Kill();
            pickdownTweens.Clear();

            var tween1 = Manager.GetElement<CanvasGroup>("CardPos0").DOFade(1f, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween1);
            var tween2 = Manager.GetElement<CanvasGroup>("CardPos1").DOFade(1f, 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween2);
            var tween3 = Manager.GetElement<CanvasGroup>("CardPos2").DOFade(1f, 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween3);

            var tween4 = Manager.GetElement<RectTransform>("CardImage0").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween4);
            var tween5 = Manager.GetElement<RectTransform>("CardImage1").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween5);
            var tween6 = Manager.GetElement<RectTransform>("CardImage2").DOAnchorPos3D(new Vector3(0f, 10f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween6);

            var tween7 = Manager.GetElement<RectTransform>("CardImage0").DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween7);
            var tween8 = Manager.GetElement<RectTransform>("CardImage2").DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutCubic);
            pickupTweens.Add(tween8);

            if(!refreshed)
                StartRefresh();
        }

        public virtual void HidePickup(bool forced = false)
        {
            if (index == 0)
                return;
            if (forced)
                forcedPickup = false;
            if (!pickuping) 
                return;
            if (forcedPickup)
                return;
            pickuping = false;

            ApplyHidePickup();
        }

        protected virtual void ApplyHidePickup(bool instant = false)
        {
            foreach (var tween in pickupTweens)
                if (tween.IsActive())
                    tween.Kill();
            pickupTweens.Clear();

            var tween1 = Manager.GetElement<CanvasGroup>("CardPos0").DOFade(0f, instant ? 0f : 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween1);
            var tween2 = Manager.GetElement<CanvasGroup>("CardPos1").DOFade(0f, instant ? 0f : 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween2);
            var tween3 = Manager.GetElement<CanvasGroup>("CardPos2").DOFade(0f, instant ? 0f : 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween3);

            var tween4 = Manager.GetElement<RectTransform>("CardImage0").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween4);
            var tween5 = Manager.GetElement<RectTransform>("CardImage1").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.22f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween5);
            var tween6 = Manager.GetElement<RectTransform>("CardImage2").DOAnchorPos3D(new Vector3(0f, -40f, 0f), 0.24f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween6);

            var tween7 = Manager.GetElement<RectTransform>("CardImage0").DOLocalRotate(new Vector3(0f, 0f, -20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween7);
            var tween8 = Manager.GetElement<RectTransform>("CardImage2").DOLocalRotate(new Vector3(0f, 0f, 20f), 0.2f).SetEase(Ease.OutCubic);
            pickdownTweens.Add(tween8);
        }

        public void ShowToggle()
        {
            if(index == 0)
            {
                return;
            }

            ApplyShowToggle();
        }

        private void ApplyShowToggle()
        {
            toggleMode = true;
            isOn = false;
            Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items[index].args[6] = "0";

            Manager.GetElement("SelectedStateToggle").SetActive(true);
            Manager.GetElement("IconOn").SetActive(false);
        }

        public void HideToggle()
        {
            ApplyHideToggle();
        }

        private void ApplyHideToggle()
        {
            toggleMode = false;
            Manager.GetElement("SelectedStateToggle").SetActive(false);
            Manager.GetElement("IconOn").SetActive(false);
            isOn = false;
        }

        public override void ToggleOnNow()
        {
        }

        public override void ToggleOffNow()
        {
        }

        protected override void ToggleOn()
        {
        }

        protected override void ToggleOff()
        {
        }

        protected override void HoverOn()
        {
            base.HoverOn();
            ShowPickup();
        }

        protected override void HoverOff(bool force = false)
        {
            base.HoverOff();
            HidePickup();
        }

        protected override int GetButtonsCount()
        {
            return Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.items.Count;
        }

        protected override int GetColumnsCount()
        {
            return Program.instance.deckSelector.GetUI<DeckSelectorUI>().superScrollView.GetColumnCount();
        }

    }
}
