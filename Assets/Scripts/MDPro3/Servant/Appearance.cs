using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using MDPro3.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.IO;
using System.Threading.Tasks;
using MDPro3.Duel.YGOSharp;
using MDPro3.UI.ServantUI;
using Cysharp.Threading.Tasks;

namespace MDPro3.Servant
{
    public class Appearance : Servant
    {

        [Header("Appearance")]
        [HideInInspector] public SelectionToggle_AppearanceGenre lastSelectedToggle;
        [HideInInspector] public SelectionToggle_AppearanceItem lastSelectedItem;

        #region Assets

        public static Sprite duelFace0;
        public static Sprite duelFace1;
        public static Sprite watchFace0;
        public static Sprite watchFace1;
        public static Sprite replayFace0;
        public static Sprite replayFace1;
        public static Sprite duelFace0Tag;
        public static Sprite duelFace1Tag;
        public static Sprite watchFace0Tag;
        public static Sprite watchFace1Tag;
        public static Sprite replayFace0Tag;
        public static Sprite replayFace1Tag;
        public static Sprite defaultFace0;
        public static Sprite defaultFace1;

        public static Material duelFrameMat0;
        public static Material duelFrameMat1;
        public static Material watchFrameMat0;
        public static Material watchFrameMat1;
        public static Material replayFrameMat0;
        public static Material replayFrameMat1;
        public static Material duelFrameMat0Tag;
        public static Material duelFrameMat1Tag;
        public static Material watchFrameMat0Tag;
        public static Material watchFrameMat1Tag;
        public static Material replayFrameMat0Tag;
        public static Material replayFrameMat1Tag;

        public static Material duelProtector0;
        public static Material duelProtector1;
        public static Material watchProtector0;
        public static Material watchProtector1;
        public static Material replayProtector0;
        public static Material replayProtector1;
        public static Material duelProtector0Tag;
        public static Material duelProtector1Tag;
        public static Material watchProtector0Tag;
        public static Material watchProtector1Tag;
        public static Material replayProtector0Tag;
        public static Material replayProtector1Tag;

        public static Material matForFace;
        public static string player = "0";
        public const string meString = "Me";
        public const string opString = "Op";
        public const string meTagString = "MeTag";
        public const string opTagString = "OpTag";

        #endregion

        private const string FaceFrameMaterialCode = "1030001";

        public enum Condition
        {
            Duel,
            Watch,
            Replay,
            DeckEditor
        }
        public static Condition condition = Condition.Duel;
        public void SwitchCondition(Condition condition)
        {
            Appearance.condition = condition;
        }

        #region Servant

        public override int Depth => 7;
        protected override bool ShowLine => false;
        protected override float SubBlackAlpha => 0.9f;
        public override void Initialize()
        {
            base.Initialize();
            _ = LoadSettingAssets();
        }

        public override void OnReturn()
        {
            if (inTransition) return;
            if (returnAction != null)
            {
                returnAction.Invoke();
                return;
            }
            AudioManager.PlaySE("SE_MENU_CANCEL");
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected == null)
                OnExit();
            else if (Cursor.lockState == CursorLockMode.None)
                OnExit();
            else if (selected.TryGetComponent<SelectionToggle_AppearanceItem>(out _) 
                || selected == GetUI<AppearanceUI>().InputPlayerName.gameObject)
            {
                if (lastSelectedToggle != null)
                    lastSelectedToggle.GetSelectable().Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                OnExit();
        }

        public override void OnExit()
        {
            if (returnAction != null) { returnAction.Invoke(); return; }
            if (condition != Condition.DeckEditor)
            {
                if (Program.instance.currentSubServant == this)
                    Program.instance.ShowSubServant(Program.instance.setting);
                else
                    Program.instance.ShiftToServant(Program.instance.setting);
            }
            else
            {
                Program.instance.ShiftToServant(Program.instance.deckEditor);
            }
        }

        public override void PerFrameFunction()
        {
            if (NeedResponseInput())
            {
                if (UserInput.WasLeftShoulderPressed)
                    if (GetUI<AppearanceUI>().CanSwitchPlayer())
                        GetUI<AppearanceUI>().OnPlayerLeft();
                if (UserInput.WasRightShoulderPressed)
                    if (GetUI<AppearanceUI>().CanSwitchPlayer())
                        GetUI<AppearanceUI>().OnPlayerRight();

                if (UserInput.WasGamepadButtonNorthPressed)
                    if (GetUI<AppearanceUI>().InPickupPage())
                        GetUI<AppearanceUI>().OnPickupChange();

                if (UserInput.MouseRightDown || UserInput.WasCancelPressed)
                    OnReturn();
            }
        }

        public override void Select(bool forced = false)
        {
            if (!forced && !UserInput.NeedDefaultSelect())
                return;

            if (lastSelectable != null)
            {
                if (lastSelectable.TryGetComponent<SelectionToggle_CharacterItem>(out _)
                    || lastSelectable.TryGetComponent<SelectionToggle_CharacterSeries>(out _))
                    lastSelectable.Select();
                else
                    servantUI.SelectDefaultSelectable();
            }
            else
                servantUI.SelectDefaultSelectable();
        }

        #endregion

        public static bool loaded;
        public async UniTask LoadSettingAssets()
        {
            if (!loaded && matForFace != null) return;
            loaded = false;
            await LoadSettingAssetsCore();
        }

        private static void ApplyFrameTexture(Material material, Sprite sprite)
        {
            if (material != null && sprite != null)
                material.SetTexture("_ProfileFrameTex", sprite.texture);
        }

