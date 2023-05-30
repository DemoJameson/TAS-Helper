using Celeste.Mod.TASHelper.Utils;
using Microsoft.Xna.Framework;
using Monocle;
using System.Reflection;

namespace Celeste.Mod.TASHelper.Entities;

public static class FireBallExt {

    public static void Load() {
        On.Monocle.EntityList.DebugRender += PatchEntityListDebugRender;
        On.Celeste.Level.LoadLevel += OnLoadLevel;
    }

    public static void Unload() {
        On.Monocle.EntityList.DebugRender -= PatchEntityListDebugRender;
        On.Celeste.Level.LoadLevel -= OnLoadLevel;
    }

    public static void Initialize() {
        LevelExtensions.AddToTracker(typeof(FireBall));
        FireBallNodesGetter = typeof(FireBall).GetField("nodes",BindingFlags.Instance| BindingFlags.NonPublic);
    }

    public static FieldInfo FireBallNodesGetter;

    internal static readonly List<Vector2[]> CachedNodes = new List<Vector2[]>();

    private static void OnLoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
        CachedNodes.Clear();
        orig(self, playerIntro, isFromLoader);
    }
    private static void PatchEntityListDebugRender(On.Monocle.EntityList.orig_DebugRender orig, EntityList self, Camera camera) {
        orig(self, camera);
        if (!TasHelperSettings.UsingFireBallTrack || self.Scene is not Level level) {
            return;
        }
        foreach (Entity entity in level.Tracker.GetEntities<FireBall>()) {
            Vector2[] nodes = (Vector2[])FireBallNodesGetter.GetValue(entity);
            if (!CachedNodes.Contains(nodes)) {
                CachedNodes.Add(nodes);
            }
        }
        foreach (Vector2[] nodes in CachedNodes) {
            for (int i = 0; i < nodes.Length - 1; i++) {
                Monocle.Draw.Line(nodes[i], nodes[i + 1], Color.Yellow * 0.5f);
            }
        }
    }

    /*
     * The KillBox is just those part under the bounce hitbox, unless FireBall happens to have Position.Y an integer
     * btw there is some OoO issue, so i decide not to render it
     * CelesteTAS
    private static void PatchFireBallDebugRender(Entity entity) {
        if (entity is not FireBall self || !(bool)IceModeGetter.GetValue(self)) {
            return;
        }
        float y = self.Y + 4f - 1f;
        float z = (float)Math.Ceiling(y);
        if (z <= y) {
            z += 1f;
        }
        float top = Math.Max(self.Collider.AbsoluteTop, z);
        Draw.Rect(self.X - 4f, top, 9f, 1f, self.Collidable ? Color.WhiteSmoke : Color.WhiteSmoke * HitboxColor.UnCollidableAlpha);
    }
    */
}