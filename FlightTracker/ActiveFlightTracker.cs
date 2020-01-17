using System.Collections.Generic;
using System;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;


namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class ActiveFlightTracker : MonoBehaviour
    {
        public static ActiveFlightTracker instance;
        private static EventData<ProtoCrewMember> onFlightTrackerUpdated;
        internal Dictionary<string, int> flights = new Dictionary<string, int>();
        internal Dictionary<string, double> met = new Dictionary<string, double>();
        internal Dictionary<string, double> launchTime = new Dictionary<string, double>();
        internal Dictionary<string, int> numberOfWorldFirsts = new Dictionary<string, int>();

        private void Awake()
        {
            instance = this;
            DontDestroyOnLoad(this);
            onFlightTrackerUpdated = new EventData<ProtoCrewMember>("onFlightTrackerUpdated");
            Debug.Log("[FlightTracker]: Flight Tracker is Awake");
        }
        [PublicAPI]
        public string ConvertUtToString(double time)
        {
            time = time / 60 / 60;
            time = (int)Math.Floor(time);
            string timeString = time.ToString();
            int stringLength = timeString.Count() - 3;
            if (time.ToString().Count() > 4) timeString = timeString.Substring(0, stringLength) + "k";
            return timeString;
        }

        [PublicAPI]
        public int GetNumberOfFlights(string kerbalName)
        {
            int i;
            flights.TryGetValue(kerbalName, out i);
            return i;
        }
        [PublicAPI]
        public double GetRecordedMissionTimeSeconds(string kerbalName)
        {
            double d;
            met.TryGetValue(kerbalName, out d);
            return d;
        }
        [PublicAPI]
        public double GetRecordedMissionTimeHours(string kerbalName)
        {
            double d;
            met.TryGetValue(kerbalName, out d);
            d = d / 60 / 60;
            return d;
        }
        [PublicAPI]
        public double GetLaunchTime(string kerbalName)
        {
            double d;
            launchTime.TryGetValue(kerbalName, out d);
            return d;
        }
        [PublicAPI]
        public int GetNumberOfWorldFirsts(string kerbalName)
        {
            int i;
            numberOfWorldFirsts.TryGetValue(kerbalName, out i);
            return i;
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
                launchTime.Remove(p.name);
                launchTime.Add(p.name, Planetarium.GetUniversalTime());
                if(!flights.TryGetValue(p.name, out int flightCount))flights.Add(p.name, 0);
                double d;
                if(!met.TryGetValue(p.name, out d))met.Add(p.name, 0);
                Debug.Log("[FlightTracker]: "+p.name+" launched at "+Planetarium.GetUniversalTime());
            }
        }

        internal void RecordWorldFirst()
        {
            Debug.Log("[FlightTracker]: OnProgressComplete fired");
            if (FlightGlobals.ActiveVessel == null) return;
            List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
            Debug.Log("[FlightTracker]: Found " + crew.Count() + " potential candidates for World Firsts");
            if (crew.Count == 0) return;
            for(int i = 0; i<crew.Count; i++)
            {
                if (!crew.ElementAt(i).flightLog.HasEntry(FlightLog.EntryType.Orbit))
                {
                    Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has not reached orbit yet and won't be given credit for this world first");
                    continue;
                }
                
                numberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out int recordedWorldFirsts);
                numberOfWorldFirsts.Remove(crew.ElementAt(i).name);
                recordedWorldFirsts++;
                numberOfWorldFirsts.Add(crew.ElementAt(i).name, recordedWorldFirsts);
                Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has achieved a World First");
            }
        }
        
        public Vessel MatchVesselToId(uint id)
        {
            for (int i = 0; i < FlightGlobals.Vessels.Count; i++)
            {
                Vessel v = FlightGlobals.Vessels.ElementAt(i);
                if (v.persistentId != id) continue;
                return v;
            }

            return null;
        }

        internal void OnVesselRecovered(ProtoVessel v)
        {
            Debug.Log("[FlightTracker]: onVesselRecovered Fired");
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (v.missionTime == 0)
            {
                Debug.Log("[FlightTracker]: " + v.vesselName + " hasn't gone anywhere. No credit will be awarded for this flight");
                return;
            }
            List<ProtoCrewMember> crew = v.GetVesselCrew();
            Debug.Log("[FlightTracker]: Processing " + crew.Count() + " Kerbals");
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
                if (flights.TryGetValue(p, out recovered)) flights.Remove(p);
                recovered = recovered + 1;
                double d = 0;
                if (met.TryGetValue(p, out d)) met.Remove(p);
                double missionTime = 0;
                double recordedLaunchTime;
                if (launchTime.TryGetValue(p, out recordedLaunchTime)) missionTime = Planetarium.GetUniversalTime() - recordedLaunchTime;
                else missionTime = v.missionTime;
                d = d + missionTime;
                flights.Add(p, recovered);
                met.Add(p, d);
                int recordedWorldFirsts;
                numberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out recordedWorldFirsts);
                Debug.Log("[FlightTracker]: Processed Recovery of " + p);
                Debug.Log("[FlightTracker]: " + p + " - Flights: " + recovered);
                Debug.Log("[FlightTracker]: " + p + " - Time Logged: " + (int)d);
                Debug.Log("[FlightTracker]: " + p + " - World Firsts Achieved: " + recordedWorldFirsts);
                onFlightTrackerUpdated.Fire(crew.ElementAt(i));
            }
        }
    }
}
