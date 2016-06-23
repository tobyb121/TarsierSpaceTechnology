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

using System.Linq;
using Contracts;
using Contracts.Agents;
using RSTUtils;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TSTTelescopeContract : Contract
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
            return TextGen.GenerateBackStories(agent.Name, agent.GetMindsetString(), "Space Telescope", target.name, "test", MissionSeed);
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
            Utilities.Log_Debug("Telescope Contracts check offers= {0} active= {1}" , offers.ToString(), active.ToString());
            if (offers >= 1)
                return false;
            if (active >= 1)
                return false;
            Utilities.Log_Debug("Generating Telescope Contract");

            agent = AgentList.Instance.GetAgent("Tarsier Space Technology");
            SetExpiry();
            expiryType = DeadlineType.None;
            deadlineType = DeadlineType.None;

            Utilities.Log_Debug("Creating Parameter");
            TSTTelescopeContractParam param = new TSTTelescopeContractParam();
            AddParameter(param);
            string target_name = TSTProgressTracker.GetNextTelescopeTarget();
            if (target_name == default(string))
            {
                Utilities.Log_Debug("target body is default (not set), cannot generate");
                return false;
            }
            Utilities.Log_Debug("Target: {0}" , target_name);
            AvailablePart ap2 = PartLoader.getPartInfoByName("tarsierAdvSpaceTelescope");
            if (!ResearchAndDevelopment.PartTechAvailable(ap2) && !ResearchAndDevelopment.PartModelPurchased(ap2) && target_name == "Galaxy1")
            {
                Utilities.Log_Debug("Contracts for Planets completed and Galaxy contracts require advanced space telescope");
                return false;
            }
            Utilities.Log_Debug("Checking Celestial Bodies");
            target = FlightGlobals.Bodies.Find(b => b.name == target_name);
            if (target == null)
            {
                Utilities.Log_Debug("Checking Galaxies");
                target = TSTGalaxies.Galaxies.Find(g => g.name == target_name);
            }
            Utilities.Log_Debug("Using target: {0}" , target.ToString());
            param.target = target;
            Utilities.Log_Debug("Creating Science Param");
            TSTScienceParam param2 = new TSTScienceParam();
            param2.matchFields.Add("TarsierSpaceTech.SpaceTelescope");
            param2.matchFields.Add("LookingAt" + target.name);
            AddParameter(param2);
            Utilities.Log_Debug("Created Science Param");
            prestige = TSTProgressTracker.getTelescopePrestige(target);
            if (TSTProgressTracker.HasTelescopeCompleted(target))
            {
                SetScience(TSTMstStgs.Instance.TSTsettings.scienceDiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetFunds(TSTMstStgs.Instance.TSTsettings.fundsdiscoveredScope * 0.75f, TSTMstStgs.Instance.TSTsettings.fundsdiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetReputation(TSTMstStgs.Instance.TSTsettings.repDiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            else
            {
                SetScience(TSTMstStgs.Instance.TSTsettings.scienceUndiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetFunds(TSTMstStgs.Instance.TSTsettings.fundsUndiscoveredScope * 0.75f, TSTMstStgs.Instance.TSTsettings.fundsUndiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
                SetReputation(TSTMstStgs.Instance.TSTsettings.repUndiscoveredScope, target.type == typeof(TSTGalaxy) ? null : (CelestialBody)target.BaseObject);
            }
            return true;
        }

        //We would activate this if we only want to block the ProgressTracker for a Contract event only. See the TSTScienceProgressionblocker.cs file.
        //protected override void AwardCompletion()
        //{
        //    TSTScienceProgressionBlocker.BlockSingleEvent();
        //    base.AwardCompletion();
        //}
    }
}
