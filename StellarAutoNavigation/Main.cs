using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;

namespace AutoNavigate
{
    [BepInPlugin(__GUID__, __NAME__, "1.0")]
    public class AutoNavigate : BaseUnityPlugin
    {
        public const string __NAME__ = "StellarAutoNavigation";
        public const string __GUID__ = "0x.plugins.dsp." + __NAME__;

        static public AutoNavigate self;
        private Player player = null;

        public static StarmapNavPin navPin;
        public static AutoStellarNavigation autoNav;

        public static bool isHistoryNav = false;
        public static ConfigEntry<double> minAutoNavEnergy;

        void Start()
        {         
            navPin = new StarmapNavPin();
            autoNav = new AutoStellarNavigation(GetNavConfig());

            self = this;
            new Harmony(__GUID__).PatchAll();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.K) && player != null)
            {
                autoNav.ToggleNav();
            }
        }

        private AutoStellarNavigation.NavigationConfig GetNavConfig()
        {
            AutoStellarNavigation.NavigationConfig navConfig = new AutoStellarNavigation.NavigationConfig();

            minAutoNavEnergy = Config.Bind<double>("AutoStellarNavigation", "minAutoNavEnergy", 50000000.0, "开启自动导航最低能量(最低50m)");
            navConfig.speedUpEnergylimit = Config.Bind<double>("AutoStellarNavigation", "SpeedUpEnergylimit", 50000000.0, "开启加速最低能量(默认50m)");
            navConfig.wrapEnergylimit = Config.Bind<double>("AutoStellarNavigation", "WrapEnergylimit", 800000000, "开启曲率最低能量(默认800m)");
            navConfig.enableLocalWrap = Config.Bind<bool>("AutoStellarNavigation", "EnableLocalWrap", true, "是否开启本地行星曲率飞行");
            navConfig.localWrapMinDistance = Config.Bind<double>("AutoStellarNavigation", "LocalWrapMinDistance", 100000.0, "本地行星曲率飞行最短距离");

            if (minAutoNavEnergy.Value < 50000000.0)
                minAutoNavEnergy.Value = 50000000.0;

            return navConfig;
        }

        class SafeMod
        {
            public static void ResetMod()
            {
                isHistoryNav = false;
                navPin.Reset();
                autoNav.Reset();
                autoNav.target.Reset();               
                self.player = null;
            }

            [HarmonyPatch(typeof(GameMain), "OnDestroy")]
            public class SafeDestroy
            {
                private static void Prefix()
                {
                    ResetMod();
                }
            }

            [HarmonyPatch(typeof(GameMain), "Pause")]
            public class SafePause
            {
                public static void Prefix()
                {
                    autoNav.pause();
                }
            }

            [HarmonyPatch(typeof(GameMain), "Resume")]
            public class SafeResume
            {
                public static void Prefix()
                {
                    autoNav.resume();
                }
            }
        }


        [HarmonyPatch(typeof(PlayerController), "Init")]
        private class PlayerControllerInit
        {
            private static void Postfix(PlayerController __instance)
            {
                self.player = __instance.player;
            }
        }
