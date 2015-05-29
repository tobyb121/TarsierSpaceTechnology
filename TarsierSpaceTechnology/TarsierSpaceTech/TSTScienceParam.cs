/*
 * TSTScienceParam.cs
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
            this.Log_Debug("Adding Callback for science data received on contract");
            GameEvents.OnScienceRecieved.Add(new EventData<float,ScienceSubject,ProtoVessel,bool>.OnEvent(OnScienceData));
            //GameEvents.OnScienceRecieved.Add(OnScienceData);            
            
        }

        protected override void OnUnregister()
        {
            this.Log_Debug("Removing Callback for science data received on contract");
            GameEvents.OnScienceRecieved.Remove(new EventData<float, ScienceSubject,ProtoVessel,bool>.OnEvent(OnScienceData));
            //GameEvents.OnScienceRecieved.Remove(OnScienceData);
            
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

        private void OnScienceData(float amount, ScienceSubject subject, ProtoVessel vessel, bool notsure)        
        {
            this.Log_Debug("Received Science Data from " + vessel.vesselName + " subject=" + subject.id + " amount=" + amount.ToString("000.00") + " bool=" + notsure);
            bool match=true;
            foreach (string f in matchFields)
            {
                this.Log_Debug("matchFields=" + f);
                match &= subject.HasPartialIDstring(f);
            }
            this.Log_Debug("Match result?=" + match.ToString());
            if (match)
            {
                base.SetComplete();
            }
        }
    }
}
