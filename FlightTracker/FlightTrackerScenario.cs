using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FlightTracker
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    internal class FlightTrackerScenario : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            int counter = 0;
            if (!KerbalTracker.Instance.Flights.Any()) return;
            node.RemoveNodes("KERBAL");
            foreach (KeyValuePair<string, int> v in KerbalTracker.Instance.Flights)
            {
                ConfigNode temp = new ConfigNode("KERBAL");
                temp.SetValue("Name", v.Key, true);
                temp.SetValue("Flights", v.Value, true);
                double d = 0;
                KerbalTracker.Instance.LaunchTime.TryGetValue(v.Key, out d);
                temp.SetValue("LaunchTime", d, true);
                if (KerbalTracker.Instance.KerbalFlightTime.TryGetValue(v.Key, out d)) temp.SetValue("TimeLogged", d, true);
                if (KerbalTracker.Instance.NumberOfWorldFirsts.TryGetValue(v.Key, out int i)) temp.SetValue("World Firsts", i, true);
                node.AddNode(temp);
                counter++;
            }
            Debug.Log("[FlightTracker]: Saved " + counter + " kerbals flight data");
            VesselTracker.Instance.OnSave(node);
        }

        public override void OnLoad(ConfigNode node)
        {
            int counter = 0;
            ConfigNode[] loaded = node.GetNodes("KERBAL");
            if (!loaded.Any()) return;
            KerbalTracker.Instance.Flights.Clear();
            KerbalTracker.Instance.KerbalFlightTime.Clear();
            KerbalTracker.Instance.NumberOfWorldFirsts.Clear();
            KerbalTracker.Instance.LaunchTime.Clear();
            for(int i = 0; i<loaded.Length;i++)
            {
                ConfigNode temp = loaded.ElementAt(i);
                string s = temp.GetValue("Name");
                if (s == null) continue;
                if (int.TryParse(temp.GetValue("Flights"), out int t)) KerbalTracker.Instance.Flights.Add(s, t);
                if (double.TryParse(temp.GetValue("TimeLogged"), out double d)) KerbalTracker.Instance.KerbalFlightTime.Add(s, d);
                double.TryParse(temp.GetValue("LaunchTime"), out d);
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (d != 0) KerbalTracker.Instance.LaunchTime.Add(s, d);
                if (int.TryParse(temp.GetValue("World Firsts"), out t)) KerbalTracker.Instance.NumberOfWorldFirsts.Add(s, t);
                counter++;
            }
            VesselTracker.Instance.OnLoad(node);
            Debug.Log("[FlightTracker]: Loaded " + counter + " kerbals flight data");
        }
    }
}
