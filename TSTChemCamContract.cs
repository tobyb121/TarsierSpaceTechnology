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
            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;

            System.Random r = new System.Random(MissionSeed);
            IEnumerable<CelestialBody> availableBodies=FlightGlobals.Bodies.Where(b=>b.name!="Sun"&&b.name!="Jool");
            target = availableBodies.ElementAt(r.Next(availableBodies.Count() - 1));
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
            if (new System.Random(MissionSeed).Next(10) > 4)
                return false;
            return true;
        }
    }
}
