using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace BetterStarmap
{
    class DetailsPreview_Impl : BaseFeature<DetailsPreview_Impl>, IFeature
    {
        private static PlanetData s_HoveredPlanet = null;
        private static StarData s_HoveredStar = null;

        public DetailsPreview_Impl()
        {
            SetFeatureName( "DetailsPreview" );
        }

        public void DrawGui()
        {
            isEnable = GUILayout.Toggle(isEnable, "星球细节预览".ModText());
        }

        static void Reset()
        {
            s_HoveredStar = (StarData)null;
            s_HoveredPlanet = (PlanetData)null;
        }

        static void SetHover(StarData star)
        {
            s_HoveredStar = star;
            s_HoveredPlanet = (PlanetData)null;
        }

        static void SetHover(PlanetData planet)
        {
            s_HoveredPlanet = planet;
            s_HoveredStar = (StarData)null;
        }

        static bool IsHoverStar => s_HoveredStar != null;
        static bool IsHoverPlanet => s_HoveredPlanet != null;

        public static void OnMouseHover(UIStarmap __instance)
        {
            if (isEnable)
                if (__instance.mouseHoverStar)
                    SetHover( __instance.mouseHoverStar.star );
                else if (__instance.mouseHoverPlanet)
                    SetHover(__instance.mouseHoverPlanet.planet);
            else
                Reset();
        }

        public static void OnStarmapClose()
        {
            Reset();
        }

        public static void OnSetPlanetDetail(ref PlanetData planet)
        {
            if (IsHoverStar)
            {
                planet = (PlanetData)null;
            }
            else if (IsHoverPlanet)
            {
                planet = s_HoveredPlanet;
                s_HoveredStar = (StarData)null;
            }
            else if (planet != null)
            {
                s_HoveredStar = (StarData)null;
            }
        }

        public static void OnSetStarDetail(ref StarData star)
        {
            if (IsHoverPlanet)
            {
                star = (StarData)null;
            }
            else if (IsHoverStar)
            {
                s_HoveredPlanet = (PlanetData)null;
                star = s_HoveredStar;
            }
            else if (star != null)
            {
                s_HoveredPlanet = (PlanetData)null;
            }
        }
    }

    class ImmediateMode_Impl : BaseFeature<ImmediateMode_Impl>, IFeature
    {
        public ImmediateMode_Impl(ConfigEntry<bool> enable)
        {
            SetFeatureName("ImmediateMode");
            configEnable = enable;
        }
        public void DrawGui()
        {
            if (configEnable.Value)
                isEnable = GUILayout.Toggle(isEnable, "查看立即模式".ModText());
        }
    }

    class DisplayStarName_Impl : BaseFeature<DisplayStarName_Impl>, IFeature
    {
        public DisplayStarName_Impl()
        {
            SetFeatureName("DisplayStarName");
        }
        public void DrawGui()
        {
             isEnable = GUILayout.Toggle(isEnable, "显示星球名称".ModText());
        }

        public static void OriginalSetTextActive(UIStarmapStar __instance, ref Text nameText)
        {
            GameHistoryData historyData = Traverse.Create((object)__instance).Field("gameHistory").GetValue<GameHistoryData>();

            Vector2 rectPoint = Vector2.zero;
            bool flag = __instance.starmap.WorldPointIntoScreen(__instance.starObject.vpos, out rectPoint) && ((UnityEngine.Object)__instance.starmap.mouseHoverStar == (UnityEngine.Object)__instance || historyData.HasFeatureKey(1001001) || historyData.HasFeatureKey(1010000 + __instance.star.id));

            float num = Mathf.Max(1f, __instance.starObject.vdist / __instance.starObject.vscale.x);
            __instance.projectedCoord = rectPoint;
            rectPoint.x += (float)(8.0f + 600f / (float)num);
            rectPoint.y += 4.0f;
            nameText.rectTransform.anchoredPosition = rectPoint;

            __instance.projected = true;
            nameText.gameObject.SetActive(true);
        }
    }

    class DisplayUnknown_Impl : BaseFeature<DisplayUnknown_Impl>, IFeature
    {
        public static int historyUniverseObserveLevel = -1;

        public DisplayUnknown_Impl(ConfigEntry<bool> enable)
        {
            SetFeatureName("DisplayUnknown");
            configEnable = enable;
        }
        public void DrawGui()
        {
            if (configEnable.Value)
                isEnable = GUILayout.Toggle(isEnable, "探测未知信息".ModText());

            CheckDisplayUnknown();
        }

        private void CheckDisplayUnknown()
        {
            if (configEnable.Value)
            {
                if (historyUniverseObserveLevel == -1)
                    historyUniverseObserveLevel = GameMain.history.universeObserveLevel;
                if (isEnable)
                    GameMain.history.universeObserveLevel = 4;
                else
                    GameMain.history.universeObserveLevel = historyUniverseObserveLevel;
            }
        }

        public static void OnStarmapClose()
        {
            if (configEnable.Value)
            {
                if(historyUniverseObserveLevel != -1)
                    GameMain.history.universeObserveLevel = historyUniverseObserveLevel;
                DisplayUnknown_Impl.historyUniverseObserveLevel = -1;
                DisplayUnknown_Impl.isEnable = false;
            }
        }

        public static void OnTechUnlock(ref int func, ref double value, ref int level)
        {
            if (configEnable.Value)
            {
                int num = value <= 0.0 ? (int)(value - 0.5f) : (int)(value + 0.5f);
                if (func == 23)
                    historyUniverseObserveLevel = num;
            }
        }
    }

    class StarHighlight_Impl : BaseFeature<DisplayStarName_Impl>, IFeature
    {
        public interface IStarHighlight
        {
            void DrawGui();
            void SetStarColor(UIStarmapStar __instance, ref Text nameText);
        }

        Queue<IStarHighlight> hignLightQueue = new Queue<IStarHighlight>();

        public void AddFeature(IStarHighlight feature)
        {
            hignLightQueue.Enqueue(feature);
        }

        public StarHighlight_Impl()
        {
            SetFeatureName("StarHighlight");
        }
        public void DrawGui()
        {
            foreach(var feature in hignLightQueue)
            {
                feature.DrawGui();
            }
        }

        public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
        {
            foreach (var feature in hignLightQueue)
            {
                feature.SetStarColor(__instance,ref nameText);
            }
        }

        public class HighLuminosity_Impl : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "高光度恒星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && __instance.star.dysonLumino > 2.0f)
                    nameText.color = Color.magenta;
            }
        }

        public class Blackhole_Impl : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "黑洞中子星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.BlackHole || __instance.star.type == EStarType.NeutronStar))
                    nameText.color = Color.green;
            }
        }

        public class GiantStar_Impl : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "巨星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.GiantStar))
                    nameText.color = Color.green;
            }
        }

        public class WhiteDwarf_Impl : IStarHighlight
        {
            bool enable = false;

            public void DrawGui()
            {
                enable = GUILayout.Toggle(enable, "白矮星".ModText());
            }

            public void SetStarColor(UIStarmapStar __instance, ref Text nameText)
            {
                if (enable && (__instance.star.type == EStarType.WhiteDwarf))
                    nameText.color = Color.green;
            }
        }
    }

    [BepInPlugin(__GUID__, __NAME__, "1.1.2")]
    public class BetterStarmap : BaseUnityPlugin
    {
        public const string __NAME__ = "betterstarmap";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;
        
        static bool g_IsStarMapOpened = false;

        public static ConfigEntry<float> g_DisplayPositionX;
        public static ConfigEntry<float> g_DisplayPositionY;

        static FeaturesManage g_MainFeatures = new FeaturesManage();    
        static StarHighlight_Impl g_StarHighLight = new StarHighlight_Impl();

        void Start()
        {
            //Add Features
            g_MainFeatures.AddFeatrue( new DetailsPreview_Impl() );
            g_MainFeatures.AddFeatrue( new ImmediateMode_Impl(Config.Bind<bool>("config", "ImmediateMode", true, "是否开启查看立即模式功能")) );
            g_MainFeatures.AddFeatrue( new DisplayStarName_Impl() );
            g_MainFeatures.AddFeatrue( new DisplayUnknown_Impl(Config.Bind<bool>("config", "DisplayUnknown", true, "是否开启探测未知信息功能")) );

            //Star HighLight
            g_StarHighLight.AddFeature(new StarHighlight_Impl.HighLuminosity_Impl());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.Blackhole_Impl());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.GiantStar_Impl());
            g_StarHighLight.AddFeature(new StarHighlight_Impl.WhiteDwarf_Impl());

            //Get Display Position
            g_DisplayPositionX = Config.Bind<float>("config", "DisplayPositionX", 0.01f, "UI显示位置X");
            g_DisplayPositionY = Config.Bind<float>("config", "DisplayPositionY", 0.7f, "UI显示位置Y");

            new Harmony(__GUID__).PatchAll();
        }
        
        private void OnGUI()
        {
            if (g_IsStarMapOpened)
            {
                GUILayout.BeginArea(new Rect(Screen.width * g_DisplayPositionX.Value, Screen.height * g_DisplayPositionY.Value, 200, 300));

                GUILayout.Label("星图功能".ModText());
                foreach(var feature in g_MainFeatures.features)
                {
                    feature.Value.DrawGui();
                }

                GUILayout.Label("星系显示".ModText());
                g_StarHighLight.DrawGui();

                GUILayout.EndArea();
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "OnStarClick")]
        private class ImmediateMode
        {
            private static void Postfix(UIStarmap __instance, ref UIStarmapStar star)
            {
                if (ImmediateMode_Impl.isEnable)
                {
                    if (__instance.viewStar == star.star)
                        return;
                    __instance.screenCameraController.SetViewTarget((PlanetData)null, star.star, (Player)null, VectorLF3.zero, (double)star.star.physicsRadius * 0.899999976158142 * 0.00025, (double)star.star.physicsRadius * (double)Mathf.Pow(star.star.radius * 0.4f, -0.4f) * 3.0 * 0.00025, true, true);
                }
            }
        }

        [HarmonyPatch(typeof(UIStarmap), "_OnClose")]
        private class SafeClose
        {
            private static void Prefix(UIStarmap __instance)
            {
                g_IsStarMapOpened = false;
                DetailsPreview_Impl.OnStarmapClose();
                DisplayUnknown_Impl.OnStarmapClose();
            }
        }

        public static class DetailsPreview
        {
            [HarmonyPatch(typeof(UIGame), "SetPlanetDetail")]
            private class Planet
            {
                private static void Prefix(UIGame __instance, ref PlanetData planet)
                {
                    if (__instance.starmap.isFullOpened)
                    {
                        DetailsPreview_Impl.OnSetPlanetDetail(ref planet);
                    }
                }
            }

            [HarmonyPatch(typeof(UIGame), "SetStarDetail")]
            private class Star
            {
                private static void Prefix(UIGame __instance, ref StarData star)
                {
                    if (__instance.starmap.isFullOpened)
                    {
                        DetailsPreview_Impl.OnSetStarDetail(ref star);
                    }
                }
            }

            [HarmonyPatch(typeof(UIStarmap), "MouseHoverCheck")]
            private class MouseHoverCheck
            {
                public static void Postfix(UIStarmap __instance)
                {
                    g_IsStarMapOpened = __instance.isFullOpened;
                    DetailsPreview_Impl.OnMouseHover(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(UIStarmapStar), "_OnLateUpdate")]
        private class StarmapStarHighlight
        {
            /// <summary>
            /// Star color default to white
            /// </summary>
            private static void Prefix(UIStarmapStar __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
                nameText.color = Color.white;
            }

            private static void Postfix(UIStarmapStar __instance)
            {
                if (DisplayStarName_Impl.isEnable)
                {
                    Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();

                    DisplayStarName_Impl.OriginalSetTextActive(__instance, ref nameText);
                    g_StarHighLight.SetStarColor(__instance, ref nameText);
                }

            }
        }

        /// <summary>
        /// We need update history tech level when unlock tech
        /// </summary>
        [HarmonyPatch(typeof(GameHistoryData), "UnlockTechFunction")]
        private class GameHistoryDataUnlockTechFunction
        {
            private static void Prefix(ref int func, ref double value, ref int level)
            {
                DisplayUnknown_Impl.OnTechUnlock(ref func,ref value,ref level);
            }
        }
        

    }
}