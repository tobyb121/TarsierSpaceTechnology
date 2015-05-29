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

            DOEPresent = AssemblyLoader.loadedAssemblies.Any(a => a.assembly.GetName().Name == "DistantObject");
        }

        private void Update()
        {

            bool ExternalControl = false;
            TSTCameraModule ActiveOptics = null;
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
                    SetDOEFOV(ExternalControl, ActiveOptics);
                }
                catch
                {
                    Debug.Log("TST: Wrong DOE library version - disabled.");
                    DOEPresent = false;
                }


            
        }

        private void SetDOEFOV(bool ExternalControl, TSTCameraModule ActiveOptics)
        {
            DistantObject.FlareDraw.SetExternalFOVControl(ExternalControl);

            if (ExternalControl)
            {
                DistantObject.FlareDraw.SetFOV(ActiveOptics.fov);
            }
        }
    }
}

