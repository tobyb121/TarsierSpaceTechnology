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
using System.Linq;
using UnityEngine;
using System.IO;

namespace TarsierSpaceTech
{
	public class TSTSpaceTelescope : PartModule, IScienceDataContainer
	{
		private int GUI_WIDTH_SMALL = 320; 
        private int GUI_WIDTH_LARGE = 600;  
        
        private static int CAMwindowID = 5955555;
        private static int GALwindowID = 5955556;
        private static int BODwindowID = 5955557;              

		private bool _inEditor = false;
		private Animation _animation;
		private Transform _baseTransform;
        private Transform _cameraTransform;
        public Transform cameraTransform
        {
            get { return _cameraTransform; }
        }
		private Transform _lookTransform;
		internal TSTCameraModule _camera; 
                  

        private bool _showTarget = false;		
		private bool _saveToFile = false;		
		private List<ScienceData> _scienceData = new List<ScienceData>();

		private Rect windowPos = new Rect(128, 128, 0, 0);
		public WindowSate windowState = WindowSate.Small;
		private Rect targetGalWindowPos = new Rect(512, 128, 0, 0);
		private Rect targetBodWindowPos = new Rect(512, 128, 0, 0);        
		private bool showGalTargetsWindow = false;
		private bool showBodTargetsWindow = false;
		private bool filterContractTargets = false;        
		int selectedTargetIndex = -1;
        private Vector2 GalscrollViewVector = Vector2.zero;
        private Vector2 BodscrollViewVector = Vector2.zero;
        public float PIDKp = 12f;
        public float PIDKi = 6f;
        public float PIDKd = 0.5f;

		[KSPField(guiActive = false, guiName = "maxZoom", isPersistant = true)]
		public int maxZoom = 5;

		[KSPField(isPersistant = true)]
		public bool Active = false;
        [KSPField]
		public string baseTransformName = "Telescope";       
        [KSPField]
		public string cameraTransformName = "CameraTransform";       
        [KSPField]
		public string lookTransformName = "LookTransform";       
		[KSPField]
		public bool servoControl = true;
		private Quaternion zeroRotation;

		[KSPField]
		public float xmitDataScalar = 0.5f;
		[KSPField]
		public float labBoostScalar = 0f;

		private int targetId = 0;
		private Vessel _vessel;

