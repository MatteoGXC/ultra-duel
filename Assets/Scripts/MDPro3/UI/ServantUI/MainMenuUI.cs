using MDPro3.Net;
using MDPro3;
using UnityEngine;
using UnityEngine.Networking;
using YgomSystem.ElementSystem;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using MDPro3.Servant;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace MDPro3.UI.ServantUI
{
    public class MainMenuUI : ServantUI
    {
        #region Elements

        private const string LABEL_MYCARD_NEWS = "News";
        private NewsManager m_News;
        public NewsManager News =>
            m_News = m_News != null ? m_News
            : Manager.GetElement<NewsManager>(LABEL_MYCARD_NEWS);

        #endregion

        public override void Initialize(Servant.Servant servant)
        {
            base.Initialize(servant);

            Title.text = "Matteo Duel";
            BuildHome();
        }

        private TextMeshProUGUI homeStatus;

        public override void ShowEvent()
        {
            base.ShowEvent();
            RefreshHomeStatus();
        }

        private void RefreshHomeStatus()
        {
            if (homeStatus != null)
                homeStatus.text = DuelFormat.DisplayName + "\n\n" + Config.GetConfigDeckName();
        }

        private RectTransform HomeRect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.gameObject.layer = gameObject.layer;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(x, y);
            rect.anchorMax = new Vector2(x + w, y + h);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return rect;
        }

        private void HomePanel(RectTransform rect, Color color)
        {
            var panel = rect.gameObject.AddComponent<Image>();
            panel.color = color;
            panel.raycastTarget = false;
        }

        private TextMeshProUGUI HomeText(Transform parent, string text, float size, Color color,
            float x, float y, float w, float h)
        {
            var label = HomeRect("Label", parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = TMP_Settings.defaultFontAsset != null ? TMP_Settings.defaultFontAsset : Title.font;
            label.text = text;
            label.fontSize = size;
            label.color = color;
            label.raycastTarget = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = size * 0.7f;
            label.fontSizeMax = size;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            return label;
        }

        private Button HomeButton(Transform parent, string title, string description, UnityAction action,
            float x, float y, float w, float h, bool primary = false)
        {
            var rect = HomeRect(title, parent, x, y, w, h);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? new Color(0.80f, 0.66f, 0.37f) : new Color(0.08f, 0.15f, 0.20f, 0.96f);
            colors.highlightedColor = colors.selectedColor = primary ? new Color(1f, 0.86f, 0.54f) : new Color(0.16f, 0.31f, 0.37f);
            colors.pressedColor = new Color(0.32f, 0.48f, 0.50f);
            button.colors = colors;
            button.onClick.AddListener(action);
            Color ink = primary ? new Color(0.04f, 0.09f, 0.12f) : Color.white;
            HomeText(rect, title, primary ? 40 : 28, ink, .055f, .42f, .89f, .45f).fontStyle = FontStyles.Bold;
            HomeText(rect, description, 19, primary ? ink : new Color(.64f, .77f, .80f), .055f, .12f, .89f, .28f);
            return button;
        }

        private void BuildHome()
        {
            // Keep the original wallpaper and callbacks; replace the crowded navigation.
            foreach (Transform child in Root) child.gameObject.SetActive(false);
            var home = HomeRect("Matteo Duel Home", Root, 0, 0, 1, 1);
            HomePanel(HomeRect("Navigation backdrop", home, 0, 0, .49f, 1), new Color(.015f, .04f, .065f, .95f));
            HomePanel(HomeRect("Gold rule", home, .045f, .83f, .405f, .002f), new Color(.80f, .66f, .37f));
            HomeText(home, "MATTEO DUEL", 53, Color.white, .045f, .87f, .42f, .075f).fontStyle = FontStyles.Bold;
            HomeText(home, "YOUR DECK. YOUR NEXT MOVE.", 19, new Color(.80f, .66f, .37f), .047f, .837f, .40f, .035f);
            defaultSelectable = HomeButton(home, "PLAY SOLO   >", "Challenge the AI with your selected deck", OnSolo, .045f, .635f, .40f, .155f, true);
            HomeButton(home, "MY DECKS", "Build, edit and check your cards", OnDeck, .045f, .49f, .40f, .12f);
            HomeButton(home, "ONLINE", "Rooms & multiplayer", OnOnline, .045f, .365f, .19f, .10f);
            HomeButton(home, "REPLAYS", "Revisit your duels", OnReplay, .255f, .365f, .19f, .10f);
            HomeButton(home, "PUZZLES", "Practice your next move", OnPuzzle, .045f, .25f, .19f, .10f);
            HomeButton(home, "CUSTOMIZE", "Mates & companions", OnMate, .255f, .25f, .19f, .10f);
            HomeButton(home, "SETTINGS", "Make it yours", OnSetting, .045f, .135f, .19f, .10f);
            HomeButton(home, "CUT-INS", "Card animations", OnCutin, .255f, .135f, .19f, .10f);
            HomeButton(home, "EXIT", "Close game", OnExit, .325f, .035f, .12f, .075f);
            HomeText(home, "TCG + GENESYS\nLOCAL DUEL PRACTICE", 17, new Color(.48f, .64f, .69f), .045f, .035f, .25f, .075f);
            var status = HomeRect("Ready panel", home, .64f, .06f, .31f, .19f);
            HomePanel(status, new Color(.015f, .04f, .065f, .92f));
            HomeText(status, "READY WHEN YOU ARE", 18, new Color(.80f, .66f, .37f), .07f, .72f, .86f, .20f);
            homeStatus = HomeText(status, "", 25, Color.white, .07f, .12f, .86f, .58f);
            RefreshHomeStatus();
        }

        #region Button Function

        public void OnSolo()
        {
            if (Program.exitOnReturn)
                return;

            DuelFormat.ApplyDeckFormat(Config.GetConfigDeckName());
            SoloSelector.HasFormatOverride = false;
            Program.instance.solo.SwitchCondition(SoloSelector.Condition.ForSolo);
            Program.instance.ShiftToServant(Program.instance.solo);
        }

        public void OnOnline()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.online);
        }

        public void OnPuzzle()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.puzzle);
        }

        public void OnReplay()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.replay);
        }

        public void OnCutin()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.cutin);
        }
        public void OnMate()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.mate);
        }

        public void OnDeck()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.deckSelector.SwitchCondition(DeckSelector.Condition.ForEdit);
            Program.instance.ShiftToServant(Program.instance.deckSelector);
        }

        public void OnSetting()
        {
            if (Program.exitOnReturn)
                return;

            Program.instance.ShiftToServant(Program.instance.setting);
        }

        public void OnExit()
        {
            if (Program.exitOnReturn)
                return;

            List<string> selections = new()
            {
                InterString.Get("确认退出"),
                InterString.Get("即将退出应用程序，@n是否确认？"),
                InterString.Get("确认"),
                InterString.Get("取消"),
                Config.STRING_YES
            };
            UIManager.ShowPopupYesOrNo(selections, Program.GameQuit, null);
        }

        #endregion
    }
}
