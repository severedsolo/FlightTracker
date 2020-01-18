using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    internal class VesselTracker : MonoBehaviour
    {
        internal static VesselTracker Instance;
        internal readonly Dictionary<uint, double> ActualLaunchTime = new Dictionary<uint, double>();

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }

        internal void StartTrackingVessel()
        {
            if (!ActualLaunchTime.TryGetValue(FlightGlobals.ActiveVessel.persistentId, out double d)) ActualLaunchTime.Add(FlightGlobals.ActiveVessel.persistentId, Planetarium.GetUniversalTime());
            else Debug.Log("[FlightTracker]: VesselTracker found a duplicate ID! " + FlightGlobals.ActiveVessel.vesselName + " " + d);
            VerifyIDs();
        }
        
        private void VerifyIDs()
        {
            Debug.Log("[FlightTracker]: Checking for vessels that no longer exist");
            for (int i = ActualLaunchTime.Count; i >= 0; i--)
            {
                uint id = ActualLaunchTime.ElementAt(i).Key;
                if (VesselStillExists(id)) continue;
                ActualLaunchTime.Remove(id);
                Debug.Log("[FlightTracker]: Stopped Tracking "+id);
            }
        }

        private bool VesselStillExists(uint id)
        {
            if (MatchVesselToId(id) == null) return false;
            return true;
        }

        internal Vessel MatchVesselToId(uint id)
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel v = FlightGlobals.Vessels.ElementAt(i);
                if (v.persistentId != id) continue;
                return v;
            }

            return null;
        }
        
        public void OnSave(ConfigNode cn)
        {
            ConfigNode trackerNode = new ConfigNode("VESSEL_TRACKER");
            foreach (KeyValuePair<uint, double> kvp in ActualLaunchTime)
            {
                ConfigNode vesselNode = new ConfigNode("VESSEL");
                vesselNode.SetValue("ID", kvp.Key, true);
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
                if (!uint.TryParse(vesselNode.GetValue("ID"), out uint id)) continue;
                if(!double.TryParse(vesselNode.GetValue("actualLaunchTime"), out double launchTime)) continue;
                ActualLaunchTime.Add(id, launchTime);
            }
        }
    }
}