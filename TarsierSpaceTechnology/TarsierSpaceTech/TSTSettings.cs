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
using KSP.Localization;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    internal class TSTMstStgs : MonoBehaviour
    {
        public static TSTMstStgs Instance { get; private set; }               
        internal ConfigNode globalNode = new ConfigNode();
        public TSTSettings TSTsettings { get; private set; }
        public TSTStockPlanets TSTstockplanets { get; private set; }
        public TSTRSSPlanets TSTrssplanets { get; private set; }
        public TSTOPMPlanets TSTopmplanets { get; private set; } 
        public TSTNHPlanets TSTnhplanets { get; private set; }        
        private readonly string globalConfigFilename;
        internal bool isRBactive = false;
        internal bool isRBloaded = false;
        internal bool loadRBthisscene = false;

        internal Dictionary<CelestialBody, RBWrapper.CelestialBodyInfo> RBCelestialBodies =
            new Dictionary<CelestialBody, RBWrapper.CelestialBodyInfo>();
        
        public TSTMstStgs()
        {                       
            Utilities.Log("TSTMstStgs Constructor");
            Instance = this;
            TSTsettings = new TSTSettings();
            TSTstockplanets = new TSTStockPlanets();
            TSTrssplanets = new TSTRSSPlanets();
            TSTopmplanets = new TSTOPMPlanets();
            TSTnhplanets = new TSTNHPlanets();
            globalConfigFilename = Path.Combine(_AssemblyFolder, "PluginData/Config.cfg").Replace("\\", "/");
            Utilities.Log("TSTMstStgs globalConfigFilename = " + globalConfigFilename);
        }

        public void Awake()
        {
            // Load the global settings
            if (File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                TSTsettings.Load(globalNode);
                TSTstockplanets.Load(globalNode);
                TSTrssplanets.Load(globalNode);
                TSTopmplanets.Load(globalNode);
                TSTnhplanets.Load(globalNode);
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
            if (Time.timeSinceLevelLoad < 3.0f || !isRBactive || !loadRBthisscene || isRBloaded) // Check not loading level, or ResearchBodies is not active, or don't need to load RB this scene or it's already loaded.
            {
                return;
            }
            
            if (isRBactive)
            {
                if (!RBWrapper.APIRBReady)
                    RBWrapper.InitRBWrapper();
                isRBloaded = RBWrapper.APIRBReady;
                if (isRBloaded)
                {
                    RBCelestialBodies = RBWrapper.RBactualAPI.CelestialBodies;
                }
            }
        }

        public void OnDestroy()
        {
            TSTsettings.Save(globalNode);
            TSTstockplanets.Save(globalNode);
            TSTrssplanets.Save(globalNode);
            TSTopmplanets.Save(globalNode);
            TSTnhplanets.Save(globalNode);
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
        { get { return Assembly.GetExecutingAssembly().Location.Replace("\\", "/"); } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return Path.GetDirectoryName(_AssemblyLocation).Replace("\\", "/"); } }

        #endregion Assembly/Class Information
    }

    public class TSTSettings
    {
        private const string configNodeName = "TSTSettings";

        public float FwindowPosX ;
        public float FwindowPosY ;
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
        public bool ZoomSkyBox;
        public int scienceUndiscoveredScope;
        public int scienceDiscoveredScope;
        public int repUndiscoveredScope;
        public int repDiscoveredScope;
        public int fundsUndiscoveredScope;
        public int fundsdiscoveredScope;
        public int scienceUndiscoveredChem;
        public int scienceDiscoveredChem;
        public int repUndiscoveredChem;
        public int repDiscoveredChem;
        public int fundsUndiscoveredChem;
        public int fundsdiscoveredChem;

        public TSTSettings()
        {
            FwindowPosX = 40;
            FwindowPosY = 50;
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
            ZoomSkyBox = true;
            scienceUndiscoveredScope = 20;
            scienceDiscoveredScope = 2;
            repUndiscoveredScope = 20;
            repDiscoveredScope = 2;
            fundsUndiscoveredScope = 600;
            fundsdiscoveredScope = 35;
            scienceUndiscoveredChem = 20;
            scienceDiscoveredChem = 5;
            repUndiscoveredChem = 35;
            repDiscoveredChem = 5;
            fundsUndiscoveredChem = 800;
            fundsdiscoveredChem = 40;
    }

        //Settings Functions Follow

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTsettingsNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTsettingsNode);
                TSTsettingsNode.TryGetValue("FwindowPosX", ref FwindowPosX);
                TSTsettingsNode.TryGetValue("FwindowPosY", ref FwindowPosY);
                TSTsettingsNode.TryGetValue("CwindowPosX", ref CwindowPosX);
                TSTsettingsNode.TryGetValue("CwindowPosY", ref CwindowPosY);
                TSTsettingsNode.TryGetValue("GalwindowPosX", ref GalwindowPosX);
                TSTsettingsNode.TryGetValue("GalwindowPosY", ref GalwindowPosY);
                TSTsettingsNode.TryGetValue("BodwindowPosX", ref BodwindowPosX);
                TSTsettingsNode.TryGetValue("BodwindowPosY", ref BodwindowPosY);
                TSTsettingsNode.TryGetValue("scienceUndiscoveredScope", ref scienceUndiscoveredScope);
                TSTsettingsNode.TryGetValue("scienceDiscoveredScope", ref scienceDiscoveredScope);
                TSTsettingsNode.TryGetValue("repUndiscoveredScope", ref repUndiscoveredScope);
                TSTsettingsNode.TryGetValue("repDiscoveredScope", ref repDiscoveredScope);
                TSTsettingsNode.TryGetValue("fundsUndiscoveredScope", ref fundsUndiscoveredScope);
                TSTsettingsNode.TryGetValue("fundsdiscoveredScope", ref fundsdiscoveredScope);
                TSTsettingsNode.TryGetValue("scienceUndiscoveredChem", ref scienceUndiscoveredChem);
                TSTsettingsNode.TryGetValue("scienceDiscoveredChem", ref scienceDiscoveredChem);
                TSTsettingsNode.TryGetValue("repUndiscoveredChem", ref repUndiscoveredChem);
                TSTsettingsNode.TryGetValue("repDiscoveredChem", ref repDiscoveredChem);
                TSTsettingsNode.TryGetValue("fundsUndiscoveredChem", ref fundsUndiscoveredChem);
                TSTsettingsNode.TryGetValue("fundsdiscoveredChem", ref fundsdiscoveredChem);

                ApplySettings();
                Utilities.Log_Debug("TSTSettings load complete");
            }
        }

        internal void ApplySettings()
        {
            Utilities.Log_Debug("TSTSettings ApplySettings Start");
            if (HighLogic.CurrentGame != null)
            {
                TST_SettingsParms TST_SettingsParms = HighLogic.CurrentGame.Parameters.CustomParams<TST_SettingsParms>();
                if (TST_SettingsParms != null)
                {
                    TSTMenu GUI = TSTMenu.Instance ?? null;
                    ChemwinSml = TST_SettingsParms.ChemwinSml;
                    ChemwinLge = TST_SettingsParms.ChemwinLge;
                    TelewinSml = TST_SettingsParms.TelewinSml;
                    TelewinLge = TST_SettingsParms.TelewinLge;
                    
                    if (UseAppLauncher != TST_SettingsParms.UseAppLauncher)
                    {
                        UseAppLauncher = TST_SettingsParms.UseAppLauncher;
                        if (GUI != null)
                        {
                            GUI.TSTMenuAppLToolBar.chgAppIconStockToolBar(UseAppLauncher);
                        }
                    }
                    debugging = TST_SettingsParms.debugging;
                    Utilities.debuggingOn = debugging;
                    Tooltips = TST_SettingsParms.ToolTips;
                    maxChemCamContracts = TST_SettingsParms.maxChemCamContracts;
                    photoOnlyChemCamContracts = TST_SettingsParms.photoOnlyChemCamContracts;
                    ZoomSkyBox = TST_SettingsParms.ZoomSkyBox;
                }
                else
                    Utilities.Log_Debug("DFSettings ApplySettings Settings Params Not Set!");
            }
            else
                Utilities.Log_Debug("DFSettings ApplySettings CurrentGame is NULL!");
            Utilities.Log_Debug("DFSettings ApplySettings End");
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
            settingsNode.AddValue("CwindowPosX", CwindowPosX);
            settingsNode.AddValue("CwindowPosY", CwindowPosY);
            settingsNode.AddValue("GalwindowPosX", GalwindowPosX);
            settingsNode.AddValue("GalwindowPosY", GalwindowPosY);
            settingsNode.AddValue("BodwindowPosX", BodwindowPosX);
            settingsNode.AddValue("BodwindowPosY", BodwindowPosY);
            settingsNode.AddValue("scienceUndiscoveredScope", scienceUndiscoveredScope);
            settingsNode.AddValue("scienceDiscoveredScope", scienceDiscoveredScope);
            settingsNode.AddValue("repUndiscoveredScope", repUndiscoveredScope);
            settingsNode.AddValue("repDiscoveredScope", repDiscoveredScope);
            settingsNode.AddValue("fundsUndiscoveredScope", fundsUndiscoveredScope);
            settingsNode.AddValue("fundsdiscoveredScope", fundsdiscoveredScope);
            settingsNode.AddValue("scienceUndiscoveredChem", scienceUndiscoveredChem);
            settingsNode.AddValue("scienceDiscoveredChem", scienceDiscoveredChem);
            settingsNode.AddValue("repUndiscoveredChem", repUndiscoveredChem);
            settingsNode.AddValue("repDiscoveredChem", repDiscoveredChem);
            settingsNode.AddValue("fundsUndiscoveredChem", fundsUndiscoveredChem);
            settingsNode.AddValue("fundsdiscoveredChem", fundsdiscoveredChem);
            Utilities.Log_Debug("TSTSettings save complete");
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
            if (StockPlanetOrder.Length > 0)
            {
                string tmpPlanetOrder = string.Join(",", StockPlanetOrder);
                TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            }
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
            if (RSSPlanetOrder.Length > 0)
            {
                string tmpPlanetOrder = string.Join(",", RSSPlanetOrder);
                TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            }
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
            if (OPMPlanetOrder.Length > 0)
            {
                string tmpPlanetOrder = string.Join(",", OPMPlanetOrder);
                TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            }
            Utilities.Log_Debug("TSTOPMPlanetOrder save complete");
        }
    }

    public class TSTNHPlanets
    {
        private const string configNodeName = "TSTNHPlanetOrder";

        public string[] NHPlanetOrder;

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = new ConfigNode();
                node.TryGetNode(configNodeName, ref TSTStockPlanetOrderNode);
                string tmpPlanetOrderString = "";
                TSTStockPlanetOrderNode.TryGetValue("planets", ref tmpPlanetOrderString);
                string[] tmpPlanetOrder = tmpPlanetOrderString.Split(',');
                NHPlanetOrder = new string[tmpPlanetOrder.Length];
                if (tmpPlanetOrder.Length > 0)
                {
                    for (int i = 0; i < tmpPlanetOrder.Length; i++)
                    {
                        NHPlanetOrder[i] = tmpPlanetOrder[i];
                    }
                }
            }
            Utilities.Log_Debug("TSTNHPlanetOrder load complete");
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
            if (NHPlanetOrder.Length > 0)
            {
                string tmpPlanetOrder = string.Join(",", NHPlanetOrder);
                TSTStockPlanetOrderNode.AddValue("planets", tmpPlanetOrder);
            }
            Utilities.Log_Debug("TSTNHPlanetOrder save complete");
        }
    }
}