using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using TAS.EverestInterop.Hitboxes;

namespace Celeste.Mod.TASHelper.Gameplay.Spinner;

internal static class SpinnerRenderHelper {

    [Initialize(depth: int.MinValue)]
    public static void Initialize() {
        if (Info.SpecialInfoHelper.VivSpinnerType is not null) {
            typeof(SpinnerRenderHelper).GetMethod(nameof(DrawSpinnerCollider)).IlHook((cursor, _) => {
                Instruction skipViv = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldarg_3);
                cursor.EmitDelegate(DrawVivCollider);
                cursor.Emit(OpCodes.Brfalse, skipViv);
                cursor.Emit(OpCodes.Ret);
            });
        }

        if (Info.SpecialInfoHelper.ChroniaSpinnerType is not null) {
            typeof(SpinnerRenderHelper).GetMethod(nameof(DrawSpinnerCollider)).IlHook((cursor, _) => {
                Instruction skipChronia = cursor.Next;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.Emit(OpCodes.Ldarg_2);
                cursor.Emit(OpCodes.Ldarg_3);
                cursor.EmitDelegate(DrawChroniaCollider);
                cursor.Emit(OpCodes.Brfalse, skipChronia);
                cursor.Emit(OpCodes.Ret);
            });
        }
    }

    internal static Color DefaultColor => TasSettings.EntityHitboxColor;
    internal static Color NotInViewColor => TasHelperSettings.NotInViewColor;
    internal static Color NeverActivateColor => TasHelperSettings.NeverActivateColor;
    internal static Color ActivateEveryFrameColor => TasHelperSettings.ActivateEveryFrameColor;
    // ActivatesEveryFrame now consists of 2 cases: (a) nocycle mod hazards (b) when time freeze

    public enum SpinnerColorIndex { Default, Group1, Group2, Group3, NotInView, MoreThan3, NeverActivate, FreezeActivateEveryFrame, NoCycle };
    public static Color GetSpinnerColor(SpinnerColorIndex index) {
#pragma warning disable CS8524
        return index switch {
            SpinnerColorIndex.Default => DefaultColor,
            SpinnerColorIndex.Group1 => TasSettings.CycleHitboxColor1,
            SpinnerColorIndex.Group2 => TasSettings.CycleHitboxColor2,
            SpinnerColorIndex.Group3 => TasSettings.CycleHitboxColor3,
            SpinnerColorIndex.NotInView => NotInViewColor,
            SpinnerColorIndex.MoreThan3 => TasSettings.OtherCyclesHitboxColor,
            SpinnerColorIndex.NeverActivate => NeverActivateColor,
            SpinnerColorIndex.FreezeActivateEveryFrame => ActivateEveryFrameColor,
            SpinnerColorIndex.NoCycle => ActivateEveryFrameColor
        };
#pragma warning restore CS8524
    }

    public static SpinnerColorIndex GetSpinnerColorIndex(Entity hazard, bool checkInView) {
        // we assume you've checked it's a hazard

        /*
         * bugfix v1.9.13
        if (SpinnerCalculateHelper.GetOffset(hazard) is null) {
            object[] errors = { $"{hazard.GetEntityId()} is not a hazard" , hazard.IsHazard(), Convert.ToString(hazard.Tag, 2), Convert.ToString(SpinnerCalculateHelper.IsHazardTagValue, 2), string.Join(",", BitTag.byName.Keys) };
            throw new Exception(string.Join(",\n", errors));
        }
        */

#pragma warning disable CS8629
        return checkInView ? CycleHitboxColorIndex(hazard, Info.OffsetHelper.GetOffset(hazard).Value, Info.PositionHelper.CameraPosition) : CycleHitboxColorIndexNoInView(hazard, Info.OffsetHelper.GetOffset(hazard).Value);
#pragma warning restore CS8629
    }

    internal const int ID_nocycle = -2;
    internal const int ID_infinity = -1;
    internal const int ID_uncollidable_offset = 163; // related codes is based on this constant, hardcoded, so don't change it
    public static void DrawCountdown(Vector2 Position, int CountdownTimer, SpinnerColorIndex index, bool collidable = true) {
        // when TimeRate > 1, NeverActivate can activate; when TimeRate < 1, FreezeActivatesEveryFrame can take more than 0 frame.
        // so in these cases i just use CountdownTimer
        // here by TimeRate i actually mean DeltaTime / RawDeltaTime
        // note in 2023 Jan, Everest introduced TimeRateModifier in the calculation of Engine.DeltaTime, so it's no longer DeltaTime = RawDeltaTime * TimeRate * TimeRateB
        int ID;
        if (index == SpinnerColorIndex.NoCycle) {
            ID = ID_nocycle;
        }
        else if (index == SpinnerColorIndex.NeverActivate && Engine.DeltaTime <= Engine.RawDeltaTime) {
            ID = ID_infinity;
        }
        else {
            ID = CountdownTimer;
        }
        if (!collidable) {
            ID += ID_uncollidable_offset;
        }
        CountdownRenderer.Add(ID, Position);
        return;
    }

    private static SpinnerColorIndex CycleHitboxColorIndex(Entity self, float offset, Vector2 CameraPosition) {
        if (TasHelperSettings.UsingNotInViewColor && !Info.InViewHelper.InView(self, CameraPosition) && !Info.SpecialInfoHelper.NoPeriodicCheckInViewBehavior(self)) {
            // NotInView Color is in some sense, not a cycle hitbox color, we make it independent
            // Dust needs InView to establish graphics, but that's almost instant (actually every frame can establish up to 25 dust graphics)
            // so InView seems meaningless for Dusts, especially if we only care about those 3f/15f periodic behaviors
            return SpinnerColorIndex.NotInView;
        }
        return CycleHitboxColorIndexNoInView(self, offset);
    }

    private static SpinnerColorIndex CycleHitboxColorIndexNoInView(Entity self, float offset) {
        if (!TasHelperSettings.ShowCycleHitboxColors) {
            return SpinnerColorIndex.Default;
        }
        if (Info.SpecialInfoHelper.NoCycle(self)) {
            return SpinnerColorIndex.NoCycle;
        }
        if (TasHelperSettings.UsingFreezeColor && Info.TimeActiveHelper.TimeActive >= 524288f) {
            // we assume the normal state is TimeRate = 1, so we do not detect time freeze by TimeActive + DeltaTime == TimeActive, instead just check >= 524288f (actually works for TimeRate <= 1.8)
            // so freeze colors will not appear too early in some extreme case like slow down of collecting heart
            // we make the color reflects its state at TimeRate = 1, so it will not flash during slowdown like collecting heart
            // unfortunately it will flash if TimeRate > 1, hope this will never happen
            return Info.TimeActiveHelper.OnInterval(Info.TimeActiveHelper.TimeActive, 0.05f, offset, Engine.RawDeltaTime) ? SpinnerColorIndex.FreezeActivateEveryFrame : SpinnerColorIndex.NeverActivate;
        }
        int group = Info.TimeActiveHelper.CalculateSpinnerGroup(offset);
#pragma warning disable CS8509
        return group switch {
            0 => SpinnerColorIndex.Group1,
            1 => SpinnerColorIndex.Group2,
            2 => SpinnerColorIndex.Group3,
            > 2 => SpinnerColorIndex.MoreThan3
        };
#pragma warning restore CS8509
    }

    public static void DrawSpinnerCollider(Entity self, Camera camera, Color color, bool collidable, bool _) {
        if (OnGrid(self)) {
            DrawVanillaCollider(self.Position, color, collidable);
        }
        else {
            DrawComplexSpinnerCollider(self, camera, color, collidable);
        }
    }

    public static bool DrawVivCollider(Entity self, Camera camera, Color color, bool collidable) {
        if (Info.SpecialInfoHelper.IsVivSpinner(self)) {
            if (OnGrid(self)) {
#pragma warning disable CS8600, CS8604
                string[] hitboxString = Info.SpecialInfoHelper.GetVivHitboxString(self);
                float scale = self.GetFieldValue<float>("scale");
                if (SpinnerColliderHelper.TryGetValue(hitboxString, scale, out SpinnerColliderHelper.SpinnerColliderValue value)) {
                    value.DrawOutlineAndInside(self.Position, color, collidable);
                    return true;
                }
#pragma warning restore CS8600, CS8604
            }
            DrawComplexSpinnerCollider(self, camera, color, collidable);
            return true;
        }
        return false;
    }

    public static bool DrawChroniaCollider(Entity self, Camera camera, Color color, bool collidable) {
        if (Info.SpecialInfoHelper.IsChroniaSpinner(self)) {
            if (OnGrid(self)) {
#pragma warning disable CS8600, CS8604
                string[] hitboxString = Info.SpecialInfoHelper.GetChroniaHitboxString(self);
                if (SpinnerColliderHelper.TryGetValue(hitboxString, 1f, out SpinnerColliderHelper.SpinnerColliderValue value)) {
                    value.DrawOutlineAndInside(self.Position, color, collidable);
                    return true;
                }
#pragma warning restore CS8600, CS8604
            }
            DrawComplexSpinnerCollider(self, camera, color, collidable);
            return true;
        }
        return false;
    }
    public static bool OnGrid(Entity self) {
        return self.Position.X == Math.Floor(self.Position.X) && self.Position.Y == Math.Floor(self.Position.Y);
    }

    public static void DrawComplexSpinnerCollider(Entity spinner, Camera camera, Color color, bool collidable) {
        if (spinner.Collider is not ColliderList clist) {
            return;
        }
        color *= TasHelperSettings.Ignore_TAS_UnCollidableAlpha || collidable ? 1f : HitboxColor.UnCollidableAlpha;
        Collider[] list = clist.colliders;

        foreach (Collider collider in list) {
            collider.Render(camera, color);
        }
    }

    public static void DrawVanillaCollider(Vector2 Position, Color color, bool Collidable) {
        SpinnerColliderHelper.Vanilla.DrawOutlineAndInside(Position, color, Collidable);
    }

    public static void DrawOutlineAndInside(this SpinnerColliderHelper.SpinnerColliderValue value, Vector2 Position, Color color, bool Collidable) {
        float alpha = TasHelperSettings.Ignore_TAS_UnCollidableAlpha || Collidable ? 1f : HitboxColor.UnCollidableAlpha;
        float inner_mult = Collidable ? TasHelperSettings.SpinnerFillerAlpha_Collidable : TasHelperSettings.SpinnerFillerAlpha_Uncollidable;

        if (Collidable || !TasHelperSettings.SimplifiedSpinnerDashedBorder) {
            value.Outline.DrawCentered(Position, color.SetAlpha(alpha));
        }
        else {
            value.Outline_Dashed1.DrawCentered(Position, color.SetAlpha(alpha));
            value.Outline_Dashed2.DrawCentered(Position, color.SetAlpha(alpha) * 0.3f);
        }
        value.Inside.DrawCentered(Position, Color.Lerp(color, Color.Black, 0.6f).SetAlpha(alpha * inner_mult));
    }
}
