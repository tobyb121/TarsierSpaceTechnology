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
            Utils.print("Starting Tarsier Progress Tracking");
            Instance = this;
        }

        int i = 0;

        public void Update()
        {
        }

        public delegate void TelescopeListener(CelestialBody body);
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

        public static void OnTelescopePicture(CelestialBody body)
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

        public static void setTelescopeContractComplete(CelestialBody body)
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
            "Pol"
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
            ConfigNode telescopeNode = node.GetNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.GetNode("TarsierChemCam");
            if (telescopeNode != null)
            {
                foreach (CelestialBody b in FlightGlobals.Bodies)
                    TelescopeData[b.name] = (telescopeNode.GetValue(b.name) == "true");
            }
            else
            {
                foreach (CelestialBody b in FlightGlobals.Bodies)
                    TelescopeData[b.name] = false;
            }

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
            ConfigNode telescopeNode = node.AddNode("TarsierSpaceTelescope");
            ConfigNode chemCamNode = node.AddNode("TarsierChemCam");
            foreach (string key in TelescopeData.Keys)
                telescopeNode.AddValue(key, TelescopeData[key]?"true":"false");
            foreach (string key in TelescopeData.Keys)
                chemCamNode.AddValue(key, ChemCamData[key]?"true":"false");
        }
    }
}
