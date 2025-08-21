namespace Gley.TrafficSystem.Internal
{
    public static class WaypointEvents
    {
        /// <summary>
        /// Triggered to change the stop value of the waypoint
        /// </summary>
        /// <param name="waypointIndex"></param>
        public delegate void TrafficLightChanged(int waypointIndex, bool stop);
        public static event TrafficLightChanged OnTrafficLightChanged;
        public static void TriggerTrafficLightChangedEvent(int waypointIndex, bool stop)
        {
            OnTrafficLightChanged?.Invoke(waypointIndex, stop);
        }


        /// <summary>
        /// Triggered to notify vehicle about stop state and give way state of the waypoint
        /// </summary>
        /// <param name="vehicleIndex">vehicle index</param>
        /// <param name="stopState">stop in point needed</param>
        /// <param name="giveWayState">give way needed</param>
        public delegate void StopStateChanged(int waypointIndex, bool stopState);
        public static event StopStateChanged OnStopStateChanged;
        public static void TriggerStopStateChangedEvent(int waypointIndex, bool stopState)
        {
            OnStopStateChanged?.Invoke(waypointIndex, stopState);
        }


        public delegate void GiveWayStateChanged(int waypointIndex, GiveWayType giveWayType);
        public static event GiveWayStateChanged OnGiveWayStateChanged;
        public static void TriggerGiveWayStateChangedEvent(int waypointIndex, GiveWayType giveWayState)
        {
            OnGiveWayStateChanged?.Invoke(waypointIndex, giveWayState);
        }
    }
}