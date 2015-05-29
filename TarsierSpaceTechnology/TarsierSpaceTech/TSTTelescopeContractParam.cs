/*
 * TSTTelescopeContractParm.cs
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
    class TSTTelescopeContractParam:ContractParameter
    {
        protected override string GetTitle()
        {
            return "Take a picture of " + target.theName + " using a Space Telescope";
        }

        protected override string GetNotes()
        {
            return "";
        }

        protected override void OnRegister()
        {
            TSTProgressTracker.AddTelescopeListener(OnTelescopeScience);
        }

        protected override void OnUnregister()
        {
            TSTProgressTracker.RemoveTelescopeListener(OnTelescopeScience);
        }

        protected override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("target"))
            {
                string t = node.GetValue("target");
                target = FlightGlobals.Bodies.Find(b => b.name == t);
                if (target == null)
                {
                    this.Log_Debug("Checking Galaxies");
                    target = TSTGalaxies.Galaxies.Find(g => g.name == t);
                }
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            node.AddValue("target", target.name);
        }

        protected override string GetHashString()
        {
            return "TSTParam.Telescope."+target.name;
        }

        public TSTSpaceTelescope.TargetableObject target;

        private void OnTelescopeScience(TSTSpaceTelescope.TargetableObject lookingAt)
        {
            if (target.name == lookingAt.name)
            {
                SetComplete();
            }
        }
    }
}
