using MDPro3.UI.ServantUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static MDPro3.UI.CardCollectionView;

namespace MDPro3.UI
{
    public class SelectionToggle_SearchOrder : SelectionToggle
    {
        [Header("SelectionToggle SearchOrder")]
        [SerializeField] private SortOrder sortOrder = SortOrder.ByType;

        protected override void Awake()
        {
            base.Awake();

            exclusiveToggle = true;
            canToggleOffSelf = false;
        }

        private void Start()
        {
            if (sortOrder == _SortOrder)
            {
                SetToggleOn();
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
        }

        protected override void CallHoverOnEvent()
        {
            base.CallHoverOnEvent();
            var popup=GetComponentInParent<Popup.PopupSearchOrder>();
            if(popup!=null)popup.lastSelectedToggle = this;
        }

        protected override void CallSubmitEvent()
        {
            _SortOrder = sortOrder;
            Program.instance.deckEditor.GetUI<DeckEditorUI>().CardCollectionView.SetSortIcon(GetIconSprite());
            Program.instance.deckEditor.GetUI<DeckEditorUI>().CardCollectionView.SetSortText(GetSortText());
            Program.instance.deckEditor.GetUI<DeckEditorUI>().CardCollectionView.PrintSearchCards();
            var popup=GetComponentInParent<Popup.PopupSearchOrder>();
            if(popup!=null)popup.Hide();
        }

        protected string GetSortText()
        {
            return sortOrder switch
            {
                SortOrder.ByType => InterString.Get("种类"),
                SortOrder.ByTypeReverse => InterString.Get("种类"),
                SortOrder.ByLevelUp => InterString.Get("等级·阶级·连接"),
                SortOrder.ByLevelDown => InterString.Get("等级·阶级·连接"),
                SortOrder.ByAttackUp => InterString.Get("攻击力"),
                SortOrder.ByAttackDown => InterString.Get("攻击力"),
                SortOrder.ByDefenceUp => InterString.Get("守备力"),
                SortOrder.ByDefenceDown => InterString.Get("守备力"),
                SortOrder.ByRarityUp => InterString.Get("稀有度"),
                SortOrder.ByRarityDown => InterString.Get("稀有度"),
                SortOrder.ByGPUp => InterString.Get("Genesys分数"),
                SortOrder.ByGPDown => InterString.Get("Genesys分数"),
                _ => string.Empty
            };
        }
        protected override void OnClick()
        {
            AudioManager.PlaySE(SoundLabelClick);
            SetToggleOn();
            CallSubmitEvent();
        }

        protected override void OnSubmit()
        {
            base.OnSubmit();
            OnClick();
        }
    }
}
