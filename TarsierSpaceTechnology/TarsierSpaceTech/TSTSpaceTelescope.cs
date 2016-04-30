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
using System.IO;
using System.Linq;
using Contracts;
using KSP.UI.Screens.Flight.Dialogs;
using RSTUtils;
using UnityEngine;
using Resources = TarsierSpaceTech.Properties.Resources;

namespace TarsierSpaceTech
{
    public class TSTSpaceTelescope : PartModule, IScienceDataContainer
    {
        private static int CAMwindowID = 5955555;
        private static int GALwindowID = 5955556;
        private static int BODwindowID = 5955557;

        private static readonly List<byte[]> targets_raw = new List<byte[]>
        {
            Resources.target_01,
            Resources.target_02,
            Resources.target_03,
            Resources.target_04,
            Resources.target_05,
            Resources.target_06,
            Resources.target_07,
            Resources.target_08,
            Resources.target_09,
            Resources.target_10,
            Resources.target_11,
            Resources.target_12
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

        [KSPField(isPersistant = true)] public bool Active;

        [KSPField] public string animationClipNameClose = "close";

        [KSPField] public string animationClipNameOpen = "open";

        [KSPField] public string animationNameClose = "";

        [KSPField] public string animationNameOpen = "";

        [KSPField] public string animationTransformName = "Telescope";

        [KSPField] public string baseTransformName = "Telescope";

        private Vector2 BodscrollViewVector = Vector2.zero;
        private CelestialBody bodyTarget;

        [KSPField] public string cameraTransformName = "CameraTransform";

        private bool filterContractTargets;
        private TSTGalaxy galaxyTarget;
        private Vector2 GalscrollViewVector = Vector2.zero;
        private int GUI_WIDTH_LARGE = 600;
        private int GUI_WIDTH_SMALL = 320;
        private bool isRBactive;

        [KSPField] public float labBoostScalar = 0f;

        [KSPField] public string lookTransformName = "LookTransform";

        [KSPField(guiActive = false, guiName = "maxZoom", isPersistant = true)] public int maxZoom = 5;

        public float PIDKd = 0.5f;
        public float PIDKi = 6f;
        public float PIDKp = 12f;
        private RBWrapper.ModuleTrackBodies RBmoduleTrackBodies;
        private Dictionary<CelestialBody, int> ResearchState = new Dictionary<CelestialBody, int>();
        private int selectedTargetIndex = -1;

        [KSPField] public bool servoControl = true;

        private bool showBodTargetsWindow;
        private bool showGalTargetsWindow;
        private Rect targetBodWindowPos = new Rect(512, 128, 0, 0);
        private Rect targetGalWindowPos = new Rect(512, 128, 0, 0);

        private int targetId;

        public TargettingMode targettingMode = TargettingMode.Galaxy;
        private Dictionary<CelestialBody, bool> TrackedBodies = new Dictionary<CelestialBody, bool>();

        private Rect windowPos = new Rect(128, 128, 0, 0);
        public WindowState windowState = WindowState.Small;

        [KSPField] public float xmitDataScalar = 0.5f;

        private Quaternion zeroRotation;

        public Transform cameraTransform { get; private set; }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Utilities.Log_Debug("TSTTel Starting Telescope");
            part.CoMOffset = part.attachNodes[0].position;
            if (state == StartState.Editor)
            {
                _inEditor = true;
                return;
            }
            _baseTransform = Utilities.FindChildRecursive(transform, baseTransformName);
            cameraTransform = Utilities.FindChildRecursive(transform, cameraTransformName);
            _lookTransform = Utilities.FindChildRecursive(transform, lookTransformName);
            _animationTransform = Utilities.FindChildRecursive(transform, animationTransformName);
            Utilities.PrintTransform(_baseTransform, "_basetransform");
            Utilities.PrintTransform(cameraTransform, "_cameratransform");
            Utilities.PrintTransform(_lookTransform, "_looktransform");
            Utilities.Log_Debug("part.CoMOffset=" + part.CoMOffset);
            zeroRotation = cameraTransform.localRotation;
            Utilities.Log_Debug("zeroRotation=" + zeroRotation);
            _camera = cameraTransform.gameObject.AddComponent<TSTCameraModule>();
            //_animation = _baseTransform.animation;
            _animationOpen = animationNameOpen == "" ? _animationTransform.GetComponent<Animation>() : Utilities.FindAnimChildRecursive(_animationTransform, animationNameOpen);
            _animationClose = animationNameClose == "" ? _animationTransform.GetComponent<Animation>() : Utilities.FindAnimChildRecursive(_animationTransform, animationNameClose);
            if (!Active) //camera is not active on startup
            {
                Events["eventOpenCamera"].active = true;
                Events["eventCloseCamera"].active = false;
                Events["eventShowGUI"].active = false;
            }
            else //Camera is active on startup
            {
                Events["eventOpenCamera"].active = false;
                Events["eventCloseCamera"].active = true;
                Events["eventShowGUI"].active = true;
            }
            Events["eventControlFromHere"].active = false;
            Events["eventReviewScience"].active = false;

            for (var i = 0; i < targets_raw.Count; i++)
            {
                var tex = new Texture2D(40, 40);
                tex.LoadImage(targets_raw[i]);
                targets.Add(tex);
            }
            Utilities.Log_Debug("TSTTel Getting ExpIDs");
            foreach (var expID in ResearchAndDevelopment.GetExperimentIDs())
            {
                Utilities.Log_Debug("TSTTel Got ExpID: " + expID);
            }
            Utilities.Log_Debug("TSTTel Got ExpIDs");
            CAMwindowID = Utilities.getnextrandomInt();
            GALwindowID = Utilities.getnextrandomInt();
            BODwindowID = Utilities.getnextrandomInt();
            Utilities.Log_Debug("TSTTel On end start");
            StartCoroutine(setSASParams());
            Utilities.Log_Debug("TSTTel Adding Input Callback");
            _vessel = vessel;
            _vessel.OnAutopilotUpdate += onFlightInput;
            GameEvents.onVesselChange.Add(refreshFlightInputHandler);
            GameEvents.onVesselDestroy.Add(removeFlightInputHandler);
            GameEvents.OnVesselRecoveryRequested.Add(removeFlightInputHandler);
            Utilities.Log_Debug("TSTTel Added Input Callback");
            if (Active)
                StartCoroutine(openCamera());
            //If ResearchBodies is installed, initialise the PartModulewrapper and get the TrackedBodies dictionary item.
            isRBactive = Utilities.IsResearchBodiesInstalled;
            if (isRBactive)
            {
                RBWrapper.InitRBDBWrapper();
                RBWrapper.InitRBFLWrapper();
                if (RBWrapper.APIFLReady)
                {
                    foreach (PartModule module in part.Modules)
                    {
                        if (module.moduleName == "ModuleTrackBodies")
                        {
                            RBmoduleTrackBodies = new RBWrapper.ModuleTrackBodies(module);
                            TrackedBodies = RBmoduleTrackBodies.TrackedBodies;
                            ResearchState = RBmoduleTrackBodies.ResearchState;

                            if (File.Exists("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg"))
                            {
                                var mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                                foreach (var cb in TSTGalaxies.CBGalaxies)
                                {
                                    var fileContainsGalaxy = false;
                                    foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                                    {
                                        if (cb.bodyName.Contains(node.GetValue("body")))
                                        {
                                            if (bool.Parse(node.GetValue("ignore")))
                                            {
                                                TrackedBodies[cb] = true;
                                                ResearchState[cb] = 100;
                                            }
                                            else
                                            {
                                                TrackedBodies[cb] = bool.Parse(node.GetValue("isResearched"));
                                                if (node.HasValue("researchState"))
                                                {
                                                    ResearchState[cb] = int.Parse(node.GetValue("researchState"));
                                                }
                                                else
                                                {
                                                    ConfigNode cbNode = null;
                                                    foreach (
                                                        ConfigNode cbSettingNode in
                                                            mainnode.GetNode("RESEARCHBODIES").nodes)
                                                    {
                                                        if (cbSettingNode.GetValue("body") == cb.GetName())
                                                            cbNode = cbSettingNode;
                                                    }
                                                    if (cbNode != null) cbNode.AddValue("researchState", "0");
                                                    mainnode.Save("saves/" + HighLogic.SaveFolder +
                                                                  "/researchbodies.cfg");
                                                    ResearchState[cb] = 0;
                                                }
                                            }
                                            fileContainsGalaxy = true;
                                        }
                                    }
                                    if (!fileContainsGalaxy)
                                    {
                                        var newNodeForCB = mainnode.GetNode("RESEARCHBODIES").AddNode("BODY");
                                        newNodeForCB.AddValue("body", cb.GetName());
                                        newNodeForCB.AddValue("isResearched", "false");
                                        newNodeForCB.AddValue("researchState", "0");
                                        newNodeForCB.AddValue("ignore", "false");
                                        TrackedBodies[cb] = false;
                                        ResearchState[cb] = 0;
                                        mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            }
        }

        private IEnumerator setSASParams()
        {
            while (FlightGlobals.ActiveVessel.Autopilot.RSAS.pidPitch == null)
                yield return null;
            Utilities.Log_Debug("Setting PIDs");
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidPitch.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidRoll.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidYaw.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
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
            Events["toggleServos"].guiName = servoControl ? "Disable Servos" : "Enable Servos";
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
                var vpm = _vessel.FindPartModulesImplementing<TSTSpaceTelescope>();
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

                var angle = Mathf.Abs(Quaternion.Angle(cameraTransform.localRotation, zeroRotation));

                if (angle > 1.5f)
                {
                    cameraTransform.localRotation = Quaternion.Slerp(zeroRotation, cameraTransform.localRotation,
                        1.5f/angle);
                }
            }
        }

        public override void OnUpdate()
        {
            Events["eventReviewScience"].active = _scienceData.Count > 0;
            if (vessel.targetObject != _lastTarget && vessel.targetObject != null)
            {
                targettingMode = TargettingMode.Planet;
                selectedTargetIndex = -1;
                _lastTarget = vessel.targetObject;
            }

            if (vessel.targetObject != null)
            {
                Utilities.Log_Debug("Vessel target=" + vessel.targetObject.GetTransform().position);
            }

            //if (!_inEditor && _camera.Enabled && windowState != WindowSate.Hidden && vessel.isActiveVessel)
            //{                
            //if (_camera.Enabled && f++ % frameLimit == 0)                                   
            //_camera.draw();                
            //}
        }

        [KSPEvent(active = true, guiActive = true, name = "eventOpenCamera", guiName = "Open Camera")]
        public void eventOpenCamera()
        {
            Events["eventOpenCamera"].active = false;
            StartCoroutine(openCamera());
        }

        [KSPAction("OpenCamera")]
        public void ActivateAction(KSPActionParam param)
        {
            eventOpenCamera();
        }

        public IEnumerator openCamera()
        {
            if (_animationOpen != null)
            {
                if (animationClipNameOpen == "")
                {
                    _animationOpen.Play();
                    var wait = Utilities.WaitForAnimationNoClip(_animationOpen);
                    while (wait.MoveNext()) yield return null;
                }
                else
                {
                    _animationOpen.Play(animationClipNameOpen);
                    var wait = Utilities.WaitForAnimation(_animationOpen, animationClipNameOpen);
                    while (wait.MoveNext()) yield return null;
                }
            }
            Events["eventCloseCamera"].active = true;
            Events["eventControlFromHere"].active = true;
            Events["toggleServos"].active = true;
            _camera.Enabled = true;
            Active = true;
            windowState = WindowState.Small;
            _camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL);
            cameraTransform.localRotation = zeroRotation;
        }

        [KSPEvent(active = false, guiActive = true, name = "eventCloseCamera", guiName = "Close Camera")]
        public void eventCloseCamera()
        {
            Events["eventShowGUI"].active = false;
            Events["eventCloseCamera"].active = false;
            Events["eventControlFromHere"].active = false;
            Events["toggleServos"].active = false;
            _camera.Enabled = false;
            Active = false;
            StartCoroutine(closeCamera());
            if (vessel.ReferenceTransform == _lookTransform)
            {
                vessel.FallBackReferenceTransform();
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
                    var wait = Utilities.WaitForAnimationNoClip(_animationClose);
                    while (wait.MoveNext()) yield return null;
                }
                else
                {
                    _animationClose.Play(animationClipNameClose);
                    var wait = Utilities.WaitForAnimation(_animationClose, animationClipNameClose);
                    while (wait.MoveNext()) yield return null;
                }
            }
            Events["eventOpenCamera"].active = true;
        }

        [KSPEvent(active = false, guiActive = true, name = "eventControlFromHere", guiName = "Control From Here")]
        public void eventControlFromHere()
        {
            part.SetReferenceTransform(_lookTransform);
            vessel.SetReferenceTransform(part);
        }

        public void takePicture(bool saveToFile)
        {
            Utilities.Log_Debug("Taking Picture");
            _scienceData.Clear();
            Utilities.Log_Debug("Checking Look At");
            var objs = getLookingAt();
            Utilities.Log_Debug("Looking at: {0} celestial objects", objs.Count.ToString());
            foreach (var obj in objs)
            {
                Utilities.Log_Debug("Looking at {0}", obj.theName);
                if (obj.type == typeof(CelestialBody))
                {
                    var body = (CelestialBody) obj.BaseObject;
                    doScience(body);
                    if (TSTProgressTracker.isActive)
                    {
                        TSTProgressTracker.OnTelescopePicture(body);
                    }
                }
                else if (obj.type == typeof(TSTGalaxy))
                {
                    var galaxy = (TSTGalaxy) obj.BaseObject;
                    doScience(galaxy);
                    if (TSTProgressTracker.isActive)
                    {
                        TSTProgressTracker.OnTelescopePicture(galaxy);
                    }
                }
            }
            Utilities.Log_Debug("Gather Science complete");
            if (objs.Count == 0)
            {
                ScreenMessages.PostScreenMessage("No science collected", 3f, ScreenMessageStyle.UPPER_CENTER);
            }

            if (saveToFile)
            {
                Utilities.Log_Debug("Saving to File");
                var i = 0;
                while (
                    KSP.IO.File.Exists<TSTSpaceTelescope>(
                        "Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i + ".png", null) ||
                    KSP.IO.File.Exists<TSTSpaceTelescope>(
                        "Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i + "Large.png", null))
                    i++;
                _camera.saveToFile("Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i, "TeleScope");
                ScreenMessages.PostScreenMessage("Picture saved", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }


        private List<TargetableObject> getLookingAt()
        {
            var result = new List<TargetableObject>();
            var bodies = FlightGlobals.Bodies.Select(b => (TargetableObject) b).ToList();
            var galaxies = TSTGalaxies.Galaxies.Select(g => (TargetableObject) g).ToList();
            Utilities.Log_Debug("getLookingAt start");
            foreach (var obj in galaxies.Concat(bodies))
            {
                var r = obj.position - cameraTransform.position;
                var distance = r.magnitude;
                var theta = Vector3d.Angle(cameraTransform.forward, r);
                var visibleWidth = 2*obj.size/distance*180/Mathf.PI;
                Utilities.Log_Debug("getLookingAt about to calc fov");
                var fov = 0.05*_camera.fov;
                Utilities.Log_Debug("{0}: distance= {1}, theta= {2}, visibleWidth= {3}, fov= {4}", obj.theName,
                    distance.ToString(), theta.ToString(), visibleWidth.ToString(), fov.ToString());
                if (theta < _camera.fov/2)
                {
                    Utilities.Log_Debug("Looking at: {0}", obj.theName);
                    if (visibleWidth > fov)
                    {
                        Utilities.Log_Debug("Can see: {0}", obj.theName);
                        result.Add(obj);
                    }
                }
            }
            return result;
        }

        public override void OnSave(ConfigNode node)
        {
            Utilities.Log_Debug("OnSave Telescope");
            foreach (var data in _scienceData)
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
                var science = node.GetNode("SCIENCE");
                foreach (var n in science.GetNodes("DATA"))
                {
                    _scienceData.Add(new ScienceData(n));
                }
            }
            foreach (var n in node.GetNodes("ScienceData"))
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
            GUILayout.Label(new GUIContent("Zoom", "Set Zoom level on camera"), GUILayout.ExpandWidth(false));
            _camera.ZoomLevel = GUILayout.HorizontalSlider(_camera.ZoomLevel, -1, maxZoom, GUILayout.ExpandWidth(true));
            GUILayout.Label(getZoomString(_camera.ZoomLevel), GUILayout.ExpandWidth(false), GUILayout.Width(60));
            GUILayout.EndHorizontal();
            var texture2D = _camera.Texture2D;
            var imageRect = GUILayoutUtility.GetRect(texture2D.width, texture2D.height);
            var center = imageRect.center;
            imageRect.width = texture2D.width;
            imageRect.height = texture2D.height;
            imageRect.center = center;
            GUI.DrawTexture(imageRect, texture2D);
            var rect = new Rect(0, 0, 40, 40);
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
                    cameraTransform = _camera._skyBoxCam.camera.transform;
                    targetTransform = galaxyTarget.transform;
                }
                if (cameraTransform != null)
                {
                    Vector3d r = targetTransform.position - cameraTransform.position;
                    var dx = Vector3d.Dot(cameraTransform.right.normalized, r.normalized);
                    var thetax = 90 - Math.Acos(dx)*Mathf.Rad2Deg;
                    var dy = Vector3d.Dot(cameraTransform.up.normalized, r.normalized);
                    var thetay = 90 - Math.Acos(dy)*Mathf.Rad2Deg;
                    var dz = Vector3d.Dot(cameraTransform.forward.normalized, r.normalized);
                    var xpos = texture2D.width*thetax/_camera.fov;
                    var ypos = texture2D.height*thetay/_camera.fov;
                    if (dz > 0 && Math.Abs(xpos) < texture2D.width/2 && Math.Abs(ypos) < texture2D.height/2)
                    {
                        rect.center = imageRect.center + new Vector2((float) xpos, -(float) ypos);
                        GUI.DrawTexture(rect, targets[targetId++/5%targets.Count], ScaleMode.StretchToFill, true);
                    }
                }
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Reset Zoom", "Reset the Camera Zoom Level"))) _camera.ZoomLevel = 0;
            if (
                GUILayout.Button(windowState == WindowState.Small
                    ? new GUIContent("Large", "Set Large Window Size")
                    : new GUIContent("Small", "set Small Window Size")))
            {
                windowState = windowState == WindowState.Small ? WindowState.Large : WindowState.Small;
                var w = windowState == WindowState.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE;
                _camera.changeSize(w, w);
                windowPos.height = 0;
            }
            if (
                GUILayout.Button(showGalTargetsWindow
                    ? new GUIContent("Hide Galaxies", "Hide the Galaxies Window")
                    : new GUIContent("Show Galaxies", "Show the Galaxies Window")))
                showGalTargetsWindow = !showGalTargetsWindow;
            if (
                GUILayout.Button(showBodTargetsWindow
                    ? new GUIContent("Hide Bodies", "Hide the Celestial Bodies Window")
                    : new GUIContent("Show Bodies", "Show the Celestial Bodies Window")))
                showBodTargetsWindow = !showBodTargetsWindow;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Hide", "Hide this Window"))) hideGUI();
            _showTarget = GUILayout.Toggle(_showTarget, new GUIContent("Show Target", "Show/Hide the Targeting Reticle"));
            _saveToFile = GUILayout.Toggle(_saveToFile,
                new GUIContent("Save To File",
                    "If this is on, picture files will be saved to GameData/TarsierSpaceTech/PluginData/TarsierSpaceTech"));
            if (GUILayout.Button(new GUIContent("Take Picture", "Take a Picture with the Camera")))
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
                new GUIContent("Show only contract targets",
                    "If selected only targets that are the subject of a current contract will be shown"));
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - TSTBodies.Count = {0}", FlightGlobals.Bodies.Count));			
            var newTarget = 0;
            if (isRBactive && RBWrapper.APIDBReady && RBWrapper.APIFLReady && RBmoduleTrackBodies.enabled)
            {
                var filterRBTargets = isRBactive;
                newTarget = FlightGlobals.Bodies.
                    FindIndex(
                        g => TSTProgressTracker.HasTelescopeCompleted(g) ||
                             (ContractSystem.Instance &&
                              ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>()
                                  .Any(t => t.target.name == g.name))
                            ? GUILayout.Button(g.theName)
                            : (filterContractTargets
                                ? false
                                : (RBmoduleTrackBodies.TrackedBodies[g] ? GUILayout.Button(g.theName) : false)));
            }
            else
            {
                newTarget = FlightGlobals.Bodies.
                    FindIndex(
                        g => TSTProgressTracker.HasTelescopeCompleted(g) ||
                             (ContractSystem.Instance &&
                              ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>()
                                  .Any(t => t.target.name == g.name))
                            ? GUILayout.Button(g.theName)
                            : (filterContractTargets ? false : GUILayout.Button(g.theName)));
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
                    ScreenMessages.PostScreenMessage("Cannot Target " + bodyTarget.theName + " as in it's SOI", 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                else
                {
                    FlightGlobals.fetch.SetVesselTarget(bodyTarget);
                    Utilities.Log_Debug("Targetting: {0} : {1}", newTarget.ToString(), bodyTarget.name);
                    Utilities.Log_Debug("Targetting: {0} : {1}, layer= {2}", newTarget.ToString(), bodyTarget.name,
                        bodyTarget.gameObject.layer.ToString());
                    Utilities.Log_Debug("pos=" + bodyTarget.position);
                    ScreenMessages.PostScreenMessage("Target: " + bodyTarget.theName, 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            showBodTargetsWindow = !GUILayout.Button(new GUIContent("Hide", "Hide this Window"));
            GUILayout.EndVertical();
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }

        private void TargettingGalWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GalscrollViewVector = GUILayout.BeginScrollView(GalscrollViewVector, GUILayout.Height(300),
                GUILayout.Width(GUI_WIDTH_SMALL));
            filterContractTargets = GUILayout.Toggle(filterContractTargets,
                new GUIContent("Show only contract targets",
                    "If selected only targets that are the subject of a current contract will be shown"));
            //RSTUtils.Utilities.Log_Debug(String.Format(" - TargettingWindow - TSTGalaxies.Galaxies.Count = {0}", TSTGalaxies.Galaxies.Count));			

            var newTarget = 0;
            if (isRBactive && RBWrapper.APIDBReady && RBWrapper.APIFLReady && RBmoduleTrackBodies.enabled)
            {
                var filterRBTargets = isRBactive;
                newTarget = TSTGalaxies.Galaxies.
                    FindIndex(
                        g => TSTProgressTracker.HasTelescopeCompleted(g) ||
                             (ContractSystem.Instance &&
                              ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>()
                                  .Any(t => t.target.name == g.name))
                            ? GUILayout.Button(g.theName)
                            : (filterContractTargets
                                ? false
                                : (RBmoduleTrackBodies.TrackedBodies[
                                    TSTGalaxies.CBGalaxies.Find(x => x.theName == g.theName)]
                                    ? GUILayout.Button(g.theName)
                                    : false)));
            }
            else
            {
                newTarget = TSTGalaxies.Galaxies.
                    FindIndex(
                        g => TSTProgressTracker.HasTelescopeCompleted(g) ||
                             (ContractSystem.Instance &&
                              ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>()
                                  .Any(t => t.target.name == g.name))
                            ? GUILayout.Button(g.theName)
                            : (filterContractTargets ? false : GUILayout.Button(g.theName)));
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
                ScreenMessages.PostScreenMessage("Target: " + galaxyTarget.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
            }
            GUILayout.EndScrollView();
            GUILayout.Space(10);
            showGalTargetsWindow = !GUILayout.Button(new GUIContent("Hide", "Hide this Window"));
            GUILayout.EndVertical();
            if (TSTMstStgs.Instance.TSTsettings.Tooltips)
                Utilities.SetTooltipText();
            GUI.DragWindow();
        }


        public void OnGUI()
        {
            if (Utilities.GameModeisFlight)
            {
                if (!_inEditor && _camera != null)
                {
                    if (!Textures.StylesSet) Textures.SetupStyles();

                    if (FlightUIModeController.Instance.Mode != FlightUIMode.ORBITAL && _camera.Enabled &&
                        windowState != WindowState.Hidden
                        && vessel.isActiveVessel && !Utilities.isPauseMenuOpen)
                    {
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

        [KSPEvent(active = true, guiActive = true, name = "eventShowGUI", guiName = "Show GUI")]
        public void eventShowGUI()
        {
            Events["eventShowGUI"].active = false;
            windowState = WindowState.Small;
            _camera.Enabled = true;
        }

        #endregion GUI

        #region Science

        private void doScience(TSTGalaxy galaxy)
        {
            Utilities.Log_Debug("Doing Science for {0}", galaxy.theName);
            var experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
            Utilities.Log_Debug("Got experiment");
            var subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(),
                Sun.Instance.sun, "LookingAt" + galaxy.name);
            subject.title = "Space Telescope picture of " + galaxy.theName;
            Utilities.Log_Debug("Got subject, determining science data using {0}", part.name);
            if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
            {
                if (part.name == "tarsierSpaceTelescope")
                {
                    var data = new ScienceData(experiment.baseValue/2*subject.dataScale, xmitDataScalar, labBoostScalar,
                        subject.id, subject.title, false, part.flightID);
                    Utilities.Log_Debug("Got data");
                    data.title = "Tarsier Space Telescope: Orbiting " + vessel.mainBody.theName + " looking at " +
                                 galaxy.theName;
                    _scienceData.Add(data);
                    Utilities.Log_Debug("Added Data Amt= {0}, TransmitValue= {1}, LabBoost= {2}, LabValue= {3}",
                        data.dataAmount.ToString(), data.transmitValue.ToString(), data.labBoost.ToString(),
                        data.labValue.ToString());
                    ScreenMessages.PostScreenMessage("Collected Science for " + galaxy.theName, 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                else
                {
                    var data = new ScienceData(experiment.baseValue*subject.dataScale, xmitDataScalar, labBoostScalar,
                        subject.id, subject.title, false, part.flightID);
                    Utilities.Log_Debug("Got data");
                    data.title = "Tarsier Space Telescope: Orbiting " + vessel.mainBody.theName + " looking at " +
                                 galaxy.theName;
                    _scienceData.Add(data);
                    Utilities.Log_Debug("Added Data Amt= {0}, TransmitValue= {1}, LabBoost= {2}, LabValue= {3}",
                        data.dataAmount.ToString(), data.transmitValue.ToString(), data.labBoost.ToString(),
                        data.labValue.ToString());
                    ScreenMessages.PostScreenMessage("Collected Science for " + galaxy.theName, 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        private void doScience(CelestialBody planet)
        {
            Utilities.Log_Debug("Doing Science for {0}", planet.theName);
            var experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
            Utilities.Log_Debug("Got experiment");
            var subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(), planet,
                "LookingAt" + planet.name);
            subject.title = "Space Telescope picture of " + planet.theName;
            Utilities.Log_Debug("Got subject");
            if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
            {
                if (part.name == "tarsierSpaceTelescope")
                {
                    var data = new ScienceData(experiment.baseValue*0.8f*subject.dataScale, xmitDataScalar,
                        labBoostScalar, subject.id, subject.title, false, part.flightID);
                    Utilities.Log_Debug("Got data");
                    data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " +
                                 planet.theName;
                    _scienceData.Add(data);
                    Utilities.Log_Debug("Added Data Amt= {0}, TransmitValue= {1}, LabBoost= {2}, LabValue= {3}",
                        data.dataAmount.ToString(), data.transmitValue.ToString(), data.labBoost.ToString(),
                        data.labValue.ToString());
                    ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName, 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
                else
                {
                    var data = new ScienceData(experiment.baseValue*subject.dataScale, xmitDataScalar, labBoostScalar,
                        subject.id, subject.title, false, part.flightID);
                    Utilities.Log_Debug("Got data");
                    data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " +
                                 planet.theName;
                    _scienceData.Add(data);
                    Utilities.Log_Debug("Added Data Amt= {0}, TransmitValue= {1}, LabBoost= {2}, LabValue= {3}",
                        data.dataAmount.ToString(), data.transmitValue.ToString(), data.labBoost.ToString(),
                        data.labValue.ToString());
                    ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName, 3f,
                        ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        private ExperimentSituations getExperimentSituation()
        {
            switch (vessel.situation)
            {
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    return ExperimentSituations.SrfLanded;
                case Vessel.Situations.SPLASHED:
                    return ExperimentSituations.SrfSplashed;
                case Vessel.Situations.FLYING:
                    return vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold
                        ? ExperimentSituations.FlyingLow
                        : ExperimentSituations.FlyingHigh;
                default:
                    return vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold
                        ? ExperimentSituations.InSpaceLow
                        : ExperimentSituations.InSpaceHigh;
            }
        }

        private string getZoomString(float zoom)
        {
            string[] unicodePowers =
            {
                "\u2070", "\u00B9", "\u00B2", "\u00B3", "\u2074", "\u2075", "\u2076", "\u2077",
                "\u2078", "\u2079"
            };
            var zStr = "x";
            var z = Mathf.Pow(10, zoom);
            var magnitude = Mathf.Pow(10, Mathf.Floor(zoom));
            var msf = Mathf.Floor(z/magnitude);
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
            return base.GetInfo();
        }

        public enum WindowState
        {
            Small,
            Large,
            Hidden
        }

        [KSPEvent(active = false, guiActive = true, name = "eventReviewScience", guiName = "Check Results")]
        public void eventReviewScience()
        {
            foreach (var data in _scienceData)
            {
                ReviewDataItem(data);
            }
        }

        private void _onPageDiscard(ScienceData data)
        {
            _scienceData.Remove(data);
        }

        private void _onPageKeep(ScienceData data)
        {
        }

        private void _onPageTransmit(ScienceData data)
        {
            var transmitters = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0 && _scienceData.Contains(data))
            {
                transmitters.First().TransmitData(new List<ScienceData> {data});
                _scienceData.Remove(data);
            }
        }

        [KSPEvent(active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "Collect Data",
            unfocusedRange = 2)]
        public void CollectScience()
        {
            var containers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            foreach (var container in containers)
            {
                if (_scienceData.Count > 0)
                {
                    if (container.StoreData(new List<IScienceDataContainer> {this}, false))
                        ScreenMessages.PostScreenMessage("Transferred Data to " + vessel.vesselName, 3f,
                            ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        private void _onPageSendToLab(ScienceData data)
        {
            Utilities.Log_Debug("Sent to lab");
        }


        // IScienceDataContainer
        public void DumpData(ScienceData data)
        {
            _scienceData.Remove(data);
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
            var labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);
            var page = new ExperimentResultDialogPage(
                part,
                data,
                xmitDataScalar,
                data.labBoost,
                false,
                "",
                true,
                labSearch,
                _onPageDiscard,
                _onPageKeep,
                _onPageTransmit,
                _onPageSendToLab);
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

            public string theName
            {
                get { return galaxy == null ? body.theName : galaxy.theName; }
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