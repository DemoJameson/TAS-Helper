﻿//#define UseRandomStuff
//#define JumpThruPatch
//#define LightingRendererBugReproducer

#if UseRandomStuff

using Monocle;
using Celeste;
using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Celeste.Mod.TASHelper.Entities;
using TAS.Input.Commands;
using TAS;
using YamlDotNet.Core;

namespace Celeste.Mod.TASHelper.Experimental;
internal class RandomStuff {
    // test some stuff, not actually in use

#if JumpThruPatch

    internal static class JumpThruPatch {
        [Load]
        private static void Load() {
            On.Celeste.JumpThru.MoveHExact += JumpThruMoveH_LiftSpeedPatch;
        }

        [Unload]
        private static void Unload() {
            On.Celeste.JumpThru.MoveHExact -= JumpThruMoveH_LiftSpeedPatch;
        }

        private static void JumpThruMoveH_LiftSpeedPatch(On.Celeste.JumpThru.orig_MoveHExact orig, JumpThru self, int move) {
            if (self.Collidable) {
                foreach (Actor entity in self.Scene.Tracker.GetEntities<Actor>()) {
                    if (entity.IsRiding(self)) {
                        entity.LiftSpeed = self.LiftSpeed;
                    }
                }
            }
            orig(self, move);
        }
    }
#endif

#if LightingRendererBugReproducer

    internal static class LightingRendererBugReproducer {
        [Load]
        private static void Load() {
            On.Celeste.Level.LoadLevel += OnLoadLevel;
        }

        [Unload]
        private static void Unload() {
            On.Celeste.Level.LoadLevel -= OnLoadLevel;
        }

        [Initialize]
        private static void Initialize() {
            typeof(LightingRenderer).GetMethodInfo("StartDrawingPrimitives").HookBefore<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    int count = 0;
                    for (int i = 0; i < 64; i++) {
                        VertexLight vertexLight = r.lights[i];
                        if (vertexLight == null || !vertexLight.Dirty) {
                            continue;
                        }
                        count++;
                    }
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"--------------- Clear ------------ {r.indexCount} {r.vertexCount} {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count} {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count} {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {count} \n");
                }
            });
            typeof(LightingRenderer).GetMethodInfo("SetOccluder").HookAfter<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"\n \n [SetCutout] indexCount = {r.indexCount}; vertexCount = {r.vertexCount}; \n LightOcclude = {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count}; EffectCutout = {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count}; VertexLight = {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {TAS.Manager.Controller.CurrentFrameInTas}");
                }
            });
            typeof(LightingRenderer).GetMethodInfo("SetCutout").HookAfter<LightingRenderer>(r => {
                if (r.indexCount > 100) {
                    Logger.Log(LogLevel.Debug, "TAS Helper", $"\n \n [SetCutout] indexCount = {r.indexCount}; vertexCount = {r.vertexCount}; \n  LightOcclude = {Engine.Scene.Tracker.Components[typeof(LightOcclude)].Count}; EffectCutout = {Engine.Scene.Tracker.Components[typeof(EffectCutout)].Count}; VertexLight = {Engine.Scene.Tracker.Components[typeof(VertexLight)].Count} {TAS.Manager.Controller.CurrentFrameInTas}");
                }
            });
        }

        public class LightingRendererKiller : Entity {
            public LightingRendererKiller(Level level) : base(new Vector2(level.Bounds.X, level.Bounds.Y)) {
                this.Collider = new Hitbox(level.Bounds.Width, level.Bounds.Height);
                for (int i = 1; i <= 4; i++) {
                    this.Add(new EffectCutout());
                }
            }
        }
        private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            self.Add(new LightingRendererKiller(self));
        }
    }
    
    private static Vector2 HandleMotionSmoothing() {
        if (Engine.Scene is not Level level) {
            return Vector2.Zero;
        }
        var ob = MotionSmoothing.Utilities.ToggleableFeature<MotionSmoothingHandler>.Instance;
        if (ob is null) {
            Logger.Log(LogLevel.Debug, "TASHelper", "1. Null Reference Exception");
            return Vector2.Zero;
        }
        MotionSmoothing.Smoothing.States.IPositionSmoothingState positionSmoothingState = ob.GetState(level.Camera) as MotionSmoothing.Smoothing.States.IPositionSmoothingState;
        if (positionSmoothingState is null) {
            MotionSmoothing.Smoothing.MotionSmoothingHandler.Instance.InvokeMethod("SmoothCamera", new object[] { level.Camera});
            if (ob.GetState(level.Camera) is null) {
                if (level.Camera is null) {
                    Logger.Log(LogLevel.Debug, "TASHelper", "2.0. Null Reference Exception");
                }
                else if (ob.ValueSmoother is null) {
                    Logger.Log(LogLevel.Debug, "TASHelper", "2.1. Null Reference Exception");
                }
                else if (ob.ValueSmoother.GetState(level.Camera) is null){
                    if (ob.PushSpriteSmoother is null) {
                        Logger.Log(LogLevel.Debug, "TASHelper", "2.2. Null Reference Exception");
                    }
                    else {
                        Logger.Log(LogLevel.Debug, "TASHelper", "2.3. Null Reference Exception");
                    }
                }
            }
            else {
                Logger.Log(LogLevel.Debug, "TASHelper", "3. Null Reference Exception");
            }
            return Vector2.Zero;
        }
        return positionSmoothingState.SmoothedRealPosition.Floor() - positionSmoothingState.SmoothedRealPosition;
    }
#endif

    [Initialize]
    private static void Initialize() {
        /*
        ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.PixelRendered.PixelComponent")?.GetMethodInfo("DebugRender")?.IlHook((cursor, _) => {
            Instruction start = cursor.Next;
            cursor.EmitDelegate(IsSimplifiedGraphics);
            cursor.Emit(OpCodes.Brfalse, start);
            cursor.Emit(OpCodes.Ret);
        });
        
        ModUtils.GetType("BrokemiaHelper", "BrokemiaHelper.PixelRendered.PixelComponent")?.GetMethodInfo("Render")?.IlHook((cursor, _) => {
            Instruction start = cursor.Next;
            cursor.EmitDelegate(IsSimplifiedGraphics);
            cursor.Emit(OpCodes.Brfalse, start);
            cursor.Emit(OpCodes.Ret);
        });
        */

        /*
        ModUtils.GetType("MotionSmoothing", "Celeste.Mod.MotionSmoothing.Smoothing.Targets.UnlockedCameraSmoother")?.GetMethodInfo("GetCameraOffset")?.IlHook((cursor, _) => {
            cursor.EmitDelegate(HandleMotionSmoothing);
            cursor.Emit(OpCodes.Ret);
        });
        */

        Logger.Log(LogLevel.Warn, "TAS Helper", "TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");
        Celeste.Commands.Log("WARNING: TAS Helper Random Stuff loaded! Please contact the author to disable these codes.");
    }


    private static bool IsSimplifiedGraphics() => TasSettings.SimplifiedGraphics;
}

#endif