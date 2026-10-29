using System.CodeDom;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using MDPro3.Servant;
using static MDPro3.Servant.DeckSelector;
using static UnityEngine.Rendering.DebugUI;
using System.Collections.Generic;
using MDPro3.Duel.YGOSharp;
using DG.Tweening;
using MDPro3.Net;
using System.Text;
using System;
using UnityEngine.AddressableAssets;
using System.IO;
using System.Linq;
using MDPro3.UI.PropertyOverride;

namespace MDPro3.UI.ServantUI
{
    public class DeckSelectorUI : ServantUI
    {

        #region Elements

        private const string LABEL_SR = "ScrollRect";
        private ScrollRect m_ScrollRect;
        private ScrollRect ScrollRect =>
            m_ScrollRect = m_ScrollRect != null ? m_ScrollRect
            : Manager.GetElement<ScrollRect>(LABEL_SR);

        private const string LABEL_IPT = "InputField";
        private TMP_InputField m_Input;
        private TMP_InputField Input =>
            m_Input = m_Input != null ? m_Input
            : Manager.GetElement<TMP_InputField>(LABEL_IPT);

        private const string LABEL_TXT_DECKNUMVALUE = "TextDeckNumValue";
        private TextMeshProUGUI m_TextDeckNumValue;
        private TextMeshProUGUI TextDeckNumValue =>
            m_TextDeckNumValue = m_TextDeckNumValue != null ? m_TextDeckNumValue
            : Manager.GetElement<TextMeshProUGUI>(LABEL_TXT_DECKNUMVALUE);

        private const string LABEL_STG_PICKUPCARD = "TogglePickupCard";
        private SelectionToggle m_TogglePickupCard;
        public SelectionToggle TogglePickupCard =>
            m_TogglePickupCard = m_TogglePickupCard != null ? m_TogglePickupCard
            : Manager.GetElement<SelectionToggle>(LABEL_STG_PICKUPCARD);

        private const string LABEL_SBN_ONLINE = "ButtonOnline";
        private SelectionButton m_ButtonOnline;
        public SelectionButton ButtonOnline =>
            m_ButtonOnline = m_ButtonOnline != null ? m_ButtonOnline
            : Manager.GetElement<SelectionButton>(LABEL_SBN_ONLINE);

        private const string LABEL_RT_HEADER = "Header";
        private RectTransform m_Header;
        private RectTransform Header => 
            m_Header = m_Header != null ? m_Header
            : Manager.GetElement<RectTransform>(LABEL_RT_HEADER);

        private const string LABEL_RT_FOOTER = "Footer";
        private RectTransform m_Footer;
        private RectTransform Footer =>
            m_Footer = m_Footer != null ? m_Footer
            : Manager.GetElement<RectTransform>(LABEL_RT_FOOTER);

        private const string LABEL_SBN_TYPE = "ButtonType";
        private SelectionButton m_ButtonType;
        public SelectionButton ButtonType =>
            m_ButtonType = m_ButtonType != null ? m_ButtonType
            : Manager.GetElement<SelectionButton>(LABEL_SBN_TYPE);

        private const string LABEL_SBN_DELETE = "ButtonDelete";
        private SelectionButton m_ButtonDelete;
        private SelectionButton ButtonDelete =>
            m_ButtonDelete = m_ButtonDelete != null ? m_ButtonDelete
            : Manager.GetElement<SelectionButton>(LABEL_SBN_DELETE);

        private const string LABEL_SBN_CANCEL = "ButtonCancel";
        private SelectionButton m_ButtonCancel;
        private SelectionButton ButtonCancel =>
            m_ButtonCancel = m_ButtonCancel != null ? m_ButtonCancel
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CANCEL);

        private const string LABEL_SBN_CONFIRM = "ButtonConfirm";
        private SelectionButton m_ButtonConfirm;
        private SelectionButton ButtonConfirm =>
            m_ButtonConfirm = m_ButtonConfirm != null ? m_ButtonConfirm
            : Manager.GetElement<SelectionButton>(LABEL_SBN_CONFIRM);

