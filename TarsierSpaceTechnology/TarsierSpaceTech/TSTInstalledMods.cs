/*
 * TSTCameraModule.cs
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

namespace TarsierSpaceTech
{
    class TSTInstalledMods
    {
            private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();                   

            internal static bool IsRTInstalled
            {
                get
                {
                    return IsModInstalled("RemoteTech");
                }
            }            

            internal static bool IsKopInstalled
            {
                get
                {
                    return IsModInstalled("Kopernicus");
                }
            }

            internal static bool IsRSSInstalled
            {
                get
                {                   
                    return IsModInstalled("RealSolarSystem");
                }
            }

        internal static bool IsResearchBodiesInstalled
        {
            get
            {
                return IsModInstalled("ResearchBodies");
            }
        }

        internal static bool IsOPMInstalled
        {
            get
            {
                CelestialBody sarnus = FlightGlobals.Bodies.FirstOrDefault(a => a.name == "Sarnus");
                if (sarnus != null)
                {
                    return true;
                }
                else
                {
                    return false;
                }                
            }
        }

        internal static bool IsModInstalled(string assemblyName)
            {
            Assembly assembly = (from a in assemblies
                                     where a.FullName.Contains(assemblyName)
                                     select a).FirstOrDefault();
                return assembly != null;
            }

        
    }
}
