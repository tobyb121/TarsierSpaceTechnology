using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Contracts;

namespace TarsierSpaceTech
{
    class TSTScienceParam : ContractParameter
    {
        protected override string GetTitle()
        {
            return "Transmit or Recover the science data";
        }

        protected override string GetNotes()
        {
            return "";
        }

        protected override void OnRegister()
        {
            Utils.print("Adding Callback for science data received on contract");
            //GameEvents.OnScienceRecieved.Add(new EventData<float,ScienceSubject,ProtoVessel>.OnEvent(OnScienceData));
            GameEvents.OnScienceRecieved.Add(OnScienceData);            
            
        }

        protected override void OnUnregister()
        {
            Utils.print("Removing Callback for science data received on contract");
            //GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject,ProtoVessel>.OnEvent(OnScienceData));
            GameEvents.OnScienceRecieved.Remove(OnScienceData);
            
        }

        protected override string GetHashString()
        {
            return "TSTParam.Science";
        }

        protected override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("FIELD"))
            {
                matchFields.AddRange(node.GetValues("FIELD"));
            }
        }

        protected override void OnSave(ConfigNode node)
        {
            foreach (string field in matchFields)
            {
                node.AddValue("FIELD", field);
            }
        }

        public List<string> matchFields = new List<string>();

        private void OnScienceData(float amount, ScienceSubject subject, ProtoVessel vessel)        
        {
            Utils.print("Received Science Data from " + vessel.vesselName + " subject=" + subject.id);
            bool match=true;
            foreach (string f in matchFields)
            {
                Utils.print("matchFields=" + f);
                match &= subject.HasPartialIDstring(f);
            }
            Utils.print("Match result?=" + match.ToString());
            if (match)
            {
                base.SetComplete();
            }
        }
    }
}
