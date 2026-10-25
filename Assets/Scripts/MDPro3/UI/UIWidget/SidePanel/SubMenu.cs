using DG.Tweening;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using YgomSystem.ElementSystem;
using MDPro3.UI.ServantUI;

namespace MDPro3.UI
{
    public class SubMenu : SidePanel
    {

        #region Elements

        private const string LABEL_SR_MENULIST = "MenuList";
        private ScrollRect m_MenuList;
        private ScrollRect MenuList =>
            m_MenuList = m_MenuList != null ? m_MenuList
            : Manager.GetElement<ScrollRect>(LABEL_SR_MENULIST);

        private const string LABEL_GO_TITLETEMPLATE = "MenuList/TitleTemplate";
        private GameObject m_TitleTemplate;
        private GameObject TitleTemplate =>
            m_TitleTemplate = m_TitleTemplate != null ? m_TitleTemplate
            : Manager.GetNestedElement(LABEL_GO_TITLETEMPLATE);

        private const string LABEL_GO_BUTTONTEMPLATE = "MenuList/ButtonTemplate";
        private GameObject m_ButtonTemplate;
        private GameObject ButtonTemplate =>
            m_ButtonTemplate = m_ButtonTemplate != null ? m_ButtonTemplate
            : Manager.GetNestedElement(LABEL_GO_BUTTONTEMPLATE);

        private const string LABEL_GO_SPACER = "MenuList/Spacer";
        private GameObject m_Spacer;
        private GameObject Spacer =>
            m_Spacer = m_Spacer != null ? m_Spacer
            : Manager.GetNestedElement(LABEL_GO_SPACER);


        #endregion

        protected override void Awake()
        {
            base.Awake();
            TitleTemplate.SetActive(false);
            ButtonTemplate.SetActive(false);
            Spacer.SetActive(false);
        }

        public void Show(List<string> menus, List<Action> actions)
        {
            base.Show();
            StartCoroutine(GenItemsAsync(menus, actions));
            MenuList.verticalNormalizedPosition = 1f;
        }

        private IEnumerator GenItemsAsync(List<string> menus, List<Action> actions)
        {
            int index = -1;
            for (int i = 0; i < menus.Count; i++)
            {
                if (actions[i] == null)
                {
                    if (i != 0)
                    {
                        var spacer = Instantiate(Spacer);
                        spacer.transform.SetParent(MenuList.content, false);
                        spacer.SetActive(true);
                    }
                    var title = Instantiate(TitleTemplate);
                    title.transform.SetParent(MenuList.content, false);
                    title.GetComponent<ElementObjectManager>()
                        .GetElement<TextMeshProUGUI>("TitleText").text = menus[i];
                    title.SetActive(true);

                    var rect = title.transform.GetChild(0).GetComponent<RectTransform>();
                    rect.anchoredPosition3D = new Vector3(70f, 0f, 0f);
                    DOTween.Sequence()
                        .AppendInterval(0.04f)
                        .Append(rect.DOAnchorPosX(0f, 0.16f).SetEase(Ease.OutCubic));

                    var cg = rect.GetComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    DOTween.Sequence()
                        .AppendInterval(0.04f)
                        .Append(cg.DOFade(1f, 0.16f).SetEase(Ease.OutCubic));

                    yield return new WaitForSeconds(0.02f);
                }
                else
                {
                    index++;
                    var button = Instantiate(ButtonTemplate);
                    button.transform.SetParent(MenuList.content, false);
                    button.SetActive(true);
                    var selection = button.GetComponent<SelectionButton>();
                    selection.SetButtonText(menus[i]);
                    selection.index = index;
                    if (defaultSelectable == null)
                    {
                        defaultSelectable = selection.GetSelectable();
                        if (UserInput.gamepadType != UserInput.GamepadType.None)
                            selection.GetSelectable().Select();
                    }

                    int j = i;
                    selection.SetClickEvent(() =>
                    {
                        actions[j].Invoke();
                        Hide();
                    });

                    var rect = button.transform.GetChild(0).GetComponent<RectTransform>();
                    rect.anchoredPosition3D = new Vector3(70f, 0f, 0f);
                    DOTween.Sequence()
                        .AppendInterval(0.04f)
                        .Append(rect.DOAnchorPosX(0f, 0.16f).SetEase(Ease.OutCubic));

                    var cg = rect.GetComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    DOTween.Sequence()
                        .AppendInterval(0.04f)
                        .Append(cg.DOFade(1f, 0.16f).SetEase(Ease.OutCubic));

                    yield return new WaitForSeconds(0.02f);
                }
            }
            UltraScreenSkin.Apply(gameObject);
        }
    }
}
