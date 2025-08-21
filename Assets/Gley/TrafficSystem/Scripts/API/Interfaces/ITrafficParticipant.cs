using UnityEngine;

namespace Gley.TrafficSystem
{
    /// <summary>
    /// Each traffic participant outside Traffic System should implement this interface so the traffic cars could overtake it.
    /// </summary>
    public interface ITrafficParticipant
    {
        bool AlreadyCollidingWith(Collider[] allColliders);
        public float GetCurrentSpeedMS();
        public Vector3 GetHeading();
    }
}