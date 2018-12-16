using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;

namespace TheThirdAge
{
    /// <summary>
    /// Original code by Xen https://github.com/XenEmpireAdmin
    /// Adjustments by Jecrell https://github.com/Jecrell
    /// </summary>
    [StaticConstructorOnStartup]
    public static class MainMenuTex
    {
        static bool debug = false;

        public static Texture2D BGMain;

        static MainMenuTex()
        {
            LoadTextures();
        }

        private static void LoadTextures()
        {
            BGMain = ContentFinder<Texture2D>.Get("UI/HeroArt/TTABGPlanet", true);

            try
            {
                Traverse.CreateWithType("UI_BackgroundMain").Field("BGPlanet").SetValue(BGMain);
            }
            catch
            {
                if (debug) Log.Message("Failed to Traverse BGPlanet");
            }
        }
    }
    [StaticConstructorOnStartup]
    internal static class SwapMainMenuGraphics
    {
        static bool debug = false;

        static SwapMainMenuGraphics()
        {
            HarmonyInstance UI_BackgroundMainPatch = HarmonyInstance.Create("TTA.MainMenu.UI_BackgroundMainPatch");
            MethodInfo methInfBackgroundOnGUI = AccessTools.Method(typeof(UI_BackgroundMain), "BackgroundOnGUI", null, null);
            HarmonyMethod harmonyMethodPreFBackgroundOnGUI = new HarmonyMethod(typeof(SwapMainMenuGraphics).GetMethod("PreFBackgroundOnGUI"));
            UI_BackgroundMainPatch.Patch(methInfBackgroundOnGUI, harmonyMethodPreFBackgroundOnGUI, null, null);
            if (debug) Log.Message("UI_BackgroundMainPatch initialized");
        }
        public static bool PreFBackgroundOnGUI()
        {
            // Shape the BG
            float floRatio = UI.screenWidth / 2048f;
            float floHeight = 1280f * floRatio;
            float floYPos = (UI.screenHeight - floHeight) / 2f;
            Rect rectBG = new Rect(0f, floYPos, UI.screenWidth, floHeight);

            // Draw the BG
            GUI.DrawTexture(rectBG, Traverse.Create(typeof(UI_BackgroundMain)).Field("BGPlanet").GetValue<Texture2D>(), ScaleMode.ScaleToFit);

            return false;
        }
    }
}