        #endregion

        public SuperScrollView superScrollView;
        public string deckType = string.Empty;
        private string selectedType;
        public Dictionary<string, Deck> decks = new();
        public bool buttonLayoutSwitching;
        public static string deckInUse;

        public override void ShowEvent()
        {
            base.ShowEvent();

            switch (condition)
            {
                case Condition.ForEdit:
                    ButtonOnline.gameObject.SetActive(true);
                    Title.text = "Deck Builder";
                    break;
                case Condition.ForDuel:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = InterString.Get("选择卡组");
                    break;
                case Condition.ForSolo:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = InterString.Get("选择卡组");
                    break;
                case Condition.MyCard:
                    ButtonOnline.gameObject.SetActive(false);
                    Title.text = InterString.Get("选择卡组");
                    break;
            }

            ShowDefaultButtons();
        }

        public override void AfterShowEvent()
        {
            base.AfterShowEvent();
            RefreshList();
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();

            Config.Save();
            TogglePickupCard.SetToggleOff();
            superScrollView.Clear();
        }

        public void RefreshList()
        {
            decks.Clear();
            ShowDefaultButtons();
            TogglePickupCard.SetToggleOff();
            ButtonType.SetButtonText(GetTypeName());

            if (!Directory.Exists(Program.PATH_DECK))
                Directory.CreateDirectory(Program.PATH_DECK);
            var path = GetCurrentTypePath();
            var files = Directory.GetFiles(path, "*.ydk");
            var fileList = files.ToList();
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                fileName = fileName[..^4];
                if (fileName == Config.GetConfigDeckName(false))
                {
                    fileList.Remove(file);
                    fileList.Insert(0, file);
                    break;
                }
            }
            foreach (var deck in fileList)
            {
                var name = Path.GetFileName(deck);
                name = name[..^4];
                decks.Add(name, new Deck(deck));
            }

