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
        
        public CameraHelper _skyBoxCam;
        public CameraHelper _farCam;
        public CameraHelper _nearCam;

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
            set
            {
                float z = Mathf.Tan(value / Mathf.Rad2Deg) / Mathf.Tan(Mathf.Deg2Rad * CameraHelper.DEFAULT_FOV);
                _zoomLevel = -Mathf.Log10(z);
                _nearCam.fov = value;
                _farCam.fov = value;
                _skyBoxCam.fov = value;
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
            _skyBoxCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera ScaledSpace"), _renderTexture, 15, false);
            _farCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 01"), _renderTexture, 16, true);
            _nearCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 00"), _renderTexture, 17, true);
            setupRenderTexture();
            _skyBoxCam.reset();
            _farCam.reset();
            _nearCam.reset();
            this.Log_Debug("Camera setup complete");                 
        }

        public void Update()
        {
            if (_enabled)
            {
                _skyBoxCam.reset();
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
        }

        public void changeSize(int width, int height)
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
            _texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false, false);
            _skyBoxCam.renderTarget = _renderTexture;
            _farCam.renderTarget = _renderTexture;
            _nearCam.renderTarget = _renderTexture;
            this.Log_Debug("Finish Setting Up Render Texture");
        }

        public Texture2D draw()
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;

            _skyBoxCam.camera.clearFlags = CameraClearFlags.Skybox;
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
            using (KSP.IO.FileStream file = KSP.IO.File.Open<TSTSpaceTelescope>(fileName, KSP.IO.FileMode.Create,null))
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
