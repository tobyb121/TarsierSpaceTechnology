/*
 * RBWrapper.cs
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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;


namespace TarsierSpaceTech
{
    /// <summary>
    /// The Wrapper class to access ResearchBodies
    /// </summary>
    public class RBWrapper
    {
        protected static System.Type RBSCAPIType;
        protected static System.Type RBDBAPIType;
        protected static System.Type RBFLAPIType;
        protected static Object actualRBSC = null;
        protected static Object actualRBDB = null;
        protected static Object actualRBFL = null;

        /// <summary>
        /// This is the ResearchBodies API object
        ///
        /// SET AFTER INIT
        /// </summary>
        public static RBSCAPI RBSCactualAPI = null;
        public static RBDBAPI RBDBactualAPI = null;

        /// <summary>
        /// Whether we found the ResearchBodies API assembly in the loadedassemblies.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean AssemblySCExists { get { return (RBSCAPIType != null); } }
        public static Boolean AssemblyDBExists { get { return (RBDBAPIType != null); } }
        public static Boolean AssemblyFLExists { get { return (RBFLAPIType != null); } }

        /// <summary>
        /// Whether we managed to hook the running Instance from the assembly.
        ///
        /// SET AFTER INIT
        /// </summary>
        public static Boolean InstanceSCExists { get { return (actualRBSC != null); } }
        public static Boolean InstanceDBExists { get { return (actualRBDB != null); } }
        public static Boolean InstanceFLExists { get { return (actualRBFL != null); } }

        /// <summary>
        /// Whether we managed to wrap all the methods/functions from the instance.
        ///
        /// SET AFTER INIT
        /// </summary>
        private static Boolean _RBSCWrapped = false;
        private static Boolean _RBDBWrapped = false;
        private static Boolean _RBFLWrapped = false;

        /// <summary>
        /// Whether the object has been wrapped
        /// </summary>
        public static Boolean APISCReady { get { return _RBSCWrapped; } }
        public static Boolean APIDBReady { get { return _RBDBWrapped; } }
        public static Boolean APIFLReady { get { return _RBFLWrapped; } }

        /// <summary>
        /// This method will set up the ResearchBodies object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitRBSCWrapper()
        {
            //reset the internal objects
            _RBSCWrapped = false;
            actualRBSC = null;
            LogFormatted_DebugOnly("Attempting to Grab ResearchBodies Types...");

            //find the base type
            RBSCAPIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "ResearchBodies.ResearchBodies");

            if (RBSCAPIType == null)
            {
                return false;
            }
            
            LogFormatted_DebugOnly("ResearchBodies Version:{0}", RBSCAPIType.Assembly.GetName().Version.ToString());

            //now grab the running instance
            LogFormatted_DebugOnly("Got Assembly Types, grabbing Instances");
            try
            {
                actualRBSC = RBSCAPIType.GetField("ResearchCost", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static).GetValue(null);
            }
            catch (Exception)
            {
                LogFormatted("No ResearchBodies Instance found");
                //throw;
            }

            if (actualRBSC == null)
            {
                LogFormatted("Failed grabbing ResearchBodies Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
            RBSCactualAPI = new RBSCAPI(actualRBSC);

            _RBSCWrapped = true;
            return true;
        }

        /// <summary>
        /// This method will set up the ModuleTrackBodies object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitRBFLWrapper()
        {
            //reset the internal objects
            _RBFLWrapped = false;
            actualRBFL = null;
            LogFormatted("Attempting to Grab ResearchBodies Types...");

            //find the base type
            RBFLAPIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "ResearchBodies.ModuleTrackBodies");

            if (RBFLAPIType == null)
            {
                return false;
            }

            LogFormatted("ResearchBodies Version:{0}", RBFLAPIType.Assembly.GetName().Version.ToString());
                      
                       

            _RBFLWrapped = true;
            return true;
        }

        /// <summary>
        /// This method will set up the DAtabase object and wrap all the methods/functions
        /// </summary>
        /// <param name="Force">This option will force the Init function to rebind everything</param>
        /// <returns></returns>
        public static Boolean InitRBDBWrapper()
        {
            //reset the internal objects
            _RBDBWrapped = false;
            actualRBDB = null;
            LogFormatted_DebugOnly("Attempting to Grab ResearchBodies Database Types...");

            //find the base type
            RBDBAPIType = AssemblyLoader.loadedAssemblies
                .Select(a => a.assembly.GetExportedTypes())
                .SelectMany(t => t)
                .FirstOrDefault(t => t.FullName == "ResearchBodies.Database");

            if (RBDBAPIType == null)
            {
                return false;
            }

            LogFormatted_DebugOnly("ResearchBodies Version:{0}", RBDBAPIType.Assembly.GetName().Version.ToString());

            //now grab the running instance
            LogFormatted_DebugOnly("Got Assembly Types, grabbing Instances");
            try
            {
                actualRBDB = RBDBAPIType.GetMember("GetIgnoredBodies", BindingFlags.Public | BindingFlags.Static);
            }
            catch (Exception)
            {
                LogFormatted("No ResearchBodies Database Instance found");
                //throw;
            }

            if (actualRBDB == null)
            {
                LogFormatted("Failed grabbing ResearchBodies Database Instance");
                return false;
            }

            //If we get this far we can set up the local object and its methods/functions
            LogFormatted_DebugOnly("Got Instance, Creating Wrapper Objects");
            RBDBactualAPI = new RBDBAPI(actualRBDB);

            _RBDBWrapped = true;
            return true;
        }

        /// <summary>
        /// The Type that is an analogue of the real ResearchBodies. This lets you access all the API-able properties and Methods of ResearchBodies
        /// </summary>
        public class RBSCAPI
        {
            internal RBSCAPI(Object a)
            {
                //store the actual object
                APIactualRBSC = a;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE

                LogFormatted_DebugOnly("Getting enabled field");
                SCenabledField = RBSCAPIType.GetField("enable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (SCenabledField != null).ToString());

                LogFormatted_DebugOnly("Getting TrackedBodies field");
                SCTrackedBodiesField = RBSCAPIType.GetField("TrackedBodies", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (SCTrackedBodiesField != null).ToString());

                LogFormatted_DebugOnly("Getting TrackedBodies field");
                SCResearchStateField = RBSCAPIType.GetField("ResearchState", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (SCResearchStateField != null).ToString());

                //Methods
                LogFormatted_DebugOnly("Getting Research Method");
                ResearchMethod = RBSCAPIType.GetMethod("Research", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (ResearchMethod != null).ToString());

                LogFormatted_DebugOnly("Getting LaunchResearchPlan Method");
                LaunchResearchPlanMethod = RBSCAPIType.GetMethod("LaunchResearchPlan", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (LaunchResearchPlanMethod != null).ToString());

                LogFormatted_DebugOnly("Getting StopResearchPlan Method");
                StopResearchPlanMethod = RBSCAPIType.GetMethod("StopResearchPlan", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (StopResearchPlanMethod != null).ToString());

                LogFormatted_DebugOnly("Getting LoadBodyLook Method");
                LoadBodyLookMethod = RBSCAPIType.GetMethod("LoadBodyLook", BindingFlags.Public | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (LoadBodyLookMethod != null).ToString());
                
            }

            private Object APIactualRBSC;

            private FieldInfo SCenabledField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Boolean enabled
            {
                get
                {
                    if (SCenabledField == null)
                        return false;

                    return (Boolean)SCenabledField.GetValue(null);
                }
            }

            private FieldInfo SCTrackedBodiesField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Dictionary<CelestialBody, bool> TrackedBodies
            {
                get
                {
                    if (SCTrackedBodiesField == null)
                        return new Dictionary<CelestialBody, bool>();

                    return (Dictionary<CelestialBody, bool>)SCTrackedBodiesField.GetValue(null);
                }
            }

            private FieldInfo SCResearchStateField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Dictionary<CelestialBody, int> ResearchState
            {
                get
                {
                    if (SCResearchStateField == null)
                        return new Dictionary<CelestialBody, int>();

                    return (Dictionary<CelestialBody, int>)SCResearchStateField.GetValue(null);
                }
            }

            #region Methods

            private MethodInfo ResearchMethod;

            /// <summary>
            /// Whether the current vessel HasLocalControl
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>Success of call</returns>
            internal bool Research(CelestialBody body, int researchToAdd)
            {
                try
                {
                    return (bool)ResearchMethod.Invoke(APIactualRBSC, new System.Object[] { body, researchToAdd });
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke Research Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo LaunchResearchPlanMethod;

            /// <summary>
            /// Whether the current vessel HasAnyConnection
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>Success of call</returns>
            internal bool LaunchResearchPlan(CelestialBody cb)
            {
                try
                {
                    LaunchResearchPlanMethod.Invoke(APIactualRBSC, new System.Object[] { cb });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke LaunchResearchPlan Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo StopResearchPlanMethod;

            /// <summary>
            /// Gets the signal delay
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>A double indicating the signaldelay time</returns>
            internal bool StopResearchPlan(CelestialBody cb)
            {
                try
                {
                    StopResearchPlanMethod.Invoke(APIactualRBSC, new System.Object[] { cb });
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke StopResearchPlan Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            private MethodInfo LoadBodyLookMethod;

            /// <summary>
            /// Gets the signal delay
            /// </summary>
            /// <param name="id">The vessel id reference</param>
            /// <returns>A double indicating the signaldelay time</returns>
            internal bool LoadBodyLook()
            {
                try
                {
                    LoadBodyLookMethod.Invoke(APIactualRBSC, null);
                    return true;
                }
                catch (Exception ex)
                {
                    LogFormatted("Unable to invoke StopResearchPlan Method");
                    LogFormatted("Exception: {0}", ex);
                    return false;
                    //throw;
                }
            }

            #endregion Methods
        }

        /// <summary>
        /// The Type that is an analogue of the real ResearchBodies. This lets you access all the API-able properties and Methods of ModuleTrackBodies
        /// </summary>
        public class ModuleTrackBodies
        {
            internal ModuleTrackBodies(Object a)
            {
                //store the actual object
                APIactualRBFL = a;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE

                LogFormatted("Getting enabled field");
                FLenabledField = RBFLAPIType.GetField("enable", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (FLenabledField != null).ToString());

                LogFormatted("Getting TrackedBodies field");
                FLTrackedBodiesField = RBFLAPIType.GetField("TrackedBodies", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (FLTrackedBodiesField != null).ToString());

                LogFormatted("Getting TrackedBodies field");
                FLResearchStateField = RBFLAPIType.GetField("ResearchState", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
                LogFormatted_DebugOnly("Success: " + (FLResearchStateField != null).ToString());

                LogFormatted("Getting TrackedBodies field");
                FLScienceRewardField = RBFLAPIType.GetField("scienceReward", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (FLScienceRewardField != null).ToString());


            }

            private Object APIactualRBFL;

            private FieldInfo FLenabledField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Boolean enabled
            {
                get
                {
                    if (FLenabledField == null)
                        return false;

                    return (Boolean)FLenabledField.GetValue(APIactualRBFL);
                }
            }

            private FieldInfo FLTrackedBodiesField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Dictionary<CelestialBody, bool> TrackedBodies
            {
                get
                {
                    if (FLTrackedBodiesField == null)
                        return new Dictionary<CelestialBody, bool>();

                    return (Dictionary<CelestialBody, bool>)FLTrackedBodiesField.GetValue(APIactualRBFL);
                }
            }

            private FieldInfo FLResearchStateField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Dictionary<CelestialBody, int> ResearchState
            {
                get
                {
                    if (FLResearchStateField == null)
                        return new Dictionary<CelestialBody, int>();

                    return (Dictionary<CelestialBody, int>)FLResearchStateField.GetValue(APIactualRBFL);
                }
            }

            private FieldInfo FLScienceRewardField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public int ScienceReward
            {
                get
                {
                    if (FLScienceRewardField == null)
                        return 0;

                    return (int)FLScienceRewardField.GetValue(APIactualRBFL);
                }
            }

        }

        /// <summary>
        /// The Type that is an analogue of the real ResearchBodies. This lets you access all the API-able properties and Methods of Database
        /// </summary>
        public class RBDBAPI
        {
            internal RBDBAPI(Object a)
            {
                //store the actual object
                APIactualRBDB = a;

                //these sections get and store the reflection info and actual objects where required. Later in the properties we then read the values from the actual objects
                //for events we also add a handler

                //WORK OUT THE STUFF WE NEED TO HOOK FOR PEOPLE HERE

                LogFormatted_DebugOnly("Getting enableInSandbox field");
                DBenableInSandboxField = RBDBAPIType.GetField("enableInSandbox", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (DBenableInSandboxField != null).ToString());

                LogFormatted_DebugOnly("Getting DiscoveryMessage field");
                DBDiscoveryMessageField = RBDBAPIType.GetField("DiscoveryMessage", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                LogFormatted_DebugOnly("Success: " + (DBDiscoveryMessageField != null).ToString());
                                
            }

            private Object APIactualRBDB;

            private FieldInfo DBenableInSandboxField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Boolean enableInSandbox
            {
                get
                {
                    if (DBenableInSandboxField == null)
                        return false;

                    return (Boolean)DBenableInSandboxField.GetValue(null);
                }
            }

            private FieldInfo DBDiscoveryMessageField;

            /// <summary>
            /// Whether the enabled field is set
            /// </summary>
            public Dictionary<string, string> DiscoveryMessage
            {
                get
                {
                    if (DBDiscoveryMessageField == null)
                        return new Dictionary<string, string>();

                    return (Dictionary<string, string>)DBDiscoveryMessageField.GetValue(null);
                }
            }
            
        }

        #region Logging Stuff

        /// <summary>
        /// Some Structured logging to the debug file - ONLY RUNS WHEN TST Debug mode is on
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>        
        internal static void LogFormatted_DebugOnly(String Message, params Object[] strParams)
        {
            TSTSettings TSTsettings = TSTMstStgs.Instance.TSTsettings;
            if (TSTsettings.debugging)
                LogFormatted(Message, strParams);
        }

        /// <summary>
        /// Some Structured logging to the debug file
        /// </summary>
        /// <param name="Message">Text to be printed - can be formatted as per String.format</param>
        /// <param name="strParams">Objects to feed into a String.format</param>
        internal static void LogFormatted(String Message, params Object[] strParams)
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
