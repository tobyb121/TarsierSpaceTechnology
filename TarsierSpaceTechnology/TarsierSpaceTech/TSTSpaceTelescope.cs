using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngineInternal;

namespace TarsierSpaceTech
{
    public class TSTSpaceTelescope : PartModule, IScienceDataContainer
    {
        private const int GUI_WIDTH_SMALL = 256;
        private const int GUI_WIDTH_LARGE = 512;

        private bool _inEditor = false;
        private Animation _animation;
        private Transform _baseTransform;
        private Transform _cameraTransform;
        private Transform _lookTransform;
        private TSTCameraModule _camera;

        private bool _showTarget = false;
        
        private bool _saveToFile = false;
        
        private List<ScienceData> _scienceData = new List<ScienceData>();

        private Rect windowPos = new Rect(128, 128, 0, 0);
        private WindowSate windowState = WindowSate.Small;
        private Rect targetWindowPos = new Rect(512, 128, 0, 0);
        private bool showTargetsWindow = false;
        int selectedTargetIndex = -1;

        [KSPField(guiActive = false, guiName = "maxZoom", isPersistant = true)]
        public int maxZoom = 5;

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

        public float labBoostScalar = 0f;

        private int targetId = 0;

        private FlightInputCallback onFlyByWire;

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

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            Utils.print("Starting Telescope");
            part.CoMOffset = part.attachNodes[0].position;
            if (state == StartState.Editor)
            {
                _inEditor = true;
                return;
            }
            _baseTransform = Utils.FindChildRecursive(transform,baseTransformName);
            _cameraTransform = Utils.FindChildRecursive(transform,cameraTransformName);
            _lookTransform = Utils.FindChildRecursive(transform,lookTransformName);
            Utils.print(_baseTransform);
            Utils.print(_cameraTransform);
            zeroRotation = _cameraTransform.localRotation;
            _camera = _cameraTransform.gameObject.AddComponent<TSTCameraModule>();
            _animation = _baseTransform.animation;
            Events["eventOpenCamera"].active = true;
            Events["eventCloseCamera"].active = false;
            Events["eventShowGUI"].active = false;
            Events["eventControlFromHere"].active = false;
            Events["eventReviewScience"].active = false;
            for (int i = 0; i < targets_raw.Count; i++)
            {
                Texture2D tex = new Texture2D(40, 40);
                tex.LoadImage(targets_raw[i]);
                targets.Add(tex);
            }
            Utils.print("Getting ExpIDs");
            foreach (String expID in ResearchAndDevelopment.GetExperimentIDs())
            {
                Utils.print("Got ExpID: " + expID);
            }
            Utils.print("Got ExpIDs");
            Utils.print("On end start");