        private async UniTask LoadSettingAssetsCore()
        {
            matForFace = await ABLoader.LoadFrameMaterial(FaceFrameMaterialCode);

            #region Face
            duelFace0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace0", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 0);
            duelFace1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace1", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 1);
            duelFace0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace0Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 2);
            duelFace1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFace1Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 3);
            watchFace0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFace0", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 0);
            watchFace1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFace1", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 1);
            watchFace0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFace0Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 2);
            watchFace1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFace1Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 3);
            replayFace0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFace0", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 0);
            replayFace1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFace1", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 1);
            replayFace0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFace0Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 2);
            replayFace1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFace1Tag", Program.items.faces[0].id.ToString()), Items.ItemType.Face, 3);
            defaultFace0 = await Program.items.LoadConcreteItemIconAsync("1010039", Items.ItemType.Face);
            defaultFace1 = await Program.items.LoadConcreteItemIconAsync("1010001", Items.ItemType.Face);

            #endregion

            #region Frame

            var duelFrame0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFrame0", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            duelFrameMat0 = await ABLoader.LoadFrameMaterial(Config.Get("DuelFrame0", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat0, duelFrame0);

            var duelFrame1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFrame1", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            duelFrameMat1 = await ABLoader.LoadFrameMaterial(Config.Get("DuelFrame1", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat1, duelFrame1);

            var duelFrame0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFrame0Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            duelFrameMat0Tag = await ABLoader.LoadFrameMaterial(Config.Get("DuelFrame0Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat0Tag, duelFrame0Tag);

            var duelFrame1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("DuelFrame1Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            duelFrameMat1Tag = await ABLoader.LoadFrameMaterial(Config.Get("DuelFrame1Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(duelFrameMat1Tag, duelFrame1Tag);

            var watchFrame0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFrame0", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            watchFrameMat0 = await ABLoader.LoadFrameMaterial(Config.Get("WatchFrame0", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat0, watchFrame0);

            var watchFrame1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFrame1", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            watchFrameMat1 = await ABLoader.LoadFrameMaterial(Config.Get("WatchFrame1", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat1, watchFrame1);

            var watchFrame0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFrame0Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            watchFrameMat0Tag = await ABLoader.LoadFrameMaterial(Config.Get("WatchFrame0Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat0Tag, watchFrame0Tag);

            var watchFrame1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("WatchFrame1Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            watchFrameMat1Tag = await ABLoader.LoadFrameMaterial(Config.Get("WatchFrame1Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(watchFrameMat1Tag, watchFrame1Tag);

            var replayFrame0 = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFrame0", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            replayFrameMat0 = await ABLoader.LoadFrameMaterial(Config.Get("ReplayFrame0", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat0, replayFrame0);

            var replayFrame1 = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFrame1", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            replayFrameMat1 = await ABLoader.LoadFrameMaterial(Config.Get("ReplayFrame1", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat1, replayFrame1);

            var replayFrame0Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFrame0Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            replayFrameMat0Tag = await ABLoader.LoadFrameMaterial(Config.Get("ReplayFrame0Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat0Tag, replayFrame0Tag);

            var replayFrame1Tag = await Program.items.LoadConcreteItemIconAsync(Config.Get("ReplayFrame1Tag", Program.items.frames[0].id.ToString()), Items.ItemType.Frame);
            replayFrameMat1Tag = await ABLoader.LoadFrameMaterial(Config.Get("ReplayFrame1Tag", Program.items.frames[0].id.ToString()));
            ApplyFrameTexture(replayFrameMat1Tag, replayFrame1Tag);

            #endregion

            #region Protector

            duelProtector0 = await ABLoader.LoadProtectorMaterial(Config.Get("DuelProtector0", Program.items.protectors[0].id.ToString()), default);
            duelProtector1 = await ABLoader.LoadProtectorMaterial(Config.Get("DuelProtector1", Program.items.protectors[0].id.ToString()), default);
            duelProtector0Tag = await ABLoader.LoadProtectorMaterial(Config.Get("DuelProtector0Tag", Program.items.protectors[0].id.ToString()), default);
            duelProtector1Tag = await ABLoader.LoadProtectorMaterial(Config.Get("DuelProtector1Tag", Program.items.protectors[0].id.ToString()), default);
            watchProtector0 = await ABLoader.LoadProtectorMaterial(Config.Get("WatchProtector0", Program.items.protectors[0].id.ToString()), default);
            watchProtector1 = await ABLoader.LoadProtectorMaterial(Config.Get("WatchProtector1", Program.items.protectors[0].id.ToString()), default);
            watchProtector0Tag = await ABLoader.LoadProtectorMaterial(Config.Get("WatchProtector0Tag", Program.items.protectors[0].id.ToString()), default);
            watchProtector1Tag = await ABLoader.LoadProtectorMaterial(Config.Get("WatchProtector1Tag", Program.items.protectors[0].id.ToString()), default);
            replayProtector0 = await ABLoader.LoadProtectorMaterial(Config.Get("ReplayProtector0", Program.items.protectors[0].id.ToString()), default);
            replayProtector1 = await ABLoader.LoadProtectorMaterial(Config.Get("ReplayProtector1", Program.items.protectors[0].id.ToString()), default);
            replayProtector0Tag = await ABLoader.LoadProtectorMaterial(Config.Get("ReplayProtector0Tag", Program.items.protectors[0].id.ToString()), default);
            replayProtector1Tag = await ABLoader.LoadProtectorMaterial(Config.Get("ReplayProtector1Tag", Program.items.protectors[0].id.ToString()), default);

            #endregion

            loaded = true;
            if (Program.instance.currentServant == Program.instance.room)
                Program.instance.room.Realize();
        }

    }
}
