using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class VesselTracker : MonoBehaviour
    {
        internal static VesselTracker Instance;
        private readonly Dictionary<uint, double> actualLaunchTime = new Dictionary<uint, double>();

        private void Awake()
        {
            Instance = this;
        }

        internal void StartTrackingVessel()
        {
            if (!actualLaunchTime.TryGetValue(FlightGlobals.ActiveVessel.persistentId, out double d)) actualLaunchTime.Add(FlightGlobals.ActiveVessel.persistentId, Planetarium.GetUniversalTime());
            else Debug.Log("[FlightTracker]: VesselTracker found a duplicate ID! " + FlightGlobals.ActiveVessel.vesselName + " " + FlightGlobals.ActiveVessel.persistentId);
            VerifyIDs();
            CorrectMETs();
        }

        private void CorrectMETs()
        {
            for (int i = 0; i < actualLaunchTime.Count; i++)
            {
                KeyValuePair<uint, double> kvp = actualLaunchTime.ElementAt(i);
                double actualMet = Planetarium.GetUniversalTime() - kvp.Value;
                Vessel v = ActiveFlightTracker.instance.MatchVesselToId(kvp.Key);
                if (v.missionTime == actualMet) continue;
                v.missionTime = actualMet;
                Debug.Log("Correcting Mission Time for " + v.vesselName + " id: " + v.persistentId + " to " + actualMet);
            }
        }

        private void VerifyIDs()
        {
            Debug.Log("[FlightTracker]: Checking for vessels that no longer exist");
            for (int i = actualLaunchTime.Count; i >= 0; i--)
            {
                uint id = actualLaunchTime.ElementAt(i).Key;
                if (VesselStillExists(id)) continue;
                actualLaunchTime.Remove(id);
                Debug.Log("[FlightTracker]: Stopped Tracking "+id);
            }
        }

        private bool VesselStillExists(uint id)
        {
            if (ActiveFlightTracker.instance.MatchVesselToId(id) == null) return false;
            return true;
        }
    }
}