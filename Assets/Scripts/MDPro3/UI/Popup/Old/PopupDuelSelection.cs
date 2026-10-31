using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MDPro3.UI 
{
    public class PopupDuelSelection : PopupDuel
    {
        [Header("Popup Duel Select Reference")]
        public ScrollRect scrollRect;
        public GameObject item;
        public RectTransform baseRect;
        public List<int> responses;

        public override void InitializeSelections()
        {
            base.InitializeSelections();
            Program.instance.currentServant.returnAction = null;

            var selectionButtons = new List<Button>();
            float cursor=20f;
            for (int i = 1; i < selections.Count; i++)
            {
                GameObject newSelection = Instantiate(item);
                newSelection.transform.SetParent(scrollRect.content, false);
                var label=newSelection.transform.GetChild(0).GetChild(0).GetComponent<Text>();
                label.text = selections[i];
                label.horizontalOverflow=HorizontalWrapMode.Wrap;
                label.verticalOverflow=VerticalWrapMode.Overflow;
                float width=Mathf.Max(100f,newSelection.GetComponent<RectTransform>().rect.width-60f);
                var settings=label.GetGenerationSettings(new Vector2(width,0));
                float rowHeight=Mathf.Max(74f,label.cachedTextGeneratorForLayout.GetPreferredHeight(label.text,settings)/label.pixelsPerUnit+16f);
                newSelection.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rowHeight);
                label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,rowHeight-12f);
                var buttonTransform = newSelection.transform.GetChild(0);
                int response=responses[i-1];
                buttonTransform.name = response.ToString();
                var button = buttonTransform.GetComponent<Button>();
                button.onClick.AddListener(() =>
                {
                    if (response != -233)
                    {
                        var binaryMaster = new BinaryMaster();
                        binaryMaster.writer.Write(response);
                        SendReturn(binaryMaster.Get());
                    }
                    Hide();
                });
                selectionButtons.Add(button);
                newSelection.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -cursor);
                cursor+=rowHeight+16f;
            }

            for (int i = 0; i < selectionButtons.Count; i++)
            {
                var button = selectionButtons[i];
                var navigation = button.navigation;
                navigation.mode = Navigation.Mode.Explicit;
                navigation.selectOnUp = selectionButtons[Mathf.Max(0, i - 1)];
                navigation.selectOnDown = selectionButtons[Mathf.Min(selectionButtons.Count - 1, i + 1)];
                navigation.selectOnLeft = button;
                navigation.selectOnRight = button;
                button.navigation = navigation;
            }

            if (scrollRect.verticalScrollbar != null)
            {
                var scrollbarNavigation = scrollRect.verticalScrollbar.navigation;
                scrollbarNavigation.mode = Navigation.Mode.None;
                scrollRect.verticalScrollbar.navigation = scrollbarNavigation;
            }

            scrollRect.content.sizeDelta = new Vector2(scrollRect.content.sizeDelta.x, cursor+5f);
            baseRect.sizeDelta = new Vector2(baseRect.sizeDelta.x,
                scrollRect.content.sizeDelta.y + 50 > 800 ?
                800 :
                scrollRect.content.sizeDelta.y + 50
                );

            scrollRect.verticalNormalizedPosition = 1f;
            if (selectionButtons.Count > 0)
                EventSystem.current.SetSelectedGameObject(selectionButtons[0].gameObject);
        }
    }

}

