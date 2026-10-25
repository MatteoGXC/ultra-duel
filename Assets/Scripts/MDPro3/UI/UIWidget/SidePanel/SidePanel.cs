using DG.Tweening;
using System;
using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI
{
    public class SidePanel : UIWidget
    {
        #region Elements

        protected SelectionButton m_ButtonBG;
        protected SelectionButton ButtonBG =>
            m_ButtonBG = m_ButtonBG != null ? m_ButtonBG
            : Manager.GetElement<SelectionButton>(LABEL_CG_BG);

        #endregion

        [HideInInspector] public bool showing;

        protected SidePanel lastSidePanel;
        protected bool shifting;

        protected virtual float TransitionTime => 0.2f;
        protected virtual float Width => Window.rect.width;
        protected virtual bool Permanent => false;
        protected virtual bool TakeOverInput => true;

        protected override void Awake()
        {
            if (ButtonBG != null)
                ButtonBG.SetClickEvent(() => Hide());

            if (Permanent)
            {
                gameObject.SetActive(false);
                if(CG != null)
                    CG.alpha = 1.0f;
            }
        }

        protected virtual void OnEnable()
        {
            UserInput.OnMouseCursorHide += OnMouseCursorHide;
        }

        protected virtual void OnDisable()
        {
            UserInput.OnMouseCursorHide -= OnMouseCursorHide;
        }

        protected virtual void Update()
        {
            if (!NeedResponse()) return;

            if (UserInput.WasCancelPressed || UserInput.MouseRightDown)
                HideWithSound();
        }

        protected virtual bool NeedResponse()
        {
            if (!showing)
                return false;
            if (!TakeOverInput)
                return false;

            return Program.instance.ui_.currentSidePanel == this;
        }

        protected virtual void OnMouseCursorHide()
        {
            if (showing && NeedResponse())
                Select();
        }

        public virtual void Show()
        {
            if (showing || shifting) return;

            showing = true;
            shifting = true;
            UltraScreenSkin.Apply(gameObject);
            AudioManager.PlaySE("SE_MENU_SLIDE_01");

            if (Permanent)
            {
                gameObject.SetActive(true);
            }

            Window.DOAnchorPosX(0f, TransitionTime).SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    shifting = false;
                    ShowCompleteCallback();
                });

            if (BG != null)
            {
                BG.DOFade(1f, TransitionTime);
                BG.blocksRaycasts = true;
            }
        }

        protected virtual void ShowCompleteCallback()
        {
            if (TakeOverInput)
            {
                lastSidePanel = Program.instance.ui_.currentSidePanel;
                Program.instance.ui_.currentSidePanel = this;
            }
        }

        public virtual void Hide()
        {
            if (!showing || shifting) return;

            showing = false;
            shifting = true;

            Window.DOAnchorPosX(Width + SafeAreaAdapter.GetSafeAreaRightOffset(), TransitionTime)
                .SetEase(Ease.OutQuart)
                .OnComplete(() =>
                {
                    shifting = false;
                    HideCompleteCallback();
                });

            if (BG != null)
            {
                BG.DOFade(0f, TransitionTime).OnComplete(() =>
                {
                    BG.blocksRaycasts = false;
                });
            }
        }

        protected virtual void HideCompleteCallback()
        {
            if (TakeOverInput && Program.instance.ui_.currentSidePanel == this)
            {
                Program.instance.ui_.currentSidePanel = null;
                Program.instance.currentServant.Select();
            }

            if (Permanent)
                gameObject.SetActive(false);
            else
                Destroy(gameObject);
        }
        protected virtual void HideWithSound()
        {
            AudioManager.PlaySE("SE_MENU_SLIDE_02");
            Hide();
        }
    }
}
