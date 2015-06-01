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
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    class TSTMenu : MonoBehaviour
    {
        //GUI Properties
        private IButton button1;
        private ApplicationLauncherButton stockToolbarButton = null; // Stock Toolbar Button
        private static int TSTwindowID = new System.Random().Next();
        private const float FWINDOW_WIDTH = 430;
        private const float WINDOW_BASE_HEIGHT = 320;
        private Rect FwindowPos = new Rect(40, Screen.height / 2 - 100, FWINDOW_WIDTH, WINDOW_BASE_HEIGHT); // Flight Window position and size
        private GUIStyle sectionTitleStyle, subsystemButtonStyle, statusStyle, warningStyle, PartListStyle, PartListPartStyle;
        private GUIStyle scrollStyle;
        private Vector2 CamscrollViewVector = Vector2.zero;
        private Vector2 SDDscrollViewVector = Vector2.zero;
                
        //GuiVisibility
        private bool _Visible = false;

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
            
            FwindowPos.x = TSTMstStgs.Instance.TSTsettings.FwindowPosX;
            FwindowPos.y = TSTMstStgs.Instance.TSTsettings.FwindowPosY;

            RT2Present = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "RemoteTech");
            if (RT2Present)
                Utilities.Log("TSTMenu", "RT2 present");

            if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
            {
                button1 = ToolbarManager.Instance.add("TarsierSpaceTech", "button1");
                button1.TexturePath = "TarsierSpaceTech/Icons/ToolbarIcon";
                button1.ToolTip = "TST";
                button1.Visibility = new GameScenesVisibility(GameScenes.FLIGHT, GameScenes.EDITOR);
                button1.OnClick += (e) => GuiVisible = !GuiVisible;
            }
            else
            {
                // Set up the stock toolbar
                Utilities.Log("TSTMenu","Adding onGUIAppLauncher callbacks");
                if (ApplicationLauncher.Ready)
                {
                    OnGUIAppLauncherReady();
                }
                else
                    GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
            }

            Utilities.Log("TSTMenu","Awake complete");
        }

        #region AppLauncher
        private void OnGUIAppLauncherReady()
        {
            this.Log_Debug("OnGUIAppLauncherReady");
            if (ApplicationLauncher.Ready)
            {
                this.Log_Debug("Adding AppLauncherButton");
                this.stockToolbarButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggle, 
                    onAppLaunchToggle, 
                    DummyVoid,
                    DummyVoid, 
                    DummyVoid, 
                    DummyVoid, 
                    ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW,
                    (Texture)GameDatabase.Instance.GetTexture("TarsierSpaceTech/Icons/TSTIconOff", false));
            }
        }

        private void DummyVoid()
        {
        }

        public void onAppLaunchToggle()
        {
            GuiVisible = !GuiVisible;
            this.stockToolbarButton.SetTexture((Texture)GameDatabase.Instance.GetTexture(GuiVisible ? "TarsierSpaceTech/Icons/TSTIconOn" : "TarsierSpaceTech/Icons/TSTIconOff", false));            
        }
        #endregion AppLauncher
                
        public void Start()
        {
            this.Log_Debug("TSTMenu Start");            
            
            // add callbacks for vessel load and change
            RenderingManager.AddToPostDrawQueue(5, this.onDraw);
            this.Log_Debug("TSTMenu Start complete");
        }

        public void OnDestroy()
        {
            if (ToolbarManager.ToolbarAvailable && TSTMstStgs.Instance.TSTsettings.UseAppLauncher == false)
            {
                button1.Destroy();
            }
            else
            {
                // Set up the stock toolbar
                this.Log_Debug("Removing onGUIAppLauncher callbacks");
                GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
                if (this.stockToolbarButton != null)
                {
                    ApplicationLauncher.Instance.RemoveModApplication(this.stockToolbarButton);
                    this.stockToolbarButton = null;
                }
            }
            if (GuiVisible) GuiVisible = !GuiVisible;
            
            RenderingManager.RemoveFromPostDrawQueue(5, this.onDraw);
            TSTMstStgs.Instance.TSTsettings.FwindowPosX = FwindowPos.x;
            TSTMstStgs.Instance.TSTsettings.FwindowPosY = FwindowPos.y;            
        }

        public void Update()
        {
            if (RT2Present)
                try
                {
                    checkRT2();
                }
                catch
                {
                    this.Log("Wrong Remote Tech 2 library version - disabled.");
                    RT2Present = false;
                }
        }

        //GUI Functions Follow

        private void onDraw()
        {
            if (!GuiVisible) return;

            
                if (FlightGlobals.fetch != null && FlightGlobals.ActiveVessel != null)  // Check if in flight
                {
                    if (FlightGlobals.ActiveVessel.isEVA) // EVA kerbal, do nothing
                    {
                        
                        return;
                    }
                    
                }                
                else   // Not in flight, in editor or F2 pressed unset the mode and return
                {
                    
                    return;
                }
                if (Utilities.isPauseMenuOpen())
                {
                    return;
                }               
                GUI.skin = HighLogic.Skin;
                if (!Utilities.WindowVisibile(FwindowPos)) Utilities.MakeWindowVisible(FwindowPos);
                FwindowPos = GUILayout.Window(TSTwindowID, FwindowPos, windowF, "Tarsier Space Technology", GUILayout.MinHeight(20));            
        }

        private void windowF(int id)
        {
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


            Vessel actVessel = FlightGlobals.ActiveVessel;
            List<TSTChemCam> tstchemcam = new List<TSTChemCam>();
            List<TSTSpaceTelescope> tstSpaceTel = new List<TSTSpaceTelescope>();
            List<TSTScienceHardDrive> tstSDD = new List<TSTScienceHardDrive>();
            //chk if current active vessel Has TST parts attached            
            tstchemcam = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTChemCam>().ToList();
            tstSpaceTel = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTSpaceTelescope>().ToList();
            tstSDD = FlightGlobals.ActiveVessel.FindPartModulesImplementing<TSTScienceHardDrive>().ToList();

            GUIContent label = new GUIContent("X", "Close Window");            
            Rect rect = new Rect(410, 4, 16, 16);
            if (GUI.Button(rect, label))
            {
                onAppLaunchToggle();
                return;
            }

            GUILayout.BeginVertical();
            //Scrollable Camera list starts here                       
            // Begin the ScrollView                        
            CamscrollViewVector = GUILayout.BeginScrollView(CamscrollViewVector, GUILayout.Height(100), GUILayout.Width(420));
            GUILayout.BeginVertical();
                      
            if (tstchemcam.Count() == 0 && tstSpaceTel.Count() == 0)
            {                
                GUILayout.Label("Active Vessel has no TST Cameras installed", statusStyle);                
            }
            else
            {
                GUILayout.Label("Cameras", sectionTitleStyle);
                GUILayout.BeginHorizontal();
                GUILayout.Label("Name", PartListStyle, GUILayout.Width(160));
                GUILayout.Label("Type", PartListStyle, GUILayout.Width(60));
                GUILayout.Label("Zoom", PartListStyle, GUILayout.Width(40));
                GUILayout.EndHorizontal();   

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
                                if (scope.windowState == TSTSpaceTelescope.WindowSate.Hidden)
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
                    }
                    else
                    {
                        GUILayout.Label("No Connection", PartListPartStyle, GUILayout.Width(76));
                    }
                    GUILayout.EndHorizontal();
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
            SDDscrollViewVector = GUILayout.BeginScrollView(SDDscrollViewVector, GUILayout.Height(215), GUILayout.Width(420));
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
                    //placeholder - put button to show science content of drive in another window
                    GUILayout.EndHorizontal();
                }
            }      
            // End the ScrollView
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            
            GUILayout.EndVertical();
            //Scrollable SDD list ends here
            
            if (!Input.GetMouseButtonDown(1))
            {
                GUI.DragWindow(new Rect(0, 0, Screen.width, 30));
            }
        }

        private void checkRT2()
        {
            RT2VesselConnected = RemoteTech.API.API.HasAnyConnection(FlightGlobals.ActiveVessel.id);
            RT2VesselDelay = RemoteTech.API.API.GetShortestSignalDelay(FlightGlobals.ActiveVessel.id);
            this.Log_Debug("RT2VesselConnected = " + RT2VesselConnected);
            this.Log_Debug("RT2VesselDelay = " + RT2VesselDelay);
        }

    }
}
