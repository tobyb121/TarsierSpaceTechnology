using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TelescopeCamera : MonoBehaviour
    {
        private int textureWidth = 256;
        private int textureHeight = 256;
        
        private CameraHelper _skyBoxCam;
        private CameraHelper _farCam;
        private CameraHelper _nearCam;

        private CameraHelper _VECam;
        private bool _VEenabled = false;

        private RenderTexture _renderTexture;
        private Texture2D _texture2D;
        private Renderer[] skyboxRenderers;
        private ScaledSpaceFader[] scaledSpaceFaders;
        public Texture2D Texture2D
        {
            get { return _texture2D; }
        }

        private float _zoomLevel = 0;
        public float ZoomLevel
        {
            get { return _zoomLevel; }
            set { _zoomLevel = value; updateZoom(); }
        }

        public float fov
        {
            get { return _nearCam.fov; }
        }

        private bool _enabled = false;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _skyBoxCam.enabled = value;
                if (_VEenabled) _VECam.enabled = value;
                _farCam.enabled = value;
                _nearCam.enabled = value;
                skyboxRenderers = (from Renderer r in (FindObjectsOfType(typeof(Renderer)) as IEnumerable<Renderer>) where (r.name == "XP" || r.name == "XN" || r.name == "YP" || r.name == "YN" || r.name == "ZP" || r.name == "ZN") select r).ToArray<Renderer>();
                scaledSpaceFaders = FindObjectsOfType(typeof(ScaledSpaceFader)) as ScaledSpaceFader[];
            }
        }

        private float _size;
        public float size
        {
            get { return _size; }
            set { _size = value; }
        }

        public void Start()
        {
            Utils.print("Setting up cameras");
            _skyBoxCam = new CameraHelper(gameObject, Utils.findCameraByName("Camera ScaledSpace"), _renderTexture, 3, false);

            Camera VEcam = Utils.findCameraByName("Camera VE Overlay");
            if (VEcam != null)
            {
                _VEenabled = true;
                _VECam = new CameraHelper(gameObject, VEcam, _renderTexture, 4, false);
            }
            _farCam = new CameraHelper(gameObject, Utils.findCameraByName("Camera 01"), _renderTexture, 5, true);
            _nearCam = new CameraHelper(gameObject, Utils.findCameraByName("Camera 00"), _renderTexture, 6, true);
            setupRenderTexture();
            _skyBoxCam.reset();
            _farCam.reset();
            if (_VEenabled) _VECam.reset();
            _nearCam.reset();
            Utils.print("Camera setup complete");
        }

        public void Update()
        {
            if (_enabled)
            {
                _skyBoxCam.reset();
                if (_VEenabled) _VECam.reset();
                draw();
            }
        }

        private void updateZoom()
        {
            float z = Mathf.Pow(10, -_zoomLevel);
            float fov = Mathf.Rad2Deg * Mathf.Atan(z * Mathf.Tan(Mathf.Deg2Rad * CameraHelper.DEFAULT_FOV));
            _skyBoxCam.fov = fov;
            if (_VEenabled) _VECam.fov = fov;
            _farCam.fov = fov;
            _nearCam.fov = fov;           
        }

        public void changeSize(int width, int height)
        {
            textureWidth = width;
            textureHeight = height;
            setupRenderTexture();
        }

        private void setupRenderTexture()
        {
            Utils.print("Setting Up Render Texture");
            if(_renderTexture)
                _renderTexture.Release();
            _renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTexture.Create();
            _texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false, false);
            _skyBoxCam.renderTarget = _renderTexture;
            if (_VEenabled) _VECam.renderTarget = _renderTexture;
            _farCam.renderTarget = _renderTexture;
            _nearCam.renderTarget = _renderTexture;
            Utils.print("Finish Setting Up Render Texture");
        }

        public Texture2D draw()
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            _skyBoxCam.camera.Render();
            foreach (Renderer r in skyboxRenderers)
                r.enabled = false;
            foreach (ScaledSpaceFader s in scaledSpaceFaders)
                s.r.enabled = true;
            _skyBoxCam.camera.clearFlags = CameraClearFlags.Depth;
            _skyBoxCam.camera.farClipPlane = 3e15f;
            _skyBoxCam.camera.Render();
            foreach (Renderer r in skyboxRenderers)
                r.enabled = true;
            if (_VEenabled) _VECam.camera.Render();
            _farCam.camera.Render();
            _nearCam.camera.Render();
            _texture2D.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0);
            _texture2D.Apply();
            RenderTexture.active = activeRT;
            return _texture2D;
        }

        public void saveToFile(string fileName)
        {
            byte[] data = _texture2D.EncodeToPNG();
            using (KSP.IO.FileStream file = KSP.IO.File.Open<SpaceTelescope>(fileName, KSP.IO.FileMode.Create,null))
            {
                file.Write(data, 0, data.Length);
            }
            
        }
    }

    internal class CameraHelper
    {
        public CameraHelper(GameObject parent, Camera copyFrom, RenderTexture renderTarget, float depth, bool attachToParent)
        {
            _copyFrom = copyFrom;
            _parent = parent;
            _go = new GameObject();
            _camera = _go.AddComponent<Camera>();
            _camera.enabled = false;
            _depth = depth;
            _attachToParent = attachToParent;
            _renderTarget = renderTarget;
            _camera.targetTexture = _renderTarget;
        }

        public const float DEFAULT_FOV = 60f;
        private Camera _camera;
        public Camera camera
        {
            get { return _camera; }
        }

        private Camera _copyFrom;
        private float _depth;
        private GameObject _go;
        private GameObject _parent;
        private bool _attachToParent;

        private RenderTexture _renderTarget;
        public RenderTexture renderTarget
        {
            get { return _renderTarget; }
            set {
                _renderTarget = value;
                _camera.targetTexture = _renderTarget;
            }
        }

        private float _fov = CameraHelper.DEFAULT_FOV;
        public float fov
        {
            get { return _fov; }
            set
            {
                _fov = value;
                _camera.fieldOfView = _fov;
            }
        }

        public bool enabled
        {
            get { return _camera.enabled; }
            set { _camera.enabled = value; }
        }

        public void reset()
        {
            _camera.CopyFrom(_copyFrom);
            _camera.targetTexture = _renderTarget;
            if (_attachToParent)
            {
                _go.transform.parent = _parent.transform;
                _go.transform.localPosition = Vector3.zero;
                _go.transform.localEulerAngles = Vector3.zero;
            }
            else
            {
                _go.transform.rotation = _parent.transform.rotation;
            }
            _camera.rect = new Rect(0, 0, 1, 1);
            _camera.depth = _depth;
            _camera.fieldOfView = _fov;
            _camera.enabled = enabled;
        }
    }
}
