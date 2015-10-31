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
using System.Linq;
using System.Text;
using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    class TSTMstStgs : MonoBehaviour
    {
        public static TSTMstStgs Instance { get; private set; }
        //private readonly string FilePath;
        private ConfigNode globalNode = new ConfigNode();
        public TSTSettings TSTsettings { get; private set; }
        public TSTGasPlanets TSTgasplanets { get; private set; }
        public TSTStockPlanets TSTstockplanets { get; private set; }
        public TSTRSSPlanets TSTrssplanets { get; private set; }
        public TSTOPMPlanets TSTopmplanets { get; private set; }
        private readonly string globalConfigFilename;


        public TSTMstStgs()
        {
            Utilities.Log("TSTMstStgs", "Constructor");
            Instance = this;
            TSTsettings = new TSTSettings();
            TSTgasplanets = new TSTGasPlanets();
            TSTstockplanets = new TSTStockPlanets();
            TSTrssplanets = new TSTRSSPlanets();
            TSTopmplanets = new TSTOPMPlanets();
            globalConfigFilename = System.IO.Path.Combine(_AssemblyFolder, "Config.cfg").Replace("\\", "/");
            Utilities.Log("TSTMstStgs", "globalConfigFilename = " + globalConfigFilename);
        }

        public void Awake()
        {
            // Load the global settings
            if (System.IO.File.Exists(globalConfigFilename))
            {
                globalNode = ConfigNode.Load(globalConfigFilename);
                TSTsettings.Load(globalNode);
                TSTgasplanets.Load(globalNode);
                TSTstockplanets.Load(globalNode);
                TSTrssplanets.Load(globalNode);
                TSTopmplanets.Load(globalNode);
            }
            Utilities.Log("TSTMstStgs", "OnLoad: \n " + globalNode);            
        }

        public void OnDestroy()
        {
            TSTsettings.Save(globalNode);
            TSTgasplanets.Save(globalNode);
            TSTstockplanets.Save(globalNode);
            TSTrssplanets.Save(globalNode);
            TSTopmplanets.Save(globalNode);
            globalNode.Save(globalConfigFilename);            
            this.Log_Debug("TSTMstStgs OnSave: \n " + globalNode);
        }


        #region Assembly/Class Information

        /// <summary>
        /// Name of the Assembly that is running this MonoBehaviour
        /// </summary>
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        /// <summary>
        /// Full Path of the executing Assembly
        /// </summary>
        internal static String _AssemblyLocation
        { get { return System.Reflection.Assembly.GetExecutingAssembly().Location; } }

        /// <summary>
        /// Folder containing the executing Assembly
        /// </summary>
        internal static String _AssemblyFolder
        { get { return System.IO.Path.GetDirectoryName(_AssemblyLocation); } }

        #endregion Assembly/Class Information
    }

    public class TSTSettings
    {
        private const string configNodeName = "TSTSettings";

        public float FwindowPosX { get; set; }
        public float FwindowPosY { get; set; }
        public float CwindowPosX { get; set; }
        public float CwindowPosY { get; set; }
        public float GalwindowPosX { get; set; }
        public float GalwindowPosY { get; set; }
        public float BodwindowPosX { get; set; }
        public float BodwindowPosY { get; set; }
        public int ChemwinSml { get; set; }
        public int ChemwinLge { get; set; }
        public int TelewinSml { get; set; }
        public int TelewinLge { get; set; }
        public bool UseAppLauncher { get; set; }
        public bool debugging { get; set; }
        public int maxChemCamContracts { get; set; }
        public bool photoOnlyChemCamContracts { get; set; }

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
            maxChemCamContracts = 3;
            photoOnlyChemCamContracts = true;
        }

        //Settings Functions Follow

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTsettingsNode = node.GetNode(configNodeName);
                FwindowPosX = Utilities.GetNodeValue(TSTsettingsNode, "FwindowPosX", FwindowPosX);
                FwindowPosY = Utilities.GetNodeValue(TSTsettingsNode, "FwindowPosY", FwindowPosY);
                CwindowPosX = Utilities.GetNodeValue(TSTsettingsNode, "CwindowPosX", CwindowPosX);
                CwindowPosY = Utilities.GetNodeValue(TSTsettingsNode, "CwindowPosY", CwindowPosY);
                GalwindowPosX = Utilities.GetNodeValue(TSTsettingsNode, "GalwindowPosX", GalwindowPosX);
                GalwindowPosY = Utilities.GetNodeValue(TSTsettingsNode, "GalwindowPosY", GalwindowPosY);
                BodwindowPosX = Utilities.GetNodeValue(TSTsettingsNode, "BodwindowPosX", BodwindowPosX);
                BodwindowPosY = Utilities.GetNodeValue(TSTsettingsNode, "BodwindowPosY", BodwindowPosY);
                ChemwinSml = Utilities.GetNodeValue(TSTsettingsNode, "ChemwinSml", ChemwinSml);
                ChemwinLge = Utilities.GetNodeValue(TSTsettingsNode, "ChemwinLge", ChemwinLge);
                TelewinSml = Utilities.GetNodeValue(TSTsettingsNode, "TelewinSml", TelewinSml);
                TelewinSml = Utilities.GetNodeValue(TSTsettingsNode, "TelewinSml", TelewinLge);
                UseAppLauncher = Utilities.GetNodeValue(TSTsettingsNode, "UseAppLauncher", UseAppLauncher);
                debugging = Utilities.GetNodeValue(TSTsettingsNode, "debugging", debugging);
                maxChemCamContracts = Utilities.GetNodeValue(TSTsettingsNode, "maxChemCamContracts", maxChemCamContracts);
                photoOnlyChemCamContracts = Utilities.GetNodeValue(TSTsettingsNode, "photoOnlyChemCamContracts", photoOnlyChemCamContracts); 
                this.Log_Debug("TSTSettings load complete");
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
            settingsNode.AddValue("maxChemCamContracts", maxChemCamContracts);
            settingsNode.AddValue("photoOnlyChemCamContracts", photoOnlyChemCamContracts);
            this.Log_Debug("TSTSettings save complete");
        }
    }

    public class TSTGasPlanets
    {
        private const string configNodeName = "TSTGasPlanets";

        public string[] TarsierPlanetOrder{ get; set; }
                
        public TSTGasPlanets()
        {
            //TarsierPlanetOrder = new string[] { };
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTGasPlanetsNode = node.GetNode(configNodeName);
                string tmpPlanetOrderString = Utilities.GetNodeValue(TSTGasPlanetsNode, "planets", string.Empty);
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
            this.Log_Debug("TSTGasPlanets load complete");
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
            this.Log_Debug("TSTGasPlanets save complete");
        }
    }

    public class TSTStockPlanets
    {
        private const string configNodeName = "TSTStockPlanetOrder";

        public string[] StockPlanetOrder { get; set; }

        public TSTStockPlanets()
        {
            //TarsierPlanetOrder = new string[] { };
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                string tmpPlanetOrderString = Utilities.GetNodeValue(TSTStockPlanetOrderNode, "planets", string.Empty);
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
            this.Log_Debug("TSTStockPlanetOrder load complete");
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
            this.Log_Debug("TSTStockPlanetOrder save complete");
        }
    }

    public class TSTRSSPlanets
    {
        private const string configNodeName = "TSTRSSPlanetOrder";

        public string[] RSSPlanetOrder { get; set; }

        public TSTRSSPlanets()
        {
            //TarsierPlanetOrder = new string[] { };
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                string tmpPlanetOrderString = Utilities.GetNodeValue(TSTStockPlanetOrderNode, "planets", string.Empty);
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
            this.Log_Debug("TSTRSSPlanetOrder load complete");
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
            this.Log_Debug("TSTRSSPlanetOrder save complete");
        }
    }

    public class TSTOPMPlanets
    {
        private const string configNodeName = "TSTOPMPlanetOrder";

        public string[] OPMPlanetOrder { get; set; }

        public TSTOPMPlanets()
        {
            //TarsierPlanetOrder = new string[] { };
        }

        public void Load(ConfigNode node)
        {
            if (node.HasNode(configNodeName))
            {
                ConfigNode TSTStockPlanetOrderNode = node.GetNode(configNodeName);
                string tmpPlanetOrderString = Utilities.GetNodeValue(TSTStockPlanetOrderNode, "planets", string.Empty);
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
            this.Log_Debug("TSTOPMPlanetOrder load complete");
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
            this.Log_Debug("TSTOPMPlanetOrder save complete");
        }
    }
}

