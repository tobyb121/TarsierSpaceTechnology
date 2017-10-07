﻿/*
 * TSTChemCam.cs
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
using KSP.IO;
using KSP.UI.Screens.Flight.Dialogs;
using RSTUtils;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;
using KSP.Localization;

namespace TarsierSpaceTech
{
	class TSTChemCam : PartModule, IScienceDataContainer
	{
		private bool _inEditor;

		private static int CHMCwindowID = 77777898;
		private int GUI_WIDTH_SMALL = 256;
		private int GUI_WIDTH_LARGE = 512;

		private Transform _lookTransform;
		private TSTCameraModule _camera;

		private Transform _lazerTransform;
		private LineRenderer _lazerObj;

		private Transform _headTransform;
		private Transform _upperArmTransform;
		private Animation _animationObj;

		private Rect _windowRect;
		public WindowSate windowState = WindowSate.Small;
		private bool _saveToFile;	

		private int frameLimit = 5;
		private int f;
		
		private List<ScienceData> _scienceData;

		private static Texture2D viewfinder;

		private static List<string> PlanetNames;

		[KSPField]
		public float xmitDataScalar = 0.5f;

		[KSPField]
		public string ExperimentID = "TarsierSpaceTech.ChemCam";

		[KSPField]
		public float labBoostScalar = 0f;

		[KSPField(isPersistant = true)]
		public bool Active;
		
		private Vessel _vessel;

        #region Cache Strings

	    private static string cacheautoLOC_TST_0031;
	    private static string cacheautoLOC_TST_0032;
	    private static string cacheautoLOC_TST_0033;
	    private static string cacheautoLOC_TST_0034;
	    private static string cacheautoLOC_TST_0035;
	    private static string cacheautoLOC_TST_0036;
	    private static string cacheautoLOC_TST_0037;
	    private static string cacheautoLOC_TST_0038;
	    private static string cacheautoLOC_TST_0039;
	    private static string cacheautoLOC_TST_0048;
	    private static string cacheautoLOC_TST_0049;
        private static string cacheautoLOC_TST_0079;

	    private static void CacheStrings()
	    {
            cacheautoLOC_TST_0031 = Localizer.Format("#autoLOC_TST_0031"); //#autoLOC_TST_0031 = Save To File
	        cacheautoLOC_TST_0032 = Localizer.Format("#autoLOC_TST_0032"); //#autoLOC_TST_0032 = If this is on, picture files will be saved to GameData/TarsierSpaceTech/PluginData/TarsierSpaceTech
            cacheautoLOC_TST_0033 = Localizer.Format("#autoLOC_TST_0033"); //#autoLOC_TST_0033 = Fire
	        cacheautoLOC_TST_0034 = Localizer.Format("#autoLOC_TST_0034"); //#autoLOC_TST_0034 = Fire the Laser!
	        cacheautoLOC_TST_0035 = Localizer.Format("#autoLOC_TST_0035"); //#autoLOC_TST_0035 = Large
	        cacheautoLOC_TST_0036 = Localizer.Format("#autoLOC_TST_0036"); //#autoLOC_TST_0036 = Set Large Window Size
	        cacheautoLOC_TST_0037 = Localizer.Format("#autoLOC_TST_0037"); //#autoLOC_TST_0037 = Small
	        cacheautoLOC_TST_0038 = Localizer.Format("#autoLOC_TST_0038"); //#autoLOC_TST_0038 = set Small Window Size
	        cacheautoLOC_TST_0039 = Localizer.Format("#autoLOC_TST_0039"); //#autoLOC_TST_0039 = ChemCam - Use I,J,K,L to move camera
	        cacheautoLOC_TST_0048 = Localizer.Format("#autoLOC_TST_0048"); //#autoLOC_TST_0048 = No Terrain in Range to analyse
	        cacheautoLOC_TST_0049 = Localizer.Format("#autoLOC_TST_0049"); //#autoLOC_TST_0049 = Picture saved
            cacheautoLOC_TST_0079 = Localizer.Format("#autoLOC_TST_0079"); //#autoLOC_TST_0079 = No Comms Devices on this vessel. Cannot Transmit Data.
        }




        #endregion

        public new void Awake()
	    {
	        base.Awake();
            viewfinder = new Texture2D(1, 1);
            _scienceData = new List<ScienceData>();
        }

		public override void OnStart(StartState state)
		{
            CacheStrings();
            base.OnStart(state);
            if (state == StartState.Editor)
			{
				_inEditor = true;
			}
            Utilities.Log_Debug("Starting ChemCam");
			_lookTransform = Utilities.FindChildRecursive(transform,"CameraTransform");
		    if (state != StartState.Editor)
		    {
		        _camera = _lookTransform.gameObject.AddComponent<TSTCameraModule>();
		        Utilities.Log_Debug("Adding Lazer");
		        _lazerTransform = Utilities.FindChildRecursive(transform, "LazerTransform");
		        _lazerObj = _lazerTransform.gameObject.AddComponent<LineRenderer>();
		        _lazerObj.enabled = false;
		        //_lazerObj.castShadows = false;
		        _lazerObj.shadowCastingMode = ShadowCastingMode.Off;
		        _lazerObj.receiveShadows = false;
		        _lazerObj.SetWidth(0.01f, 0.01f);
		        _lazerObj.SetPosition(0, new Vector3(0, 0, 0));
		        _lazerObj.SetPosition(1, new Vector3(0, 0, 5));
		        _lazerObj.useWorldSpace = false;
		        _lazerObj.material = new Material(Shader.Find("Particles/Additive"));
		        _lazerObj.material.color = Color.red;
		        _lazerObj.SetColors(Color.red, Color.red);
		    }

		    Utilities.Log_Debug("Finding Camera Transforms");
			_headTransform = Utilities.FindChildRecursive(transform, "CamBody");
			_upperArmTransform = Utilities.FindChildRecursive(transform, "ArmUpper");

			Utilities.Log_Debug("Finding Animation Object");
			_animationObj = Utilities.FindChildRecursive(transform, "ChemCam").GetComponent<Animation>();
            
			PlanetNames = Utilities.GetCelestialBodyNames();
			CHMCwindowID = Utilities.getnextrandomInt();

		    if (state != StartState.Editor)
		    {
		        viewfinder.LoadImage(Properties.Resources.viewfinder);
                Utilities.Log_Debug("Adding Input Callback");
		        _vessel = vessel;
		        vessel.OnAutopilotUpdate += handleInput;
		        GameEvents.onVesselChange.Add(refreshFlghtInptHandler);
		        GameEvents.onVesselDestroy.Add(removeFlghtInptHandler);
		        GameEvents.OnVesselRecoveryRequested.Add(removeFlghtInptHandler);
		        Utilities.Log_Debug("Added Input Callback");
		    }
		    Events["eventOpenCamera"].active = true;
			Actions["actionOpenCamera"].active = true;
			Events["eventCloseCamera"].active = false;
			Actions["actionCloseCamera"].active = false;
			updateAvailableEvents();
			if (Active)
				StartCoroutine(openCamera());
		}

		public override void OnUpdate()
		{
			base.OnUpdate();
			if (!_inEditor && vessel.isActiveVessel)
			{
				updateAvailableEvents();
				if (_camera.Enabled && f++ % frameLimit == 0)
				{
					_camera.draw();
				}
			}
		}

		public void removeFlghtInptHandler(Vessel target)
		{
			Utilities.Log_Debug("{0}:Removing Input Callback vessel: {1}" , GetType().Name , target.name);
			if (vessel == target)
			{
				_vessel.OnAutopilotUpdate -= (handleInput);
				GameEvents.onVesselChange.Remove(refreshFlghtInptHandler);
				GameEvents.onVesselDestroy.Remove(removeFlghtInptHandler);
				GameEvents.OnVesselRecoveryRequested.Remove(removeFlghtInptHandler);
				Utilities.Log_Debug("Input Callbacks removed this vessel");
			}            
		}

		public enum WindowSate
		{
			Small, Large, Hidden
		}

		private void drawWindow(int windowID)
		{
			GUILayout.Box(_camera.Texture2D);
			GUI.DrawTexture(GUILayoutUtility.GetLastRect(), viewfinder);
			GUILayout.BeginHorizontal();
			_saveToFile = GUILayout.Toggle(_saveToFile, new GUIContent(cacheautoLOC_TST_0031, cacheautoLOC_TST_0032));
			if (GUILayout.Button(new GUIContent(cacheautoLOC_TST_0033, cacheautoLOC_TST_0034))) StartCoroutine(fireCamera(_saveToFile));
			if (GUILayout.Button(windowState == WindowSate.Small ? new GUIContent(cacheautoLOC_TST_0035, cacheautoLOC_TST_0036) : new GUIContent(cacheautoLOC_TST_0037, cacheautoLOC_TST_0038)))
			{
				windowState = windowState == WindowSate.Small ? WindowSate.Large : WindowSate.Small;
				int w = (windowState == WindowSate.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE);
				_camera.changeSize(w, w);
				_windowRect.height = 0;
			}
			GUILayout.EndHorizontal();
			if (TSTMstStgs.Instance.TSTsettings.Tooltips)
				Utilities.SetTooltipText();
			GUI.DragWindow();
		}

		public void OnGUI()
		{
            if (Time.timeSinceLevelLoad < 2f) return;
            if (!_inEditor && _camera.Enabled && vessel.isActiveVessel && FlightUIModeController.Instance.Mode != FlightUIMode.ORBITAL && !Utilities.isPauseMenuOpen)
			{
				if (!Textures.StylesSet) Textures.SetupStyles();

				_windowRect = GUILayout.Window(CHMCwindowID, _windowRect, drawWindow, cacheautoLOC_TST_0039, GUILayout.Width(GUI_WIDTH_SMALL));
				if (TSTMstStgs.Instance.TSTsettings.Tooltips)
					Utilities.DrawToolTip();
			}
		}
        public override string GetInfo()
        {
            string infoStr = "";
            infoStr += Localizer.Format("#autoLOC_TST_0040", XKCDColors.HexFormat.Cyan);
            infoStr += Localizer.Format("#autoLOC_TST_0041");
            infoStr += Localizer.Format("#autoLOC_238797", true);
            infoStr += Localizer.Format("#autoLOC_238798", true);
            infoStr += Localizer.Format("#autoLOC_238799", true);
            return infoStr;
        }

        private void refreshFlghtInptHandler(Vessel target)
		{
			Utilities.Log_Debug("OnVesselSwitch curr: {0}, target: {1}" , vessel.name , target.name);
			if (vessel != target)
			{
				Utilities.Log_Debug("This vessel != target removing Callback");
				_vessel.OnAutopilotUpdate -= (handleInput);
			}
				
			if (vessel == target)
			{
				_vessel = target;
				List<TSTChemCam> vpm = _vessel.FindPartModulesImplementing<TSTChemCam>();
				if (vpm.Count > 0)
				{
					Utilities.Log_Debug("Adding Input Callback");
					_vessel.OnAutopilotUpdate += handleInput;
					Utilities.Log_Debug("Added Input Callback");
				}            
			}
			
		}
  
		private void handleInput(FlightCtrlState ctrl)
		{
			if (_camera.Enabled)
			{
				float rotX = _headTransform.localEulerAngles.x;
				if (rotX > 180f) rotX = rotX - 360;
				if (ctrl.X > 0)
				{
					_upperArmTransform.Rotate(Vector3.forward, -0.3f);
				}
				else if (ctrl.X < 0)
				{
					_upperArmTransform.Rotate(Vector3.forward, 0.3f);
				}
				if (ctrl.Y > 0 && rotX > -90)
				{
					_headTransform.Rotate(Vector3.right, -0.3f);
				}
				else if (ctrl.Y < 0 && rotX < 90)
				{
					_headTransform.Rotate(Vector3.right, 0.3f);
				}
			}
		}

		[KSPEvent(guiName = "#autoLOC_TST_0042", active = true, guiActiveEditor = true, guiActive = true)] //#autoLOC_TST_0042 = Open Camera
        public void eventOpenCamera()
		{
			StartCoroutine(openCamera());
		}

		[KSPAction("#autoLOC_TST_0042")] //#autoLOC_TST_0042 = Open Camera
        public void actionOpenCamera(KSPActionParam actParams)
		{
			StartCoroutine(openCamera());
		}

		private IEnumerator openCamera()
		{
			_animationObj.Play("open");
			Events["eventOpenCamera"].active = false;
			Actions["actionOpenCamera"].active = false;
			IEnumerator wait = Utilities.WaitForAnimation(_animationObj, "open");
			while (wait.MoveNext()) yield return null;
			string anim="wiggle"+Random.Range(1,5);
			_animationObj.Play(anim);
			wait = Utilities.WaitForAnimation(_animationObj, anim);
			while (wait.MoveNext()) yield return null;
			Events["eventCloseCamera"].active = true;
			Actions["actionCloseCamera"].active = true;
		    if (_inEditor)
		    {
		        yield break;
		    }
			_camera.Enabled = true;
			Active = true;
			_camera.fov = 80;
			_camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL);
			
		}

		[KSPEvent (guiName = "#autoLOC_TST_0043", guiActiveEditor  = true, active = false, guiActive = true)] //#autoLOC_TST_0043 = Close Camera

        public void eventCloseCamera ()
		{
			StartCoroutine (closeCamera());
		}
		
		[KSPAction("#autoLOC_TST_0043")] //#autoLOC_TST_0043 = Close Camera
        public void actionCloseCamera (KSPActionParam actParams){
			StartCoroutine(closeCamera());
		}

		private IEnumerator closeCamera()
		{
			Events["eventCloseCamera"].active = false;
			Actions["actionCloseCamera"].active = false;
            if (_camera != null)
			    _camera.Enabled = false;    
			Active = false;
			while (_upperArmTransform.localEulerAngles != Vector3.zero || _headTransform.localEulerAngles != Vector3.zero)
			{
				float rotZ = _upperArmTransform.localEulerAngles.z;
				if (rotZ > 180f) rotZ = rotZ - 360;
				float rotX = _headTransform.localEulerAngles.x;
				if (rotX > 180f) rotX = rotX - 360;
				_upperArmTransform.Rotate(Vector3.forward, Mathf.Clamp(rotZ* -0.3f,-2,2));
				_headTransform.Rotate(Vector3.right, Mathf.Clamp(rotX * -0.3f,-2,2));
				if (_upperArmTransform.localEulerAngles.magnitude < 0.5f) _upperArmTransform.localEulerAngles = Vector3.zero;
				if (_headTransform.localEulerAngles.magnitude < 0.5f) _headTransform.localEulerAngles = Vector3.zero;
				yield return null;
			}
			_animationObj.Play("close");
			IEnumerator wait = Utilities.WaitForAnimation(_animationObj, "close");
			while (wait.MoveNext()) yield return null;
			Events["eventOpenCamera"].active = true;
			Actions["actionOpenCamera"].active = true;
		}


		[KSPEvent(active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "Collect Data", unfocusedRange = 2)]
		public void eventCollectDataExternal()
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

		private IEnumerator fireCamera(bool saveToFile)
		{
			_lazerObj.enabled = true;
			yield return new WaitForSeconds(0.75f);
			_lazerObj.enabled = false;
			RaycastHit hit = new RaycastHit();
			if (Physics.Raycast(_lazerObj.transform.position, _lookTransform.forward, out hit))
			{
				if (hit.distance < 10f)
				{
					Utilities.Log_Debug("Hit Planet");
					Transform t = hit.collider.transform;
				    bool traversing = true;
				    while (traversing)
				    {
				        for (int pnI = 0; pnI < PlanetNames.Count; pnI++)
				        {
				            if (t.name.Contains(PlanetNames[pnI]))
				            {
				                traversing = false;
                                break;
				            }
				        }
				        if (traversing)
				        {
				            //not found yet, go up to parent.
				            if (t.parent == null)
				            {
				                traversing = false; //If parent is null we are done.
				            }
				            t = t.parent; // go to parent.
				        }
				    }
				    if (t != null) //We found a match.
					{
                        CelestialBody body = FlightGlobals.Bodies.Find(c=>t.name.Contains(c.bodyName));
					    if (body != null)
					    {
					        doScience(body);
					    }
					    else
					    {
					        ScreenMessages.PostScreenMessage(cacheautoLOC_TST_0048, 3f, ScreenMessageStyle.UPPER_CENTER);
                        }
					}
					else
					{
						ScreenMessages.PostScreenMessage(cacheautoLOC_TST_0048, 3f, ScreenMessageStyle.UPPER_CENTER);
					}
				}
			}
			if (saveToFile)
			{
				Utilities.Log_Debug("Saving to File");
				int i = 0;
				while ((File.Exists<TSTChemCam>("ChemCam_" + DateTime.Now.ToString("d-m-y") + "_" + i + ".png")) ||
					(File.Exists<TSTChemCam>("ChemCam_" + DateTime.Now.ToString("d-m-y") + "_" + i + "Large.png")))
					i++;
				_camera.saveToFile("ChemCam_" + DateTime.Now.ToString("d-m-y") + "_" + i, "ChemCam");
				ScreenMessages.PostScreenMessage(cacheautoLOC_TST_0049, 3f, ScreenMessageStyle.UPPER_CENTER);
			}			
		}

		public void doScience(CelestialBody planet)
		{
			Utilities.Log_Debug("Doing Science for {0}" , planet.name);
			ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(ExperimentID);
			Utilities.Log_Debug("Got experiment");
            ExperimentSituations situation = ScienceUtil.GetExperimentSituation(vessel);
            if (experiment.IsAvailableWhile(ScienceUtil.GetExperimentSituation(vessel), planet))
			{
                string biomeID = "";
                string displaybiomeID = string.Empty;
                if (part.vessel.landedAt != string.Empty)
                {
                    biomeID = Vessel.GetLandedAtString(part.vessel.landedAt);
                    displaybiomeID = Localizer.Format(part.vessel.displaylandedAt);
                }
                else
                {
                    biomeID = ScienceUtil.GetExperimentBiome(planet, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude);
                    displaybiomeID = ScienceUtil.GetBiomedisplayName(vessel.mainBody, biomeID);
                }                
                ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, situation, planet, biomeID, displaybiomeID);
                Utilities.Log_Debug("Got subject");
                ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title, false, part.flightID);
				Utilities.Log_Debug("Got data");
				_scienceData.Add(data);
				Utilities.Log_Debug("Added Data");				
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_238419", part.partInfo.title, data.dataAmount.ToString(), subject.title), 8f, ScreenMessageStyle.UPPER_LEFT);
                ReviewDataItem(data);
                if (TSTProgressTracker.isActive)
				{
					TSTProgressTracker.OnChemCamFire(planet,biomeID);
				}
			}
            else
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_238424", experiment.experimentTitle), 5f, ScreenMessageStyle.UPPER_CENTER);
            }
			updateAvailableEvents();
		}

		private void updateAvailableEvents()
		{
			if (_scienceData.Count > 0)
			{
				Events["eventReviewScience"].active = true;
				Events["eventCollectDataExternal"].active = true;
			}
			else
			{
				Events["eventReviewScience"].active = false;
				Events["eventCollectDataExternal"].active = false;
			}
		}

		[KSPEvent(active = false, guiActive = true, guiName = "Check Results")]
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
                ScreenMessages.PostScreenMessage(cacheautoLOC_TST_0079, 3f, ScreenMessageStyle.UPPER_CENTER);
            }
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

		public override void OnSave(ConfigNode node)
		{
			foreach (ScienceData data in _scienceData)
			{
				data.Save(node.AddNode("ScienceData"));
			}
			TSTMstStgs.Instance.TSTsettings.CwindowPosX = _windowRect.x;
			TSTMstStgs.Instance.TSTsettings.CwindowPosY = _windowRect.y; 
		}

		public override void OnLoad(ConfigNode node)
		{
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
			_windowRect.x = TSTMstStgs.Instance.TSTsettings.CwindowPosX;
			_windowRect.y = TSTMstStgs.Instance.TSTsettings.CwindowPosY;            
			GUI_WIDTH_SMALL = TSTMstStgs.Instance.TSTsettings.ChemwinSml;
			GUI_WIDTH_LARGE = TSTMstStgs.Instance.TSTsettings.ChemwinLge;
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
					true,
		            Localizer.Format("#autoLOC_TST_0051", Mathf.Round(data.baseTransmitValue * 100)),
                    true,
					labSearch,
					_onPageDiscard,
					_onPageKeep,
					_onPageTransmit,
					_onPageSendToLab);
			ExperimentsResultDialog.DisplayResult(page);
		}
	}
}