            onFlyByWire = new FlightInputCallback(onFlightInput);
            Utils.print("Adding Input Callback");
            vessel.OnFlyByWire += onFlyByWire;
            Utils.print("Added Input Callback");

            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(refreshFlightInputHandler));
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
            if (vessel != FlightGlobals.ActiveVessel && vessel.OnFlyByWire.GetInvocationList().Contains(onFlyByWire))
                vessel.OnFlyByWire -= onFlyByWire;
            Utils.print("OnVesselSwitch");
            if (!vessel.OnFlyByWire.GetInvocationList().Contains(onFlyByWire))
            {
                Utils.print("Adding Input Callback");
                vessel.OnFlyByWire += onFlyByWire;
                Utils.print("Added Input Callback");
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
            if (!_inEditor && _camera.Enabled && windowState != WindowSate.Hidden && vessel.isActiveVessel)
            {
                _camera.draw();
            }
        }

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
            if (GUILayout.Button(showTargetsWindow?"Hide Galaxies":"Show Galaxies")) showTargetsWindow = !showTargetsWindow;
            if (GUILayout.Button("Hide")) hideGUI();
			GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            _showTarget = GUILayout.Toggle(_showTarget, "Show Target");
            _saveToFile = GUILayout.Toggle(_saveToFile, "Save To File");
            if (GUILayout.Button("Take Picture")) takePicture(_saveToFile);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void TargettingWindow(int windowID)
        {
            GUILayout.BeginVertical();

            int newTarget = TSTGalaxies.Galaxies.
                FindIndex(
                    g => (
                        TSTProgressTracker.HasTelescopeCompleted(g) ||
                        (Contracts.ContractSystem.Instance &&
                            Contracts.ContractSystem.Instance.GetCurrentActiveContracts<TSTTelescopeContract>()
                                .Any(t => t.target.name == g.name)
                        )
                ) ? GUILayout.Button(g.theName) : false);

            if (newTarget != -1 && newTarget != selectedTargetIndex)
            {
                vessel.targetObject = null;
                FlightGlobals.fetch.SetVesselTarget(null);
                targettingMode = TargettingMode.Galaxy;
                selectedTargetIndex = newTarget;
                galaxyTarget = TSTGalaxies.Galaxies[selectedTargetIndex];
                Utils.print("Targetting: " + newTarget.ToString() + " " + galaxyTarget.name);
                ScreenMessages.PostScreenMessage("Target: "+galaxyTarget.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
            }

            GUILayout.Space(10);
            showTargetsWindow = !GUILayout.Button("Hide");
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if (!_inEditor && FlightUIModeController.Instance.Mode != FlightUIMode.ORBITAL && _camera.Enabled && windowState != WindowSate.Hidden && vessel.isActiveVessel)
            {
                windowPos = GUILayout.Window(1, windowPos, WindowGUI, "Space Telescope", GUILayout.Width(windowState == WindowSate.Small ? GUI_WIDTH_SMALL : GUI_WIDTH_LARGE));
                if(showTargetsWindow)
                    targetWindowPos = GUILayout.Window(2, targetWindowPos, TargettingWindow, "Select Target", GUILayout.Width(GUI_WIDTH_SMALL));
            }
        }

        private void hideGUI()
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

        [KSPEvent(active = true, guiActive = true, name = "eventOpenCamera", guiName = "Open Camera")]
        public void eventOpenCamera()
        {
            Events["eventOpenCamera"].active = false;
            StartCoroutine(openCamera());
        }

        public IEnumerator openCamera()
        {
            _animation.Play("open");
            IEnumerator wait = Utils.WaitForAnimation(_animation, "open");
            while (wait.MoveNext()) yield return null;
            Events["eventCloseCamera"].active = true;
            Events["eventControlFromHere"].active = true;
            Events["toggleServos"].active = true;
            _camera.Enabled = true;
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
            StartCoroutine(closeCamera());
            if (vessel.ReferenceTransform == _lookTransform)
            {
                vessel.FallBackReferenceTransform();
            }
        }

        public IEnumerator closeCamera()
        {
            _animation.Play("close");
            IEnumerator wait = Utils.WaitForAnimation(_animation, "close");
            while (wait.MoveNext()) yield return null;
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
            Utils.print("Taking Picture");
            _scienceData.Clear();
            Utils.print("Checking Look At");
            List<TargetableObject> objs=getLookingAt();
            Utils.print("Looking at: " + objs.Count.ToString() + " celestial objects");
            foreach (TargetableObject obj in objs)
            {
                Utils.print("Looking at " + obj.theName);
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
            Utils.print("Gather Science complete");
            if (objs.Count == 0)
            {
                ScreenMessages.PostScreenMessage("Nothing to see here",3f,ScreenMessageStyle.UPPER_CENTER);
            }

            if (saveToFile)
            {
                Utils.print("Saving to File");
                int i = 0;
                while (KSP.IO.File.Exists<TSTSpaceTelescope>("Telescope_" + DateTime.Now.ToString("d-m-y")+"_"+i.ToString() + ".png",null)) i++;
                _camera.saveToFile("Telescope_" + DateTime.Now.ToString("d-m-y") + "_" + i.ToString() + ".png");
            }
        }

        private List<TargetableObject> getLookingAt()
        {
            List<TargetableObject> result = new List<TargetableObject>();
            List<TargetableObject> bodies = FlightGlobals.Bodies.Select(b => (TargetableObject)b).ToList();
            List<TargetableObject> galaxies = TSTGalaxies.Galaxies.Select(g => (TargetableObject)g).ToList();
            foreach (TargetableObject obj in galaxies.Concat(bodies))
            {
                Vector3 r = (obj.position - _cameraTransform.position);
                float distance = r.magnitude;
                double theta = Vector3d.Angle(_cameraTransform.forward, r);
                double visibleWidth = (2 * obj.size / distance) * 180 / Mathf.PI;
                Utils.print(obj.theName + ": |r|=" + distance.ToString() + "  theta=" + theta.ToString() + "  angle=" + visibleWidth.ToString());
                if (theta < _camera.fov / 2)
                {
                    Utils.print("Looking at: " + obj.theName);
                    if (visibleWidth > 0.05 * _camera.fov)
                    {
                        Utils.print("Can see: " + obj.theName); 
                        result.Add(obj);
                    }
                }
            }
            return result;
        }

        public void doScience(TSTGalaxy galaxy)
        {
            Utils.print("Doing Science for " + galaxy.theName);
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
            Utils.print("Got experiment");
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(), Sun.Instance.sun, "LookingAt" + galaxy.name);
            subject.title = "Space Telescope picture of " + galaxy.theName;
            Utils.print("Got subject");
            if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
            {
                ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title);
                Utils.print("Got data");
                data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " + galaxy.theName;
                _scienceData.Add(data);
                Utils.print("Added Data");
                ScreenMessages.PostScreenMessage("Collected Science for " + galaxy.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        public void doScience(CelestialBody planet)
        {
            Utils.print("Doing Science for " + planet.theName);
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment("TarsierSpaceTech.SpaceTelescope");
            Utils.print("Got experiment");
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, getExperimentSituation(), planet, "LookingAt" + planet.name);
            subject.title = "Space Telescope picture of "+planet.theName;
            Utils.print("Got subject");
            if (experiment.IsAvailableWhile(getExperimentSituation(), vessel.mainBody))
            {
                ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title);
                Utils.print("Got data");
                data.title = "Tarsier Space Telescope: Oriting " + vessel.mainBody.theName + " looking at " + planet.theName;
                _scienceData.Add(data);
                Utils.print("Added Data");
                ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName,3f,ScreenMessageStyle.UPPER_CENTER);
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

        private enum WindowSate
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
            Utils.print("Sent to lab");
        }

        public override void OnSave(ConfigNode node)
        {
            foreach (ScienceData data in _scienceData)
            {
                data.Save(node.AddNode("ScienceData"));
            }
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


        public bool IsRerunnable()
        {
            Utils.print("Is rerunnable");
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
    }
}
