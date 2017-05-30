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
using UniLinq;
using KSP.UI.Screens.Flight.Dialogs;
using RSTUtils;
using UnityEngine;
using KSP.Localization;

namespace TarsierSpaceTech
{
    class TSTScienceHardDrive : PartModule, IScienceDataContainer
    {
        public List<ScienceData> scienceData = new List<ScienceData>();       
                
        [KSPField(guiActive = true, guiName = "#autoLOC_TST_0066", isPersistant = true, guiUnits = "#autoLOC_TST_0067")] //#autoLOC_TST_0066 = Capacity #autoLOC_TST_0067 = \u0020Mits
        public float Capacity = 50f;

        [KSPField(guiActive = true, guiName = "#autoLOC_TST_0276", isPersistant = true, guiUnits = "%")] //#autoLOC_TST_0276 = Data Corruption
        public float corruption = 0.2f;

        [KSPField(guiActive = true, guiName = "#autoLOC_TST_0277", isPersistant = true, guiFormat = "F3", guiUnits = "#autoLOC_6002100")] //#autoLOC_TST_0277 = Power Usage #autoLOC_6002100 = EC
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

        [KSPField(guiActive = true, guiName = "#autoLOC_TST_0068", isPersistant = false, guiFormat = "0", guiUnits = " %")]  //#autoLOC_TST_0068 = Percentage Full
        public float PercentageFull;

        public override void OnStart(StartState state)
        {
            Events["fillDrive"].guiActiveUnfocused = fillFromEVA;
            Events["fillDrive"].unfocusedRange = EVARange;            
        }

        [KSPEvent(name = "fillDrive", active = true, guiActive = true, externalToEVAOnly = false, guiName = "#autoLOC_TST_0069")] //#autoLOC_TST_0069 = Move All Science to Drive
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
                                float ECAmount = d.dataAmount*powerUsage;
                                double resAvail = 0;
                                double restotal = 0;
                                if (CheatOptions.InfiniteElectricity || Utilities.requireResource(vessel, "ElectricCharge", ECAmount, true, true, false, out resAvail, out restotal)) // GetAvailableResource(part, "ElectricCharge") >= d.dataAmount * powerUsage)
                                {
                                    //Utilities.Log_Debug("Removing Electric Charge");
                                    //part.RequestResource("ElectricCharge", d.dataAmount * powerUsage);
                                    Utilities.Log_Debug("Adding Data");
                                    scienceData.Add(d);
                                    d.dataAmount *= (1 - corruption);
                                    Utilities.Log_Debug("Incrementing stored val");
                                    _DataAmount += d.dataAmount;
                                    Utilities.Log_Debug("Removing Data from source");
                                    container.DumpData(d);
                                    Utilities.Log_Debug("Data Added");
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0070", d.title), 10f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0070 = Moved <<1>> to TST Drive"
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0071", ECAmount.ToString("00.00")), 10f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0071 = Required <<1>> ElectricCharge not available to store data
                                }
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0072"), 10f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0072 = Not enough storage capacity to store data
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

        [KSPEvent(name = "expfillDrive", active = true, guiActive = true, externalToEVAOnly = false, guiName = "#autoLOC_TST_0073")] //#autoLOC_TST_0073 = Move Experiments to Drive
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
                                float ECAmount = d.dataAmount * powerUsage;
                                double resAvail = 0;
                                double restotal = 0;
                                if (CheatOptions.InfiniteElectricity || Utilities.requireResource(vessel, "ElectricCharge", ECAmount, true, true, false, out resAvail, out restotal)) //.GetAvailableResource(part, "ElectricCharge") >= d.dataAmount * powerUsage)
                                {
                                    //Utilities.Log_Debug("Removing Electric Charge");
                                    //part.RequestResource("ElectricCharge", d.dataAmount * powerUsage);
                                    Utilities.Log_Debug("Adding Data");
                                    scienceData.Add(d);
                                    d.dataAmount *= (1 - corruption);
                                    Utilities.Log_Debug("Incrementing stored val");
                                    _DataAmount += d.dataAmount;
                                    Utilities.Log_Debug("Removing Data from source");
                                    container.DumpData(d);
                                    Utilities.Log_Debug("Data Added");
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0074",d.title), 10f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0074 = Moved <<1>> to TST Drive
                                }
                                else
                                {
                                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0075", ECAmount.ToString("00.00")), 10f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0075 = Required <<1>> ElectricCharge not available to store data
                                }
                            }
                            else
                            {
                                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0076"), 10f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0076 = Not enough storage capacity to store data
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


        [KSPEvent(name = "reviewScience", active = true, guiActive = false, externalToEVAOnly = false, guiName = "#autoLOC_TST_0077")] //#autoLOC_TST_0077 = Review Data
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
            string returnInfo = string.Empty;
            returnInfo = Localizer.Format("#autoLOC_TST_0078", Capacity); // #autoLOC_TST_0078 = Capacity: <<1>>Mits\n
            returnInfo += Localizer.Format("#autoLOC_TST_0276") + ": "+ corruption.ToString("#0.##") + "%\n";
            returnInfo += Localizer.Format("#autoLOC_TST_0277") + ": " + powerUsage.ToString("#0.###") + Localizer.Format("#autoLOC_6002100") + "\n";
            return returnInfo;
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
            IScienceDataTransmitter transmitter = ScienceUtil.GetBestTransmitter(vessel);

            if (transmitter != null)
            {
                List<ScienceData> dataToSend = new List<ScienceData>();
                dataToSend.Add(data);
                transmitter.TransmitData(dataToSend);
                scienceData.Remove(data);                
            }
            else
            {
                ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0079"), 3f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0079 = No Comms Devices on this vessel. Cannot Transmit Data.
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
        
        [KSPEvent(active = true, externalToEVAOnly = true, guiActiveUnfocused = true, guiName = "#autoLOC_TST_0044", unfocusedRange = 2)] //#autoLOC_TST_0044 = Collect Data
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
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0045", part.partInfo.title), 5f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0045 = <color=#99ff00ff>[<<1>>]: All Items Collected.</color>
                    }
                    else
                    {
                        ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0046", part.partInfo.title), 5f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0046 = <color=orange>[<<1>>]: Not all items could be Collected.</color>
                    }
                }
                else
                {
                    ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0047", part.partInfo.title), 3f, ScreenMessageStyle.UPPER_CENTER); //#autoLOC_TST_0047 = <color=#99ff00ff>[<<1>>]: Nothing to Collect.</color>
                }
            }
        }

        // IScienceDataContainer
        public void DumpData(ScienceData data)
        {
            ScreenMessages.PostScreenMessage(Localizer.Format("#autoLOC_TST_0233", part.partInfo.title, data.title), 5f, ScreenMessageStyle.UPPER_LEFT); //#autoLOC_TST_0233 = <color=#ff9900ff>[<<1>>]: <<2>> Removed.</color>
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
                    data.baseTransmitValue,
                    data.transmitBonus,
                    true,
                    Localizer.Format("#autoLOC_TST_0051", Mathf.Round(data.baseTransmitValue * 100)), //#autoLOC_TST_0051 = If you transmit this data it will only be worth: <<1>>% of the full science value
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
                    data.baseTransmitValue,
                    data.transmitBonus,
                    true,
                    Localizer.Format("#autoLOC_TST_0051", Mathf.Round(data.baseTransmitValue * 100)), //#autoLOC_TST_0051 = If you transmit this data it will only be worth: <<1>>% of the full science value
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