/// --------------------------
/// AutoStellarNavigation
/// --------------------------
        [HarmonyPatch(typeof(UIGeneralTips), "_OnUpdate")]
        private class NavigateTips
        {
            static Vector2 anchoredPosition = new Vector2(0.0f, 160.0f);

            public static void Postfix(UIGeneralTips __instance)
            {
                Text modeText = Traverse.Create((object)__instance).Field("modeText").GetValue<Text>();
                if (autoNav.enable)
                {
                    modeText.gameObject.SetActive(true);
                    modeText.rectTransform.anchoredPosition = anchoredPosition;

                    if (autoNav.IsCurNavPlanet())
                        modeText.text = ModText.StellarAutoNavigation;
                    else if (autoNav.IsCurNavStar())
                        modeText.text = ModText.GalaxyAutoNavigation;

                    autoNav.modeText = modeText;
                }

            }
        }

        [HarmonyPatch(typeof(VFInput), "_sailSpeedUp", MethodType.Getter)]
        private class SailSpeedUp
        {
            private static void Postfix(ref bool __result)
            {
                if (autoNav.enable && autoNav.sailSpeedUp)
                {
                    __result = true;
                }                 
            }
        }

        [HarmonyPatch(typeof(PlayerMove_Sail), "GameTick")]
        private class SailMode_AutoNavigate
        {
            private static Quaternion oTargetURot;

            private static void Prefix(PlayerMove_Sail __instance)
            {
                if (autoNav.enable && (__instance.player.sailing || __instance.player.warping))
                {
                    ++__instance.controller.input0.y;
                    oTargetURot = __instance.sailPoser.targetURot;

                    if (autoNav.IsCurNavStar())
                    {
                       autoNav.StarNavigation(__instance);
                       
                    }
                    else if (autoNav.IsCurNavPlanet() )
                    {
                        autoNav.PlanetNavigation(__instance);
                    }
                }
            }

            private static void Postfix(PlayerMove_Sail __instance)
            {
                if (autoNav.enable && (GameMain.localPlanet != null || autoNav.target.IsVaild() ))
                {
                    __instance.sailPoser.targetURot = oTargetURot;
                    autoNav.HandlePlayerInput();
                }

                autoNav.sailSpeedUp = false;
            }
        }

        [HarmonyPatch(typeof(PlayerMove_Fly), "GameTick")]
        private class FlyMode_TrySwtichToSail
        {
            static float sailMinAltitude = 49.0f;

            private static void Prefix(PlayerMove_Fly __instance)
            {
                if (autoNav.enable)
                {
                    if (__instance.player.movementState != EMovementState.Fly)
                        return;

                    if (autoNav.DetermineArrive())
                    {
#if DEBUG
                        ModDebug.Log("FlyModeArrive");
#endif
                        autoNav.Arrive();

                    }
                    else if (
                        __instance.mecha.thrusterLevel < 2)
                    {
                        autoNav.Arrive(ModText.ThrusterLevelTooLow);
                    }
                    else if (__instance.player.mecha.coreEnergy < minAutoNavEnergy.Value)
                    {
                        autoNav.Arrive(ModText.MechaEnergyTooLow);
                    }
                    else
                    {
                        ++__instance.controller.input1.y;

                        if (__instance.currentAltitude > sailMinAltitude)
                        {
                            AutoStellarNavigation.Fly.TrySwtichToSail(__instance);
                        }
                    }
                }

            }
        }

        [HarmonyPatch(typeof(PlayerMove_Walk), "UpdateJump")]
        private class WalkMode_TrySwticToFly
        {
            private static void Postfix(PlayerMove_Walk __instance, ref bool __result)
            {

                if (autoNav.enable && autoNav.target.IsVaild())
                {
                    if (autoNav.DetermineArrive())
                    {
#if DEBUG
                        ModDebug.Log("WalkModeArrive");
#endif
                        autoNav.Arrive();
                    }
                    else if (
                        __instance.mecha.thrusterLevel < 1)
                    {
                        autoNav.Arrive(ModText.ThrusterLevelTooLow);
                    }
                    else if (__instance.player.mecha.coreEnergy < minAutoNavEnergy.Value)
                    {
                        autoNav.Arrive(ModText.MechaEnergyTooLow);
                    }
                    else
                    {
                        AutoStellarNavigation.Walk.TrySwitchToFly(__instance);
                        __result = true;
                        return;

                    }

                    __result = false;
                }

            }

        }

