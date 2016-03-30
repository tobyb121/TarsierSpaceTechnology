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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RSTUtils;
using UnityEngine;
using KSP.UI.Screens;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class TSTMenu : MonoBehaviour
    {
        //GUI Properties 
        private IButton _button1;
        private ApplicationLauncherButton _stockToolbarButton = null; // Stock Toolbar Button
        private static int TSTwindowID = new System.Random().Next();
        private static int FTTwindowID = 0;
        private static int SCwindowID = 0;
        private const float FWINDOW_WIDTH = 430;        
        private const float SWINDOW_WIDTH = 450;
        private const float WINDOW_BASE_HEIGHT = 320;
        private Rect FwindowPos = new Rect(40, Screen.height / 2 - 100, FWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private Rect FTwindowPos = new Rect(60 + FWINDOW_WIDTH, Screen.height / 2 - 100, FWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private Rect SCwindowPos = new Rect(40, Screen.height / 2 - 100, SWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private GUIStyle sectionTitleStyle, subsystemButtonStyle, statusStyle, warningStyle, PartListStyle, PartListPartStyle;
        private GUIStyle scrollStyle, resizeStyle;
        private Vector2 CamscrollViewVector = Vector2.zero;
        private Vector2 SDDscrollViewVector = Vector2.zero;
        private bool mouseDown = false;
        private bool stylesSet = false;
        private bool LoadConfig = true;

        //Settings Menu Temp Vars              
        private string InputSChemwinSml, InputSChemwinLge, InputSTelewinLge, InputSTelewinSml;
        private int InputVChemwinSml, InputVChemwinLge, InputVTelewinLge, InputVTelewinSml;
        private bool InputUseAppLauncher, Inputdebugging, InputTooltips;


        //GuiVisibility
        private bool _Visible = false;
        private bool _FTVisible = false;
        private enum TSTWindow
        {
            FLIGHT,
            SETTINGS
        }
        private TSTWindow crntWindow;

        //TST Parts
        private List<TSTChemCam> tstchemcam = new List<TSTChemCam>();
        private List<TSTSpaceTelescope> tstSpaceTel = new List<TSTSpaceTelescope>();
        private List<TSTScienceHardDrive> tstSDD = new List<TSTScienceHardDrive>();
        private List<TSTGyroReactionWheel> tstGyroReactionWheel = new List<TSTGyroReactionWheel>();        
        private int FTTelIndex = 0;

        public Boolean GuiVisible
        {
            get { return _Visible; }
            set
            {
                _Visible = value;      //Set the private variable
            }
        }

        private bool RT2Present = false;
        private bool RT2VesselConnected = false;
        private double RT2VesselDelay = 0f;        
                

        public void Awake()
        {
            Utilities.Log("TSTMenu awake in " + HighLogic.LoadedScene.ToString());
            if (HighLogic.LoadedScene != GameScenes.SPACECENTER && HighLogic.LoadedScene != GameScenes.FLIGHT)
            {
                Utilities.Log("TSTMenu Not SpaceCenter or Flight Scene, Destroying this instance.");
                Destroy(this);
            }
            TSTwindowID = Utilities.getnextrandomInt();
            FTTwindowID = TSTwindowID + 1;
            SCwindowID = FTTwindowID + 1;
            RT2Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "RemoteTech");
            Utilities.Log("Awake complete");
        }

        #region AppLauncher
        private void OnGUIAppLauncherReady()
        {
            Utilities.Log_Debug("OnGUIAppLauncherReady");
            if (ApplicationLauncher.Ready)
            {
                Utilities.Log_Debug("Adding AppLauncherButton");
                this._stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggle, 
                    onAppLaunchToggle, 
                    DummyVoid,
                    DummyVoid, 
                    DummyVoid, 
                    DummyVoid, 
                    ApplicationLauncher.AppScenes.SPACECENTER |
                    //ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | | ApplicationLauncher.AppScenes.MAPVIEW
                    ApplicationLauncher.AppScenes.FLIGHT,
                    (Texture)GameDatabase.Instance.GetTexture("TarsierSpaceTech/Icons/TSTIconOff", false));
            }
        }

        private void DummyVoid()
        {
        }

        public void onAppLaunchToggle()
        {
            GuiVisible = !GuiVisible;
            this._stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(GuiVisible ? "TarsierSpaceTech/Icons/TSTIconOn" : "TarsierSpaceTech/Icons/TSTIconOff", false));            
        }

        void OnGameSceneLoadRequestedForAppLauncher(GameScenes SceneToLoad)
        {
            if (this._stockToolbarButton != null)
            {
                ApplicationLauncherButton[] lstButtons = TSTMenu.FindObjectsOfType<ApplicationLauncherButton>();
                Utilities.Log_Debug("TSTMenu AppLauncher: Destroying Button-Button Count:" + lstButtons.Length.ToString());
                ApplicationLauncher.Instance.RemoveModApplication(this._stockToolbarButton);
                this._stockToolbarButton = null;
            }
        }

        #endregion AppLauncher

        public void Start()
        {
            Utilities.Log_Debug("TSTMenu Start in " + HighLogic.LoadedScene.ToString());
            FwindowPos.x = TSTMstStgs.Instance.TSTsettings.FwindowPosX;
            FwindowPos.y = TSTMstStgs.Instance.TSTsettings.FwindowPosY;
            SCwindowPos.x = TSTMstStgs.Instance.TSTsettings.SCwindowPosX;
            SCwindowPos.y = TSTMstStgs.Instance.TSTsettings.SCwindowPosY;
            if (_button1 != null)
                _button1.Destroy();

            if (RT2Present)
            {
                Utilities.Log("RT2 present");
                RTWrapper.InitTRWrapper();
            }
            if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
            {
                
                _button1 = ToolbarManager.Instance.add("TarsierSpaceTech", "button1");
                _button1.TexturePath = "TarsierSpaceTech/Icons/ToolbarIcon";
                _button1.ToolTip = "Tarsier Space Technology";
                _button1.Visibility = new GameScenesVisibility(GameScenes.SPACECENTER, GameScenes.FLIGHT);
                _button1.OnClick += (e) => { GuiVisible = !GuiVisible; Utilities.Log("TSTButtonClicked visible=" + GuiVisible); };
                Utilities.Log("ToolBar Set");}
            else
            {
                // Set up the stock toolbar
                Utilities.Log("Adding onGUIAppLauncher callbacks");
                if (ApplicationLauncher.Ready)
                {
                    OnGUIAppLauncherReady();
                }
                else
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            }

            // add callbacks for vessel load and change
            GameEvents.onGameSceneLoadRequested.Add(OnGameSceneLoadRequestedForAppLauncher);
            Utilities.setScaledScreen();
            //RenderingManager.AddToPostDrawQueue(5, this.onDraw);
            Utilities.Log_Debug("TSTMenu Start complete");
        }

        public void OnDestroy()
        {
            if (_button1 != null)
            {
                _button1.Destroy();
                Utilities.Log("Toolbar Removed");
            }
            
            // Stock toolbar
            Utilities.Log_Debug("Removing onGUIAppLauncher callbacks");
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
            if (this._stockToolbarButton != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(this._stockToolbarButton);
                this._stockToolbarButton = null;
            }
            
            if (GuiVisible) GuiVisible = !GuiVisible;
            //RenderingManager.RemoveFromPostDrawQueue(5, this.onDraw);
            GameEvents.onGameSceneLoadRequested.Remove(OnGameSceneLoadRequestedForAppLauncher);
            TSTMstStgs.Instance.TSTsettings.FwindowPosX = FwindowPos.x;
            TSTMstStgs.Instance.TSTsettings.FwindowPosY = FwindowPos.y;
            TSTMstStgs.Instance.TSTsettings.SCwindowPosX = SCwindowPos.x;
            TSTMstStgs.Instance.TSTsettings.SCwindowPosY = SCwindowPos.y;
        }

        public void Update()
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

            if (Utilities.GameModeisFlight && FlightGlobals.ActiveVessel != null)  // Check if in flight
            {
                //chk if current active vessel Has TST parts attached            
                tstchemcam = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTChemCam>().ToList();
                tstSpaceTel = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTSpaceTelescope>().ToList();
                tstSDD = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTScienceHardDrive>().ToList();
                tstGyroReactionWheel = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTGyroReactionWheel>().ToList();

                if (tstchemcam.Count == 0 && tstSpaceTel.Count == 0 && tstSDD.Count == 0) // No TST parts on-board disable buttons
                {
                    if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
                    {
                        _button1.Visible = false;
                    }
                    else
                    {
                        this._stockToolbarButton.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER;
                    }
                    GuiVisible = false;
                }  
                else //TST parts are on-board enable buttons
                {
                    if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
                    {
                        _button1.Visible = true;
                    }
                    else
                    {
                        this._stockToolbarButton.VisibleInScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT;
                    }
                }
            }
               
        }

        //GUI Functions Follow

        private void OnGUI()
        {
            //if (Utilities.GameModeisEVA || !GuiVisible || Utilities.isPauseMenuOpen)  // EVA kerbal, GUI not set to visible or pause menu is open :- do nothing
            if (Utilities.GameModeisEVA || !GuiVisible)
            {
                return;
            }
            GUI.skin = HighLogic.Skin;
            if (!stylesSet) setupStyles();  //Set Styles if not already set (only need once).
            if (!Textures.StylesSet) Textures.SetupStyles(); 

            if (Utilities.GameModeisFlight)
            {
                crntWindow = TSTWindow.FLIGHT;
                if (!Utilities.WindowVisibile(FwindowPos)) Utilities.MakeWindowVisible(FwindowPos);
                FwindowPos = GUILayout.Window(TSTwindowID, FwindowPos, windowF, "Tarsier Space Technology", GUILayout.MinHeight(20), GUILayout.ExpandWidth(true),
                                GUILayout.ExpandHeight(true));
                if (_FTVisible)
                    FTwindowPos = GUILayout.Window(FTTwindowID, FTwindowPos, windowFT, "Fine Tune Gyros & SAS", GUILayout.MinHeight(20));
            }    
                         
            if (Utilities.GameModeisSpaceCenter)
            {
                crntWindow = TSTWindow.SETTINGS;
                if (LoadConfig)
                {
                    TSTMstStgs.Instance.TSTsettings.Load(TSTMstStgs.Instance.globalNode);                    
                    InputVChemwinSml = TSTMstStgs.Instance.TSTsettings.ChemwinSml;
                    InputVChemwinLge = TSTMstStgs.Instance.TSTsettings.ChemwinLge;
                    InputVTelewinSml = TSTMstStgs.Instance.TSTsettings.TelewinSml;
                    InputVTelewinLge = TSTMstStgs.Instance.TSTsettings.TelewinLge;
                    InputSChemwinSml = InputVChemwinSml.ToString();
                    InputSChemwinLge = InputVChemwinLge.ToString();
                    InputSTelewinSml = InputVTelewinSml.ToString();
                    InputSTelewinLge = InputVTelewinLge.ToString();
                    InputUseAppLauncher = TSTMstStgs.Instance.TSTsettings.UseAppLauncher;
                    Inputdebugging = TSTMstStgs.Instance.TSTsettings.debugging;
                    InputTooltips = TSTMstStgs.Instance.TSTsettings.Tooltips;                 
                    LoadConfig = false;
                }
                if (!Utilities.WindowVisibile(SCwindowPos)) Utilities.MakeWindowVisible(SCwindowPos);
                SCwindowPos = GUILayout.Window(SCwindowID, SCwindowPos, windowSC, "Tarsier Space Technology", GUILayout.MinHeight(20), GUILayout.ExpandWidth(true),
                                GUILayout.ExpandHeight(true));                
            }

            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.DrawToolTip();
        }

        private void windowF(int id)
        {

            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, "Close Window");
            Rect closeRect = new Rect(FwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                onAppLaunchToggle();
                return;
            }

            GUILayout.BeginVertical();
            //Scrollable Camera list starts here                       
            // Begin the ScrollView                        
            CamscrollViewVector = GUILayout.BeginScrollView(CamscrollViewVector, GUILayout.Height(100));
            GUILayout.BeginVertical();
                      
            if (tstchemcam.Count() == 0 && tstSpaceTel.Count() == 0)
            {                
                GUILayout.Label("Active Vessel has no TST Cameras installed", statusStyle);                
            }
            else
            {
                GUILayout.Label(new GUIContent("Cameras", "TST Cameras"), sectionTitleStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", PartListStyle, GUILayout.Width(160));
                GUILayout.Label("Type", PartListStyle, GUILayout.Width(60));
                GUILayout.Label("Zoom", PartListStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();

                int ind = 0;
                foreach (TSTSpaceTelescope scope in tstSpaceTel)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(scope.name, PartListPartStyle, GUILayout.Width(160));
                    if (scope.part.name == "tarsierSpaceTelescope")
                    {
                        GUILayout.Label("SmlScope", PartListPartStyle, GUILayout.Width(60));
                    }
                    else
                    {
                        GUILayout.Label("AdvScope", PartListPartStyle, GUILayout.Width(60));
                    }
                    GUILayout.Label(string.Format("{0}x", scope.maxZoom), PartListPartStyle, GUILayout.Width(30));
                    if (!RT2Present || (RT2Present && RT2VesselConnected))
                    {
                        if (scope.Active)
                        {
                            if (GUILayout.Button(new GUIContent("Close", "Close Camera"), GUILayout.Width(42f)))
                            {
                                scope.eventCloseCamera();
                            }
                            if (GUILayout.Button(new GUIContent("GUI", "Toggle the Camera GUI on/off"), GUILayout.Width(34f)))
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
                            if (GUILayout.Button(new GUIContent("Open", "Open Camera"), GUILayout.Width(42f)))
                            {
                                scope.eventOpenCamera();
                            }
                        }
                        if (scope.GetScienceCount() > 0)
                        {
                            if (GUILayout.Button(new GUIContent("View", "View Science"), GUILayout.Width(35f)))
                                scope.ReviewData();
                        }
                        if (GUILayout.Button(new GUIContent("F/T", "Fine Tune Gyros"), GUILayout.Width(34f)))
                        {
                            FTTelIndex = ind;
                            _FTVisible = !_FTVisible;
                        }
                    }
                    else
                    {
                        GUILayout.Label("No Connection", PartListPartStyle, GUILayout.Width(76));
                    }
                    GUILayout.EndHorizontal();
                    
                    ind++;
                }
                foreach (TSTChemCam chemcam in tstchemcam)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(chemcam.name, PartListPartStyle, GUILayout.Width(160));
                    GUILayout.Label("RovCam", PartListPartStyle, GUILayout.Width(60));
                    if (!RT2Present || (RT2Present && RT2VesselConnected))
                    {
                        if (chemcam.Active)
                        {
                            if (GUILayout.Button(new GUIContent("Close", "Close Camera"), GUILayout.Width(42f)))
                            {
                                chemcam.eventCloseCamera();
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(new GUIContent("Open", "Open Camera"), GUILayout.Width(42f)))
                            {
                                chemcam.eventOpenCamera();
                            }
                        }
                        if (chemcam.GetScienceCount() > 0)
                        {
                            if (GUILayout.Button(new GUIContent("View", "View Science"), GUILayout.Width(35f)))
                                chemcam.ReviewData();
                        }
                    }
                    else
                    {
                        GUILayout.Label("No Connection", PartListPartStyle, GUILayout.Width(76));
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
            if (tstSDD.Count() == 0)
            {
                GUILayout.Label("Active Vessel has no TST SDDs installed", PartListPartStyle);
            }
            else
            {
                GUILayout.Label("SDDs", sectionTitleStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", PartListStyle, GUILayout.Width(155));
                GUILayout.Label("Capacity", PartListStyle, GUILayout.Width(65));
                GUILayout.Label("Used", PartListStyle, GUILayout.Width(40));
                GUILayout.Label("Science", PartListStyle, GUILayout.Width(70));
                GUILayout.EndHorizontal();

                foreach (TSTScienceHardDrive drive in tstSDD)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(drive.name, PartListPartStyle, GUILayout.Width(155));
                    GUILayout.Label(string.Format("{0}Mits",drive.Capacity), PartListPartStyle, GUILayout.Width(65));
                    GUILayout.Label(string.Format("{0}%", drive.PercentageFull), PartListPartStyle, GUILayout.Width(40));
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(string.Format("{0}", drive.scienceData.Count()), PartListPartStyle, GUILayout.Width(10));
                    if (!RT2Present || (RT2Present && RT2VesselConnected))
                    {
                        if (drive.PercentageFull >= 100f)
                        {
                            GUI.enabled = false;
                        }
                        if (GUILayout.Button(new GUIContent("Fill", "FillDrive with Science"), GUILayout.Width(35f), GUILayout.Height(28f)))
                            drive.fillDrive();
                        GUI.enabled = true;
                        if (drive.scienceData.Count() > 0)
                        {
                            if (GUILayout.Button(new GUIContent("View", "View Science"), GUILayout.Width(35f), GUILayout.Height(28f)))
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

            GUIContent resizeContent = new GUIContent(Textures.BtnResize, "Resize Window");
            Rect resizeRect = new Rect(FwindowPos.width - 17, FwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, Textures.ResizeStyle);
            HandleResizeEvents(resizeRect);
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            
            GUI.DragWindow();
        }

        private void windowSC(int id)
        {
            GUIContent closeContent = new GUIContent(Textures.BtnRedCross, "Close Window");
            Rect closeRect = new Rect(SCwindowPos.width - 21, 4, 16, 16);
            if (GUI.Button(closeRect, closeContent, Textures.ClosebtnStyle))
            {
                onAppLaunchToggle();
                return;
            }

            //Settings Menu                     
            GUILayout.BeginVertical(); 
            // Begin the ScrollView                        
            CamscrollViewVector = GUILayout.BeginScrollView(CamscrollViewVector, GUILayout.Height(250));
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Small Size setting of ChemCam Window in pixels", "Small Size setting of ChemCam Window in pixels"), statusStyle, GUILayout.Width(300));
            InputSChemwinSml = Regex.Replace(GUILayout.TextField(InputSChemwinSml, 4, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!int.TryParse(InputSChemwinSml, out InputVChemwinSml))
            {
                InputVChemwinSml = TSTMstStgs.Instance.TSTsettings.ChemwinSml;
            }
            if (InputVChemwinSml > Utilities.scaledScreenWidth - 20)
            {
                InputVChemwinSml = Utilities.scaledScreenWidth - 20;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Large Size setting of ChemCam Window in pixels", "Large Size setting of ChemCam Window in pixels"), statusStyle, GUILayout.Width(300));
            InputSChemwinLge = Regex.Replace(GUILayout.TextField(InputSChemwinLge, 4, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!int.TryParse(InputSChemwinLge, out InputVChemwinLge))
            {
                InputVChemwinLge = TSTMstStgs.Instance.TSTsettings.ChemwinLge;
            }
            if (InputVChemwinLge > Utilities.scaledScreenWidth - 20)
            {
                InputVChemwinLge = Utilities.scaledScreenWidth - 20;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Small Size setting of Telescope Window in pixels", "Small Size setting of Telescope Window in pixels"), statusStyle, GUILayout.Width(300));
            InputSTelewinSml = Regex.Replace(GUILayout.TextField(InputSTelewinSml, 4, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!int.TryParse(InputSTelewinSml, out InputVTelewinSml))
            {
                InputVTelewinSml = TSTMstStgs.Instance.TSTsettings.TelewinSml;
            }
            if (InputVTelewinSml > Utilities.scaledScreenWidth - 20)
            {
                InputVTelewinSml = Utilities.scaledScreenWidth - 20;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box(new GUIContent("Large Size setting of ChemCam Window in pixels", "Large Size setting of ChemCam Window in pixels"), statusStyle, GUILayout.Width(300));
            InputSTelewinLge = Regex.Replace(GUILayout.TextField(InputSTelewinLge, 4, GUILayout.MinWidth(30.0F)), "[^.0-9]", "");  //you can play with the width of the text box
            GUILayout.EndHorizontal();
            if (!int.TryParse(InputSTelewinLge, out InputVTelewinLge))
            {
                InputVTelewinLge = TSTMstStgs.Instance.TSTsettings.TelewinLge;
            }
            if (InputVTelewinLge > Utilities.scaledScreenWidth - 20)
            {
                InputVTelewinLge = Utilities.scaledScreenWidth - 20;
            }

            GUILayout.BeginHorizontal();
            GUILayout.Box("Use Stock App Icon (Scene Change required)", statusStyle, GUILayout.Width(280));
            InputUseAppLauncher = GUILayout.Toggle(InputUseAppLauncher, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box("Debug Logging ON", statusStyle, GUILayout.Width(280));
            Inputdebugging = GUILayout.Toggle(Inputdebugging, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Box("ToolTips ON", statusStyle, GUILayout.Width(280));
            InputTooltips = GUILayout.Toggle(InputTooltips, "", GUILayout.MinWidth(30.0F)); //you can play with the width of the text box
            GUILayout.EndHorizontal();

            // End the ScrollView
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Save & Exit Settings", "Save & Exit Settings"), GUILayout.Width(155f)))
            {
                TSTMstStgs.Instance.TSTsettings.ChemwinSml = InputVChemwinSml;
                TSTMstStgs.Instance.TSTsettings.ChemwinLge = InputVChemwinLge;
                TSTMstStgs.Instance.TSTsettings.TelewinSml = InputVTelewinSml;
                TSTMstStgs.Instance.TSTsettings.TelewinLge = InputVTelewinLge;
                TSTMstStgs.Instance.TSTsettings.UseAppLauncher = InputUseAppLauncher;
                TSTMstStgs.Instance.TSTsettings.debugging = Inputdebugging;
                TSTMstStgs.Instance.TSTsettings.Tooltips = InputTooltips;                                              
                LoadConfig = true;                
                TSTMstStgs.Instance.TSTsettings.Save(TSTMstStgs.Instance.globalNode);
                GuiVisible = !GuiVisible;
            }
            if (GUILayout.Button(new GUIContent("Reset Settings", "Reset Settings"), GUILayout.Width(155f)))
            {
                LoadConfig = true;
            }
            GUILayout.EndHorizontal();
            //Settings Menu    
            GUILayout.EndVertical();            
            GUILayout.Space(14);

            GUIContent resizeContent = new GUIContent(Textures.BtnResize, "Resize Window");
            Rect resizeRect = new Rect(SCwindowPos.width - 17, SCwindowPos.height - 17, 16, 16);
            GUI.Label(resizeRect, resizeContent, Textures.ResizeStyle);
            HandleResizeEvents(resizeRect);
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();

            GUI.DragWindow();
        }

        private void HandleResizeEvents(Rect resizeRect)
        {
            var theEvent = Event.current;
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

                            case TSTWindow.SETTINGS:
                                SCwindowPos.width = Mathf.Clamp(Input.mousePosition.x - SCwindowPos.x + (resizeRect.width / 2), 50, Screen.width - SCwindowPos.x);
                                SCwindowPos.height = Mathf.Clamp(mouseY - SCwindowPos.y + (resizeRect.height / 2), 50, Screen.height - SCwindowPos.y);
                                break;

                            default:
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

        private void windowFT(int id)
        {                                    
            GUIContent label = new GUIContent("X", "Close Window");
            Rect rect = new Rect(410, 4, 16, 16);
            if (GUI.Button(rect, label))
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

            scrollStyle = new GUIStyle(GUI.skin.scrollView);

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
                RT2VesselConnected = (RTWrapper.RTactualAPI.HasLocalControl(FlightGlobals.ActiveVessel.id) || RTWrapper.RTactualAPI.HasLocalControl(FlightGlobals.ActiveVessel.id));
                RT2VesselDelay = RTWrapper.RTactualAPI.GetShortestSignalDelay(FlightGlobals.ActiveVessel.id);
            }
            Utilities.Log_Debug("RT2VesselConnected = " + RT2VesselConnected.ToString());
            Utilities.Log_Debug("RT2VesselDelay = " + RT2VesselDelay.ToString());
        }

    }
}
