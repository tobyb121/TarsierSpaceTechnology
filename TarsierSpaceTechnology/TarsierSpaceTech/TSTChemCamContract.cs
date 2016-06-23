/*
 * TSTChemCamContract.cs
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
using Contracts;
using Contracts.Agents;
using RSTUtils;

namespace TarsierSpaceTech
{
    internal class TSTChemCamContract : Contract
    {
        public override bool CanBeCancelled()
        {
            return true;
        }

        public override bool CanBeDeclined()
        {
            return true;
        }

        protected override string GetHashString()
        {
            return "TST_chemcam_" + target.name + biome;
        }

        protected override string GetDescription()
        {
            return TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(), "ChemCam", target.name, "test", MissionSeed);
        }

        protected override string GetTitle()
        {
            return "Analyse the surface composition of " + target.theName;
        }

        protected override string MessageCompleted()
        {
            return "The data has been collected, please send it back for analysis";
        }

        public override bool MeetRequirements()
        {
            AvailablePart ap1 = PartLoader.getPartInfoByName("tarsierChemCam");
            return ResearchAndDevelopment.PartTechAvailable(ap1);
        }

        protected override string GetSynopsys()
        {
            if (biome != "")
            {
                return "Use the ChemCam to analyse the surface composition of the " + biome + " on " + target.theName;
            }
            return "Use the ChemCam to analyse the surface of " + target.theName;
        }

        protected override void OnCompleted()
        {
            string targetname = target.name;
            if (biome != "")
                targetname += "," + biome;

            TSTProgressTracker.setChemCamContractComplete(targetname);
        }

        private CelestialBody target;

        private string biome = "";

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("target", target.name);
            node.AddValue("biome", biome);
        }

        protected override void OnLoad(ConfigNode node)
        {
            string targetName = node.GetValue("target");
            target = FlightGlobals.Bodies.Find(b => b.name == targetName);
            biome = node.GetValue("biome");
        }
        private int offers = 0;
        private int active = 0;
        private TSTChemCamContract countActivecontracts;
        
        protected override bool Generate()
        {
            TSTChemCamContract[] TSTChemCamContracts = ContractSystem.Instance.GetCurrentContracts<TSTChemCamContract>();
            offers = 0;
            active = 0;
            for (int i = 0; i < TSTChemCamContracts.Length; i++)
            {
                countActivecontracts = TSTChemCamContracts[i];
                if (countActivecontracts.ContractState == State.Offered)
                    offers++;
                else if (countActivecontracts.ContractState == State.Active)
                    active++;
            }
            Utilities.Log_Debug("ChemCam Contracts check offers={0}, active={1}" , offers.ToString(), active.ToString());
            if (offers >= TSTMstStgs.Instance.TSTsettings.maxChemCamContracts)
                return false;
            if (active >= TSTMstStgs.Instance.TSTsettings.maxChemCamContracts)
                return false;

            Utilities.Log_Debug("Generating ChemCam Contract");
            agent = AgentList.Instance.GetAgent("Tarsier Space Technology");
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;
            Random r = new Random(MissionSeed);

            //If we only want Bodies that have already been PhotoGraphed by a Telescope
            if (TSTMstStgs.Instance.TSTsettings.photoOnlyChemCamContracts)  
            {
                TSTTelescopeContract[] TSTTelescopeContractsCompleted = ContractSystem.Instance.GetCompletedContracts<TSTTelescopeContract>();
                List<CelestialBody> availTelescopeBodies = new List<CelestialBody>();
                for (int i = 0; i < TSTTelescopeContractsCompleted.Length; i++)
                {
                    if (TSTTelescopeContractsCompleted[i].target.type == typeof(CelestialBody))  //We only want Bodies, not Galaxies
                    {
                        CelestialBody contractBody = (CelestialBody)TSTTelescopeContractsCompleted[i].target.BaseObject;
                        availTelescopeBodies.Add(contractBody);
                    }
                }
                IEnumerable<CelestialBody> availableBodies = availTelescopeBodies.ToArray()
                    //.Where(b => !TSTMstStgs.Instance.TSTgasplanets.TarsierPlanetOrder.Contains(b.name) && b.Radius > 100 && b.pqsController != null);  //Exclude the GasPlanets
                    .Where(b => b.Radius > 100 && b.pqsController != null);  //Exclude the GasPlanets & Sigma Binaries
                if (!availableBodies.Any())
                {
                    Utilities.Log_Debug("There are no Bodies that have been photographed, cannot generate ChemCam Contract at this time");
                    return false;
                }
                target = availableBodies.ElementAt(r.Next(availableBodies.Count() - 1));
            }
            else  //We can use any Bodies
            {
                IEnumerable<CelestialBody> availableBodies = FlightGlobals.Bodies
                    //.Where(b => !TSTMstStgs.Instance.TSTgasplanets.TarsierPlanetOrder.Contains(b.name) && b.Radius > 100 && b.pqsController != null); //Exclude the GasPlanets
                    .Where(b => b.Radius > 100 && b.pqsController != null);  //Exclude the GasPlanets & Sigma Binaries
                if (!availableBodies.Any())
                {
                    Utilities.Log_Debug("There are no Bodies that have been photographed, cannot generate ChemCam Contract at this time");
                    return false;
                }
                target = availableBodies.ElementAt(r.Next(availableBodies.Count() - 1));
            }

            // if ResearchBodies is installed we need to check if the target body has been found. If it has not, then we set the target to default so a contract is not generated at this time.
            if (TSTMstStgs.Instance.isRBactive)
            {
                try
                {
                    if (RBWrapper.APISCReady)
                    {                        
                        List<KeyValuePair<CelestialBody, bool>> trackbodyentry = TSTMstStgs.Instance.TrackedBodies.Where(e => e.Key == target).ToList();
                        if (trackbodyentry.Count != 1)
                        {
                            Utilities.Log("ChemCam Contract cannot find target in ResearchBodies TrackedBodies {0}" , target.name);
                            return false;
                        }
                        if (trackbodyentry[0].Value == false)
                        {
                            Utilities.Log("ChemCam Contract target in ResearchBodies TrackedBodies is still not tracked {0}" , target.name);
                            return false;
                        }
                    }
                    else
                    {
                        Utilities.Log("ResearchBodies is not Ready, cannot test ChemCam target for contract generation at this time");
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Utilities.Log("Checking ResearchBodies status for target {0} Failed unexpectedly. Ex: {1}" , target.name , ex.Message);
                }
            }

            Utilities.Log_Debug("Target: {0}" , target.name);
            Utilities.Log_Debug("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.ChemCam");
            param2.matchFields.Add(target.name);
            biome = "";
            List<string> biomes = ResearchAndDevelopment.GetBiomeTags(target);
            if (biomes.Count > 1)
            {
                do
                {
                    biome = biomes[r.Next(biomes.Count - 1)];
                } while (biome.Contains("Water"));
                param2.matchFields.Add(biome);
            }
            AddParameter(param2);
            ContractPrestige p = TSTProgressTracker.getChemCamPrestige(target); //Get the target prestige level
            if (p != prestige)  //If the prestige is not the required level don't generate.
                return false;
            string targetname = target.name;
            if (biome != "")
                    targetname += "," + biome;
            if (TSTProgressTracker.HasChemCamCompleted(targetname))
            {
                SetFunds(TSTMstStgs.Instance.TSTsettings.fundsdiscoveredChem * 0.75f, TSTMstStgs.Instance.TSTsettings.fundsdiscoveredChem, target);
                SetReputation(TSTMstStgs.Instance.TSTsettings.repDiscoveredChem, target);
                SetScience(TSTMstStgs.Instance.TSTsettings.scienceDiscoveredChem, target);
            }
            else
            {
                SetFunds(TSTMstStgs.Instance.TSTsettings.fundsUndiscoveredChem * 0.75f, TSTMstStgs.Instance.TSTsettings.fundsUndiscoveredChem, target);
                SetReputation(TSTMstStgs.Instance.TSTsettings.repUndiscoveredChem, target);
                SetScience(TSTMstStgs.Instance.TSTsettings.scienceUndiscoveredChem, target);
            }
            if (new Random(MissionSeed).Next(10) > 3)
            {
                Utilities.Log_Debug("Random Seed False, not generating contract");
                return false;
            }
            Utilities.Log_Debug("Random Seed True, generating contract");
            return true;
        }
    }
}