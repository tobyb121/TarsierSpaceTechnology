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
using UniLinq;
using Contracts;
using Contracts.Agents;
using RSTUtils;
using KSP.Localization;

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
            return TextGen.GenerateBackStories(Localizer.Format("#autoLOC_TST_0052"), agent.Name, Localizer.Format("#autoLOC_TST_0053") , target.name, MissionSeed, true, true, true); //#autoLOC_TST_0052 = Exploration # autoLOC_TST_0053 = ChemCam 
        }

        protected override string GetTitle()
        {
            return Localizer.Format("#autoLOC_TST_0054", target.displayName); //#autoLOC_TST_0054 = Analyse the surface composition of <<1>>
        }

        protected override string MessageCompleted()
        {
            return Localizer.Format("#autoLOC_TST_0055"); //#autoLOC_TST_0055 = The data has been collected, please send it back for analysis
        }

        public override bool MeetRequirements()
        {
            AvailablePart ap1 = PartLoader.getPartInfoByName("tarsierChemCam");
            if (ap1 != null)
            {
                return ResearchAndDevelopment.PartTechAvailable(ap1);
            }
            Utilities.Log("It appears the TST ChemCam part is missing. Cannot check Contract Requirements");
            return false;
        }

        protected override string GetSynopsys()
        {
            if (biome != "")
            {
                string biomedisplayName = ScienceUtil.GetBiomedisplayName(target, biome);
                return Localizer.Format("#autoLOC_TST_0056", biomedisplayName, target.displayName); //#autoLOC_TST_0056 = Use the ChemCam to analyse the surface composition of the <<1>> on <<2>>
            }
            return Localizer.Format("#autoLOC_TST_0057", target.displayName); //#autoLOC_TST_0057 = Use the ChemCam to analyse the surface of <<1>>
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
                    .Where(b => b.Radius > 100 && b.pqsController != null && b.hasSolidSurface);  //Exclude the GasPlanets & Sigma Binaries
                if (!availableBodies.Any())
                {
                    Utilities.Log_Debug("There are no Bodies that have been photographed, cannot generate ChemCam Contract at this time");
                    return false;
                }
                target = availableBodies.ElementAt(r.Next(availableBodies.Count() - 1));
            }

            // if ResearchBodies is installed we need to check if the target body has been found. If it has not, then we set the target to default so a contract is not generated at this time.
            if (TSTMstStgs.Instance.isRBactive && RBWrapper.RBactualAPI.enabled)
            {
                try
                {
                    if (RBWrapper.APIRBReady)
                    {                        
                        List<KeyValuePair<CelestialBody, RBWrapper.CelestialBodyInfo>> trackbodyentry = TSTMstStgs.Instance.RBCelestialBodies.Where(e => e.Key.name == target.name).ToList();
                        if (trackbodyentry.Count != 1)
                        {
                            Utilities.Log("ChemCam Contract cannot find target in ResearchBodies TrackedBodies {0}" , target.name);
                            return false;
                        }
                        if (trackbodyentry[0].Value.isResearched == false)
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
            List<string> biomes = ResearchAndDevelopment.GetBiomeTags(target, true);
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