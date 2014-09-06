using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using Contracts;

namespace TarsierSpaceTech
{
    class TSTTelescopeContract : Contracts.Contract
    {
        public override bool CanBeCancelled()
        {
            return false;
        }

        public override bool CanBeDeclined()
        {
            return false;
        }

        protected override string GetHashString()
        {
            return "TST_telescope_"+target;
        }

        protected override string GetDescription()
        {
            return Contracts.TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(), "Space Telescope", target.name, "test", MissionSeed);
        }

        protected override string GetTitle()
        {
            return "Take a picture of "+target.theName;
        }

        protected override string MessageCompleted()
        {
            return "Great picture";
        }

        public override bool MeetRequirements()
        {
            AvailablePart ap1 = PartLoader.getPartInfoByName("tarsierSpaceTelescope");
            AvailablePart ap2 = PartLoader.getPartInfoByName("tarsierAdvSpaceTelescope");
            return ResearchAndDevelopment.PartTechAvailable(ap1)||ResearchAndDevelopment.PartTechAvailable(ap2);
        }

        protected override string GetSynopsys()
        {
            
            return "Use a space telescope to take a picture of "+target.theName;
        }

        private TSTSpaceTelescope.TargetableObject target
        {
            get
            {
                return GetParameter<TSTTelescopeContractParam>().target;
            }

            set
            {
                GetParameter<TSTTelescopeContractParam>().target = value;
            }
        }

        protected override void OnCompleted()
        {
            TSTProgressTracker.setTelescopeContractComplete(target);
        }

        protected override bool Generate()
        {
            Utils.print("Generating Telescope Contract");

            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;

            Utils.print("Creating Parameter");
            TSTTelescopeContractParam param = new TSTTelescopeContractParam();
            AddParameter(param);
            string target_name = TSTProgressTracker.GetNextTelescopeTarget();
            Utils.print("Target: "+target_name);
            Utils.print("Checking Celestial Bodies");
            target = FlightGlobals.Bodies.Find(b => b.name == target_name);
            if (target == null)
            {
                Utils.print("Checking Galaxies");
                target = TSTGalaxies.Galaxies.Find(g => g.name == target_name);
            }
            Utils.print("Using target: " + target.ToString());
            param.target = target;
            Utils.print("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.SpaceTelescope");
            param2.matchFields.Add("LookingAt" + target.name);
            AddParameter(param2);
            prestige=TSTProgressTracker.getTelescopePrestige(target.name);
            if (TSTProgressTracker.HasTelescopeCompleted(target))
            {
                SetFunds(10, 15, target.type==typeof(TSTGalaxy)?null:(CelestialBody)target.BaseObject);
                SetReputation(5, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetReputation(5, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            else
            {
                SetScience(30, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetFunds(75, 150, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetReputation(20, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            return true;
        }
    }
}
