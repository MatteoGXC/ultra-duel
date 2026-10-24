using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using MDPro3.Duel.YGOSharp;
using Spine.Unity;
using TMPro;
using UnityEngine.AddressableAssets;
using MDPro3.UI;
using MDPro3.Net;
using MDPro3.Utility;
using MDPro3.UI.PropertyOverride;
using MDPro3.UI.Popup;
using MDPro3.Servant;
using Cysharp.Threading.Tasks;
using MDPro3.Duel;
using UnityEngine.Playables;

namespace MDPro3
{
    public class UIManager : Manager
    {
        private const string TRANSLATE_PREFIX = "#Text";

        [Header("Public Reference")]
        public CanvasGroup wallpaper;
        public Button btnExit;
        public CanvasGroup line;
        public Image blackBack;
        public RectTransform popup;
        public RectTransform sidePanel;
        public RectTransform duelButton;
        public const string DefaultWallpaper = "1130001"; // Blue-Eyes White Dragon
        public static string currentWallpaper;

        [Header("UI Handler")]
        public FPSHandler fps;
        List<UIHandler> handlers;

        [Header("Side Panel")]
        public ChatPanel chatPanel;

        [Header("Source Reference")]
        public Font cnFont;
        public Font jpFont;
        public Font cnMenuFont;
        public TMP_FontAsset tmpFont;
        public TMP_FontAsset jpMenuTmpFont;
        public TMP_FontAsset cnMenuTmpFont;

        [HideInInspector] public PopupBase currentPopup;
        [HideInInspector] public Popup currentPopupB;
        [HideInInspector] public SidePanel currentSidePanel;

        [HideInInspector] public static MonoBehaviour InputBlocker;

        public override void Initialize()
        {
            base.Initialize();
            if(btnExit!=null)
            {
                foreach(var graphic in btnExit.GetComponentsInChildren<Graphic>(true))graphic.enabled=false;
                var backObject=new GameObject("Ultra circular back",typeof(RectTransform));
                backObject.transform.SetParent(btnExit.transform,false);backObject.layer=btnExit.gameObject.layer;
                var backRect=(RectTransform)backObject.transform;backRect.anchorMin=Vector2.zero;backRect.anchorMax=Vector2.one;backRect.offsetMin=backRect.offsetMax=Vector2.zero;
                var back=backObject.AddComponent<MDPro3.UI.ServantUI.UltraCircleBack>();back.raycastTarget=true;btnExit.targetGraphic=back;
                var colors=btnExit.colors;colors.normalColor=Color.white;colors.highlightedColor=colors.selectedColor=new Color(.85f,.85f,.85f);colors.pressedColor=new Color(.65f,.65f,.65f);btnExit.colors=colors;
                btnExit.gameObject.AddComponent<MDPro3.UI.ServantUI.UltraButtonMotion>();
            }

            handlers = new List<UIHandler>()
            {
                fps,
            };
            foreach (UIHandler handler in handlers)
                handler.Initialize();

            try
            {
                currentWallpaper = Config.Get("Wallpaper", DefaultWallpaper);
                ChangeWallpaper(currentWallpaper);
                InitializeLanguage();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"UIManager Initialize Error: {e}");
            }
        }

        public override void PerFrameFunction()
        {
            base.PerFrameFunction();
            foreach (UIHandler handler in handlers)
                    handler.PerframeFunction();
        }

        public static void Translate(GameObject go)
        {
            //UnityEngine.Debug.Log($"Translate: {go.name}");
            //TO DELETE
            foreach (var text in go.GetComponentsInChildren<Text>(true))
                if (text.name.StartsWith(TRANSLATE_PREFIX))
                    text.text = InterString.Get(text.text.Replace(Program.STRING_LINE_BREAK, InterString.CONFIG_LINE_BREAK).Replace("\n", InterString.CONFIG_LINE_BREAK));
            foreach (var tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (tmp.name.StartsWith(TRANSLATE_PREFIX))
                    tmp.text = InterString.Get(tmp.text.Replace(Program.STRING_LINE_BREAK, InterString.CONFIG_LINE_BREAK).Replace("\n", InterString.CONFIG_LINE_BREAK));
                if (tmp.name.EndsWith("Menu"))
                {
                    UIManager instance = Program.instance.ui_;

                    if ((Language.GetConfig() == Language.English
                        || Language.GetConfig() == Language.Japanese))
                    {
                        tmp.font = instance.jpMenuTmpFont;
                        tmp.fontSize = 64f;
                    }
                    else if (Language.GetConfig() == Language.SimplifiedChinese)
                    {
                        tmp.font = instance.cnMenuTmpFont;
                        tmp.fontSize = 62f;
                    }
                    else
                    {
                        tmp.font = instance.tmpFont;
                        tmp.fontSize = 60f;
                    }
                }
            }
        }

