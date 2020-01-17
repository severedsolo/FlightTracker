using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FlightTracker
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, GameScenes.SPACECENTER, GameScenes.FLIGHT, GameScenes.TRACKSTATION)]
    class FlightTrackerScenario : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            int counter = 0;
            if (!ActiveFlightTracker.instance.flights.Any()) return;
            node.RemoveNodes("KERBAL");
            foreach (var v in ActiveFlightTracker.instance.flights)
            {
                ConfigNode temp = new ConfigNode("KERBAL");
                temp.SetValue("Name", v.Key, true);
                temp.SetValue("Flights", v.Value, true);
                double d = 0;
                ActiveFlightTracker.instance.launchTime.TryGetValue(v.Key, out d);
                temp.SetValue("LaunchTime", d, true);
                if (ActiveFlightTracker.instance.met.TryGetValue(v.Key, out d)) temp.SetValue("TimeLogged", d, true);
                int i;
                if (ActiveFlightTracker.instance.numberOfWorldFirsts.TryGetValue(v.Key, out i)) temp.SetValue("World Firsts", i, true);
                node.AddNode(temp);
                counter++;
            }
            Debug.Log("[FlightTracker]: Saved " + counter + " kerbals flight data");
        }

        public override void OnLoad(ConfigNode node)
        {
            int counter = 0;
            ConfigNode[] loaded = node.GetNodes("KERBAL");
            if (!loaded.Any()) return;
            ActiveFlightTracker.instance.flights.Clear();
            ActiveFlightTracker.instance.met.Clear();
            ActiveFlightTracker.instance.numberOfWorldFirsts.Clear();
            ActiveFlightTracker.instance.launchTime.Clear();
            for(int i = 0; i<loaded.Count();i++)
            {
                ConfigNode temp = loaded.ElementAt(i);
                string s = temp.GetValue("Name");
                if (s == null) continue;
                int t;
                if (Int32.TryParse(temp.GetValue("Flights"), out t)) ActiveFlightTracker.instance.flights.Add(s, t);
                double d;
                if (Double.TryParse(temp.GetValue("TimeLogged"), out d)) ActiveFlightTracker.instance.met.Add(s, d);
                Double.TryParse(temp.GetValue("LaunchTime"), out d);
                if (d != 0) ActiveFlightTracker.instance.launchTime.Add(s, d);
                if (Int32.TryParse(temp.GetValue("World Firsts"), out t)) ActiveFlightTracker.instance.numberOfWorldFirsts.Add(s, t);
                counter++;
            }
            Debug.Log("[FlightTracker]: Loaded " + counter + " kerbals flight data");
        }
    }
}
