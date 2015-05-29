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
using System.Text;

using UnityEngine;
using Contracts;

namespace TarsierSpaceTech
{
    class TSTChemCamContract : Contracts.Contract
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
            return "TST_chemcam_"+target.name+biome;
        }

        protected override string GetDescription()
        {
            return Contracts.TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(), "ChemCam", target.name, "test", MissionSeed);
        }

        protected override string GetTitle()
        {
            return "Analyse the surface composition of "+target.theName;
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
                return "Use the ChemCam to analyse the surface composition of the "+biome+" on " + target.theName;
            }
            return "Use the ChemCam to analyse the surface of "+target.theName;
        }

        protected override void OnCompleted()
        {
            TSTProgressTracker.setChemCamContractComplete(target);
        }

        CelestialBody target;

        string biome="";

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

        protected override bool Generate()
        {
            this.Log_Debug("Generating ChemCam Contract");
            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;            
            System.Random r = new System.Random(MissionSeed);
            IEnumerable<CelestialBody> availableBodies=FlightGlobals.Bodies.Where(b=>b.name!="Sun" && b.name!="Jool");
            target = availableBodies.ElementAt(r.Next(availableBodies.Count() - 1));
            this.Log_Debug("Target: " + target.name);
            this.Log_Debug("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.ChemCam");
            param2.matchFields.Add(target.name);
            List<string> biomes=ResearchAndDevelopment.GetBiomeTags(target);
            if (biomes.Count() > 1)
            {
                biome = biomes[r.Next(biomes.Count - 1)];
                param2.matchFields.Add(biome);
            }
            AddParameter(param2);
            ContractPrestige p = TSTProgressTracker.getChemCamPrestige(target);
            if (p != base.prestige)
                return false;
            SetFunds(300, 400,target);
            SetReputation(35,target);
            SetScience(30,target);
            if (new System.Random(MissionSeed).Next(10) > 3)
            {
                this.Log_Debug("Random Seed False, not generating contract");
                return false;
            }
            this.Log_Debug("Random Seed True, generating contract");    
            return true;
        }
    }
}
