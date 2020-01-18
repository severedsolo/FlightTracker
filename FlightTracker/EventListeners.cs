using UnityEngine;

namespace FlightTracker
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class EventListeners : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(this);
        }

        private void Start()
        {
            GameEvents.onVesselRecovered.Add(OnVesselRecovered);
            GameEvents.OnProgressComplete.Add(OnProgressComplete);
            GameEvents.OnVesselRollout.Add(OnVesselRollout);
            Debug.Log("[FlightTracker]: Registered Event Handlers");
        }

        private void OnVesselRollout(ShipConstruct ship)
        {
            KerbalTracker.Instance.OnVesselRollout();
            VesselTracker.Instance.StartTrackingVessel();
        }

        private void OnProgressComplete(ProgressNode data)
        {
            KerbalTracker.Instance.RecordWorldFirst();
        }

        private void OnVesselRecovered(ProtoVessel recoveredVessel, bool data1)
        {
            KerbalTracker.Instance.ProcessKerbalRecovery(recoveredVessel);
        }


        private void OnDestroy()
        {
            GameEvents.onVesselRecovered.Remove(OnVesselRecovered);
            GameEvents.OnProgressReached.Remove(OnProgressComplete);
            GameEvents.OnVesselRollout.Remove(OnVesselRollout);
            Debug.Log("[FlightTracker]: Unregistered Event Handlers");
        }
    }
}