/// --------------------------
/// StarmapPin
/// --------------------------

        [HarmonyPatch(typeof(UIStarmap), "OnScreenClick")]
        private class UIStarmapOnMouseClick
        {
            private static void Prefix(UIStarmap __instance, ref BaseEventData evtData)
            {
                if (!(evtData is PointerEventData pointerEventData) || pointerEventData.button != PointerEventData.InputButton.Left && pointerEventData.button != PointerEventData.InputButton.Right)
                    return;

                if ((UnityEngine.Object)__instance.mouseHoverPlanet != (UnityEngine.Object)null)
                {
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                       // ModDebug.Log("InputButton.Right == mouseHoverPlanet");

                        SetPlanetPin(__instance);
                        if (navPin.target.planet != null)
                            autoNav.target.SetTarget(__instance.mouseHoverPlanet.planet);
                        return;
                    }

                    __instance.OnPlanetClick(__instance.mouseHoverPlanet);
                }
                else
                {
                    if (!((UnityEngine.Object)__instance.mouseHoverStar != (UnityEngine.Object)null))
                        return;
                    if (pointerEventData.button == PointerEventData.InputButton.Right)
                    {
                        //ModDebug.Log("InputButton.Right == mouseHoverStar");

                        SetStarPin(__instance);
                        if(navPin.target.star != null)
                            autoNav.target.SetTarget(__instance.mouseHoverStar);
                        return;
                    }

                    __instance.OnStarClick(__instance.mouseHoverStar);
                }

                return;
            }

            public static void SetPlanetPin(UIStarmap __instance)
            {
                navPin.RecoverName();
                if (__instance.mouseHoverPlanet.planet.id == navPin.target.id)
                {
                    //ModDebug.Log("mouseHoverPlanet == navPin.target");                   
                    navPin.Reset();
                    autoNav.Arrive();
                }
                else
                {
                    navPin.SetPin(__instance.mouseHoverPlanet, __instance.mouseHoverPlanet.planet);
                    StarmapNavPin.SetPlanetName(__instance, navPin.target.name);
                }
            }

            
            public static void SetStarPin(UIStarmap __instance)
            {
                void SetName()
                {
                    navPin.RecoverName();

                    if (__instance.mouseHoverStar.star.id == navPin.target.id)
                    {
                        //ModDebug.Log("mouseHoverStar == navPin.target");                      
                        navPin.Reset();
                        autoNav.Arrive();
                    }
                    else
                    {
                        navPin.SetPin(__instance.mouseHoverStar);            
                        StarmapNavPin.SetStarName(__instance, navPin.target.name);
                    }
                }

                void SetPin()
                {
                    GameHistoryData mouseHoverStar_gameHistory = Traverse.Create((object)__instance.mouseHoverStar).Field("gameHistory").GetValue<GameHistoryData>();
                    UISpaceGuide spaceGuide = UIRoot.instance.uiGame.spaceGuide;
                    if (spaceGuide != null)
                    {
                        if (navPin.isPined == false)
                        {
                            if (mouseHoverStar_gameHistory.HasFeatureKey(1001001) ||
                                mouseHoverStar_gameHistory.HasFeatureKey(1010000 + __instance.mouseHoverStar.star.id))
                            {
                                //ModDebug.Log("Star Alread Pinned");
                                navPin.alreadyPin = true;
                            }                            
                            else
                            {
                                //ModDebug.Log("Star Alread No Pinned");
                                navPin.alreadyPin = false;
                            }                              

                            spaceGuide.SetStarPin(__instance.mouseHoverStar.star.id, true);
                            navPin.isPined = true;
                        }
                        else
                        {
                            if (!navPin.alreadyPin)
                            {
                                //ModDebug.Log("navPin.alreadyPin == false");
                                spaceGuide.SetStarPin(navPin.target.id, false);                                
                            }                              

                            navPin.isPined = false;
                            navPin.alreadyPin = false;
                        }
                    }
                      
                }

                SetPin();
                SetName();                             

            }
        }

        [HarmonyPatch(typeof(UISpaceGuideEntry), "_OnLateUpdate")]
        private class Space_PinStarPlanetColorScale
        {
            static Vector3 lScale = new Vector3(2.2f, 1.0f, 1.0f);

            private static void Postfix(UISpaceGuideEntry __instance)
            {
                Image image = Traverse.Create((object)__instance).Field("markIcon").GetValue<Image>();
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();

                if (navPin.target.id != -1 &&
                    __instance.objId == navPin.target.id &&
                    (__instance.guideType == ESpaceGuideType.Star || __instance.guideType == ESpaceGuideType.Planet))
                {
                    image.rectTransform.localScale = lScale;
                    image.color = Color.red;
                    nameText.color = Color.red;
                }
                else
                {
                    image.rectTransform.localScale = Vector3.one;
                    nameText.color = Color.white;
                }

            }
        }

        [HarmonyPatch(typeof(UIStarmapStar), "_OnLateUpdate")]
        private class Starmap_PinStarColor
        {
            private static void Prefix(UIStarmapStar __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
                nameText.color = Color.white;
            }

            private static void Postfix(UIStarmapStar __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();

                if (navPin.target != null && 
                    navPin.target.id != -1 && 
                    __instance.star.id == navPin.target.id
                    )
                    nameText.color = Color.red;
            }
        }

        [HarmonyPatch(typeof(UIStarmapPlanet), "_OnLateUpdate")]
        private class Starmap_PinPlanetColor
        {
            private static void Prefix(UIStarmapPlanet __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
                nameText.color = Color.white;
            }

            private static void Postfix(UIStarmapPlanet __instance)
            {
                Text nameText = Traverse.Create((object)__instance).Field("nameText").GetValue<Text>();
               
                if (navPin.target != null && 
                    navPin.target.id != -1 &&                 
                    __instance.planet.id == navPin.target.id
                    )
                    nameText.color = Color.red;
            }
        }


    }
}