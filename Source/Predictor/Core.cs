﻿using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Cil;
using System.Reflection;
using TAS;

namespace Celeste.Mod.TASHelper.Predictor;
public static class Core {

    public static List<RenderData> futures = new();

    public static bool HasCachedFutures {
        get => CacheFutureCountdown > 0;
        set {
            if (value) {
                CacheFutureCountdown = CacheFuturePeriod;
            }
            else {
                CacheFutureCountdown = 0;
            }
        }
    }

    public static bool InPredict = false;

    public static readonly List<Func<bool>> SkipPredictChecks = new();

    public static readonly List<Func<RenderData, bool>> EarlyStopChecks = new();

    public static int CacheFuturePeriod { get; private set; } = 60;

    public static int CacheFutureCountdown { get; private set; } = 0;

    public static void Predict(int frames, bool mustRedo = true) {
        if (!mustRedo && HasCachedFutures) {
            return;
        }

        if (!TasHelperSettings.PredictFutureEnabled || InPredict) {
            return;
        }

        TasHelperSettings.Enabled = false;
        // this stops most hooks from working (in particular, SpinnerCalculateHelper.PreSpinnerCalculate)
        SafePredict(frames);
        TasHelperSettings.Enabled = true;
    }

    private static void SafePredict(int frames) {
        if (SkipPredictCheck()) {
            return;
        }

        // warn: this overrides SpeedrunTool's (and thus TAS's) savestate
        if (!TinySRT.TH_StateManager.SaveState()) {
            return;
        }

        // Celeste.Commands.Log($"An actual Prediction in frame: {Manager.Controller.CurrentFrameInTas}");

        SaveForTAS();

        InPredict = true;

        futures.Clear();

        ModifiedAutoMute.StartMute();
        InputManager.ReadInputs(frames);

        PlayerState PreviousState;
        PlayerState CurrentState = PlayerState.GetState();

        for (int i = 0; i < frames; i++) {
            TAS.InputHelper.FeedInputs(InputManager.Inputs[i]);
            // commands are not supported

            AlmostEngineUpdate(Engine.Instance, (GameTime)typeof(Game).GetFieldInfo("gameTime").GetValue(Engine.Instance));

            PreviousState = CurrentState;
            CurrentState = PlayerState.GetState();
            if (PreventSwitchScene()) {
                break;
            }
            RenderData data = new RenderData(i + 1, PreviousState, CurrentState);
            futures.Add(data);
            if (EarlyStopCheck(data)) {
                break;
            }
        }

        TinySRT.TH_StateManager.LoadState();
        LoadForTAS();
        ModifiedAutoMute.EndMute();

        HasCachedFutures = true;
        InPredict = false;
        CacheFutureCountdown = CacheFuturePeriod;
    }

    private static void AlmostEngineUpdate(Engine engine, GameTime gameTime) {
        Engine.RawDeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Engine.DeltaTime = Engine.RawDeltaTime * Engine.TimeRate * Engine.TimeRateB * Engine.GetTimeRateComponentMultiplier(engine.scene);
        Engine.FrameCounter++;
        FreezeTimerBeforeUpdate = Engine.FreezeTimer;

        if (Engine.DashAssistFreeze) {
            if (Input.Dash.Check || !Engine.DashAssistFreezePress) {
                if (Input.Dash.Check) {
                    Engine.DashAssistFreezePress = true;
                }
                if (engine.scene != null) {
                    engine.scene.Tracker.GetEntity<PlayerDashAssist>()?.Update();
                    if (engine.scene is Level) {
                        (engine.scene as Level).UpdateTime();
                    }
                    engine.scene.Entities.UpdateLists();
                }
            }
            else {
                Engine.DashAssistFreeze = false;
            }
        }
        if (!Engine.DashAssistFreeze) {
            if (Engine.FreezeTimer > 0f) {
                Engine.FreezeTimer = Math.Max(Engine.FreezeTimer - Engine.RawDeltaTime, 0f);
            }
            else if (engine.scene != null) {
                engine.scene.BeforeUpdate();
                engine.scene.Update();
                engine.scene.AfterUpdate();
            }
        }

        /* dont do this, leave it to PreventSwitchScene
        if (engine.scene != engine.nextScene) {
            Scene from = engine.scene;
            if (engine.scene != null) {
                engine.scene.End();
            }
            engine.scene = engine.nextScene;
            engine.OnSceneTransition(from, engine.nextScene);
            if (engine.scene != null) {
                engine.scene.Begin();
            }
        }
        */
        // base.Update(gameTime); i don't know how to call this correctly... bugs always occur
    }

