using HarmonyLib;
using nel;
using PixelLiner;
using UnityEngine;
using XX;
using KnightInCradle.CharmUi;

namespace KnightInCradle
{
    internal static class NativeJoniFrame
    {
        private static UIStatus lastUi;
        private static bool lastActive;
        private static bool lastHive;
        private static bool lastKnight;
        private static float flutterClock;
        private static bool wasFluttering;
        private static bool Active => CharmEffects.JoniHudActive;
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UIStatus), "DrawGageLay"),
                prefix: new HarmonyMethod(typeof(NativeJoniFrame), nameof(Before)),
                postfix: new HarmonyMethod(typeof(NativeJoniFrame), nameof(After)));
        }
        internal static void Tick()
        {
            var ui = UIStatus.Instance;
            bool active = Active;
            bool knight = KnightInCradlePlugin.KnightModeActive;
            bool hive = !knight && CharmEffects.IsEquipped(CharmOwner.Noel, CharmEffects.HiveId);
            if (ui != null && (ui != lastUi || active != lastActive || hive != lastHive || knight != lastKnight))
            {
                // Fill colors are cached independently from frames and numeric HP changes.
                // Regenerate native geometry on equip AND unequip so old tint cannot remain.
                ui.redraw_hp = true;
                ui.redraw_mp = true;
            }
            if (ui != null && (ui != lastUi || active != lastActive))
            {
                ui.redraw_gage = true;
                ui.redraw_bar_num = true;
            }
            if (!active || ui != lastUi || !lastActive) flutterClock = 0f;
            else flutterClock += Time.deltaTime;
            bool fluttering = false;
            if (active)
                for (int leaf = 0; leaf < 10; leaf++) fluttering |= LeafFlutter.Amount(flutterClock, leaf) > 0f;
            // Redraw during the short beat, plus one final frame to restore the open shape.
            if (ui != null && active && (fluttering || wasFluttering)) ui.redraw_gage = true;
            wasFluttering = fluttering;
            lastUi = ui; lastActive = active;
            lastHive = hive; lastKnight = knight;
        }
        private static void Before(MeshDrawer Md, PxlLayer L, out int __state)
        {
            __state = -1;
            var layers = MTR.AGauge;
            // Foreground frame pieces only: keep the original atlas, UVs and geometry.
            if (Active && Md != null && layers != null && layers.Length > 8 &&
                (ReferenceEquals(L, layers[0]) || ReferenceEquals(L, layers[2]) || ReferenceEquals(L, layers[10]) || ReferenceEquals(L, layers[4]) || ReferenceEquals(L, layers[6]) || ReferenceEquals(L, layers[8])))
                __state = Md.getVertexMax();
        }
        private static void After(MeshDrawer Md, PxlLayer L, int __state)
        {
            if (__state < 0) return;
            int end = Md.getVertexMax();
            var colors = Md.getColorArray();
            var vertices = Md.getVertexArray();
            if (end <= __state || colors == null || vertices == null) return;
            float low = vertices[__state].y, high = low;
            for (int i = __state + 1; i < end; i++)
            { low = Mathf.Min(low, vertices[i].y); high = Mathf.Max(high, vertices[i].y); }
            Color dark = new Color(0.12f, 0.40f, 0.72f);
            Color light = new Color(0.60f, 0.91f, 1f);
            for (int i = __state; i < end; i++)
            {
                Color32 tint = Color.Lerp(dark, light, Mathf.InverseLerp(low, high, vertices[i].y));
                // Alpha comes from the native frame, including its fades.
                colors[i] = new Color32(tint.r, tint.g, tint.b, colors[i].a);
            }
            if (ReferenceEquals(L, MTR.AGauge[2]) || ReferenceEquals(L, MTR.AGauge[10]))
            {
                float left = vertices[__state].x, right = left;
                for (int i = __state + 1; i < end; i++)
                { left = Mathf.Min(left, vertices[i].x); right = Mathf.Max(right, vertices[i].x); }
                byte alpha = colors[__state].a;
                Md.Identity();
                Md.initForImg(MTRX.IconWhite, 0);
                DrawVines(Md, left * IN.ppu, right * IN.ppu, low * IN.ppu, high * IN.ppu, alpha);
            }
        }
        private static Color32 Ink(byte r, byte g, byte b, byte alpha) => new Color32(r, g, b, alpha);
        private static void Curve(MeshDrawer md, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float width, Color32 color)
        {
            md.Col = color;
            Vector2 previous = a;
            for (int i = 1; i <= 24; i++)
            {
                float t = i / 24f, u = 1f - t;
                Vector2 point = u*u*u*a + 3*u*u*t*b + 3*u*t*t*c + t*t*t*d;
                md.Line(previous.x, previous.y, point.x, point.y, width);
                previous = point;
            }
        }
        private static void Petal(MeshDrawer md, float x, float y, float rx, float ry, float angle, Color32 color)
        {
            md.Col = color;
            const int count = 20;
            int firstVertex = md.getVertexMax();
            // Native UI material culls counter-clockwise faces (MeshDrawer.Poly uses 0,next,current).
            for (int i = 0; i < count; i++) md.Tri(0).Tri((i+1)%count+1).Tri(i+1);
            md.PosD(x, y, null);
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                float px = Mathf.Cos(a)*rx, py = Mathf.Sin(a)*ry;
                md.PosD(x + px*Mathf.Cos(angle)-py*Mathf.Sin(angle),
                    y + px*Mathf.Sin(angle)+py*Mathf.Cos(angle), null);
            }
            // PosD deliberately leaves UV untouched in IMG mode. Unlike Line(), this
            // custom fan has no InputImageUv step (the native helper covers only 4 vertices).
            // Sample the center of the atlas white tile for EVERY vertex in the fan.
            Vector2 whiteUv = MTRX.IconWhite.RectIUv.center;
            Vector2[] uv = md.getUvArray();
            for (int i = firstVertex; i < md.getVertexMax(); i++) uv[i] = whiteUv;
        }
        private static void Leaf(MeshDrawer md, Vector2 center, float x, float y, float rx, float ry,
            float localAngle, float rotation, Color32 color)
        {
            float c = Mathf.Cos(rotation), sn = Mathf.Sin(rotation);
            Petal(md, center.x+x*c-y*sn, center.y+x*sn+y*c, rx, ry, localAngle+rotation, color);
        }
        private static void Lobe(MeshDrawer md, Vector2 center, float size, float rotation,
            float x, float y, float width, float height, float tilt, byte alpha, float fold)
        {
            float opening = 1f - 0.82f * fold;
            x *= opening; width *= opening; tilt *= opening;
            Leaf(md,center,x*size,y*size,width*size,height*size,tilt,rotation,Ink(18,75,116,alpha));
            Leaf(md,center,x*size,(y+0.07f)*size,width*size*0.78f,height*size*0.87f,tilt,rotation,Ink(92,214,244,alpha));
            Leaf(md,center,(x*0.88f)*size,(y-0.1f)*size,width*size*0.30f,height*size*0.60f,tilt,rotation,Ink(203,254,255,alpha));
        }
        private static void Bloom(MeshDrawer md, Vector2 center, float size, float rotation, int variant, byte alpha)
        {
            float fold = LeafFlutter.Amount(flutterClock, variant);
            // Ten distinct silhouettes, rather than one butterfly with transformed copies.
            switch (variant)
            {
                case 0: // A narrow, closed young bud.
                    Lobe(md,center,size,rotation,-0.14f,0.9f,0.27f,1.12f,0.12f,alpha,fold);
                    Lobe(md,center,size,rotation,0.24f,0.48f,0.22f,0.62f,-0.25f,alpha,fold); break;
                case 1: // Large side-facing wing and a small upright companion.
                    Lobe(md,center,size,rotation,-0.68f,0.47f,0.46f,0.98f,1.04f,alpha,fold);
                    Lobe(md,center,size,rotation,0.25f,0.78f,0.33f,0.87f,-0.30f,alpha,fold); break;
                case 2: // Full four-lobed butterfly.
                    Lobe(md,center,size,rotation,-0.62f,0.75f,0.55f,1.12f,0.68f,alpha,fold);
                    Lobe(md,center,size,rotation,0.58f,0.96f,0.46f,1.31f,-0.45f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.3f,-0.16f,0.32f,0.46f,-0.55f,alpha,fold);
                    Lobe(md,center,size,rotation,0.32f,-0.11f,0.32f,0.40f,0.68f,alpha,fold); break;
                case 3: // Three narrow blades in an open fan.
                    Lobe(md,center,size,rotation,-0.64f,0.48f,0.24f,1.07f,0.99f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.05f,0.95f,0.26f,1.22f,0.1f,alpha,fold);
                    Lobe(md,center,size,rotation,0.55f,0.55f,0.25f,0.91f,-0.87f,alpha,fold); break;
                case 4: // One long drooping leaf, with a tiny basal leaf.
                    Lobe(md,center,size,rotation,0.73f,0.48f,0.39f,1.43f,-1.12f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.20f,0.16f,0.22f,0.37f,0.58f,alpha,fold); break;
                case 5: // Broad low wings and a trailing lower lobe.
                    Lobe(md,center,size,rotation,-0.65f,0.38f,0.49f,0.83f,1.24f,alpha,fold);
                    Lobe(md,center,size,rotation,0.71f,0.41f,0.44f,1.02f,-1.19f,alpha,fold);
                    Lobe(md,center,size,rotation,0.12f,-0.28f,0.25f,0.51f,0.22f,alpha,fold); break;
                case 6: // Single round bud on two short sepals.
                    Lobe(md,center,size,rotation,0.04f,0.75f,0.56f,0.79f,-0.19f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.27f,0.0f,0.19f,0.32f,-0.72f,alpha,fold);
                    Lobe(md,center,size,rotation,0.29f,-0.01f,0.17f,0.28f,0.8f,alpha,fold); break;
                case 7: // An uneven three-lobed cluster.
                    Lobe(md,center,size,rotation,-0.46f,1.02f,0.33f,1.47f,0.32f,alpha,fold);
                    Lobe(md,center,size,rotation,0.53f,0.64f,0.48f,0.95f,-0.83f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.45f,0.06f,0.28f,0.40f,1.18f,alpha,fold); break;
                case 8: // Short, rounded butterfly with a deep notch.
                    Lobe(md,center,size,rotation,-0.64f,0.68f,0.57f,0.78f,0.72f,alpha,fold);
                    Lobe(md,center,size,rotation,0.68f,0.62f,0.57f,0.76f,-0.9f,alpha,fold);
                    Lobe(md,center,size,rotation,-0.27f,-0.1f,0.23f,0.31f,-0.45f,alpha,fold);
                    Lobe(md,center,size,rotation,0.30f,-0.15f,0.27f,0.39f,0.48f,alpha,fold); break;
                default: // Two long forked blades and an off-center lower leaf.
                    Lobe(md,center,size,rotation,-0.38f,1.04f,0.25f,1.41f,0.29f,alpha,fold);
                    Lobe(md,center,size,rotation,0.60f,0.74f,0.24f,1.17f,-0.63f,alpha,fold);
                    Lobe(md,center,size,rotation,0.22f,0.03f,0.22f,0.33f,0.94f,alpha,fold); break;
            }
        }
        private static void Vine(MeshDrawer md, Vector2 a, Vector2 b, Vector2 c, Vector2 d, byte alpha)
        {
            Curve(md,a,b,c,d,1.2f,Ink(14,48,86,alpha));
            Vector2 light = new Vector2(0.12f,0.22f);
            Curve(md,a+light,b+light,c+light,d+light,0.38f,Ink(51,127,180,alpha));
        }
        private static void DrawVines(MeshDrawer md, float left, float right, float bottom, float top, byte alpha)
        {
            Color32 old = md.Col;
            float width = right-left;
            bool hp = (left+right)*0.5f < 0f;
            // Stagger the two frames. Stable variations avoid flicker on native redraws.
            float phase = hp ? 0f : 0.65f;
            for (int edge = 0; edge < 2; edge++)
            {
                float y = edge == 0 ? top+0.6f : bottom-0.6f;
                float bend = edge == 0 ? 1.8f : -1.8f;
                for (int n = 0; n < 5; n++)
                {
                    float x0 = left+width*n/5f, x1 = left+width*(n+1)/5f;
                    Vine(md,new Vector2(x0,y),new Vector2(x0+(x1-x0)*0.33f,y+bend),
                        new Vector2(x1-(x1-x0)*0.33f,y-bend),new Vector2(x1,y),alpha);
                }
            }
            // Diagonal front turns and curved outer hooks read as a vine wound around the frame.
            for (int n = 0; n < 5; n++)
            {
                float x = left+width*(0.065f+n*0.19f+(hp?0f:0.025f));
                float slant = n%2 == 0 ? 3.8f : -3f;
                Vine(md,new Vector2(x, bottom-0.8f),new Vector2(x-slant,bottom-2.4f),
                    new Vector2(x+slant,top+2.5f),new Vector2(x+slant*1.2f,top+0.7f),alpha);
            }
            for (int n = 0; n < 5; n++)
            {
                bool below = n == 1 || n == 4;
                // Lower leaves stay at the outer side, clear of the central number plaques.
                float fraction = below ? (hp ? (n==1?0.08f:0.40f) : (n==1?0.60f:0.93f)) :
                    (n==0?0.17f:n==2?0.53f:0.87f)+(hp?0f:0.025f);
                float outward = below ? -1f : 1f;
                float x = left+width*fraction;
                float bend = Mathf.Sin(n*2.1f+phase)*4.5f;
                float length = 3.8f+(n%3)*1.6f;
                Vector2 root = new Vector2(x,below?bottom-0.6f:top+0.6f);
                Vector2 tip = root+new Vector2(bend,outward*length);
                Vine(md,root,root+new Vector2(-bend*0.8f,outward*1.5f),
                    tip-new Vector2(bend*0.65f,outward*2f),tip,alpha);
                float rotation = (below?Mathf.PI:0f)+Mathf.Sin(n*1.9f+phase)*0.75f;
                float size = 1.65f+(n%3)*0.53f+(hp?0.1f:0f);
                Bloom(md,tip,size,rotation,hp ? n : n+5,alpha);
            }
            md.Col = old;
        }
    }
}
