/**
 * TSTSettings.cs
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
using System.Reflection;
using RSTUtils;
using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal class TSTMstStgs : MonoBehaviour
    {
        public static TSTMstStgs Instance { get; private set; }               
        internal ConfigNode globalNode = new ConfigNode();
        public TSTSettings TSTsettings { get; }
        public TSTGasPlanets TSTgasplanets { get; }
        public TSTStockPlanets TSTstockplanets { get; }
        public TSTRSSPlanets TSTrssplanets { get; }
        public TSTOPMPlanets TSTopmplanets { get; }         
        private readonly string globalConfigFilename;
        internal bool isRBactive;
        internal bool isRBloaded;
        internal bool loadRBthisscene;
        internal Dictionary<CelestialBody, bool> TrackedBodies = new Dictionary<CelestialBody, bool>();
        internal Dictionary<CelestialBody, int> ResearchState = new Dictionary<CelestialBody, int>();

        public TSTMstStgs()
        {                       
            Utilities.Log("TSTMstStgs Constructor");
            Instance = this;
            TSTsettings = new TSTSettings();
            TSTgasplanets = new TSTGasPlanets();
            TSTstockplanets = new TSTStockPlanets();
            TSTrssplanets = new TSTRSSPlanets();
            TSTopmplanets = new TSTOPMPlanets();
            globalConfigFilename = Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            Utilities.Log("TSTMstStgs globalConfigFilename = " + globalConfigFilename);
        }

        public void Awake()
        {
            // Load the global settings
            if (File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                TSTsettings.Load(globalNode);
                TSTgasplanets.Load(globalNode);
                TSTstockplanets.Load(globalNode);
                TSTrssplanets.Load(globalNode);
                TSTopmplanets.Load(globalNode);
            }           
            Utilities.Log("TSTMstStgs", "OnLoad: \n {0}" + globalNode);
        }

        public void Start()
        {
            isRBactive = Utilities.IsResearchBodiesInstalled;
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER || HighLogic.LoadedScene == GameScenes.TRACKSTATION || HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                loadRBthisscene = true;
            }
        }

        public void Update()
        {
            if (Time.timeSinceLevelLoad < 4.0f || !isRBactive || !loadRBthisscene || isRBloaded) // Check not loading level, or ResearchBodies is not active, or don't need to load RB this scene or it's already loaded.
            {
                return;
            }
            isRBactive = Utilities.IsResearchBodiesInstalled;
            if (isRBactive)
            {
                RBWrapper.InitRBDBWrapper();
                RBWrapper.InitRBSCWrapper();
                RBWrapper.InitRBFLWrapper();
                if (RBWrapper.APISCReady)
                {

                    TrackedBodies = RBWrapper.RBSCactualAPI.TrackedBodies;
                    ResearchState = RBWrapper.RBSCactualAPI.ResearchState;

                    Dictionary<string, string> dbdiscoverymsgs = RBWrapper.RBDBactualAPI.DiscoveryMessage;

                    if (File.Exists("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg"))
                    {
                        ConfigNode mainnode = ConfigNode.Load("saves/" + HighLogic.SaveFolder + "/researchbodies.cfg");
                        foreach (CelestialBody cb in TSTGalaxies.CBGalaxies)
                        {
                            bool fileContainsGalaxy = false;
                            foreach (ConfigNode node in mainnode.GetNode("RESEARCHBODIES").nodes)
                            {
                                if (cb.bodyName.Contains(node.GetValue("body")))
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
                                    fileContainsGalaxy = true;
                                }
                            }
                            if (!fileContainsGalaxy)
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
                        isRBloaded = true;
                    }
                }
            }
        }

        public void OnDestroy()
        {
            TSTsettings.Save(globalNode);
            TSTgasplanets.Save(globalNode);
            TSTstockplanets.Save(globalNode);
            TSTrssplanets.Save(globalNode);
            TSTopmplanets.Save(globalNode);
            globalNode.Save(globalConfigFilename);
            Utilities.Log_Debug("TSTMstStgs OnSave: \n {0}" , globalNode.ToString());
        }

        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    public class TSTSettings
    {
        private const string configNodeName = "TSTSettings";

        public float FwindowPosX ;
        public float FwindowPosY ;
        public float SCwindowPosX ;
        public float SCwindowPosY ;
        public float CwindowPosX ;
        public float CwindowPosY ;
        public float GalwindowPosX ;
        public float GalwindowPosY ;
        public float BodwindowPosX ;
        public float BodwindowPosY ;
        public int ChemwinSml ;
        public int ChemwinLge ;
        public int TelewinSml ;
        public int TelewinLge ;
        public bool UseAppLauncher ;
        public bool debugging ;
        public bool Tooltips ;
        public int maxChemCamContracts ;
        public bool photoOnlyChemCamContracts ;

        public TSTSettings()
        {
            FwindowPosX = 40;
            FwindowPosY = 50;
            SCwindowPosX = 40;
            SCwindowPosY = 50;
            CwindowPosX = 128;
            CwindowPosY = 128;
            GalwindowPosX = 512;
            GalwindowPosY = 128;
            BodwindowPosX = 512;
            BodwindowPosY = 128;
            ChemwinSml = 256;
            ChemwinLge = 512;
            TelewinSml = 320;
            TelewinLge = 600;
            UseAppLauncher = true;
            debugging = true;
            Tooltips = true;
            maxChemCamContracts = 3;
            photoOnlyChemCamContracts = true;
        }

        //Settings Functions Follow

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTsettingsNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTsettingsNode);
                TSTsettingsNode.TryGetValue( "FwindowPosX", ref FwindowPosX);
                TSTsettingsNode.TryGetValue( "FwindowPosY", ref FwindowPosY);
                TSTsettingsNode.TryGetValue( "SCwindowPosX", ref FwindowPosX);
                TSTsettingsNode.TryGetValue( "SCwindowPosY", ref FwindowPosY);
                TSTsettingsNode.TryGetValue( "CwindowPosX", ref CwindowPosX);
                TSTsettingsNode.TryGetValue( "CwindowPosY", ref CwindowPosY);
                TSTsettingsNode.TryGetValue( "GalwindowPosX", ref GalwindowPosX);
                TSTsettingsNode.TryGetValue( "GalwindowPosY", ref GalwindowPosY);
                TSTsettingsNode.TryGetValue( "BodwindowPosX", ref BodwindowPosX);
                TSTsettingsNode.TryGetValue( "BodwindowPosY", ref BodwindowPosY);
                TSTsettingsNode.TryGetValue( "ChemwinSml", ref ChemwinSml);
                TSTsettingsNode.TryGetValue( "ChemwinLge", ref ChemwinLge);
                TSTsettingsNode.TryGetValue( "TelewinSml", ref TelewinSml);
                TSTsettingsNode.TryGetValue( "TelewinLge", ref TelewinLge);
                TSTsettingsNode.TryGetValue( "UseAppLauncher", ref UseAppLauncher);
                TSTsettingsNode.TryGetValue( "debugging", ref debugging);
                TSTsettingsNode.TryGetValue( "Tooltips", ref Tooltips);
                Utilities.debuggingOn = debugging;
                TSTsettingsNode.TryGetValue( "maxChemCamContracts", ref maxChemCamContracts);
                TSTsettingsNode.TryGetValue( "photoOnlyChemCamContracts", ref photoOnlyChemCamContracts);
                Utilities.Log_Debug("TSTSettings load complete");
            }
        }

        public void Save(ConfigNode node)
        {
            ConfigNode settingsNode;
            if (node.HasNode(configNodeName))
            {
                settingsNode = node.GetNode(configNodeName);
                settingsNode.ClearData();
            }
            else
            {
                settingsNode = node.AddNode(configNodeName);
            }
            settingsNode.AddValue("FwindowPosX", FwindowPosX);
            settingsNode.AddValue("FwindowPosY", FwindowPosY);
            settingsNode.AddValue("SCwindowPosX", FwindowPosX);
            settingsNode.AddValue("SCwindowPosY", FwindowPosY);
            settingsNode.AddValue("CwindowPosX", CwindowPosX);
            settingsNode.AddValue("CwindowPosY", CwindowPosY);
            settingsNode.AddValue("GalwindowPosX", GalwindowPosX);
            settingsNode.AddValue("GalwindowPosY", GalwindowPosY);
            settingsNode.AddValue("BodwindowPosX", BodwindowPosX);
            settingsNode.AddValue("BodwindowPosY", BodwindowPosY);
            settingsNode.AddValue("ChemwinSml", ChemwinSml);
            settingsNode.AddValue("ChemwinLge", ChemwinLge);
            settingsNode.AddValue("TelewinSml", TelewinSml);
            settingsNode.AddValue("TelewinLge", TelewinLge);
            settingsNode.AddValue("UseAppLauncher", UseAppLauncher);
            settingsNode.AddValue("debugging", debugging);
            settingsNode.AddValue("Tooltips", Tooltips);
            settingsNode.AddValue("maxChemCamContracts", maxChemCamContracts);
            settingsNode.AddValue("photoOnlyChemCamContracts", photoOnlyChemCamContracts);
            Utilities.Log_Debug("TSTSettings save complete");
        }
    }

    public class TSTGasPlanets
    {
        private const string configNodeName = "TSTGasPlanets";

        public string[] TarsierPlanetOrder ;

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTGasPlanetsNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTGasPlanetsNode);
                string tmpPlanetOrderString = "";
                TSTGasPlanetsNode.TryGetValue("planets", ref tmpPlanetOrderString);
                string[] tmpPlanetOrder = tmpPlanetOrderString.Split(',');
                TarsierPlanetOrder = new string[tmpPlanetOrder.Length];
                if (tmpPlanetOrder.Length > 0)
                {
                    for (int i = 0; i < tmpPlanetOrder.Length; i++)
                    {
                        TarsierPlanetOrder[i] = tmpPlanetOrder[i];
                    }
                }
            }
            Utilities.Log_Debug("TSTGasPlanets load complete");
        }

        public void Save(ConfigNode node)
        {
            ConfigNode TSTGasPlanetsNode;
            if (node.HasNode(configNodeName))
            {
                TSTGasPlanetsNode = node.GetNode(configNodeName);
                TSTGasPlanetsNode.ClearData();
            }
            else
            {
                TSTGasPlanetsNode = node.AddNode(configNodeName);
            }
            string tmpPlanetOrder = string.Join(",", TarsierPlanetOrder);
            TSTGasPlanetsNode.AddValue("planets", tmpPlanetOrder);
            Utilities.Log_Debug("TSTGasPlanets save complete");
        }
    }

    public class TSTStockPlanets
    {
        private const string configNodeName = "TSTStockPlanetOrder";

        public string[] StockPlanetOrder ;

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTStockPlanetOrderNode);
                string tmpPlanetOrderString = "";
                TSTStockPlanetOrderNode.TryGetValue("planets", ref tmpPlanetOrderString);
                string[] tmpPlanetOrder = tmpPlanetOrderString.Split(',');
                StockPlanetOrder = new string[tmpPlanetOrder.Length];
                if (tmpPlanetOrder.Length > 0)
                {
                    for (int i = 0; i < tmpPlanetOrder.Length; i++)
                    {
                        StockPlanetOrder[i] = tmpPlanetOrder[i];
                    }
                }
            }
            Utilities.Log_Debug("TSTStockPlanetOrder load complete");
        }

        public void Save(ConfigNode node)
        {
            ConfigNode TSTStockPlanetOrderNode;
            if (node.HasNode(configNodeName))
            {
                TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                TSTStockPlanetOrderNode.ClearData();
            }
            else
            {
                TSTStockPlanetOrderNode = node.AddNode(configNodeName);
            }
            string tmpPlanetOrder = string.Join(",", StockPlanetOrder);
            TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            Utilities.Log_Debug("TSTStockPlanetOrder save complete");
        }
    }

    public class TSTRSSPlanets
    {
        private const string configNodeName = "TSTRSSPlanetOrder";

        public string[] RSSPlanetOrder ;

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTStockPlanetOrderNode);
                string tmpPlanetOrderString = "";
                TSTStockPlanetOrderNode.TryGetValue("planets", ref tmpPlanetOrderString);
                string[] tmpPlanetOrder = tmpPlanetOrderString.Split(',');
                RSSPlanetOrder = new string[tmpPlanetOrder.Length];
                if (tmpPlanetOrder.Length > 0)
                {
                    for (int i = 0; i < tmpPlanetOrder.Length; i++)
                    {
                        RSSPlanetOrder[i] = tmpPlanetOrder[i];
                    }
                }
            }
            Utilities.Log_Debug("TSTRSSPlanetOrder load complete");
        }

        public void Save(ConfigNode node)
        {
            ConfigNode TSTStockPlanetOrderNode;
            if (node.HasNode(configNodeName))
            {
                TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                TSTStockPlanetOrderNode.ClearData();
            }
            else
            {
                TSTStockPlanetOrderNode = node.AddNode(configNodeName);
            }
            string tmpPlanetOrder = string.Join(",", RSSPlanetOrder);
            TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            Utilities.Log_Debug("TSTRSSPlanetOrder save complete");
        }
    }

    public class TSTOPMPlanets
    {
        private const string configNodeName = "TSTOPMPlanetOrder";

        public string[] OPMPlanetOrder ;

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTStockPlanetOrderNode);
                string tmpPlanetOrderString = "";
                TSTStockPlanetOrderNode.TryGetValue( "planets", ref tmpPlanetOrderString);
                string[] tmpPlanetOrder = tmpPlanetOrderString.Split(',');
                OPMPlanetOrder = new string[tmpPlanetOrder.Length];
                if (tmpPlanetOrder.Length > 0)
                {
                    for (int i = 0; i < tmpPlanetOrder.Length; i++)
                    {
                        OPMPlanetOrder[i] = tmpPlanetOrder[i];
                    }
                }
            }
            Utilities.Log_Debug("TSTOPMPlanetOrder load complete");
        }

        public void Save(ConfigNode node)
        {
            ConfigNode TSTStockPlanetOrderNode;
            if (node.HasNode(configNodeName))
            {
                TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                TSTStockPlanetOrderNode.ClearData();
            }
            else
            {
                TSTStockPlanetOrderNode = node.AddNode(configNodeName);
            }
            string tmpPlanetOrder = string.Join(",", OPMPlanetOrder);
            TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            Utilities.Log_Debug("TSTOPMPlanetOrder save complete");
        }
    }
}