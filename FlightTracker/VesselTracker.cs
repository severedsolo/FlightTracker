using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    internal class VesselTracker : MonoBehaviour
    {
        internal static VesselTracker Instance;
        internal readonly Dictionary<Guid, double> ActualLaunchTime = new Dictionary<Guid, double>();

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        internal void StartTrackingVessel()
        {
            VerifyIDs();
            if (!ActualLaunchTime.TryGetValue(FlightGlobals.ActiveVessel.id, out double d)) ActualLaunchTime.Add(FlightGlobals.ActiveVessel.id, Planetarium.GetUniversalTime());
            else
            {
                ActualLaunchTime[FlightGlobals.ActiveVessel.id] = Planetarium.GetUniversalTime();
                Debug.Log("[FlightTracker]: VesselTracker found a duplicate ID! " + FlightGlobals.ActiveVessel.vesselName + " " + FlightGlobals.ActiveVessel.id);
            }
        }
        
        private void VerifyIDs()
        {
            Debug.Log("[FlightTracker]: Checking for vessels that no longer exist");
            if (ActualLaunchTime.Count == 0) return;
            Guid[] vesselIDs = ActualLaunchTime.Keys.ToArray();
            if (vesselIDs.Length == 0) return;
            foreach (Guid idToCheck in vesselIDs)
            {
                if (VesselStillExists(idToCheck)) continue;
                ActualLaunchTime.Remove(idToCheck);
            }
        }

        private bool VesselStillExists(Guid id)
        {
            if (MatchVesselToId(id) == null) return false;
            return true;
        }

        internal Vessel MatchVesselToId(Guid id)
        {
            if (FlightGlobals.Vessels.Count == 0) return null;
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel v = FlightGlobals.Vessels.ElementAt(i);
                if (v.id != id) continue;
                return v;
            }

            return null;
        }
        
        public void OnSave(ConfigNode cn)
        {
            ConfigNode trackerNode = new ConfigNode("VESSEL_TRACKER");
            foreach (KeyValuePair<Guid, double> kvp in ActualLaunchTime)
            {
                ConfigNode vesselNode = new ConfigNode("VESSEL");
                vesselNode.SetValue("ID", kvp.Key.ToString(), true);
                vesselNode.SetValue("actualLaunchTime", kvp.Value, true);
                trackerNode.AddNode(vesselNode);
            }
            cn.AddNode(trackerNode);
        }

        public void OnLoad(ConfigNode cn)
        {
            ActualLaunchTime.Clear();
            ConfigNode trackerNode = cn.GetNode("VESSEL_TRACKER");
            if (trackerNode == null) return;
            ConfigNode[] vesselNodes = trackerNode.GetNodes("VESSEL");
            foreach (ConfigNode vesselNode in vesselNodes)
            {
                if (!Guid.TryParse(vesselNode.GetValue("ID"), out Guid id)) continue;
                if(!double.TryParse(vesselNode.GetValue("actualLaunchTime"), out double launchTime)) continue;
                ActualLaunchTime.Add(id, launchTime);
            }
        }
    }
}