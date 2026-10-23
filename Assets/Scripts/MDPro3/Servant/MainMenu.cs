using MDPro3.Net;
using MDPro3.UI;
using MDPro3.UI.ServantUI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MDPro3.Servant
{
    public class MainMenu : Servant
    {
        [HideInInspector] public SelectionButton_MainMenu lastSelectedButton;

        public override int Depth => 0;
        protected override bool ShowLine => false;
        protected override bool NeedExitButton => false;

        public override void Initialize()
        {
            base.Initialize();
            
            showing = true;
            UIManager.HideExitButton(0f);
            UIManager.HideLine(0f);
            Program.instance.currentServant = this;
            Program.instance.depth = 0;

            LoadUI();
        }

        public override void OnExit()
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

        protected override void FirstLoadEvent()
        {
            base.FirstLoadEvent();
            servantUI.ResetUI();

            EnsureButtonsWidth();
            Program.instance.ReadParams();
        }

        protected override void ApplyShowArrangement(int preDepth)
        {
            base.ApplyShowArrangement(preDepth);

            UIManager.ShowWallpaper(TransitionTime);
        }

        protected override void ApplyHideArrangement(int preDepth)
        {
            base.ApplyHideArrangement(preDepth);

            UIManager.HideWallpaper(TransitionTime);
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (UserInput.WasCancelPressed)
                    OnReturn();

                if (GetUI<MainMenuUI>().News.showing)
                {
                    if (UserInput.WasLeftPressed)
                        GetUI<MainMenuUI>().News.OnLeft();
                    else if (UserInput.WasRightPressed)
                        GetUI<MainMenuUI>().News.OnRight();

                    if (UserInput.WasGamepadButtonWestPressed)
                        GetUI<MainMenuUI>().News.OnNewsClick();
                    if (UserInput.WasGamepadButtonNorthPressed)
                        GetUI<MainMenuUI>().News.OnClose();
                }
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectedButton != null)
                lastSelectedButton.GetSelectable().Select();
            else
                GetUI<MainMenuUI>().SelectDefaultSelectable();
        }

        private IEnumerator CheckUpdateAsync()
        {
            yield return new WaitForSeconds(3);
            using var www = UnityWebRequest.Get(Settings.Data.MDPro3VersionUrl);
            www.SetRequestHeader("Cache-Control", "max-age=0, no-cache, no-store");
            www.SetRequestHeader("Pragma", "no-cache");
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                var result = www.downloadHandler.text;
                var lines = result.Replace("\r", "").Split('\n');
                if (Application.version != lines[0])
                    MessageManager.Cast(InterString.Get("检测到新版本[[?]]。", lines[0]));
            }
            else
                MessageManager.Cast(InterString.Get("检查更新失败！"));

            var filePath = Path.Combine(Program.PATH_EXPANSIONS, Path.GetFileName(Settings.Data.PrereleasePackUrl));
            if (!File.Exists(filePath))
            {
                Config.Set("Prerelease", "0");
                Config.Save();
            }

            using var www2 = UnityWebRequest.Get(Settings.GetPrereleasePackVersionUrl());
            yield return www2.SendWebRequest();
            if (www2.result == UnityWebRequest.Result.Success)
            {
                var result = www2.downloadHandler.text;
                var lines = result.Replace("\r", "").Split('\n');
                if (Config.Get("Prerelease", "0") != lines[0])
                    MessageManager.Cast(InterString.Get("检测到新版先行卡，请至 [游戏设置]-[扩展卡包]-[更新先行卡] 处进行更新。"));
            }
        }

        private IEnumerator LoadMyCardNewsAsync()
        {
            while(OnlineService.myCardNews == null)
                yield return null;
            var news = OnlineService.myCardNews;
            GetUI<MainMenuUI>().News.news = news;
            GetUI<MainMenuUI>().News.LoadNews();
        }

        private SelectionButton_MainMenu[] buttons;
        private SelectionButton_MainMenu[] Buttons
            => buttons ??= GetButtons();

        private SelectionButton_MainMenu[] GetButtons()
        {
            if (servantUI == null)
                return null;
            return transform.GetComponentsInChildren<SelectionButton_MainMenu>(true);
        }

        public void EnsureButtonsWidth()
        {
            if (Buttons == null)
                return;

            var widths = new float[Buttons.Length];
            for (int i = 0; i < Buttons.Length; i++)
                widths[i] = buttons[i].GetPreferredWidth();
            var maxWidth = Mathf.Max(widths);
            foreach(var button in Buttons)
                button.SetWidth(maxWidth);
        }

    }
}
