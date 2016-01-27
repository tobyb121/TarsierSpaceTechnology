/*
 * TSTGalaxies.cs
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
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]    
    public class TSTGalaxies : MonoBehaviour
    {
        public static TSTGalaxies Instance;

        internal GameObject baseTransform;
        private List<TSTGalaxy> _galaxies = new List<TSTGalaxy>();
        private List<CelestialBody> _CBgalaxies = new List<CelestialBody>();

        public static List<TSTGalaxy> Galaxies
        {
            get
            {
                return Instance._galaxies;
            }
        }

        public static List<CelestialBody> CBGalaxies
        {
            get
            {
                return Instance._CBgalaxies;
            }
        }
        
        public void Awake()
        {
            Debug.Log("TSTGalaxies Galaxies Awake");
            if (Instance != null)
                Destroy(this);
            else
                Instance = this;                        
            DontDestroyOnLoad(this);

            Debug.Log("TSTGalaxies Starting Galaxies");

            baseTransform = new GameObject();
            baseTransform.transform.localPosition = Vector3.zero;
            baseTransform.transform.localRotation = Quaternion.identity;
            
            if (ScaledSun.Instance != null)
            {
                baseTransform.transform.parent = ScaledSun.Instance.transform;
                Debug.Log("TSTGalaxies BaseTransform set to the ScaledSun.Instance");
            }
            else
            {
                baseTransform.SetActive(false);
                Debug.Log("TSTGalaxies BaseTransform setactive = false, ScaledSun does not exist");
            }
            if (TSTInstalledMods.IsKopInstalled)
            {
                baseTransform.transform.parent = FlightGlobals.Bodies[1].transform;
                Debug.Log("TSTGalaxies - Detected Kopernicus - BaseTransform set to Home Planet");
            }
            
            UrlDir.UrlConfig[] galaxyCfgs = GameDatabase.Instance.GetConfigs("GALAXY");
            foreach (UrlDir.UrlConfig cfg in galaxyCfgs)
            {
                GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(TSTGalaxy));
                go.transform.parent = baseTransform.transform;                
                TSTGalaxy galaxy = go.GetComponent<TSTGalaxy>();
                galaxy.Load(cfg.config);
                Debug.Log("TSTGalaxies Adding Galaxy " + galaxy.name);
                Galaxies.Add(galaxy);
                
                GameObject goCB = new GameObject(name, typeof(CelestialBody));                
                goCB.transform.parent = go.transform;
                CelestialBody celestialBody = goCB.GetComponent<CelestialBody>();
                celestialBody.bodyName = galaxy.theName;
                celestialBody.transform.position = galaxy.position;
                celestialBody.atmosphere = false;
                celestialBody.ocean = false;
                celestialBody.orbitDriver = null;
                celestialBody.progressTree = null;                
                CBGalaxies.Add(celestialBody);
            }
        }                
    }
}
