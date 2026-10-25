using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using YgomSystem.UI;
using DG.Tweening;
using static YgomSystem.UI.ColorContainer;
using UnityEngine.Events;
using TMPro;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(ElementObjectManager))]
    public class SelectionButton : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler,
        IPointerDownHandler, IPointerUpHandler,
        IPointerClickHandler, ISubmitHandler,
        ISelectHandler, IDeselectHandler,
        IMoveHandler
    {

        #region Elements

        private ElementObjectManager m_Manager;
        protected ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private Selectable m_Selectable;
        protected Selectable Selectable =>
            m_Selectable = m_Selectable != null ? m_Selectable
            : GetComponent<Selectable>();


        private const string LABEL_CG_HOVER = "Hover";
        private CanvasGroup m_Hover;
        private CanvasGroup Hover =>
            m_Hover = m_Hover != null ? m_Hover
            : Manager.GetElement<CanvasGroup>(LABEL_CG_HOVER);
        private DOTweenAnimation m_HoverAnimation;
        private DOTweenAnimation HoverAnimation =>
            m_HoverAnimation = m_HoverAnimation != null ? m_HoverAnimation
            : Manager.GetElement<DOTweenAnimation>(LABEL_CG_HOVER);

        private const string LABEL_CG_SELECTCURSOROFFSET = "SelectCursorOffset";
        private CanvasGroup m_SelectCursorOffset;
        protected CanvasGroup SelectCursorOffset =>
            m_SelectCursorOffset = m_SelectCursorOffset != null ? m_SelectCursorOffset
            : Manager.GetElement<CanvasGroup>(LABEL_CG_SELECTCURSOROFFSET);
        private DOTweenAnimation m_SelectCursorOffsetAnimation;
        protected DOTweenAnimation SelectCursorOffsetAnimation =>
            m_SelectCursorOffsetAnimation = m_SelectCursorOffsetAnimation != null ? m_SelectCursorOffsetAnimation
            : Manager.GetElement<DOTweenAnimation>(LABEL_CG_SELECTCURSOROFFSET);

        private const string LABEL_RT_CORNER = "Corner";
        private RectTransform m_Corner;
        private RectTransform Corner =>
            m_Corner = m_Corner != null ? m_Corner
            : Manager.GetElement<RectTransform>(LABEL_RT_CORNER);

        private const string LABEL_RT_ARROW = "Arrow";
        private RectTransform m_Arrow;
        private RectTransform Arrow =>
            m_Arrow = m_Arrow != null ? m_Arrow
            : Manager.GetElement<RectTransform>(LABEL_RT_ARROW);

        private const string LABEL_TXT_BUTTONTEXT = "ButtonText";
        private TextMeshProUGUI m_ButtonText;
        protected TextMeshProUGUI ButtonText =>
            m_ButtonText = m_ButtonText != null ? m_ButtonText
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_BUTTONTEXT);

        private const string LABEL_IMG_ICON = "Icon";
        private Image m_Icon;
        protected Image Icon =>
            m_Icon = m_Icon != null ? m_Icon
            : Manager.GetElement<Image>(LABEL_IMG_ICON);

        #endregion

        [Header("Selection Button")]
        public int index = -1;
        [SerializeField] protected bool SetToResponser = true;
        [SerializeField] protected bool manuallySetNavigation = true;

        [SerializeField] protected string SoundLabelClick;
        [SerializeField] protected string SoundLabelClickInactive;
        [SerializeField] protected string SoundLabelPointerEnter;
        [SerializeField] protected string SoundLabelSelectedGamePad;

        [SerializeField] protected SelectionButtonNavigationEvent navigationEvent;
        [SerializeField] protected SelectionButtonHoverEvent hoverEvent;
        [SerializeField] protected SelectionButtonSelectEvent selectEvent;
        [SerializeField] protected SelectionButtonClickEvent clickEvent;


        protected List<Tweener> hoverOnTweens = new();
        protected List<Tweener> hoverOffTweens = new();
        protected bool hovering;
        protected bool pressing;
        protected bool hoverd;
        RectTransform pressPlate;
        Vector3 pressRest = Vector3.one;
        Tweener pressTween;
        public bool selected;

        protected bool clickIsSubmit = true;
        protected bool selectedWhenHover;
        protected bool pointerExitThenDeselect = true;

        protected bool nonPersistentNavigationEventAdded;

        #region InterFace Implementation

        public virtual void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                OnClick();
            else if (eventData.button == PointerEventData.InputButton.Right)
                clickEvent.onRightClick?.Invoke();
            else if (eventData.button == PointerEventData.InputButton.Middle)
                clickEvent.onMiddleClick?.Invoke();
        }

        public virtual void OnPointerEnter(PointerEventData eventData)
        {
            hovering = true;
            OnEnter();
        }

        public virtual void OnPointerExit(PointerEventData eventData)
        {
            hovering = false;
            if (pressing) ReleasePress();
            pressing = false;
            OnExit();
        }

        public virtual void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                pressing = true;
                OnDown();
            }
        }

        public virtual void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                pressing = false;
                OnUp();
            }
        }

        public virtual void OnSubmit(BaseEventData eventData)
        {
            OnSubmit();
        }

        public virtual void OnSelect(BaseEventData eventData)
        {
            selected = true;
            if (eventData is not PointerEventData || !hovering)
            {
                OnSelect(eventData is AxisEventData || UserInput.NextSelectionIsAxis);
                UserInput.NextSelectionIsAxis = false;
            }
        }

        public virtual void OnDeselect(BaseEventData eventData)
        {
            selected = false;
            OnDeselect();
        }

        public virtual void OnMove(AxisEventData eventData)
        {
            OnNavigation(eventData);
        }

        #endregion

        #region Input Control

        protected virtual void Awake()
        {
            SetColor(SelectMode.Unselected, StatusMode.Normal, Selectable.interactable);

            HoverOff(true);
        }

        protected virtual void OnDisable()
        {
            pressTween?.Kill();
            if (pressPlate != null) pressPlate.localScale = pressRest;
            SetColor(SelectMode.Unselected, StatusMode.Normal, Selectable.interactable);
        }

        /// <summary>
        /// 仅被鼠标点击触发；
        /// </summary>
        protected virtual void OnClick()
        {
            AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
            if (SetToResponser)
            {
                if (Program.instance.ui_.currentPopupB != null)
                    Program.instance.ui_.currentPopupB.lastSelectable = m_Selectable;
                else
                    Program.instance.currentServant.lastSelectable = Selectable;
            }
            clickEvent.onLeftClick?.Invoke();
        }

        /// <summary>
        /// 选中时，键盘回车、手柄A键触发
        /// </summary>
        protected virtual void OnSubmit()
        {
            AudioManager.PlaySE(Selectable.interactable ? SoundLabelClick : SoundLabelClickInactive);
        }

        /// <summary>
        /// 鼠标进入时触发
        /// 调用HoverOn();
        /// </summary>
        protected virtual void OnEnter()
        {
            HoverOn();
            AudioManager.PlaySE(SoundLabelPointerEnter);
            SetColor(selected ? SelectMode.Selected : SelectMode.Unselected, StatusMode.Enter, Selectable.interactable);
            if (selectedWhenHover && !UserInput.InputFieldActivating())
                Selectable.Select();
        }

        /// <summary>
        /// 鼠标离开时触发，鼠标未锁定时，自动取消所有物品的选中状态
        /// 调用HoverOff();
        /// </summary>
        protected virtual void OnExit()
        {
            if (!Program.Running)
                return;
            if (pointerExitThenDeselect)
                if (Cursor.lockState == CursorLockMode.None)
                    if(EventSystem.current.currentSelectedGameObject == gameObject)
                            EventSystem.current.SetSelectedGameObject(null);
            HoverOff();
            SetColor(selected ? SelectMode.Selected : SelectMode.Unselected, StatusMode.Normal, Selectable.interactable);
        }

        /// <summary>
        /// 鼠标按下时触发
        /// </summary>
        protected virtual void OnDown()
        {
            SetColor(selected ? SelectMode.Selected : SelectMode.Unselected, StatusMode.Down, Selectable.interactable);
            var plate = PressPlate();
            if (plate == null) return;
            pressTween?.Kill();
            pressTween = plate.DOScale(pressRest * 0.95f, 0.07f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>
        /// 鼠标抬起时触发
        /// </summary>
        protected virtual void OnUp()
        {
            SetColor(selected ? SelectMode.Selected : SelectMode.Unselected
                , hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
            ReleasePress();
        }

        RectTransform PressPlate()
        {
            if (pressPlate != null) return pressPlate;
            if (GetType().Name.Contains("Card")) return null;
            pressPlate = transform.Find("DisplayGroup") as RectTransform;
            if (pressPlate == null && transform.childCount > 0) pressPlate = transform.GetChild(0) as RectTransform;
            if (pressPlate != null) pressRest = pressPlate.localScale;
            return pressPlate;
        }

        void ReleasePress()
        {
            if (pressPlate == null) return;
            pressTween?.Kill();
            pressTween = pressPlate.DOScale(pressRest, 0.14f).SetEase(Ease.OutCubic).SetUpdate(true);
        }

        /// <summary>
        /// 手柄方向键选中时触发
        /// 调用HoverOn();
        /// </summary>
        /// <param name="playSE">是否播放选中音效</param>
        protected virtual void OnSelect(bool playSE)
        {
            HoverOn();
            selectEvent.onSelect?.Invoke();
            if (playSE)
                AudioManager.PlaySE(SoundLabelSelectedGamePad);
            if (SetToResponser)
            {
                if (Program.instance.ui_.currentPopupB != null)
                    Program.instance.ui_.currentPopupB.lastSelectable = Selectable;
                else
                    Program.instance.currentServant.lastSelectable = Selectable;
            }

            SetColor(SelectMode.Selected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        /// <summary>
        /// 脱离选中时触发
        /// 调用HoverOff();
        /// </summary>
        protected virtual void OnDeselect()
        {
            HoverOff();
            selectEvent.onDeselect?.Invoke();
            SetColor(SelectMode.Unselected, hovering ? StatusMode.Enter : StatusMode.Normal, Selectable.interactable);
        }

        #endregion

        #region Input Reaction

        /// <summary>
        /// 鼠标进入或者被选中时触发；
        /// 调用HoverEvent()。
        /// </summary>
        protected virtual void HoverOn()
        {
            if (hoverd)
                return;
            hoverd = true;

            foreach (var tween in hoverOffTweens)
                if (tween.IsActive())
                    tween.Kill();
            hoverOffTweens.Clear();

            if(SelectCursorOffset != null)
                SelectCursorOffset.alpha = 1.0f;
            if (SelectCursorOffsetAnimation != null)
                SelectCursorOffsetAnimation.DORestart();

            if(Corner != null)
            {
                Corner.offsetMin = new Vector2(-16f, -16f);
                Corner.offsetMax = new Vector2(16f, 16f);
                hoverOnTweens.Add(Corner.DOSizeDelta(Vector2.zero, 0.16f).SetEase(Ease.OutCubic));
            }

            if(Hover != null)
                Hover.alpha = 1f;
            if(HoverAnimation != null)
                HoverAnimation.DORestart();

            if(Arrow != null)
            {
                Arrow.anchoredPosition3D = new Vector3(-12f, 0f, 0f);
                hoverOnTweens.Add(Arrow.DOAnchorPos3D(Vector3.zero, 0.16f).SetEase(Ease.OutCubic));
            }

            CallHoverOnEvent();
        }

        /// <summary>
        /// 鼠标离开或脱离选中状态时触发
        /// </summary>
        protected virtual void HoverOff(bool forced = false)
        {
            if(forced)
                if (!hoverd)
                    return;

            hoverd = false;

            foreach (var tween in hoverOnTweens)
                if (tween.IsActive())
                    tween.Kill();
            hoverOnTweens.Clear();

            if (SelectCursorOffsetAnimation != null)
                SelectCursorOffsetAnimation.DOPause();
            if (SelectCursorOffset != null)
                SelectCursorOffset.alpha = 0f;

            if (HoverAnimation != null)
                HoverAnimation.DOPause();
            if (Hover != null)
                Hover.alpha = 0f;

            if(Arrow != null)
                hoverOffTweens.Add(Arrow.DOAnchorPos3D(Vector3.zero, 0.16f).SetEase(Ease.OutCubic));

            CallHoverOffEvent();
        }

        protected virtual void CallHoverOnEvent()
        {
            hoverEvent.onHoverOff?.Invoke();
        }

        protected virtual void CallHoverOffEvent()
        {
            hoverEvent.onHoverOff?.Invoke();
        }

        #endregion

        #region Public Function

        public virtual void SetClickEvent(UnityAction call)
        {
            if (Selectable is Button button)
                button.onClick.AddListener(call);
            else
                clickEvent.onLeftClick.AddListener(call);
        }

        public virtual void SetRightClickEvent(UnityAction call)
        {
            clickEvent.onRightClick.AddListener(call);
        }

        public virtual void SetMiddleClickEvent(UnityAction call)
        {
            clickEvent.onMiddleClick.AddListener(call);
        }

        public virtual void RemoveAllListeners()
        {
            if (Selectable is Button button)
                button.onClick.RemoveAllListeners();
        }

        public virtual void SetNavigationEvent(MoveDirection direction, UnityAction call)
        {
            nonPersistentNavigationEventAdded = true;
            switch (direction)
            {
                case MoveDirection.Left:
                    navigationEvent.onLeftNavigation.AddListener(call);
                    break;
                case MoveDirection.Right:
                    navigationEvent.onRightNavigation.AddListener(call);
                    break;
                case MoveDirection.Up:
                    navigationEvent.onUpNavigation.AddListener(call);
                    break;
                case MoveDirection.Down:
                    navigationEvent.onDownNavigation.AddListener(call);
                    break;
            }
        }

        public virtual void SetButtonText(string title)
        {
            if(ButtonText != null)
            {
                ButtonText.text = title;
                if (!GetType().Name.Contains("Card"))
                {
                    UltraTheme.Style(ButtonText, true, 8f);
                    if (UltraTheme.IsDanger(title)) ButtonText.color = UltraTheme.Danger;
                }
            }
        }

        public virtual string GetButtonText()
        {
            if(ButtonText != null)
                return ButtonText.text;
            return string.Empty;
        }

        public virtual void SetButtonTextColor(Color color)
        {
            if (ButtonText != null)
                ButtonText.color = color;
        }

        public virtual Color GetButtonTextColor()
        {
            if (ButtonText != null)
                return ButtonText.color;
            return Color.white;
        }

        public virtual Sprite GetIconSprite()
        {
            if(Icon != null)
                return Icon.sprite;
            return null;
        }

        public virtual void SetIconSprite(Sprite sprite)
        {
            if(Icon != null)
                Icon.sprite = sprite;
        }

        public virtual void ShowIcon(bool show)
        {
            if(Icon != null)
                Icon.gameObject.SetActive(show);
        }

        public virtual void SetHoverOnEvent(UnityAction call)
        {
            hoverEvent.onHoverOn.AddListener(call);
        }

        public virtual void SetHoverOffEvent(UnityAction call)
        {
            hoverEvent.onHoverOff.AddListener(call);
        }

        public virtual void SetSelectEvent(UnityAction call)
        {
            selectEvent.onSelect.AddListener(call);
        }

        public virtual void SetDeselectEvent(UnityAction call)
        {
            selectEvent.onDeselect.AddListener(call);
        }

        private ShortcutIcon[] shortcutIcons;
        public virtual void SetInteractable(bool active)
        {
            Selectable.interactable = active;
            SetColor(selected ? SelectMode.Selected : SelectMode.Unselected
                , hovering ? StatusMode.Enter : StatusMode.Normal, active);
            shortcutIcons ??= transform.GetComponentsInChildren<ShortcutIcon>(true);
            foreach (var shortcut in shortcutIcons)
                shortcut.Show = active;
        }

        public Selectable GetSelectable()
        {
            return Selectable;
        }

        public virtual void ResetVisualState(bool clearEventSelection = false)
        {
            hovering = false;
            pressing = false;
            selected = false;

            if (clearEventSelection && EventSystem.current != null
                && EventSystem.current.currentSelectedGameObject == gameObject)
                EventSystem.current.SetSelectedGameObject(null);

            HoverOff();
            SetColor(SelectMode.Unselected, StatusMode.Normal, Selectable.interactable);
        }

        #endregion

        #region Navigation

        /// <summary>
        /// 选中时响应方向键；
        /// 注册了对应方向的SelectionButtonNavigationEvent就执行Event并返回。
        /// 如果manuallySetNavigation，即手动设置了Selectable的Navigation，就链式选中对应方向上的激活按钮（自动跳过未激活按钮）；
        /// 否则，依照GetRowsNum() GetColumnsNum() GetButtonsCount()获取的行列数和按钮总数自动导航。
        /// </summary>
        /// <param name="eventData"></param>
        protected virtual void OnNavigation(AxisEventData eventData)
        {
            if (eventData.moveDir == MoveDirection.Up)
            {
                if (navigationEvent.onUpNavigation.GetPersistentEventCount() > 0
                    || nonPersistentNavigationEventAdded)
                {
                    navigationEvent.onUpNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Down)
            {
                if (navigationEvent.onDownNavigation.GetPersistentEventCount() > 0
                    || nonPersistentNavigationEventAdded)
                {
                    navigationEvent.onDownNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Left)
            {
                if (navigationEvent.onLeftNavigation.GetPersistentEventCount() > 0
                    || nonPersistentNavigationEventAdded)
                {
                    navigationEvent.onLeftNavigation.Invoke();
                    return;
                }
            }
            else if (eventData.moveDir == MoveDirection.Right)
            {
                if (navigationEvent.onRightNavigation.GetPersistentEventCount() > 0
                    || nonPersistentNavigationEventAdded)
                {
                    navigationEvent.onRightNavigation.Invoke();
                    return;
                }
            }

            if (manuallySetNavigation)
            {
                if (eventData.moveDir == MoveDirection.Left && Selectable.navigation.selectOnLeft != null && !Selectable.navigation.selectOnLeft.gameObject.activeSelf)
                {
                    var nextSeletable = Selectable.navigation.selectOnLeft;
                    while (nextSeletable != null && !nextSeletable.gameObject.activeSelf)
                        nextSeletable = GetNextSelectable(nextSeletable, MoveDirection.Left);
                    if (nextSeletable != null)
                    {
                        UserInput.NextSelectionIsAxis = true;
                        EventSystem.current.SetSelectedGameObject(nextSeletable.gameObject);
                    }
                }
                else if (eventData.moveDir == MoveDirection.Right && Selectable.navigation.selectOnRight != null && !Selectable.navigation.selectOnRight.gameObject.activeSelf)
                {
                    var nextSeletable = Selectable.navigation.selectOnRight;
                    while (nextSeletable != null && !nextSeletable.gameObject.activeSelf)
                        nextSeletable = GetNextSelectable(nextSeletable, MoveDirection.Right);
                    if (nextSeletable != null)
                    {
                        UserInput.NextSelectionIsAxis = true;
                        EventSystem.current.SetSelectedGameObject(nextSeletable.gameObject);
                    }
                }
                else if (eventData.moveDir == MoveDirection.Up && Selectable.navigation.selectOnUp != null && !Selectable.navigation.selectOnUp.gameObject.activeSelf)
                {
                    var nextSeletable = Selectable.navigation.selectOnUp;
                    while (nextSeletable != null && !nextSeletable.gameObject.activeSelf)
                        nextSeletable = GetNextSelectable(nextSeletable, MoveDirection.Up);
                    if (nextSeletable != null)
                    {
                        UserInput.NextSelectionIsAxis = true;
                        EventSystem.current.SetSelectedGameObject(nextSeletable.gameObject);
                    }
                }
                else if (eventData.moveDir == MoveDirection.Down && Selectable.navigation.selectOnDown != null && !Selectable.navigation.selectOnDown.gameObject.activeSelf)
                {
                    var nextSeletable = Selectable.navigation.selectOnDown;
                    while (nextSeletable != null && !nextSeletable.gameObject.activeSelf)
                        nextSeletable = GetNextSelectable(nextSeletable, MoveDirection.Down);
                    if (nextSeletable != null)
                    {
                        UserInput.NextSelectionIsAxis = true;
                        EventSystem.current.SetSelectedGameObject(nextSeletable.gameObject);
                    }
                }
            }
            else
            {
                var selfIndex = index;
                if (selfIndex < 0)
                    selfIndex = transform.GetSiblingIndex();

                var count = GetButtonsCount();
                var columes = GetColumnsCount();

                var targetIndex = selfIndex + 1;

                if (eventData.moveDir == MoveDirection.Left)
                {
                    if (selfIndex % columes == 0)
                    {
                        OnNavigationLeftBorder();
                        return;
                    }
                    targetIndex = selfIndex - 1;
                }
                else if (eventData.moveDir == MoveDirection.Right)
                {
                    if (selfIndex % columes == columes - 1
                        || targetIndex >= count)
                    {
                        OnNavigationRightBorder();
                        return;
                    }
                }
                else if (eventData.moveDir == MoveDirection.Up)
                {
                    targetIndex = selfIndex - columes;
                    if (targetIndex < 0)
                    {
                        OnNavigationUpBorder();
                        return;
                    }
                }
                else if (eventData.moveDir == MoveDirection.Down)
                {
                    var lastLineLeft = count % columes;
                    var bound = count - lastLineLeft - 1;
                    if (lastLineLeft == 0)
                        bound -= columes;
                    if (selfIndex > bound)
                    {
                        OnNavigationDownBorder();
                        return;
                    }
                    targetIndex = selfIndex + columes;
                    if (targetIndex >= count)
                        targetIndex = count - 1;
                }

                for (int i = 0; i < transform.parent.childCount; i++)
                {
                    var child = transform.parent.GetChild(i);
                    if (!child.gameObject.activeSelf)
                        continue;
                    if (!child.TryGetComponent<SelectionButton>(out var selection))
                        continue;

                    var buttonIndex = selection.index;
                    if (buttonIndex < 0)
                        buttonIndex = i;

                    if (buttonIndex == targetIndex)
                    {
                        UserInput.NextSelectionIsAxis = true;
                        EventSystem.current.SetSelectedGameObject(transform.parent.GetChild(i).gameObject);
                        break;
                    }
                }
            }
        }

        protected Selectable GetNextSelectable(Selectable selectable, MoveDirection direction)
        {
            return direction switch
            {
                MoveDirection.Left => selectable.navigation.selectOnLeft,
                MoveDirection.Right => selectable.navigation.selectOnRight,
                MoveDirection.Up => selectable.navigation.selectOnUp,
                MoveDirection.Down => selectable.navigation.selectOnDown,
                _ => null,
            };
        }

        protected virtual int GetColumnsCount()
        {
            return 1;
        }

        protected virtual int GetButtonsCount()
        {
            return transform.parent.childCount;
        }

        protected virtual void OnNavigationLeftBorder()
        {
        }

        protected virtual void OnNavigationRightBorder()
        {
        }

        protected virtual void OnNavigationUpBorder()
        {
        }

        protected virtual void OnNavigationDownBorder()
        {
        }

        #endregion

        #region Other

        private ColorContainerGraphic[] colorContainers;
        protected void SetColor(SelectMode selectMode, StatusMode statusMode, bool interactable)
        {
            colorContainers ??= transform.GetComponentsInChildren<ColorContainerGraphic>(true);
            foreach (var ccg in colorContainers)
                ccg.SetColor(selectMode, statusMode, interactable);
        }

        #endregion
    }

    [Serializable]
    public class SelectionButtonNavigationEvent
    {
        public UnityEvent onLeftNavigation;
        public UnityEvent onRightNavigation;
        public UnityEvent onUpNavigation;
        public UnityEvent onDownNavigation;
    }

    [Serializable]
    public class SelectionButtonHoverEvent
    {
        public UnityEvent onHoverOn;
        public UnityEvent onHoverOff;
    }

    [Serializable]
    public class SelectionButtonSelectEvent
    {
        public UnityEvent onSelect;
        public UnityEvent onDeselect;
    }

    [Serializable]
    public class SelectionButtonClickEvent
    {
        public UnityEvent onLeftClick;
        public UnityEvent onMiddleClick;
        public UnityEvent onRightClick;
    }

}