        public static void InitializeLanguage()
        {
            InterString.Initialize();
            StringHelper.Initialize();
            CardsManager.Initialize();
            Program.items.Initialize();

            UIManager instance = Program.instance.ui_;
            foreach (var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith(TRANSLATE_PREFIX))
                {
                    if(t.TryGetComponent<Text>(out var text))
                    {
                        text.text = InterString.Get(text.text);
                    }
                    else if(t.TryGetComponent<TextMeshProUGUI>(out var tmp))
                    {
                        tmp.text = InterString.Get(tmp.text);

                        if (tmp.name.EndsWith("Menu"))
                        {
                            if ((Language.GetConfig() == Language.English
                                || Language.GetConfig() == Language.Japanese))
                            {
                                tmp.font = instance.jpMenuTmpFont;
                                tmp.fontSize = 64f;
                            }
                            else if (Language.GetConfig() == Language.SimplifiedChinese)
                            {
                                tmp.font = instance.cnMenuTmpFont;
                                tmp.fontSize = 62f;
                            }
                            else
                            {
                                tmp.font = instance.tmpFont;
                                tmp.fontSize = 60f;
                            }
                        }
                    }
                }
            }
        }

        public static void ChangeLanguage()
        {
            UIManager instance = Program.instance.ui_;
            foreach(var t in instance.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.StartsWith(TRANSLATE_PREFIX))
                {
                    if(t.TryGetComponent<Text>(out var text))
                        text.text = InterString.GetOriginal(text.text);
                    else if (t.TryGetComponent<TextMeshProUGUI>(out var tmp))
                        tmp.text = InterString.GetOriginal(tmp.text);
                }
            }

            CardImageLoader.ClearCache();
            Program.instance.UnloadUnusedAssets();

            InitializeLanguage();
            Program.instance.menu.EnsureButtonsWidth();
            Program.instance.cutin.LoadCutins();
            Program.instance.mate.LoadMates();
            Program.instance.solo.LoadBots();
            Program.instance.character.LoadCharacters();
            Program.instance.setting.RefreshCharacterName();

            SystemEvent.CallLanguageChangeEvent();
            SystemEvent.CallVideoCardConfigChangeEvent();
        }

        public static void ChangeLayout()
        {
            UIManager instance = Program.instance.ui_;
            foreach (var overrider in instance.GetComponentsInChildren<PropertyOverrider>(true))
                overrider.Override();

            ShowExitButton(0f);
            Program.instance.cutin.LoadCutins();
            Program.instance.mate.LoadMates();
            Program.instance.solo.LoadBots();
            Program.instance.puzzle.PrintPuzzles();
        }

        private async UniTask LoadDiyWallpaperAsync(string path, Transform parent)
        {
            GameObject dynamic = await ABLoader.LoadFromFileAsync(path, false, true);
            dynamic.transform.SetParent(parent, false);
        }

        public Transform ChangeWallpaper(string path)
        {
            if (wallpaper.transform.childCount > 0)
                Destroy(wallpaper.transform.GetChild(0).gameObject);
            if (path == Items.CODE_NONE.ToString())
                return null;

            path = "MasterDuel/" + Program.items.GetWallpaperPath(path);
            if (!path.ToLower().Contains("front"))
            {
                Transform frontback = ChangeWallpaper("1130002");
                Destroy(frontback.GetChild(1).gameObject);
                _ = LoadDiyWallpaperAsync(path, frontback);
                return frontback;
            }
            GameObject frontLoader = ABLoader.LoadFromFolder<RectTransform>(path, false, true);
            frontLoader = Instantiate(frontLoader);
            var front = frontLoader.transform;
            front.SetParent(wallpaper.transform, false);
            for (int i = 0; i < front.transform.childCount; i++)
                front.transform.GetChild(i).gameObject.AddComponent<RectLoopMoveY>();

            foreach (ParticleSystem p in front.GetComponentsInChildren<ParticleSystem>(true))
                p.Play();
            return front;
        }

        public static void ShowWallpaper(float time)
        {
            UIManager instance = Program.instance.ui_;
            instance.wallpaper.gameObject.SetActive(true);
            DOTween.To(() => instance.wallpaper.alpha, x => instance.wallpaper.alpha = x, 1, time);
            foreach (var p in instance.wallpaper.transform.GetComponentsInChildren<ParticleSystem>(true))
            {
                p.gameObject.SetActive(true);
                p.Play();
            }
            foreach (var skeleton in instance.wallpaper.transform.GetComponentsInChildren<SkeletonAnimation>())
            {
                //skeleton.GetComponent<Renderer>().material.SetColor("_Color", new Color(1f, 1f, 1f, 0f));
                skeleton.GetComponent<Renderer>().material.DOFade(1f, time - 0.1f).OnComplete(() => { });

            }
        }

        public static void HideWallpaper(float time)
        {
            DOTween.To(() => Program.instance.ui_.wallpaper.alpha, x => Program.instance.ui_.wallpaper.alpha = x, 0, time).OnComplete(() =>
                Program.instance.ui_.wallpaper.gameObject.SetActive(false));
            foreach (var p in Program.instance.ui_.wallpaper.transform.GetComponentsInChildren<ParticleSystem>(true))
                p.gameObject.SetActive(false);
            foreach (var skeleton in Program.instance.ui_.wallpaper.transform.GetComponentsInChildren<SkeletonAnimation>())
                skeleton.GetComponent<Renderer>().material.DOFade(0f, time - 0.1f).OnComplete(() => { });
        }

        public static void ShowExitButton(float time, Ease ease = Ease.Linear)
        {
            Program.instance.ui_.btnExit.GetComponent<RectTransform>().DOAnchorPosY(PropertyOverrider.NeedMobileLayout() ? -65f : -60f, time).SetEase(ease);
        }

        public static void HideExitButton(float time, Ease ease = Ease.Linear)
        {
            Program.instance.ui_.btnExit.GetComponent<RectTransform>().DOAnchorPosY(PropertyOverrider.NeedMobileLayout() ? 65f : 60f, time).SetEase(ease);
        }

        public static void ShowLine(float time)
        {
            Program.instance.ui_.line.DOFade(1f, time);
        }

        public static void HideLine(float time)
        {
            Program.instance.ui_.line.DOFade(0f, time);
        }

        public static void ShowFPS()
        {
            Program.instance.ui_.fps.gameObject.SetActive(true);
        }

        public static void HideFPS()
        {
            Program.instance.ui_.fps.gameObject.SetActive(false);
        }

        public static void ShowFPSLeft()
        {
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchorMin = new Vector2(0, 1);
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchorMax = new Vector2(0, 1);
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 0);
        }

        public static void ShowFPSRight()
        {
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchorMin = new Vector2(1, 1);
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchorMax = new Vector2(1, 1);
            Program.instance.ui_.fps.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
        }

        public static void SetFpsSize(int size)
        {
            Program.instance.ui_.fps.text.fontSize = size;
        }

        #region Popup

        public static void ShowPopupSelection(
            List<string> selections, 
            Action decideAction, 
            Action cancelAction = null)
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupSelection.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupSelection = result.Result.GetComponent<UI.Popup.PopupSelection>();
                popupSelection.args = selections;
                popupSelection.decideAction = decideAction;
                popupSelection.quitAction = cancelAction;
                popupSelection.Show();
            };
        }

        public static void ShowPopupYesOrNo(
            List<string> selections, 
            Action decideAction, 
            Action cancelAction)
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupYesOrNo.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupYesOrNo = result.Result.GetComponent<UI.Popup.PopupYesOrNo>();
                popupYesOrNo.args = selections;
                popupYesOrNo.decideAction = decideAction;
                popupYesOrNo.cancelAction = cancelAction;
                popupYesOrNo.Show();
            };
        }

        public static void ShowPopupConfirm(List<string> selections)
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupConfirm.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupConfirm = result.Result.GetComponent<UI.Popup.PopupConfirm>();
                popupConfirm.args = selections;
                popupConfirm.Show();
            };
        }

        public static void ShowPopupInput(
            List<string> selections, 
            Action<string> decideAction, 
            Action cancelAction, 
            TmpInputValidation.ValidationType type = TmpInputValidation.ValidationType.None)
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupInput.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupInput = result.Result.GetComponent<UI.Popup.PopupInput>();
                popupInput.args = selections;
                popupInput.decideAction = decideAction;
                popupInput.cancelAction = cancelAction;
                popupInput.validationType = type;
                popupInput.Show();
            };
        }

        public static void ShowPopupFilter()
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupSearchFilter.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupSearchFilter = result.Result.GetComponent<UI.Popup.PopupSearchFilter>();
                popupSearchFilter.Show();
            };
        }

        public static void ShowPopupText(
            List<string> selections, 
            HorizontalAlignmentOptions alignment = HorizontalAlignmentOptions.Center)
        {
            var handle = Addressables.InstantiateAsync("Popup/PopupText.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var popupText = result.Result.GetComponent<UI.Popup.PopupText>();
                popupText.alignment = alignment;
                popupText.args = selections;
                popupText.Show();
            };
        }

        #endregion

        #region UI Tools

        private static GameObject duelTransition;

        public static void UIBlackIn(float time)
        {
            if (duelTransition == null)
            {
                duelTransition = ABLoader.LoadMasterDuelOutDuelObject("DuelEndTransition");
                duelTransition.transform.SetParent(Program.instance.container_2D, false);
                duelTransition.GetComponent<PlayableDirector>().Play();
            }
        }

        public static void UIBlackOut(float time)
        {
            if(duelTransition != null)
            {
                duelTransition.GetComponent<LoopTrackManager>().StopLoop();
                Destroy(duelTransition, 1f);
                duelTransition = null;
            }
        }

        public static void ShowBlackBack(float alpha, float time, Action action = null)
        {
            Program.instance.ui_.blackBack.raycastTarget = true;
            Program.instance.ui_.blackBack.DOFade(alpha, time)
                .SetUpdate(true)
                .OnComplete(() => 
            {
                action?.Invoke();
            });
        }

        public static void HideBlackBack(float time)
        {
            Program.instance.ui_.blackBack.DOFade(0f, time)
                .SetUpdate(true)
                .OnComplete(() => 
            {
                Program.instance.ui_.blackBack.raycastTarget = false;
            });
        }

        public static void SetCanvasMatch(float match, float duration)
        {
            var instance = Program.instance.ui_;
            var scaler = instance.GetComponent<CanvasScaler>();
            DOTween.To(() => scaler.matchWidthOrHeight, x => scaler.matchWidthOrHeight = x, match, duration);
        }

        public static void ShowCardExpand(Card data)
        {
            var handle = Addressables.InstantiateAsync("UIWidges/CardExpand.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var handler = result.Result.GetComponent<CardExpand>();
                handler.Show(data);
            };
        }

        public static void ShowCardInfoDetail(Card data)
        {
            var handle = Addressables.InstantiateAsync("UIWidges/CardInfoDetail.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var handler = result.Result.GetComponent<CardInfoDetail>();
                handler.Show(data);
            };
        }

        public static void ShowCardInfoDetail(List<int> cards, int index)
        {
            var handle = Addressables.InstantiateAsync("UIWidges/CardInfoDetail.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.popup, false);
                var handler = result.Result.GetComponent<CardInfoDetail>();
                handler.Show(cards, index);
            };
        }

        public static void ShowSubMenu(List<string> menus, List<Action> actions)
        {
            var handle = Addressables.InstantiateAsync("UIWidges/SubMenuUI.prefab");
            handle.Completed += (result) =>
            {
                result.Result.transform.SetParent(Program.instance.ui_.sidePanel, false);
                var handler = result.Result.GetComponent<SubMenu>();
                InputBlocker = handler;
                handler.Show(menus, actions);
            };
        }

        #endregion

        #region Public Static Tools

        public static Vector2 WorldToScreenPoint(Camera camera, Vector3 positon)
        {
            var screenPosition = camera.WorldToScreenPoint(positon);
            var sizeDelta = Program.instance.ui_.GetComponent<RectTransform>().sizeDelta;
            return new Vector2(screenPosition.x * sizeDelta.x / Screen.width, screenPosition.y * sizeDelta.y / Screen.height);
        }

        public static Vector2 ScreenToNoScalerScreenPoint(Vector2 position)
        {
            var sizeDelta = Program.instance.ui_.GetComponent<RectTransform>().sizeDelta;
            return new Vector2(position.x * Screen.width / sizeDelta.x, position.y * Screen.height / sizeDelta.y);
        }

        public static Vector3 ScreenToWorldPoint(Camera camera, Vector2 positon)
        {
            var screenPosition = ScreenToNoScalerScreenPoint(positon);
            return camera.ScreenToWorldPoint(screenPosition);
        }

        public static float ScreenLengthWithoutScalerX(float length)
        {
            var sizeDelta = Program.instance.ui_.GetComponent<RectTransform>().sizeDelta;
            return length * sizeDelta.x / Screen.width;
        }

        public static float ScreenLengthWithScalerX(float length)
        {
            var sizeDelta = Program.instance.ui_.GetComponent<RectTransform>().sizeDelta;
            return length * Screen.width / sizeDelta.x;
        }

        public static float ScreenLengthWithoutScalerY(float length)
        {
            var sizeDelta = Program.instance.ui_.GetComponent<RectTransform>().sizeDelta;
            return length * sizeDelta.y / Screen.height;
        }

        public static Vector2 GetMousePositionToAnchorPosition()
        {
            var returnValue = UserInput.MousePos;
            var uiWidth = 1080f * Screen.width / Screen.height;
            returnValue.x = returnValue.x * uiWidth / Screen.width;
            returnValue.y = returnValue.y * 1080 / Screen.height;
            returnValue.x -= uiWidth / 2f;
            returnValue.y -= 540;
            return returnValue;
        }

        #endregion

    }
}
