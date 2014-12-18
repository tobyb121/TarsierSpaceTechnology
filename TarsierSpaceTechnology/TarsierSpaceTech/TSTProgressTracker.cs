﻿using System;
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
            Utils.print("Starting Tarsier Progress Tracking");
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
            Utils.print("Loading Tarsier Progress Tracker");
            ConfigNode telescopeNode = node.GetNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.GetNode("TarsierChemCam");

            Utils.print("Getting Telescope Celestial Body Status");
            foreach (CelestialBody b in FlightGlobals.Bodies)
                TelescopeData[b.name] = telescopeNode != null ? (telescopeNode.GetValue(b.name) == "true") : false;

            Utils.print("Getting Telescope Galaxy Status");
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
                Utils.print("Getting Telescope Galaxy Failed unexpectedly. Ex: " + ex.Message);
            }

            Utils.print("Getting ChemCam Celestial Body Status");
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
            Utils.print("Saving TST Progress data");
            ConfigNode telescopeNode = node.AddNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.AddNode("TarsierChemCam");
            foreach (string key in TelescopeData.Keys)
                telescopeNode.AddValue(key, TelescopeData[key]?"true":"false");
            foreach (string key in ChemCamData.Keys)
                chemCamNode.AddValue(key, ChemCamData[key]?"true":"false");
        }
    }
}
