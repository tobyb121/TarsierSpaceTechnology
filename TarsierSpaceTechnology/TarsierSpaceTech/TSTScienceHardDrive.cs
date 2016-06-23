/*
 * TSTScienceHardDrive.cs
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

using System.Collections.Generic;
using System.Linq;
using KSP.UI.Screens.Flight.Dialogs;
using RSTUtils;
using UnityEngine;

namespace TarsierSpaceTech
{
    class TSTScienceHardDrive : PartModule, IScienceDataContainer
    {
        public List<ScienceData> scienceData = new List<ScienceData>();       
                
        [KSPField(guiActive = true, guiName = "Capacity", isPersistant = true, guiUnits = " Mits")]
        public float Capacity = 50f;

        [KSPField(guiActive = false, isPersistant = true)]
        public float corruption = 0.2f;

        [KSPField(guiActive = false, isPersistant = true)]
        public float powerUsage = 0.5f;

        [KSPField(guiActive = false, isPersistant = false)]
        public bool fillFromEVA = false;

        [KSPField(guiActive = false, isPersistant = false)]
        public float EVARange = 1.2f;

        private float _dataAmount;
        private float _DataAmount
        {
            get
            {
                return _dataAmount;
            }
            set
            {
                _dataAmount = value;
                PercentageFull = Mathf.Round(1000 * _dataAmount / Capacity) / 10;
            }
        }

        [KSPField(guiActive = true, guiName = "Percentage Full", isPersistant = false, guiUnits = " %")]
        public float PercentageFull;

        public override void OnStart(StartState state)
        {
            Events["fillDrive"].guiActiveUnfocused = fillFromEVA;
            Events["fillDrive"].unfocusedRange = EVARange;            
        }

        [KSPEvent(name = "fillDrive", active = true, guiActive = true, externalToEVAOnly = false, guiName = "Move All Science to Drive")]
        public void fillDrive()
        {
            Utilities.Log_Debug("Filling drive with all the juicy science");

            //List<Part> parts = vessel.Parts.Where(p => p.FindModulesImplementing<IScienceDataContainer>().Count > 0).ToList();
            List<Part> parts = FlightGlobals.ActiveVessel.Parts.Where(p => p.FindModulesImplementing<IScienceDataContainer>().Count > 0).ToList();
            parts.RemoveAll(p => p.FindModulesImplementing<TSTScienceHardDrive>().Count > 0);
            Utilities.Log_Debug("Parts= {0}" , parts.Count.ToString());
            foreach (Part p in parts)
            {
                List<IScienceDataContainer> containers = p.FindModulesImplementing<IScienceDataContainer>().ToList();
                Utilities.Log_Debug("Got containers: {0}" , containers.Count.ToString());
                foreach (IScienceDataContainer container in containers)
                {
                    Utilities.Log_Debug("Checking Data");
                    ScienceData[] data = container.GetData();
                    Utilities.Log_Debug("Got Data: {0}" , data.Length.ToString());
                    foreach (ScienceData d in data)
                    {
                        if (d != null)
                        {
                            Utilities.Log_Debug("Checking Space: {0} : {1} : {2}" , d.dataAmount.ToString() , _dataAmount.ToString() , Capacity.ToString());
                            if (d.dataAmount + _dataAmount <= Capacity)
                            {
                                if (Utilities.GetAvailableResource(part, "ElectricCharge") >= d.dataAmount * powerUsage)
                                {
                                    Utilities.Log_Debug("Removing Electric Charge");
                                    part.RequestResource("ElectricCharge", d.dataAmount * powerUsage);
                                    Utilities.Log_Debug("Adding Data");
                                    scienceData.Add(d);
                                    d.dataAmount *= (1 - corruption);
                                    Utilities.Log_Debug("Incrementing stored val");
                                    _DataAmount += d.dataAmount;
                                    Utilities.Log_Debug("Removing Data from source");
                                    container.DumpData(d);
                                    Utilities.Log_Debug("Data Added");
                                    ScreenMessages.PostScreenMessage("Moved " + d.title + " to TST Drive", 10f, ScreenMessageStyle.UPPER_LEFT);
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage("Required " + (d.dataAmount * powerUsage).ToString("00.00") + " ElectricCharge not available to store data" , 10f, ScreenMessageStyle.UPPER_CENTER);
                                }
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Not enough storage capacity to store data", 10f, ScreenMessageStyle.UPPER_CENTER);
                            }
                        }
                    }
                }
            }
            Events["reviewScience"].guiActive = scienceData.Count > 0;
        }

        [KSPAction("fillDrive")]
        public void ActivateAction(KSPActionParam param)
        {
            fillDrive();
        }

        [KSPEvent(name = "expfillDrive", active = true, guiActive = true, externalToEVAOnly = false, guiName = "Move Experiments to Drive")]
        public void expfillDrive()
        {
            Utilities.Log_Debug("Filling drive with non ScienceConverter and Command parts (where crewcapacity > 0) science only");

            //List<Part> parts = vessel.Parts.Where(p => p.FindModulesImplementing<IScienceDataContainer>().Count > 0).ToList();
            List<Part> parts = FlightGlobals.ActiveVessel.Parts.Where(p => p.FindModulesImplementing<IScienceDataContainer>().Count > 0).ToList();
            parts.RemoveAll(p => p.FindModulesImplementing<TSTScienceHardDrive>().Count > 0);
            parts.RemoveAll(p => p.FindModulesImplementing<ModuleScienceConverter>().Count > 0);
            parts.RemoveAll(p => p.FindModulesImplementing<ModuleCommand>().Count > 0 && p.CrewCapacity > 0);
            Utilities.Log_Debug("Parts= {0}", parts.Count.ToString());
            foreach (Part p in parts)
            {
                List<IScienceDataContainer> containers = p.FindModulesImplementing<IScienceDataContainer>().ToList();
                Utilities.Log_Debug("Got experiments: {0}", containers.Count.ToString());
                foreach (IScienceDataContainer container in containers)
                {
                    Utilities.Log_Debug("Checking Data");
                    ScienceData[] data = container.GetData();
                    Utilities.Log_Debug("Got Data: {0}", data.Length.ToString());
                    foreach (ScienceData d in data)
                    {
                        if (d != null)
                        {
                            Utilities.Log_Debug("Checking Space: {0} : {1} : {2}", d.dataAmount.ToString(), _dataAmount.ToString(), Capacity.ToString());
                            if (d.dataAmount + _dataAmount <= Capacity)
                            {
                                if (Utilities.GetAvailableResource(part, "ElectricCharge") >= d.dataAmount * powerUsage)
                                {
                                    Utilities.Log_Debug("Removing Electric Charge");
                                    part.RequestResource("ElectricCharge", d.dataAmount * powerUsage);
                                    Utilities.Log_Debug("Adding Data");
                                    scienceData.Add(d);
                                    d.dataAmount *= (1 - corruption);
                                    Utilities.Log_Debug("Incrementing stored val");
                                    _DataAmount += d.dataAmount;
                                    Utilities.Log_Debug("Removing Data from source");
                                    container.DumpData(d);
                                    Utilities.Log_Debug("Data Added");
                                    ScreenMessages.PostScreenMessage("Moved " + d.title + " to TST Drive", 10f, ScreenMessageStyle.UPPER_LEFT);
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage("Required " + (d.dataAmount * powerUsage).ToString("00.00") + " ElectricCharge not available to store data", 10f, ScreenMessageStyle.UPPER_CENTER);
                                }
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage("Not enough storage capacity to store data", 10f, ScreenMessageStyle.UPPER_CENTER);
                            }
                        }
                    }
                }
            }
            Events["reviewScience"].guiActive = scienceData.Count > 0;
        }

        [KSPAction("expfillDrive")]
        public void expfillActivateAction(KSPActionParam param)
        {
            expfillDrive();
        }


        [KSPEvent(name = "reviewScience", active = true, guiActive = false, externalToEVAOnly = false, guiName = "Review Data")]
        public void reviewScience()
        {
            ReviewData();
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
            if (node.HasNode("ScienceData"))
            {
                foreach (ConfigNode dataNode in node.GetNodes("ScienceData"))
                {
                    ScienceData data = new ScienceData(dataNode);
                    scienceData.Add(data);
                    _DataAmount += data.dataAmount;
                }
            }
            Events["reviewScience"].guiActive = scienceData.Count > 0;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ScienceData dat in scienceData)
            {
                ConfigNode dataNode = node.AddNode("ScienceData");
                dat.Save(dataNode);
            }
        }

        public override string GetInfo()
        {
            return "Capacity: " + Capacity + "Mits\n";
        }

        // Results Dialog Page Callbacks
        
        private void _onPageDiscard(ScienceData data)
        {
            DumpData(data);
        }

        private void _onPageKeep(ScienceData data)
        {

        }

        private void _onPageTransmit(ScienceData data)
        {
            List<IScienceDataTransmitter> transmitters = vessel.FindPartModulesImplementing<IScienceDataTransmitter>();
            if (transmitters.Count > 0)
            {
                IScienceDataTransmitter transmitter = transmitters.FirstOrDefault(t => t.CanTransmit());
                if (transmitter != null)
                {
                    transmitter.TransmitData(new List<ScienceData> { data });
                    _DataAmount -= data.dataAmount;
                    scienceData.Remove(data);
                }
            }
            else
            {
                ScreenMessages.PostScreenMessage("No Comms Devices on this vessel. Cannot Transmit Data.", 3f, ScreenMessageStyle.UPPER_CENTER);
            }
        }

        private void _onPageSendToLab(ScienceData data)
        {
            ScienceLabSearch scienceLabSearch = new ScienceLabSearch(base.vessel, data);
            if (scienceLabSearch.NextLabForDataFound)
            {
                StartCoroutine(scienceLabSearch.NextLabForData.ProcessData(data, new Callback<ScienceData>(DumpData)));
            }
            else
            {
                scienceLabSearch.PostErrorToScreen();
            }
        }
        
        [KSPEvent(active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "Collect Data", unfocusedRange = 2)]
        public void CollectScience()
        {
            List<ModuleScienceContainer> containers = FlightGlobals.ActiveVessel.FindPartModulesImplementing<ModuleScienceContainer>();
            foreach (ModuleScienceContainer container in containers)
            {
                if (scienceData.Count > 0)
                {
                    if (container.StoreData(new List<IScienceDataContainer> {this}, false))
                    {
                        //ScreenMessages.PostScreenMessage("Transferred Data to " + vessel.vesselName, 3f, ScreenMessageStyle.UPPER_CENTER);
                        ScreenMessages.PostScreenMessage("<color=#99ff00ff>[" + base.part.partInfo.title + "]: All Items Collected.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage("<color=orange>[" + base.part.partInfo.title + "]: Not all items could be Collected.</color>", 5f, ScreenMessageStyle.UPPER_LEFT);
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage("<color=#99ff00ff>[" + base.part.partInfo.title + "]: Nothing to Collect.</color>", 3f, ScreenMessageStyle.UPPER_CENTER);
                }
            }
        }

        // IScienceDataContainer
        public void DumpData(ScienceData data)
        {
            ScreenMessages.PostScreenMessage(string.Concat(new string[]{"<color=#ff9900ff>[",base.part.partInfo.title,"]: ",data.title," Removed.</color>"}), 5f, ScreenMessageStyle.UPPER_LEFT);
            _DataAmount -= data.dataAmount;
            scienceData.Remove(data);
            Events["reviewScience"].guiActive = scienceData.Count > 0;
        }

        public ScienceData[] GetData()
        {
            return scienceData.ToArray();
        }

        public int GetScienceCount()
        {
            return scienceData.Count;
        }

        public void ReviewData()
        {
            foreach (ScienceData data in scienceData)
            {
                ScienceLabSearch labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    part,
                    data,
                    data.transmitValue,
                    data.labBoost,
                    true,
                    "If you transmit this data it will only be worth: " + Mathf.Round(data.transmitValue * 100) + "% of the full science value",
                    true,
                    labSearch,
                    _onPageDiscard,
                    _onPageKeep,
                    _onPageTransmit,
                    _onPageSendToLab);
                ExperimentsResultDialog.DisplayResult(page);
            }
        }

        public void ReviewDataItem(ScienceData data)
        {
            ScienceLabSearch labSearch = new ScienceLabSearch(FlightGlobals.ActiveVessel, data);
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    part,
                    data,
                    data.transmitValue,
                    data.labBoost,
                    true,
                    "If you transmit this data it will only be worth: " + Mathf.Round(data.transmitValue * 100) + "% of the full science value",
                    true,
                    labSearch,
                    _onPageDiscard,
                    _onPageKeep,
                    _onPageTransmit,
                    _onPageSendToLab);
            ExperimentsResultDialog.DisplayResult(page);
        }

        public void ReturnData(ScienceData data)
        {
            if (data == null)
            {
                return;
            }
            scienceData.Add(data);
        }
        
        public bool IsRerunnable()
        {
            return false;
        }
    }
}
