/*
 * TSTScienceProgressionBlocker.cs
 * (C) Copyright 2015, Jamie Leighton
 * Tarsier Space Technologies
 * The original code and concept of TarsierSpaceTech rights go to Tobyb121 on the Kerbal Space Program Forums, which was covered by the MIT license.
 * Original License is here: https://github.com/JPLRepo/TarsierSpaceTechnology/blob/master/LICENSE
 * As such this code continues to be covered by MIT license.
 * Kerbal Space Program is Copyright (C) 2013 Squad. See http://kerbalspaceprogram.com/. This
 * project is in no way associated with nor endorsed by Squad.
 *
 *  The code in this file is based off code originally supplied by the KSP forum user xEvilReeperx - Thanks to him for supplying it.
 *  http://forum.kerbalspaceprogram.com/index.php?/topic/7542-the-official-unoffical-quothelp-a-fellow-plugin-developerquot-thread/&do=findComment&comment=2594258
 *  
 *  This file is part of TarsierSpaceTech.
 *  This code will re-proxy the stored GameEvents.OnScienceReceived for the ProgressTracker (KSP World First Events) Science progress on celestialbodies
 *  where the science subject contains the string : "TarsierSpaceTech.SpaceTelescope". This stops world first progress registering a unmanned science/visited event
 *  for celestial bodies that we take pictures of with a TST space telescope. The reason for doing is, is so that the stock contract generator will still generate
 *  "go visit planet x" or "explore planet x" contracts for celestial bodies that have had their picture taken.
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
using UnityEngine;

namespace TarsierSpaceTech
{
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    class TSTScienceProgressionBlocker : MonoBehaviour
    {
        //Proxy OnScienceReceived event
        public static readonly EventData<float, ScienceSubject, ProtoVessel, bool> ProxyOnScienceReceived =
            new EventData<float, ScienceSubject, ProtoVessel, bool>("Proxy.OnScienceReceived");

        private static TSTScienceProgressionBlocker Instance { get; set; }
        //private static bool _block = false;

        //If set to true will only bock a single event, otherwise will not in OnScienceReceived.
        //public static void BlockSingleEvent()
        //{
        //    _block = true;
        //}


        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this); // GameEvents throws an exception on static methods, so we need a reference ;\

            // We want this event to be the very first one, if possible. That will ensure it runs last
            GameEvents.OnScienceRecieved.Add(OnScienceReceived);
        }

        //Our override OnScienceReceived event. 
        //We block all OnScienceReceived from triggering the ProgressTracker where the subject contains: "TarsierSpaceTech.SpaceTelescope"
        private void OnScienceReceived(float amount, ScienceSubject subject, ProtoVessel vessel, bool data3)
        {
            //if (!_block)
            if (!subject.id.Contains("TarsierSpaceTech.SpaceTelescope"))  
                ProxyOnScienceReceived.Fire(amount, subject, vessel, data3);

            //_block = false;
        }
    }


    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.FLIGHT, GameScenes.TRACKSTATION, GameScenes.SPACECENTER)]
    public class TSTProgressTracker_Trap : ScenarioModule
    {
        private IEnumerator Start()
        {
            yield return new WaitForEndOfFrame(); // ProgressTracker might not have started, wait and make sure

            if (ProgressTracking.Instance == null)
            {
                Debug.LogError("ProgressTracking instance not found!"); //This is not good.
                yield break;
            }

            foreach (var cb in FlightGlobals.Bodies)
            {
                // get the scienceAchievement node
                var tree = ProgressTracking.Instance.GetBodyTree(cb);
                var scienceAchievement = tree.science;

                // remove science callbacks to onScienceReceived and onScienceDataTransmitted
                scienceAchievement.OnStow();

                // wrap the add/remove callbacks inside another little method that tricks them into
                // registering with the proxy GameEvents
                var originalStow = scienceAchievement.OnStow;
                var originalDeploy = scienceAchievement.OnDeploy;

                scienceAchievement.OnStow = () =>
                {
                    SwapInProxyEvents(originalStow);
                };

                scienceAchievement.OnDeploy = () =>
                {
                    SwapInProxyEvents(originalDeploy);
                };

                // restore science callbacks (although now they'll register with TSTScienceProgressionBlocker instead of the real ones)
                scienceAchievement.OnDeploy();
            }
        }


        private static void SwapInProxyEvents(Callback call)
        {
            var original = GameEvents.OnScienceRecieved;

            try
            {
                GameEvents.OnScienceRecieved = TSTScienceProgressionBlocker.ProxyOnScienceReceived;
                call();
            }
            finally
            {
                GameEvents.OnScienceRecieved = original;
            }
        }
    }
}
