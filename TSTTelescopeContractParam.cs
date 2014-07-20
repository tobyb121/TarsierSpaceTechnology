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

        public CelestialBody target;

        private void OnTelescopeScience(CelestialBody lookingAt)
        {
            if (target.name == lookingAt.name)
            {
                SetComplete();
            }
        }
    }
}
