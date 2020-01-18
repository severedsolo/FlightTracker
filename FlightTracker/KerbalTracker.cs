using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    internal class KerbalTracker : MonoBehaviour
    {
        internal static KerbalTracker Instance;
        private static EventData<ProtoCrewMember> onFlightTrackerUpdated;
        internal readonly Dictionary<string, int> Flights = new Dictionary<string, int>();
        internal readonly Dictionary<string, double> KerbalFlightTime = new Dictionary<string, double>();
        internal readonly Dictionary<string, double> LaunchTime = new Dictionary<string, double>();
        internal readonly Dictionary<string, int> NumberOfWorldFirsts = new Dictionary<string, int>();

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(this);
            onFlightTrackerUpdated = new EventData<ProtoCrewMember>("onFlightTrackerUpdated");
            Debug.Log("[FlightTracker]: Flight Tracker is Awake");
        }

        internal void OnVesselRollout()
        {
            Debug.Log("[FlightTracker]: OnVesselRollout fired");
            if (FlightGlobals.ActiveVessel.GetCrewCount() == 0) return;
            for(int i = 0; i<FlightGlobals.ActiveVessel.GetCrewCount();i++)
            {
                ProtoCrewMember p = FlightGlobals.ActiveVessel.GetVesselCrew().ElementAt(i);
                if (p == null) return;
                if (p.type == ProtoCrewMember.KerbalType.Tourist)
                {
                    Debug.Log("[FlightTracker]: " + p.name + " is a tourist and won't be tracked");
                    continue;
                }
                LaunchTime.Remove(p.name);
                LaunchTime.Add(p.name, Planetarium.GetUniversalTime());
                if(!Flights.TryGetValue(p.name, out int _))Flights.Add(p.name, 0);
                if(!KerbalFlightTime.TryGetValue(p.name, out double _))KerbalFlightTime.Add(p.name, 0);
                Debug.Log("[FlightTracker]: "+p.name+" launched at "+Planetarium.GetUniversalTime());
            }
        }

        internal void RecordWorldFirst()
        {
            Debug.Log("[FlightTracker]: OnProgressComplete fired");
            if (FlightGlobals.ActiveVessel == null) return;
            List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
            Debug.Log("[FlightTracker]: Found " + crew.Count + " potential candidates for World Firsts");
            if (crew.Count == 0) return;
            for(int i = 0; i<crew.Count; i++)
            {
                if (!crew.ElementAt(i).flightLog.HasEntry(FlightLog.EntryType.Orbit))
                {
                    Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has not reached orbit yet and won't be given credit for this world first");
                    continue;
                }
                
                NumberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out int recordedWorldFirsts);
                NumberOfWorldFirsts.Remove(crew.ElementAt(i).name);
                recordedWorldFirsts++;
                NumberOfWorldFirsts.Add(crew.ElementAt(i).name, recordedWorldFirsts);
                Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has achieved a World First");
            }
        }

        internal void ProcessKerbalRecovery(ProtoVessel v)
        {
            Debug.Log("[FlightTracker]: onVesselRecovered Fired");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (v.missionTime == 0)
            {
                Debug.Log("[FlightTracker]: " + v.vesselName + " hasn't gone anywhere. No credit will be awarded for this flight");
                return;
            }
            List<ProtoCrewMember> crew = v.GetVesselCrew();
            Debug.Log("[FlightTracker]: Processing " + crew.Count + " Kerbals");
            if (crew.Count == 0) return;
            for (int i = 0; i < crew.Count; i++)
            {
                if (crew.ElementAt(i).type == ProtoCrewMember.KerbalType.Tourist)
                {
                    Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " is a tourist and won't be tracked");
                    continue;
                }
                string p = crew.ElementAt(i).name;
                int recovered = 0;
                if (Flights.TryGetValue(p, out recovered)) Flights.Remove(p);
                recovered += 1;
                double d = 0;
                if (KerbalFlightTime.TryGetValue(p, out d)) KerbalFlightTime.Remove(p);
                double missionTime = 0;
                if (LaunchTime.TryGetValue(p, out double recordedLaunchTime)) missionTime = Planetarium.GetUniversalTime() - recordedLaunchTime;
                else missionTime = v.missionTime;
                d += missionTime;
                Flights.Add(p, recovered);
                KerbalFlightTime.Add(p, d);
                NumberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out int recordedWorldFirsts);
                Debug.Log("[FlightTracker]: Processed Recovery of " + p);
                Debug.Log("[FlightTracker]: " + p + " - Flights: " + recovered);
                Debug.Log("[FlightTracker]: " + p + " - Time Logged: " + (int)d);
                Debug.Log("[FlightTracker]: " + p + " - World Firsts Achieved: " + recordedWorldFirsts);
                onFlightTrackerUpdated.Fire(crew.ElementAt(i));
            }
        }
    }
}
