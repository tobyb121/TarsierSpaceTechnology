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
using Contracts;
using KSP.Localization;
using UniLinq;
using RSTUtils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace TarsierSpaceTech
{
    [KSPScenario(ScenarioCreationOptions.AddToExistingCareerGames | ScenarioCreationOptions.AddToNewCareerGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.EDITOR, GameScenes.TRACKSTATION)]
    public class TSTProgressTracker : ScenarioModule
    {
        private static TSTProgressTracker Instance;

        private static bool isRSSactive;
        private static bool isOPMactive;
        private static bool isNHactive;
        private static bool isRBactive;

        //ResearchBodies Mod vars
        private static int RBwindowID = 5955558;
        private Rect RBWindowPos = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 200, 0, 0);
        private bool showRBWindow;
        private string RBPopupMsg = string.Empty;
        private int ScienceReward = 20; // This should actually be coming from the Telescope Part attached ModuleTrackBodies PartModule field. - One day when I have time to make the code changes.
        

        //private List<CelestialBody> BodyList = new List<CelestialBody>();
        private List<string> bodyNames = new List<string>(); 


        public static bool isActive
        {
            get
            {
                return Instance != null;
            }
        }

        public void Start()
        {
            Utilities.Log_Debug("Starting Tarsier Progress Tracking");
            Instance = this;
            isRSSactive = Utilities.IsRSSInstalled;
            isOPMactive = Utilities.IsOPMInstalled;
            isNHactive = Utilities.IsNHInstalled;
            isRBactive = Utilities.IsResearchBodiesInstalled;
            bodyNames = FlightGlobals.Bodies.Where(p => p.Radius > 100).Select(p => p.name).ToList();  //List of CBs where radius > 100m (exclude Sigma Binaries)

            if (isRBactive)
            {
                try
                {
                    if (!RBWrapper.APIRBReady)
                        RBWrapper.InitRBWrapper();
                    RBwindowID = Utilities.getnextrandomInt();

                    if (RBWrapper.APIRBReady)
                        GameEvents.OnScienceRecieved.Add(processScience);
                    else
                    {
                        Utilities.Log("Initialise of ResearchBodies interface failed unexpectedly. Interface disabled.");
                        isRBactive = false;
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log("Initialise of ResearchBodies interface failed unexpectedly. Interface disabled. Ex: {0}" , ex.Message);
                    isRBactive = false;
                }
                
            }            
        }               

        public void OnDestroy()
        {
            if (isRBactive)
            {
                GameEvents.OnScienceRecieved.Remove(processScience);
            }
        }

        public delegate void TelescopeListener(TSTSpaceTelescope.TargetableObject body);

        public delegate void ChemCamListener(CelestialBody body, string biome);

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

        public static void OnChemCamFire(CelestialBody body, string biome)
        {
            if (isActive)
            {
                List<ChemCamListener> listeners = new List<ChemCamListener>(Instance.ChemCamListeners);
                foreach (ChemCamListener listener in listeners)
                {
                    listener(body, biome);
                }
            }
        }

        public static void setTelescopeContractComplete(TSTSpaceTelescope.TargetableObject body)
        {
            Instance.TelescopeData[body.name] = true;
        }

        public static void setChemCamContractComplete(string targetname)
        {
            Instance.ChemCamData[targetname] = true;
        }

        public static string GetNextTelescopeTarget()
        {
            string target = default(string);
            if (isRSSactive) //If Real Solar System is installed
            {
                target = TSTMstStgs.Instance.TSTrssplanets.RSSPlanetOrder.FirstOrDefault(s => !Instance.TelescopeData[s]);
                if (target == default(string))
                    target = TSTMstStgs.Instance.TSTrssplanets.RSSPlanetOrder[Random.Range(0, TSTMstStgs.Instance.TSTrssplanets.RSSPlanetOrder.Length)];
            }
            else
            {
                if (isOPMactive) // If Outer Planets Mod is installed
                {
                    target = TSTMstStgs.Instance.TSTopmplanets.OPMPlanetOrder.FirstOrDefault(s => !Instance.TelescopeData[s]);
                    if (target == default(string))
                        target = TSTMstStgs.Instance.TSTopmplanets.OPMPlanetOrder[Random.Range(0, TSTMstStgs.Instance.TSTopmplanets.OPMPlanetOrder.Length)];
                }
                else
                {
                    if (isNHactive) // If New Horizons Planets Mod is installed
                    {
                        target = TSTMstStgs.Instance.TSTnhplanets.NHPlanetOrder.FirstOrDefault(s => !Instance.TelescopeData[s]);
                        if (target == default(string))
                            target = TSTMstStgs.Instance.TSTnhplanets.NHPlanetOrder[Random.Range(0, TSTMstStgs.Instance.TSTnhplanets.NHPlanetOrder.Length)];
                    }
                    else
                    {
                        if (Utilities.IsKopInstalled)  // If Kopernicus is installed, but not RSS or OPM use a list of the planets in order
                        {

                            target = Instance.bodyNames.FirstOrDefault(s => !Instance.TelescopeData[s]);
                            if (target == default(string))
                                target = Instance.bodyNames[Random.Range(0, Instance.bodyNames.Count)];
                        }
                        else  //Default Stock
                        {
                            target = TSTMstStgs.Instance.TSTstockplanets.StockPlanetOrder.FirstOrDefault(s => !Instance.TelescopeData[s]);
                            if (target == default(string))
                                target = TSTMstStgs.Instance.TSTstockplanets.StockPlanetOrder[Random.Range(0, TSTMstStgs.Instance.TSTstockplanets.StockPlanetOrder.Length)];
                        }
                    }
                }
            }
            // if ResearchBodies is installed we need to check if the target body has been found. If it has not, then we set the target to default so a contract is not generated at this time.
            if (isRBactive && RBWrapper.RBactualAPI.enabled && target != default(string))
            {
                try
                {
                    if (RBWrapper.APIRBReady)
                    {
                        //if (RBWrapper.RBSCactualAPI.enabled)
                        //{
                        List<KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo>> trackbodyentry = TSTMstStgs.Instance.RBCelestialBodies.Where(e => e.Key.name == target).ToList();
                        if (trackbodyentry.Count != 1)
                        {
                            Utilities.Log("Unable to set target {0} at this time as it is not a tracked body", target);
                            target = default(string);
                            return target;
                        }
                        if (trackbodyentry[0].Value.isResearched == false)
                        {
                            Utilities.Log("Unable to set target {0} at this time as it is not discovered", target);
                            target = default(string);
                            return target;
                        }
                    }
                    
                    else
                    {
                        Utilities.Log("ResearchBodies is not Ready, cannot check Telescope target for contract generation at this time.");
                        target = default(string);
                        return target;
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log("Checking ResearchBodies status for target {0}. Failed unexpectedly. Ex: {1}" , target ,  ex.Message);
                }
            }

            return target;
        }

        public static bool HasTelescopeCompleted(TSTSpaceTelescope.TargetableObject body)
        {
            return !isActive || Instance.TelescopeData[body.name];
        }

        public static bool HasChemCamCompleted(string entry)
        {
            return !isActive || Instance.ChemCamData[entry];
        }

        public static Contract.ContractPrestige getTelescopePrestige(TSTSpaceTelescope.TargetableObject body)
        {
            double distance = 0f;
            if (body.type == typeof(TSTGalaxy))
            {
                Vector3d bodyPos = body.position;
                CelestialBody HmePlanet = Planetarium.fetch.Home;
                Vector3d hmeplntPos = HmePlanet.getPositionAtUT(0);
                distance = Math.Sqrt(Math.Pow(bodyPos.x - hmeplntPos.x, 2) + Math.Pow(bodyPos.y - hmeplntPos.y, 2) + Math.Pow(bodyPos.z - hmeplntPos.z, 2));
            }
            else
            {
                distance = Utilities.DistanceFromHomeWorld(body.name);
            }
            if (distance < 13000000000)
            {
                return Contract.ContractPrestige.Trivial;
            }
            if (distance < 20000000000)
            {
                return Contract.ContractPrestige.Significant;
            }
            return Contract.ContractPrestige.Exceptional;
        }

        public static Contract.ContractPrestige getChemCamPrestige(CelestialBody body)
        {
            double distance = Utilities.DistanceFromHomeWorld(body.name);
            if (distance < 13000000000)
            {
                return Contract.ContractPrestige.Trivial;
            }
            if (distance < 20000000000)
            {
                return Contract.ContractPrestige.Significant;
            }
            return Contract.ContractPrestige.Exceptional;
        }

        public Dictionary<string, bool> TelescopeData = new Dictionary<string, bool>();
        public Dictionary<string, bool> ChemCamData = new Dictionary<string, bool>();

        public override void OnLoad(ConfigNode node)
        {
            Utilities.Log_Debug("Loading Tarsier Progress Tracker");
            ConfigNode telescopeNode = node.GetNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.GetNode("TarsierChemCam");

            try
            {
                if (telescopeNode != null)
                {
                    Utilities.Log_Debug("Getting Telescope Celestial Body Status");
                    foreach (CelestialBody b in FlightGlobals.Bodies.Where(p => p.Radius > 100))
                    {
                        if (telescopeNode.HasValue(b.name))
                            TelescopeData[b.name] = telescopeNode.GetValue(b.name) == "true";
                        else
                        {
                            TelescopeData[b.name] = false;
                        }
                    }

                    Utilities.Log_Debug("Getting Telescope Galaxy Status");
                    foreach (TSTGalaxy g in TSTGalaxies.Galaxies)
                    {
                        if (telescopeNode.HasValue(g.name))
                            TelescopeData[g.name] = telescopeNode.GetValue(g.name) == "true";
                        else
                        {
                            TelescopeData[g.name] = false;
                        }
                    }
                }
                else
                {
                    foreach (CelestialBody b in FlightGlobals.Bodies.Where(p => p.Radius > 100))
                        TelescopeData[b.name] = false;
                    foreach (TSTGalaxy g in TSTGalaxies.Galaxies)
                        TelescopeData[g.name] = false;
                }
            }
            catch (Exception ex)
            {
                Utilities.Log_Debug("Getting Telescope ConfigNode data Failed unexpectedly. Ex: " + ex.Message);
            }
            try
            {
                Utilities.Log_Debug("Getting ChemCam Celestial Body Status");
                if (chemCamNode != null)
                {
                    foreach (CelestialBody b in FlightGlobals.Bodies.Where(p => p.Radius > 100 && p.pqsController != null))
                    {
                        List<string> biomes = ResearchAndDevelopment.GetBiomeTags(b, true);
                        if (biomes.Count > 1)
                        {
                            foreach (string biome in biomes)
                            {
                                string nodename = b.name + "," + biome;
                                if (chemCamNode.HasValue(nodename))
                                    ChemCamData[nodename] = chemCamNode.GetValue(nodename) == "true";
                                else
                                {
                                    ChemCamData[nodename] = false;
                                }
                            }
                        }
                        else
                        {
                            if (chemCamNode.HasValue(b.name))
                                ChemCamData[b.name] = chemCamNode.GetValue(b.name) == "true";
                            else
                            {
                                ChemCamData[b.name] = false;
                            }
                        }
                    }
                }
                else
                {
                    foreach (CelestialBody b in FlightGlobals.Bodies.Where(p => p.Radius > 100 && p.pqsController != null))
                    {
                        List<string> biomes = ResearchAndDevelopment.GetBiomeTags(b, true);
                        if (biomes.Count > 1)
                        {
                            foreach (string biome in biomes)
                            {
                                string nodename = b.name + "," + biome;
                                ChemCamData[nodename] = false;
                            }
                        }
                        else
                        {
                            ChemCamData[b.name] = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.Log_Debug("Getting Telescope ConfigNode data Failed unexpectedly. Ex: " + ex.Message);
            }
        }

        public override void OnSave(ConfigNode node)
        {
            Utilities.Log_Debug("Saving TST Progress data");
            ConfigNode telescopeNode = node.AddNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.AddNode("TarsierChemCam");
            foreach (string key in TelescopeData.Keys)
                telescopeNode.AddValue(key, TelescopeData[key] ? "true" : "false");
            foreach (string key in ChemCamData.Keys)
                chemCamNode.AddValue(key, ChemCamData[key] ? "true" : "false");
        }

        #region ResearchBodies
        
        private void processScience(float science, ScienceSubject scienceSubject, ProtoVessel vessel, bool whoKnows)
        {
            if (isRBactive && RBWrapper.RBactualAPI.enabled)
            {
                if (scienceSubject.id.Contains("TarsierSpaceTech.SpaceTelescope"))
                {
                    int index = scienceSubject.id.IndexOf("LookingAt");
                    if (index != -1)                        
                    {
                        string[] tmpIDelements = scienceSubject.id.Split('@');
                        string[] valuesasarray = { "LookingAt" };
                        string[] splitvars = tmpIDelements[1].Split(valuesasarray, StringSplitOptions.None);
                        string bodyName = splitvars[1];                        
                        KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo> foundbodyentry = TSTMstStgs.Instance.RBCelestialBodies.FirstOrDefault(a => a.Key.name == bodyName);
                        if (foundbodyentry.Key != null)
                        {
                            try
                            {
                                processResearchBody(foundbodyentry.Key);
                            }
                            catch (Exception ex)
                            {
                                Utilities.Log("Processing of celestial body for ResearchBodies unexpectedly. Interface disabled. Ex: {0}" , ex.Message);
                                isRBactive = false;
                            }
                        }
                        else
                        {
                            Utilities.Log("Failed to find ResearchBody {0} to process for ResearchBodies mod" , bodyName);
                        }
                    }
                }
            }            
        }

        private void processResearchBody(CelestialBody bodyFound)
        {
            //If ResearchBodies is installed this method is called when a picture is taken, we need to check if we have discovered this object before or not. If we haven't - discover it.
            //If we have, increase the research amount.
            bool foundBody = false, withParent = false;
            CelestialBody parentBody = null;
            
            
            if (!TSTMstStgs.Instance.RBCelestialBodies[bodyFound].isResearched)  //This body has not been previously discovered
            {
                foundBody = RBWrapper.RBactualAPI.FoundBody(0, bodyFound, out withParent, out parentBody);
            }
            else //This body has been previously discovered, so a new picture means Increase the Research amount
            {
                RBWrapper.RBactualAPI.Research(bodyFound, ScienceReward);
            }

            if (foundBody) //If we found a new body we create some screen messages
            {
                if (RBWrapper.APIRBReady)
                {
                    if (RBPopupMsg != string.Empty)
                    {
                        if (TSTMstStgs.Instance.RBCelestialBodies.ContainsKey(bodyFound))
                        {
                            RBPopupMsg = RBPopupMsg + " \r" + Localizer.Format("#autoLOC_RBodies_discovery_" + bodyFound.bodyName);
                        }
                    }
                    else
                    {
                        if (TSTMstStgs.Instance.RBCelestialBodies.ContainsKey(bodyFound))
                        {
                            RBPopupMsg = Localizer.Format("#autoLOC_RBodies_discovery_" + bodyFound.bodyName);
                        }
                    }
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0064", bodyFound.displayName), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0064 = Discovered new body <<1>>
                    if (withParent)
                    {
                        if (RBWrapper.RBactualAPI.CelestialBodies.ContainsKey(parentBody))
                        {
                            RBPopupMsg = RBPopupMsg + " \r" + Localizer.Format("#autoLOC_RBodies_discovery_" + parentBody.bodyName);
                        }
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0064", parentBody.displayName), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0064 = Discovered new body <<1>>
                    }
                    showRBWindow = true;
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0064", bodyFound.displayName), 5.0f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0064 = Discovered new body <<1>>
                }

            }
        }

        public void OnGUI()
        {
            if (!Utilities.isPauseMenuOpen)
            {
                if (showRBWindow)
                    RBWindowPos = GUILayout.Window(RBwindowID, RBWindowPos, RBWindow, Localizer.Format("#autoLOC_TST_0063"), GUILayout.Width(200)); //#autoLOC_TST_0063 = Research Bodies

            }
        }
        private void RBWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(RBPopupMsg, GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            if (GUILayout.Button(Localizer.Format("#autoLOC_TST_0065"))) //#autoLOC_TST_0065 = Close
            {
                showRBWindow = false;
                RBPopupMsg = string.Empty;
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        #endregion ResearchBodies

    }
}