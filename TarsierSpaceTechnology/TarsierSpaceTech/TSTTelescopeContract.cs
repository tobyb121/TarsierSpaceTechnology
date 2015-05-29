/*
 * TSTTelescopeContract.cs
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
            this.Log_Debug("Telescope Contracts check offers=" + offers + " active=" + active);
            if (offers >= 1)
                return false;
            if (active >= 1)
                return false;            
            this.Log_Debug("Generating Telescope Contract");

            agent = Contracts.Agents.AgentList.Instance.GetAgent("Tarsier Space Technology");
            base.SetExpiry();
            base.expiryType = DeadlineType.None;
            base.deadlineType = DeadlineType.None;
            
            this.Log_Debug("Creating Parameter");
            TSTTelescopeContractParam param = new TSTTelescopeContractParam();
            this.AddParameter(param);
            string target_name = TSTProgressTracker.GetNextTelescopeTarget();
            this.Log_Debug("Target: "+target_name);
            AvailablePart ap2 = PartLoader.getPartInfoByName("tarsierAdvSpaceTelescope");
            if(!ResearchAndDevelopment.PartTechAvailable(ap2) && !ResearchAndDevelopment.PartModelPurchased(ap2) && target_name == "Galaxy1")                
            {
                this.Log_Debug("Contracts for Planets completed and Galaxy contracts require advanced space telescope");
                return false;
            }
            this.Log_Debug("Checking Celestial Bodies");
            this.target = FlightGlobals.Bodies.Find(b => b.name == target_name);
            if (target == null)
            {
                this.Log_Debug("Checking Galaxies");
                this.target = TSTGalaxies.Galaxies.Find(g => g.name == target_name);
            }
            this.Log_Debug("Using target: " + this.target.ToString());
            param.target = target;
            this.Log_Debug("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.SpaceTelescope");
            param2.matchFields.Add("LookingAt" + target.name);
            this.AddParameter(param2);
            this.Log_Debug("Created Science Param");
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
