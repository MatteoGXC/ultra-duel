using DG.Tweening;
using MDPro3;
using MDPro3.UI;
using MDPro3.Net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.ServantUI;

namespace MDPro3.Servant
{
    public class OnlineDeckViewer : Servant
    {

        public static OnlineDeck.OnlineDeckData[] decks;
        public static string FormatFilter = "TCG";
        public static string SortOrder = "A–Z";
        public static bool AvailableOnly;
        public static PremadeDeckLibrary.Entry[] premades = Array.Empty<PremadeDeckLibrary.Entry>();
        [HideInInspector] public SelectionToggle_DeckOnline lastSelectedDeckItem;

        public override int Depth => 5;
        protected override bool ShowLine => true;

        #region Servant
        public override void Initialize()
        {
            returnServant = Program.instance.deckSelector;
            base.Initialize();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);
            RefreshList();
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedDeckItem != null) lastSelectedDeckItem.GetSelectable().Select();
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                    OnReturn();
                if (UserInput.WasGamepadButtonWestPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<OnlineDeckViewerUI>().InputDeckName.ActivateInputField();
                }
                if (UserInput.WasGamepadButtonNorthPressed)
                {
                    AudioManager.PlaySE("SE_MENU_SELECT_01");
                    GetUI<OnlineDeckViewerUI>().ShowFormatPicker();
                }
            }
        }

        #endregion

        public void RefreshList()
        {
            try
            {
                premades = PremadeDeckLibrary.Search(GetUI<OnlineDeckViewerUI>().InputDeckName.text, FormatFilter);
                if (AvailableOnly) premades = premades.Where(e => e.available).ToArray();
                if (SortOrder == "Z–A") premades = premades.OrderByDescending(e => e.archetype, StringComparer.OrdinalIgnoreCase).ToArray();
                else if (SortOrder == "Lowest points") premades = premades.OrderBy(e => !e.available).ThenBy(e => e.points).ThenBy(e => e.archetype, StringComparer.OrdinalIgnoreCase).ToArray();
                else if (SortOrder == "Highest points") premades = premades.OrderBy(e => !e.available).ThenByDescending(e => e.points).ThenBy(e => e.archetype, StringComparer.OrdinalIgnoreCase).ToArray();
                GetUI<OnlineDeckViewerUI>().Print();
            }
            catch (Exception e)
            {
                MessageManager.Cast(e.Message);
            }
        }

    }
}
