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
            GameEvents.OnScienceRecieved.Add(new EventData<float,ScienceSubject>.OnEvent(OnScienceData));
        }

        protected override void OnUnregister()
        {
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject>.OnEvent(OnScienceData));
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

        private void OnScienceData(float amount, ScienceSubject subject)
        {
            Utils.print(subject.id);
            bool match=true;
            foreach (string f in matchFields)
            {
                match &= subject.HasPartialIDstring(f);
            }
            if (match)
            {
                SetComplete();
            }
        }
    }
}
