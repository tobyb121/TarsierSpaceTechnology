using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    class ChemCam : ModuleScienceExperiment
    {
        private bool _inEditor = false;

        private const int GUI_WIDTH_SMALL = 256;
        private const int GUI_WIDTH_LARGE = 512;

        private Transform _lookTransform;
        private CameraModule _camera;

        private Transform _lazerTransform;
        private LineRenderer _lazerObj;

        private Transform _headTransform;
        private Transform _upperArmTransform;
        private Animation _animationObj;

        private Rect _windowRect=new Rect();

        private int frameLimit = 5;
        private int f = 0;

        private static Texture2D viewfinder = new Texture2D(1, 1);

        private static List<string> PlanetNames = (from CelestialBody b in FlightGlobals.Bodies select b.name).ToList();

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
            _camera=_lookTransform.gameObject.AddComponent<CameraModule>();

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

            Utils.print("Adding Input Callback");
            vessel.OnFlyByWire += new FlightInputCallback(handleInput);

            viewfinder.LoadImage(Properties.Resources.viewfinder);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!_inEditor)
            {
                if (_camera.Enabled && f++ % frameLimit == 0)
                {
                    _camera.draw();
                }
            }
        }

        private void drawWindow(int windowID)
        {
            GUILayout.Box(_camera.Texture2D);
            GUI.DrawTexture(GUILayoutUtility.GetLastRect(), viewfinder);
            if (GUILayout.Button("Fire")) CollectData();
            GUI.DragWindow();
        }

        public void OnGUI()
        {
            if (!_inEditor && _camera.Enabled)
            {
                _windowRect = GUILayout.Window(1, _windowRect, drawWindow, "ChemCam");
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

        [KSPEvent(guiName = "Deploy", active = true, guiActive = true)]
        new public void DeployExperiment()
        {
            StartCoroutine(openCamera());
        }

        new public void DeployAction(KSPActionParam actParams)
        {
            StartCoroutine(openCamera());
        }

        private IEnumerator openCamera()
        {
            _animationObj.Play("open");
            Events["DeployExperiment"].active = false;
            Actions["DeployAction"].active = false;
            IEnumerator wait = Utils.WaitForAnimation(_animationObj, "open");
            while (wait.MoveNext()) yield return null;
            string anim="wiggle"+UnityEngine.Random.Range(1,5).ToString();
            _animationObj.Play(anim);
            wait = Utils.WaitForAnimation(_animationObj, anim);
            while (wait.MoveNext()) yield return null;
            Events["CollectData"].active = true;
            Events["ResetExperiment"].active = true;
            Events["ResetExperimentExternal"].active = true;
            Actions["ResetAction"].active = true;
            _camera.Enabled = true;
            _camera.fov = 100;
            _camera.changeSize(GUI_WIDTH_SMALL, GUI_WIDTH_SMALL);
            if (_camera.gameObject.GetComponentInChildren<NoiseEffect>() == null)
            {
                NoiseEffect noise = _camera._nearCam.camera.gameObject.AddComponent<NoiseEffect>();
                noise.shaderRGB = Shader.Find("Hidden/Noise Shader RGB");
                noise.shaderYUV = Shader.Find("Hidden/Noise Shader YUV");
                noise.monochrome = true;
                Texture2D scratch = new Texture2D(1, 1);
                scratch.LoadImage(Properties.Resources.NoiseEffectScratch);
                noise.scratchTexture = scratch;
                Texture2D grain = new Texture2D(1, 1);
                grain.LoadImage(Properties.Resources.NoiseEffectGrain);
                noise.grainTexture = grain;
                noise.grainSize = 2f;
                noise.grainIntensityMax = 0.5f;
                noise.grainIntensityMin = 0.2f;
            }
        }

        [KSPEvent (guiName = "Close", active = true, guiActive = true)]
	    new public void ResetExperiment ()
	    {
		    StartCoroutine (closeCamera());
	    }
	    
        new public void ResetAction (KSPActionParam actParams){
            StartCoroutine(closeCamera());
        }

        [KSPEvent(guiName = "Close", active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false)]
        new public void ResetExperimentExternal()
        {
            StartCoroutine (closeCamera());
        }

        private IEnumerator closeCamera()
        {
            Events["CollectData"].active = false;
            Events["ResetExperiment"].active = false;
            Events["ResetExperimentExternal"].active = false;
            Actions["ResetAction"].active = false;
            _camera.Enabled = false;
            while (_upperArmTransform.localEulerAngles != Vector3.zero && _headTransform.localEulerAngles != Vector3.zero)
            {
                float rotZ = _upperArmTransform.localEulerAngles.z;
                if (rotZ > 180f) rotZ = rotZ - 360;
                float rotX = _headTransform.localEulerAngles.x;
                if (rotX > 180f) rotX = rotX - 360;
                _upperArmTransform.Rotate(Vector3.forward, Mathf.Clamp(rotZ* -0.3f,-10,10));
                _headTransform.Rotate(Vector3.right, Mathf.Clamp(rotX * -0.3f,-10,10));
                if (_upperArmTransform.localEulerAngles.magnitude < 0.5f) _upperArmTransform.localEulerAngles = Vector3.zero;
                if (_headTransform.localEulerAngles.magnitude < 0.5f) _headTransform.localEulerAngles = Vector3.zero;
                yield return null;
            }
            _animationObj.Play("close");
            IEnumerator wait = Utils.WaitForAnimation(_animationObj, "close");
            while (wait.MoveNext()) yield return null;
            Events["DeployExperiment"].active = true;
            Actions["DeployAction"].active = true;
        }

        [KSPEvent(active = false, guiActive = true, guiName = "Collect Data")]
        public void CollectData()
        {
            StartCoroutine(fireCamera());
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
                        base.DeployExperiment();
                        yield break;
                    }
                }
            }
            ScreenMessages.PostScreenMessage("No Terrain in Range");
        }
    }
}
