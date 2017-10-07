/*
 * TSTMenu.cs
 * (C) Copyright 2015, Jamie Leighton
 * Tarsier Space Technologies
 * The original code and concept of TarsierSpaceTech rights go to Tobyb121 on the Kerbal Space Program Forums, which was covered by the MIT license.
 * Original License is here: https://github.com/JPLRepo/TarsierSpaceTechnology/blob/master/LICENSE
 * As such this code continues to be covered by MIT license.
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  This file is part of TarsierSpaceTech.
 *
 *  TarsierSpaceTech is free software: you can redistribute it and/or modify
 *  it under the terms of the MIT License 
 *
 *  TarsierSpaceTech is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  
 *
 *  You should have received a copy of the MIT License
 *  along with TarsierSpaceTech.  If not, see <http://opensource.org/licenses/MIT>.
 *
 */

using System.Collections.Generic;
using KSP.UI.Screens;
using RSTUtils;
using RSTUtils.Extensions;
using UnityEngine;
using Random = System.Random;
using KSP.Localization;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class TSTMenu : MonoBehaviour
    {
        public static TSTMenu Instance;
        //GUI Properties 
        internal AppLauncherToolBar TSTMenuAppLToolBar;
        private static int TSTwindowID = new Random().Next();
        private static int FTTwindowID;
        private const float FWINDOW_WIDTH = 480;        
        private const float WINDOW_BASE_HEIGHT = 380;
        private Rect FwindowPos = new Rect(40, Screen.height / 2 - 100, FWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private Rect FTwindowPos = new Rect(60 + FWINDOW_WIDTH, Screen.height / 2 - 100, FWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private GUIStyle sectionTitleStyle, subsystemButtonStyle, statusStyle, warningStyle, PartListStyle, PartListPartStyle;
        private GUIStyle resizeStyle; //scrollStyle,
        private Vector2 CamscrollViewVector = Vector2.zero;
        private Vector2 SDDscrollViewVector = Vector2.zero;
        private bool mouseDown;
        private bool stylesSet;
        private bool LoadConfig = true;
        private string tmpToolTip;
        
        //GuiVisibility
        
        private bool _FTVisible;
        private enum TSTWindow
        {
            FLIGHT,
            SETTINGS
        }
        private TSTWindow crntWindow;
        private bool _onGameSceneSwitchRequested;

        //TST Parts
        private List<TSTChemCam> tstchemcam = new List<TSTChemCam>();
        private List<TSTSpaceTelescope> tstSpaceTel = new List<TSTSpaceTelescope>();
        private List<TSTScienceHardDrive> tstSDD = new List<TSTScienceHardDrive>();
        //private List<TSTGyroReactionWheel> tstGyroReactionWheel = new List<TSTGyroReactionWheel>();        
        private int FTTelIndex;
        
        private bool RT2Present;
        private bool RT2Enabled;
        private bool RT2VesselConnected;
        private double RT2VesselDelay;

        #region String Caches

        private static string cacheautoLOC_TST_0043;
        private static string cacheautoLOC_TST_0065;
        private static string cacheautoLOC_TST_0067;
        private static string cacheautoLOC_TST_0084;
        private static string cacheautoLOC_TST_0236;
        private static string cacheautoLOC_TST_0237;
        private static string cacheautoLOC_TST_0238;
        private static string cacheautoLOC_TST_0239;
        private static string cacheautoLOC_TST_0240;
        private static string cacheautoLOC_TST_0241;
        private static string cacheautoLOC_TST_0242;
        private static string cacheautoLOC_TST_0243;
        private static string cacheautoLOC_TST_0244;
        private static string cacheautoLOC_TST_0245;
        private static string cacheautoLOC_TST_0246;
        private static string cacheautoLOC_TST_0247;
        private static string cacheautoLOC_TST_0248;
        private static string cacheautoLOC_TST_0249;
        private static string cacheautoLOC_TST_0250;
        private static string cacheautoLOC_TST_0251;
        private static string cacheautoLOC_TST_0252;
        private static string cacheautoLOC_TST_0253;
        private static string cacheautoLOC_TST_0254;
        private static string cacheautoLOC_TST_0255;
        private static string cacheautoLOC_TST_0256;
        private static string cacheautoLOC_TST_0257;
        private static string cacheautoLOC_TST_0258;
        private static string cacheautoLOC_TST_0259;
        private static string cacheautoLOC_TST_0260;
        private static string cacheautoLOC_TST_0261;
        private static string cacheautoLOC_TST_0262;
        private static string cacheautoLOC_TST_0263;
        private static string cacheautoLOC_TST_0264;
        private static string cacheautoLOC_TST_0265;
        private static string cacheautoLOC_TST_0266;
        private static string cacheautoLOC_TST_0267;
        private static string cacheautoLOC_TST_0268;
        private static string cacheautoLOC_TST_0269;
        private static string cacheautoLOC_TST_0270;
        private static string cacheautoLOC_TST_0271;
        private static string cacheautoLOC_TST_0272;
        private static string cacheautoLOC_TST_0273;
        private static string cacheautoLOC_TST_0274;
        private static string cacheautoLOC_TST_0275;


        private static void CacheStrings()
        {
            cacheautoLOC_TST_0043 = Localizer.Format("#autoLOC_TST_0043"); //#autoLOC_TST_0043 = Close Camera
            cacheautoLOC_TST_0065 = Localizer.Format("#autoLOC_TST_0065"); //#autoLOC_TST_0065 = Close
            cacheautoLOC_TST_0067 = Localizer.Format("#autoLOC_TST_0067"); //#autoLOC_TST_0067 = \u0020Mits
            cacheautoLOC_TST_0084 = Localizer.Format("#autoLOC_TST_0084"); //#autoLOC_TST_0084 = Open Camera
            cacheautoLOC_TST_0236 = Localizer.Format("#autoLOC_TST_0236"); //#autoLOC_TST_0236 = Tarsier Space Technology
            cacheautoLOC_TST_0237 = Localizer.Format("#autoLOC_TST_0237"); //#autoLOC_TST_0237 = Close Window
            cacheautoLOC_TST_0238 = Localizer.Format("#autoLOC_TST_0238"); //#autoLOC_TST_0238 = Active Vessel has no TST Cameras installed
            cacheautoLOC_TST_0239 = Localizer.Format("#autoLOC_TST_0239"); //#autoLOC_TST_0239 = Cameras
            cacheautoLOC_TST_0240 = Localizer.Format("#autoLOC_TST_0240"); //#autoLOC_TST_0240 = TST Cameras
            cacheautoLOC_TST_0241 = Localizer.Format("#autoLOC_TST_0241"); //#autoLOC_TST_0241 = Name
            cacheautoLOC_TST_0242 = Localizer.Format("#autoLOC_TST_0242"); //#autoLOC_TST_0242 = The Vessel name
            cacheautoLOC_TST_0243 = Localizer.Format("#autoLOC_TST_0243"); //#autoLOC_TST_0243 = Type
            cacheautoLOC_TST_0244 = Localizer.Format("#autoLOC_TST_0244"); //#autoLOC_TST_0244 = The part type name
            cacheautoLOC_TST_0245 = Localizer.Format("#autoLOC_TST_0245"); //#autoLOC_TST_0245 = Zoom
            cacheautoLOC_TST_0246 = Localizer.Format("#autoLOC_TST_0246"); //#autoLOC_TST_0246 = The maximum zoom capability of this part
            cacheautoLOC_TST_0247 = Localizer.Format("#autoLOC_TST_0247"); //#autoLOC_TST_0247 = SmlScope
            cacheautoLOC_TST_0248 = Localizer.Format("#autoLOC_TST_0248"); //#autoLOC_TST_0248 = Space Telescope
            cacheautoLOC_TST_0249 = Localizer.Format("#autoLOC_TST_0249"); //#autoLOC_TST_0249 = AdvScope
            cacheautoLOC_TST_0250 = Localizer.Format("#autoLOC_TST_0250"); //#autoLOC_TST_0250 = Advanced Space Telescope
            cacheautoLOC_TST_0251 = Localizer.Format("#autoLOC_TST_0251"); //#autoLOC_TST_0251 = Close
            cacheautoLOC_TST_0252 = Localizer.Format("#autoLOC_TST_0252"); //#autoLOC_TST_0252 = GUI
            cacheautoLOC_TST_0253 = Localizer.Format("#autoLOC_TST_0253"); //#autoLOC_TST_0253 = Toggle the Camera GUI on or off
            cacheautoLOC_TST_0254 = Localizer.Format("#autoLOC_TST_0254"); //#autoLOC_TST_0254 = Open
            cacheautoLOC_TST_0255 = Localizer.Format("#autoLOC_TST_0255"); //#autoLOC_TST_0255 = View
            cacheautoLOC_TST_0256 = Localizer.Format("#autoLOC_TST_0256"); //#autoLOC_TST_0256 = View stored Science
            cacheautoLOC_TST_0257 = Localizer.Format("#autoLOC_TST_0257"); //#autoLOC_TST_0257 = No Connection
            cacheautoLOC_TST_0258 = Localizer.Format("#autoLOC_TST_0258"); //#autoLOC_TST_0258 = Vessel has no Remote Tech connection or crew
            cacheautoLOC_TST_0259 = Localizer.Format("#autoLOC_TST_0259"); //#autoLOC_TST_0259 = RovCam
            cacheautoLOC_TST_0260 = Localizer.Format("#autoLOC_TST_0260"); //#autoLOC_TST_0260 = ChemCam Rover part
            cacheautoLOC_TST_0261 = Localizer.Format("#autoLOC_TST_0261"); //#autoLOC_TST_0261 = Active Vessel has no TST SSDs installed
            cacheautoLOC_TST_0262 = Localizer.Format("#autoLOC_TST_0262"); //#autoLOC_TST_0262 = SSDs
            cacheautoLOC_TST_0263 = Localizer.Format("#autoLOC_TST_0263"); //#autoLOC_TST_0263 = Science Storage Device
            cacheautoLOC_TST_0264 = Localizer.Format("#autoLOC_TST_0264"); //#autoLOC_TST_0264 = Device type name
            cacheautoLOC_TST_0265 = Localizer.Format("#autoLOC_TST_0265"); //#autoLOC_TST_0265 = Capacity
            cacheautoLOC_TST_0266 = Localizer.Format("#autoLOC_TST_0266"); //#autoLOC_TST_0266 = Storage capacity in Mits
            cacheautoLOC_TST_0267 = Localizer.Format("#autoLOC_TST_0267"); //#autoLOC_TST_0267 = Used
            cacheautoLOC_TST_0268 = Localizer.Format("#autoLOC_TST_0268"); //#autoLOC_TST_0268 = The percentage used
            cacheautoLOC_TST_0269 = Localizer.Format("#autoLOC_TST_0269"); //#autoLOC_TST_0269 = Science
            cacheautoLOC_TST_0270 = Localizer.Format("#autoLOC_TST_0270"); //#autoLOC_TST_0270 = The number of science data entries stored
            cacheautoLOC_TST_0271 = Localizer.Format("#autoLOC_TST_0271"); //#autoLOC_TST_0271 = Fill
            cacheautoLOC_TST_0272 = Localizer.Format("#autoLOC_TST_0272"); //#autoLOC_TST_0272 = Fill the Drive with Science
            cacheautoLOC_TST_0273 = Localizer.Format("#autoLOC_TST_0273"); //#autoLOC_TST_0273 = View
            cacheautoLOC_TST_0274 = Localizer.Format("#autoLOC_TST_0274"); //#autoLOC_TST_0274 = Review all Science
            cacheautoLOC_TST_0275 = Localizer.Format("#autoLOC_TST_0275"); //#autoLOC_TST_0275 = Resize Window

        }

        #endregion
        public TSTMenu()
        {
            Instance = this;
        }     

        public void Awake()
        {
            Utilities.Log("TSTMenu awake in " + HighLogic.LoadedScene);
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                Utilities.Log("TSTMenu Not SpaceCenter or Flight Scene, Destroying this instance.");
                Destroy(this);
            }
            CacheStrings();
            TSTwindowID = Utilities.getnextrandomInt();
            FTTwindowID = Utilities.getnextrandomInt();
            RT2Present = Utilities.IsRTInstalled;
            
            TSTMenuAppLToolBar = new AppLauncherToolBar(Localizer.Format("#autoLOC_TST_0234"), Localizer.Format("#autoLOC_TST_0235"), //#autoLOC_TST_0234 = TST #autoLOC_TST_0235 = Tarsier Space Tech
                "TarsierSpaceTech/Icons/ToolbarIcon",
                (ApplicationLauncher.AppScenes.FLIGHT),
                GameDatabase.Instance.GetTexture("TarsierSpaceTech/Icons/TSTIconOn", false), GameDatabase.Instance.GetTexture("TarsierSpaceTech/Icons/TSTIconOff", false),
                GameScenes.FLIGHT);

            GameEvents.onGameSceneSwitchRequested.Add(onGameSceneSwitchRequested);
            GameEvents.onVesselSwitching.Add(onVesselSwitching);

            Utilities.Log("Awake complete");
        }
        
        
        
        public void Start()
        {
            Utilities.Log_Debug("TSTMenu Start in " + HighLogic.LoadedScene);
            FwindowPos.x = TSTMstStgs.Instance.TSTsettings.FwindowPosX;
            FwindowPos.y = TSTMstStgs.Instance.TSTsettings.FwindowPosY;
            
            if (RT2Present)
            {
                Utilities.Log("RT2 present");
                RTWrapper.InitTRWrapper();
            }

            //If TST Settings wants to use ToolBar mod, check it is installed and available. If not set the TST Setting to use Stock.
            if (!ToolbarManager.ToolbarAvailable && !TSTMstStgs.Instance.TSTsettings.UseAppLauncher)
            {
                TSTMstStgs.Instance.TSTsettings.UseAppLauncher = true;
            }

            TSTMenuAppLToolBar.Start(TSTMstStgs.Instance.TSTsettings.UseAppLauncher);
            
            Utilities.setScaledScreen();
            Utilities.Log_Debug("TSTMenu Start complete");
        }
        
        public void OnDestroy()
        {
            TSTMenuAppLToolBar.Destroy();
            
            TSTMstStgs.Instance.TSTsettings.FwindowPosX = FwindowPos.x;
            TSTMstStgs.Instance.TSTsettings.FwindowPosY = FwindowPos.y;

            GameEvents.onGameSceneSwitchRequested.Remove(onGameSceneSwitchRequested);
            GameEvents.onVesselSwitching.Remove(onVesselSwitching);

        }

        private void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> fromto)
        {
            _onGameSceneSwitchRequested = true;
        }

        private void onVesselSwitching(Vessel from, Vessel to)
        {
            if (TSTMenuAppLToolBar.GuiVisible)
                TSTMenuAppLToolBar.GuiVisible = false;
        }

        public void Update()
        {
            if (Time.timeSinceLevelLoad < 2f || _onGameSceneSwitchRequested)
                return;
            

            if (Utilities.GameModeisFlight && FlightGlobals.ActiveVessel != null)  // Check if in flight
            {
                if (RT2Present)
                {
                    try
                    {
                        checkRT2();
                    }
                    catch
                    {
                        Utilities.Log("Wrong Remote Tech 2 library version - disabled.");
                        RT2Present = false;
                    }
                }
                //chk if current active vessel Has TST parts attached  
                tstchemcam.Clear();
                tstSpaceTel.Clear();
                tstSDD.Clear();
                //tstGyroReactionWheel.Clear();          
                tstchemcam = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTChemCam>();
                tstSpaceTel = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTSpaceTelescope>();
                tstSDD = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTScienceHardDrive>();
                //tstGyroReactionWheel = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTGyroReactionWheel>();

                if (tstchemcam.Count == 0 && tstSpaceTel.Count == 0 && tstSDD.Count == 0) // No TST parts on-board disable buttons
                {
                    if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
                    {
                        TSTMenuAppLToolBar.setToolBarBtnVisibility(false);
                    }
                    else
                    {
                        TSTMenuAppLToolBar.setAppLSceneVisibility(ApplicationLauncher.AppScenes.SPACECENTER);
                    }
                    TSTMenuAppLToolBar.GuiVisible = false;
                }  
                else //TST parts are on-board enable buttons
                {
                    if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
                    {
                        TSTMenuAppLToolBar.setToolBarBtnVisibility(true);
                    }
                    else
                    {
                        TSTMenuAppLToolBar.setAppLSceneVisibility(ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT);
                    }
                }
            }
        }

        //GUI Functions Follow

        private void OnGUI()
        {
            if (Utilities.GameModeisEVA || !TSTMenuAppLToolBar.GuiVisible || TSTMenuAppLToolBar.gamePaused || TSTMenuAppLToolBar.hideUI
                || _onGameSceneSwitchRequested)
            {
                return;
            }
            GUI.skin = HighLogic.Skin;
            if (!stylesSet) setupStyles();  //Set Styles if not already set (only need once).
            if (!Textures.StylesSet) Textures.SetupStyles(); 

            if (Utilities.GameModeisFlight)
            {
                crntWindow = TSTWindow.FLIGHT;
                FwindowPos.ClampToScreen();
                FwindowPos = GUILayout.Window(TSTwindowID, FwindowPos, windowF, cacheautoLOC_TST_0236, GUILayout.MinHeight(160), GUILayout.ExpandWidth(true), //#autoLOC_TST_0236 = Tarsier Space Technology
                                GUILayout.ExpandHeight(true));
                //if (_FTVisible)
                //    FTwindowPos = GUILayout.Window(FTTwindowID, FTwindowPos, windowFT, "Fine Tune Gyros & SAS", GUILayout.MinHeight(20));
            }    
            
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.DrawToolTip();
        }

        private void windowF(int id)
        {

            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, cacheautoLOC_TST_0237); //#autoLOC_TST_0237 = Close Window
            Rect closeRect = new Rect(FwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                TSTMenuAppLToolBar.onAppLaunchToggle();
                return;
            }

            GUILayout.BeginVertical();
            //Scrollable Camera list starts here                       
            // Begin the ScrollView                        
            CamscrollViewVector = GUILayout.BeginScrollView(CamscrollViewVector, GUILayout.Height(120));
            GUILayout.BeginVertical();
                      
            if (tstchemcam.Count == 0 && tstSpaceTel.Count == 0)
            {                
                GUILayout.Label(cacheautoLOC_TST_0238, statusStyle); //  #autoLOC_TST_0238 = Active Vessel has no TST Cameras installed
            }
            else
            {
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0239, cacheautoLOC_TST_0240), sectionTitleStyle); //#autoLOC_TST_0239 = Cameras #autoLOC_TST_0240 = TST Cameras
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0241, cacheautoLOC_TST_0242), PartListStyle, GUILayout.Width(180)); //#autoLOC_TST_0241 = Name #autoLOC_TST_0242 = The Vessel name
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0243, cacheautoLOC_TST_0244), PartListStyle, GUILayout.Width(80)); //#autoLOC_TST_0243 = Type #autoLOC_TST_0244 = The part type name
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0245, cacheautoLOC_TST_0246), PartListStyle, GUILayout.Width(40)); //#autoLOC_TST_0245 = Zoom #autoLOC_TST_0246 = The maximum zoom capability of this part
                GUILayout.EndHorizontal();

                int ind = 0;
                foreach (TSTSpaceTelescope scope in tstSpaceTel)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(scope.name, PartListPartStyle, GUILayout.Width(180));
                    if (scope.part.name == "tarsierSpaceTelescope")
                    {
                        GUILayout.Label(new GUIContent(cacheautoLOC_TST_0247, cacheautoLOC_TST_0248), PartListPartStyle, GUILayout.Width(80)); //#autoLOC_TST_0247 = SmlScope #autoLOC_TST_0248 = Space Telescope
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent(cacheautoLOC_TST_0249, cacheautoLOC_TST_0250), PartListPartStyle, GUILayout.Width(80)); //#autoLOC_TST_0249 = AdvScope #autoLOC_TST_0250 = Advanced Space Telescope
                    }
                    GUILayout.Label(string.Format("{0}x", scope.maxZoom), PartListPartStyle, GUILayout.Width(40));
                    if (!RT2Present || (RT2Present && (!RT2Enabled || RT2VesselConnected)))
                    {
                        if (scope.Active)
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0251, cacheautoLOC_TST_0043), GUILayout.Width(42f))) //#autoLOC_TST_0251 = Close #autoLOC_TST_0043 = Close Camera
                            {
                                scope.eventCloseCamera();
                            }
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0252, cacheautoLOC_TST_0253), GUILayout.Width(34f))) //#autoLOC_TST_0252 = GUI #autoLOC_TST_0253 = Toggle the Camera GUI on or off
                            {
                                if (scope.windowState == TSTSpaceTelescope.WindowState.Hidden)
                                {
                                    scope.eventShowGUI();
                                }
                                else
                                {
                                    scope.hideGUI();
                                }
                            }                            
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0254, cacheautoLOC_TST_0084), GUILayout.Width(42f))) //#autoLOC_TST_0254 = Open #autoLOC_TST_0084 = Open Camera
                            {
                                scope.eventOpenCamera();
                            }
                        }
                        if (scope.GetScienceCount() > 0)
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0255, cacheautoLOC_TST_0256), GUILayout.Width(35f))) //#autoLOC_TST_0255 = View #autoLOC_TST_0256 = View stored Science
                                scope.ReviewData();
                        }
                        /* Deprecated
                        if (tstGyroReactionWheel.ElementAtOrDefault(ind) != null)
                        {
                            if (GUILayout.Button(new GUIContent("F/T", "Fine Tune Camera Gyros"), GUILayout.Width(34f)))
                            {
                                FTTelIndex = ind;
                                _FTVisible = !_FTVisible;
                            }
                        }*/
                        
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent(cacheautoLOC_TST_0257, cacheautoLOC_TST_0258), PartListPartStyle, GUILayout.Width(76)); //#autoLOC_TST_0257 = No Connection #autoLOC_TST_0258 = Vessel has no Remote Tech connection or crew
                    }
                    GUILayout.EndHorizontal();
                    
                    ind++;
                }
                foreach (TSTChemCam chemcam in tstchemcam)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(chemcam.name, PartListPartStyle, GUILayout.Width(180));
                    GUILayout.Label(new GUIContent(cacheautoLOC_TST_0259, cacheautoLOC_TST_0260), PartListPartStyle, GUILayout.Width(80)); //#autoLOC_TST_0259 = RovCam #autoLOC_TST_0260 = ChemCam Rover part
                    if (!RT2Present || (RT2Present && (!RT2Enabled || RT2VesselConnected)))
                    {
                        if (chemcam.Active)
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0065 , cacheautoLOC_TST_0043), GUILayout.Width(42f))) //#autoLOC_TST_0065 = Close #autoLOC_TST_0043 = Close Camera
                            {
                                chemcam.eventCloseCamera();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0254, cacheautoLOC_TST_0084), GUILayout.Width(42f))) //#autoLOC_TST_0254 = Open #autoLOC_TST_0084 = Open Camera
                            {
                                chemcam.eventOpenCamera();
                            }
                        }
                        if (chemcam.GetScienceCount() > 0)
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0255, cacheautoLOC_TST_0256), GUILayout.Width(35f))) //#autoLOC_TST_0255 = View #autoLOC_TST_0256 = View stored Science
                                chemcam.ReviewData();
                        }
                    }
                    else
                    {
                        GUILayout.Label(new GUIContent(cacheautoLOC_TST_0257, cacheautoLOC_TST_0258), PartListPartStyle, GUILayout.Width(76)); //#autoLOC_TST_0257 = No Connection #autoLOC_TST_0258 = Vessel has no Remote Tech connection or crew
                    }
                    GUILayout.EndHorizontal();
                    
                }    

            }
            // End the ScrollView
            GUILayout.EndVertical();
            GUILayout.EndScrollView();          
            
            //Scrollable SDD list starts here                       
            // Begin the ScrollView                        
            SDDscrollViewVector = GUILayout.BeginScrollView(SDDscrollViewVector);
            GUILayout.BeginVertical();
            if (tstSDD.Count == 0)
            {
                GUILayout.Label(cacheautoLOC_TST_0261, PartListPartStyle); //#autoLOC_TST_0261 = Active Vessel has no TST SSDs installed
            }
            else
            {
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0262, cacheautoLOC_TST_0263), sectionTitleStyle); //#autoLOC_TST_0262 = SSDs #autoLOC_TST_0263 = Science Storage Device
                GUILayout.BeginHorizontal();
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0241, cacheautoLOC_TST_0264),PartListStyle, GUILayout.Width(155)); //#autoLOC_TST_0241 = Name #autoLOC_TST_0264 = Device type name
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0265, cacheautoLOC_TST_0266), PartListStyle, GUILayout.Width(65)); //#autoLOC_TST_0265 = Capacity #autoLOC_TST_0266 = Storage capacity in Mits
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0267, cacheautoLOC_TST_0268), PartListStyle, GUILayout.Width(40)); //#autoLOC_TST_0267 = Used #autoLOC_TST_0268 = The percentage used
                GUILayout.Label(new GUIContent(cacheautoLOC_TST_0269, cacheautoLOC_TST_0270), PartListStyle, GUILayout.Width(70)); //#autoLOC_TST_0269 = Science #autoLOC_TST_0270 = The number of science data entries stored
                GUILayout.EndHorizontal();

                foreach (TSTScienceHardDrive drive in tstSDD)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(drive.name, PartListPartStyle, GUILayout.Width(155));
                    GUILayout.Label(string.Format("{0}",drive.Capacity) + cacheautoLOC_TST_0067, PartListPartStyle, GUILayout.Width(65)); //#autoLOC_TST_0067 = \u0020Mits
                    GUILayout.Label(string.Format("{0}%", drive.PercentageFull), PartListPartStyle, GUILayout.Width(40));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0}", drive.scienceData.Count), PartListPartStyle, GUILayout.Width(10));
                    if (!RT2Present || (RT2Present && (!RT2Enabled || RT2VesselConnected)))
                    {
                        if (drive.PercentageFull >= 100f)
                        {
                            GUI.enabled = false;
                        }
                        if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0271, cacheautoLOC_TST_0272), GUILayout.Width(35f), GUILayout.Height(28f))) //#autoLOC_TST_0271 = Fill #autoLOC_TST_0272 = Fill the Drive with Science
                            drive.fillDrive();
                        GUI.enabled = true;
                        if (drive.scienceData.Count > 0)
                        {
                            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0273, cacheautoLOC_TST_0274), GUILayout.Width(35f), GUILayout.Height(28f))) //#autoLOC_TST_0273 = View #autoLOC_TST_0274 = Review all Science
                                drive.ReviewData();
                        }
                    }
                    GUILayout.EndHorizontal();                    
                    GUILayout.EndHorizontal();
                }
            }      
            // End the ScrollView
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
            //Scrollable SDD list ends here
            GUILayout.Space(14);

            GUIContent resizeContent = new GUIContent(Textures.BtnResize, cacheautoLOC_TST_0275); //#autoLOC_TST_0275 = Resize Window
            Rect resizeRect = new Rect(FwindowPos.width - 17, FwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, Textures.ResizeStyle);
            HandleResizeEvents(resizeRect);
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            
            GUI.DragWindow();
        }
        
        private void HandleResizeEvents(Rect resizeRect)
        {
            Event theEvent = Event.current;
            if (theEvent != null)
            {
                if (!mouseDown)
                {
                    if (theEvent.type == EventType.MouseDown && theEvent.button == 0 && resizeRect.Contains(theEvent.mousePosition))
                    {
                        mouseDown = true;
                        theEvent.Use();
                    }
                }
                else if (theEvent.type != EventType.Layout)
                {
                    if (Input.GetMouseButton(0))
                    {
                        // Flip the mouse Y so that 0 is at the top
                        float mouseY = Screen.height - Input.mousePosition.y;

                        switch (crntWindow)
                        {
                            case TSTWindow.FLIGHT:
                                FwindowPos.width = Mathf.Clamp(Input.mousePosition.x - FwindowPos.x + (resizeRect.width / 2), 50, Screen.width - FwindowPos.x);
                                FwindowPos.height = Mathf.Clamp(mouseY - FwindowPos.y + (resizeRect.height / 2), 50, Screen.height - FwindowPos.y);
                                break;
                        }                        
                    }
                    else
                    {
                        mouseDown = false;
                    }
                }
            }
        }
        /*
        private void windowFT(int id)
        {

            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, "Close Window");
            Rect closeRect = new Rect(FTwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                _FTVisible = false;
                return;
            }
            
            GUILayout.BeginVertical();
            GUILayout.Label("Gyroscopic Reaction Wheels:");
            float GyroSensitivity = tstGyroReactionWheel[FTTelIndex].sensitivity;
            GUILayout.Label("Sensitivity=" + GyroSensitivity.ToString("##00.00"), statusStyle, GUILayout.Width(130));
            GyroSensitivity = GUILayout.HorizontalSlider(GyroSensitivity, 0.001f, 1f, GUILayout.ExpandWidth(true));
            tstGyroReactionWheel[FTTelIndex].sensitivity = GyroSensitivity;

            float powerscale = tstGyroReactionWheel[FTTelIndex].powerscale;
            GUILayout.Label("PowerScale=" + powerscale.ToString("##00.00"), statusStyle, GUILayout.Width(130));
            powerscale = GUILayout.HorizontalSlider(powerscale, 0f, 2f, GUILayout.ExpandWidth(true));
            tstGyroReactionWheel[FTTelIndex].powerscale = powerscale;

            float pidkp = tstSpaceTel[FTTelIndex].PIDKp;
            float pidki = tstSpaceTel[FTTelIndex].PIDKi; 
            float pidkd = tstSpaceTel[FTTelIndex].PIDKd;
            GUILayout.Label("RSAS PIDS:"); 
            GUILayout.BeginHorizontal();      
            GUILayout.Label("KP=" + pidkp.ToString("##00.000"),statusStyle, GUILayout.Width(100));
            pidkp = GUILayout.HorizontalSlider(pidkp, 0.001f, 12f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("KI=" + pidki.ToString("##00.000"), statusStyle, GUILayout.Width(100));
            pidki = GUILayout.HorizontalSlider(pidki, 0.001f, 12f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("KD=" + pidkd.ToString("##00.000"), statusStyle, GUILayout.Width(100));
            pidkd = GUILayout.HorizontalSlider(pidkd, 0.001f, 12f, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            tstSpaceTel[FTTelIndex].PIDKp = pidkp;
            tstSpaceTel[FTTelIndex].PIDKi = pidki;
            tstSpaceTel[FTTelIndex].PIDKd = pidkd;            
            GUILayout.EndVertical();
            
            GUI.DragWindow();
        }
        */
        private void setupStyles()
        {
            GUI.skin = HighLogic.Skin;

            //Init styles
            sectionTitleStyle = new GUIStyle(GUI.skin.label);
            sectionTitleStyle.alignment = TextAnchor.MiddleCenter;
            sectionTitleStyle.stretchWidth = true;
            sectionTitleStyle.fontStyle = FontStyle.Bold;

            statusStyle = new GUIStyle(GUI.skin.label);
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.stretchWidth = true;
            statusStyle.normal.textColor = Color.white;

            warningStyle = new GUIStyle(GUI.skin.label);
            warningStyle.alignment = TextAnchor.MiddleLeft;
            warningStyle.stretchWidth = true;
            warningStyle.fontStyle = FontStyle.Bold;
            warningStyle.normal.textColor = Color.red;

            subsystemButtonStyle = new GUIStyle(GUI.skin.toggle);
            subsystemButtonStyle.margin.top = 0;
            subsystemButtonStyle.margin.bottom = 0;
            subsystemButtonStyle.padding.top = 0;
            subsystemButtonStyle.padding.bottom = 0;

            //scrollStyle = new GUIStyle(GUI.skin.scrollView);

            PartListStyle = new GUIStyle(GUI.skin.label);
            PartListStyle.alignment = TextAnchor.MiddleLeft;
            PartListStyle.stretchWidth = false;
            PartListStyle.normal.textColor = Color.yellow;

            PartListPartStyle = new GUIStyle(GUI.skin.label);
            PartListPartStyle.alignment = TextAnchor.LowerLeft;
            PartListPartStyle.stretchWidth = false;
            PartListPartStyle.normal.textColor = Color.white;

            resizeStyle = new GUIStyle(GUI.skin.button);
            resizeStyle.alignment = TextAnchor.MiddleCenter;
            resizeStyle.padding = new RectOffset(1, 1, 1, 1);

            stylesSet = true;
        }

        private void checkRT2()
        {
            if (RTWrapper.APIReady)
            {
                RT2Enabled = RTWrapper.RTactualAPI.IsRemoteTechEnabled;
                RT2VesselConnected = (RTWrapper.RTactualAPI.HasLocalControl(FlightGlobals.ActiveVessel.id) || RTWrapper.RTactualAPI.HasAnyConnection(FlightGlobals.ActiveVessel.id));
                RT2VesselDelay = RTWrapper.RTactualAPI.GetShortestSignalDelay(FlightGlobals.ActiveVessel.id);
            }
            Utilities.Log_Debug("RT2VesselConnected = " + RT2VesselConnected);
            Utilities.Log_Debug("RT2VesselDelay = " + RT2VesselDelay);
        }

    }
}
