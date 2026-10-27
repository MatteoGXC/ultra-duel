using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using YgomSystem.ElementSystem;
using MDPro3.Servant;

namespace MDPro3.UI.ServantUI
{
    /// <summary>
    /// Servant的UI部件，在parentServant Show时激活，Hide时摧毁或者SetActive(false);
    /// 因此尽量不要在此类中调用协程。
    /// 主要用途是初始化UI和给按钮分配方法。
    /// </summary>
    [RequireComponent(typeof(ElementObjectManager))]
    public class ServantUI : MonoBehaviour
    {

        #region Elements

        private ElementObjectManager m_Manager;
        public ElementObjectManager Manager =>
            m_Manager = m_Manager != null ? m_Manager
            : GetComponent<ElementObjectManager>();

        private const string LABEL_RT_ROOT = "Root";
        private RectTransform m_Root;
        protected RectTransform Root =>
            m_Root = m_Root != null ? m_Root
            : Manager.GetElement<RectTransform>(LABEL_RT_ROOT);

        private CanvasGroup m_CG;
        public virtual CanvasGroup CG =>
            m_CG = m_CG != null ? m_CG
            : Root.GetComponent<CanvasGroup>();

        private const string LABEL_TXT_TITLE = "TextTitle";
        private TextMeshProUGUI m_Title;
        protected TextMeshProUGUI Title =>
            m_Title = m_Title != null ? m_Title
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_TITLE);

        private const string LABEL_RT_LEFTPART = "LeftPart";
        private RectTransform m_LeftPart;
        protected RectTransform LeftPart =>
            m_LeftPart = m_LeftPart != null ? m_LeftPart
            : Manager.GetElement<RectTransform>(LABEL_RT_LEFTPART);

        private const string LABEL_RT_RIGHTPART = "RightPart";
        private RectTransform m_RightPart;
        protected RectTransform RightPart =>
            m_RightPart = m_RightPart != null ? m_RightPart
            : Manager.GetElement<RectTransform>(LABEL_RT_RIGHTPART);

        #endregion

        [Header("Servant UI")]
        [SerializeField] protected Selectable defaultSelectable;

        protected Servant.Servant parentServant;

        public virtual void Initialize(Servant.Servant servant)
        {
            parentServant = servant;
            UIManager.Translate(gameObject);
        }

        public virtual void OnBack()
        {
            Program.instance.ExitCurrentServant();
        }

        public virtual void SelectDefaultSelectable()
        {
            if (defaultSelectable != null)
                defaultSelectable.Select();
        }

        public virtual void ResetUI()
        {
            Root.anchoredPosition3D = Vector3.zero;
            Root.localEulerAngles = Vector3.zero;
            CG.alpha = 1f;
            CG.blocksRaycasts = true;
            ResetLeftAndRightParts();
        }

        public virtual void ShutDown()
        {
            gameObject.SetActive(false);
        }

        public virtual void Show(bool cover)
        {
            if (HaveTwoParts())
                Show_Push();
            else
            {
                if(cover)
                    Show_Cover();
                else
                    Show_Uncover();
            }
            ShowEvent();
            UltraScreenSkin.Apply(gameObject);
        }

        public virtual void ShowEvent()
        {
        }

        public virtual void AfterShowEvent()
        {
        }

        public virtual void Hide(bool cover)
        {
            if (HaveTwoParts())
                Hide_Pop();
            else
            {
                if(cover)
                    Hide_Cover();
                else
                    Hide_Uncover();
            }
            HideEvent();
        }

        protected virtual void HideEvent()
        {
        }

        protected virtual void AfterHideEvent()
        {
        }

        protected T GetServant<T>() where T : Servant.Servant
        {
            return parentServant as T;
        }

        protected bool HaveTwoParts()
        {
            return LeftPart != null && RightPart != null;
        }

        protected virtual void ResetLeftAndRightParts()
        {
            if (!HaveTwoParts())
                return;

            LeftPart.anchoredPosition = Vector2.zero;
            RightPart.anchoredPosition = Vector2.zero;
        }

        protected virtual void Show_Push()
        {
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            DOTween.Sequence()
                .SetUpdate(true)
                .Append(CG.DOFade(1f, 0.25f).SetEase(UltraMotion.Ease))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            LeftPart.anchoredPosition = new Vector2(-70f, 0f);
            LeftPart.DOAnchorPosX(0f, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);

            RightPart.anchoredPosition = new Vector2(70f, 0f);
            RightPart.DOAnchorPosX(0f, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        protected virtual void Show_Cover()
        {
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            Root.localEulerAngles = Vector3.zero;
            DOTween.Sequence()
                .SetUpdate(true)
                .Append(CG.DOFade(1f, 0.25f).SetEase(UltraMotion.Ease))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            Root.anchoredPosition = new Vector2(-70f, 0f);
            Root.DOAnchorPos(Vector2.zero, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        protected virtual void Show_Uncover()
        {
            gameObject.SetActive(true);
            CG.alpha = 0f;
            CG.blocksRaycasts = false;

            Root.localEulerAngles = Vector3.zero;
            DOTween.Sequence()
                .SetUpdate(true)
                .Append(CG.DOFade(1f, 0.25f).SetEase(UltraMotion.Ease))
                .OnComplete(() =>
                {
                    CG.blocksRaycasts = true;
                    AfterShowEvent();
                });

            Root.anchoredPosition = new Vector2(70f, 0f);
            Root.DOAnchorPos(Vector2.zero, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        protected virtual void Hide_Pop()
        {
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.25f)
                .SetEase(UltraMotion.Ease)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            LeftPart.DOAnchorPosX(-50f, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
            RightPart.DOAnchorPosX(50f, 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        protected virtual void Hide_Cover()
        {
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.25f)
                .SetEase(UltraMotion.Ease)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            Root.DOAnchorPos(new Vector2(-50f, 0f), 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

        protected virtual void Hide_Uncover()
        {
            CG.blocksRaycasts = false;
            CG.DOFade(0f, 0.25f)
                .SetEase(UltraMotion.Ease)
                .SetUpdate(true)
                .OnComplete(() =>
            {
                gameObject.SetActive(false);
                AfterHideEvent();
            });

            Root.DOAnchorPos(new Vector2(50f, 0f), 0.25f).SetEase(UltraMotion.Ease).SetUpdate(true);
        }

    }
}