		private static List<byte[]> targets_raw = new List<byte[]> {
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
		private static List<Texture2D> targets = new List<Texture2D>();

		public TargettingMode targettingMode = TargettingMode.Galaxy;
		private TSTGalaxy galaxyTarget;
		private CelestialBody bodyTarget;
        private bool isRBactive = false;
        private Dictionary<CelestialBody, bool> TrackedBodies = new Dictionary<CelestialBody, bool>();
        private Dictionary<CelestialBody, int> ResearchState=  new Dictionary<CelestialBody, int>();
        private RBWrapper.ModuleTrackBodies RBmoduleTrackBodies;
		

		public override void OnStart(StartState state)
		{
			base.OnStart(state);
			Utilities.Log_Debug("TSTTel","Starting Telescope");
			part.CoMOffset = part.attachNodes[0].position;
			if (state == StartState.Editor)
			{
				_inEditor = true;
				return;
			}            
            _baseTransform = Utilities.FindChildRecursive(transform, baseTransformName);
			_cameraTransform = Utilities.FindChildRecursive(transform,cameraTransformName);
			_lookTransform = Utilities.FindChildRecursive(transform,lookTransformName);			
            Utilities.PrintTransform(_baseTransform, "_basetransform");            
            Utilities.PrintTransform(_cameraTransform, "_cameratransform");            
            Utilities.PrintTransform(_lookTransform, "_looktransform");
            this.Log_Debug("part.CoMOffset=" + part.CoMOffset);
			zeroRotation = _cameraTransform.localRotation;
            this.Log_Debug("zeroRotation=" + zeroRotation);
            _camera = _cameraTransform.gameObject.AddComponent<TSTCameraModule>();
			_animation = _baseTransform.animation;
            
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
            
            for (int i = 0; i < targets_raw.Count; i++)
			{
				Texture2D tex = new Texture2D(40, 40);
				tex.LoadImage(targets_raw[i]);
				targets.Add(tex);
			}
			Utilities.Log_Debug("TSTTel","Getting ExpIDs");
			foreach (String expID in ResearchAndDevelopment.GetExperimentIDs())
			{
				Utilities.Log_Debug("TSTTel","Got ExpID: " + expID);
			}
			Utilities.Log_Debug("TSTTel","Got ExpIDs");
            CAMwindowID = Utilities.getnextrandomInt();
            GALwindowID = Utilities.getnextrandomInt();
            BODwindowID = Utilities.getnextrandomInt();            
            Utilities.Log_Debug("TSTTel","On end start");
            StartCoroutine(setSASParams());
            Utilities.Log_Debug("TSTTel","Adding Input Callback");            
			_vessel = vessel;
			_vessel.OnAutopilotUpdate += new FlightInputCallback(onFlightInput);				
			GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(refreshFlightInputHandler));
			GameEvents.onVesselDestroy.Add(new EventData<Vessel>.OnEvent(removeFlightInputHandler));
			GameEvents.OnVesselRecoveryRequested.Add(new EventData<Vessel>.OnEvent(removeFlightInputHandler));
			Utilities.Log_Debug("TSTTel","Added Input Callback");
            if (Active)
                StartCoroutine(openCamera());
            //If ResearchBodies is installed, initialise the PartModulewrapper and get the TrackedBodies dictionary item.
            isRBactive = TSTInstalledMods.IsResearchBodiesInstalled;
            if (isRBactive)
            {
                RBWrapper.InitRBDBWrapper();
                RBWrapper.InitRBFLWrapper();
                if (RBWrapper.APIFLReady)
                {                    
                    foreach (PartModule module in this.part.Modules)
                    {
                        if (module.moduleName == "ModuleTrackBodies")
                        {
                            RBmoduleTrackBodies = new RBWrapper.ModuleTrackBodies(module);
                            TrackedBodies = RBmoduleTrackBodies.TrackedBodies;
                            ResearchState = RBmoduleTrackBodies.ResearchState;

                            if (File.Exists("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg"))
                            {
                                ConfigNode mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                                foreach (CelestialBody cb in TSTGalaxies.CBGalaxies)
                                {
                                    bool fileContainsGalaxy = false;
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
                                                    foreach (ConfigNode cbSettingNode in mainnode.GetNode("RESEARCHBODIES").nodes)
                                                    {
                                                        if (cbSettingNode.GetValue("body") == cb.GetName())
                                                            cbNode = cbSettingNode;
                                                    }
                                                    cbNode.AddValue("researchState", "0");
                                                    mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                                                    ResearchState[cb] = 0;
                                                }
                                            }
                                            fileContainsGalaxy = true;
                                        }
                                    }
                                    if (!fileContainsGalaxy)
                                    {
                                        ConfigNode newNodeForCB = mainnode.GetNode("RESEARCHBODIES").AddNode("BODY");
                                        newNodeForCB.AddValue("body", cb.GetName());
                                        newNodeForCB.AddValue("isResearched", "false");
                                        newNodeForCB.AddValue("researchState", "0");
                                        newNodeForCB.AddValue("ignore", "false");
                                        TrackedBodies[cb] = false; ResearchState[cb] = 0;
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
            
        IEnumerator setSASParams()
        {
            while (FlightGlobals.ActiveVessel.Autopilot.RSAS.pidPitch == null)
                yield return null;
            this.Log_Debug("Setting PIDs");
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidPitch.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidRoll.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
            FlightGlobals.ActiveVessel.Autopilot.RSAS.pidYaw.ReinitializePIDsOnly(PIDKp, PIDKi, PIDKd);
        }

        public void removeFlightInputHandler(Vessel target)
		{
			Utilities.Log_Debug("TSTTel","Removing Input Callback vessel: " + target.name);
			if (this.vessel == target)
			{
				_vessel.OnAutopilotUpdate -= (onFlightInput);
				GameEvents.onVesselChange.Remove(this.refreshFlightInputHandler);
				GameEvents.onVesselDestroy.Remove(this.removeFlightInputHandler);
				GameEvents.OnVesselRecoveryRequested.Remove(this.removeFlightInputHandler);
                //StopCoroutine(setSASParams());
                Utilities.Log_Debug("TSTTel","Input Callbacks removed this vessel");
			}            			
		}

		[KSPEvent(active = false, guiActive = true, guiName = "Disable Servos")]
		public void toggleServos()
		{
			servoControl = !servoControl;
			Events["toggleServos"].guiName = servoControl ? "Disable Servos" : "Enable Servos";
			if (!servoControl)
				_cameraTransform.localRotation = zeroRotation;
		}

		private void refreshFlightInputHandler(Vessel target)
		{
			Utilities.Log_Debug("TSTTel","OnVesselSwitch curr: " + vessel.name + " target: " + target.name);
			if (this.vessel != target)
			{
				Utilities.Log_Debug("TSTTel","This vessel != target removing Callback");
				_vessel.OnAutopilotUpdate -= (onFlightInput);
			}
			if (this.vessel == target)
			{
				_vessel = target;
				List<TSTSpaceTelescope> vpm = _vessel.FindPartModulesImplementing<TSTSpaceTelescope>();
				if (vpm.Count > 0)
				{
					Utilities.Log_Debug("TSTTel","Adding Input Callback");
					_vessel.OnAutopilotUpdate += new FlightInputCallback(onFlightInput);
					Utilities.Log_Debug("TSTTel","Added Input Callback");
				}
			}							   									
		}

		private void onFlightInput(FlightCtrlState ctrl)
		{
			if (_camera.Enabled && servoControl)
			{                
                if (ctrl.X > 0)
				{
					_cameraTransform.Rotate(Vector3.up, -0.005f * _camera.fov);
				}
				else if (ctrl.X < 0)
				{
					_cameraTransform.Rotate(Vector3.up, 0.005f * _camera.fov);
				}
				if (ctrl.Y > 0)
				{
					_cameraTransform.Rotate(Vector3.right, -0.005f * _camera.fov);
				}
				else if (ctrl.Y < 0)
				{
					_cameraTransform.Rotate(Vector3.right, 0.005f * _camera.fov);
				}

				float angle=Mathf.Abs(Quaternion.Angle(_cameraTransform.localRotation, zeroRotation));

				if (angle > 1.5f)
				{
					_cameraTransform.localRotation = Quaternion.Slerp(zeroRotation, _cameraTransform.localRotation, 1.5f / angle);
				}
			}
		}
                
		private ITargetable _lastTarget;
		public override void OnUpdate()
		{
			Events["eventReviewScience"].active=(_scienceData.Count > 0);
			if (vessel.targetObject != _lastTarget && vessel.targetObject != null)
			{
				targettingMode = TargettingMode.Planet;
				selectedTargetIndex = -1;
				_lastTarget = vessel.targetObject;
			}

            if (vessel.targetObject != null)
            {
                this.Log_Debug("Vessel target=" + vessel.targetObject.GetTransform().position);
            }

            //if (!_inEditor && _camera.Enabled && windowState != WindowSate.Hidden && vessel.isActiveVessel)
            //{                
                //if (_camera.Enabled && f++ % frameLimit == 0)                                   
                //_camera.draw();                
			//}
		}

        #region GUI

        private void WindowGUI(int windowID)
		{            
            GUILayout.BeginVertical();
			GUILayout.BeginHorizontal();
			GUILayout.Label("Zoom", GUILayout.ExpandWidth(false));
			_camera.ZoomLevel = GUILayout.HorizontalSlider(_camera.ZoomLevel, -1, maxZoom, GUILayout.ExpandWidth(true));
			GUILayout.Label(getZoomString(_camera.ZoomLevel), GUILayout.ExpandWidth(false), GUILayout.Width(60));
			GUILayout.EndHorizontal();
			Texture2D texture2D = _camera.Texture2D;
			Rect imageRect=GUILayoutUtility.GetRect(texture2D.width, texture2D.height);
			Vector2 center = imageRect.center;
			imageRect.width = texture2D.width;
			imageRect.height = texture2D.height;
			imageRect.center = center;
			GUI.DrawTexture(imageRect, texture2D);
			Rect rect=new Rect(0,0,40,40);
			if (_showTarget)
			{
				Transform cameraTransform = null;
				Transform targetTransform = null;
				if (targettingMode == TargettingMode.Planet && FlightGlobals.fetch.VesselTarget != null)
				{
					cameraTransform = _cameraTransform;
					targetTransform = FlightGlobals.fetch.vesselTargetTransform;
                    this.Log_Debug("showtarget cameratransform=" + cameraTransform.position + ",targettransform=" + targetTransform.position);
				}
				else if (targettingMode == TargettingMode.Galaxy && galaxyTarget != null)
				{
					cameraTransform = _camera._skyBoxCam.camera.transform;
					targetTransform = galaxyTarget.transform;
				}                    
				if (cameraTransform != null)
				{
					Vector3d r = targetTransform.position - cameraTransform.position;
					double dx = Vector3d.Dot(cameraTransform.right.normalized, r.normalized);
					double thetax = 90 - Math.Acos(dx) * Mathf.Rad2Deg;
					double dy = Vector3d.Dot(cameraTransform.up.normalized, r.normalized);
					double thetay = 90 - Math.Acos(dy) * Mathf.Rad2Deg;
					double dz = Vector3d.Dot(cameraTransform.forward.normalized, r.normalized);
					double xpos = texture2D.width * thetax / _camera.fov;
					double ypos = texture2D.height * thetay / _camera.fov;
					if (dz > 0 && Math.Abs(xpos) < texture2D.width / 2 && Math.Abs(ypos) < texture2D.height / 2)
					{
						rect.center = imageRect.center + new Vector2((float)xpos, -(float)ypos);
						GUI.DrawTexture(rect, targets[(targetId++ / 5) % targets.Count], ScaleMode.StretchToFill, true);
					}
				}
			}
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Reset Zoom")) _camera.ZoomLevel = 0;
			if (GUILayout.Button(windowState == WindowSate.Small ? "Large" : "Small"))
			{
				windowState = windowState == WindowSate.Small ? WindowSate.Large : WindowSate.Small;
				int w=(windowState == WindowSate.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE);
				_camera.changeSize(w,w);
				windowPos.height = 0;
			};
			if (GUILayout.Button(showGalTargetsWindow?"Hide Galaxies":"Show Galaxies")) showGalTargetsWindow = !showGalTargetsWindow;
			if (GUILayout.Button(showBodTargetsWindow?"Hide Bodies":"Show Bodies")) showBodTargetsWindow = !showBodTargetsWindow;			
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Hide")) hideGUI();
			_showTarget = GUILayout.Toggle(_showTarget, "Show Target");
			_saveToFile = GUILayout.Toggle(_saveToFile, "Save To File");
			if (GUILayout.Button("Take Picture")) takePicture(_saveToFile);
			GUILayout.EndHorizontal();
			GUILayout.EndVertical();
			GUI.DragWindow();
		}
		
		private void TargettingBodWindow(int windowID)
		{
			
            GUILayout.BeginVertical();
            BodscrollViewVector = GUILayout.BeginScrollView(BodscrollViewVector, GUILayout.Height(300), GUILayout.Width(GUI_WIDTH_SMALL));
            
			filterContractTargets = GUILayout.Toggle(filterContractTargets, "Show only contract targets");           
            //Utilities.Log_Debug("TSTTel",String.Format(" - TargettingWindow - TSTBodies.Count = {0}", FlightGlobals.Bodies.Count));			
            int newTarget = 0;
            if (isRBactive && RBWrapper.APIDBReady && RBWrapper.APIFLReady && RBmoduleTrackBodies.enabled)
            {
                bool filterRBTargets = isRBactive;
                newTarget = FlightGlobals.Bodies.
                            FindIndex(
                                   g => (TSTProgressTracker.HasTelescopeCompleted(g) ||
                                            (Contracts.ContractSystem.Instance && Contracts.ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>().Any(t => t.target.name == g.name))
                                            ) ? GUILayout.Button(g.theName) : (filterContractTargets ? false : (RBmoduleTrackBodies.TrackedBodies[g] ? GUILayout.Button(g.theName) : false)));
            }
            else
            {
                newTarget = FlightGlobals.Bodies.
                            FindIndex(
                                g => (TSTProgressTracker.HasTelescopeCompleted(g) ||
                                         (Contracts.ContractSystem.Instance && Contracts.ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>().Any(t => t.target.name == g.name))
                                     ) ? GUILayout.Button(g.theName) : (filterContractTargets ? false : GUILayout.Button(g.theName)));
            }			

			//Utilities.Log_Debug("TSTTel",String.Format(" - TargettingWindow - newTarget = {0}", newTarget));

			if (newTarget != -1 && newTarget != selectedTargetIndex)
			{
				vessel.targetObject = null;
				FlightGlobals.fetch.SetVesselTarget(null);
				targettingMode = TargettingMode.Planet;
				selectedTargetIndex = newTarget;                
				bodyTarget = FlightGlobals.Bodies[selectedTargetIndex];
				if (FlightGlobals.ActiveVessel.mainBody.name == bodyTarget.name)
				{
					Utilities.Log_Debug("TSTTel","Cannot Target: " + newTarget.ToString() + " " + bodyTarget.name + " in it's SOI");
					ScreenMessages.PostScreenMessage("Cannot Target " + bodyTarget.theName + " as in it's SOI", 3f, ScreenMessageStyle.UPPER_CENTER);
				}
				else
				{
					FlightGlobals.fetch.SetVesselTarget(bodyTarget);
					Utilities.Log_Debug("TSTTel","Targetting: " + newTarget.ToString() + " " + bodyTarget.name);
                    Utilities.Log_Debug("TSTTel", "Targetting: " + newTarget.ToString() + " " + bodyTarget.name + ",layer=" + bodyTarget.gameObject.layer);
                    Utilities.Log_Debug("pos=" + bodyTarget.position);
					ScreenMessages.PostScreenMessage("Target: " + bodyTarget.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
				}				
			}            
            GUILayout.EndScrollView(); 
			GUILayout.Space(10);
			showBodTargetsWindow = !GUILayout.Button("Hide");
			GUILayout.EndVertical();
            GUI.DragWindow();
		}

		private void TargettingGalWindow(int windowID)
		{
            
            GUILayout.BeginVertical();
            GalscrollViewVector = GUILayout.BeginScrollView(GalscrollViewVector, GUILayout.Height(300), GUILayout.Width(GUI_WIDTH_SMALL));
            filterContractTargets = GUILayout.Toggle(filterContractTargets, "Show only contract targets");
            //Utilities.Log_Debug("TSTTel",String.Format(" - TargettingWindow - TSTGalaxies.Galaxies.Count = {0}", TSTGalaxies.Galaxies.Count));			

            int newTarget = 0;
            if (isRBactive && RBWrapper.APIDBReady && RBWrapper.APIFLReady && RBmoduleTrackBodies.enabled)
            {
                bool filterRBTargets = isRBactive;
                newTarget = TSTGalaxies.Galaxies.
                            FindIndex(
                                   g => (TSTProgressTracker.HasTelescopeCompleted(g) ||
                                            (Contracts.ContractSystem.Instance && Contracts.ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>().Any(t => t.target.name == g.name))
                                            ) ? GUILayout.Button(g.theName) : (filterContractTargets ? false : (RBmoduleTrackBodies.TrackedBodies[TSTGalaxies.CBGalaxies.Find(x => x.theName == g.theName)] ? GUILayout.Button(g.theName) : false)));
            }
            else
            {
                newTarget = TSTGalaxies.Galaxies.
                            FindIndex(
                                g => (TSTProgressTracker.HasTelescopeCompleted(g) ||
                                         (Contracts.ContractSystem.Instance && Contracts.ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>().Any(t => t.target.name == g.name))
                                     ) ? GUILayout.Button(g.theName) : (filterContractTargets ? false : GUILayout.Button(g.theName)));
            }

            //Utilities.Log_Debug("TSTTel",String.Format(" - TargettingWindow - newTarget = {0}", newTarget));

            if (newTarget != -1 && newTarget != selectedTargetIndex)
			{
				vessel.targetObject = null;
				FlightGlobals.fetch.SetVesselTarget(null);
				targettingMode = TargettingMode.Galaxy;
				selectedTargetIndex = newTarget;
				galaxyTarget = TSTGalaxies.Galaxies[selectedTargetIndex];
				FlightGlobals.fetch.SetVesselTarget(galaxyTarget);                
				Utilities.Log_Debug("TSTTel","Targetting: " + newTarget.ToString() + " " + galaxyTarget.name + ",layer=" + galaxyTarget.gameObject.layer + ",scaledpos=" + galaxyTarget.scaledPosition);
                Utilities.Log_Debug("pos=" + galaxyTarget.position);
				ScreenMessages.PostScreenMessage("Target: "+galaxyTarget.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
			}            
            GUILayout.EndScrollView(); 
			GUILayout.Space(10);
			showGalTargetsWindow = !GUILayout.Button("Hide");
			GUILayout.EndVertical();
            GUI.DragWindow();		
		}
                

        public void OnGUI()
		{
            if (!_inEditor && FlightUIModeController.Instance.Mode != FlightUIMode.ORBITAL && _camera.Enabled && windowState != WindowSate.Hidden 
                && vessel.isActiveVessel && !Utilities.isPauseMenuOpen())
			{
                windowPos = GUILayout.Window(CAMwindowID, windowPos, WindowGUI, "Space Telescope", GUILayout.Width(windowState == WindowSate.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE));
				if(showGalTargetsWindow)
                    targetGalWindowPos = GUILayout.Window(GALwindowID, targetGalWindowPos, TargettingGalWindow, "Select Target", GUILayout.Width(GUI_WIDTH_SMALL));
				if(showBodTargetsWindow)
                    targetBodWindowPos = GUILayout.Window(BODwindowID, targetBodWindowPos, TargettingBodWindow, "Select Target", GUILayout.Width(GUI_WIDTH_SMALL));                

			}
		}

		public void hideGUI()
		{
			windowState = WindowSate.Hidden;
			_camera.Enabled = false;
			Events["eventShowGUI"].active = true;
		}

		[KSPEvent(active = true, guiActive = true, name = "eventShowGUI", guiName = "Show GUI")]
		public void eventShowGUI()
		{
			Events["eventShowGUI"].active = false;
			windowState = WindowSate.Small;
			_camera.Enabled = true;
		}

#endregion GUI

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
			if (_animation != null)
            {                
                _animation.Play("open");
                IEnumerator wait = Utilities.WaitForAnimation(_animation, "open");
                while (wait.MoveNext()) yield return null;                            
            }            
			Events["eventCloseCamera"].active = true;
			Events["eventControlFromHere"].active = true;
			Events["toggleServos"].active = true;
			_camera.Enabled = true;            
			Active = true;
			windowState = WindowSate.Small;
			_camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL );
			_cameraTransform.localRotation = zeroRotation;
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
            if (_animation != null)
            {
                
                    _animation.Play("close");
                    IEnumerator wait = Utilities.WaitForAnimation(_animation, "close");
                    while (wait.MoveNext()) yield return null;
                
                
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
			Utilities.Log_Debug("TSTTel","Taking Picture");
			_scienceData.Clear();
			Utilities.Log_Debug("TSTTel","Checking Look At");
			List<TargetableObject> objs=getLookingAt();
			Utilities.Log_Debug("TSTTel","Looking at: " + objs.Count.ToString() + " celestial objects");
			foreach (TargetableObject obj in objs)
			{
				Utilities.Log_Debug("TSTTel","Looking at " + obj.theName);
				if(obj.type == typeof(CelestialBody)){
					CelestialBody body = (CelestialBody)obj.BaseObject;                    
                    doScience(body);
					if (TSTProgressTracker.isActive)
					{
						TSTProgressTracker.OnTelescopePicture(body);
					}
				}
				else if (obj.type == typeof(TSTGalaxy))
				{
					TSTGalaxy galaxy = (TSTGalaxy)obj.BaseObject;                    
                    doScience(galaxy);
					if (TSTProgressTracker.isActive)
					{
						TSTProgressTracker.OnTelescopePicture(galaxy);
					}
				}
			}
			Utilities.Log_Debug("TSTTel","Gather Science complete");
			if (objs.Count == 0)
			{
				ScreenMessages.PostScreenMessage("No science collected",3f,ScreenMessageStyle.UPPER_CENTER);
			}

			if (saveToFile)
			{
				Utilities.Log_Debug("TSTTel","Saving to File");
				int i = 0;
				while ((KSP.IO.File.Exists<TSTSpaceTelescope>("Telescope_" + DateTime.Now.ToString("d-m-y")+"_"+i.ToString() + ".png",null)) ||
                    (KSP.IO.File.Exists<TSTSpaceTelescope>("Telescope_" + DateTime.Now.ToString("d-m-y")+"_"+i.ToString() + "Large.png",null))) 
                    i++;
				_camera.saveToFile("Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i.ToString(), "TeleScope");
                ScreenMessages.PostScreenMessage("Picture saved", 3f, ScreenMessageStyle.UPPER_CENTER);
			}
		}

        

		private List<TargetableObject> getLookingAt()
		{
			List<TargetableObject> result = new List<TargetableObject>();
			List<TargetableObject> bodies = FlightGlobals.Bodies.Select(b => (TargetableObject)b).ToList();
			List<TargetableObject> galaxies = TSTGalaxies.Galaxies.Select(g => (TargetableObject)g).ToList();
			Utilities.Log_Debug("TSTTel","getLookingAt start");
			foreach (TargetableObject obj in galaxies.Concat(bodies))
			{
				Vector3 r = (obj.position - _cameraTransform.position);
				float distance = r.magnitude;                
				double theta = Vector3d.Angle(_cameraTransform.forward, r);
				double visibleWidth = (2 * obj.size / distance) * 180 / Mathf.PI;
				Utilities.Log_Debug("TSTTel","getLookingAt about to calc fov");
				double fov = 0.05 * _camera.fov;
				Utilities.Log_Debug("TSTTel",obj.theName + ": distance=" + distance.ToString() + "  theta=" + theta.ToString() + "  visibleWidth=" + visibleWidth.ToString() + " fov=" + fov.ToString());                
				if (theta < _camera.fov / 2)
				{
					Utilities.Log_Debug("TSTTel","Looking at: " + obj.theName);
					if (visibleWidth > fov)
					{
						Utilities.Log_Debug("TSTTel","Can see: " + obj.theName);                         
						result.Add(obj);                        
					}
				}
			}
			return result;
		}
        #region Science

        private void doScience(TSTGalaxy galaxy)
		{
			Utilities.Log_Debug("TSTTel","Doing Science for " + galaxy.theName);
			ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
			Utilities.Log_Debug("TSTTel","Got experiment");
			ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(), Sun.Instance.sun, "LookingAt" + galaxy.name);
			subject.title = "Space Telescope picture of " + galaxy.theName;
			Utilities.Log_Debug("TSTTel","Got subject, determining science data using " + part.name);
			if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
			{
				if (part.name == "tarsierSpaceTelescope")
				{
					ScienceData data = new ScienceData((experiment.baseValue / 2) * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title, false, part.flightID);
					Utilities.Log_Debug("TSTTel","Got data");
					data.title = "Tarsier Space Telescope: Orbiting " + vessel.mainBody.theName + " looking at " + galaxy.theName;
					_scienceData.Add(data);
					Utilities.Log_Debug("TSTTel","Added Data Amt=" + data.dataAmount + " TransmitValue=" + data.transmitValue + " LabBoost=" + data.labBoost + " LabValue=" + data.labValue);
					ScreenMessages.PostScreenMessage("Collected Science for " + galaxy.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
				}
				else
				{
					ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title, false, part.flightID);
					Utilities.Log_Debug("TSTTel","Got data");
					data.title = "Tarsier Space Telescope: Orbiting " + vessel.mainBody.theName + " looking at " + galaxy.theName;
					_scienceData.Add(data);
					Utilities.Log_Debug("TSTTel","Added Data Amt=" + data.dataAmount + " TransmitValue=" + data.transmitValue + " LabBoost=" + data.labBoost + " LabValue=" + data.labValue);
					ScreenMessages.PostScreenMessage("Collected Science for " + galaxy.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
				}                
				
			}
		}

		private void doScience(CelestialBody planet)
		{
			Utilities.Log_Debug("TSTTel","Doing Science for " + planet.theName);
			ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
			Utilities.Log_Debug("TSTTel","Got experiment");
			ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(), planet, "LookingAt" + planet.name);
			subject.title = "Space Telescope picture of "+planet.theName;
			Utilities.Log_Debug("TSTTel","Got subject");
			if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
			{
				if (part.name == "tarsierSpaceTelescope")
				{
					ScienceData data = new ScienceData((experiment.baseValue * 0.8f) * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title, false, part.flightID);
					Utilities.Log_Debug("TSTTel","Got data");
					data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " + planet.theName;
					_scienceData.Add(data);
					Utilities.Log_Debug("TSTTel","Added Data Amt=" + data.dataAmount + " TransmitValue=" + data.transmitValue + " LabBoost=" + data.labBoost + " LabValue=" + data.labValue);
					ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
				}
				else
				{
					ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title, false, part.flightID);
					Utilities.Log_Debug("TSTTel","Got data");
					data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " + planet.theName;
					_scienceData.Add(data);
					Utilities.Log_Debug("TSTTel","Added Data Amt=" + data.dataAmount + " TransmitValue=" + data.transmitValue + " LabBoost=" + data.labBoost + " LabValue=" + data.labValue);
					ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
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
					return (vessel.altitude < vessel.mainBody.scienceValues.flyingAltitudeThreshold) ? ExperimentSituations.FlyingLow : ExperimentSituations.FlyingHigh;
				default:
					return (vessel.altitude < vessel.mainBody.scienceValues.spaceAltitudeThreshold) ? ExperimentSituations.InSpaceLow : ExperimentSituations.InSpaceHigh;
			}
		}
		
		private string getZoomString(float zoom)
		{
			string[] unicodePowers = { "\u2070", "\u00B9", "\u00B2", "\u00B3", "\u2074", "\u2075", "\u2076", "\u2077", "\u2078", "\u2079" };
			string zStr = "x";
			float z = Mathf.Pow(10, zoom);
			float magnitude = Mathf.Pow(10, Mathf.Floor(zoom));
			float msf = Mathf.Floor(z / magnitude);
			if (zoom >= 3)
			{
				zStr += msf.ToString() + "x10" + unicodePowers[Mathf.FloorToInt(zoom)];
			}
			else
			{
				zStr += (msf * magnitude).ToString();
			}
			return zStr;
		}

		public override string GetInfo()
		{
			return base.GetInfo();
		}

		public enum WindowSate
		{
			Small, Large, Hidden
		}

		[KSPEvent(active = false, guiActive = true, name = "eventReviewScience", guiName = "Check Results")]
		public void eventReviewScience()
		{
			foreach (ScienceData data in _scienceData)
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
			
			List<IScienceDataTransmitter> transmitters = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
			if (transmitters.Count > 0 && _scienceData.Contains(data))
			{
				transmitters.First().TransmitData(new List<ScienceData> { data });
				_scienceData.Remove(data);
			}
		}

		[KSPEvent(active=true,externalToEVAOnly=true,guiActiveUnfocused=true,guiName="Collect Data",unfocusedRange=2)]
		public void CollectScience()
		{
		   List<ModuleScienceContainer> containers =  FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
		   foreach (ModuleScienceContainer container in containers)
		   {
			   if (_scienceData.Count > 0)
			   {
				   if(container.StoreData(new List<IScienceDataContainer>(){this},false))
					   ScreenMessages.PostScreenMessage("Transferred Data to "+vessel.vesselName,3f,ScreenMessageStyle.UPPER_CENTER);
			   }
		   }
		}

		private void _onPageSendToLab(ScienceData data)
		{
			Utilities.Log_Debug("TSTTel","Sent to lab");
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
            Utilities.Log_Debug("TSTTel", "Is rerunnable");
            return true;
        }

        public void ReviewDataItem(ScienceData data)
        {
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    part,
                    data,
                    xmitDataScalar,
                    data.labBoost,
                    false,
                    "",
                    true,
                    false,
                    new Callback<ScienceData>(_onPageDiscard),
                    new Callback<ScienceData>(_onPageKeep),
                    new Callback<ScienceData>(_onPageTransmit),
                    new Callback<ScienceData>(_onPageSendToLab));
            ExperimentsResultDialog.DisplayResult(page);
        }

        #endregion Science

        public override void OnSave(ConfigNode node)
		{
            Utilities.Log_Debug("TSTTel", "OnSave Telescope"); 
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
            Utilities.Log_Debug("TSTTel", "OnSave Telescope End");
		}

		public override void OnLoad(ConfigNode node)
		{
            Utilities.Log_Debug("TSTTel", "OnLoad Telescope");
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
            Utilities.Log_Debug("TSTTel", "OnLoad Telescope End");
		}

        #region Galaxy

        //Galaxy Wrapper
		public enum TargettingMode
		{
			Galaxy,
			Planet
		}

		public class TargetableObject
		{
			private TSTGalaxy galaxy;
			private CelestialBody body;

			public static implicit operator TargetableObject(TSTGalaxy galaxy)
			{
				if (galaxy != null)
					return new TargetableObject(galaxy);
				else
					return null;
			}

			private TargetableObject(TSTGalaxy galaxy)
			{
				this.galaxy = galaxy;
			}

			public static implicit operator TargetableObject(CelestialBody body)
			{
				if (body != null)
					return new TargetableObject(body);
				else
					return null;
			}

			private TargetableObject(CelestialBody body)
			{
				this.body = body;
			}

			public Type type
			{
				get
				{
					return galaxy == null ? typeof(CelestialBody) : typeof(TSTGalaxy);
				}
			}

			public object BaseObject
			{
				get
				{
					return galaxy == null ? (object)body : (object)galaxy;
				}
			}

			public Vector3 position
			{
				get
				{
					return galaxy == null ? body.transform.position : galaxy.position;
				}
			}

			public double size
			{
				get
				{
					return galaxy == null ? body.Radius : (double) galaxy.size;
				}
			}

			public string name
			{
				get
				{
					return galaxy == null ? body.name : galaxy.name;
				}
			}

			public string theName
			{
				get
				{
					return galaxy == null ? body.theName : galaxy.theName;
				}
			}
        }
        #endregion Galaxy
    }
}
