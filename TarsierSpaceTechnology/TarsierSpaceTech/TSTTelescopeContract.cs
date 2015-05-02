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

        public TSTSpaceTelescope.TargetableObject target
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
            TSTTelescopeContract[] TSTTelescopeContracts = ContractSystem.Instance.GetCurrentContracts<TSTTelescopeContract>();
            int offers = 0;
            int active = 0;

            for (int i = 0; i < TSTTelescopeContracts.Length; i++)
            {
                TSTTelescopeContract m = TSTTelescopeContracts[i];
                if (m.ContractState == State.Offered)
                    offers++;
                else if (m.ContractState == State.Active)
                    active++;
            }
            Utils.print("Telescope Contracts check offers=" + offers + " active=" + active);
            if (offers >= 1)
                return false;
            if (active >= 1)
                return false;            
            Utils.print("Generating Telescope Contract");

            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            base.SetExpiry();
            base.expiryType = DeadlineType.None;
            base.deadlineType = DeadlineType.None;
            
            Utils.print("Creating Parameter");
            TSTTelescopeContractParam param = new TSTTelescopeContractParam();
            this.AddParameter(param);
            string target_name = TSTProgressTracker.GetNextTelescopeTarget();
            Utils.print("Target: "+target_name);
            AvailablePart ap2 = PartLoader.getPartInfoByName("tarsierAdvSpaceTelescope");
            if(!ResearchAndDevelopment.PartTechAvailable(ap2) && !ResearchAndDevelopment.PartModelPurchased(ap2) && target_name == "Galaxy1")                
            {
                Utils.print("Contracts for Planets completed and Galaxy contracts require advanced space telescope");
                return false;
            }
            Utils.print("Checking Celestial Bodies");
            this.target = FlightGlobals.Bodies.Find(b => b.name == target_name);
            if (target == null)
            {
                Utils.print("Checking Galaxies");
                this.target = TSTGalaxies.Galaxies.Find(g => g.name == target_name);
            }
            Utils.print("Using target: " + this.target.ToString());
            param.target = target;
            Utils.print("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.SpaceTelescope");
            param2.matchFields.Add("LookingAt" + target.name);
            this.AddParameter(param2);
            Utils.print("Created Science Param");
            base.prestige=TSTProgressTracker.getTelescopePrestige(target.name);
            if (TSTProgressTracker.HasTelescopeCompleted(target))
            {
                base.SetFunds(10, 15, target.type==typeof(TSTGalaxy)?null:(CelestialBody)target.BaseObject);
                base.SetReputation(5, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                base.SetReputation(5, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            else
            {
                base.SetScience(30, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                base.SetFunds(75, 150, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                base.SetReputation(20, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            return true;
        }
    }
}
