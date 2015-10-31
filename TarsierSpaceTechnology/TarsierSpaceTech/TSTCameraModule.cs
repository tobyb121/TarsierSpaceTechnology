/*
 * TSTCameraModule.cs
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
    class TSTCameraModule : MonoBehaviour
    {
        private int textureWidth = 256; 
        private int textureHeight = 256;
        // Standard Cameras        
        public CameraHelper _skyBoxCam;
        public CameraHelper _farCam;
        public CameraHelper _nearCam;
        // FullScreen Cameras        
        public CameraHelper _skyBoxCamFS;
        public CameraHelper _farCamFS;
        public CameraHelper _nearCamFS;        
        // Render Textures
        private RenderTexture _renderTexture;
        private RenderTexture _renderTextureFS;        
        private Texture2D _texture2D;
        private Texture2D _texture2DFullSze;
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
            set
            {
                float z = Mathf.Tan(value / Mathf.Rad2Deg) / Mathf.Tan(Mathf.Deg2Rad * CameraHelper.DEFAULT_FOV);
                _zoomLevel = -Mathf.Log10(z);
                _nearCam.fov = value;
                _farCam.fov = value;
                _skyBoxCam.fov = value;

                _nearCamFS.fov = value;
                _farCamFS.fov = value;
                _skyBoxCamFS.fov = value; 
            }
        }

        private bool _enabled = false;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _skyBoxCam.enabled = value;
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
            this.Log_Debug("Setting up cameras");
            bool cams = Utilities.dumpCameras();            
            _skyBoxCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera ScaledSpace"), _renderTexture, 17, false);
            _farCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 01"), _renderTexture, 18, true);
            _nearCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 00"), _renderTexture, 19, true);            
            _skyBoxCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera ScaledSpace"), _renderTextureFS, 21, false);
            _farCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 01"), _renderTextureFS, 22, true);
            _nearCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 00"), _renderTextureFS, 23, true);
            setupRenderTexture();            
            _skyBoxCam.reset();
            _farCam.reset();
            _nearCam.reset();            
            _skyBoxCamFS.reset();
            _farCamFS.reset();
            _nearCamFS.reset();
            this.Log_Debug("_skyBoxCam CullingMask =" + _skyBoxCam.camera.cullingMask + ",camera.nearClipPlane" + _skyBoxCam.camera.nearClipPlane + ",camera.farClipPlane" + _skyBoxCam.camera.farClipPlane);
            this.Log_Debug("_farCam CullingMask =" + _farCam.camera.cullingMask + ",camera.farClipPlane" + _farCam.camera.farClipPlane);
            this.Log_Debug("_nearCam CullingMask =" + _nearCam.camera.cullingMask + ",camera.farClipPlane" + _nearCam.camera.farClipPlane);
            this.Log_Debug("Camera setup complete");    
            
        }
        
        internal void LateUpdate()
        {
            if (_enabled)
            {
                _skyBoxCam.reset();
                _farCam.reset();
                _nearCam.reset();

                _skyBoxCamFS.reset();                               
                _farCamFS.reset();                
                _nearCamFS.reset();
                draw();
            }
        }

        private void updateZoom()
        {
            float z = Mathf.Pow(10, -_zoomLevel);
            float fov = Mathf.Rad2Deg * Mathf.Atan(z * Mathf.Tan(Mathf.Deg2Rad * CameraHelper.DEFAULT_FOV));
            _skyBoxCam.fov = fov;
            _farCam.fov = fov;
            _nearCam.fov = fov;

            _skyBoxCamFS.fov = fov;
            _farCamFS.fov = fov;
            _nearCamFS.fov = fov;    
        }

        internal void changeSize(int width, int height)
        {
            textureWidth = width;
            textureHeight = height;
            setupRenderTexture();
        }

        private void setupRenderTexture()
        {
            this.Log_Debug("Setting Up Render Texture");
            if(_renderTexture)
                _renderTexture.Release();            
            _renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTexture.Create();
            if (_renderTextureFS)
                _renderTextureFS.Release();  
            _renderTextureFS = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTextureFS.Create();
            _texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false, false);
            _texture2DFullSze = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, false);
            _skyBoxCam.renderTarget = _renderTexture;
            _farCam.renderTarget = _renderTexture;
            _nearCam.renderTarget = _renderTexture;
            _skyBoxCamFS.renderTarget = _renderTextureFS;
            _farCamFS.renderTarget = _renderTextureFS;
            _nearCamFS.renderTarget = _renderTextureFS;
            this.Log_Debug("Finish Setting Up Render Texture");
        }

        internal Texture2D draw()
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            
            this.Log_Debug("about to draw: vessel.position " + FlightGlobals.ActiveVessel.GetTransform().position + ",vessel.worldpos3d " + FlightGlobals.ActiveVessel.GetWorldPos3D());            
            this.Log_Debug("skyBoxCam.position " + _skyBoxCam.position + ",farcamposition=" + _farCam.position + ",nearcamposition=" + _nearCam.position);
            //set camera clearflags to the skybox and clear/render the skybox only
            _skyBoxCam.camera.clearFlags = CameraClearFlags.Skybox;            
            _skyBoxCam.camera.Render();
            //turn off the skybox renderers - XP, XN, YP, YN, ZP, ZN which are used to draw the KSP skybox. We don't want to see them in the camera
            foreach (Renderer r in skyboxRenderers)
                r.enabled = false;
            // KSP/Scaled Space/Planet Fader turn on the renderers for the planet faders
            foreach (ScaledSpaceFader s in scaledSpaceFaders) 
                s.r.enabled = true;            
            _skyBoxCam.camera.clearFlags = CameraClearFlags.Depth; //clear only the depth buffer
            _skyBoxCam.camera.farClipPlane = 3e15f; //set clipping plane distance            
            _skyBoxCam.camera.Render(); // render the skyboxcam
            foreach (Renderer r in skyboxRenderers) // turn the skybox renderers back on
                r.enabled = true;
            _farCam.camera.Render(); // render camera 01
            _nearCam.camera.Render(); // render camera 00
            _texture2D.ReadPixels(new Rect(0, 0, textureWidth, textureHeight), 0, 0); // read the camera pixels into the texture2D
            _texture2D.Apply();            
            RenderTexture.active = activeRT;
            return _texture2D;
        }
                
        internal Texture2D drawFS() // Same as Draw() but for fullscreencameras
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = _renderTextureFS;
            _skyBoxCamFS.reset();              
            _farCamFS.reset();            
            _nearCamFS.reset();
            _skyBoxCamFS.camera.clearFlags = CameraClearFlags.Skybox;
            _skyBoxCamFS.camera.Render();
            foreach (Renderer r in skyboxRenderers)
                r.enabled = false;
            foreach (ScaledSpaceFader s in scaledSpaceFaders)
                s.r.enabled = true;
            _skyBoxCamFS.camera.clearFlags = CameraClearFlags.Depth;
            _skyBoxCamFS.camera.farClipPlane = 3e15f;
            _skyBoxCamFS.camera.Render();
            foreach (Renderer r in skyboxRenderers)
                r.enabled = true;
            _farCamFS.camera.Render();
            _nearCamFS.camera.Render();            
            _texture2DFullSze.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
            _texture2DFullSze.Apply();
            RenderTexture.active = activeRT;
            return _texture2DFullSze;
        }
             
        public void saveToFile(string fileName, string devtype) // Save Image to filesystem
        {
            string lgefileName = fileName + "Large.png";
            fileName += ".png";
            byte[] data = _texture2D.EncodeToPNG();
            drawFS();
            byte[] dataFS = _texture2DFullSze.EncodeToPNG();

            if (devtype == "ChemCam")
            {
                using (KSP.IO.FileStream file = KSP.IO.File.Open<TSTChemCam>(fileName, KSP.IO.FileMode.Create, null))
                {
                    file.Write(data, 0, data.Length);
                }                
                using (KSP.IO.FileStream file = KSP.IO.File.Open<TSTChemCam>(lgefileName, KSP.IO.FileMode.Create, null))
                {
                    file.Write(dataFS, 0, dataFS.Length);
                }   
            }
            if (devtype == "TeleScope")
            {
                using (KSP.IO.FileStream file = KSP.IO.File.Open<TSTSpaceTelescope>(fileName, KSP.IO.FileMode.Create, null))
                {
                    file.Write(data, 0, data.Length);
                }                
                using (KSP.IO.FileStream file = KSP.IO.File.Open<TSTSpaceTelescope>(lgefileName, KSP.IO.FileMode.Create, null))
                {
                    file.Write(dataFS, 0, dataFS.Length);
                }   
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

        public Vector3d position
        {
            get { return _go.transform.position; }
            set { _go.transform.position = position;  }
        }
              

        public void reset()
        {
            //this.Log_Debug("Resetting camera " + _camera.name);
            _camera.CopyFrom(_copyFrom);
            _camera.targetTexture = _renderTarget;
            if (_attachToParent)
            {
                _go.transform.parent = _parent.transform;
                _go.transform.localPosition = Vector3.zero;
                _go.transform.localEulerAngles = Vector3.zero;
                //this.Log_Debug("Attached to parent, pos=" + _go.transform.parent.transform.position.ToString("############.#################"));
            }
            else
            {
                _go.transform.rotation = _parent.transform.rotation;
                //this.Log_Debug("NOT Attached to parent pos=" + _go.transform.position + ",rotation=" + _go.transform.rotation);
            }
            _camera.rect = new Rect(0, 0, 1, 1);
            _camera.depth = _depth;
            _camera.fieldOfView = _fov;
            _camera.enabled = enabled;            
        }
    }
}
