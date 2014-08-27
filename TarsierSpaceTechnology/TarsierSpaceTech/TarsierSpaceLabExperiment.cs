using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TarsierSpaceTech
{
    class TarsierSpaceLabExperiment
    {
        public string ExperimentID = "";

        public double experimentStartTime = 0;
        private double lastUpdateTime;
        public CelestialBody targetBody;

        public float xmitDataScalar = 0.75f;
        public float labBoostScalar = 1.25f;

        public bool collectingData = false;
        private float collectedData = 0;

        public Part part;

        public void OnLoad(ConfigNode node)
        {
            ExperimentID = node.GetValue("ExperimentID");
            experimentStartTime = double.Parse(node.GetValue("experimentStartTime"));
            lastUpdateTime = double.Parse(node.GetValue("lastUpdateTime"));
            targetBody = FlightGlobals.Bodies.Find(b => b.name == node.GetValue("targetBody"));
            xmitDataScalar = float.Parse(node.GetValue("xmitDataScalar"));
            labBoostScalar = float.Parse(node.GetValue("labBoostScalar"));
            collectingData = bool.Parse(node.GetValue("collectingData"));
            collectedData = float.Parse(node.GetValue("collectedData"));
        }

        public void OnSave(ConfigNode node)
        {
            node.AddValue("ExperimentID", ExperimentID);
            node.AddValue("experimentStartTime", experimentStartTime);
            node.AddValue("lastUpdateTime", lastUpdateTime);
            node.AddValue("targetBody", targetBody.name);
            node.AddValue("xmitDataScalar", xmitDataScalar);
            node.AddValue("labBoostScalar", labBoostScalar);
            node.AddValue("collectingData", collectingData);
            node.AddValue("collectedData", collectedData);
        }
    }
}
