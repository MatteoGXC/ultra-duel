using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using System.Collections;
using UnityEngine.EventSystems;
using YgomSystem.ElementSystem;
using TMPro;
using UnityEngine.AddressableAssets;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class Servant : MonoBehaviour
    {
        [HideInInspector] public Servant returnServant;
        [HideInInspector] public Selectable lastSelectable;

        [HideInInspector] public bool showing;
        [HideInInspector] public bool inTransition;

        public Action returnAction;
        public virtual float TransitionTime => 0.4f;

        public virtual int Depth => 1;
        protected virtual bool ShowLine => false;
        protected virtual bool NeedExitButton => true;
        protected virtual float BlackAlpha => 0f;
        protected virtual float SubBlackAlpha => 0f;
        protected virtual string Label_UI => $"ServantUI/{GetType().Name}UI.prefab";

        public ServantUI servantUI;

        public virtual void Initialize()
        {
            UserInput.OnMouseCursorHide += OnMouseCursorHide;
        }

        public void Show(int preDepth)
        {
            if(showing) return;

            showing = true;

            if (servantUI != null)
            {
                ApplyShowArrangement(preDepth);
            }
            else
            {
                inTransition = true;
                var handle = Addressables.InstantiateAsync(Label_UI);
                handle.Completed += (result) =>
                {
                    result.Result.transform.SetParent(transform, false);
                    servantUI = result.Result.GetComponent<ServantUI>();
                    FirstLoadEvent();
                    ApplyShowArrangement(preDepth);
                };
            }
        }

        public void Hide(int nextDepth)
        {
            if (!showing) return;

            showing = false;
            ApplyHideArrangement(nextDepth);
        }

        public virtual void PerFrameFunction()
        {
            if (!NeedResponseInput())
                return;

            if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                OnReturn();
        }

        /// <summary>
        /// 响应后退动作：鼠标右键、键盘Esc键、手柄East键等。
        /// </summary>
        public virtual void OnReturn()
        {
            AudioManager.PlaySE("SE_MENU_CANCEL");
            if (returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            else
                OnExit();
        }

        /// <summary>
        /// 退出当前Servant。
        /// </summary>
        public virtual void OnExit()
        {
            if (Program.instance.currentSubServant == this)
            {
                if (this is SettingServant)
                {
                    Hide(-2);
                    Program.instance.currentSubServant = null;
                }
                else
                    Program.instance.ShowSubServant(returnServant);
            }
            else
                Program.instance.ShiftToServant(returnServant);
        }

        public virtual bool NeedResponseInput()
        {
            if (!showing) return false;
            if (inTransition) return false;
            if (servantUI == null) return false;

            if (UIManager.InputBlocker != null)
                return false;
            if (Program.instance.ui_.currentPopup != null)
                return false;
            if (Program.instance.ui_.currentPopupB != null)
                return false;
            if (Program.instance.ui_.currentSidePanel != null)
                return false;
            if (UserInput.InputFieldActivating())
                return false;
            return true;
        }

        public virtual void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;
            if (lastSelectable != null)
                lastSelectable.Select();
            else if (servantUI != null)
                servantUI.SelectDefaultSelectable();
        }

        public virtual void JudgeInputBlockerExitMark(object o)
        {
        }

        public virtual T GetUI<T>() where T : ServantUI
        {
            return servantUI as T;
        }

        protected void LoadUI()
        {
            var handle = Addressables.InstantiateAsync(Label_UI);
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(transform, false);
                servantUI = result.Result.GetComponent<ServantUI>();
                FirstLoadEvent();
            };
        }

        protected virtual void FirstLoadEvent()
        {
            servantUI.Initialize(this);
        }

        protected virtual void ApplyShowArrangement(int preDepth)
        {
            inTransition = true;

            bool fromDuel = 
                Program.instance.currentServant == this 
                && preDepth == -1;

            if (this is not MainMenu && Program.instance != null && Program.instance.ui_ != null)
                UIManager.HideWallpaper(fromDuel ? 0f : TransitionTime);

            if (fromDuel)
            {
                servantUI.ShowEvent();
                DOTween.To(v => { }, 0, 0, Program.instance.ocgcore.TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    servantUI.gameObject.SetActive(true);
                    inTransition = false;
                    servantUI.ResetUI();

                    if (NeedExitButton)
                        UIManager.ShowExitButton(0);
                    else
                        UIManager.HideExitButton(0);

                    if (ShowLine)
                        UIManager.ShowLine(0);
                    else
                        UIManager.HideLine(0);

                    UIManager.ShowBlackBack(BlackAlpha, 0f);
                    servantUI.AfterShowEvent();
                    AfterShowingEvent();
                });
            }
            else
            {
                servantUI.gameObject.SetActive(true);
                servantUI.Show(preDepth > Depth);

                if (NeedExitButton)
                    UIManager.ShowExitButton(TransitionTime);
                else
                    UIManager.HideExitButton(TransitionTime);
                if (ShowLine)
                    UIManager.ShowLine(TransitionTime);
                else
                    UIManager.HideLine(TransitionTime);

                UIManager.ShowBlackBack(
                    Program.instance.currentServant is OcgCore
                    ? SubBlackAlpha : BlackAlpha
                    , TransitionTime
                    , () => 
                    {
                        inTransition = false;
                        AfterShowingEvent();
                    });
            }
        }

        protected virtual void AfterShowingEvent()
        {
            if (UserInput.NeedDefaultSelect())
                Select();
        }

        protected virtual void ApplyHideArrangement(int nextDepth)
        {
            inTransition = true;

            bool toDuel = nextDepth == -1;

            if (toDuel)
            {
                DOTween.To(v => { }, 0, 0, TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    servantUI.ShutDown();
                    inTransition = false;
                });
            }
            else
            {
                servantUI.Hide(nextDepth > Depth);
                DOTween.To(v => { }, 0, 0, TransitionTime)
                    .SetUpdate(true)
                    .OnComplete(() =>
                {
                    inTransition = false;
                    AfterHidingEvent();
                });
            }
        }

        protected virtual void AfterHidingEvent()
        {
        }

        protected virtual void OnMouseCursorHide()
        {
            if (NeedResponseInput())
                Select();
        }

    }
}

