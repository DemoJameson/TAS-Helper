using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Module.Menu;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TAS.EverestInterop;
using YamlDotNet.Serialization;

namespace Celeste.Mod.TASHelper.Module;

[SettingName("TAS_HELPER_NAME")]
public class TASHelperSettings : EverestModuleSettings {

    public static TASHelperSettings Instance { get; private set; }

    public TASHelperSettings() {
        Instance = this;
    }

    internal void OnLoadSettings() {
        UpdateAuxiliaryVariable();

        if (keyMainSwitch is null) {
            keyMainSwitch = new((Buttons)0, Keys.LeftControl, Keys.E);
        }
        if (keyCountDown is null) {
            keyCountDown = new((Buttons)0, Keys.LeftControl, Keys.R);
        }
        if (keyLoadRange is null) {
            keyLoadRange = new((Buttons)0, Keys.LeftControl, Keys.T);
        }
        if (keyPixelGridWidth is null) {
            keyPixelGridWidth = new((Buttons)0, Keys.LeftControl, Keys.F);
        }
        // it seems some bug can happen with deserialization
    }

    public bool Enabled = true;

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    public MainSwitchModes mainSwitch { get; set; } = MainSwitchModes.OnlyDefault;

    [YamlIgnore]
    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            if (mainSwitch == value) {
                // prevent infinite recursion
                return;
            }
            mainSwitch = value;
            MainSwitchWatcher.instance?.Refresh();
            switch (value) {
                case MainSwitchModes.Off:
                    Enabled = false;
                    Sleep();
                    break;
                default:
                    Enabled = true;
                    Awake(value == MainSwitchModes.AllowAll);
                    break;
            }
            // the setters are not called, so the auxiliary variables don't get update, we have to update them
            // we can't call the setters, which will otherwise break the Awake...s
            UpdateAuxiliaryVariable();
            return;
        }
    }
    internal void Sleep() {
        MainSwitch = MainSwitchModes.Off;
        Awake_CycleHitboxColors = false;
        Awake_UsingNotInViewColor = false;
        Awake_EnableSimplifiedSpinner = false;
        Awake_CountdownModes = false;
        Awake_LoadRange = false;
        Awake_CameraTarget = false;
        Awake_PixelGrid = false;
        Awake_SpawnPoint = false;
        Awake_EntityActivatorReminder = false;
        Awake_FireBallTrack = false;
    }
    internal void Awake(bool awakeAll) {
        MainSwitch = awakeAll ? MainSwitchModes.AllowAll : MainSwitchModes.OnlyDefault;
        Awake_CycleHitboxColors = true;
        Awake_UsingNotInViewColor = true;
        Awake_EnableSimplifiedSpinner = true;
        Awake_CountdownModes = awakeAll;
        Awake_LoadRange = awakeAll;
        Awake_CameraTarget = awakeAll;
        Awake_PixelGrid = awakeAll;
        Awake_SpawnPoint = true;
        Awake_EntityActivatorReminder = true;
        Awake_FireBallTrack = true;
    }

    #endregion

    #region CycleHitboxColor
    public bool Awake_CycleHitboxColors = true;

    // we need to make it public, so this setting is stored
    // though we don't want anyone to visit it directly...

    public bool showCycleHitboxColor { get; set; } = true;

    [YamlIgnore]
    public bool ShowCycleHitboxColors {
        get => Enabled && Awake_CycleHitboxColors && showCycleHitboxColor;
        set {
            showCycleHitboxColor = value;
            Awake_CycleHitboxColors = true;
        }
    }

    public bool Awake_UsingNotInViewColor = true;
    public enum UsingNotInViewColorModes { Off, WhenUsingInViewRange, Always };

    public UsingNotInViewColorModes usingNotInViewColorMode { get; set; } = UsingNotInViewColorModes.WhenUsingInViewRange;

    [YamlIgnore]
    public UsingNotInViewColorModes UsingNotInViewColorMode {
        get => Enabled && Awake_UsingNotInViewColor ? usingNotInViewColorMode : UsingNotInViewColorModes.Off;
        set {
            usingNotInViewColorMode = value;
            Awake_UsingNotInViewColor = true;
            UsingNotInViewColor = (value == UsingNotInViewColorModes.Always) || (value == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        }
    }

    public bool UsingNotInViewColor = true;

    #endregion

    #region Countdown
    public bool Awake_CountdownModes = true;
    public enum CountdownModes { Off, _3fCycle, _15fCycle };

    public CountdownModes countdownMode { get; set; } = CountdownModes.Off;

    [YamlIgnore]
    public CountdownModes CountdownMode {
        get => Enabled && Awake_CountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            Awake_CountdownModes = true;

            UsingCountDown = (CountdownMode != CountdownModes.Off);
            if (CountdownMode == CountdownModes._3fCycle) {
                SpinnerCountdownLoad = true;
                SpinnerInterval = 0.05f;
            }
            else {
                SpinnerCountdownLoad = false;
                SpinnerInterval = 0.25f;
            }
        }
    }

    public enum CountdownFonts { PixelFont, HiresFont };

    public CountdownFonts CountdownFont = CountdownFonts.HiresFont;

    public int HiresFontSize = 8;

    public bool usingHiresFont => CountdownFont == CountdownFonts.HiresFont;

    public int HiresFontStroke = 5;

    public bool DoNotRenderWhenFarFromView = true;

    #endregion

    #region LoadRange

    public bool Awake_LoadRange = true;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    public LoadRangeModes loadRangeMode { get; set; } = LoadRangeModes.Neither;

    [YamlIgnore]
    public LoadRangeModes LoadRangeMode {
        get => Enabled && Awake_LoadRange ? loadRangeMode : LoadRangeModes.Neither;
        set {
            loadRangeMode = value;
            Awake_LoadRange = true;

            UsingLoadRange = LoadRangeMode != LoadRangeModes.Neither;
            UsingInViewRange = LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNearPlayerRange = LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both;
            UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        }
    }

    public int InViewRangeWidth { get; set; } = 16;

    public int NearPlayerRangeWidth { get; set; } = 8;

    private int loadRangeOpacity { get; set; } = 4;

    [YamlIgnore]
    public int LoadRangeOpacity {
        get => loadRangeOpacity;
        set {
            loadRangeOpacity = value;
            RangeAlpha = value * 0.1f;
        }
    }

    public bool ApplyCameraZoom { get; set; } = false;

    #endregion

    #region Simplified Spinner

    public bool Awake_EnableSimplifiedSpinner = true;

    public bool enableSimplifiedSpinner { get; set; } = true;

    [YamlIgnore]
    public bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
        }
    }

    public enum ClearSpritesMode { Off, WhenSimplifyGraphics, Always };

    public ClearSpritesMode enforceClearSprites { get; set; } = ClearSpritesMode.WhenSimplifyGraphics;

    [YamlIgnore]
    public ClearSpritesMode EnforceClearSprites {
        get => enforceClearSprites;
        set => enforceClearSprites = value;
    }

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && (EnforceClearSprites == ClearSpritesMode.Always || (EnforceClearSprites == ClearSpritesMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics));

    public int spinnerFillerOpacity { get; set; } = 2;

    [YamlIgnore]
    public int SpinnerFillerOpacity {
        get => spinnerFillerOpacity;
        set {
            spinnerFillerOpacity = value;
            SpinnerFillerAlpha = value * 0.1f;
        }
    }
    #endregion


    public bool Awake_EntityActivatorReminder = true;

    public bool entityActivatorReminder { get; set; } = true;

    [YamlIgnore]
    public bool EntityActivatorReminder {
        get => Enabled && Awake_EntityActivatorReminder && entityActivatorReminder;
        set {
            entityActivatorReminder = value;
            Awake_EntityActivatorReminder = true;
        }
    }

    #region Auxilary Variables
    public void UpdateAuxiliaryVariable() {
        // update the variables associated to variables govern by spinner main switch
        // it can happen their value is changed but not via the setter (i.e. change the Awake_...s)

        UsingNotInViewColor = (UsingNotInViewColorMode == UsingNotInViewColorModes.Always) || (UsingNotInViewColorMode == UsingNotInViewColorModes.WhenUsingInViewRange && UsingInViewRange);
        UsingCountDown = (CountdownMode != CountdownModes.Off);
        if (CountdownMode == CountdownModes._3fCycle) {
            SpinnerCountdownLoad = true;
            SpinnerInterval = 0.05f;
        }
        else {
            SpinnerCountdownLoad = false;
            SpinnerInterval = 0.25f;
        }
        UsingLoadRange = (LoadRangeMode != LoadRangeModes.Neither);
        UsingInViewRange = (LoadRangeMode == LoadRangeModes.InViewRange || LoadRangeMode == LoadRangeModes.Both);
        UsingNearPlayerRange = (LoadRangeMode == LoadRangeModes.NearPlayerRange || LoadRangeMode == LoadRangeModes.Both);
    }
    public bool UsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool UsingInViewRange = true;
    public bool UsingNearPlayerRange = true;
    public bool SpinnerCountdownLoad = true;

    [Obsolete]
    public int SpinnerCountdownUpperBound => SpinnerCountdownLoad ? 9 : 99;
    public float SpinnerInterval = 0.05f;
    public float RangeAlpha = 0.4f;
    public float SpinnerFillerAlpha = 0.4f;
    public bool UsingFreezeColor = true;

    public Color LoadRangeColliderColor = CustomColors.defaultLoadRangeColliderColor;
    public Color InViewRangeColor = CustomColors.defaultInViewRangeColor;
    public Color NearPlayerRangeColor = CustomColors.defaultNearPlayerRangeColor;
    public Color CameraTargetColor = CustomColors.defaultCameraTargetColor;
    public Color NotInViewColor = CustomColors.defaultNotInViewColor;
    public Color NeverActivateColor = CustomColors.defaultNeverActivateColor;
    public Color ActivateEveryFrameColor = CustomColors.defaultActivateEveryFrameColor;

    #endregion

    #region Other

    public bool Awake_CameraTarget = true;

    public bool usingCameraTarget { get; set; } = false;

    [YamlIgnore]
    public bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    public int CameraTargetLinkOpacity { get; set; } = 6;

    public bool Awake_PixelGrid = true;

    public bool enablePixelGrid { get; set; } = false;

    [YamlIgnore]
    public bool EnablePixelGrid {
        get => Enabled && Awake_PixelGrid && enablePixelGrid;
        set {
            enablePixelGrid = value;
            Awake_PixelGrid = true;
        }
    }

    public int PixelGridWidth = 2;
    public int PixelGridOpacity { get; set; } = 8;

    public bool Awake_SpawnPoint = true;

    public bool usingSpawnPoint { get; set; } = true;

    [YamlIgnore]
    public bool UsingSpawnPoint {
        get => Enabled && Awake_SpawnPoint && usingSpawnPoint;
        set {
            usingSpawnPoint = value;
            Awake_SpawnPoint = true;
        }
    }

    public int CurrentSpawnPointOpacity = 5;

    public int OtherSpawnPointOpacity = 2;

    public bool Awake_FireBallTrack = true;

    public bool usingFireBallTrack { get; set; } = true;

    [YamlIgnore]
    public bool UsingFireBallTrack {
        get => Enabled && Awake_FireBallTrack && usingFireBallTrack;
        set {
            usingFireBallTrack = value;
            Awake_FireBallTrack = true;
        }
    }

    public bool AllowEnableModWithMainSwitch = true;

    public bool mainSwitchStateVisualize { get; set; } = true;

    [YamlIgnore]
    public bool MainSwitchStateVisualize {
        get => mainSwitchStateVisualize;
        set {
            mainSwitchStateVisualize = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Visible = mainSwitchStateVisualize;
            }
        }
    }

    public bool mainSwitchThreeStates { get; set; } = true;

    [YamlIgnore]
    public bool MainSwitchThreeStates {
        get => mainSwitchThreeStates;
        set {
            mainSwitchThreeStates = value;
            if (MainSwitchWatcher.instance is MainSwitchWatcher watcher) {
                watcher.Refresh();
            }
        }
    }

    #endregion

    #region HotKey

    [SettingName("TAS_HELPER_MAIN_SWITCH_HOTKEY")]
    [SettingSubHeader("TAS_HELPER_HOTKEY_DESCRIPTION")]
    [SettingDescriptionHardcoded]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.E)]
    public ButtonBinding keyMainSwitch { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.E);


    [SettingName("TAS_HELPER_SWITCH_COUNT_DOWN_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.R)]
    public ButtonBinding keyCountDown { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.R);


    [SettingName("TAS_HELPER_SWITCH_LOAD_RANGE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.T)]
    public ButtonBinding keyLoadRange { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.T);


    [SettingName("TAS_HELPER_SWITCH_PIXEL_GRID_WIDTH_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.F)]
    public ButtonBinding keyPixelGridWidth { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.F);


    // should not use a List<Hotkey> var, coz changing KeyPixelGridWidth will cause the hotkey get newed
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        bool updateKey = true;
        bool updateButton = true;
        bool InOuiModOption = TASHelperMenu.mainItem?.Container?.Focused is bool b && b;
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(KeyboardConfigUI), out var list) && list.Count > 0) ||
            (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list2) && list2.Count > 0)) {
            updateKey = false;
        }
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(ButtonConfigUI), out var list3) && list3.Count > 0)) {
            updateButton = false;
        }

        TH_Hotkeys.MainSwitchHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.CountDownHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.LoadRangeHotkey.Update(updateKey, updateButton);
        TH_Hotkeys.PixelGridWidthHotkey.Update(updateKey, updateButton);

        bool changed = false;

        if (TH_Hotkeys.MainSwitchHotkey.Pressed) {
            changed = true;
            switch (MainSwitch) {
                case MainSwitchModes.Off: {
                        if (!AllowEnableModWithMainSwitch) {
                            changed = false;
                            MainSwitchWatcher.instance?.Refresh(true);
                            break;
                        }
                        MainSwitch = MainSwitchThreeStates ? MainSwitchModes.OnlyDefault : MainSwitchModes.AllowAll;
                        break;
                    }
                // it may happen that MainSwitchThreeStates = false but MainSwitch = OnlyDefault... it's ok
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
            }
        }
        if (TH_Hotkeys.CountDownHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (CountdownMode) {
                    case CountdownModes.Off: CountdownMode = CountdownModes._3fCycle; break;
                    case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; break;
                    case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; break;
                }
            }
            else {
                MainSwitchWatcher.instance?.RefreshOther();
            }
        }
        if (TH_Hotkeys.LoadRangeHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (LoadRangeMode) {
                    case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; break;
                    case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; break;
                    case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; break;
                    case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; break;
                }
            }
            else {
                MainSwitchWatcher.instance?.RefreshOther();
            }
        }
        if (TH_Hotkeys.PixelGridWidthHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                EnablePixelGrid = true;
                PixelGridWidth = PixelGridWidth switch {
                    < 2 => 2,
                    < 4 => 4,
                    < 8 => 8,
                    _ => 0,
                };
                if (PixelGridWidth == 0) {
                    EnablePixelGrid = false;
                }
            }
            else {
                MainSwitchWatcher.instance?.RefreshOther();
            }
        }
        return changed;
    }

    #endregion


}

[AttributeUsage(AttributeTargets.Property)]
public class SettingDescriptionHardcodedAttribute : Attribute {
    public string description() {
        if (Dialog.Language == Dialog.Languages["schinese"]) {
            return TasHelperSettings.MainSwitchThreeStates ? "�� [�� - Ĭ�� - ȫ��] ���߼��л�\n������������ʱ���� ȫ�� ״̬�½���." : "�� [�� - ȫ��] ���߼��л�";
        }
        return TasHelperSettings.MainSwitchThreeStates ? "Switch among [Off - Default - All]\nPlease configure other settings in State All." : "Switch between [Off - All]";
    }
}

