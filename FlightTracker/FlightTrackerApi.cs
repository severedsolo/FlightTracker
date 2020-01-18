using System;
using System.Globalization;
using JetBrains.Annotations;
using UnityEngine;

namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class FlightTrackerApi : MonoBehaviour
    {
        public static EventData<ProtoCrewMember> OnFlightTrackerUpdated;
        
        [PublicAPI]
        public static FlightTrackerApi Instance;
        
        private void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
            OnFlightTrackerUpdated = new EventData<ProtoCrewMember>("onFlightTrackerUpdated");    
        }

        [PublicAPI]
        public double VesselRealMet(uint id)
        {
            if(VesselTracker.Instance.ActualLaunchTime.TryGetValue(id, out double d)) return d;
            return VesselTracker.Instance.MatchVesselToId(id).missionTime;
        }
        
        
        [PublicAPI]
        public double VesselRealMet(Vessel v)
        {
            if(VesselTracker.Instance.ActualLaunchTime.TryGetValue(v.persistentId, out double d)) return d;
            return v.missionTime;
        }
        
        [PublicAPI]
        public string ConvertUtToString(double time)
        {
            time = time / 60 / 60;
            time = (int)Math.Floor(time);
            string timeString = time.ToString(CultureInfo.CurrentCulture);
            int stringLength = timeString.Length - 3;
            if (time.ToString(CultureInfo.CurrentCulture).Length > 4) timeString = timeString.Substring(0, stringLength) + "k";
            return timeString;
        }

        [PublicAPI]
        public int GetNumberOfFlights(string kerbalName)
        {
            KerbalTracker.Instance.Flights.TryGetValue(kerbalName, out int i);
            return i;
        }
        [PublicAPI]
        public double GetRecordedMissionTimeSeconds(string kerbalName)
        {
            KerbalTracker.Instance.KerbalFlightTime.TryGetValue(kerbalName, out double d);
            return d;
        }
        [PublicAPI]
        public double GetRecordedMissionTimeHours(string kerbalName)
        {
            KerbalTracker.Instance.KerbalFlightTime.TryGetValue(kerbalName, out double d);
            d = d / 60 / 60;
            return d;
        }
        [PublicAPI]
        public double GetLaunchTime(string kerbalName)
        {
            KerbalTracker.Instance.LaunchTime.TryGetValue(kerbalName, out double d);
            return d;
        }
        [PublicAPI]
        public int GetNumberOfWorldFirsts(string kerbalName)
        {
            KerbalTracker.Instance.NumberOfWorldFirsts.TryGetValue(kerbalName, out int i);
            return i;
        }
    }
}