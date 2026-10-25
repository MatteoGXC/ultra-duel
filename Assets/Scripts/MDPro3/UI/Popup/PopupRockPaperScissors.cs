using Cysharp.Threading.Tasks;
using MDPro3.UI.ServantUI;
using TMPro;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MDPro3.UI.Popup
{
    public class PopupRockPaperScissors : Popup
    {

        protected override void Initialize()
        {
            base.Initialize();
            cancelCallHide = false;
            AddChoice("RockButton", "ROCK", 2);
            AddChoice("PaperButton", "PAPER", 3);
            AddChoice("ScissorsButton", "SCISSORS", 1);
        }

        private void AddChoice(string key, string title, byte response)
        {
            var button = Manager.GetElement<Button>(key);
            button.onClick.AddListener(() => { TcpHelper.CtosMessage_HandResult(response); Hide(); });
            var image = Manager.GetElement<RawImage>(key);
            var builtIn = Resources.Load<Texture2D>("Ultra/RPS" + key.Replace("Button", ""));
            image.texture = builtIn != null ? builtIn : Texture2D.whiteTexture;
            image.color = builtIn != null ? Color.white : new Color(.10f, .14f, .18f, .98f);
            var bar = new GameObject("Choice caption");
            bar.transform.SetParent(button.transform, false);
            bar.layer = button.gameObject.layer;
            var rect = bar.AddComponent<RectTransform>();
            UltraUi.EnsureRenderer(bar);
            bar.AddComponent<Image>();
            rect.anchorMin = new Vector2(.02f, .02f);
            rect.anchorMax = new Vector2(.98f, .23f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var background = bar.GetComponent<Image>();
            background.color = new Color(UltraTheme.Ink.r, UltraTheme.Ink.g, UltraTheme.Ink.b, .86f);
            background.raycastTarget = false;
            var label = new GameObject("Label");
            label.transform.SetParent(bar.transform, false);
            label.layer = bar.layer;
            label.AddComponent<RectTransform>();
            var text = UltraUi.Text(label);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
            text.font = Program.instance.ui_.tmpFont;
            text.text = title;
            UltraTheme.Style(text, true, 8f);
            text.fontSize = 28;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16;
            text.fontSizeMax = 28;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.raycastTarget = false;
        }

        private void Start()
        {
            _ = LoadAsync();
        }

        private async UniTask LoadAsync()
        {
            var pic = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Rock.png");
            if (pic != null)
            {
                var rock = Manager.GetElement<RawImage>("RockButton");
                rock.texture = pic;
                rock.color = Color.white;
            }

            pic = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Paper.png");
            if (pic != null)
            {
                var paper = Manager.GetElement<RawImage>("PaperButton");
                paper.texture = pic;
                paper.color = Color.white;
            }

            pic = await TextureManager.LoadPicFromFileAsync(Program.PATH_DIY + "Scissors.png");
            if (pic != null)
            {
                var scissors = Manager.GetElement<RawImage>("ScissorsButton");
                scissors.texture = pic;
                scissors.color = Color.white;
            }
        }
    }
}

