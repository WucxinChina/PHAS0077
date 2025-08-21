using UnityEngine;

namespace Gley.TrafficSystem
{
    public class PlayerRightFront : MonoBehaviour
    {
        public PlayerComponent player;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnTriggerEnter(Collider other)
        {

            var aiCar = other.GetComponentInParent<VehicleComponent>();
            if (aiCar != null && !player._rightFrontAIVehicles.Contains(aiCar))
            {
                player._rightFrontAIVehicles.Add(aiCar);
            }

        }

        private void OnTriggerExit(Collider other)
        {
            var aiCar = other.GetComponentInParent<VehicleComponent>();

            if (aiCar != null && player._rightFrontAIVehicles.Contains(aiCar))
            {
                aiCar.RemoveBehindMergeAssistTarget();
                player._rightFrontAIVehicles.Remove(aiCar);
            }

        }
    }
}