    [Initialize]
    public static void Initialize() {
        // CelesteTAS.Core uses DetourContext {After = new List<string> {"*"}}, so our hooks are "inside" TAS.Core hooks
        // how tas frame is paused: early return in MInput. So our hooks should be after this

        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).HookAfter(() => {
            if (StrictFrameStep && TasHelperSettings.PredictOnFrameStep && Engine.Scene is Level) {
                Predict(TasHelperSettings.TimelineLength + CacheFuturePeriod, false);
            }
        });
        typeof(Engine).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).IlHook((cursor, _) => {
            if (cursor.TryGotoNext(MoveType.After, ins => ins.MatchCall(typeof(MInput), "Update"))) {
                cursor.EmitDelegate(AfterMInputUpdate);
            }
        });

        typeof(Level).GetMethod("BeforeRender").HookBefore(DelayedActions);

        HookHelper.SkipMethod(typeof(Core), nameof(InPredictMethod), typeof(GameInfo).GetMethod("Update", BindingFlags.Public | BindingFlags.Static));

        InitializeChecks();
        InitializeCachePeriod();
    }

    private static void AfterMInputUpdate() {
        FreezeTimerBeforeUpdate = Engine.FreezeTimer;
        neverClearStateThisFrame = true;
        if (!Manager.Running) {
            HasCachedFutures = false;
            futures.Clear();
            TinySRT.TH_StateManager.ClearState();
            return;
        }

        CacheFutureCountdown--;
        if (!FutureMoveLeft()) {
            HasCachedFutures = false;
            futures.Clear();
            TinySRT.TH_StateManager.ClearState();
            return;
        }
        if (!HasCachedFutures) {
            TinySRT.TH_StateManager.ClearState();
        }
    }

    public static float FreezeTimerBeforeUpdate = 0f; // include those predicted frames

    public static bool ThisPredictedFrameFreezed => FreezeTimerBeforeUpdate > 0f;
    public static void InitializeChecks() {
        SkipPredictChecks.Clear();
        EarlyStopChecks.Clear();
        SkipPredictChecks.Add(SafeGuard);
        if (!TasHelperSettings.StartPredictWhenTransition) {
            SkipPredictChecks.Add(() => Engine.Scene is Level level && level.Transitioning);
        }
        if (TasHelperSettings.StopPredictWhenTransition) {
            EarlyStopChecks.Add(data => data.Keyframe.HasFlag(KeyframeType.BeginTransition));
        }
        if (TasHelperSettings.StopPredictWhenDeath) {
            EarlyStopChecks.Add(data => data.Keyframe.HasFlag(KeyframeType.GainDead));
        }
    }

    public static void InitializeCachePeriod() {
        if (TasHelperSettings.TimelineLength > 500) {
            CacheFuturePeriod = 120;
        }
        else {
            CacheFuturePeriod = 60;
        }
        TinySRT.TH_StateManager.ClearState();
        HasCachedFutures = false;
        futures.Clear();
    }

    public static bool FutureMoveLeft() {
        if (futures.Count <= 0) {
            return false;
        }
        futures.RemoveAt(0);
        futures = futures.Select(future => future with { index = future.index - 1 }).ToList();
        return true;
    }

    private static bool SafeGuard() {
        return Engine.Scene is not Level;
    }

    internal static bool delayedClearState = false;

    private static bool neverClearStateThisFrame = true;

    private static bool delayedMustRedo;

    private static bool hasDelayedPredict = false;
    public static void PredictLater(bool mustRedo) {
        hasDelayedPredict = true;
        delayedMustRedo = mustRedo;
    }
    private static void DelayedActions() {
        DelayedClearState();
        DelayedPredict();
    }
    private static void DelayedClearState() {
        if (delayedClearState && neverClearStateThisFrame && !InPredict) {
            neverClearStateThisFrame = false;
            delayedClearState = false;
            TinySRT.TH_StateManager.ClearState();
        }
    }
    private static void DelayedPredict() {
        if (hasDelayedPredict && !InPredict) {
            Manager.Controller.RefreshInputs(false);
            GameInfo.Update();
            Predict(TasHelperSettings.TimelineLength + CacheFuturePeriod, delayedMustRedo);
            hasDelayedPredict = false;
        }
        // we shouldn't do this in half of the render process
    }

    private static bool InPredictMethod() {
        return InPredict;
    }

    private static void SaveForTAS() {
        DashTime = GameInfo.DashTime;
        Frozen = GameInfo.Frozen;
        TransitionFrames = GameInfo.TransitionFrames;
        freezeTimerBeforeUpdateBeforePredictLoops = FreezeTimerBeforeUpdate;
    }

    private static float DashTime;
    private static bool Frozen;
    private static int TransitionFrames;
    private static float freezeTimerBeforeUpdateBeforePredictLoops;
    private static void LoadForTAS() {
        GameInfo.DashTime = DashTime;
        GameInfo.Frozen = Frozen;
        GameInfo.TransitionFrames = TransitionFrames;
        FreezeTimerBeforeUpdate = freezeTimerBeforeUpdateBeforePredictLoops;
    }

    public static bool SkipPredictCheck() {
        foreach (Func<bool> check in SkipPredictChecks) {
            if (check()) {
                return true;
            }
        }
        return false;
    }

    public static bool PreventSwitchScene() {
        if (Engine.Instance.scene != Engine.Instance.nextScene) {
            Engine.Instance.nextScene = Engine.Instance.scene;
            return true;
        }

        return false;
    }
    public static bool EarlyStopCheck(RenderData data) {
        foreach (Func<RenderData, bool> check in EarlyStopChecks) {
            if (check(data)) {
                return true;
            }
        }
        return false;
    }
}
