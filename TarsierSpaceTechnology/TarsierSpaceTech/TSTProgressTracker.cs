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
using System.IO;
using System.Linq;
using Contracts;
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
        private float ScienceReward = 20;

        public Dictionary<CelestialBody, bool> TrackedBodies = new Dictionary<CelestialBody, bool>();
        public Dictionary<CelestialBody, int> ResearchState = new Dictionary<CelestialBody, int>();
        private List<CelestialBody> BodyList = new List<CelestialBody>();
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
                    RBWrapper.InitRBDBWrapper();
                    RBwindowID = Utilities.getnextrandomInt();
                    LoadRBConfig();
                    if (RBWrapper.APIDBReady)
                        GameEvents.OnScienceRecieved.Add(processScience);
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

        public static void setChemCamContractComplete(CelestialBody body)
        {
            Instance.ChemCamData[body.name] = true;
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
            if (TSTMstStgs.Instance.isRBactive && target != default(string))
            {
                try
                {
                    if (RBWrapper.APISCReady)
                    {
                        //if (RBWrapper.RBSCactualAPI.enabled)
                        //{
                        List<KeyValuePair<CelestialBody, bool>> trackbodyentry = TSTMstStgs.Instance.TrackedBodies.Where(e => e.Key.name == target).ToList();
                        if (trackbodyentry.Count != 1)
                        {
                            Utilities.Log("Unable to set target {0} at this time as it is not a tracked body", target);
                            target = default(string);
                            return target;
                        }
                        if (trackbodyentry[0].Value == false)
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

        public static Contract.ContractPrestige getTelescopePrestige(string bodyName)
        {
            int i = 0;
            int significant = 4;
            int exceptional = 7;

            if (isRSSactive) //If Real Solar System is installed
            {
                i = Array.IndexOf(TSTMstStgs.Instance.TSTrssplanets.RSSPlanetOrder, bodyName);
                significant = 3;
                exceptional = 7;
            }
            else
            {
                if (isOPMactive) // If Outer Planets Mod is installed
                {
                    i = Array.IndexOf(TSTMstStgs.Instance.TSTopmplanets.OPMPlanetOrder, bodyName);
                    significant = 7;
                    exceptional = 17;
                }
                else
                {
                    if (isNHactive) // If new Horizons Planets Mod is installed
                    {
                        i = Array.IndexOf(TSTMstStgs.Instance.TSTnhplanets.NHPlanetOrder, bodyName);
                        significant = 7;
                        exceptional = 17;
                    }
                    else
                    {
                        if (Utilities.IsKopInstalled)
                        {
                            //i = Array.IndexOf(Instance.bodyNames.ToArray(), bodyName);
                            double distance = Utilities.DistanceFromHomeWorld(bodyName);
                            // We need some formula here based on distance
                        }
                        else  //Default Stock
                        {
                            //i = Array.IndexOf(TSTMstStgs.Instance.TSTstockplanets.StockPlanetOrder, bodyName);
                            double distance = Utilities.DistanceFromHomeWorld(bodyName);
                            // We need some formula here based on distance
                        }
                    }
                }
            }
            if (i < significant)
                return Contract.ContractPrestige.Trivial;
            if (i < exceptional)
                return Contract.ContractPrestige.Significant;
            return Contract.ContractPrestige.Exceptional;
        }

        public static Contract.ContractPrestige getChemCamPrestige(CelestialBody body)
        {
            int i = 0;
            int significant = 4;
            int exceptional = 7;

            if (isRSSactive) //If Real Solar System is installed
            {
                i = Array.IndexOf(TSTMstStgs.Instance.TSTrssplanets.RSSPlanetOrder, body.name);
                significant = 3;
                exceptional = 7;
            }
            else
            {
                if (isOPMactive) // If Outer Planets Mod is installed
                {
                    i = Array.IndexOf(TSTMstStgs.Instance.TSTopmplanets.OPMPlanetOrder, body.name);
                    significant = 7;
                    exceptional = 17;
                }
                else
                {
                    if (isNHactive) // If New Horizons Planets Mod is installed
                    {
                        i = Array.IndexOf(TSTMstStgs.Instance.TSTnhplanets.NHPlanetOrder, body.name);
                        significant = 7;
                        exceptional = 17;
                    }
                    else
                    {
                        if (Utilities.IsKopInstalled)
                        {
                            //i = Array.IndexOf(Instance.bodyNames.ToArray(), body.name);
                            double distance = Utilities.DistanceFromHomeWorld(body.name);
                            // We need some formula here based on distance
                        }
                        else  //Default Stock
                        {
                            //i = Array.IndexOf(TSTMstStgs.Instance.TSTstockplanets.StockPlanetOrder, body.name);
                            double distance = Utilities.DistanceFromHomeWorld(body.name);
                            // We need some formula here based on distance
                        }
                    }
                }
            }

            if (i < significant)
                return Contract.ContractPrestige.Trivial;
            if (i < exceptional)
                return Contract.ContractPrestige.Significant;
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
                        if (chemCamNode.HasValue(b.name))
                            ChemCamData[b.name] = chemCamNode.GetValue(b.name) == "true";
                        else
                        {
                            ChemCamData[b.name] = false;
                        }
                    }
                    
                }
                else
                {
                    foreach (CelestialBody b in FlightGlobals.Bodies.Where(p => p.Radius > 100 && p.pqsController != null))
                        ChemCamData[b.name] = false;
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

        public void LoadRBConfig()
        {
            TrackedBodies.Clear();
            ResearchState.Clear();
            if (!File.Exists("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg"))
            {
                ConfigNode file = new ConfigNode();
                ConfigNode node = file.AddNode("RESEARCHBODIES");

                BodyList = FlightGlobals.Bodies;
                BodyList = BodyList.Concat(TSTGalaxies.CBGalaxies).ToList();


                foreach (CelestialBody cb in BodyList)
                {
                    ConfigNode cbCfg = node.AddNode("BODY");
                    cbCfg.AddValue("body", cb.GetName());
                    cbCfg.AddValue("isResearched", "false");
                    cbCfg.AddValue("researchState", "0");
                    TrackedBodies[cb] = false;
                    ResearchState[cb] = 0;
                }
                file.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
            }
            else
            {
                ConfigNode mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");

                BodyList = FlightGlobals.Bodies;
                BodyList = BodyList.Concat(TSTGalaxies.CBGalaxies).ToList();

                foreach (CelestialBody cb in BodyList)
                {
                    bool fileContainsCB = false;
                    foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                    {
                        if (cb.GetName().Contains(node.GetValue("body")))
                        {
                            bool ignore = false;
                            bool.TryParse(node.GetValue("ignore"), out ignore);
                            if (ignore)
                            {
                                TrackedBodies[cb] = true;
                                ResearchState[cb] = 100;
                            }
                            else
                            {
                                TrackedBodies[cb] = bool.Parse(node.GetValue("isResearched"));
                                if (node.HasValue("researchState"))
                                {
                                    ResearchState[cb] = int.Parse(node.GetValue("researchState"));
                                }
                                else
                                {
                                    ConfigNode cbNode = null;
                                    foreach (ConfigNode cbSettingNode in mainnode.GetNode("RESEARCHBODIES").nodes)
                                    {
                                        if (cbSettingNode.GetValue("body") == cb.GetName())
                                            cbNode = cbSettingNode;
                                    }
                                    if (cbNode != null) cbNode.AddValue("researchState", "0");
                                    mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                                    ResearchState[cb] = 0;
                                }
                            }
                            fileContainsCB = true;
                        }
                    }
                    if (!fileContainsCB)
                    {
                        ConfigNode newNodeForCB = mainnode.GetNode("RESEARCHBODIES").AddNode("BODY");
                        newNodeForCB.AddValue("body", cb.GetName());
                        newNodeForCB.AddValue("isResearched", "false");
                        newNodeForCB.AddValue("researchState", "0");
                        newNodeForCB.AddValue("ignore", "false");
                        TrackedBodies[cb] = false; ResearchState[cb] = 0;
                        mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                    }
                }
            }
        }

        private void processScience(float science, ScienceSubject scienceSubject, ProtoVessel vessel, bool whoKnows)
        {
            if (isRBactive)
            {
                if (scienceSubject.id.Contains("TarsierSpaceTech.SpaceTelescope"))
                {
                    if (scienceSubject.title.Contains("Space Telescope picture of "))
                    {
                        string bodyName = scienceSubject.title.Substring(27);
                        CelestialBody body = BodyList.FirstOrDefault(a => a.theName == bodyName);
                        if (body != null)
                        {
                            try
                            {
                                processResearchBody(body);
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

        private void processResearchBody(CelestialBody body)
        {
            //If ResearchObjects is installed this method is called when a picture is taken, we need to check if we have discovered this object before or not. If we haven't - discover it.
            //If we have, increase the research amount.
            bool foundBody = false, withParent = false;
            string bodyFound = string.Empty, parentBody = string.Empty;

            LoadRBConfig();

            
            if (!TrackedBodies[body])  //This body has not been previously discovered
            {
                foundBody = true;
                if (HighLogic.CurrentGame.Mode != Game.Modes.SANDBOX) //give science reward for finding a new body
                {
                    ResearchAndDevelopment.Instance.AddScience(ScienceReward, TransactionReasons.None);
                    ScreenMessages.PostScreenMessage("Added " + ScienceReward + " science points !", 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }
                ConfigNode mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                
                    if (body.referenceBody.DiscoveryInfo.Level == DiscoveryLevels.Presence)  // If the parent body of our newly discovered body is also not discovered - we discover both.
                    {
                        TrackedBodies[body.referenceBody] = true;
                        TrackedBodies[body] = true;
                        withParent = true;
                        parentBody = body.referenceBody.GetName();
                        //I'm not sure we really need to write this to the ResearchBodies config file, because we aren't for other changes.. but just to be on the safe side.
                        foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                        {
                            if (node.GetValue("body") == body.referenceBody.GetName())
                            {
                                node.SetValue("isResearched", "true");
                            }
                            if (node.GetValue("body") == body.GetName())
                            {
                                node.SetValue("isResearched", "true");
                            }
                            try
                            {
                                if (node.GetValue("body") == body.referenceBody.referenceBody.GetName() && (body.referenceBody.referenceBody.DiscoveryInfo.Level == DiscoveryLevels.Appearance || body.referenceBody.referenceBody.DiscoveryInfo.Level == DiscoveryLevels.Presence))
                                {
                                    node.SetValue("isResearched", "true");
                                }
                            }
                            catch { }
                        }
                        Utilities.Log_Debug("[ResearchBodies] Found body " + body.GetName() + " orbiting around " + body.referenceBody.GetName() + " !");
                    }
                    else  //The parent body is already known, we are discovering just the one body.
                    {
                        TrackedBodies[body] = true;
                        withParent = false;
                        foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                        {
                            if (node.GetValue("body") == body.GetName())
                            {
                                node.SetValue("isResearched", "true");
                            }
                        }
                        Utilities.Log_Debug("[ResearchBodies] Found body " + body.GetName() + " !");
                    }
                
                bodyFound = body.GetName();
                mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
            }
            else //This body has been previously discovered, so a new picture means Increase the Research amount
            {
                foundBody = false;
                if (ResearchState[body] < 100)
                {
                    ResearchState[body] += 20;

                    ConfigNode mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                    ConfigNode bodyNode = null;
                    foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                    {
                        if (node.GetValue("body") == body.GetName())
                            bodyNode = node;
                    }
                    if (bodyNode != null) bodyNode.SetValue("researchState", ResearchState[body].ToString());
                    mainnode.Save("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                }
            }

            if (foundBody) //If we found a new body we create some screen messages
            {
                if (RBWrapper.APIDBReady)
                {
                    if (RBPopupMsg != string.Empty)
                    {
                        if (RBWrapper.RBDBactualAPI.DiscoveryMessage.ContainsKey(bodyFound))
                        {
                            RBPopupMsg = RBPopupMsg + " \r" + RBWrapper.RBDBactualAPI.DiscoveryMessage[bodyFound];
                        }
                    }
                    else
                    {
                        if (RBWrapper.RBDBactualAPI.DiscoveryMessage.ContainsKey(bodyFound))
                        {
                            RBPopupMsg = RBWrapper.RBDBactualAPI.DiscoveryMessage[bodyFound];
                        }
                    }
                    ScreenMessages.PostScreenMessage("Discovered new body " + body.name, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    if (withParent)
                    {
                        if (RBWrapper.RBDBactualAPI.DiscoveryMessage.ContainsKey(parentBody))
                        {
                            RBPopupMsg = RBPopupMsg + " \r" + RBWrapper.RBDBactualAPI.DiscoveryMessage[parentBody];
                        }
                        ScreenMessages.PostScreenMessage("Discovered new body " + parentBody, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                    }
                    showRBWindow = true;
                }
                else
                {
                    ScreenMessages.PostScreenMessage("Discovered new body " + body.name, 5.0f, ScreenMessageStyle.UPPER_CENTER);
                }

            }
        }

        public void OnGUI()
        {
            if (!Utilities.isPauseMenuOpen)
            {
                if (showRBWindow)
                    RBWindowPos = GUILayout.Window(RBwindowID, RBWindowPos, RBWindow, "Research Bodies", GUILayout.Width(200));

            }
        }
        private void RBWindow(int windowID)
        {
            GUILayout.BeginVertical();
            GUILayout.Label(RBPopupMsg, GUILayout.ExpandWidth(false));
            GUILayout.Space(10);
            if (GUILayout.Button("Close"))
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