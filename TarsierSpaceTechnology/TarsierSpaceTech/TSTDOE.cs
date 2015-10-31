/*
* http://creativecommons.org/licenses/by/4.0/
*
* This work, is a derivative of "CactEye 2," which is a derivative of "CactEye Orbital Telescope" by Rubber-Ducky, used under CC BY 4.0. "CactEye 2" is licensed under CC BY 4.0 by Raven.
*/
/*
 * TSTDOE.cs
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
using System.Reflection;
using UnityEngine;


namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class TSTDOE : MonoBehaviour
    {
        private List<TSTCameraModule> TSTCam = new List<TSTCameraModule>();
        private static bool DOEPresent = false;

        private void Start()
        {

            if (TSTCam == null)
            {
                Debug.Log("TST: DOEWrapper: Uh-oh, we have a problem. If you see this error, then you're gonna have a bad day.");
            }

            else
            {
                foreach (Part p in FlightGlobals.ActiveVessel.Parts)
                {
                    TSTCameraModule tstCam = p.GetComponent<TSTCameraModule>();
                    if (tstCam != null)
                    {
                        if (!TSTCam.Contains(tstCam))
                        {
                            TSTCam.Add(tstCam);

                        }
                    }
                }
            }
            DOEPresent = DOEWrapper.InitDOEWrapper();
            // DOEPresent = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "DistantObject");
        }

        private void Update()
        {

            bool ExternalControl = false;
            TSTCameraModule ActiveOptics = null;
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                TSTSpaceTelescope tstSpcTel = p.GetComponent<TSTSpaceTelescope>();
                if (tstSpcTel != null)
                {
                    TSTCameraModule tstCam = tstSpcTel._camera;
                    if (tstCam != null)
                    {
                        if (!TSTCam.Contains(tstCam))
                        {
                            TSTCam.Add(tstCam);

                        }
                    }
                }
                
            }

            foreach (TSTCameraModule tstCam in TSTCam)
            {
                //Check for when optics is null, this avoids an unknown exception
                if (tstCam != null && tstCam.Enabled)
                {
                    ExternalControl = true;
                    ActiveOptics = tstCam;
                }
            }

            if (DOEPresent)
                try
                {
                    //SetDOEFOV(ExternalControl, ActiveOptics);
                    if (DOEWrapper.APIReady)
                    {
                        DOEWrapper.DOEapi.SetExternalFOVControl(ExternalControl);
                        if (ExternalControl)
                        {
                            DOEWrapper.DOEapi.SetFOV(ActiveOptics.fov);
                        }
                    }
                }
                catch
                {
                    Debug.Log("TST: Wrong DOE library version - disabled.");
                    DOEPresent = false;
                }


            
        }        
    }
   
    /// <summary>
    /// The Wrapper class to access DOE
    /// </summary>
    public class DOEWrapper
    {
        protected static System.Type DOEType;
        protected static System.Type DOEFlareDrawType;
                        
        protected static System.Object actualDOEFlareDraw;        

        /// <summary>
        /// This is the DOE API object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static DOEAPI DOEapi;

        /// <summary>
        /// Whether we found the DOE assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblyExists { get { return (DOEType != null); } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceExists { get { return (DOEapi != null); } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _DOEWrapped;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APIReady { get { return _DOEWrapped; } }

        /// <summary>
        /// This method will set up the DOE object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitDOEWrapper()
        {
            //reset the internal objects
            _DOEWrapped = false;
            actualDOEFlareDraw = null;
            DOEapi = null;
            LogFormatted("Attempting to Grab DOE Types...");

            //find the base type
            DOEType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "DistantObject.FlareDraw");

            if (DOEType == null)
            {
                return false;
            }

            LogFormatted("DistantObject Version:{0}", DOEType.Assembly.GetName().Version.ToString());

            //find the FlareDraw class type
            DOEFlareDrawType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "DistantObject.FlareDraw");

            if (DOEFlareDrawType == null)
            {
                return false;
            }

            //now grab the running instance
            LogFormatted("Got Assembly Types, grabbing Instances");
            
            try
            {
                actualDOEFlareDraw = DOEFlareDrawType.GetMethod("SetExternalFOVControl", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
            }
            catch (Exception)
            {
                LogFormatted("No DOE FlareDraw Instance found");
                //throw;
            }
            if (actualDOEFlareDraw == null)
            {
                LogFormatted("Failed grabbing Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted("Got Instance, Creating Wrapper Objects");
            DOEapi = new DOEAPI(actualDOEFlareDraw);

            _DOEWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real DOE. This lets you access all the API-able properties and Methods of DOE
        /// </summary>
        public class DOEAPI
        {
            internal DOEAPI(System.Object actualDOEFlareDraw)
            {
                //store the actual object
                APIactualDOEFlareDraw = actualDOEFlareDraw;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler
                //LogFormatted("Getting APIReady Object");
                //APIReadyField = TRType.GetField("APIReady", BindingFlags.Public | BindingFlags.Static);
                //LogFormatted("Success: " + (APIReadyField != null).ToString());

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE
                //Methods
                LogFormatted("Getting Methods");
                SetExternalFOVControlMethod = DOEFlareDrawType.GetMethod("SetExternalFOVControl", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                SetFOVMethod = DOEFlareDrawType.GetMethod("SetFOV", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance);
                LogFormatted_DebugOnly("Success: " + (SetExternalFOVControlMethod != null).ToString());
            }

            private System.Object APIactualDOEFlareDraw;

            #region Methods

            private MethodInfo SetExternalFOVControlMethod;

            /// <summary>
            /// Set DOE ExternalFOVControl bool
            /// </summary>
            /// <param name="setting">True or False</param>
            /// <returns>Success of call</returns>
            internal bool SetExternalFOVControl(bool setting)
            {
                try
                {
                    SetExternalFOVControlMethod.Invoke(APIactualDOEFlareDraw, new System.Object[] { setting });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke DOE SetExternalFOVControl Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo SetFOVMethod;

            /// <summary>
            /// Set the FOV for Flare Draw
            /// </summary>
            /// <param name="setting">Float value of the FOV to set</param>
            /// <returns>Success of call</returns>
            internal bool SetFOV(float setting)
            {
                try
                {
                    SetFOVMethod.Invoke(APIactualDOEFlareDraw, new System.Object[] { setting });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke DOE SetFOV Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            #endregion Methods
        }

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN DLL COMPILED IN DEBUG MODE
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        [System.Diagnostics.Conditional("DEBUG")]
        internal static void LogFormatted_DebugOnly(String Message, params System.Object[] strParams)
        {
            LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params System.Object[] strParams)
        {
            Message = String.Format(Message, strParams);
            String strMessageLine = String.Format("{0},{2}-{3},{1}",
                DateTime.Now, Message, System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);
            UnityEngine.Debug.Log(strMessageLine);
        }

        #endregion Logging Stuff
    }
}

