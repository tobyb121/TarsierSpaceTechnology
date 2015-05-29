/*
 * TSTProgressTracker.cs
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

    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames|ScenarioCreationOptions.AddToNewCareerGames,GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    public class TSTProgressTracker : ScenarioModule
    {
        private static TSTProgressTracker Instance;

        public static bool isActive
        {
            get
            {
                return Instance != null;
            }
        }

        public void Start()
        {
            this.Log_Debug("Starting Tarsier Progress Tracking");
            Instance = this;
        }

        //int i = 0;

        public void Update()
        {
        }

        public delegate void TelescopeListener(TSTSpaceTelescope.TargetableObject body);
        public delegate void ChemCamListener(CelestialBody body,string biome);

        private List<TelescopeListener> TelescopeListeners = new List<TelescopeListener>();

        private List<ChemCamListener> ChemCamListeners = new List<ChemCamListener>();

        public static void AddTelescopeListener(TelescopeListener listener)
        {
            Instance.TelescopeListeners.Add(listener);
        }

        public static void RemoveTelescopeListener(TelescopeListener listener)
        {
            Instance.TelescopeListeners.Remove(listener);
        }

        public static void OnTelescopePicture(TSTSpaceTelescope.TargetableObject body)
        {
            if (isActive)
            {
                List<TelescopeListener> listeners = new List<TelescopeListener>(Instance.TelescopeListeners);
                foreach (TelescopeListener listener in listeners)
                {
                    listener(body);
                }
            }
        }

        public static void OnChemCamFire(CelestialBody body,string biome)
        {
            if (isActive)
            {
                List<ChemCamListener> listeners = new List<ChemCamListener>(Instance.ChemCamListeners);
                foreach (ChemCamListener listener in listeners)
                {
                    listener(body,biome);
                }
            }
        }

        public static void setTelescopeContractComplete(TSTSpaceTelescope.TargetableObject body)
        {
            Instance.TelescopeData[body.name] = true;
        }

        public static void setChemCamContractComplete(CelestialBody body)
        {
            Instance.ChemCamData[body.name] = true;
        }

        public static string GetNextTelescopeTarget()
        {
            string target = TarsierPlanetOrder.FirstOrDefault(s=>!Instance.TelescopeData[s]);

            if (target == default(string))
                target = TarsierPlanetOrder[UnityEngine.Random.Range((int)0, TarsierPlanetOrder.Length)];

            return target;
        }

        public static bool HasTelescopeCompleted(TSTSpaceTelescope.TargetableObject body)
        {
            return isActive ? Instance.TelescopeData[body.name] : true;
        }

        private static string[] TarsierPlanetOrder = new string[] {
            "Sun",
            "Kerbin",
            "Mun",
            "Minmus",
            "Duna",
            "Eve",
            "Moho",
            "Jool",
            "Dres",
            "Eeloo",
            "Ike",
            "Laythe",
            "Gilly",
            "Tylo",
            "Vall",
            "Bop",
            "Pol",
            "Galaxy1",
            "Galaxy2",
            "Galaxy3",
            "Galaxy4",
            "Galaxy5",
            "Galaxy6",
            "Galaxy7",
            "Galaxy8",
        };

        public static Contracts.Contract.ContractPrestige getTelescopePrestige(string bodyName)
        {
            int i=Array.IndexOf(TarsierPlanetOrder, bodyName);
            if (i < 4)
                return Contracts.Contract.ContractPrestige.Trivial;
            else if (i < 7)
                return Contracts.Contract.ContractPrestige.Significant;
            else
                return Contracts.Contract.ContractPrestige.Exceptional;
        }

        public static Contracts.Contract.ContractPrestige getChemCamPrestige(CelestialBody body)
        {
            int i = Array.IndexOf(TarsierPlanetOrder, body.name);
            if (i < 4)
                return Contracts.Contract.ContractPrestige.Trivial;
            else if (i < 7)
                return Contracts.Contract.ContractPrestige.Significant;
            else
                return Contracts.Contract.ContractPrestige.Exceptional;
        }

        public Dictionary<string, bool> TelescopeData = new Dictionary<string, bool>();
        public Dictionary<string, bool> ChemCamData = new Dictionary<string, bool>();

        public override void OnLoad(ConfigNode node)
        {
            this.Log_Debug("Loading Tarsier Progress Tracker");
            ConfigNode telescopeNode = node.GetNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.GetNode("TarsierChemCam");

            this.Log_Debug("Getting Telescope Celestial Body Status");
            foreach (CelestialBody b in FlightGlobals.Bodies)
                TelescopeData[b.name] = telescopeNode != null ? (telescopeNode.GetValue(b.name) == "true") : false;

            this.Log_Debug("Getting Telescope Galaxy Status");
            try {
                foreach (TSTGalaxy g in TSTGalaxies.Galaxies)
                {
                    //Added null check as it was throwing errors in my career games and maybe causing issues with other scenario modules
                    if (telescopeNode != null && telescopeNode.HasValue(g.name))
                        TelescopeData[g.name] = telescopeNode != null ? (telescopeNode.GetValue(g.name) == "true") : false;
                    else
                        TelescopeData[g.name] = false;
                }
            } catch (Exception ex) {
                this.Log_Debug("Getting Telescope Galaxy Failed unexpectedly. Ex: " + ex.Message);
            }

            this.Log_Debug("Getting ChemCam Celestial Body Status");
            if (chemCamNode != null)
            {
                foreach (CelestialBody b in FlightGlobals.Bodies)
                    ChemCamData[b.name] = (chemCamNode.GetValue(b.name) == "true");
            }
            else
            {
                foreach (CelestialBody b in FlightGlobals.Bodies)
                    ChemCamData[b.name] = false;
            }
        }

        public override void OnSave(ConfigNode node)
        {
            this.Log_Debug("Saving TST Progress data");
            ConfigNode telescopeNode = node.AddNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.AddNode("TarsierChemCam");
            foreach (string key in TelescopeData.Keys)
                telescopeNode.AddValue(key, TelescopeData[key]?"true":"false");
            foreach (string key in ChemCamData.Keys)
                chemCamNode.AddValue(key, ChemCamData[key]?"true":"false");
        }
    }
}
