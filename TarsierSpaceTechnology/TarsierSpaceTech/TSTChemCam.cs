using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TSTChemCam : PartModule, IScienceDataContainer
    {
        private bool _inEditor = false;

        private const int GUI_WIDTH_SMALL = 256;
        private const int GUI_WIDTH_LARGE = 512;

        private Transform _lookTransform;
        private TSTCameraModule _camera;

        private Transform _lazerTransform;
        private LineRenderer _lazerObj;

        private Transform _headTransform;
        private Transform _upperArmTransform;
        private Animation _animationObj;

        private Rect _windowRect=new Rect();

        private int frameLimit = 5;
        private int f = 0;
        
        private List<ScienceData> _scienceData = new List<ScienceData>();

        private static Texture2D viewfinder = new Texture2D(1, 1);

        private static List<string> PlanetNames;

        [KSPField]
        public float xmitDataScalar = 0.5f;

        [KSPField]
        public string ExperimentID = "TarsierSpaceTech.ChemCam";

        public float labBoostScalar = 0f;

        private VesselAutopilotUI ui;
        private Vessel _vessel;


        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (state == StartState.Editor)
            {
                _inEditor = true;
                return;
            }

            Utils.print("Starting ChemCam");
            _lookTransform = Utils.FindChildRecursive(transform,"CameraTransform");
            _camera=_lookTransform.gameObject.AddComponent<TSTCameraModule>();

            Utils.print("Adding Lazer");
            _lazerTransform = Utils.FindChildRecursive(transform, "LazerTransform");
            _lazerObj = _lazerTransform.gameObject.AddComponent<LineRenderer>();
            _lazerObj.enabled = false;
            _lazerObj.castShadows = false;
            _lazerObj.receiveShadows = false;
            _lazerObj.SetWidth(0.01f, 0.01f);
            _lazerObj.SetPosition(0, new Vector3(0, 0, 0));
            _lazerObj.SetPosition(1, new Vector3(0, 0, 5));
            _lazerObj.useWorldSpace = false;
            _lazerObj.material = new Material(Shader.Find("Particles/Additive"));
            _lazerObj.material.color = Color.red;
            _lazerObj.SetColors(Color.red, Color.red);

            Utils.print("Finding Camera Transforms");
            _headTransform = Utils.FindChildRecursive(transform, "CamBody");
            _upperArmTransform = Utils.FindChildRecursive(transform, "ArmUpper");

            Utils.print("Finding Animation Object");
            _animationObj = Utils.FindChildRecursive(transform, "ChemCam").animation;

            viewfinder.LoadImage(Properties.Resources.viewfinder);

            PlanetNames = (from CelestialBody b in FlightGlobals.Bodies select b.name).ToList();
            
            Utils.print("Adding Input Callback");            
            ui = VesselAutopilotUI.FindObjectOfType<VesselAutopilotUI>();
            _vessel = FlightGlobals.ActiveVessel;
            vessel.OnAutopilotUpdate += new FlightInputCallback(handleInput);
            Utils.print("Added Input Callback");

            GameEvents.onVesselChange.Add(new EventData<Vessel>.OnEvent(refreshFlightInputHandler));

            Events["eventOpenCamera"].active = true;
            Actions["actionOpenCamera"].active = true;
            Events["eventCloseCamera"].active = false;
            Actions["actionCloseCamera"].active = false;
            updateAvailableEvents();
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

        public override void OnInactive()
        {
            Utils.print("Removing Input Callback");
            _vessel.OnAutopilotUpdate -= new FlightInputCallback(handleInput);
            GameEvents.onVesselChange.Remove(new EventData<Vessel>.OnEvent(refreshFlightInputHandler));
            base.OnInactive();
        }

        private void drawWindow(int windowID)
        {
            GUILayout.Box(_camera.Texture2D);
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), viewfinder);
            if (GUILayout.Button("Fire")) StartCoroutine(fireCamera());
            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if (!_inEditor && _camera.Enabled && vessel.isActiveVessel && FlightUIModeController.Instance.Mode != FlightUIMode.ORBITAL)
            {
                _windowRect = GUILayout.Window(1, _windowRect, drawWindow, "ChemCam - Use I,J,K,L to move camera");
            }
        }

        private void refreshFlightInputHandler(Vessel target)
        {
            Utils.print("OnVesselSwitch");
            //if (vessel != FlightGlobals.ActiveVessel && vessel.OnFlyByWire.GetInvocationList().Contains(onFlyByWire))
            //		vessel.OnFlyByWire -= onFlyByWire;
            _vessel.OnAutopilotUpdate -= new FlightInputCallback(handleInput);
            _vessel = target;

            //if (!vessel.OnFlyByWire.GetInvocationList().Contains(onFlyByWire))
            //{
            Utils.print("Adding Input Callback");
            //vessel.OnFlyByWire += onFlyByWire;
            _vessel.OnAutopilotUpdate += new FlightInputCallback(handleInput);
            Utils.print("Added Input Callback");
            //}
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

        [KSPEvent(guiName = "Open Camera", active = true, guiActive = true)]
        public void eventOpenCamera()
        {
            StartCoroutine(openCamera());
        }

        [KSPAction("Open Camera")]
        public void actionOpenCamera(KSPActionParam actParams)
        {
            StartCoroutine(openCamera());
        }

        private IEnumerator openCamera()
        {
            _animationObj.Play("open");
            Events["eventOpenCamera"].active = false;
            Actions["actionOpenCamera"].active = false;
            IEnumerator wait = Utils.WaitForAnimation(_animationObj, "open");
            while (wait.MoveNext()) yield return null;
            string anim="wiggle"+UnityEngine.Random.Range(1,5).ToString();
            _animationObj.Play(anim);
            wait = Utils.WaitForAnimation(_animationObj, anim);
            while (wait.MoveNext()) yield return null;
            Events["eventCloseCamera"].active = true;
            Actions["actionCloseCamera"].active = true;
            _camera.Enabled = true;
            _camera.fov = 80;
            _camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL);
            
        }

        [KSPEvent (guiName = "Close Camera", active = false, guiActive = true)]
	    public void eventCloseCamera ()
	    {
		    StartCoroutine (closeCamera());
	    }
	    
        [KSPAction("Close Camera")]
        public void actionCloseCamera (KSPActionParam actParams){
            StartCoroutine(closeCamera());
        }

        private IEnumerator closeCamera()
        {
            Events["eventCloseCamera"].active = false;
            Actions["actionCloseCamera"].active = false;
            _camera.Enabled = false;
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
            IEnumerator wait = Utils.WaitForAnimation(_animationObj, "close");
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
                    if (container.StoreData(new List<IScienceDataContainer>() { this }, false))
                        ScreenMessages.PostScreenMessage("Transferred Data to " + vessel.vesselName, 3f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
            updateAvailableEvents();
        }

        private IEnumerator fireCamera()
        {
            _lazerObj.enabled = true;
            yield return new WaitForSeconds(0.75f);
            _lazerObj.enabled = false;
            RaycastHit hit = new RaycastHit();
            if (Physics.Raycast(_lazerObj.transform.position, _lookTransform.forward, out hit))
            {
                if (hit.distance < 10f)
                {
                    Utils.print("Hit Planet");
                    Transform t = hit.collider.transform;
                    while (t != null)
                    {
                        if (PlanetNames.Contains(t.name))
                            break;
                        t = t.parent;
                    }
                    if (t != null)
                    {
                        CelestialBody body=FlightGlobals.Bodies.Find(c=>c.name==t.name);
                        doScience(body);
                        yield break;
                    }
                }
            }
            ScreenMessages.PostScreenMessage("No Terrain in Range",3f,ScreenMessageStyle.UPPER_CENTER);
        }

        public void doScience(CelestialBody planet)
        {
            Utils.print("Doing Science for " + planet.theName);
            ScienceExperiment experiment = ResearchAndDevelopment.GetExperiment(ExperimentID);
            Utils.print("Got experiment");
            string biome = "";
            if (part.vessel.landedAt != string.Empty)
                biome = part.vessel.landedAt;
            else
                biome = ScienceUtil.GetExperimentBiome(planet, FlightGlobals.ship_latitude, FlightGlobals.ship_longitude);
            ScienceSubject subject = ResearchAndDevelopment.GetExperimentSubject(experiment, ScienceUtil.GetExperimentSituation(vessel), planet, biome);
            Utils.print("Got subject");
            if (experiment.IsAvailableWhile(ScienceUtil.GetExperimentSituation(vessel), planet))
            {
                ScienceData data = new ScienceData(experiment.baseValue * subject.dataScale, xmitDataScalar, labBoostScalar, subject.id, subject.title);
                Utils.print("Got data");
                _scienceData.Add(data);
                Utils.print("Added Data");
                ScreenMessages.PostScreenMessage("Collected Science for " + planet.theName, 3f, ScreenMessageStyle.UPPER_CENTER);
                if (TSTProgressTracker.isActive)
                {
                    TSTProgressTracker.OnChemCamFire(planet,biome);
                }
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
            List<IScienceDataTransmitter> transmitters = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0 && _scienceData.Contains(data))
            {
                transmitters.First().TransmitData(new List<ScienceData> { data });
                _scienceData.Remove(data);
                updateAvailableEvents();
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
    }
}
