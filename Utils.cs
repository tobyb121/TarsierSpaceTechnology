using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

class Utils
{
    public static void print(object message)
    {
#if DEBUG
        KSPLog.print("[TST]: " + message);
#endif
    }

    public static double GetAvailableResource(Part part, String resourceName)
    {
        var resources = new List<PartResource>();
        part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resourceName).id, resources);
        double total = 0;
        foreach (PartResource pr in resources)
        {
            total += pr.amount;
        }
        return total;
    }
}
