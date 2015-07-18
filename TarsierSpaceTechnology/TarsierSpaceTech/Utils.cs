/*
 * Utilities.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;


namespace TarsierSpaceTech
{

    public static class Utilities
    {
       
        public static int randomSeed = new System.Random().Next();
        private static int _nextrandomInt = randomSeed;

        public static int getnextrandomInt()
        {
            _nextrandomInt ++;
            return _nextrandomInt;
        }
        
        public static Transform FindChildRecursive(Transform parent, string name)
        {
            return parent.gameObject.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == name);
        }

        
        public static double GetAvailableResource(Part part, String resourceName)
        {
            var resources = new List<PartResource>();
            part.GetConnectedResources(PartResourceLibrary.Instance.GetDefinition(resourceName).id, ResourceFlowMode.ALL_VESSEL, resources);
            double total = 0;
            foreach (PartResource pr in resources)
            {
                total += pr.amount;
            }
            return total;
        }

        public static bool dumpCameras()
        {
            foreach (Camera cam in Camera.allCameras)
            {
                Log_Debug("Camera=" + cam.name + ",depth=" + cam.depth + ",cullingMask=" + cam.cullingMask + ",nearClipPane=" + cam.nearClipPlane + ",farClipPane=" + cam.farClipPlane + ",fieldofView=" + cam.fieldOfView);
            }
            return true;
        }

        public static Camera findCameraByName(string camera)
        {
            foreach (Camera cam in Camera.allCameras)
            {               
                if (cam.name == camera)
                    return cam;
            }
            return null;
        }

        public static IEnumerator WaitForAnimation(Animation animation, string name)
        {
            do
            {
                yield return null;
            } while (animation.IsPlaying(name));
        }

        // GUI & Window Methods

        public static bool WindowVisibile(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;
            bool xRnge = (winpos.x > xMin) && (winpos.x < xMax);
            bool yRnge = (winpos.y > yMin) && (winpos.y < yMax);
            return xRnge && yRnge;
        }

        public static Rect MakeWindowVisible(Rect winpos)
        {
            float minmargin = 20.0f; // 20 bytes margin for the window
            float xMin = minmargin - winpos.width;
            float xMax = Screen.width - minmargin;
            float yMin = minmargin - winpos.height;
            float yMax = Screen.height - minmargin;

            winpos.x = Mathf.Clamp(winpos.x, xMin, xMax);
            winpos.y = Mathf.Clamp(winpos.y, yMin, yMax);

            return winpos;
        }

        internal static bool isPauseMenuOpen()
        {
            try
            {
                return PauseMenu.isOpen;
            }
            catch
            {
                return false;
            }
        }

        // Get Config Node Values out of a config node Methods

        public static bool GetNodeValue(ConfigNode confignode, string fieldname, bool defaultValue)
        {
            bool newValue;
            if (confignode.HasValue(fieldname) && bool.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static int GetNodeValue(ConfigNode confignode, string fieldname, int defaultValue)
        {
            int newValue;
            if (confignode.HasValue(fieldname) && int.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static float GetNodeValue(ConfigNode confignode, string fieldname, float defaultValue)
        {
            float newValue;
            if (confignode.HasValue(fieldname) && float.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static double GetNodeValue(ConfigNode confignode, string fieldname, double defaultValue)
        {
            double newValue;
            if (confignode.HasValue(fieldname) && double.TryParse(confignode.GetValue(fieldname), out newValue))
            {
                return newValue;
            }
            else
            {
                return defaultValue;
            }
        }

        public static string GetNodeValue(ConfigNode confignode, string fieldname, string defaultValue)
        {
            if (confignode.HasValue(fieldname))
            {
                return confignode.GetValue(fieldname);
            }
            else
            {
                return defaultValue;
            }
        }

        public static T GetNodeValue<T>(ConfigNode confignode, string fieldname, T defaultValue) where T : IComparable, IFormattable, IConvertible
        {
            if (confignode.HasValue(fieldname))
            {
                string stringValue = confignode.GetValue(fieldname);
                if (Enum.IsDefined(typeof(T), stringValue))
                {
                    return (T)Enum.Parse(typeof(T), stringValue);
                }
            }
            return defaultValue;
        }

        public static void PrintTransform(Transform t, string title = "")
        {
            Log_Debug("------" + title + "------");
            Log_Debug("Position: " + t.localPosition);
            Log_Debug("Rotation: " + t.localRotation);
            Log_Debug("Scale: " + t.localScale);
            Log_Debug("------------------");
        }

        public static void DumpObjectProperties(object o, string title = "---------")
        {
            // Iterate through all of the properties
            Log_Debug("--------- " + title + " ------------");
            foreach (PropertyInfo property in o.GetType().GetProperties())
            {
                if (property.CanRead)
                    Log_Debug(property.Name + " = " + property.GetValue(o, null));
            }
            Log_Debug("--------------------------------------");
        }

        // Logging Functions
        // Name of the Assembly that is running this MonoBehaviour
        internal static String _AssemblyName
        { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Name; } }

        public static void Log(this UnityEngine.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(this System.Object obj, string message)
        {
            Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log(string context, string message)
        {
            Debug.Log(context + "[][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(this UnityEngine.Object obj, string message)
        {
            TSTSettings TSTsettings = TSTMstStgs.Instance.TSTsettings;
            if (TSTsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetInstanceID().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(this System.Object obj, string message)
        {
            TSTSettings TSTsettings = TSTMstStgs.Instance.TSTsettings;
            if (TSTsettings.debugging)
                Debug.Log(obj.GetType().FullName + "[" + obj.GetHashCode().ToString("X") + "][" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(string context, string message)
        {
            TSTSettings TSTsettings = TSTMstStgs.Instance.TSTsettings;
            if (TSTsettings.debugging)
                Debug.Log("[TST] " + context + "[" + Time.time.ToString("0.00") + "]: " + message);
        }

        public static void Log_Debug(string message)
        {
            TSTSettings TSTsettings = TSTMstStgs.Instance.TSTsettings;
            if (TSTsettings.debugging)
                Debug.Log("[TST] " + "[" + Time.time.ToString("0.00") + "]: " + message);
        }
    }
}