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

        private CelestialBody target
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
            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;

            TSTTelescopeContractParam param = new TSTTelescopeContractParam();
            AddParameter(param);
            target = FlightGlobals.Bodies.Find(b => b.name == TSTProgressTracker.GetNextTelescopeTarget());
            param.target = target;
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.SpaceTelescope");
            param2.matchFields.Add("LookingAt" + target.name);
            AddParameter(param2);
            prestige=TSTProgressTracker.getTelescopePrestige(target.name);
            if (TSTProgressTracker.HasTelescopeCompleted(target))
            {
                SetFunds(10, 15, target);
                SetReputation(5, target);
                SetReputation(5, target);
            }
            else
            {
                SetScience(30, target);
                SetFunds(75, 150, target);
                SetReputation(20, target);
            }
            return true;
        }
    }
}
