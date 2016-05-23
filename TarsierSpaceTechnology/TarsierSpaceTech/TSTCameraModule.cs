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
using KSP.IO;
using RSTUtils;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TSTCameraModule : MonoBehaviour
    {
        private int textureWidth = 256; 
        private int textureHeight = 256;
        // Standard Cameras  
        public CameraHelper _galaxyCam;
        public CameraHelper _scaledSpaceCam;
        public CameraHelper _farCam;
        public CameraHelper _nearCam;
        // FullScreen Cameras  
        public CameraHelper _galaxyCamFS;
        public CameraHelper _scaledSpaceCamFS;
        public CameraHelper _farCamFS;
        public CameraHelper _nearCamFS;        
        // Render Textures
        private RenderTexture _renderTexture;
        private RenderTexture _renderTextureFS;        
        private Texture2D _texture2D;
        private Texture2D _texture2DFullSze;
        private Renderer[] skyboxRenderers;
        private ScaledSpaceFader[] scaledSpaceFaders;
        private AtmosphereFromGround[] atmospheres;
        //camera rendering info cache
        Dictionary<AtmosphereFromGround, Vector4> atmoInfo = new Dictionary<AtmosphereFromGround, Vector4>();
        //TempVars
        private float exposure;
        private Color origColor;
        private Renderer skyboxRenderer;
        private float tmpZoom;
        private float tmpfov;
        private bool zoomSkyBox = true;
        private float TanRadDfltFOV;

        //Const - but we don't use constants for Garbage collector
        private double KPtoAtms = 0.009869232;
        public float SkyboxExposure = 1;

        public Texture2D Texture2D
        {
            get { return _texture2D; }
        }

        private float _zoomLevel;
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
                _scaledSpaceCam.fov = value;
                _galaxyCam.fov = value;

                _nearCamFS.fov = value;
                _farCamFS.fov = value;
                _scaledSpaceCamFS.fov = value;
                _galaxyCamFS.fov = value;
            }
        }

        private bool _enabled;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                _galaxyCam.enabled = value;
                _scaledSpaceCam.enabled = value;
                _farCam.enabled = value;
                _nearCam.enabled = value;                
                skyboxRenderers = (from Renderer r in (FindObjectsOfType(typeof(Renderer)) as IEnumerable<Renderer>) where (r.name == "XP" || r.name == "XN" || r.name == "YP" || r.name == "YN" || r.name == "ZP" || r.name == "ZN") select r).ToArray();
                scaledSpaceFaders = FindObjectsOfType<ScaledSpaceFader>();
                atmospheres = FindObjectsOfType<AtmosphereFromGround>();
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
            Utilities.Log_Debug("{0}:Setting up cameras" , GetType().Name);
            //Utilities.DumpCameras();
            _galaxyCam = new CameraHelper(gameObject, Utilities.findCameraByName("GalaxyCamera"), _renderTexture, 17, false);
            _scaledSpaceCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera ScaledSpace"), _renderTexture, 18, false);
            _farCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 01"), _renderTexture, 19, true);
            _nearCam = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 00"), _renderTexture, 20, true);
            _galaxyCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("GalaxyCamera"), _renderTextureFS, 21, false);
            _scaledSpaceCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera ScaledSpace"), _renderTextureFS, 22, false);
            _farCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 01"), _renderTextureFS, 23, true);
            _nearCamFS = new CameraHelper(gameObject, Utilities.findCameraByName("Camera 00"), _renderTextureFS, 24, true);
            setupRenderTexture();
            _galaxyCam.reset();
            _scaledSpaceCam.reset();
            _farCam.reset();
            _nearCam.reset();
            _galaxyCamFS.reset();
            _scaledSpaceCamFS.reset();
            _farCamFS.reset();
            _nearCamFS.reset();
            if (TSTMstStgs.Instance != null)
            {
                zoomSkyBox = TSTMstStgs.Instance.TSTsettings.ZoomSkyBox;
            }
            TanRadDfltFOV = Mathf.Tan(Mathf.Deg2Rad*CameraHelper.DEFAULT_FOV);
            
            Utilities.Log_Debug("{0}: skyBoxCam CullingMask = {1}, camera.nearClipPlane = {2}, camera.farClipPlane = {3}" , GetType().Name , _scaledSpaceCam.camera.cullingMask , _scaledSpaceCam.camera.nearClipPlane , _scaledSpaceCam.camera.farClipPlane);
            Utilities.Log_Debug("{0}: farCam CullingMask = {1}, camera.farClipPlane = {2}", GetType().Name, _farCam.camera.cullingMask , _farCam.camera.farClipPlane);
            Utilities.Log_Debug("{0}: nearCam CullingMask = {1}, camera.farClipPlane = {2}", GetType().Name, _nearCam.camera.cullingMask , _nearCam.camera.farClipPlane);
            Utilities.Log_Debug("{0}: Camera setup complete", GetType().Name);    
            
        }
        
        internal void LateUpdate()
        {
            if (_enabled) 
            {
                _galaxyCam.reset();
                _scaledSpaceCam.reset();
                _farCam.reset();
                _nearCam.reset();

                _galaxyCamFS.reset();
                _scaledSpaceCamFS.reset();                               
                _farCamFS.reset();                
                _nearCamFS.reset();
                draw();
            }
        }

        private void updateZoom()
        {
            
            if (zoomSkyBox)
            {
                tmpZoom = Mathf.Pow(10, -_zoomLevel / 10);
                tmpfov = Mathf.Rad2Deg * Mathf.Atan(tmpZoom * TanRadDfltFOV);
                _galaxyCam.fov = tmpfov;
                _galaxyCamFS.fov = tmpfov;
            }
                
            tmpZoom = Mathf.Pow(10, -_zoomLevel);
            tmpfov = Mathf.Rad2Deg * Mathf.Atan(tmpZoom * TanRadDfltFOV);
            _scaledSpaceCam.fov = tmpfov;
            _farCam.fov = tmpfov;
            _nearCam.fov = tmpfov;
            
            _scaledSpaceCamFS.fov = tmpfov;
            _farCamFS.fov = tmpfov;
            _nearCamFS.fov = tmpfov;    
        }

        internal void changeSize(int width, int height)
        {
            textureWidth = width;
            textureHeight = height;
            setupRenderTexture();
        }

        private void setupRenderTexture()
        {
            Utilities.Log_Debug("{0}:Setting Up Render Texture", GetType().Name);
            if (_renderTexture)
            {
                _galaxyCam.renderTarget.Release();
                _scaledSpaceCam.renderTarget.Release();
                _farCam.renderTarget.Release();
                _nearCam.renderTarget.Release();
                _renderTexture.Release();
            }
                            
            _renderTexture = new RenderTexture(textureWidth, textureHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTexture.Create();
            if (_renderTextureFS)
            {
                _galaxyCamFS.renderTarget.Release();
                _scaledSpaceCamFS.renderTarget.Release();
                _farCamFS.renderTarget.Release();
                _nearCamFS.renderTarget.Release();
                _renderTextureFS.Release();
            }
            _renderTextureFS = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            _renderTextureFS.antiAliasing = GameSettings.ANTI_ALIASING;
            _renderTextureFS.Create();
            _texture2D = new Texture2D(textureWidth, textureHeight, TextureFormat.RGB24, false, false);
            _texture2DFullSze = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false, false);
            _galaxyCam.renderTarget = _renderTexture;
            _scaledSpaceCam.renderTarget = _renderTexture;
            _farCam.renderTarget = _renderTexture;
            _nearCam.renderTarget = _renderTexture;
            _galaxyCamFS.renderTarget = _renderTextureFS;
            _scaledSpaceCamFS.renderTarget = _renderTextureFS;
            _farCamFS.renderTarget = _renderTextureFS;
            _nearCamFS.renderTarget = _renderTextureFS;
            Utilities.Log_Debug("{0}:Finish Setting Up Render Texture", GetType().Name);
        }

        internal Texture2D draw()
        {
            RenderTexture activeRT = RenderTexture.active;
            RenderTexture.active = _renderTexture;
            
            //Render the Skybox
            //Calculate exposure setting for skybox
            exposure = CalculateExposure(ScaledSpace.ScaledToLocalSpace(_scaledSpaceCam.position));
            origColor = skyboxRenderers[0].sharedMaterial.GetColor(PropertyIDs._Color);
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                skyboxRenderer = skyboxRenderers[i];
                Color color = Color.Lerp(GalaxyCubeControl.Instance.minGalaxyColor, GalaxyCubeControl.Instance.maxGalaxyColor, exposure);
                skyboxRenderer.material.SetColor(PropertyIDs._Color, color);
            }

            _galaxyCam.camera.Render();
            
            //set exposure back to what it was
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                var sr = skyboxRenderers[i];
                sr.material.SetColor(PropertyIDs._Color, origColor);
            }

            //Render ScaledSpace
            //Render Atmospheres
            foreach (var afg in atmospheres)
            {
                //cache the atmosphere info
                if (!atmoInfo.ContainsKey(afg))
                    atmoInfo.Add(afg, new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight));
                else
                    atmoInfo[afg] = new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight);

                //set the atmosphere's camera to the scaled camera
                afg.cameraPos = _scaledSpaceCam.position;
                afg.cameraHeight = (float)_scaledSpaceCam.position.magnitude;
                afg.cameraHeight2 = afg.cameraHeight * afg.cameraHeight;
                afg.SetMaterial(false);
            }

            // KSP/Scaled Space/Planet Fader turn on the renderers for the planet faders
            foreach (ScaledSpaceFader s in scaledSpaceFaders) 
                s.r.enabled = true;            
            _scaledSpaceCam.camera.clearFlags = CameraClearFlags.Depth; //clear only the depth buffer
            _scaledSpaceCam.camera.farClipPlane = 3e15f; //set clipping plane distance            
            _scaledSpaceCam.camera.Render(); // render the ScaledSpaceCam

            //reset atmoInfo
            foreach (var afg in atmospheres)
            {
                //reset the atmosphere info from the cache
                if (atmoInfo.ContainsKey(afg))
                {
                    var info = atmoInfo[afg];
                    afg.cameraPos = new Vector3(info.x, info.y, info.z);
                    afg.cameraHeight = info.z;
                    afg.cameraHeight2 = info.z * info.z;
                    afg.SetMaterial(false);
                }
            }

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
            _galaxyCamFS.reset();
            _scaledSpaceCamFS.reset();              
            _farCamFS.reset();            
            _nearCamFS.reset();


            //Render the Skybox
            //Calculate exposure setting for skybox
            exposure = CalculateExposure(ScaledSpace.ScaledToLocalSpace(_scaledSpaceCam.position));
            origColor = skyboxRenderers[0].sharedMaterial.GetColor(PropertyIDs._Color);
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                skyboxRenderer = skyboxRenderers[i];
                Color color = Color.Lerp(GalaxyCubeControl.Instance.minGalaxyColor, GalaxyCubeControl.Instance.maxGalaxyColor, exposure);
                skyboxRenderer.material.SetColor(PropertyIDs._Color, color);
            }

            _galaxyCamFS.camera.Render();

            //set exposure back to what it was
            for (int i = 0; i < skyboxRenderers.Length; i++)
            {
                var sr = skyboxRenderers[i];
                sr.material.SetColor(PropertyIDs._Color, origColor);
            }

            //Render ScaledSpace
            //Render Atmospheres
            foreach (var afg in atmospheres)
            {
                //cache the atmosphere info
                if (!atmoInfo.ContainsKey(afg))
                    atmoInfo.Add(afg, new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight));
                else
                    atmoInfo[afg] = new Vector4(afg.cameraPos.x, afg.cameraPos.y, afg.cameraPos.z, afg.cameraHeight);

                //set the atmosphere's camera to the scaled camera
                afg.cameraPos = _scaledSpaceCam.position;
                afg.cameraHeight = (float)_scaledSpaceCam.position.magnitude;
                afg.cameraHeight2 = afg.cameraHeight * afg.cameraHeight;
                afg.SetMaterial(false);
            }

            // KSP/Scaled Space/Planet Fader turn on the renderers for the planet faders
            foreach (ScaledSpaceFader s in scaledSpaceFaders)
                s.r.enabled = true;
            _scaledSpaceCamFS.camera.clearFlags = CameraClearFlags.Depth; //clear only the depth buffer
            _scaledSpaceCamFS.camera.farClipPlane = 3e15f; //set clipping plane distance            
            _scaledSpaceCamFS.camera.Render(); // render the ScaledSpaceCam

            //reset atmoInfo
            foreach (var afg in atmospheres)
            {
                //reset the atmosphere info from the cache
                if (atmoInfo.ContainsKey(afg))
                {
                    var info = atmoInfo[afg];
                    afg.cameraPos = new Vector3(info.x, info.y, info.z);
                    afg.cameraHeight = info.z;
                    afg.cameraHeight2 = info.z * info.z;
                    afg.SetMaterial(false);
                }
            }

            _farCamFS.camera.Render(); // render camera 01
            _nearCamFS.camera.Render(); // render camera 00



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
                using (FileStream file = File.Open<TSTChemCam>(fileName, FileMode.Create))
                {
                    file.Write(data, 0, data.Length);
                }                
                using (FileStream file = File.Open<TSTChemCam>(lgefileName, FileMode.Create))
                {
                    file.Write(dataFS, 0, dataFS.Length);
                }   
            }
            if (devtype == "TeleScope")
            {
                using (FileStream file = File.Open<TSTSpaceTelescope>(fileName, FileMode.Create))
                {
                    file.Write(data, 0, data.Length);
                }                
                using (FileStream file = File.Open<TSTSpaceTelescope>(lgefileName, FileMode.Create))
                {
                    file.Write(dataFS, 0, dataFS.Length);
                }   
            }                                         
        }

        private float CalculateExposure(Vector3d cameraWorldPos)
        {
            float pressure = (float)(FlightGlobals.getStaticPressure(cameraWorldPos) * KPtoAtms);
            return Mathf.Lerp(SkyboxExposure, 0f, pressure);
        }
    }

    internal class CameraHelper
    {
        public CameraHelper(GameObject parent, Camera copyFrom, RenderTexture renderTarget, float depth, bool attachToParent)
        {
            _copyFrom = copyFrom;
            _renderTarget = renderTarget;
            _parent = parent;
            _go = new GameObject();
            _camera = _go.AddComponent<Camera>();
            _depth = depth;
            _attachToParent = attachToParent;
            _camera.enabled = false;
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

        private float _fov = DEFAULT_FOV;
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
            _camera.CopyFrom(_copyFrom);
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
            _camera.targetTexture = _renderTarget;
            _camera.enabled = enabled;
        }
    }
}