            Print(Input.text);
        }

        public void ActivateInputField()
        {
            Input.ActivateInputField();
        }

        public void Print(string search = "")
        {
            SwitchToDefaultLayout();

            superScrollView?.Clear();

            var handle = Addressables.LoadAssetAsync<GameObject>("UI/ItemDeck.prefab");
            handle.Completed += (result) =>
            {
                var itemWidth = PropertyOverrider.NeedMobileLayout() ? 340f : 304f;
                var itemHeight = PropertyOverrider.NeedMobileLayout() ? 460f : 500f;
                var space = PropertyOverrider.NeedMobileLayout() ? 24f : 28f;
                var bottomPadding = (PropertyOverrider.NeedMobileLayout() ? 196f : 150f) - space;
                superScrollView = new SuperScrollView(
                    -1,
                    itemWidth + space,
                    itemHeight + space,
                    10,
                    bottomPadding,
                    result.Result,
                    ItemOnListRefresh,
                    Manager.GetElement<ScrollRect>("ScrollRect"));
                List<string[]> tasks = new() { new string[7] { string.Empty, "0", "0", "0", "0", "0", "0" } };
                foreach (var deck in decks)
                {
                    if (deck.Key.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    var task = new string[8]
                    {
                        deck.Key,
                        deck.Value.Case.ToString(),
                        "0", "0", "0",
                        deck.Value.Protector.ToString(),
                        "0",//For Delete
                        deck.Value.deckId
                    };
                    if (deck.Value.Pickup.Count > 0)
                        task[2] = deck.Value.Pickup[0].ToString();
                    if (deck.Value.Pickup.Count > 1)
                        task[3] = deck.Value.Pickup[1].ToString();
                    if (deck.Value.Pickup.Count > 2)
                        task[4] = deck.Value.Pickup[2].ToString();
                    tasks.Add(task);
                }
                superScrollView.Print(tasks);
                Program.instance.deckSelector.lastSelectedDeckItem = superScrollView.items[0].gameObject.GetComponent<SelectionToggle_Deck>();
                if (Cursor.lockState == CursorLockMode.Locked)
                    Program.instance.deckSelector.Select();
                UpdateDeckNum();
            };
        }

        private void ItemOnListRefresh(string[] task, GameObject item)
        {
            var handler = item.GetComponent<SelectionToggle_Deck>();
            handler.deckName = task[0];
            handler.deckCase = int.Parse(task[1]);
            handler.card0 = int.Parse(task[2]);
            handler.card1 = int.Parse(task[3]);
            handler.card2 = int.Parse(task[4]);
            handler.protector = task[5];
            handler.isOn = task[6] != "0";
            handler.Refresh();
        }

        public bool PickupShowing
        {
            get { return m_pickupShowing; }
            set
            {
                m_pickupShowing = value;
                DeckHover();
            }
        }
        private bool m_pickupShowing = false;
        public void DeckHover()
        {
            if (superScrollView == null)
                return;

            foreach (var item in superScrollView.items)
            {
                if (item.gameObject == null)
                    continue;
                var handler = item.gameObject.GetComponent<SelectionToggle_Deck>();

                if (PickupShowing)
                    handler.ShowPickup(true);
                else
                    handler.HidePickup(true);
            }
        }

        public void DeckCreate()
        {
            SwitchToDefaultLayout();
            var selections = new List<string>()
            {
                InterString.Get("请输入卡组名。@n创建卡组时会自动导入剪切板中的卡组码。"),
                string.Empty
            };
            UIManager.ShowPopupInput(selections, DeckCheck, null, TmpInputValidation.ValidationType.Path);
        }

        private void DeckCheck(string deckName)
        {
            var path = GetDeckPath(deckName);

            if (File.Exists(path))
            {
                deckInUse = deckName;
                List<string> tasks = new()
                {
                    InterString.Get("该卡组名已存在"),
                    InterString.Get("该卡组名的文件已存在，是否直接覆盖创建？"),
                    InterString.Get("覆盖"),
                    InterString.Get("取消")
                };
                DOTween.To(v => { }, 0, 0, 0.6f).OnComplete(() =>
                {
                    UIManager.ShowPopupYesOrNo(tasks, DeckFileCreateWithName, null);
                });
            }
            else
                DeckFileCreate(deckName);
        }

        private void DeckFileCreateWithName()
        {
            DeckFileCreate(deckInUse);
        }

        private void DeckFileCreate(string deckName)
        {
            try
            {
                var path = GetDeckPath(deckName);
                File.Create(path).Close();

                string clipBoard = GUIUtility.systemCopyBuffer;
                if (clipBoard.Contains("#main"))
                    File.WriteAllText(path!, clipBoard, Encoding.UTF8);
                else if (clipBoard.Contains("ygotype=deck&v=1&d="))
                {
                    var uri = new Uri(clipBoard);
                    var deck = DeckShareURL.UriToDeck(uri);
                    deck.type = deckType;
                    deck.Save(deckName, DateTime.UtcNow);
                }
                else if (clipBoard.Contains(YdkeConverter.ydkeHeader))
                {
                    var deck = YdkeConverter.Ydke2Deck(clipBoard);
                    deck.type = deckType;
                    deck.Save(deckName, DateTime.UtcNow);
                }
                Config.SetConfigDeck(deckName, true);
                RefreshList();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                MessageManager.Cast(InterString.Get("创建卡组失败！请检查文件夹权限。"));
            }
        }

        private void DeleteOnlineDecks(List<string> ids)
        {
            if (MyCard.account == null)
                return;
            _ = OnlineDeck.DeleteDecks(ids);
        }

        public void OnOnlineDeckView()
        {
            Program.instance.ShiftToServant(Program.instance.onlineDeckViewer);
        }

        public void OnShowPickup()
        {
            PickupShowing = true;
        }

        public void OnHidePickup()
        {
            PickupShowing = false;
        }

        #region Deck Type

        public void OnType()
        {
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                InterString.Get("卡组分组"),
                InterString.Get("彩色选项为功能选项，白色选项为卡组分组。"),
                "g:" + InterString.Get("新建分组"),
                "r:" + InterString.Get("删除分组"),
                "b:" + InterString.Get("重命名分组"),
                "b:" + InterString.Get("移动到分组"),
                "b:" + InterString.Get("复制到分组"),
                InterString.Get("默认分组")
            };
            types.AddRange(folders);

            UIManager.ShowPopupSelection(types, OnTypeChange, null);
        }

        private void OnTypeChange()
        {
            var button = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>();
            var color = button.GetButtonTextColor();
            string selected = button.GetButtonText();

            if(color != Color.white)
            {
                if (selected == InterString.Get("新建分组"))
                    AddDeckType();
                if (selected == InterString.Get("删除分组"))
                    RemoveDeckType();
                if (selected == InterString.Get("重命名分组"))
                    RenameDeckType();
                if (selected == InterString.Get("移动到分组"))
                    MoveToType();
                if (selected == InterString.Get("复制到分组"))
                    CopyToType();
                return;
            }

            var type = selected;
            if(selected == InterString.Get("默认分组"))
                type = string.Empty;
            deckType = type;
            RefreshList();
        }

        private void AddDeckType()
        {
            var selections = new List<string>()
                {
                    InterString.Get("添加新卡组分组"),
                    string.Empty
                };
            UIManager.ShowPopupInput(selections, AddDeckType, null, TmpInputValidation.ValidationType.Path);
        }

        private void AddDeckType(string type)
        {
            if(type == string.Empty)
                return;

            var path = Program.PATH_DECK + type + "/";
            if (Directory.Exists(path))
            {
                MessageManager.Cast(InterString.Get("该分组已存在！"));
                return;
            }
            Directory.CreateDirectory(path);
            deckType = type;
            RefreshList();
        }

        private void RemoveDeckType()
        {
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                InterString.Get("删除分组"),
                string.Empty,
            };
            types.AddRange(folders);
            UIManager.ShowPopupSelection(types, DeleteDeckType, null);
        }

        private void DeleteDeckType()
        {
            var type = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            var path = Program.PATH_DECK + type + "/";
            if (!Directory.Exists(path))
                return;
            var files = Directory.GetFiles(path, "*.ydk");
            if(files.Length > 0)
            {
                var selections = new List<string>
                {
                    InterString.Get("删除分组"),
                    InterString.Get("该卡组分类包含[?]个卡组，是否确认删除？", files.Length.ToString()),
                    InterString.Get("确认"),
                    InterString.Get("取消")
                };
                UIManager.ShowPopupYesOrNo(selections, () => DeleteDeckType(type), null);
            }
            else
            {
                DeleteDeckType(type);
            }
        }

        private void DeleteDeckType(string type)
        {
            var path = Program.PATH_DECK + type + "/";
            DeleteOnlineDecks(GetAllDeckIds(path));
            var files = Directory.GetFiles(path, "*.ydk");
            foreach (var file in files)
                File.Delete(file);
            Directory.Delete(path);
            if (deckType == type)
                deckType = string.Empty;
            RefreshList();
        }

        private List<string> GetAllDeckIds(string dir)
        {
            var value = new List<string>();
            var files = Directory.GetFiles(dir, "*.ydk");
            foreach (var file in files)
            {
                var deck = new Deck(file);
                value.Add(deck.deckId);
                Debug.Log("--: " + deck.deckId);
            }

            return value;
        }

        private void RenameDeckType()
        {
            var folders = GetAllDeckTypes();
            var types = new List<string>
            {
                InterString.Get("重命名分组"),
                string.Empty,
            };
            types.AddRange(folders);
            UIManager.ShowPopupSelection(types, RenameType, null);
        }

        private void RenameType()
        {
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            var selections = new List<string>()
                {
                    InterString.Get("重命名分组"),
                    selectedType
                };
            UIManager.ShowPopupInput(selections, CheckRenamedDeckType, null, TmpInputValidation.ValidationType.Path);
        }

        private void CheckRenamedDeckType(string newType)
        {
            if (newType == selectedType)
                return;
            var allTypes = GetAllDeckTypes();
            bool exists = allTypes.Any(type=> type != selectedType && type == newType);
            if (exists)
            {
                MessageManager.Cast(InterString.Get("该分组已存在！"));
                return;
            }

            var path = Program.PATH_DECK + selectedType;
            var newPath = Program.PATH_DECK + newType;
            Directory.Move(path, newPath);
            var files = Directory.GetFiles(newPath, "*.ydk");

            foreach (var file in files)
            {
                var deck = new Deck(file)
                {
                    type = newType
                };
                deck.Save(Path.GetFileNameWithoutExtension(file), DateTime.UtcNow, true, false);
            }

            deckType = newType;
            RefreshList();
        }

        private void MoveToType()
        {
            var folders = GetAllDeckTypes(deckType);
            var types = new List<string>
            {
                InterString.Get("移动到分组"),
                string.Empty
            };
            if (deckType != string.Empty)
                types.Add(InterString.Get("默认分组"));
            types.AddRange(folders);
            UIManager.ShowPopupSelection(types, OnMoveToType, null);
        }

        private void CopyToType()
        {
            var folders = GetAllDeckTypes(deckType);
            var types = new List<string>
            {
                InterString.Get("复制到分组"),
                string.Empty
            };
            if (deckType != string.Empty)
                types.Add(InterString.Get("默认分组"));
            types.AddRange(folders);
            UIManager.ShowPopupSelection(types, OnCopyToType, null);
        }

        private string GetCurrentTypePath()
        {
            if (deckType == string.Empty)
                return Program.PATH_DECK;
            else
                return Program.PATH_DECK + deckType + "/";
        }

        private string GetTypeName(string type)
        {
            if(type == string.Empty)
                return InterString.Get("默认分组");
            else
                return type;
        }

        private string GetTypeName()
        {
            return GetTypeName(deckType);
        }

        private string GetTypePath(string type)
        {
            if (type == string.Empty)
                return Program.PATH_DECK;
            else
                return Program.PATH_DECK + $"{type}/";
        }

        private string GetDeckPath(string deckName, string type)
        {
            return GetTypePath(type) + deckName + Program.EXPANSION_YDK;
        }

        private string GetDeckPath(string deckName)
        {
            return GetDeckPath(deckName, deckType);
        }

        private string[] GetAllDeckTypes(string excludeType = null)
        {
            var folders = Directory.GetDirectories(Program.PATH_DECK);
            var types = folders.Select(f => Path.GetFileName(f)).ToArray();
            if(excludeType != null)
                types = types.Where(t => t != excludeType).ToArray();
            return types;
        }

        #endregion

        #region Delete Deck

        public void OnDelete()
        {
            if (buttonLayoutSwitching) return;
            SwitchButtonLayouts(LayoutType.Delete);
        }

        private void OnDeleteConfirm()
        {
            if (buttonLayoutSwitching) return;

            var deleteIndexs = new List<int>();
            var deleteIds = new List<string>();
            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    File.Delete(GetDeckPath(superScrollView.items[i].args[0]));
                    deleteIndexs.Add(i);
                    deleteIds.Add(superScrollView.items[i].args[7]);
                }
            var lastSelect = Program.instance.deckSelector.lastSelectedDeckItem.index;
            int removedCount = 0;
            for (int i = 0; i < deleteIndexs.Count; i++)
            {
                superScrollView.RemoveAt(deleteIndexs[i] - removedCount);
                removedCount++;
            }
            Program.instance.deckSelector.lastSelectedDeckItem = (SelectionToggle_Deck)superScrollView.GetItemByIndex(lastSelect);
            if (Cursor.lockState == CursorLockMode.Locked)
                Program.instance.deckSelector.Select();
            DeleteOnlineDecks(deleteIds);

            SwitchToDefaultLayout();
            UpdateDeckNum();
        }

        #endregion

        #region Move Deck to Type

        private void OnMoveToType()
        {
            if (buttonLayoutSwitching) return;
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            if(selectedType == InterString.Get("默认分组"))
                selectedType = string.Empty;
            SwitchButtonLayouts(LayoutType.MoveToType);
        }

        private void OnMoveToTypeConfirm()
        {
            if (buttonLayoutSwitching) return;

            var moveIndexs = new List<int>();
            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    var filePath = GetDeckPath(superScrollView.items[i].args[0]);
                    var newPath = GetDeckPath(superScrollView.items[i].args[0], selectedType);
                    if(File.Exists(newPath))
                    {
                        MessageManager.Cast(InterString.Get("操作失败，目标分组已存在同名卡组：[[?]]。", superScrollView.items[i].args[0]));
                        continue;
                    }

                    var deck = new Deck(filePath)
                    {
                        type = selectedType
                    };
                    deck.Save(superScrollView.items[i].args[0], DateTime.UtcNow, true, false);
                    File.Delete(filePath);
                    moveIndexs.Add(i);
                }
            var lastSelect = Program.instance.deckSelector.lastSelectedDeckItem.index;
            int removedCount = 0;
            for (int i = 0; i < moveIndexs.Count; i++)
            {
                superScrollView.RemoveAt(moveIndexs[i] - removedCount);
                removedCount++;
            }
            Program.instance.deckSelector.lastSelectedDeckItem = (SelectionToggle_Deck)superScrollView.GetItemByIndex(lastSelect);
            if (Cursor.lockState == CursorLockMode.Locked)
                Program.instance.deckSelector.Select();

            SwitchToDefaultLayout();
            UpdateDeckNum();
        }

        #endregion

        #region Copy Deck to Type

        private void OnCopyToType()
        {
            if (buttonLayoutSwitching) return;
            selectedType = UnityEngine.EventSystems.EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            if (selectedType == InterString.Get("默认分组"))
                selectedType = string.Empty;
            SwitchButtonLayouts(LayoutType.CopyToType);
        }

        private void OnCopyToTypeConfirm()
        {
            if (buttonLayoutSwitching) return;

            for (int i = 0; i < superScrollView.items.Count; i++)
                if (superScrollView.items[i].args[6] != "0")
                {
                    var filePath = GetDeckPath(superScrollView.items[i].args[0]);
                    var newPath = GetDeckPath(superScrollView.items[i].args[0], selectedType);
                    if (File.Exists(newPath))
                    {
                        MessageManager.Cast(InterString.Get("操作失败，目标分组已存在同名卡组：[[?]]。", superScrollView.items[i].args[0]));
                        continue;
                    }

                    var deck = new Deck(filePath)
                    {
                        deckId = string.Empty,
                        type = selectedType
                    };
                    deck.Save(superScrollView.items[i].args[0], DateTime.UtcNow, true, false);
                }
            if (Cursor.lockState == CursorLockMode.Locked)
                Program.instance.deckSelector.Select();
            SwitchToDefaultLayout();
        }

        #endregion

        #region Switch Layout

        private enum LayoutType
        {
            Default,
            Delete,
            MoveToType,
            CopyToType
        }

        private LayoutType layoutType;

        private void ShowDefaultButtons()
        {
            ButtonOnline.SetButtonText("Premade Decks");
            ButtonDelete.gameObject.SetActive(true);
            ButtonCancel.gameObject.SetActive(false);
            ButtonOnline.gameObject.SetActive(true);
            ButtonConfirm.gameObject.SetActive(false);
            Input.gameObject.SetActive(true);
            ButtonType.gameObject.SetActive(true);
        }

        private void ShowDeleteButtons()
        {
            ButtonDelete.gameObject.SetActive(false);
            ButtonCancel.gameObject.SetActive(true);
            ButtonOnline.gameObject.SetActive(false);
            ButtonConfirm.gameObject.SetActive(true);
            Input.gameObject.SetActive(false);
            ButtonType.gameObject.SetActive(false);

            ButtonCancel.ShowIcon(true);
            ButtonConfirm.SetButtonText(InterString.Get("确认删除"));
        }

        private void ShowMoveOrCopyButtons()
        {
            ButtonDelete.gameObject.SetActive(false);
            ButtonCancel.gameObject.SetActive(true);
            ButtonOnline.gameObject.SetActive(false);
            ButtonConfirm.gameObject.SetActive(true);
            Input.gameObject.SetActive(false);
            ButtonType.gameObject.SetActive(false);

            ButtonCancel.ShowIcon(false);
            ButtonConfirm.SetButtonText(InterString.Get("确认"));
        }

        private void UpdateDeckNum()
        {
            TextDeckNumValue.text = decks.Count.ToString();
        }

        private void SwitchToDefaultLayout()
        {
            HideAllToggles();
            SwitchButtonLayouts(LayoutType.Default);
        }

        private void SwitchButtonLayouts(LayoutType layoutType)
        {
            if (this.layoutType == layoutType)
                return;
            buttonLayoutSwitching = true;
            this.layoutType = layoutType;

            var header = Manager.GetElement<RectTransform>("Header");
            var footer = Manager.GetElement<RectTransform>("Footer");
            UIManager.HideExitButton(0.2f);

            DOTween.Sequence()
                .Append(header.DOAnchorPosY(PropertyOverrider.NeedMobileLayout() ? 130f : 120f, 0.2f).OnComplete(() =>
                {
                    if (layoutType == LayoutType.Default)
                        ShowDefaultButtons();
                    else if (layoutType == LayoutType.Delete)
                        ShowDeleteButtons();
                    else
                        ShowMoveOrCopyButtons();
                    UIManager.ShowExitButton(0.3f, Ease.OutQuart);
                    if (layoutType != LayoutType.Default)
                        foreach (var item in superScrollView.items)
                        {
                            if (item.gameObject == null)
                                continue;
                            item.gameObject.GetComponent<SelectionToggle_Deck>().ShowToggle();
                        }
                    Title.text = layoutType switch
                    {
                        LayoutType.MoveToType => InterString.Get("移动到分组") + $" [{GetTypeName(selectedType)}]",
                        LayoutType.CopyToType => InterString.Get("复制到分组") + $" [{GetTypeName(selectedType)}]",
                        LayoutType.Delete => InterString.Get("删除卡组"),
                        _ => "Deck Builder"
                    };
                }))
                .Join(footer.DOAnchorPosY(PropertyOverrider.NeedMobileLayout() ? -186f : -140f, 0.2f))
                .Append(header.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutQuart))
                .Join(footer.DOAnchorPosY(0f, 0.3f).SetEase(Ease.OutQuart)).OnComplete(() =>
                {
                    buttonLayoutSwitching = false;
                });
        }

        private void HideAllToggles()
        {
            if (superScrollView == null || superScrollView.items == null)
                return;

            foreach (var item in superScrollView.items)
                item.args[6] = "0";
            foreach (var item in superScrollView.items)
            {
                if (item.gameObject == null)
                    continue;
                item.gameObject.GetComponent<SelectionToggle_Deck>().HideToggle();
            }
        }

        public void OnConfirm()
        {
            if (layoutType == LayoutType.Delete)
                OnDeleteConfirm();
            else if (layoutType == LayoutType.MoveToType)
                OnMoveToTypeConfirm();
            else if (layoutType == LayoutType.CopyToType)
                OnCopyToTypeConfirm();
        }

        public void OnCancel()
        {
            if (buttonLayoutSwitching) return;
            SwitchToDefaultLayout();
        }

        #endregion

    }
}
