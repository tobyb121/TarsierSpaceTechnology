/*
 * TSTSpaceTelescope.cs
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
using System.Collections;
using System.Collections.Generic;
using Contracts;
using KSP.UI.Screens.Flight.Dialogs;
using RSTUtils;
using UnityEngine;
using KSP.Localization;

namespace TarsierSpaceTech
{
    public class TSTSpaceTelescope : PartModule, IScienceDataContainer
    {
        private static int CAMwindowID = 5955555;
        private static int GALwindowID = 5955556;
        private static int BODwindowID = 5955557;

        private static readonly List<byte[]> targets_raw = new List<byte[]>
        {
            Properties.Resources.target_01,
            Properties.Resources.target_02,
            Properties.Resources.target_03,
            Properties.Resources.target_04,
            Properties.Resources.target_05,
            Properties.Resources.target_06,
            Properties.Resources.target_07,
            Properties.Resources.target_08,
            Properties.Resources.target_09,
            Properties.Resources.target_10,
            Properties.Resources.target_11,
            Properties.Resources.target_12
        };

        private static readonly List<Texture2D> targets = new List<Texture2D>();
        private Animation _animationClose;
        private Animation _animationOpen;
        private Transform _animationTransform;
        private Transform _baseTransform;
        internal TSTCameraModule _camera;

        private bool _inEditor;

        private ITargetable _lastTarget;
        private Transform _lookTransform;
        private bool _saveToFile;
        private readonly List<ScienceData> _scienceData = new List<ScienceData>();

        private bool _showTarget;
        private Vessel _vessel;
        private bool isRBInstalled = false;

        [KSPField(isPersistant = true)] public bool Active;

        [KSPField] public string animationClipNameClose = "close";
        [KSPField] public string animationClipNameOpen = "open";
        [KSPField] public string animationNameClose = "";
        [KSPField] public string animationNameOpen = "";
        [KSPField] public string animationTransformName = "Telescope";
        [KSPField] public string baseTransformName = "Telescope";
        [KSPField] public string cameraTransformName = "CameraTransform";
        [KSPField] public string overrideModuleName = "";
        [KSPField] public string overrideEventNameOpen = "";
        [KSPField] public string overrideEventNameClose = "";
        [KSPField] public string disableModuleName = "";
        [KSPField] public string disableEventName = "";


        private Vector2 BodscrollViewVector = Vector2.zero;
        private CelestialBody bodyTarget;
        private bool filterContractTargets;
        private TSTGalaxy galaxyTarget;
        private Vector2 GalscrollViewVector = Vector2.zero;
        private int GUI_WIDTH_LARGE = 600;
        private int GUI_WIDTH_SMALL = 320;
        private bool overrideEvents, overrideEventsProcessed;
        private bool disableEvents, disableEventsProcessed;
        private BaseEventList overrideEventList;
        private BaseEventList disableEventList;
        private bool activeonStartup = false;

        [KSPField] public float labBoostScalar = 0f;

        [KSPField] public string lookTransformName = "LookTransform";

        [KSPField(guiActive = false, guiName = "#autoLOC_TST_0081", isPersistant = true)] public int maxZoom = 5; //#autoLOC_TST_0081 = maxZoom

        public float PIDKd = 0.5f;
        public float PIDKi = 6f;
        public float PIDKp = 12f;
        private int selectedTargetIndex = -1;

        [KSPField]
        public bool servoControl = true;

        private bool showBodTargetsWindow;
        private bool showGalTargetsWindow;
        private Rect targetBodWindowPos = new Rect(512, 128, 0, 0);
        private Rect targetGalWindowPos = new Rect(512, 128, 0, 0);

        private int targetId;
        private List<TargetableObject> lookingAtObjects;
        public List<TargetableObject> LookingAtObjects
        {
            get { return lookingAtObjects; }
        }
        private List<TargetableObject> possibletargetObjects;
        private List<CelestialBody> cbTargetList;
        private List<CelestialBody> galaxyTargetList;
        private List<string> contractTargets;

        public TargettingMode targettingMode = TargettingMode.Galaxy;
        private Rect windowPos = new Rect(128, 128, 0, 0);
        public WindowState windowState = WindowState.Small;

        [KSPField]
        public float xmitDataScalar = 0.5f;

        private Quaternion zeroRotation;
        public Transform cameraTransform { get; private set; }

        #region String Caches

        private static string cacheautoLOC_TST_0035;
        private static string cacheautoLOC_TST_0036;
        private static string cacheautoLOC_TST_0037;
        private static string cacheautoLOC_TST_0038;
        private static string cacheautoLOC_TST_0088;
        private static string cacheautoLOC_TST_0089;
        private static string cacheautoLOC_TST_0090;
        private static string cacheautoLOC_TST_0091;
        private static string cacheautoLOC_TST_0092;
        private static string cacheautoLOC_TST_0093;
        private static string cacheautoLOC_TST_0094;
        private static string cacheautoLOC_TST_0095;
        private static string cacheautoLOC_TST_0096;
        private static string cacheautoLOC_TST_0097;
        private static string cacheautoLOC_TST_0098;
        private static string cacheautoLOC_TST_0099;
        private static string cacheautoLOC_TST_0100;
        private static string cacheautoLOC_TST_0101;
        private static string cacheautoLOC_TST_0102;
        private static string cacheautoLOC_TST_0103;
        private static string cacheautoLOC_TST_0104;
        private static string cacheautoLOC_TST_0105;
        private static string cacheautoLOC_TST_0106;
        private static string cacheautoLOC_TST_0107;
        private static string cacheautoLOC_TST_0108;
        private static string cacheautoLOC_TST_0109;

        private static void CacheStrings()
        {
            cacheautoLOC_TST_0035 = Localizer.Format("#autoLOC_TST_0035"); //#autoLOC_TST_0035 = Large
            cacheautoLOC_TST_0036 = Localizer.Format("#autoLOC_TST_0036"); //#autoLOC_TST_0036 = Set Large Window Size
            cacheautoLOC_TST_0037 = Localizer.Format("#autoLOC_TST_0037"); //#autoLOC_TST_0037 = Small
            cacheautoLOC_TST_0038 = Localizer.Format("#autoLOC_TST_0038"); //#autoLOC_TST_0038 = set Small Window Size
            cacheautoLOC_TST_0088 = Localizer.Format("#autoLOC_TST_0088"); //#autoLOC_TST_0088 = Zoom
            cacheautoLOC_TST_0089 = Localizer.Format("#autoLOC_TST_0089"); //#autoLOC_TST_0089 = Set Zoom level on camera
            cacheautoLOC_TST_0090 = Localizer.Format("#autoLOC_TST_0090"); //#autoLOC_TST_0090 = Reset Zoom
            cacheautoLOC_TST_0091 = Localizer.Format("#autoLOC_TST_0091"); //#autoLOC_TST_0091 = Reset the Camera Zoom Level
            cacheautoLOC_TST_0092 = Localizer.Format("#autoLOC_TST_0092"); //#autoLOC_TST_0092 = Hide Galaxies
            cacheautoLOC_TST_0093 = Localizer.Format("#autoLOC_TST_0093"); //#autoLOC_TST_0093 = Hide the Galaxies Window
            cacheautoLOC_TST_0094 = Localizer.Format("#autoLOC_TST_0094"); //#autoLOC_TST_0094 = Show Galaxies
            cacheautoLOC_TST_0095 = Localizer.Format("#autoLOC_TST_0095"); //#autoLOC_TST_0095 = Show the Galaxies Window
            cacheautoLOC_TST_0096 = Localizer.Format("#autoLOC_TST_0096"); //#autoLOC_TST_0096 = Hide Bodies
            cacheautoLOC_TST_0097 = Localizer.Format("#autoLOC_TST_0097"); //#autoLOC_TST_0097 = Hide the Celestial Bodies Window
            cacheautoLOC_TST_0098 = Localizer.Format("#autoLOC_TST_0098"); //#autoLOC_TST_0098 = Show Bodies
            cacheautoLOC_TST_0099 = Localizer.Format("#autoLOC_TST_0099"); //#autoLOC_TST_0099 = Show the Celestial Bodies Window
            cacheautoLOC_TST_0100 = Localizer.Format("#autoLOC_TST_0100"); //#autoLOC_TST_0100 = Hide
            cacheautoLOC_TST_0101 = Localizer.Format("#autoLOC_TST_0101"); //#autoLOC_TST_0101 = Hide this Window
            cacheautoLOC_TST_0102 = Localizer.Format("#autoLOC_TST_0102"); //#autoLOC_TST_0102 = Show Target
            cacheautoLOC_TST_0103 = Localizer.Format("#autoLOC_TST_0103"); //#autoLOC_TST_0103 = Show/Hide the Targeting Reticle
            cacheautoLOC_TST_0104 = Localizer.Format("#autoLOC_TST_0104"); //#autoLOC_TST_0104 = Save To File
            cacheautoLOC_TST_0105 = Localizer.Format("#autoLOC_TST_0105"); //#autoLOC_TST_0105 = If this is on, picture files will be saved to GameData/TarsierSpaceTech/PluginData/TarsierSpaceTech
            cacheautoLOC_TST_0106 = Localizer.Format("#autoLOC_TST_0106"); //#autoLOC_TST_0106 = Take Picture
            cacheautoLOC_TST_0107 = Localizer.Format("#autoLOC_TST_0107"); //#autoLOC_TST_0107 = Take a Picture with the Camera
            cacheautoLOC_TST_0108 = Localizer.Format("#autoLOC_TST_0108"); //#autoLOC_TST_0108 = Show only contract targets
            cacheautoLOC_TST_0109 = Localizer.Format("#autoLOC_TST_0109"); //#autoLOC_TST_0109 = If selected only targets that are the subject of a current contract will be shown

        }

        #endregion

        public override void OnStart(StartState state)
        {
            CacheStrings();
            base.OnStart(state);
            Utilities.Log_Debug("TSTTel Starting Telescope");
            lookingAtObjects = new List<TargetableObject>();
            possibletargetObjects = new List<TargetableObject>();
            part.CoMOffset = part.attachNodes[0].position;
            if (state == StartState.Editor)
            {
                _inEditor = true;
            }
            _baseTransform = Utilities.FindChildRecursive(transform, baseTransformName);
            cameraTransform = Utilities.FindChildRecursive(transform, cameraTransformName);
            _lookTransform = Utilities.FindChildRecursive(transform, lookTransformName);
            _animationTransform = Utilities.FindChildRecursive(transform, animationTransformName);
            zeroRotation = cameraTransform.localRotation;
            if (state != StartState.Editor)
            {
                _camera = cameraTransform.gameObject.AddComponent<TSTCameraModule>();
                _camera.telescopeReference = this;
            }

            //_animation = _baseTransform.animation;
            if (_animationTransform != null)
            {
                _animationOpen = animationNameOpen == "" ? _animationTransform.GetComponent<Animation>() : Utilities.FindAnimChildRecursive(_animationTransform, animationNameOpen);
                _animationClose = animationNameClose == "" ? _animationTransform.GetComponent<Animation>() : Utilities.FindAnimChildRecursive(_animationTransform, animationNameClose);
            }

            //Set start-up Event state for this module.
            if (!Active) //camera is not active on startup
            {
                activeonStartup = false;
                Events["eventOpenCamera"].active = true;
                Events["eventCloseCamera"].active = false;
                Events["eventShowGUI"].active = false;
            }
            else //Camera is active on startup
            {
                activeonStartup = true;
                Events["eventOpenCamera"].active = false;
                Events["eventCloseCamera"].active = true;
                Events["eventShowGUI"].active = true;
            }
            Events["eventControlFromHere"].active = false;
            Events["eventReviewScience"].active = false;

            //Check if override or disable fields are supplied and if so set flags to process them in OnUpdate (have to wait for the other
            // part modules to start first, so can't do them here).
            if (overrideModuleName != "" && (overrideEventNameOpen != "" || overrideEventNameClose != ""))
            {
                overrideEvents = true;
                overrideEventsProcessed = false;
            }
            if (disableModuleName != "" && disableEventName != "")
            {
                disableEvents = true;
                disableEventsProcessed = false;
            }

            if (state == StartState.Editor)
                return;

            for (int i = 0; i < targets_raw.Count; i++)
            {
                Texture2D tex = new Texture2D(40, 40);
                tex.LoadImage(targets_raw[i]);
                targets.Add(tex);
            }
            
            buildTargetLists();
            CAMwindowID = Utilities.getnextrandomInt();
            GALwindowID = Utilities.getnextrandomInt();
            BODwindowID = Utilities.getnextrandomInt();
            Utilities.Log_Debug("TSTTel On end start");
            //StartCoroutine(setSASParams());
            Utilities.Log_Debug("TSTTel Adding Input Callback");
            _vessel = vessel;
            _vessel.OnAutopilotUpdate += onFlightInput;
            GameEvents.onVesselChange.Add(refreshFlightInputHandler);
            GameEvents.onVesselDestroy.Add(removeFlightInputHandler);
            GameEvents.OnVesselRecoveryRequested.Add(removeFlightInputHandler);
            GameEvents.Contract.onContractsLoaded.Add(onContractSystemReady);
            GameEvents.Contract.onAccepted.Add(onContractAccepted);
            Utilities.Log_Debug("TSTTel Added Input Callback");
            //if (Active) //Moved to OnUpdate so we can process any override/disable event parameters correctly.
            //    StartCoroutine(openCamera());
            isRBInstalled = Utilities.IsResearchBodiesInstalled;
            getTargetObjectList();
        }

        public void OnDestroy()
        {
            GameEvents.onVesselChange.Remove(refreshFlightInputHandler);
            GameEvents.onVesselDestroy.Remove(removeFlightInputHandler);
            GameEvents.OnVesselRecoveryRequested.Remove(removeFlightInputHandler);
            GameEvents.Contract.onContractsLoaded.Remove(onContractSystemReady);
            GameEvents.Contract.onAccepted.Remove(onContractAccepted);
        }

        private void buildTargetLists()
        {
            cbTargetList = new List<CelestialBody>(FlightGlobals.Bodies);
            galaxyTargetList = new List<CelestialBody>(TSTGalaxies.CBGalaxies);
            contractTargets = new List<string>();
            List<CelestialBody> cbsToRemove = new List<CelestialBody>();
            for (int i = 0; i < cbTargetList.Count; i++)
            {
                if (cbTargetList[i].Radius < 100)
                {
                    cbsToRemove.Add(cbTargetList[i]);
                }
            }
            for (int i = 0; i < cbsToRemove.Count; i++)
            {
                cbTargetList.Remove(cbsToRemove[i]);
            }
        }

        protected void onContractSystemReady()
        {
            //Get contract targets
            if (ContractSystem.Instance)
            {
                TSTTelescopeContract[] TSTtelescopecontracts = ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>();
                for (int i = 0; i < TSTtelescopecontracts.Length; i++)
                {
                    contractTargets.Add(TSTtelescopecontracts[i].target.name);
                }
            }
        }

        protected void onContractAccepted(Contracts.Contract contract)
        {
            contractTargets.Clear();
            onContractSystemReady();
        }

        private IEnumerator setSASParams()
        {
            while (FlightGlobals.ActiveVessel.Autopilot.SAS.pidLockedPitch == null)
                yield return null;
            Utilities.Log_Debug("Setting PIDs");
            FlightGlobals.ActiveVessel.Autopilot.SAS.pidLockedPitch.Reinitialize(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.SAS.pidLockedRoll.Reinitialize(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.SAS.pidLockedYaw.Reinitialize(PIDKp, PIDKi, PIDKd);
        }

        public void removeFlightInputHandler(Vessel target)
        {
            Utilities.Log_Debug("Removing Input Callback vessel: " + target.name);
            if (vessel == target)
            {
                _vessel.OnAutopilotUpdate -= onFlightInput;
                GameEvents.onVesselChange.Remove(refreshFlightInputHandler);
                GameEvents.onVesselDestroy.Remove(removeFlightInputHandler);
                GameEvents.OnVesselRecoveryRequested.Remove(removeFlightInputHandler);
                //StopCoroutine(setSASParams());
                Utilities.Log_Debug("Input Callbacks removed this vessel");
            }
        }

        [KSPEvent(active = false, guiActive = true, guiName = "Disable Servos")]
        public void toggleServos()
        {
            servoControl = !servoControl;
            Events["toggleServos"].guiName = servoControl ? Localizer.Format("#autoLOC_TST_0082") : Localizer.Format("#autoLOC_TST_0083"); //#autoLOC_TST_0082 = Disable Servos #autoLOC_TST_0083 = Enable Servos
            if (!servoControl)
                cameraTransform.localRotation = zeroRotation;
        }

        private void refreshFlightInputHandler(Vessel target)
        {
            Utilities.Log_Debug("OnVesselSwitch curr: " + vessel.name + " target: " + target.name);
            if (vessel != target)
            {
                Utilities.Log_Debug("This vessel != target removing Callback");
                _vessel.OnAutopilotUpdate -= onFlightInput;
            }
            if (vessel == target)
            {
                _vessel = target;
                List<TSTSpaceTelescope> vpm = _vessel.FindPartModulesImplementing<TSTSpaceTelescope>();
                if (vpm.Count > 0)
                {
                    Utilities.Log_Debug("Adding Input Callback");
                    _vessel.OnAutopilotUpdate += onFlightInput;
                    Utilities.Log_Debug("Added Input Callback");
                }
            }
        }

        private void onFlightInput(FlightCtrlState ctrl)
        {
            if (_camera.Enabled && servoControl)
            {
                if (ctrl.X > 0)
                {
                    cameraTransform.Rotate(Vector3.up, -0.005f*_camera.fov);
                }
                else if (ctrl.X < 0)
                {
                    cameraTransform.Rotate(Vector3.up, 0.005f*_camera.fov);
                }
                if (ctrl.Y > 0)
                {
                    cameraTransform.Rotate(Vector3.right, -0.005f*_camera.fov);
                }
                else if (ctrl.Y < 0)
                {
                    cameraTransform.Rotate(Vector3.right, 0.005f*_camera.fov);
                }

                float angle = Mathf.Abs(Quaternion.Angle(cameraTransform.localRotation, zeroRotation));

                if (angle > 1.5f)
                {
                    cameraTransform.localRotation = Quaternion.Slerp(zeroRotation, cameraTransform.localRotation,
                        1.5f/angle);
                }
            }
        }

        public override void OnUpdate()
        {
            //If there are overrideEvents and we haven't initially processed them do so now (should only run ONCE)
            if (overrideEvents && !overrideEventsProcessed)
            {
                try
                {
                    foreach (PartModule pm in part.Modules) //should be a shorter way to do this, but a foreach cycle works
                    {
                        if (pm.moduleName == overrideModuleName)
                        {
                            overrideEventList = pm.Events;
                            if (overrideEventNameOpen != "" && !overrideEventList.Contains(overrideEventNameOpen))
                            {
                                overrideEventNameOpen = "";
                                overrideEvents = false;
                            }

                            if (overrideEventNameClose != "" && !overrideEventList.Contains(overrideEventNameClose))
                            {
                                overrideEventNameClose = "";
                                overrideEvents = false;
                            }
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    //throw;
                }
                overrideEventsProcessed = true;
            }
            
            //If there are disableEvents and we haven't initially processed them do so now (should only run ONCE)
            if (disableEvents && !disableEventsProcessed)
            {
                try
                {
                    foreach (PartModule pm in part.Modules) //should be a shorter way to do this, but a foreach cycle works
                    {
                        if (pm.moduleName == disableModuleName)
                        {
                            disableEventList = pm.Events;
                            if (disableEventName != "" && !disableEventList.Contains(disableEventName))
                            {
                                disableEventName = "";
                                disableEvents = false;
                            }
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    //throw;
                }
                disableEventsProcessed = true;
            }

            //Once-OFF if camera active on startup/loading open the camera (moved from OnStart to allow for disable and override events
            if (activeonStartup)  
            {
                eventOpenCamera();
                activeonStartup = false;
            }

            //If there are overrideEvents or disableEvents process them on every OnUpdate in case their partmodule is resetting them.
            if (overrideEvents || disableEvents)
            {
                processOverrideDisableEvents(!Active);
            }

            

            Events["eventReviewScience"].active = _scienceData.Count > 0;
            if (vessel.targetObject != _lastTarget && vessel.targetObject != null)
            {
                targettingMode = TargettingMode.Planet;
                selectedTargetIndex = -1;
                _lastTarget = vessel.targetObject;
            }

            //if (vessel.targetObject != null)
            //{
            //    Utilities.Log_Debug("Vessel target=" + vessel.targetObject.GetTransform().position);
            //}

            //if (!_inEditor && _camera.Enabled && windowState != WindowSate.Hidden && vessel.isActiveVessel)
            //{                
            //if (_camera.Enabled && f++ % frameLimit == 0)                                   
            //_camera.draw();                
            //}
        }

        [KSPEvent(active = true, guiActive = true, name = "eventOpenCamera", guiActiveEditor = true, guiName = "#autoLOC_TST_0084")] //#autoLOC_TST_0084 = Open Camera
        public void eventOpenCamera()
        {
            Events["eventOpenCamera"].active = false;
            try
            {
                //If there is an override Event fire it off now (external module animation like DMagic scanners
                if (overrideEvents && overrideEventNameOpen != "" && overrideEventList != null)
                {
                    try
                    {
                        overrideEventList[overrideEventNameOpen].Invoke();
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("An error occurred attempting to execute override Event {0}", overrideEventNameOpen);
                        Utilities.Log("Err: " + ex);
                        //throw;
                    }
                }
                //Start the coroutine to open the camera
                StartCoroutine(openCamera());
            }
            catch (Exception)
            {
                //throw;
            }
        }

        [KSPAction("OpenCamera")]
        public void ActivateAction(KSPActionParam param)
        {
            eventOpenCamera();
        }

        public IEnumerator openCamera()
        {
            //If there is an animation defined, run it and yield until it is complete.
            if (_animationOpen != null)
            {
                if (animationClipNameOpen == "")
                {
                    _animationOpen.Play();
                    IEnumerator wait = Utilities.WaitForAnimationNoClip(_animationOpen);
                    while (wait.MoveNext()) yield return null;
                }
                else
                {
                    _animationOpen.Play(animationClipNameOpen);
                    IEnumerator wait = Utilities.WaitForAnimation(_animationOpen, animationClipNameOpen);
                    while (wait.MoveNext()) yield return null;
                }
            }
            //Set events
            Events["eventShowGUI"].active = true;
            Events["eventCloseCamera"].active = true;
            Events["eventControlFromHere"].active = true;
            Events["toggleServos"].active = true;
            if (!_inEditor)
            {
                //Set camera active
                _camera.Enabled = true;
                Active = true;
                windowState = WindowState.Small;
                _camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL);
                cameraTransform.localRotation = zeroRotation;
            }
        }

        [KSPEvent(active = false, guiActive = true, name = "eventCloseCamera", guiActiveEditor = true, guiName = "#autoLOC_TST_0085")] //#autoLOC_TST_0085 = Close Camera
        public void eventCloseCamera()
        {
            try
            {
                if (overrideEvents && overrideEventNameClose != "" && overrideEventList != null)
                {
                    try
                    {
                        overrideEventList[overrideEventNameClose].Invoke();
                    }
                    catch (Exception ex)
                    {
                        Utilities.Log("An error occurred attempting to execute override Event {0}", overrideEventNameClose);
                        Utilities.Log("Err: " + ex);
                        //throw;
                    }
                }
                    
                
                Events["eventShowGUI"].active = false;
                Events["eventCloseCamera"].active = false;
                Events["eventControlFromHere"].active = false;
                Events["toggleServos"].active = false;
                if (_camera != null)
                    _camera.Enabled = false;
                Active = false;
                StartCoroutine(closeCamera());
                if (_inEditor)
                    return;
                if (vessel.ReferenceTransform == _lookTransform)
                {
                    vessel.FallBackReferenceTransform();
                }
            }
            catch (Exception)
            {
                //throw;
            }
        }

        [KSPAction("CloseCamera")]
        public void DeactivateAction(KSPActionParam param)
        {
            eventCloseCamera();
        }

        public IEnumerator closeCamera()
        {
            if (_animationClose != null)
            {
                if (animationClipNameClose == "")
                {
                    _animationClose[animationNameClose].speed = -1f;
                    _animationClose[animationNameClose].normalizedTime = 1f;
                    _animationClose.Play();
                    IEnumerator wait = Utilities.WaitForAnimationNoClip(_animationClose);
                    while (wait.MoveNext()) yield return null;
                }
                else
                {
                    _animationClose.Play(animationClipNameClose);
                    IEnumerator wait = Utilities.WaitForAnimation(_animationClose, animationClipNameClose);
                    while (wait.MoveNext()) yield return null;
                }
            }
            Events["eventOpenCamera"].active = true;
        }

        [KSPEvent(active = false, guiActive = true, name = "eventControlFromHere", guiName = "#autoLOC_TST_0086")] //#autoLOC_TST_0086 = Control From Here
        public void eventControlFromHere()
        {
            part.SetReferenceTransform(_lookTransform);
            vessel.SetReferenceTransform(part);
        }

        public void processOverrideDisableEvents(bool active)
        {
            if (overrideEvents)
            {
                if (overrideEventNameOpen != "" && overrideEventList != null)
                {
                    try
                    {
                        if (overrideEventList.Contains(overrideEventNameOpen))
                            overrideEventList[overrideEventNameOpen].guiActive = false;
                    }
                    catch (Exception)
                    {
                        //throw;
                    }

                }

                if (overrideEventNameClose != "" && overrideEventList != null)
                {
                    try
                    {
                        
                        if (overrideEventList.Contains(overrideEventNameClose))
                            overrideEventList[overrideEventNameClose].guiActive = false;
                    }
                    catch (Exception)
                    {
                        //throw;
                    }

                }
            }

            if (disableEvents)
            {
                if (disableEventName != "" && disableEventList != null)
                    //disable only if camera is active, enable if it isn't (via passed in Bool).
                {
                    disableEventList[disableEventName].active = true;
                    try
                    {
                        disableEventList[disableEventName].guiActive = active;
                        disableEventList[disableEventName].active = active;
                    }
                    catch (Exception)
                    {
                        //throw;
                    }
                }
            }
        }

        public void takePicture(bool saveToFile)
        {
            Utilities.Log_Debug("Taking Picture");
            _scienceData.Clear();
            Utilities.Log_Debug("Checking Look At");
            getLookingAt(true);
            Utilities.Log_Debug("Looking at: {0} celestial objects", lookingAtObjects.Count.ToString());
            for (int i=0; i < lookingAtObjects.Count; i++)
            {
                TargetableObject obj = lookingAtObjects[i];            
                Utilities.Log_Debug("Looking at {0}", obj.name);
                if (obj.type == typeof(CelestialBody))
                {
                    CelestialBody body = (CelestialBody) obj.BaseObject;
                    doScience(null, body);
                    if (TSTProgressTracker.isActive)
                    {
                        TSTProgressTracker.OnTelescopePicture(body);
                    }
                }
                else if (obj.type == typeof(TSTGalaxy))
                {
                    TSTGalaxy galaxy = (TSTGalaxy) obj.BaseObject;
                    doScience(galaxy, null);
                    if (TSTProgressTracker.isActive)
                    {
                        TSTProgressTracker.OnTelescopePicture(galaxy);
                    }
                }
            }
            Utilities.Log_Debug("Gather Science complete");
            if (lookingAtObjects.Count == 0)
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0087"), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0087 = No science collected
            }

            if (saveToFile)
            {
                Utilities.Log_Debug("Saving to File");
                int i = 0;
                while (
                    KSP.IO.File.Exists<TSTSpaceTelescope>(
                        "Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i + ".png", null) ||
                    KSP.IO.File.Exists<TSTSpaceTelescope>(
                        "Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i + "Large.png", null))
                    i++;
                _camera.saveToFile("Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i, "TeleScope");
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0049"), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0049 = Picture saved
            }
        }

        private void getTargetObjectList()
        {
            possibletargetObjects.Clear();
            for (int i = 0; i < FlightGlobals.Bodies.Count; i++)
            {
                TargetableObject tempObject = (TargetableObject)FlightGlobals.Bodies[i];
                if (tempObject != null)
                {
                    possibletargetObjects.Add(tempObject);
                }
            }
            for (int i = 0; i < TSTGalaxies.Galaxies.Count; i++)
            {
                TargetableObject tempObject = (TargetableObject)TSTGalaxies.Galaxies[i];
                if (tempObject != null)
                {
                    possibletargetObjects.Add(tempObject);
                }
            }            
        }

        private void getLookingAt(bool logmsgs = false)
        {
            lookingAtObjects.Clear();
            if (logmsgs)
            {
                Utilities.Log_Debug("getLookingAt start");
            }
            for (int i = 0; i < possibletargetObjects.Count; i++)
            {
                TargetableObject obj = possibletargetObjects[i];            
                Vector3 r = obj.position - cameraTransform.position;
                float distance = r.magnitude;
                double theta = Vector3d.Angle(cameraTransform.forward, r);
                double visibleWidth = 2*obj.size/distance*180/Mathf.PI;
                if (logmsgs)
                {
                    Utilities.Log_Debug("getLookingAt about to calc fov");
                }

                double fov = 0.05*_camera.fov;
                if (logmsgs)
                {
                    Utilities.Log_Debug("{0}: distance= {1}, theta= {2}, visibleWidth= {3}, fov= {4}", obj.name,
                        distance.ToString(), theta.ToString(), visibleWidth.ToString(), fov.ToString());
                }
                if (theta < _camera.fov/2)
                {
                    if (logmsgs)
                    {
                        Utilities.Log_Debug("Looking at: {0}", obj.name);
                    }

                    if (visibleWidth > fov)
                    {
                        if (logmsgs)
                        {
                            Utilities.Log_Debug("Can see: {0}", obj.name);
                        }

                        lookingAtObjects.Add(obj);
                    }
                }
            }            
        }
        
        public override void OnSave(ConfigNode node)
        {
            Utilities.Log_Debug("OnSave Telescope");
            foreach (ScienceData data in _scienceData)
            {
                data.Save(node.AddNode("ScienceData"));
            }
            TSTMstStgs.Instance.TSTsettings.CwindowPosX = windowPos.x;
            TSTMstStgs.Instance.TSTsettings.CwindowPosY = windowPos.y;
            TSTMstStgs.Instance.TSTsettings.BodwindowPosX = targetBodWindowPos.x;
            TSTMstStgs.Instance.TSTsettings.BodwindowPosY = targetBodWindowPos.y;
            TSTMstStgs.Instance.TSTsettings.GalwindowPosX = targetGalWindowPos.x;
            TSTMstStgs.Instance.TSTsettings.GalwindowPosY = targetGalWindowPos.y;
            Utilities.Log_Debug("OnSave Telescope End");
        }

        public override void OnLoad(ConfigNode node)
        {
            Utilities.Log_Debug("OnLoad Telescope");
            if (node.HasNode("SCIENCE"))
            {
                ConfigNode science = node.GetNode("SCIENCE");
                foreach (ConfigNode n in science.GetNodes("DATA"))
                {
                    _scienceData.Add(new ScienceData(n));
                }
            }
            foreach (ConfigNode n in node.GetNodes("ScienceData"))
            {
                _scienceData.Add(new ScienceData(n));
            }
            windowPos.x = TSTMstStgs.Instance.TSTsettings.CwindowPosX;
            windowPos.y = TSTMstStgs.Instance.TSTsettings.CwindowPosY;
            targetBodWindowPos.x = TSTMstStgs.Instance.TSTsettings.BodwindowPosX;
            targetBodWindowPos.y = TSTMstStgs.Instance.TSTsettings.BodwindowPosY;
            targetGalWindowPos.x = TSTMstStgs.Instance.TSTsettings.GalwindowPosX;
            targetGalWindowPos.y = TSTMstStgs.Instance.TSTsettings.GalwindowPosY;
            GUI_WIDTH_SMALL = TSTMstStgs.Instance.TSTsettings.TelewinSml;
            GUI_WIDTH_LARGE = TSTMstStgs.Instance.TSTsettings.TelewinLge;
            Utilities.Log_Debug("OnLoad Telescope End");
        }

        #region GUI

        private void WindowGUI(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(new GUIContent(cacheautoLOC_TST_0088, cacheautoLOC_TST_0089), GUILayout.ExpandWidth(false)); //#autoLOC_TST_0088 = Zoom #autoLOC_TST_0089 = Set Zoom level on camera
            _camera.ZoomLevel = GUILayout.HorizontalSlider(_camera.ZoomLevel, -1, maxZoom, GUILayout.ExpandWidth(true));
            GUILayout.Label(getZoomString(_camera.ZoomLevel), GUILayout.ExpandWidth(false), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            Texture2D texture2D = _camera.Texture2D;
            Rect imageRect = GUILayoutUtility.GetRect(texture2D.width, texture2D.height);
            Vector2 center = imageRect.center;
            imageRect.width = texture2D.width;
            imageRect.height = texture2D.height;
            imageRect.center = center;
            GUI.DrawTexture(imageRect, texture2D);
            Rect rect = new Rect(0, 0, 40, 40);
            if (_showTarget)
            {
                Transform cameraTransform = null;
                Transform targetTransform = null;
                if (targettingMode == TargettingMode.Planet && FlightGlobals.fetch.VesselTarget != null)
                {
                    cameraTransform = this.cameraTransform;
                    targetTransform = FlightGlobals.fetch.vesselTargetTransform;
                    Utilities.Log_Debug("showtarget cameratransform=" + cameraTransform.position + ",targettransform=" +
                                        targetTransform.position);
                }
                else if (targettingMode == TargettingMode.Galaxy && galaxyTarget != null)
                {
                    cameraTransform = _camera._scaledSpaceCam.camera.transform;
                    targetTransform = galaxyTarget.transform;
                }
                if (cameraTransform != null)
                {
                    Vector3d r = targetTransform.position - cameraTransform.position;
                    double dx = Vector3d.Dot(cameraTransform.right.normalized, r.normalized);
                    double thetax = 90 - Math.Acos(dx)*Mathf.Rad2Deg;
                    double dy = Vector3d.Dot(cameraTransform.up.normalized, r.normalized);
                    double thetay = 90 - Math.Acos(dy)*Mathf.Rad2Deg;
                    double dz = Vector3d.Dot(cameraTransform.forward.normalized, r.normalized);
                    double xpos = texture2D.width*thetax/_camera.fov;
                    double ypos = texture2D.height*thetay/_camera.fov;
                    if (dz > 0 && Math.Abs(xpos) < texture2D.width/2 && Math.Abs(ypos) < texture2D.height/2)
                    {
                        rect.center = imageRect.center + new Vector2((float) xpos, -(float) ypos);
                        GUI.DrawTexture(rect, targets[targetId++/5%targets.Count], ScaleMode.StretchToFill, true);
                    }
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0090, cacheautoLOC_TST_0091))) _camera.ZoomLevel = 0; //#autoLOC_TST_0090 = Reset Zoom #autoLOC_TST_0091 = Reset the Camera Zoom Level
            if (
                GUILayout.Button(windowState == WindowState.Small
                    ? new GUIContent(cacheautoLOC_TST_0035, cacheautoLOC_TST_0036) : 
                    new GUIContent(cacheautoLOC_TST_0037, cacheautoLOC_TST_0038))) //#autoLOC_TST_0035 = Large #autoLOC_TST_0036 = Set Large Window Size #autoLOC_TST_0037 = Small #autoLOC_TST_0038 = set Small Window Size
            {
                windowState = windowState == WindowState.Small ? WindowState.Large : WindowState.Small;
                int w = windowState == WindowState.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE;
                _camera.changeSize(w, w);
                windowPos.height = 0;
            }
            if (
                GUILayout.Button(showGalTargetsWindow ? new GUIContent(cacheautoLOC_TST_0092, cacheautoLOC_TST_0093)
                    : new GUIContent(cacheautoLOC_TST_0094, cacheautoLOC_TST_0095))) //#autoLOC_TST_0092 = Hide Galaxies #autoLOC_TST_0093 = Hide the Galaxies Window #autoLOC_TST_0094 = Show Galaxies #autoLOC_TST_0095 = Show the Galaxies Window
                showGalTargetsWindow = !showGalTargetsWindow;
            if (
                GUILayout.Button(showBodTargetsWindow
                    ? new GUIContent(cacheautoLOC_TST_0096, cacheautoLOC_TST_0097)
                    : new GUIContent(cacheautoLOC_TST_0098, cacheautoLOC_TST_0099))) //#autoLOC_TST_0096 = Hide Bodies #autoLOC_TST_0097 = Hide the Celestial Bodies Window #autoLOC_TST_0098 = Show Bodies #autoLOC_TST_0099 = Show the Celestial Bodies Window
                showBodTargetsWindow = !showBodTargetsWindow;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0100, cacheautoLOC_TST_0101))) hideGUI(); //#autoLOC_TST_0100 = Hide #autoLOC_TST_0101 = Hide this Window
            _showTarget = GUILayout.Toggle(_showTarget, new GUIContent(cacheautoLOC_TST_0102, cacheautoLOC_TST_0103)); //#autoLOC_TST_0102 = Show Target #autoLOC_TST_0103 = Show/Hide the Targeting Reticle
            _saveToFile = GUILayout.Toggle(_saveToFile,
                new GUIContent(cacheautoLOC_TST_0104, cacheautoLOC_TST_0105)); //#autoLOC_TST_0104 = Save To File #autoLOC_TST_0105 = If this is on, picture files will be saved to GameData/TarsierSpaceTech/PluginData/TarsierSpaceTech
            if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0106, cacheautoLOC_TST_0107))) //#autoLOC_TST_0106 = Take Picture #autoLOC_TST_0107 = Take a Picture with the Camera
                takePicture(_saveToFile);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void TargettingBodWindow(int windowID)
        {
            GUILayout.BeginVertical();
            BodscrollViewVector = GUILayout.BeginScrollView(BodscrollViewVector, GUILayout.Height(300),
                GUILayout.Width(GUI_WIDTH_SMALL));

            filterContractTargets = GUILayout.Toggle(filterContractTargets,
                new GUIContent(cacheautoLOC_TST_0108, cacheautoLOC_TST_0109)); //#autoLOC_TST_0108 = Show only contract targets #autoLOC_TST_0109 = If selected only targets that are the subject of a current contract will be shown
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - TSTBodies.Count = {0}", FlightGlobals.Bodies.Count));			
            int newTarget = -1;
            for (int i = 0; i < cbTargetList.Count; i++)
            {
                //If Career Game and Not Progress completed on this body skip it. 
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !TSTProgressTracker.HasTelescopeCompleted(cbTargetList[i]) && (!TSTMstStgs.Instance.isRBloaded || !RBWrapper.RBactualAPI.enabled))
                {
                    continue;
                }
                //If we are filtering the list to only contract targets and this body is not in the list skip it (career game only).
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && filterContractTargets && !contractTargets.Contains(cbTargetList[i].name))
                {
                    continue;
                }
                //If ResearchBodies is installed and enabled, check if body is researched, if not skip it.
                if (TSTMstStgs.Instance.isRBloaded && RBWrapper.RBactualAPI.enabled)
                {
                    if (!TSTMstStgs.Instance.RBCelestialBodies[cbTargetList[i]].isResearched)
                    {
                        continue;
                    }
                }
                //Now we draw the button and if they click find and set the index.
                if (GUILayout.Button(cbTargetList[i].displayName.LocalizeRemoveGender()))
                {
                    for (int j = 0; j < FlightGlobals.Bodies.Count; j++)
                    {
                        if (FlightGlobals.Bodies[j] == cbTargetList[i])
                        {
                            newTarget = j;
                            break;
                        }
                    }
                }
            }
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - newTarget = {0}", newTarget));

            if (newTarget != -1 && newTarget != selectedTargetIndex)
            {
                vessel.targetObject = null;
                FlightGlobals.fetch.SetVesselTarget(null);
                targettingMode = TargettingMode.Planet;
                selectedTargetIndex = newTarget;
                bodyTarget = FlightGlobals.Bodies[selectedTargetIndex];
                if (FlightGlobals.ActiveVessel.mainBody.name == bodyTarget.name)
                {
                    Utilities.Log_Debug("Cannot Target: {0} : {1} in it's SOI", newTarget.ToString(), bodyTarget.name);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0110", bodyTarget.displayName), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0110 = Cannot Target <<1>> as in it's SOI
                }
                else
                {
                    FlightGlobals.fetch.SetVesselTarget(bodyTarget);
                    Utilities.Log_Debug("Targetting: {0} : {1}", newTarget.ToString(), bodyTarget.name);
                    Utilities.Log_Debug("Targetting: {0} : {1}, layer= {2}", newTarget.ToString(), bodyTarget.name,
                        bodyTarget.gameObject.layer.ToString());
                    Utilities.Log_Debug("pos=" + bodyTarget.position);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0111", bodyTarget.displayName), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0111 = Target: <<1>>
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            showBodTargetsWindow = !GUILayout.Button(new GUIContent(cacheautoLOC_TST_0100, cacheautoLOC_TST_0101)); //#autoLOC_TST_0100 = Hide #autoLOC_TST_0101 = Hide this Window
            GUILayout.EndVertical();
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void TargettingGalWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GalscrollViewVector = GUILayout.BeginScrollView(GalscrollViewVector, GUILayout.Height(300),GUILayout.Width(GUI_WIDTH_SMALL));
            filterContractTargets = GUILayout.Toggle(filterContractTargets,
                new GUIContent(cacheautoLOC_TST_0108, cacheautoLOC_TST_0109)); //#autoLOC_TST_0108 = Show only contract targets #autoLOC_TST_0109 = If selected only targets that are the subject of a current contract will be shown
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - TSTGalaxies.Galaxies.Count = {0}", TSTGalaxies.Galaxies.Count));			
            int newTarget = -1;
            for (int i = 0; i < galaxyTargetList.Count; i++)
            {
                //If Career Game and Not Progress completed on this body skip it. 
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && !TSTProgressTracker.HasTelescopeCompleted(galaxyTargetList[i]) && (!TSTMstStgs.Instance.isRBloaded || !RBWrapper.RBactualAPI.enabled))
                {
                    continue;
                }
                //If we are filtering the list to only contract targets and this body is not in the list skip it (career game only).
                if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER && filterContractTargets && !contractTargets.Contains(galaxyTargetList[i].name))
                {
                    continue;
                }                
                //If ResearchBodies is installed and enabled, check if body is researched, if not skip it.
                if (TSTMstStgs.Instance.isRBloaded && RBWrapper.RBactualAPI.enabled)
                {
                    if (!TSTMstStgs.Instance.RBCelestialBodies[galaxyTargetList[i]].isResearched)
                    {
                        continue;
                    }
                }
                //Now we draw the button and if they click find and set the index.
                if (GUILayout.Button(galaxyTargetList[i].displayName.LocalizeRemoveGender()))
                {
                    for (int j = 0; j < FlightGlobals.Bodies.Count; j++)
                    {
                        if (FlightGlobals.Bodies[j] == cbTargetList[i])
                        {
                            newTarget = j;
                            break;
                        }
                    }
                }
            }
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - newTarget = {0}", newTarget));

            if (newTarget != -1 && newTarget != selectedTargetIndex)
            {
                vessel.targetObject = null;
                FlightGlobals.fetch.SetVesselTarget(null);
                targettingMode = TargettingMode.Galaxy;
                selectedTargetIndex = newTarget;
                galaxyTarget = TSTGalaxies.Galaxies[selectedTargetIndex];
                FlightGlobals.fetch.SetVesselTarget(galaxyTarget);
                Utilities.Log_Debug("Targetting: {0} : {1},layer= {2},scaledpos= {3}", newTarget.ToString(),
                    galaxyTarget.name, galaxyTarget.gameObject.layer.ToString(), galaxyTarget.scaledPosition.ToString());
                Utilities.Log_Debug("pos= {0}", galaxyTarget.position.ToString());
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0111", galaxyTarget.displayName), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0111 = Target: <<1>>
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            showGalTargetsWindow = !GUILayout.Button(new GUIContent(cacheautoLOC_TST_0100, cacheautoLOC_TST_0101)); //#autoLOC_TST_0100 = Hide #autoLOC_TST_0101 = Hide this Window
            GUILayout.EndVertical();
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }


        public void OnGUI()
        {
            if (Time.timeSinceLevelLoad < 2f) return;
            if (Utilities.GameModeisFlight)
            {
                if (!_inEditor && _camera != null && !Utilities.isPauseMenuOpen)
                {
                    if (!Textures.StylesSet) Textures.SetupStyles();

                    if (!MapView.MapIsEnabled && _camera.Enabled &&
                        windowState != WindowState.Hidden
                        && vessel.isActiveVessel && !Utilities.isPauseMenuOpen)
                    {
                        getLookingAt();
                        windowPos = GUILayout.Window(CAMwindowID, windowPos, WindowGUI, "Space Telescope",
                            GUILayout.Width(windowState == WindowState.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE),
                            GUILayout.Height(windowState == WindowState.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE));
                        if (showGalTargetsWindow)
                            targetGalWindowPos = GUILayout.Window(GALwindowID, targetGalWindowPos, TargettingGalWindow,
                                "Select Target", GUILayout.Width(GUI_WIDTH_SMALL));
                        if (showBodTargetsWindow)
                            targetBodWindowPos = GUILayout.Window(BODwindowID, targetBodWindowPos, TargettingBodWindow,
                                "Select Target", GUILayout.Width(GUI_WIDTH_SMALL));

                        if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                            Utilities.DrawToolTip();
                    }
                }
            }
        }

        public void hideGUI()
        {
            windowState = WindowState.Hidden;
            _camera.Enabled = false;
            Events["eventShowGUI"].active = true;
        }

        [KSPEvent(active = true, guiActive = true, name = "eventShowGUI", guiName = "#autoLOC_TST_0114")] //#autoLOC_TST_0114 = Show GUI
        public void eventShowGUI()
        {
            Events["eventShowGUI"].active = false;
            windowState = WindowState.Small;
            _camera.Enabled = true;
        }

        #endregion GUI

        #region Science

        private void updateAvailableEvents()
        {
            if (_scienceData.Count > 0)
            {
                Events["eventReviewScience"].active = true;
                Events["CollectScience"].active = true;
            }
            else
            {
                Events["eventReviewScience"].active = false;
                Events["CollectScience"].active = false;
            }
        }

        private void doScience(TSTGalaxy galaxy, CelestialBody planet)
        {
            
            ScienceExperiment experiment;
            ScienceSubject subject;
            ScienceData data;
            ExperimentSituations situation = ScienceUtil.GetExperimentSituation(vessel);            
            string tgtName;
            string tgtdisplayName;
            try
            {                
                experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
                if (experiment == null)
                {
                    Utilities.Log("Unable to find experiment TarsierSpaceTech.SpaceTelescope, Are you missing a config file? Report on forums.");
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0115"), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0115 = TST Unable to find Experiment - Internal Failure. See Log.
                    return;
                }
                Utilities.Log_Debug("Got experiment");
                if (galaxy != null)
                {
                    tgtName = galaxy.name;
                    tgtdisplayName = galaxy.displayName;
                }
                else
                {
                    tgtName = planet.name;
                    tgtdisplayName = planet.displayName;
                }
                Utilities.Log_Debug("Doing Science for {0}", tgtName);                

                if (experiment.IsAvailableWhile(situation, vessel.mainBody))
                {
                    subject = ResearchAndDevelopment.GetExperimentSubject(experiment, situation, vessel.mainBody, "LookingAt" + tgtName, "");
                    subject.title = Localizer.Format("#autoLOC_TST_0116", tgtdisplayName); //#autoLOC_TST_0116 = Space Telescope picture of <1>>
                    Utilities.Log_Debug("Got subject, determining science data using {0}", part.name);

                    if (part.name == "tarsierSpaceTelescope")
                    {
                        if (galaxy != null)
                        {
                            data = new ScienceData(experiment.baseValue/2*subject.dataScale, xmitDataScalar,labBoostScalar,subject.id, subject.title, false, part.flightID);
                        }
                        else
                        {
                            data = new ScienceData(experiment.baseValue * 0.8f * subject.dataScale, xmitDataScalar,labBoostScalar, subject.id, subject.title, false, part.flightID);
                        }
                    }
                    else
                    {
                        if (galaxy != null)
                        {
                            data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar,subject.id, subject.title, false, part.flightID);
                        }
                        else
                        {
                            data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar,subject.id, subject.title, false, part.flightID);
                        }
                    }
                    Utilities.Log_Debug("Got data");
                    data.title = Localizer.Format("#autoLOC_TST_0117", vessel.mainBody.displayName, tgtdisplayName); //#autoLOC_TST_0117 = Tarsier Space Telescope: Orbiting <<1>> looking at <<2>>
                    _scienceData.Add(data);
                    Utilities.Log_Debug("Added Data Amt= {0}, TransmitValue= {1}, LabBoost= {2}, LabValue= {3}",
                        data.dataAmount.ToString(), data.baseTransmitValue.ToString(), data.transmitBonus.ToString(),
                        data.labValue.ToString());
                    //If ResearchBodies is installed check if body is already found or not, if it isn't change the screen message to say "Unknown Body"
                    if (isRBInstalled && RBWrapper.RBactualAPI.enabled)
                    {
                        KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo> keyvalue = new KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo>();
                        foreach (KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo> kvp in TSTMstStgs.Instance.RBCelestialBodies)
                        {
                            if (kvp.Key.name == tgtName)
                            {
                                keyvalue = kvp;
                                break;
                            }
                        }
                        if (keyvalue.Key != null && keyvalue.Value != null)
                        {
                            if (!keyvalue.Value.isResearched)
                            {
                                tgtdisplayName = Localizer.Format("#autoLOC_TST_0118"); //#autoLOC_TST_0118 = Unknown Body
                            }
                        }
                    }
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_238419", part.partInfo.title, data.dataAmount.ToString(), subject.title), 8f, ScreenMessageStyle.UPPER_LEFT);
                    ReviewDataItem(data);                    
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_238424", experiment.experimentTitle), 5f, ScreenMessageStyle.UPPER_CENTER);
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0119", situation.displayDescription()), 5f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0119 = Cannot take picture whilst <<1>>
                }
            }
            catch (Exception ex)
            {
                if (galaxy != null)
                    Utilities.Log("An error occurred attempting to capture science of {0}", galaxy.name);
                else if (planet != null)
                {
                    Utilities.Log("An error occurred attempting to capture science of {0}", planet.name);
                }
                else
                {
                    Utilities.Log("An error occurred attempting to capture science");
                }
                Utilities.Log("Err: " + ex);
                //throw;
            }
        }
          
        private string getZoomString(float zoom)
        {
            string[] unicodePowers =
            {
                "\u2070", "\u00B9", "\u00B2", "\u00B3", "\u2074", "\u2075", "\u2076", "\u2077",
                "\u2078", "\u2079"
            };
            string zStr = "x";
            float z = Mathf.Pow(10, zoom);
            float magnitude = Mathf.Pow(10, Mathf.Floor(zoom));
            float msf = Mathf.Floor(z/magnitude);
            if (zoom >= 3)
            {
                zStr += msf + "x10" + unicodePowers[Mathf.FloorToInt(zoom)];
            }
            else
            {
                zStr += (msf*magnitude).ToString();
            }
            return zStr;
        }

        public override string GetInfo()
        {
            string infoStr = "";
            infoStr += Localizer.Format("#autoLOC_TST_0120", XKCDColors.HexFormat.Cyan); //#autoLOC_TST_0120 = <color=<<1>>>Space Telescope</color>\nCan take pictures of heavenly bodies in the sky.\n
            infoStr += Localizer.Format("#autoLOC_TST_0121", maxZoom); //#autoLOC_TST_0121 = Maximum Camera Zoom: <<1>>
            infoStr += "\n\n";
            infoStr += Localizer.Format("#autoLOC_TST_0122", XKCDColors.HexFormat.Cyan); //#autoLOC_TST_0122 = <color=<<1>>>Space Telescope Picture</color>\n
            infoStr += Localizer.Format("#autoLOC_238797", true);    
            infoStr += Localizer.Format("#autoLOC_238798", true);    
            infoStr += Localizer.Format("#autoLOC_238799", true);
            return infoStr;
        }

        public enum WindowState
        {
            Small,
            Large,
            Hidden
        }

        [KSPEvent(active = false, guiActive = true, name = "eventReviewScience", guiName = "#autoLOC_TST_0050")] //#autoLOC_TST_0050 = Check Results
        public void eventReviewScience()
        {
            foreach (ScienceData data in _scienceData)
            {
                ReviewDataItem(data);
                updateAvailableEvents();
            }
        }

        private void _onPageDiscard(ScienceData data)
        {
            _scienceData.Remove(data);
            updateAvailableEvents();
        }

        private void _onPageKeep(ScienceData data)
        {
        }

        private void _onPageTransmit(ScienceData data)
        {
            IScienceDataTransmitter transmitter = ScienceUtil.GetBestTransmitter(vessel);

            if (transmitter != null)
            {
                List<ScienceData> dataToSend = new List<ScienceData>();
                dataToSend.Add(data);
                transmitter.TransmitData(dataToSend);
                _scienceData.Remove(data);
                updateAvailableEvents();
            }
            else
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0079"), 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        [KSPEvent(active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "#autoLOC_TST_0044",unfocusedRange = 2)] //#autoLOC_TST_0044 = Collect Data
        public void CollectScience()
        {
            List<ModuleScienceContainer> containers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            foreach (ModuleScienceContainer container in containers)
            {
                if (_scienceData.Count > 0)
                {
                    if (container.StoreData(new List<IScienceDataContainer> { this }, false))
                    {
                        //ScreenMessages.PostScreenMessage("Transferred Data to " + vessel.vesselName, 3f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0045", part.partInfo.title), 5f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0045 = <color=#99ff00ff>[<<1>>]: All Items Collected.</color>
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0046", part.partInfo.title), 5f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0046 = <color=orange>[<<1>>]: Not all items could be Collected.</color>
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0047", part.partInfo.title), 3f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0047 = <color=#99ff00ff>[<<1>>]: Nothing to Collect.</color>
                }
            }
            updateAvailableEvents();
        }

        private void _onPageSendToLab(ScienceData data)
        {
            ScienceLabSearch scienceLabSearch = new ScienceLabSearch(base.vessel, data);
            if (scienceLabSearch.NextLabForDataFound)
            {
                StartCoroutine(scienceLabSearch.NextLabForData.ProcessData(data, new Callback<ScienceData>(DumpData)));
            }
            else
            {
                scienceLabSearch.PostErrorToScreen();
            }
            updateAvailableEvents();
        }


        // IScienceDataContainer
        public void DumpData(ScienceData data)
        {
            _scienceData.Remove(data);
            updateAvailableEvents();
        }

        public ScienceData[] GetData()
        {
            return _scienceData.ToArray();
        }

        public int GetScienceCount()
        {
            return _scienceData.Count;
        }

        public void ReviewData()
        {
            eventReviewScience();
        }

        public void ReturnData(ScienceData data)
        {
            if (data == null)
            {
                return;
            }
            _scienceData.Add(data);
        }

        public bool IsRerunnable()
        {
            Utilities.Log_Debug("Is rerunnable");
            return true;
        }

        public void ReviewDataItem(ScienceData data)
        {
            ScienceLabSearch labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                part,
                data,
                xmitDataScalar,
                data.transmitBonus,
                false,
                "",
                true,
                labSearch,
                _onPageDiscard,
                _onPageKeep,
                _onPageTransmit,
                _onPageSendToLab);
            //If ResearchBodies is installed we check if the body is discovered or not. If it isn't we change the science results page text.
            if (isRBInstalled && RBWrapper.RBactualAPI.enabled)
            {
                if (data.subjectID.Contains("TarsierSpaceTech.SpaceTelescope"))
                {                    
                    int index = data.subjectID.IndexOf("LookingAt");
                    if (index != -1)
                    {
                        string[] tmpIDelements = data.subjectID.Split('@');
                        string[] valuesasarray = { "LookingAt" };
                        string[] splitvars = tmpIDelements[1].Split(valuesasarray, StringSplitOptions.None);
                        string bodyName = splitvars[1];  
                        KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo> keyvalue = new KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo>();
                        foreach (KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo> kvp in TSTMstStgs.Instance.RBCelestialBodies)
                        {
                            if (kvp.Key.name == bodyName)
                            {
                                keyvalue = kvp;
                                break;
                            }
                        }
                        if (keyvalue.Key != null && keyvalue.Value != null)
                        {
                            if (!keyvalue.Value.isResearched)
                            {
                                page.resultText = Localizer.Format("#autoLOC_TST_0123"); //#autoLOC_TST_0123 = We have collected a picture of a previously unknown body. If we return or transmit this data to home we will learn more about it.
                                page.title = Localizer.Format("#autoLOC_TST_0124"); //#autoLOC_TST_0124 = Space Telescope picture of Unknown Body
                            }
                        }
                    }                    
                }
            }
            ExperimentsResultDialog.DisplayResult(page);
        }

        #endregion Science

        #region Galaxy

        //Galaxy Wrapper
        public enum TargettingMode
        {
            Galaxy,
            Planet
        }

        public class TargetableObject
        {
            private readonly CelestialBody body;
            private readonly TSTGalaxy galaxy;

            private TargetableObject(TSTGalaxy galaxy)
            {
                this.galaxy = galaxy;
            }

            private TargetableObject(CelestialBody body)
            {
                this.body = body;
            }

            public Type type
            {
                get { return galaxy == null ? typeof(CelestialBody) : typeof(TSTGalaxy); }
            }

            public object BaseObject
            {
                get { return galaxy == null ? body : (object) galaxy; }
            }

            public Vector3 position
            {
                get { return galaxy == null ? body.transform.position : galaxy.position; }
            }

            public double size
            {
                get { return galaxy == null ? body.Radius : galaxy.size; }
            }

            public string name
            {
                get { return galaxy == null ? body.name : galaxy.name; }
            }

            public string displayName
            {
                get { return galaxy == null ? body.displayName : galaxy.displayName; }
            }

            public static implicit operator TargetableObject(TSTGalaxy galaxy)
            {
                if (galaxy != null)
                    return new TargetableObject(galaxy);
                return null;
            }

            public static implicit operator TargetableObject(CelestialBody body)
            {
                if (body != null)
                    return new TargetableObject(body);
                return null;
            }
        }

        #endregion Galaxy
    }
}