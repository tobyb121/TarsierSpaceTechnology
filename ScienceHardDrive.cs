using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TarsierSpaceTech
{
    class ScienceHardDrive : PartModule, IScienceDataContainer
    {
        private List<ScienceData> _scienceData = new List<ScienceData>();

        [KSPField(guiActive = true, guiName = "Capacity", isPersistant = true, guiUnits = " Mits")]
        public float Capacity = 50f;

        [KSPField(guiActive = false, isPersistant = true)]
        public float corruption = 0.2f;

        [KSPField(guiActive = false, isPersistant = true)]
        public float powerUsage = 0.5f;

        private float _dataAmount = 0;
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

        [KSPEvent(name = "fillDrive", active = true, guiActive = true, externalToEVAOnly = false, guiName = "Fill Hard Drive")]
        public void fillDrive()
        {
            Utils.print("FILLING DRIVE");

            List<Part> parts = vessel.Parts.Where(p => p.FindModulesImplementing<IScienceDataContainer>().Count > 0).ToList();
            parts.RemoveAll(p => p.FindModulesImplementing<ScienceHardDrive>().Count > 0);
            Utils.print(parts.Count);
            foreach (Part p in parts)
            {
                List<IScienceDataContainer> containers = p.FindModulesImplementing<IScienceDataContainer>().ToList();
                Utils.print("Got containers: " + containers.Count.ToString());
                foreach (IScienceDataContainer container in containers)
                {
                    Utils.print("Checking Data");
                    ScienceData[] data = container.GetData();
                    Utils.print("Got Data: " + data.Length.ToString());
                    foreach (ScienceData d in data)
                    {
                        if (d != null)
                        {
                            Utils.print("Checking Space: " + d.dataAmount.ToString() + " " + _dataAmount.ToString() + " " + Capacity.ToString());
                            if (d.dataAmount + _dataAmount <= Capacity)
                            {
                                if (Utils.GetAvailableResource(part, "ElectricCharge") >= d.dataAmount * powerUsage)
                                {
                                    Utils.print("Removing Electric Charge");
                                    part.RequestResource("ElectricCharge", d.dataAmount * powerUsage);
                                    Utils.print("Adding Data");
                                    _scienceData.Add(d);
                                    d.dataAmount *= (1 - corruption);
                                    Utils.print("Incrementing stored val");
                                    _DataAmount += d.dataAmount;
                                    Utils.print("Removing Data from target");
                                    container.DumpData(d);
                                    Utils.print("Data Added");
                                }
                            }
                        }
                    }
                }
            }
            Events["reviewScience"].guiActive = _scienceData.Count > 0;
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
                    _scienceData.Add(data);
                    _DataAmount += data.dataAmount;
                }
            }
            Events["reviewScience"].guiActive = _scienceData.Count > 0;
        }

        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);
            foreach (ScienceData dat in _scienceData)
            {
                ConfigNode dataNode = node.AddNode("ScienceData");
                dat.Save(dataNode);
            }
        }

        public override string GetInfo()
        {
            return "Capacity: " + Capacity.ToString() + "Mits\n";
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
                    _scienceData.Remove(data);
                }
            }
        }

        private void _onPageSendToLab(ScienceData data)
        {

        }

        // IScienceDataContainer
        public void DumpData(ScienceData data)
        {
            _DataAmount -= data.dataAmount;
            _scienceData.Remove(data);
            Events["reviewScience"].guiActive = _scienceData.Count > 0;
        }

        public ScienceData[] GetData()
        {
            return _scienceData.ToArray();
        }

        public int GetScienceCount()
        {
            return _scienceData.Count;
        }

        public void ReviewData()
        {
            foreach (ScienceData data in _scienceData)
            {
                ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    part,
                    data,
                    data.transmitValue,
                    data.labBoost,
                    true,
                    "If you transmit this data it will only be worth: " + Mathf.Round(data.transmitValue * 100).ToString() + "% of the full scienc value",
                    true,
                    false,
                    new Callback<ScienceData>(_onPageDiscard),
                    new Callback<ScienceData>(_onPageKeep),
                    new Callback<ScienceData>(_onPageTransmit),
                    new Callback<ScienceData>(_onPageSendToLab));
                ExperimentsResultDialog.DisplayResult(page);
            }
        }


        public bool IsRerunnable()
        {
            return false;
        }

        public void ReviewDataItem(ScienceData data)
        {
            ExperimentResultDialogPage page = new ExperimentResultDialogPage(
                    part,
                    data,
                    data.transmitValue,
                    data.labBoost,
                    true,
                    "If you transmit this data it will only be worth: " + Mathf.Round(data.transmitValue * 100).ToString() + "% of the full scienc value",
                    true,
                    false,
                    new Callback<ScienceData>(_onPageDiscard),
                    new Callback<ScienceData>(_onPageKeep),
                    new Callback<ScienceData>(_onPageTransmit),
                    new Callback<ScienceData>(_onPageSendToLab));
            ExperimentsResultDialog.DisplayResult(page);
        }
    }
}
