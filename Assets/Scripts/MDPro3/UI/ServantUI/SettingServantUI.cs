using MDPro3.Duel;
using MDPro3.Duel.YGOSharp;
using MDPro3.Net;
using MDPro3.Servant;
using MDPro3.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.Rendering.Universal;
using static MDPro3.CardRenderer;
using ShadowResolution = UnityEngine.Rendering.Universal.ShadowResolution;


namespace MDPro3.UI.ServantUI
{
    public class SettingServantUI : ServantUI
    {
        [Header("Setting Servant UI")]
        [SerializeField] private SelectionToggle_Setting defaultToggle;

        #region Elements

        #region Toggles

        private const string LABEL_STG_SYSTEM = "ToggleSystem";
        private SelectionToggle_Setting m_ToggleSystem;
        private SelectionToggle_Setting ToggleSystem =>
            m_ToggleSystem = m_ToggleSystem != null ? m_ToggleSystem
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_SYSTEM);

        private const string LABEL_STG_DUEL = "ToggleDuel";
        private SelectionToggle_Setting m_ToggleDuel;
        private SelectionToggle_Setting ToggleDuel =>
            m_ToggleDuel = m_ToggleDuel != null ? m_ToggleDuel
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_DUEL);

        private const string LABEL_STG_WATCH = "ToggleWatch";
        private SelectionToggle_Setting m_ToggleWatch;
        private SelectionToggle_Setting ToggleWatch =>
            m_ToggleWatch = m_ToggleWatch != null ? m_ToggleWatch
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_WATCH);

        private const string LABEL_STG_REPLAY = "ToggleReplay";
        private SelectionToggle_Setting m_ToggleReplay;
        private SelectionToggle_Setting ToggleReplay =>
            m_ToggleReplay = m_ToggleReplay != null ? m_ToggleReplay
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_REPLAY);

        private const string LABEL_STG_PORT = "TogglePort";
        private SelectionToggle_Setting m_TogglePort;
        private SelectionToggle_Setting TogglePort =>
            m_TogglePort = m_TogglePort != null ? m_TogglePort
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_PORT);

        private const string LABEL_STG_EXPANSIONS = "ToggleExpansions";
        private SelectionToggle_Setting m_ToggleExpansions;
        private SelectionToggle_Setting ToggleExpansions =>
            m_ToggleExpansions = m_ToggleExpansions != null ? m_ToggleExpansions
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_EXPANSIONS);

        private const string LABEL_STG_ABOUT = "ToggleAbout";
        private SelectionToggle_Setting m_ToggleAbout;
        private SelectionToggle_Setting ToggleAbout =>
            m_ToggleAbout = m_ToggleAbout != null ? m_ToggleAbout
            : Manager.GetElement<SelectionToggle_Setting>(LABEL_STG_ABOUT);

        private const string LABEL_SBN_RETRY = "ButtonRetry";
        private SelectionButton m_ButtonRetry;
        private SelectionButton ButtonRetry =>
            m_ButtonRetry = m_ButtonRetry != null ? m_ButtonRetry
            : Manager.GetElement<SelectionButton>(LABEL_SBN_RETRY);

        private const string LABEL_SBN_SURRENDER = "ButtonSurrender";
        private SelectionButton m_ButtonSurrender;
        private SelectionButton ButtonSurrender =>
            m_ButtonSurrender = m_ButtonSurrender != null ? m_ButtonSurrender
            : Manager.GetElement<SelectionButton>(LABEL_SBN_SURRENDER);

        #endregion

        #region System

        private const string LABEL_SBN_BGM = "BGM";
        private SelectionButton_Setting m_ButtonBGM;
        private SelectionButton_Setting ButtonBGM =>
            m_ButtonBGM = m_ButtonBGM != null ? m_ButtonBGM
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BGM);

        private const string LABEL_SBN_SE = "SE";
        private SelectionButton_Setting m_ButtonSE;
        private SelectionButton_Setting ButtonSE =>
            m_ButtonSE = m_ButtonSE != null ? m_ButtonSE
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SE);

        private const string LABEL_SBN_VOICE = "Voice";
        private SelectionButton_Setting m_ButtonVoice;
        private SelectionButton_Setting ButtonVoice =>
            m_ButtonVoice = m_ButtonVoice != null ? m_ButtonVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_VOICE);

        private const string LABEL_SBN_SCREENMODE = "ScreenMode";
        private SelectionButton_Setting m_ButtonScreenMode;
        private SelectionButton_Setting ButtonScreenMode =>
            m_ButtonScreenMode = m_ButtonScreenMode != null ? m_ButtonScreenMode
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SCREENMODE);

        private const string LABEL_SBN_RESOLUTION = "Resolution";
        private SelectionButton_Setting m_ButtonResolution;
        private SelectionButton_Setting ButtonResolution =>
            m_ButtonResolution = m_ButtonResolution != null ? m_ButtonResolution
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_RESOLUTION);

        private const string LABEL_SBN_SCALE = "Scale";
        private SelectionButton_Setting m_ButtonScale;
        private SelectionButton_Setting ButtonScale =>
            m_ButtonScale = m_ButtonScale != null ? m_ButtonScale
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SCALE);

        private const string LABEL_SBN_QUALITY = "Quality";
        private SelectionButton_Setting m_ButtonQuality;
        private SelectionButton_Setting ButtonQuality =>
            m_ButtonQuality = m_ButtonQuality != null ? m_ButtonQuality
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_QUALITY);

        private const string LABEL_SBN_FAA = "FAA";
        private SelectionButton_Setting m_ButtonFAA;
        private SelectionButton_Setting ButtonFAA =>
            m_ButtonFAA = m_ButtonFAA != null ? m_ButtonFAA
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_FAA);

        private const string LABEL_SBN_AAA = "AAA";
        private SelectionButton_Setting m_ButtonAAA;
        private SelectionButton_Setting ButtonAAA =>
            m_ButtonAAA = m_ButtonAAA != null ? m_ButtonAAA
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_AAA);

        private const string LABEL_SBN_SHADOW = "Shadow";
        private SelectionButton_Setting m_ButtonShadow;
        private SelectionButton_Setting ButtonShadow =>
            m_ButtonShadow = m_ButtonShadow != null ? m_ButtonShadow
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SHADOW);

        private const string LABEL_SBN_FPS = "FPS";
        private SelectionButton_Setting m_ButtonFPS;
        private SelectionButton_Setting ButtonFPS =>
            m_ButtonFPS = m_ButtonFPS != null ? m_ButtonFPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_FPS);

        private const string LABEL_SBN_SHOWFPS = "ShowFPS";
        private SelectionButton_Setting m_ButtonShowFPS;
        private SelectionButton_Setting ButtonShowFPS =>
            m_ButtonShowFPS = m_ButtonShowFPS != null ? m_ButtonShowFPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_SHOWFPS);

        private const string LABEL_SBN_RUMBLE = "Rumble";
        private SelectionButton_Setting m_ButtonRumble;
        private SelectionButton_Setting ButtonRumble =>
            m_ButtonRumble = m_ButtonRumble != null ? m_ButtonRumble
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_RUMBLE);

        private const string LABEL_SBN_Confirm = "Confirm";
        private SelectionButton_Setting m_ButtonConfirm;
        private SelectionButton_Setting ButtonConfirm =>
            m_ButtonConfirm = m_ButtonConfirm != null ? m_ButtonConfirm
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_Confirm);

        private const string LABEL_SBN_LAYOUT = "Layout";
        private SelectionButton_Setting m_ButtonLayout;
        private SelectionButton_Setting ButtonLayout =>
            m_ButtonLayout = m_ButtonLayout != null ? m_ButtonLayout
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_LAYOUT);

        private const string LABEL_SBN_BACKGROUND = "Background";
        private SelectionButton_Setting m_ButtonBackground;
        private SelectionButton_Setting ButtonBackground =>
            m_ButtonBackground = m_ButtonBackground != null ? m_ButtonBackground
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BACKGROUND);

        private const string LABEL_SBN_BGMBY = "BgmBy";
        private SelectionButton_Setting m_ButtonBgmBy;
        private SelectionButton_Setting ButtonBgmBy =>
            m_ButtonBgmBy = m_ButtonBgmBy != null ? m_ButtonBgmBy
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_BGMBY);

        private const string LABEL_SBN_CARDSTYLE = "CardStyle";
        private SelectionButton_Setting m_ButtonCardStyle;
        private SelectionButton_Setting ButtonCardStyle =>
            m_ButtonCardStyle = m_ButtonCardStyle != null ? m_ButtonCardStyle
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_CARDSTYLE);

        private const string LABEL_SBN_VIDEO_CARD = "VideoCard";
        private SelectionButton_Setting m_ButtonVideoCard;
        private SelectionButton_Setting ButtonVideoCard =>
            m_ButtonVideoCard = m_ButtonVideoCard != null ? m_ButtonVideoCard
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_VIDEO_CARD);

        private const string LABEL_SBN_CARDLANGUAGE = "CardLanguage";
        private SelectionButton_Setting m_ButtonCardLanguage;
        private SelectionButton_Setting ButtonCardLanguage =>
            m_ButtonCardLanguage = m_ButtonCardLanguage != null ? m_ButtonCardLanguage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_CARDLANGUAGE);

        private const string LABEL_SBN_LANGUAGE = "Language";
        private SelectionButton_Setting m_ButtonLanguage;
        private SelectionButton_Setting ButtonLanguage =>
            m_ButtonLanguage = m_ButtonLanguage != null ? m_ButtonLanguage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_LANGUAGE);

        #endregion

        #region Duel

        private const string LABEL_SBN_DUELAPPEARANCE = "DuelAppearance";
        private SelectionButton_Setting m_ButtonDuelAppearance;
        private SelectionButton_Setting ButtonDuelAppearance =>
            m_ButtonDuelAppearance = m_ButtonDuelAppearance != null ? m_ButtonDuelAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAPPEARANCE);

        private const string LABEL_SBN_WATCHAPPEARANCE = "WatchAppearance";
        private SelectionButton_Setting m_ButtonWatchAppearance;
        private SelectionButton_Setting ButtonWatchAppearance =>
            m_ButtonWatchAppearance = m_ButtonWatchAppearance != null ? m_ButtonWatchAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAPPEARANCE);

        private const string LABEL_SBN_REPLAYAPPEARANCE = "ReplayAppearance";
        private SelectionButton_Setting m_ButtonReplayAppearance;
        private SelectionButton_Setting ButtonReplayAppearance =>
            m_ButtonReplayAppearance = m_ButtonReplayAppearance != null ? m_ButtonReplayAppearance
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAPPEARANCE);

        private const string LABEL_SBN_DUELCHARACTER = "DuelCharacter";
        private SelectionButton_Setting m_ButtonDuelCharacter;
        private SelectionButton_Setting ButtonDuelCharacter =>
            m_ButtonDuelCharacter = m_ButtonDuelCharacter != null ? m_ButtonDuelCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCHARACTER);

        private const string LABEL_SBN_WATCHCHARACTER = "WatchCharacter";
        private SelectionButton_Setting m_ButtonWatchCharacter;
        private SelectionButton_Setting ButtonWatchCharacter =>
            m_ButtonWatchCharacter = m_ButtonWatchCharacter != null ? m_ButtonWatchCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCHARACTER);

        private const string LABEL_SBN_REPLAYCHARACTER = "ReplayCharacter";
        private SelectionButton_Setting m_ButtonReplayCharacter;
        private SelectionButton_Setting ButtonReplayCharacter =>
            m_ButtonReplayCharacter = m_ButtonReplayCharacter != null ? m_ButtonReplayCharacter
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCHARACTER);

        private const string LABEL_SBN_DUELVOICE = "DuelVoice";
        private SelectionButton_Setting m_ButtonDuelVoice;
        private SelectionButton_Setting ButtonDuelVoice =>
            m_ButtonDuelVoice = m_ButtonDuelVoice != null ? m_ButtonDuelVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELVOICE);

        private const string LABEL_SBN_WATCHVOICE = "WatchVoice";
        private SelectionButton_Setting m_ButtonWatchVoice;
        private SelectionButton_Setting ButtonWatchVoice =>
            m_ButtonWatchVoice = m_ButtonWatchVoice != null ? m_ButtonWatchVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHVOICE);

        private const string LABEL_SBN_REPLAYVOICE = "ReplayVoice";
        private SelectionButton_Setting m_ButtonReplayVoice;
        private SelectionButton_Setting ButtonReplayVoice =>
            m_ButtonReplayVoice = m_ButtonReplayVoice != null ? m_ButtonReplayVoice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYVOICE);

        private const string LABEL_SBN_DUELCLOSEUP = "DuelCloseup";
        private SelectionButton_Setting m_ButtonDuelCloseup;
        private SelectionButton_Setting ButtonDuelCloseup =>
            m_ButtonDuelCloseup = m_ButtonDuelCloseup != null ? m_ButtonDuelCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCLOSEUP);

        private const string LABEL_SBN_WATCHCLOSEUP = "WatchCloseup";
        private SelectionButton_Setting m_ButtonWatchCloseup;
        private SelectionButton_Setting ButtonWatchCloseup =>
            m_ButtonWatchCloseup = m_ButtonWatchCloseup != null ? m_ButtonWatchCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCLOSEUP);

        private const string LABEL_SBN_REPLAYCLOSEUP = "ReplayCloseup";
        private SelectionButton_Setting m_ButtonReplayCloseup;
        private SelectionButton_Setting ButtonReplayCloseup =>
            m_ButtonReplayCloseup = m_ButtonReplayCloseup != null ? m_ButtonReplayCloseup
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCLOSEUP);

        private const string LABEL_SBN_DUELPENDULUM = "DuelPendulum";
        private SelectionButton_Setting m_ButtonDuelPendulum;
        private SelectionButton_Setting ButtonDuelPendulum =>
            m_ButtonDuelPendulum = m_ButtonDuelPendulum != null ? m_ButtonDuelPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELPENDULUM);

        private const string LABEL_SBN_WATCHPENDULUM = "WatchPendulum";
        private SelectionButton_Setting m_ButtonWatchPendulum;
        private SelectionButton_Setting ButtonWatchPendulum =>
            m_ButtonWatchPendulum = m_ButtonWatchPendulum != null ? m_ButtonWatchPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHPENDULUM);

        private const string LABEL_SBN_REPLAYPENDULUM = "ReplayPendulum";
        private SelectionButton_Setting m_ButtonReplayPendulum;
        private SelectionButton_Setting ButtonReplayPendulum =>
            m_ButtonReplayPendulum = m_ButtonReplayPendulum != null ? m_ButtonReplayPendulum
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYPENDULUM);

        private const string LABEL_SBN_DUELCUTIN = "DuelCutin";
        private SelectionButton_Setting m_ButtonDuelCutin;
        private SelectionButton_Setting ButtonDuelCutin =>
            m_ButtonDuelCutin = m_ButtonDuelCutin != null ? m_ButtonDuelCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCUTIN);

        private const string LABEL_SBN_WATCHCUTIN = "WatchCutin";
        private SelectionButton_Setting m_ButtonWatchCutin;
        private SelectionButton_Setting ButtonWatchCutin =>
            m_ButtonWatchCutin = m_ButtonWatchCutin != null ? m_ButtonWatchCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCUTIN);

        private const string LABEL_SBN_REPLAYCUTIN = "ReplayCutin";
        private SelectionButton_Setting m_ButtonReplayCutin;
        private SelectionButton_Setting ButtonReplayCutin =>
            m_ButtonReplayCutin = m_ButtonReplayCutin != null ? m_ButtonReplayCutin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCUTIN);

        private const string LABEL_SBN_DUELEFFECT = "DuelEffect";
        private SelectionButton_Setting m_ButtonDuelEffect;
        private SelectionButton_Setting ButtonDuelEffect =>
            m_ButtonDuelEffect = m_ButtonDuelEffect != null ? m_ButtonDuelEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELEFFECT);

        private const string LABEL_SBN_WATCHEFFECT = "WatchEffect";
        private SelectionButton_Setting m_ButtonWatchEffect;
        private SelectionButton_Setting ButtonWatchEffect =>
            m_ButtonWatchEffect = m_ButtonWatchEffect != null ? m_ButtonWatchEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHEFFECT);

        private const string LABEL_SBN_REPLAYEFFECT = "ReplayEffect";
        private SelectionButton_Setting m_ButtonReplayEffect;
        private SelectionButton_Setting ButtonReplayEffect =>
            m_ButtonReplayEffect = m_ButtonReplayEffect != null ? m_ButtonReplayEffect
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYEFFECT);

        private const string LABEL_SBN_DUELCHAIN = "DuelChain";
        private SelectionButton_Setting m_ButtonDuelChain;
        private SelectionButton_Setting ButtonDuelChain =>
            m_ButtonDuelChain = m_ButtonDuelChain != null ? m_ButtonDuelChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCHAIN);

        private const string LABEL_SBN_WATCHCHAIN = "WatchChain";
        private SelectionButton_Setting m_ButtonWatchChain;
        private SelectionButton_Setting ButtonWatchChain =>
            m_ButtonWatchChain = m_ButtonWatchChain != null ? m_ButtonWatchChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCHAIN);

        private const string LABEL_SBN_REPLAYCHAIN = "ReplayChain";
        private SelectionButton_Setting m_ButtonReplayChain;
        private SelectionButton_Setting ButtonReplayChain =>
            m_ButtonReplayChain = m_ButtonReplayChain != null ? m_ButtonReplayChain
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCHAIN);

        private const string LABEL_SBN_DUELDICE = "DuelDice";
        private SelectionButton_Setting m_ButtonDuelDice;
        private SelectionButton_Setting ButtonDuelDice =>
            m_ButtonDuelDice = m_ButtonDuelDice != null ? m_ButtonDuelDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELDICE);

        private const string LABEL_SBN_WATCHDICE = "WatchDice";
        private SelectionButton_Setting m_ButtonWatchDice;
        private SelectionButton_Setting ButtonWatchDice =>
            m_ButtonWatchDice = m_ButtonWatchDice != null ? m_ButtonWatchDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHDICE);

        private const string LABEL_SBN_REPLAYDICE = "ReplayDice";
        private SelectionButton_Setting m_ButtonReplayDice;
        private SelectionButton_Setting ButtonReplayDice =>
            m_ButtonReplayDice = m_ButtonReplayDice != null ? m_ButtonReplayDice
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYDICE);

        private const string LABEL_SBN_DUELCOIN = "DuelCoin";
        private SelectionButton_Setting m_ButtonDuelCoin;
        private SelectionButton_Setting ButtonDuelCoin =>
            m_ButtonDuelCoin = m_ButtonDuelCoin != null ? m_ButtonDuelCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELCOIN);

        private const string LABEL_SBN_WATCHCOIN = "WatchCoin";
        private SelectionButton_Setting m_ButtonWatchCoin;
        private SelectionButton_Setting ButtonWatchCoin =>
            m_ButtonWatchCoin = m_ButtonWatchCoin != null ? m_ButtonWatchCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHCOIN);

        private const string LABEL_SBN_REPLAYCOIN = "ReplayCoin";
        private SelectionButton_Setting m_ButtonReplayCoin;
        private SelectionButton_Setting ButtonReplayCoin =>
            m_ButtonReplayCoin = m_ButtonReplayCoin != null ? m_ButtonReplayCoin
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYCOIN);

        private const string LABEL_SBN_DUELAUTOINFO = "DuelAutoInfo";
        private SelectionButton_Setting m_ButtonDuelAutoInfo;
        private SelectionButton_Setting ButtonDuelAutoInfo =>
            m_ButtonDuelAutoInfo = m_ButtonDuelAutoInfo != null ? m_ButtonDuelAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAUTOINFO);

        private const string LABEL_SBN_WATCHAUTOINFO = "WatchAutoInfo";
        private SelectionButton_Setting m_ButtonWatchAutoInfo;
        private SelectionButton_Setting ButtonWatchAutoInfo =>
            m_ButtonWatchAutoInfo = m_ButtonWatchAutoInfo != null ? m_ButtonWatchAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAUTOINFO);

        private const string LABEL_SBN_REPLAYAUTOINFO = "ReplayAutoInfo";
        private SelectionButton_Setting m_ButtonReplayAutoInfo;
        private SelectionButton_Setting ButtonReplayAutoInfo =>
            m_ButtonReplayAutoInfo = m_ButtonReplayAutoInfo != null ? m_ButtonReplayAutoInfo
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAUTOINFO);

        private const string LABEL_SBN_DUELFACEDOWN = "DuelFaceDown";
        private SelectionButton_Setting m_ButtonDuelFaceDown;
        private SelectionButton_Setting ButtonDuelFaceDown =>
            m_ButtonDuelFaceDown = m_ButtonDuelFaceDown != null ? m_ButtonDuelFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELFACEDOWN);

        private const string LABEL_SBN_WATCHFACEDOWN = "WatchFaceDown";
        private SelectionButton_Setting m_ButtonWatchFaceDown;
        private SelectionButton_Setting ButtonWatchFaceDown =>
            m_ButtonWatchFaceDown = m_ButtonWatchFaceDown != null ? m_ButtonWatchFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHFACEDOWN);

        private const string LABEL_SBN_REPLAYFACEDOWN = "ReplayFaceDown";
        private SelectionButton_Setting m_ButtonReplayFaceDown;
        private SelectionButton_Setting ButtonReplayFaceDown =>
            m_ButtonReplayFaceDown = m_ButtonReplayFaceDown != null ? m_ButtonReplayFaceDown
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYFACEDOWN);

        private const string LABEL_SBN_DUELPLAYERMESSAGE = "DuelPlayerMessage";
        private SelectionButton_Setting m_ButtonDuelPlayerMessage;
        private SelectionButton_Setting ButtonDuelPlayerMessage =>
            m_ButtonDuelPlayerMessage = m_ButtonDuelPlayerMessage != null ? m_ButtonDuelPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELPLAYERMESSAGE);

        private const string LABEL_SBN_WATCHPLAYERMESSAGE = "WatchPlayerMessage";
        private SelectionButton_Setting m_ButtonWatchPlayerMessage;
        private SelectionButton_Setting ButtonWatchPlayerMessage =>
            m_ButtonWatchPlayerMessage = m_ButtonWatchPlayerMessage != null ? m_ButtonWatchPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHPLAYERMESSAGE);

        private const string LABEL_SBN_REPLAYPLAYERMESSAGE = "ReplayPlayerMessage";
        private SelectionButton_Setting m_ButtonReplayPlayerMessage;
        private SelectionButton_Setting ButtonReplayPlayerMessage =>
            m_ButtonReplayPlayerMessage = m_ButtonReplayPlayerMessage != null ? m_ButtonReplayPlayerMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYPLAYERMESSAGE);

        private const string LABEL_SBN_DUELSYSTEMMESSAGE = "DuelSystemMessage";
        private SelectionButton_Setting m_ButtonDuelSystemMessage;
        private SelectionButton_Setting ButtonDuelSystemMessage =>
            m_ButtonDuelSystemMessage = m_ButtonDuelSystemMessage != null ? m_ButtonDuelSystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELSYSTEMMESSAGE);

        private const string LABEL_SBN_WATCHSYSTEMMESSAGE = "WatchSystemMessage";
        private SelectionButton_Setting m_ButtonWatchSystemMessage;
        private SelectionButton_Setting ButtonWatchSystemMessage =>
            m_ButtonWatchSystemMessage = m_ButtonWatchSystemMessage != null ? m_ButtonWatchSystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHSYSTEMMESSAGE);

        private const string LABEL_SBN_REPLAYSYSTEMMESSAGE = "ReplaySystemMessage";
        private SelectionButton_Setting m_ButtonReplaySystemMessage;
        private SelectionButton_Setting ButtonReplaySystemMessage =>
            m_ButtonReplaySystemMessage = m_ButtonReplaySystemMessage != null ? m_ButtonReplaySystemMessage
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYSYSTEMMESSAGE);

        private const string LABEL_SBN_DUELACC = "DuelAcc";
        private SelectionButton_Setting m_ButtonDuelAcc;
        private SelectionButton_Setting ButtonDuelAcc =>
            m_ButtonDuelAcc = m_ButtonDuelAcc != null ? m_ButtonDuelAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELACC);

        private const string LABEL_SBN_WATCHACC = "WatchAcc";
        private SelectionButton_Setting m_ButtonWatchAcc;
        private SelectionButton_Setting ButtonWatchAcc =>
            m_ButtonWatchAcc = m_ButtonWatchAcc != null ? m_ButtonWatchAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHACC);

        private const string LABEL_SBN_REPLAYACC = "ReplayAcc";
        private SelectionButton_Setting m_ButtonReplayAcc;
        private SelectionButton_Setting ButtonReplayAcc =>
            m_ButtonReplayAcc = m_ButtonReplayAcc != null ? m_ButtonReplayAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYACC);

        private const string LABEL_SBN_DUELAUTOACC = "DuelAutoAcc";
        private SelectionButton_Setting m_ButtonDuelAutoAcc;
        private SelectionButton_Setting ButtonDuelAutoAcc =>
            m_ButtonDuelAutoAcc = m_ButtonDuelAutoAcc != null ? m_ButtonDuelAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DUELAUTOACC);

        private const string LABEL_SBN_WATCHAUTOACC = "WatchAutoAcc";
        private SelectionButton_Setting m_ButtonWatchAutoAcc;
        private SelectionButton_Setting ButtonWatchAutoAcc =>
            m_ButtonWatchAutoAcc = m_ButtonWatchAutoAcc != null ? m_ButtonWatchAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_WATCHAUTOACC);

        private const string LABEL_SBN_REPLAYAUTOACC = "ReplayAutoAcc";
        private SelectionButton_Setting m_ButtonReplayAutoAcc;
        private SelectionButton_Setting ButtonReplayAutoAcc =>
            m_ButtonReplayAutoAcc = m_ButtonReplayAutoAcc != null ? m_ButtonReplayAutoAcc
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_REPLAYAUTOACC);

        private const string LABEL_SBN_TIMING = "Timing";
        private SelectionButton_Setting m_ButtonTiming;
        private SelectionButton_Setting ButtonTiming =>
            m_ButtonTiming = m_ButtonTiming != null ? m_ButtonTiming
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_TIMING);

        private const string LABEL_SBN_AUTORPS = "AutoRPS";
        private SelectionButton_Setting m_ButtonAutoRPS;
        private SelectionButton_Setting ButtonAutoRPS =>
            m_ButtonAutoRPS = m_ButtonAutoRPS != null ? m_ButtonAutoRPS
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_AUTORPS);

        #endregion

        #region Expansion

        private const string LABEL_SBN_EXPANSIONSUPPORT = "ExpansionSupport";
        private SelectionButton_Setting m_ButtonExpansionSupport;
        private SelectionButton_Setting ButtonExpansionSupport =>
            m_ButtonExpansionSupport = m_ButtonExpansionSupport != null ? m_ButtonExpansionSupport
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_EXPANSIONSUPPORT);

        private const string LABEL_SBN_UPDATEPRERELEASE = "UpdatePrerelease";
        private SelectionButton_Setting m_ButtonUpdatePrerelease;
        public SelectionButton_Setting ButtonUpdatePrerelease =>
            m_ButtonUpdatePrerelease = m_ButtonUpdatePrerelease != null ? m_ButtonUpdatePrerelease
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_UPDATEPRERELEASE);

        private const string LABEL_SBN_DOWNLOAD_YPK = "DonwloadYPK";
        private SelectionButton_Setting m_ButtonDownloadYPK;
        public SelectionButton_Setting ButtonDownloadYPK =>
            m_ButtonDownloadYPK = m_ButtonDownloadYPK != null ? m_ButtonDownloadYPK
            : Manager.GetElement<SelectionButton_Setting>(LABEL_SBN_DOWNLOAD_YPK);

        #endregion

        #endregion

        public override void Initialize(Servant.Servant servant)
        {
            base.Initialize(servant);
            ButtonRetry.SetClickEvent(OnRetry);

            InitializeVolume();
            InitializeScreenMode();
            InitializeResolution();
            InitializeScale();
            InitializeQuality();
            InitializeFAA();
            InitializeAAA();
            InitializeShadow();
            InitializeFPS();
            InitializeShowFPS();
            InitializeRumble();
            InitializeConfirm();
            InitializeLayout();
            InitializeBackground();
            InitializeBgmBy();
            InitializeCardStyle();
            InitializeVideoCard();
            InitializeCardLanguage();
            InitializeLanguage();

            InitializeAppearance();
            InitializeCharacter();
            InitializeVoice();
            InitializeCloseup();
            InitializeSummon();
            InitializePendulum();
            InitializeCutin();
            InitializeEffect();
            InitializeChain();
            InitializeDice();
            InitializeCoin();
            InitializeAutoInfo();
            InitializeFaceDown();
            InitializePlayerMessage();
            InitializeSystemMessage();
            InitializeAcc();
            InitializeAutoAcc();
            InitializeTimming();
            InitializeAutoRPS();

            InitializeExpansions();
        }

        public override void SelectDefaultSelectable()
        {
            defaultToggle.GetSelectable().Select();
            defaultToggle.ScrollRectToTop();
        }

        public override void ShowEvent()
        {
            base.ShowEvent();

            if (Program.instance.currentServant == Program.instance.ocgcore)
            {
                if (OcgCore.condition == OcgCore.Condition.Duel)
                {
                    ToggleDuel.gameObject.SetActive(true);
                    ToggleWatch.gameObject.SetActive(false);
                    ToggleReplay.gameObject.SetActive(false);

                    var canRetrySolo = RoomServant.FromSolo;
                    ButtonRetry.gameObject.SetActive(canRetrySolo);
                    if (canRetrySolo)
                        ButtonRetry.SetButtonText(InterString.Get("重试"));
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(InterString.Get("投降"));
                }
                else if (OcgCore.condition == OcgCore.Condition.Watch)
                {
                    ToggleDuel.gameObject.SetActive(false);
                    ToggleWatch.gameObject.SetActive(true);
                    ToggleReplay.gameObject.SetActive(false);

                    ButtonRetry.gameObject.SetActive(false);
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(InterString.Get("退出观战"));
                }
                else if (OcgCore.condition == OcgCore.Condition.Replay)
                {
                    ToggleDuel.gameObject.SetActive(false);
                    ToggleWatch.gameObject.SetActive(false);
                    ToggleReplay.gameObject.SetActive(true);

                    ButtonRetry.gameObject.SetActive(false);
                    ButtonSurrender.gameObject.SetActive(true);
                    ButtonSurrender.SetButtonText(InterString.Get("退出回放"));
                }
                TogglePort.gameObject.SetActive(false);
                ToggleExpansions.gameObject.SetActive(false);
            }
            else
            {
                ToggleDuel.gameObject.SetActive(true);
                ToggleWatch.gameObject.SetActive(true);
                ToggleReplay.gameObject.SetActive(true);
                TogglePort.gameObject.SetActive(true);
                ToggleExpansions.gameObject.SetActive(true);

                ButtonRetry.gameObject.SetActive(false);
                ButtonSurrender.gameObject.SetActive(false);
            }
        }

        protected override void HideEvent()
        {
            base.HideEvent();
            Save();
            if (Program.instance.ocgcore.showing)
            {
                UIManager.HideBlackBack(Program.instance.setting.TransitionTime);
                UIManager.HideExitButton(Program.instance.setting.TransitionTime);
            }

            if (videoCardConfigChanged)
            {
                videoCardConfigChanged = false;
                SystemEvent.CallVideoCardConfigChangeEvent();
            }
        }

        protected override void AfterHideEvent()
        {
            base.AfterHideEvent();
            if (Program.instance.ocgcore.showing)
                UIManager.ShowFPSLeft();
        }

        private void Save()
        {
            //Non-WholeNumbers Slider Value Need be saved here;

            Config.SetFloat("BgmVol", GetBGMVolum());
            Config.SetFloat("SEVol", GetSEVolum());
            Config.SetFloat("VoiceVol", GetVoiceVolum());

            Config.SetFloat("Scale", ButtonScale.GetSliderValue());
            FpsSave();

            Config.SetFloat("DuelAcc", ButtonDuelAcc.GetSliderValue());
            Config.SetFloat("WatchAcc", ButtonWatchAcc.GetSliderValue());
            Config.SetFloat("ReplayAcc", ButtonReplayAcc.GetSliderValue());

            Config.Save();
        }

        #region System

        #region Volume

        private void InitializeVolume()
        {
            ButtonBGM.SetSliderEvent(OnBgmVolChange);
            ButtonBGM.SetSliderValue(Config.GetFloat("BgmVol", 0.7f));

            ButtonSE.SetSliderEvent(OnSeVolChange);
            ButtonSE.SetSliderValue(Config.GetFloat("SEVol", 0.7f));

            ButtonVoice.SetSliderEvent(OnVoiceVolChange);
            ButtonVoice.SetSliderValue(Config.GetFloat("VoiceVol", 0.7f));
            ButtonVoice.SetClickEvent(OnPlayTestVoice);
        }
        public float GetBGMVolum()
        {
            return ButtonBGM.GetSliderValue();
        }
        public float GetSEVolum()
        {
            return ButtonSE.GetSliderValue();
        }
        public float GetVoiceVolum()
        {
            return ButtonVoice.GetSliderValue();
        }
        private void OnPlayTestVoice()
        {
            AudioManager.PlayVoiceByResourcePath("VOICE/VoiceSample");
        }
        private void OnBgmVolChange(float vol)
        {
            AudioManager.SetBGMVol(vol);
        }
        private void OnSeVolChange(float vol)
        {
            AudioManager.SetSeVol(vol);
        }
        private void OnVoiceVolChange(float vol)
        {
            AudioManager.SetVoiceVol(vol);
        }

        #endregion

        #region Screen Mode

        private void InitializeScreenMode()
        {
            ButtonScreenMode.SetClickEvent(OnScreenModeChange);
            string value = Config.Get("ScreenMode", "1");
            if (value == "0")
            {
                ButtonScreenMode.SetModeText(InterString.Get("独占全屏"));
                ButtonScreenMode.SetNoteText(InterString.Get("独占全屏（仅Windows端有效）"));
            }
            else if (value == "1")
            {
                ButtonScreenMode.SetModeText(InterString.Get("窗口全屏"));
                ButtonScreenMode.SetNoteText(InterString.Get("全屏显示"));
            }
            else
            {
                ButtonScreenMode.SetModeText(InterString.Get("窗口化"));
                ButtonScreenMode.SetNoteText(InterString.Get("窗口化（仅桌面端有效）"));
            }
        }
        private void OnScreenModeChange()
        {
            List<string> selections = new()
            {
                InterString.Get("显示模式"),
                string.Empty,
                InterString.Get("独占全屏"),
                InterString.Get("窗口全屏"),
                InterString.Get("窗口化")
            };
            UIManager.ShowPopupSelection(selections, ScreenModeChange);
        }
        private void ScreenModeChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();

            if (selected == InterString.Get("独占全屏"))
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.ExclusiveFullScreen);
                ButtonScreenMode.SetModeText(InterString.Get("独占全屏"));
                ButtonScreenMode.SetNoteText(InterString.Get("独占全屏（仅Windows端有效）"));
            }
            else if (selected == InterString.Get("窗口全屏"))
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
                ButtonScreenMode.SetModeText(InterString.Get("窗口全屏"));
                ButtonScreenMode.SetNoteText(InterString.Get("全屏显示"));
            }
            else
            {
                Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, false);
                ButtonScreenMode.SetModeText(InterString.Get("窗口化"));
                ButtonScreenMode.SetNoteText(InterString.Get("窗口化（仅桌面端有效）"));
            }

            Config.Set("ScreenMode", SaveScreenMode(selected));
        }
        private string SaveScreenMode(string value)
        {
            string returnValue = "1";
            if (value == InterString.Get("独占全屏"))
                returnValue = "0";
            else if (value == InterString.Get("窗口全屏"))
                returnValue = "1";
            else if (value == InterString.Get("窗口化"))
                returnValue = "2";
            return returnValue;
        }

        #endregion

        #region Resolution

        private void InitializeResolution()
        {
            ButtonResolution.SetClickEvent(OnResolutionChange);
            SetResolutionText();
            SystemEvent.OnResolutionChange += SetResolutionText;
        }
        private void OnResolutionChange()
        {
            List<string> selections = new()
            {
                InterString.Get("分辨率"),
                string.Empty
            };
            foreach (var resolution in Screen.resolutions)
            {
                string selection = Regex.Split(resolution.ToString(), " @ ")[0];
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
                int height = int.Parse(Regex.Split(selection, " x ")[0]);
                int width = int.Parse(Regex.Split(selection, " x ")[1]);
                if (height > width)
                    (width, height) = (height, width);
                if (height > 540)
                {
                    string r = (width * 540 / height).ToString() + " x " + 540.ToString();
                    if(!selections.Contains(r))
                        selections.Add(r);
                }
                if(height > 720)
                {
                    string r = (width * 720 / height).ToString() + " x " + 720.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1080)
                {
                    string r = (width * 1080 / height).ToString() + " x " + 1080.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1200)
                {
                    string r = (width * 1200 / height).ToString() + " x " + 1200.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1440)
                {
                    string r = (width * 1440 / height).ToString() + " x " + 1440.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1600)
                {
                    string r = (width * 1600 / height).ToString() + " x " + 1600.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 1800)
                {
                    string r = (width * 1800 / height).ToString() + " x " + 1800.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                if (height > 2160)
                {
                    string r = (width * 2160 / height).ToString() + " x " + 2160.ToString();
                    if (!selections.Contains(r))
                        selections.Add(r);
                }
                selection = width.ToString() + " x " + height.ToString();
#endif
                if (!selections.Contains(selection))
                    selections.Add(selection);
            }

            //selections.Add("3600 x 1620");

            UIManager.ShowPopupSelection(selections, ResolutioChange);
        }
        private void ResolutioChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            Screen.SetResolution(int.Parse(Regex.Split(selected, " x ")[0]), int.Parse(Regex.Split(selected, " x ")[1]), Screen.fullScreen);
            Config.Set("Resolution", selected);
        }
        private void SetResolutionText()
        {
            ButtonResolution.SetModeText($"{Screen.width} x {Screen.height}");
        }

        #endregion

        #region Scale

        private void InitializeScale()
        {
            ButtonScale.SetSliderEvent(OnScaleChange);
            ButtonScale.SetSliderValue(GetScale());
        }
        private void OnScaleChange(float vol)
        {
            string value = vol.ToString();
            value = value.Length > 4 ? value.Substring(0, 4) : value;
            ButtonScale.SetModeText(value);
            Program.instance.camera_.urpAsset.renderScale = float.Parse(value);
        }
        public static float GetScale()
        {
            var defau = 1f;
#if UNITY_ANDROID || UNITY_IOS
            defau = 0.5f;
#endif
            return Config.GetFloat("Scale", defau);
        }

        #endregion

        #region Quality

        private void InitializeQuality()
        {
            ButtonQuality.SetSliderEvent(OnQualityChange);
            var configQuality = Config.GetFloat("Quality", 2f);
            ButtonQuality.SetSliderValue(configQuality);
        }
        private void OnQualityChange(float value)
        {
            string qualityText = (int)value switch
            {
                0 => InterString.Get("非常低"),
                1 => InterString.Get("低"),
                2 => InterString.Get("中等"),
                3 => InterString.Get("高"),
                4 => InterString.Get("非常高"),
                5 => InterString.Get("极致"),
                _ => InterString.Get("中等"),
            };
            Config.SetFloat("Quality", value);
            ButtonQuality.SetModeText(qualityText);
        }

        #endregion

        #region FAA

        private void InitializeFAA()
        {
            ButtonFAA.SetSliderEvent(OnFAAChange);
            ButtonFAA.SetSliderValue(Config.GetFloat("FAA", 1f));
        }

        private void OnFAAChange(float value)
        {
            value = 1f;

            var modeText = "Off";
            switch ((int)value)
            {
                case 1:
                    modeText = InterString.Get("Off");
                    Program.instance.camera_.urpAsset.msaaSampleCount = 1;
                    Program.instance.camera_.urpAssetForUI.msaaSampleCount = 1;
                    break;
                case 2:
                    modeText = "MSAA 2x";
                    Program.instance.camera_.urpAsset.msaaSampleCount = 2;
                    Program.instance.camera_.urpAssetForUI.msaaSampleCount = 2;
                    break;
                case 3:
                    modeText = "MSAA 4x";
                    Program.instance.camera_.urpAsset.msaaSampleCount = 4;
                    Program.instance.camera_.urpAssetForUI.msaaSampleCount = 4;
                    break;
                case 4:
                    modeText = "MSAA 8x";
                    Program.instance.camera_.urpAsset.msaaSampleCount = 8;
                    Program.instance.camera_.urpAssetForUI.msaaSampleCount = 8;
                    break;
            }

            ButtonFAA.SetModeText(modeText);
            Config.SetFloat("FAA", value);
        }

        #endregion

        #region AAA

        private void InitializeAAA()
        {
            ButtonAAA.SetSliderEvent(OnAAAChange);
            ButtonAAA.SetSliderValue(Config.GetFloat("AAA", 0f));
        }
        private void OnAAAChange(float value)
        {
            ButtonAAA.SetModeText(ChangeAAA(value));
            Config.SetFloat("AAA", value);
        }
        public static string ChangeAAA(float value)
        {
            var cameraData3D = Program.instance.camera_.cameraMain.GetUniversalAdditionalCameraData();

            var modeText = "Off";
            switch ((int)value)
            {
                case 0:
                    modeText = InterString.Get("Off");
                    cameraData3D.antialiasing = AntialiasingMode.None;
                    break;
                case 1:
                    modeText = "FAA";
                    cameraData3D.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case 2:
                    modeText = "SMAA Low";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.Low;
                    break;
                case 3:
                    modeText = "SMAA Medium";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.Medium;
                    break;
                case 4:
                    modeText = "SMAA High";
                    cameraData3D.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    cameraData3D.antialiasingQuality = AntialiasingQuality.High;
                    break;
                case 5:
                    modeText = "TAA";
                    cameraData3D.antialiasing = AntialiasingMode.TemporalAntiAliasing;
                    Program.instance.camera_.urpAsset.msaaSampleCount = 1;
                    Program.instance.camera_.urpAssetForUI.msaaSampleCount = 1;
                    break;
            }

            return modeText;
        }

        #endregion

        #region Shadow

        private void InitializeShadow()
        {
            ButtonShadow.SetSliderEvent(OnShadowChange);
            ButtonShadow.SetSliderValue(Config.GetFloat("Shadow", 0f));
        }
        private void OnShadowChange(float value)
        {
            ButtonShadow.SetModeText(ChangeShadow(value));
            Config.SetFloat("Shadow", value);
        }
        public static string ChangeShadow(float value)
        {
            SROptions sr = new();
            var modeText = InterString.Get("非常低");
            switch ((int)value)
            {
                case 0:
                    modeText = InterString.Get("非常低");
                    sr.MainLightShadowResolution = ShadowResolution._256;
                    sr.SupportsSoftShadows = false;
                    break;
                case 1:
                    modeText = InterString.Get("低");
                    sr.MainLightShadowResolution = ShadowResolution._512;
                    sr.SupportsSoftShadows = false;
                    break;
                case 2:
                    modeText = InterString.Get("中等");
                    sr.MainLightShadowResolution = ShadowResolution._1024;
                    sr.SupportsSoftShadows = false;
                    break;
                case 3:
                    modeText = InterString.Get("高");
                    sr.MainLightShadowResolution = ShadowResolution._2048;
                    sr.SupportsSoftShadows = true;
                    break;
                case 4:
                    modeText = InterString.Get("非常高");
                    sr.MainLightShadowResolution = ShadowResolution._4096;
                    sr.SupportsSoftShadows = true;
                    break;
            }

            return modeText;
        }

        #endregion

        #region FPS

        private void InitializeFPS()
        {
            ButtonFPS.SetSliderEvent(OnFpsChange);

            var configFPS = GetFPS();
            if (configFPS == 0)
                configFPS = 29;
            ButtonFPS.SetSliderValue(configFPS);
        }
        private void OnFpsChange(float value)
        {
            QualitySettings.vSyncCount = 0;
            if (value == 29f)
                value = 0f;
            Application.targetFrameRate = (int)value;
            ButtonFPS.SetModeText(value.ToString());
        }
        public static int ChangeFPS(float value)
        {
            QualitySettings.vSyncCount = 0;
            if (value == 29f)
                value = 0f;
            Application.targetFrameRate = (int)value;
            return (int)value;
        }
        public static int GetFPS()
        {
            var defau = 60f;
            if (DeviceInfo.OnMobile())
                defau = 30f;
            return (int)Config.GetFloat("FPS", defau);
        }
        private void FpsSave()
        {
            var config = ButtonFPS.GetSliderValue();
            if (config == 29f)
                config = 0f;
            Config.SetFloat("FPS", config);
        }

        #endregion

        #region ShowFPS

        private void InitializeShowFPS()
        {
            ButtonShowFPS.SetClickEvent(OnShowFPSClicked);

            var config = Config.GetBool("ShowFPS", true);
            ButtonShowFPS.SetModeText(InterString.Get(config ? "开" : "关"));
            ChangeShowFPS();
        }
        private void OnShowFPSClicked()
        {
            var config = Config.GetBool("ShowFPS", true);
            Config.SetBool("ShowFPS", !config);
            ButtonShowFPS.SetModeText(InterString.Get(config ? "关" : "开"));
            ChangeShowFPS();
        }
        public static void ChangeShowFPS()
        {
            if(Config.GetBool("ShowFPS", true))
                UIManager.ShowFPS();
            else
                UIManager.HideFPS();
        }

        #endregion

        #region Rumble

        private void InitializeRumble()
        {
            ButtonRumble.SetClickEvent(OnRumbleClicked);
            var config = Config.GetBool("Rumble", true);
            ButtonRumble.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnRumbleClicked()
        {
            var config = Config.GetBool("Rumble", true);
            ButtonRumble.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("Rumble", !config);
        }

        #endregion

        #region Confirm

        private void InitializeConfirm()
        {
            ButtonConfirm.SetClickEvent(OnConfirmClicked);
            var config = Config.GetBool("Confirm", false);
            ButtonConfirm.SetModeText(InterString.Get(config ? "左" : "右"));
        }

        private void OnConfirmClicked()
        {
            var config = Config.GetBool("Confirm", false);
            ButtonConfirm.SetModeText(InterString.Get(config ? "右" : "左"));
            Config.SetBool("Confirm", !config);
        }

        #endregion

        #region Layout

        private void InitializeLayout()
        {
            ButtonLayout.SetClickEvent(OnLayoutClicked);
            SetLayoutMode(Config.GetFloat("Layout", 0f));
        }
        private void OnLayoutClicked()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                InterString.Get("UI布局"),
                string.Empty,
                InterString.Get("自动判断"),
                InterString.Get("桌面布局"),
                InterString.Get("移动布局")
            };
            UIManager.ShowPopupSelection(selections, LayoutChange);
        }
        private void LayoutChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            ButtonLayout.SetModeText(selected);

            var config = 0f;
            if (selected == InterString.Get("桌面布局"))
                config = 1f;
            else if (selected == InterString.Get("移动布局"))
                config = 2f;
            Config.SetFloat("Layout", config);

            UIManager.ChangeLayout();
        }
        private void SetLayoutMode(float config)
        {
            var value = InterString.Get("自动判断");
            if (config == 1f)
                value = InterString.Get("桌面布局");
            else if (config == 2f)
                value = InterString.Get("移动布局");
            ButtonLayout.SetModeText(value);
        }

        #endregion

        #region Background

        private void InitializeBackground()
        {
            ButtonBackground.SetClickEvent(OnBackgroundClicked);
            ChangeBackgroundModeText();
        }
        private void ChangeBackgroundModeText()
        {
            var id = int.Parse(Config.Get("Background", "0"));
            var value = InterString.Get("随机");
            if (id != 0)
                if (!BackgroundManager.backgrounds.TryGetValue(id, out value))
                    value = "Classic";
            if (string.IsNullOrEmpty(value))
                value = InterString.Get("随机");
            ButtonBackground.SetModeText(value);
        }
        private void OnBackgroundClicked()
        {
            List<string> selections = new()
            {
                InterString.Get("更换背景"),
                string.Empty,
                InterString.Get("随机"),
            };
            foreach (var background in BackgroundManager.backgrounds)
                selections.Add(background.Value);

            UIManager.ShowPopupSelection(selections, BackgroundChange);
        }
        private void BackgroundChange()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            var id = Program.instance.background_.GetIDByName(selected);
            Config.Set("Background", id.ToString());
            Program.instance.background_.Change(id);
            ChangeBackgroundModeText();
        }

        #endregion

        #region BgmBy

        private void InitializeBgmBy()
        {
            ButtonBgmBy.SetClickEvent(OnBgmByClicked);
            var config = Config.GetBool("BGMbyMySide", true);
            ButtonBgmBy.SetModeText(InterString.Get(config ? "我方" : "对方"));
        }

        private void OnBgmByClicked()
        {
            var config = Config.GetBool("BGMbyMySide", true);
            ButtonBgmBy.SetModeText(InterString.Get(config ? "对方" : "我方"));
            Config.SetBool("BGMbyMySide", !config);
        }

        #endregion

        #region CardStyle

        private void InitializeCardStyle()
        {
            ButtonCardStyle.SetClickEvent(OnCardStyleChange);
            ButtonCardStyle.SetModeText(Config.Get("CardStyle", CardStyle.OCG_TCG.ToString()));
        }

        private void OnCardStyleChange()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                InterString.Get("卡图风格"),
                string.Empty
            };
            var values = Enum.GetValues(typeof(CardStyle));
            foreach (var value in values)
                selections.Add(value.ToString());
            UIManager.ShowPopupSelection(selections, ChangeCardStyle);
        }

        private void ChangeCardStyle()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            var currrent = Config.Get("CardStyle", selected);
            Config.Set("CardStyle", selected);
            if(currrent != selected)
            {
                ButtonCardStyle.SetModeText(selected);
                UIManager.ChangeLanguage();
                MaterialLoader.ClearCachedOverFrameMasks();
            }
        }

        #endregion

        #region Video Card

        private bool videoCardConfigChanged;

        private void InitializeVideoCard()
        {
            ButtonVideoCard.SetClickEvent(OnVideoCardClicked);

            var config = Config.GetBool("VideoCard", true);
            ButtonVideoCard.SetModeText(InterString.Get(config ? "开" : "关"));
        }

        private void OnVideoCardClicked()
        {
            var config = Config.GetBool("VideoCard", true);
            Config.SetBool("VideoCard", !config);
            ButtonVideoCard.SetModeText(InterString.Get(config ? "关" : "开"));
            videoCardConfigChanged = true;
        }

        #endregion

        #region CardLanguage

        private void InitializeCardLanguage()
        {
            ButtonCardLanguage.SetClickEvent(OnCardLanguageClicked);
            ButtonCardLanguage.SetModeText(InterString.Get(Language.GetCardConfig()));
        }
        private void OnCardLanguageClicked()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                InterString.Get("卡图语言"),
                string.Empty
            };
            DirectoryInfo[] infos = new DirectoryInfo(Program.PATH_LOCALES).GetDirectories();
            foreach (DirectoryInfo info in infos)
                selections.Add(InterString.Get(info.Name));
            UIManager.ShowPopupSelection(selections, ChangeCardLanguage);
        }
        private void ChangeCardLanguage()
        {
            string selected = EventSystem.current.currentSelectedGameObject
                .GetComponent<SelectionButton>().GetButtonText();
            ButtonCardLanguage.SetModeText(selected);
            Language.SetCardConfig(InterString.GetOriginal(selected));
            UIManager.ChangeLanguage();
        }

        #endregion

        #region Language

        private void InitializeLanguage()
        {
            ButtonLanguage.SetClickEvent(OnLanguageClicked);
            ButtonLanguage.SetModeText(InterString.Get(Language.GetConfig()));
        }
        private void OnLanguageClicked()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            List<string> selections = new()
            {
                InterString.Get("语言"),
                string.Empty
            };
            DirectoryInfo[] infos = new DirectoryInfo(Program.PATH_LOCALES).GetDirectories();
            foreach (DirectoryInfo info in infos)
                selections.Add(InterString.Get(info.Name));
            UIManager.ShowPopupSelection(selections, OnLanguageSelection);
        }
        private void OnLanguageSelection()
        {
            string selected = EventSystem.current.
                currentSelectedGameObject.GetComponent<SelectionButton>().GetButtonText();
            ButtonLanguage.SetModeText(selected);
            Language.SetConfig(InterString.GetOriginal(selected));
            UIManager.ChangeLanguage();
        }

        #endregion

        #endregion

        #region Duel

        #region Appearance

        private void InitializeAppearance()
        {
            ButtonDuelAppearance.SetClickEvent(OnDuelAppearcanceClick);
            ButtonWatchAppearance.SetClickEvent(OnWatchAppearcanceClick);
            ButtonReplayAppearance.SetClickEvent(OnReplayAppearcanceClick);
            RefreshAppearanceModeText();
        }
        private void OnDuelAppearcanceClick()
        {
            Program.instance.appearance.SwitchCondition(Appearance.Condition.Duel);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.appearance);
            else
                Program.instance.ShiftToServant(Program.instance.appearance);
        }
        private void OnWatchAppearcanceClick()
        {
            Program.instance.appearance.SwitchCondition(Appearance.Condition.Watch);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.appearance);
            else
                Program.instance.ShiftToServant(Program.instance.appearance);
        }
        private void OnReplayAppearcanceClick()
        {
            Program.instance.appearance.SwitchCondition(Appearance.Condition.Replay);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.appearance);
            else
                Program.instance.ShiftToServant(Program.instance.appearance);
        }
        public void RefreshAppearanceModeText()
        {
            ButtonDuelAppearance.SetModeText(Config.Get("DuelPlayerName0", Config.EMPTY_STRING));
            ButtonWatchAppearance.SetModeText(Config.Get("WatchPlayerName0", Config.EMPTY_STRING));
            ButtonReplayAppearance.SetModeText(Config.Get("ReplayPlayerName0", Config.EMPTY_STRING));
        }

        #endregion

        #region Character

        private void InitializeCharacter()
        {
            ButtonDuelCharacter.SetClickEvent(OnDuelCharacterClick);
            ButtonWatchCharacter.SetClickEvent(OnWatchCharacterClick);
            ButtonReplayCharacter.SetClickEvent(OnReplayCharacterClick);
            RefreshCharacterName();
        }
        private void OnDuelCharacterClick()
        {
            Program.instance.character.SwitchCondition(CharacterSelector.Condition.Duel);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.character);
            else
                Program.instance.ShiftToServant(Program.instance.character);
        }
        private void OnWatchCharacterClick()
        {
            Program.instance.character.SwitchCondition(CharacterSelector.Condition.Watch);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.character);
            else
                Program.instance.ShiftToServant(Program.instance.character);
        }
        private void OnReplayCharacterClick()
        {
            Program.instance.character.SwitchCondition(CharacterSelector.Condition.Replay);
            if (Program.instance.currentSubServant == Program.instance.setting)
                Program.instance.ShowSubServant(Program.instance.character);
            else
                Program.instance.ShiftToServant(Program.instance.character);
        }
        public void RefreshCharacterName()
        {
            if (CharacterSelector.characters == null)
                return;

            var characterName = CharacterSelector.characters.GetName(Config.Get("DuelCharacter0", VoicePlayer.defaultCharacter));
            ButtonDuelCharacter.SetModeText(characterName);

            characterName = CharacterSelector.characters.GetName(Config.Get("WatchCharacter0", VoicePlayer.defaultCharacter));
            ButtonWatchCharacter.SetModeText(characterName);

            characterName = CharacterSelector.characters.GetName(Config.Get("ReplayCharacter0", VoicePlayer.defaultCharacter));
            ButtonReplayCharacter.SetModeText(characterName);
        }

        #endregion

        #region Voice

        private void InitializeVoice()
        {
            var config = Config.GetBool("DuelVoice", false);
            ButtonDuelVoice.SetClickEvent(OnDuelVoiceClick);
            ButtonDuelVoice.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchVoice", false);
            ButtonWatchVoice.SetClickEvent(OnWatchVoiceClick);
            ButtonWatchVoice.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayVoice", false);
            ButtonReplayVoice.SetClickEvent(OnReplayVoiceClick);
            ButtonReplayVoice.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelVoiceClick()
        {
            var config = Config.GetBool("DuelVoice", false);
            ButtonDuelVoice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelVoice", !config);

            Program.instance.ocgcore.CheckCharaFace();
        }
        private void OnWatchVoiceClick()
        {
            var config = Config.GetBool("WatchVoice", false);
            ButtonWatchVoice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchVoice", !config);

            Program.instance.ocgcore.CheckCharaFace();
        }
        private void OnReplayVoiceClick()
        {
            var config = Config.GetBool("ReplayVoice", false);
            ButtonReplayVoice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayVoice", !config);

            Program.instance.ocgcore.CheckCharaFace();
        }

        #endregion

        #region Closeup

        private void InitializeCloseup()
        {
            var config = Config.GetBool("DuelCloseup", false);
            ButtonDuelCloseup.SetClickEvent(OnDuelCloseupClick);
            ButtonDuelCloseup.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchCloseup", false);
            ButtonWatchCloseup.SetClickEvent(OnWatchCloseupClick);
            ButtonWatchCloseup.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayCloseup", false);
            ButtonReplayCloseup.SetClickEvent(OnReplayCloseupClick);
            ButtonReplayCloseup.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCloseupClick()
        {
            var config = Config.GetBool("DuelCloseup", false);
            ButtonDuelCloseup.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelCloseup", !config);
            Program.instance.ocgcore.RefreshAllCardsLabel();
        }
        private void OnWatchCloseupClick()
        {
            var config = Config.GetBool("WatchCloseup", false);
            ButtonWatchCloseup.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchCloseup", !config);
            Program.instance.ocgcore.RefreshAllCardsLabel();
        }
        private void OnReplayCloseupClick()
        {
            var config = Config.GetBool("ReplayCloseup", false);
            ButtonReplayCloseup.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayCloseup", !config);
            Program.instance.ocgcore.RefreshAllCardsLabel();
        }

        #endregion

        #region Summon

        private void InitializeSummon()
        {
            var button = Manager.GetElement<SelectionButton_Setting>("DuelSummon");
            button.SetClickEvent(OnDuelSummonClick);
            var config = Config.GetBool("DuelSummon", true);
            button.SetModeText(InterString.Get(config ? "开" : "关"));

            button = Manager.GetElement<SelectionButton_Setting>("WatchSummon");
            button.SetClickEvent(OnWatchSummonClick);
            config = Config.GetBool("WatchSummon", true);
            button.SetModeText(InterString.Get(config ? "开" : "关"));

            button = Manager.GetElement<SelectionButton_Setting>("ReplaySummon");
            button.SetClickEvent(OnReplaySummonClick);
            config = Config.GetBool("ReplaySummon", true);
            button.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelSummonClick()
        {
            var button = Manager.GetElement<SelectionButton_Setting>("DuelSummon");
            var config = Config.GetBool("DuelSummon", true);
            button.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelSummon", !config);
        }
        private void OnWatchSummonClick()
        {
            var button = Manager.GetElement<SelectionButton_Setting>("WatchSummon");
            var config = Config.GetBool("WatchSummon", true);
            button.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchSummon", !config);
        }
        private void OnReplaySummonClick()
        {
            var button = Manager.GetElement<SelectionButton_Setting>("ReplaySummon");
            var config = Config.GetBool("ReplaySummon", true);
            button.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplaySummon", !config);
        }
        #endregion

        #region Pendulum

        private void InitializePendulum()
        {
            var config = Config.GetBool("DuelPendulum", true);
            ButtonDuelPendulum.SetClickEvent(OnDuelPendulumClick);
            ButtonDuelPendulum.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchPendulum", true);
            ButtonWatchPendulum.SetClickEvent(OnWatchPendulumClick);
            ButtonWatchPendulum.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayPendulum", true);
            ButtonReplayPendulum.SetClickEvent(OnReplayPendulumClick);
            ButtonReplayPendulum.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelPendulumClick()
        {
            var config = Config.GetBool("DuelPendulum", true);
            ButtonDuelPendulum.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelPendulum", !config);
        }
        private void OnWatchPendulumClick()
        {
            var config = Config.GetBool("WatchPendulum", true);
            ButtonWatchPendulum.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchPendulum", !config);
        }
        private void OnReplayPendulumClick()
        {
            var config = Config.GetBool("ReplayPendulum", true);
            ButtonReplayPendulum.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayPendulum", !config);
        }

        #endregion

        #region Cutin

        private void InitializeCutin()
        {
            var config = Config.GetBool("DuelCutin", true);
            ButtonDuelCutin.SetClickEvent(OnDuelCutinClick);
            ButtonDuelCutin.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchCutin", true);
            ButtonWatchCutin.SetClickEvent(OnWatchCutinClick);
            ButtonWatchCutin.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayCutin", true);
            ButtonReplayCutin.SetClickEvent(OnReplayCutinClick);
            ButtonReplayCutin.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCutinClick()
        {
            var config = Config.GetBool("DuelCutin", true);
            ButtonDuelCutin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelCutin", !config);
        }
        private void OnWatchCutinClick()
        {
            var config = Config.GetBool("WatchCutin", true);
            ButtonWatchCutin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchCutin", !config);
        }
        private void OnReplayCutinClick()
        {
            var config = Config.GetBool("ReplayCutin", true);
            ButtonReplayCutin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayCutin", !config);
        }

        #endregion

        #region Effect

        private void InitializeEffect()
        {
            var config = Config.GetBool("DuelEffect", true);
            ButtonDuelEffect.SetClickEvent(OnDuelEffectClick);
            ButtonDuelEffect.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchEffect", true);
            ButtonWatchEffect.SetClickEvent(OnWatchEffectClick);
            ButtonWatchEffect.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayEffect", true);
            ButtonReplayEffect.SetClickEvent(OnReplayEffectClick);
            ButtonReplayEffect.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelEffectClick()
        {
            var config = Config.GetBool("DuelEffect", true);
            ButtonDuelEffect.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelEffect", !config);
        }
        private void OnWatchEffectClick()
        {
            var config = Config.GetBool("WatchEffect", true);
            ButtonWatchEffect.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchEffect", !config);
        }
        private void OnReplayEffectClick()
        {
            var config = Config.GetBool("ReplayEffect", true);
            ButtonReplayEffect.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayEffect", !config);
        }

        #endregion

        #region Chain

        private void InitializeChain()
        {
            var config = Config.GetBool("DuelChain", true);
            ButtonDuelChain.SetClickEvent(OnDuelChainClick);
            ButtonDuelChain.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchChain", true);
            ButtonWatchChain.SetClickEvent(OnWatchChainClick);
            ButtonWatchChain.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayChain", true);
            ButtonReplayChain.SetClickEvent(OnReplayChainClick);
            ButtonReplayChain.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelChainClick()
        {
            var config = Config.GetBool("DuelChain", true);
            ButtonDuelChain.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelChain", !config);
        }
        private void OnWatchChainClick()
        {
            var config = Config.GetBool("WatchChain", true);
            ButtonWatchChain.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchChain", !config);
        }
        private void OnReplayChainClick()
        {
            var config = Config.GetBool("ReplayChain", true);
            ButtonReplayChain.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayChain", !config);
        }

        #endregion

        #region Dice

        private void InitializeDice()
        {
            var config = Config.GetBool("DuelDice", true);
            ButtonDuelDice.SetClickEvent(OnDuelDiceClick);
            ButtonDuelDice.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchDice", true);
            ButtonWatchDice.SetClickEvent(OnWatchDiceClick);
            ButtonWatchDice.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayDice", true);
            ButtonReplayDice.SetClickEvent(OnReplayDiceClick);
            ButtonReplayDice.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelDiceClick()
        {
            var config = Config.GetBool("DuelDice", true);
            ButtonDuelDice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelDice", !config);
        }
        private void OnWatchDiceClick()
        {
            var config = Config.GetBool("WatchDice", true);
            ButtonWatchDice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchDice", !config);
        }
        private void OnReplayDiceClick()
        {
            var config = Config.GetBool("ReplayDice", true);
            ButtonReplayDice.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayDice", !config);
        }

        #endregion

        #region Coin

        private void InitializeCoin()
        {
            var config = Config.GetBool("DuelCoin", true);
            ButtonDuelCoin.SetClickEvent(OnDuelCoinClick);
            ButtonDuelCoin.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchCoin", true);
            ButtonWatchCoin.SetClickEvent(OnWatchCoinClick);
            ButtonWatchCoin.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayCoin", true);
            ButtonReplayCoin.SetClickEvent(OnReplayCoinClick);
            ButtonReplayCoin.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelCoinClick()
        {
            var config = Config.GetBool("DuelCoin", true);
            ButtonDuelCoin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelCoin", !config);
        }
        private void OnWatchCoinClick()
        {
            var config = Config.GetBool("WatchCoin", true);
            ButtonWatchCoin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchCoin", !config);
        }
        private void OnReplayCoinClick()
        {
            var config = Config.GetBool("ReplayCoin", true);
            ButtonReplayCoin.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayCoin", !config);
        }

        #endregion

        #region AutoInfo

        private void InitializeAutoInfo()
        {
            var config = Config.GetBool("DuelAutoInfo", true);
            ButtonDuelAutoInfo.SetClickEvent(OnDuelAutoInfoClick);
            ButtonDuelAutoInfo.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchAutoInfo", true);
            ButtonWatchAutoInfo.SetClickEvent(OnWatchAutoInfoClick);
            ButtonWatchAutoInfo.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayAutoInfo", true);
            ButtonReplayAutoInfo.SetClickEvent(OnReplayAutoInfoClick);
            ButtonReplayAutoInfo.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelAutoInfoClick()
        {
            var config = Config.GetBool("DuelAutoInfo", true);
            ButtonDuelAutoInfo.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelAutoInfo", !config);
        }
        private void OnWatchAutoInfoClick()
        {
            var config = Config.GetBool("WatchAutoInfo", true);
            ButtonWatchAutoInfo.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchAutoInfo", !config);
        }
        private void OnReplayAutoInfoClick()
        {
            var config = Config.GetBool("ReplayAutoInfo", true);
            ButtonReplayAutoInfo.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayAutoInfo", !config);
        }

        #endregion

        #region FaceDown

        private void InitializeFaceDown()
        {
            var config = Config.GetBool("DuelFaceDown", true);
            ButtonDuelFaceDown.SetClickEvent(OnDuelFaceDownClick);
            ButtonDuelFaceDown.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchFaceDown", true);
            ButtonWatchFaceDown.SetClickEvent(OnWatchFaceDownClick);
            ButtonWatchFaceDown.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayFaceDown", true);
            ButtonReplayFaceDown.SetClickEvent(OnReplayFaceDownClick);
            ButtonReplayFaceDown.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelFaceDownClick()
        {
            var config = Config.GetBool("DuelFaceDown", true);
            ButtonDuelFaceDown.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelFaceDown", !config);

            foreach (var card in OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }
        private void OnWatchFaceDownClick()
        {
            var config = Config.GetBool("WatchFaceDown", true);
            ButtonWatchFaceDown.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchFaceDown", !config);

            foreach (var card in OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }
        private void OnReplayFaceDownClick()
        {
            var config = Config.GetBool("ReplayFaceDown", true);
            ButtonReplayFaceDown.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayFaceDown", !config);

            foreach (var card in OcgCore.cards)
                card.ShowFaceDownCardOrNot(card.NeedShowFaceDownCard());
        }

        #endregion

        #region PlayerMessage

        private void InitializePlayerMessage()
        {
            var config = Config.GetBool("DuelPlayerMessage", true);
            ButtonDuelPlayerMessage.SetClickEvent(OnDuelPlayerMessageClick);
            ButtonDuelPlayerMessage.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchPlayerMessage", true);
            ButtonWatchPlayerMessage.SetClickEvent(OnWatchPlayerMessageClick);
            ButtonWatchPlayerMessage.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayPlayerMessage", true);
            ButtonReplayPlayerMessage.SetClickEvent(OnReplayPlayerMessageClick);
            ButtonReplayPlayerMessage.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelPlayerMessageClick()
        {
            var config = Config.GetBool("DuelPlayerMessage", true);
            ButtonDuelPlayerMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelPlayerMessage", !config);
        }
        private void OnWatchPlayerMessageClick()
        {
            var config = Config.GetBool("WatchPlayerMessage", true);
            ButtonWatchPlayerMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchPlayerMessage", !config);
        }
        private void OnReplayPlayerMessageClick()
        {
            var config = Config.GetBool("ReplayPlayerMessage", true);
            ButtonReplayPlayerMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayPlayerMessage", !config);
        }

        #endregion

        #region SystemMessage

        private void InitializeSystemMessage()
        {
            var config = Config.GetBool("DuelSystemMessage", true);
            ButtonDuelSystemMessage.SetClickEvent(OnDuelSystemMessageClick);
            ButtonDuelSystemMessage.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchSystemMessage", true);
            ButtonWatchSystemMessage.SetClickEvent(OnWatchSystemMessageClick);
            ButtonWatchSystemMessage.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplaySystemMessage", true);
            ButtonReplaySystemMessage.SetClickEvent(OnReplaySystemMessageClick);
            ButtonReplaySystemMessage.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelSystemMessageClick()
        {
            var config = Config.GetBool("DuelSystemMessage", true);
            ButtonDuelSystemMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelSystemMessage", !config);
        }
        private void OnWatchSystemMessageClick()
        {
            var config = Config.GetBool("WatchSystemMessage", true);
            ButtonWatchSystemMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchSystemMessage", !config);
        }
        private void OnReplaySystemMessageClick()
        {
            var config = Config.GetBool("ReplaySystemMessage", true);
            ButtonReplaySystemMessage.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplaySystemMessage", !config);
        }

        #endregion

        #region Acc

        private void InitializeAcc()
        {
            var config = Config.GetFloat("DuelAcc", 2f);
            ButtonDuelAcc.SetSliderEvent(OnDuelAccChange);
            ButtonDuelAcc.SetSliderValue(config);

            config = Config.GetFloat("WatchAcc", 2f);
            ButtonWatchAcc.SetSliderEvent(OnWatchAccChange);
            ButtonWatchAcc.SetSliderValue(config);

            config = Config.GetFloat("ReplayAcc", 2f);
            ButtonReplayAcc.SetSliderEvent(OnReplayAccChange);
            ButtonReplayAcc.SetSliderValue(config);
        }
        private void OnDuelAccChange(float value)
        {
            string result = value.ToString();
            ButtonDuelAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Program.instance.ocgcore.showing)
                if (OcgCore.condition == OcgCore.Condition.Duel)
                    if (OcgCore.Accing)
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }
        private void OnWatchAccChange(float value)
        {
            string result = value.ToString();
            ButtonWatchAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Program.instance.ocgcore.showing)
                if (OcgCore.condition == OcgCore.Condition.Watch)
                    if (OcgCore.Accing)
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }
        private void OnReplayAccChange(float value)
        {
            string result = value.ToString();
            ButtonReplayAcc.SetModeText(result.Length > 4 ? result[..4] : result);
            if (Program.instance.ocgcore.showing)
                if (OcgCore.condition == OcgCore.Condition.Replay)
                    if (OcgCore.Accing)
                        Program.instance.ocgcore.GetUI<OcgCoreUI>().OnAcc();
        }

        #endregion

        #region AutoAcc

        private void InitializeAutoAcc()
        {
            var config = Config.GetBool("DuelAutoAcc", true);
            ButtonDuelAutoAcc.SetClickEvent(OnDuelAutoAccClick);
            ButtonDuelAutoAcc.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("WatchAutoAcc", true);
            ButtonWatchAutoAcc.SetClickEvent(OnWatchAutoAccClick);
            ButtonWatchAutoAcc.SetModeText(InterString.Get(config ? "开" : "关"));

            config = Config.GetBool("ReplayAutoAcc", true);
            ButtonReplayAutoAcc.SetClickEvent(OnReplayAutoAccClick);
            ButtonReplayAutoAcc.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnDuelAutoAccClick()
        {
            var config = Config.GetBool("DuelAutoAcc", true);
            ButtonDuelAutoAcc.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("DuelAutoAcc", !config);
        }
        private void OnWatchAutoAccClick()
        {
            var config = Config.GetBool("WatchAutoAcc", true);
            ButtonWatchAutoAcc.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("WatchAutoAcc", !config);
        }
        private void OnReplayAutoAccClick()
        {
            var config = Config.GetBool("ReplayAutoAcc", true);
            ButtonReplayAutoAcc.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("ReplayAutoAcc", !config);
        }

        #endregion

        #region Timming

        private void InitializeTimming()
        {
            var config = Config.GetBool(Config.LABEL_TIMING, false);
            ButtonTiming.SetClickEvent(OnTimmingClick);
            ButtonTiming.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnTimmingClick()
        {
            var config = Config.GetBool(Config.LABEL_TIMING, false);
            ButtonTiming.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool(Config.LABEL_TIMING, !config);
        }

        #endregion

        #region AutoRPS

        private void InitializeAutoRPS()
        {
            var config = Config.GetBool("AutoRPS", false);
            ButtonAutoRPS.SetClickEvent(OnAutoRPSClick);
            ButtonAutoRPS.SetModeText(InterString.Get(config ? "开" : "关"));
        }
        private void OnAutoRPSClick()
        {
            var config = Config.GetBool("AutoRPS", false);
            ButtonAutoRPS.SetModeText(InterString.Get(config ? "关" : "开"));
            Config.SetBool("AutoRPS", !config);
        }

        #endregion

        #endregion

        #region Port

        public void OnImport()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            PortHelper.ImportFiles();
        }
        public void OnImportBG()
        {
            PortHelper.ImportBG();
        }
        public void OnExportDeck()
        {
            PortHelper.ExportAllDecks();
        }
        public void OnExportReplay()
        {
            PortHelper.ExportAllReplays();
        }
        public void OnExportPicture()
        {
            PortHelper.ExportAllPictures();
        }
        public void OnClearPicture()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                InterString.Get("确定清空"),
                InterString.Get("是否确认删除所有导入的卡图？"),
                InterString.Get("确认"),
                InterString.Get("取消")
            };
            UIManager.ShowPopupYesOrNo(selections, () =>
            {
                if (!Directory.Exists(Program.PATH_ALT_ART))
                    Directory.CreateDirectory(Program.PATH_ALT_ART);
                foreach (var file in Directory.GetFiles(Program.PATH_ALT_ART))
                    File.Delete(file);
            }, null);
        }
        public void OnClearArtVideos()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                InterString.Get("确定清空"),
                InterString.Get("是否确认删除所有的动态卡图？@n您可以在系统设置中选择关闭动态卡图的显示。"),
                InterString.Get("确认"),
                InterString.Get("取消")
            };
            UIManager.ShowPopupYesOrNo(selections, () =>
            {
                if (!Directory.Exists(Program.PATH_VIDEO_ART))
                    Directory.CreateDirectory(Program.PATH_ALT_ART);
                foreach (var file in Directory.GetFiles(Program.PATH_VIDEO_ART))
                    File.Delete(file);
                CardImageLoader.ReloadArtVideos();
            }, null);
        }
        #endregion

        #region Expansion

        private void InitializeExpansions()
        {
            var config = Config.GetBool("Expansions", true);
            ButtonExpansionSupport.SetModeText(InterString.Get(config ? "是" : "否"));
        }

        public void OnSupportExpansions()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能更改此选项。"));
                return;
            }

            var config = Config.GetBool("Expansions", true);
            ButtonExpansionSupport.SetModeText(InterString.Get(config ? "否" : "是"));
            Config.SetBool("Expansions", !config);

            Program.instance.InitializeForDataChange();
        }

        public void OnClearExpansions()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            var selections = new List<string>
            {
                InterString.Get("确定清空"),
                InterString.Get("是否确认删除所有导入的扩展卡包？"),
                InterString.Get("确认"),
                InterString.Get("取消")
            };
            UIManager.ShowPopupYesOrNo(selections, () =>
            {
                ZipHelper.Dispose();
                if (!Directory.Exists(Program.PATH_EXPANSIONS))
                    Directory.CreateDirectory(Program.PATH_EXPANSIONS);
                foreach (var file in Directory.GetFiles(Program.PATH_EXPANSIONS))
                    File.Delete(file);
                Program.instance.InitializeForDataChange();
            }, null);
        }

        public void OnUpdatePrerelease()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }
            Program.instance.setting.UpdatePrerelease();
        }

        public void OnDownloadYPK()
        {
            if (Program.instance.ocgcore.showing)
            {
                MessageManager.Toast(InterString.Get("决斗中不能进行此操作。"));
                return;
            }

            Program.instance.setting.DownloadYPK();
        }

        #endregion

        #region About

        private static string GetAboutText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            var result = text.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd();
            if (result.EndsWith("_END_", StringComparison.Ordinal))
                result = result[..^"_END_".Length].TrimEnd();
            return result;
        }

        public void OnAboutGame()
        {
            var address = Language.UseChinese() ? "AboutGame" : "AboutGame_en";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    InterString.Get("关于游戏"),
                    GetAboutText(result.Result.text)
                };
                if (Language.UseChinese())
                    UIManager.ShowPopupText(selections);
                else
                    UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }
        public void OnAboutVersion()
        {
            var address = Language.UseChinese() ? "AboutVersion" : "AboutVersion_en";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            handle.Completed += (result) =>
            {
                var selections = new List<string>()
                {
                    InterString.Get("关于版本号"),
                    GetAboutText(result.Result.text)
                };
                UIManager.ShowPopupText(selections, TMPro.HorizontalAlignmentOptions.Left);
            };
        }
        public void OnAboutUpdate()
        {
            MDPro3.Net.UltraServices.CheckUpdates(true);
        }
        public void OnUpdateContent()
        {
            Application.OpenURL(MDPro3.Net.UltraServices.Repository + "/releases/latest");
        }

        #endregion

        public void OnSurrender()
        {
            Program.instance.ocgcore.OnDuelResultConfirmed(true);
        }

        public void OnRetry()
        {
            if (OcgCore.condition != OcgCore.Condition.Duel || !RoomServant.FromSolo)
                return;
            if (!Program.instance.solo.CanRetryLastSoloDuel())
                return;

            Program.instance.ocgcore.onSurrenderConfirmed = Program.instance.solo.QueueRetryLastSoloDuel;
            Program.instance.ocgcore.OnDuelResultConfirmed(true);
        }

    }

    public partial class SROptions
    {
        private UniversalRenderPipelineAsset urpa;
        private Type universalRenderPipelineAssetType;
        private FieldInfo mainLightShadowmapResolutionFieldInfo;
        private FieldInfo supportsSoftShadowsFieldInfo;

        private void InitializeShadowMapFieldInfo()
        {
            urpa = Resources.Load<UniversalRenderPipelineAsset>("Settings/URPAsset");
            universalRenderPipelineAssetType = urpa.GetType();
            mainLightShadowmapResolutionFieldInfo = universalRenderPipelineAssetType.GetField("m_MainLightShadowmapResolution", BindingFlags.Instance | BindingFlags.NonPublic);
            supportsSoftShadowsFieldInfo = universalRenderPipelineAssetType.GetField("m_SoftShadowsSupported", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public ShadowResolution MainLightShadowResolution
        {
            get
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                return (ShadowResolution)mainLightShadowmapResolutionFieldInfo.GetValue(urpa);
            }
            set
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                mainLightShadowmapResolutionFieldInfo.SetValue(urpa, value);
            }
        }

        public bool SupportsSoftShadows
        {
            get
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                return (bool)supportsSoftShadowsFieldInfo.GetValue(urpa);
            }
            set
            {
                if (mainLightShadowmapResolutionFieldInfo == null)
                {
                    InitializeShadowMapFieldInfo();
                }
                supportsSoftShadowsFieldInfo.SetValue(urpa, value);
            }
        }
    }
}
