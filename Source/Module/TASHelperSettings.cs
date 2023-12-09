using Celeste.Mod.TASHelper.Entities;
using Celeste.Mod.TASHelper.Gameplay.Spinner;
using Celeste.Mod.TASHelper.Module.Menu;
using Celeste.Mod.TASHelper.OrderOfOperation;
using Celeste.Mod.TASHelper.Utils;
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

        // LoadSettings will call ctor to set default values, which seem to go wrong for enum if we write these codes outside
        mainSwitch = MainSwitchModes.OnlyDefault;
        usingNotInViewColorMode = UsingNotInViewColorModes.WhenUsingInViewRange;
        countdownMode = CountdownModes.Off;
        CountdownFont = CountdownFonts.HiresFont;
        loadRangeMode = LoadRangeModes.Neither;
        EnforceClearSprites = SimplifiedGraphicsMode.WhenSimplifyGraphics;
        LoadRangeColliderMode = LoadRangeColliderModes.Auto;
    }

    internal void OnLoadSettings() {
        UpdateAuxiliaryVariable();

        keyMainSwitch ??= new((Buttons)0, Keys.LeftControl, Keys.E);
        keyCountDown ??= new((Buttons)0, Keys.LeftControl, Keys.R);
        keyLoadRange ??= new((Buttons)0, Keys.LeftControl, Keys.T);
        keyPixelGridWidth ??= new((Buttons)0, Keys.LeftControl, Keys.F);
        keyPredictEnable ??= new((Buttons)0, Keys.LeftControl, Keys.W);
        keyPredictFuture ??= new((Buttons)0, Keys.LeftControl, Keys.P);
        keyOoO_Step ??= new((Buttons)0, Keys.LeftControl, Keys.G);
        keyOoO_Fastforward ??= new((Buttons)0, Keys.LeftControl, Keys.Y);

        // it seems some bug can happen with deserialization
    }

    public bool Enabled = true;

    #region MainSwitch

    // it will only affect main options, will not affect suboptions (which will not work if corresponding main option is not on)

    public enum MainSwitchModes { Off, OnlyDefault, AllowAll }

    public MainSwitchModes mainSwitch;

    [YamlIgnore]
    public MainSwitchModes MainSwitch {
        get => mainSwitch;
        set {
            if (mainSwitch == value) {
                // prevent infinite recursion
                return;
            }
            mainSwitch = value;
            HotkeyWatcher.instance?.RefreshMainSwitch();
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
        Awake_TrackSpinnerTrack = false;
        Awake_RotateSpinnerTrack = false;
        Awake_PredictFuture = false;
        Awake_EnableOoO = false;
        Awake_OpenConsoleInTas = false;
        Awake_ScrollableHistoryLog = false;
        Awake_BetterInvincible = false;
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
        Awake_TrackSpinnerTrack = true;
        Awake_RotateSpinnerTrack = true;
        Awake_PredictFuture = true;
        Awake_EnableOoO = true;
        Awake_OpenConsoleInTas = true;
        Awake_ScrollableHistoryLog = true;
        Awake_BetterInvincible = true;
    }

    #endregion

    #region CycleHitboxColor
    public bool Awake_CycleHitboxColors = true;

    // we need to make it public, so this setting is stored
    // though we don't want anyone to visit it directly...

    public bool showCycleHitboxColor = true;

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

    public UsingNotInViewColorModes usingNotInViewColorMode;

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

    public CountdownModes countdownMode;

    [YamlIgnore]
    public CountdownModes CountdownMode {
        get => Enabled && Awake_CountdownModes ? countdownMode : CountdownModes.Off;
        set {
            countdownMode = value;
            Awake_CountdownModes = true;

            UsingCountDown = (CountdownMode != CountdownModes.Off);
            CountdownRenderer.ClearCache();
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

    public CountdownFonts CountdownFont;

    public int HiresFontSize = 8;

    public bool UsingHiresFont => CountdownFont == CountdownFonts.HiresFont;

    public int HiresFontStroke = 5;

    [Obsolete]
    public bool DoNotRenderWhenFarFromView = true;

    public bool CountdownBoost = false;

    public bool DarkenWhenUncollidable = true;

    #endregion

    #region LoadRange

    public bool Awake_LoadRange = true;
    public enum LoadRangeModes { Neither, InViewRange, NearPlayerRange, Both };

    public LoadRangeModes loadRangeMode;

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
            UsingLoadRangeCollider = LoadRangeColliderMode switch {
                LoadRangeColliderModes.Off => false,
                LoadRangeColliderModes.Auto => UsingLoadRange,
                LoadRangeColliderModes.Always => true,
                _ => true
            };
            LoadRangeColliderRenderer.ClearCache();
            CountdownRenderer.ClearCache(); // using load range affects count down position, so we need clear cache
        }
    }

    public int InViewRangeWidth = 16;

    public int NearPlayerRangeWidth = 8;

    public int loadRangeOpacity = 4;

    [YamlIgnore]
    public int LoadRangeOpacity {
        get => loadRangeOpacity;
        set {
            loadRangeOpacity = value;
            RangeAlpha = value * 0.1f;
        }
    }

    public bool ApplyCameraZoom = false;

    public enum LoadRangeColliderModes { Off, Auto, Always };

    public LoadRangeColliderModes loadRangeColliderMode = LoadRangeColliderModes.Auto;

    [YamlIgnore]

    public LoadRangeColliderModes LoadRangeColliderMode {
        get => loadRangeColliderMode;
        set {
            loadRangeColliderMode = value;
            UsingLoadRangeCollider = LoadRangeColliderMode switch {
                LoadRangeColliderModes.Off => false,
                LoadRangeColliderModes.Auto => UsingLoadRange,
                LoadRangeColliderModes.Always => true,
                _ => true
            };
            LoadRangeColliderRenderer.ClearCache();
        }
    }

    #endregion

    #region Simplified Graphics

    public bool Awake_EnableSimplifiedSpinner = true;

    public bool enableSimplifiedSpinner = true;

    [YamlIgnore]
    public bool EnableSimplifiedSpinner {
        get => Enabled && Awake_EnableSimplifiedSpinner && enableSimplifiedSpinner;
        set {
            enableSimplifiedSpinner = value;
            Awake_EnableSimplifiedSpinner = true;
        }
    }

    public enum SimplifiedGraphicsMode { Off, WhenSimplifyGraphics, Always };

    public static bool SGModeToBool(SimplifiedGraphicsMode mode) {
        return mode == SimplifiedGraphicsMode.Always || (mode == SimplifiedGraphicsMode.WhenSimplifyGraphics && TasSettings.SimplifiedGraphics);
    }

    public SimplifiedGraphicsMode EnforceClearSprites = SimplifiedGraphicsMode.WhenSimplifyGraphics;

    public bool ClearSpinnerSprites => EnableSimplifiedSpinner && SGModeToBool(EnforceClearSprites);

    public int spinnerFillerOpacity_Collidable = 8;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Collidable {
        get => spinnerFillerOpacity_Collidable;
        set {
            spinnerFillerOpacity_Collidable = value;
            SpinnerFillerAlpha_Collidable = value * 0.1f;
        }
    }

    public int spinnerFillerOpacity_Uncollidable = 2;

    [YamlIgnore]
    public int SpinnerFillerOpacity_Uncollidable {
        get => spinnerFillerOpacity_Uncollidable;
        set {
            spinnerFillerOpacity_Uncollidable = value;
            SpinnerFillerAlpha_Uncollidable = value * 0.1f;
        }
    }

    public bool Ignore_TAS_UnCollidableAlpha = true;

    public bool SimplifiedSpinnerDashedBorder = true;

    public SimplifiedGraphicsMode EnableSimplifiedLightningMode = SimplifiedGraphicsMode.WhenSimplifyGraphics;

    public bool EnableSimplifiedLightning => Enabled && SGModeToBool(EnableSimplifiedLightningMode); // both inner and outline

    public bool HighlightLoadUnload = false;

    public bool ApplyActualCollideHitboxForSpinner = false;

    public bool ApplyActualCollideHitboxForLightning = false;

    #endregion

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

        UsingLoadRangeCollider = LoadRangeColliderMode switch {
            LoadRangeColliderModes.Off => false,
            LoadRangeColliderModes.Auto => UsingLoadRange,
            LoadRangeColliderModes.Always => true,
            _ => true
        };
        LoadRangeColliderRenderer.ClearCache();
        CountdownRenderer.ClearCache();
    }
    public bool UsingCountDown = false;
    public bool UsingLoadRange = true;
    public bool UsingInViewRange = true;
    public bool UsingNearPlayerRange = true;
    public bool SpinnerCountdownLoad = true;
    public bool UsingLoadRangeCollider = true;
    public float SpinnerInterval = 0.05f;
    public float RangeAlpha = 0.4f;
    public float SpinnerFillerAlpha_Collidable = 0.8f;
    public float SpinnerFillerAlpha_Uncollidable = 0f;
    public bool UsingFreezeColor = true;

    public Color LoadRangeColliderColor = CustomColors.defaultLoadRangeColliderColor;
    public Color InViewRangeColor = CustomColors.defaultInViewRangeColor;
    public Color NearPlayerRangeColor = CustomColors.defaultNearPlayerRangeColor;
    public Color CameraTargetColor = CustomColors.defaultCameraTargetColor;
    public Color NotInViewColor = CustomColors.defaultNotInViewColor;
    public Color NeverActivateColor = CustomColors.defaultNeverActivateColor;
    public Color ActivateEveryFrameColor = CustomColors.defaultActivateEveryFrameColor;

    #endregion

    #region Predictor

    public bool predictFutureEnabled = false;

    public bool Awake_PredictFuture = true;

    [YamlIgnore]
    public bool PredictFutureEnabled {
        get => Enabled && Awake_PredictFuture && ModUtils.SpeedrunToolInstalled && predictFutureEnabled;
        set {
            predictFutureEnabled = value;
            Awake_PredictFuture = true;
        }
    }

    public bool DropPredictionWhenTasFileChange = true;

    public bool PredictOnFrameStep = true;

    public bool PredictOnFileChange = false;

    public bool PredictOnHotkeyPressed = true;

    public int TimelineLength = 100;

    public int UltraSpeedLowerLimit = 170;

    public bool TimelineFinestScale = true;

    public TimelineScales TimelineFineScale = TimelineScales._5;

    public TimelineScales TimelineCoarseScale = TimelineScales.NotApplied;

    public enum TimelineScales { NotApplied, _2, _5, _10, _15, _20, _25, _30, _45, _60, _100 }

    public static int ToInt(TimelineScales scale) {
        return scale switch {
            TimelineScales.NotApplied => -1,
            TimelineScales._2 => 2,
            TimelineScales._5 => 5,
            TimelineScales._10 => 10,
            TimelineScales._15 => 15,
            TimelineScales._20 => 20,
            TimelineScales._25 => 25,
            TimelineScales._30 => 30,
            TimelineScales._45 => 45,
            TimelineScales._60 => 60,
            TimelineScales._100 => 100,
            _ => -1
        };
    }

    public bool TimelineFadeOut = true;

    public bool StartPredictWhenTransition = true;

    public bool StopPredictWhenTransition = true;

    public bool StopPredictWhenDeath = true;

    public bool StopPredictWhenKeyframe = false;

    public bool UseKeyFrame = true;

    public bool UseKeyFrameTime = true;

    public bool UseFlagDead = true;

    public bool UseFlagGainCrouched = false;

    public bool UseFlagLoseCrouched = false;

    public bool UseFlagGainOnGround = true;

    public bool UseFlagLoseOnGround = false;

    public bool UseFlagGainPlayerControl = true;

    public bool UseFlagLosePlayerControl = true;

    public bool UseFlagOnEntityState = true;

    public bool UseFlagRefillDash = false;

    public bool UseFlagGainUltra = true;

    public bool UseFlagOnBounce = true;

    public bool UseFlagCanDashInStLaunch = true;

    public bool UseFlagGainLevelControl = true;

    public bool UseFlagLoseLevelControl = true;

    public bool UseFlagRespawnPointChange = false;

    public bool UseFlagGainFreeze = false;

    public bool UseFlagLoseFreeze = false;

    public bool UseFlagGetRetained = false;

    public Color PredictorEndpointColor = CustomColors.defaultPredictorEndpointColor;

    public Color PredictorFinestScaleColor = CustomColors.defaultPredictorFinestScaleColor;

    public Color PredictorFineScaleColor = CustomColors.defaultPredictorFineScaleColor;

    public Color PredictorCoarseScaleColor = CustomColors.defaultPredictorCoarseScaleColor;

    public Color PredictorKeyframeColor = CustomColors.defaultPredictorKeyframeColor;

    #endregion

    #region Other

    public bool Awake_EntityActivatorReminder = true;

    public bool entityActivatorReminder = true;

    [YamlIgnore]
    public bool EntityActivatorReminder {
        get => Enabled && Awake_EntityActivatorReminder && entityActivatorReminder;
        set {
            entityActivatorReminder = value;
            Awake_EntityActivatorReminder = true;
        }
    }

    public bool Awake_CameraTarget = true;

    public bool usingCameraTarget = false;

    [YamlIgnore]
    public bool UsingCameraTarget {
        get => Enabled && Awake_CameraTarget && usingCameraTarget;
        set {
            usingCameraTarget = value;
            Awake_CameraTarget = true;
        }
    }

    public int CameraTargetLinkOpacity = 6;

    public bool Awake_PixelGrid = true;

    public bool enablePixelGrid = false;

    [YamlIgnore]
    public bool EnablePixelGrid {
        get => Enabled && Awake_PixelGrid && enablePixelGrid;
        set {
            enablePixelGrid = value;
            Awake_PixelGrid = true;
        }
    }

    public int PixelGridWidth = 2;
    public int PixelGridOpacity = 8;

    public bool Awake_SpawnPoint = true;

    public bool usingSpawnPoint = true;

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

    public bool usingFireBallTrack = true;

    [YamlIgnore]
    public bool UsingFireBallTrack {
        get => Enabled && Awake_FireBallTrack && usingFireBallTrack;
        set {
            usingFireBallTrack = value;
            Awake_FireBallTrack = true;
        }
    }

    public bool Awake_TrackSpinnerTrack = true;

    public bool usingTrackSpinnerTrack = false;

    [YamlIgnore]
    public bool UsingTrackSpinnerTrack {
        get => Enabled && Awake_TrackSpinnerTrack && usingTrackSpinnerTrack;
        set {
            usingTrackSpinnerTrack = value;
            Awake_TrackSpinnerTrack = true;
        }
    }

    public bool Awake_RotateSpinnerTrack = true;

    public bool usingRotateSpinnerTrack = false;

    [YamlIgnore]
    public bool UsingRotateSpinnerTrack {
        get => Enabled && Awake_RotateSpinnerTrack && usingRotateSpinnerTrack;
        set {
            usingRotateSpinnerTrack = value;
            Awake_RotateSpinnerTrack = true;
        }
    }

    public bool AllowEnableModWithMainSwitch = true;

    public bool hotKeyStateVisualize = true;

    [YamlIgnore]
    public bool HotkeyStateVisualize {
        get => hotKeyStateVisualize;
        set {
            hotKeyStateVisualize = value;
            if (HotkeyWatcher.instance is HotkeyWatcher watcher) {
                watcher.Visible = hotKeyStateVisualize;
            }
        }
    }

    public bool mainSwitchThreeStates = true;

    [YamlIgnore]
    public bool MainSwitchThreeStates {
        get => mainSwitchThreeStates;
        set {
            mainSwitchThreeStates = value;
            if (HotkeyWatcher.instance is HotkeyWatcher watcher) {
                watcher.RefreshMainSwitch();
            }
        }
    }

    public bool enableOoO = false;

    public bool Awake_EnableOoO = true;

    [YamlIgnore]

    public bool EnableOoO {
        get => Enabled && Awake_EnableOoO && enableOoO;
        set {
            enableOoO = value;
            Awake_EnableOoO = true;
        }
    }

    public bool enableOpenConsoleInTas { get; set; } = true;

    public bool Awake_OpenConsoleInTas = true;

    [YamlIgnore]
    public bool EnableOpenConsoleInTas {
        get => Enabled && Awake_OpenConsoleInTas && enableOpenConsoleInTas;
        set {
            enableOpenConsoleInTas = value;
            Awake_OpenConsoleInTas = true;
        }
    }

    public bool enableScrollableHistoryLog { get; set; } = true;

    public bool Awake_ScrollableHistoryLog = true;

    [YamlIgnore]
    public bool EnableScrollableHistoryLog {
        get => Enabled && Awake_ScrollableHistoryLog && enableScrollableHistoryLog;
        set {
            enableScrollableHistoryLog = value;
            Awake_ScrollableHistoryLog = true;
        }
    }

    public bool betterInvincible = true;

    public bool Awake_BetterInvincible = true;

    [YamlIgnore]

    public bool BetterInvincible {
        get => Enabled && Awake_BetterInvincible && betterInvincible;
        set {
            betterInvincible = value;
            Awake_BetterInvincible = true;
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

    [SettingName("TAS_HELPER_PREDICT_ENABLE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.W)]
    public ButtonBinding keyPredictEnable { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.W);

    [SettingName("TAS_HELPER_PREDICT_FUTURE_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.P)]
    public ButtonBinding keyPredictFuture { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.P);

    [SettingName("TAS_HELPER_OOO_STEP_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.G)]
    public ButtonBinding keyOoO_Step { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.G);

    [SettingName("TAS_HELPER_OOO_FASTFORWARD_HOTKEY")]
    [DefaultButtonBinding2(0, Keys.LeftControl, Keys.Y)]
    public ButtonBinding keyOoO_Fastforward { get; set; } = new((Buttons)0, Keys.LeftControl, Keys.Y);


    // should not use a List<Hotkey> var, coz changing KeyPixelGridWidth will cause the hotkey get newed
    public bool SettingsHotkeysPressed() {
        if (Engine.Scene is not Level level) {
            return false;
        }

        bool updateKey = true;
        bool updateButton = true;
        bool InOuiModOption = TASHelperMenu.mainItem?.Container is { } container && container.Visible;
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(KeyboardConfigUI), out var list) && list.Count > 0) ||
            (level.Tracker.Entities.TryGetValue(typeof(ModuleSettingsKeyboardConfigUIExt), out var list2) && list2.Count > 0)) {
            updateKey = false;
        }
        if (InOuiModOption || (level.Tracker.Entities.TryGetValue(typeof(ButtonConfigUI), out var list3) && list3.Count > 0)) {
            updateButton = false;
        }

        TH_Hotkeys.Update(updateKey, updateButton);

        bool changed = false; // if settings need to be saved

        if (TH_Hotkeys.MainSwitchHotkey.Pressed) {
            changed = true;
            switch (MainSwitch) {
                case MainSwitchModes.Off: {
                        if (!AllowEnableModWithMainSwitch) {
                            changed = false;
                            Refresh("Enabling TAS Helper with Hotkey is disabled!");
                            break;
                        }
                        MainSwitch = MainSwitchThreeStates ? MainSwitchModes.OnlyDefault : MainSwitchModes.AllowAll;
                        break;
                    }
                // it may happen that MainSwitchThreeStates = false but MainSwitch = OnlyDefault... it's ok
                case MainSwitchModes.OnlyDefault: MainSwitch = MainSwitchModes.AllowAll; break;
                case MainSwitchModes.AllowAll: MainSwitch = MainSwitchModes.Off; break;
                    // other HotkeyWatcher refresh are left to the setter of mainSwitch
            }
        }
        if (TH_Hotkeys.CountDownHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (CountdownMode) {
                    case CountdownModes.Off: CountdownMode = CountdownModes._3fCycle; Refresh("Hazard Countdown Mode = 3f Cycle"); break;
                    case CountdownModes._3fCycle: CountdownMode = CountdownModes._15fCycle; Refresh("Hazard Countdown Mode = 15f Cycle"); break;
                    case CountdownModes._15fCycle: CountdownMode = CountdownModes.Off; Refresh("Hazard Countdown Mode = Off"); break;
                }

            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        if (TH_Hotkeys.LoadRangeHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                switch (LoadRangeMode) {
                    case LoadRangeModes.Neither: LoadRangeMode = LoadRangeModes.InViewRange; Refresh("Load Range Mode = InView"); break;
                    case LoadRangeModes.InViewRange: LoadRangeMode = LoadRangeModes.NearPlayerRange; Refresh("Load Range Mode = NearPlayer"); break;
                    case LoadRangeModes.NearPlayerRange: LoadRangeMode = LoadRangeModes.Both; Refresh("Load Range Mode = Both"); break;
                    case LoadRangeModes.Both: LoadRangeMode = LoadRangeModes.Neither; Refresh("Load Range Mode = Neither"); break;
                }
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
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
                string str = !EnablePixelGrid || DebugRendered ? "" : ", but DebugRender is not turned on";
                Refresh($"Pixel Grid Width = {PixelGridWidth}{str}");
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        if (TH_Hotkeys.PredictEnableHotkey.Pressed) {
            if (Enabled) {
                changed = true;
                predictFutureEnabled = !predictFutureEnabled;
                Refresh("Predictor " + (predictFutureEnabled ? "Enabled" : "Disabled"));
            }
            else {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
        }
        if (TH_Hotkeys.PredictFutureHotkey.Pressed) {
            if (!Enabled) {
                HotkeyWatcher.RefreshHotkeyDisabled();
            }
            else if (!TasHelperSettings.PredictFutureEnabled) {
                Refresh("Predictor NOT enabled");
            }
            else if (!TasHelperSettings.PredictOnHotkeyPressed) {
                Refresh("Make-a-Prediction hotkey NOT enabled");
            }
            else if (!FrameStep) {
                Refresh("Not frame-stepping, refuse to predict");
            }
            else {
                Predictor.Core.PredictLater(false);
                Refresh("Predictor Start");
            }
        }
        if (EnableOpenConsoleInTas && TH_Hotkeys.OpenConsole.Pressed) {
            Gameplay.ConsoleEnhancement.SetOpenConsole();
            // it's completely ok that this feature is not enabled and people press this key, so there's no warning
        }
        OoO_Core.OnHotkeysPressed();

        return changed;

        void Refresh(string text) {
            HotkeyWatcher.Refresh(text);
        }
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

