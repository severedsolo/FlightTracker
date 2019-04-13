using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;


namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, true)]
    public class ActiveFlightTracker : MonoBehaviour
    {
        public static ActiveFlightTracker instance;
        public static EventData<ProtoCrewMember> onFlightTrackerUpdated;
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
        private void Start()
        {
            GameEvents.onVesselRecovered.Add(onVesselRecovered);
            GameEvents.OnProgressComplete.Add(OnProgressComplete);
            GameEvents.OnVesselRollout.Add(OnVesselRollout);
            Debug.Log("[FlightTracker]: Registered Event Handlers");
        }

        public string ConvertUtToString(double time)
        {
            time = time / 60 / 60;
            time = (int)Math.Floor(time);
            string timeString = time.ToString();
            int stringLength = timeString.Count() - 3;
            if (time.ToString().Count() > 4) timeString = timeString.Substring(0, stringLength) + "k";
            return timeString;
        }

        public void EarnYourStripesUpgradePath(ConfigNode cn)
        {
            if (cn == null) return;
            ConfigNode[] loaded = cn.GetNodes("KERBAL");
            if (loaded.Count() == 0) return;
            for (int i = 0; i < loaded.Count(); i++)
            {
                ConfigNode temp = loaded.ElementAt(i);
                string s = temp.GetValue("Name");
                if (s == null) continue;
                flights.TryGetValue(s, out int currentInt);
                int.TryParse(temp.GetValue("Flights"), out int loadedInt);
                if (currentInt < loadedInt)
                {
                    flights.Remove(s);
                    flights.Add(s, loadedInt);
                }
                met.TryGetValue(s, out double currentDouble);
                double.TryParse(temp.GetValue("TimeLogged"), out double loadedDouble);
                if (currentDouble < loadedDouble)
                {
                    met.Remove(s);
                    met.Add(s, loadedDouble);
                }
                launchTime.TryGetValue(s, out currentDouble);
                double.TryParse(temp.GetValue("LaunchTime"), out loadedDouble);
                if (currentDouble < loadedDouble)
                {
                    launchTime.Remove(s);
                    launchTime.Add(s, loadedDouble);
                }
                bool.TryParse("WorldFirst", out bool b);
                if (b)
                {
                    numberOfWorldFirsts.Remove(s);
                    numberOfWorldFirsts.Add(s, 1);
                }
            }
            Debug.Log("[FlightTracker]: Successfully upgraded StripesData");
        }

        public int GetNumberOfFlights(string kerbalName)
        {
            flights.TryGetValue(kerbalName, out int i);
            return i;
        }

        public double GetRecordedMissionTimeSeconds(string kerbalName)
        {
            met.TryGetValue(kerbalName, out double d);
            return d;
        }

        public double GetRecordedMissionTimeHours(string kerbalName)
        {
            met.TryGetValue(kerbalName, out double d);
            d = d / 60 / 60;
            return d;
        }

        public double GetLaunchTime(string kerbalName)
        {
            launchTime.TryGetValue(kerbalName, out double d);
            return d;
        }

        public int GetNumberOfWorldFirsts(string kerbalName)
        {
            numberOfWorldFirsts.TryGetValue(kerbalName, out int i);
            return i;
        }

        private void OnVesselRollout(ShipConstruct data)
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
                if(!met.TryGetValue(p.name, out double d))met.Add(p.name, 0);
                Debug.Log("[FlightTracker]: "+p.name+" launched at "+Planetarium.GetUniversalTime());
            }
        }

        private void OnProgressComplete(ProgressNode data)
        {
            Debug.Log("[FlightTracker]: OnProgressComplete fired");
            if (FlightGlobals.ActiveVessel == null) return;
            List<ProtoCrewMember> crew = FlightGlobals.ActiveVessel.GetVesselCrew();
            Debug.Log("[FlightTracker]: Found " + crew.Count() + " potential candiates for World Firsts");
            if (crew.Count == 0) return;
            int recordedWorldFirsts = 0;
            for(int i = 0; i<crew.Count; i++)
            {
                if (!crew.ElementAt(i).flightLog.HasEntry(FlightLog.EntryType.Orbit))
                {
                    Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has not reached orbit yet and won't be given credit for this world first");
                    continue;
                }
                numberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out recordedWorldFirsts);
                numberOfWorldFirsts.Remove(crew.ElementAt(i).name);
                recordedWorldFirsts++;
                numberOfWorldFirsts.Add(crew.ElementAt(i).name, recordedWorldFirsts);
                Debug.Log("[FlightTracker]: " + crew.ElementAt(i).name + " has achieved a World First");
            }
        }

        private void onVesselRecovered(ProtoVessel v, bool data1)
        {
            Debug.Log("[FlightTracker]: onVesselRecovered Fired");
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
                if (launchTime.TryGetValue(p, out double recordedLaunchTime)) missionTime = Planetarium.GetUniversalTime() - recordedLaunchTime;
                else missionTime = v.missionTime;
                d = d + missionTime;
                flights.Add(p, recovered);
                met.Add(p, d);
                numberOfWorldFirsts.TryGetValue(crew.ElementAt(i).name, out int recordedWorldFirsts);
                Debug.Log("[FlightTracker]: Processed Recovery of " + p);
                Debug.Log("[FlightTracker]: " + p + " - Flights: " + recovered);
                Debug.Log("[FlightTracker]: " + p + " - Time Logged: " + (int)d);
                Debug.Log("[FlightTracker]: " + p + " - World Firsts Achieved: " + recordedWorldFirsts);
                onFlightTrackerUpdated.Fire(crew.ElementAt(i));
            }
        }

        private void OnDestroy()
        {
            GameEvents.onVesselRecovered.Remove(onVesselRecovered);
            GameEvents.OnProgressReached.Remove(OnProgressComplete);
            GameEvents.OnVesselRollout.Remove(OnVesselRollout);
            Debug.Log("[FlightTracker]: Unregistered Event Handlers");
        }
    }
}
