using UnityEngine;

namespace Gley.TrafficSystem
{
    public class PlayerRightRear : MonoBehaviour
    {
        public PlayerComponent player;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void OnTriggerEnter(Collider other)
        {

            var aiCar = other.GetComponentInParent<VehicleComponent>();
            if (aiCar != null && !player._rightRearAIVehicles.Contains(aiCar))
            {
                player._rightRearAIVehicles.Add(aiCar);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var aiCar = other.GetComponentInParent<VehicleComponent>();

            if (aiCar != null && player._rightRearAIVehicles.Contains(aiCar))
            {
                aiCar.RemoveFrontMergeAssistTarget();
                player._rightRearAIVehicles.Remove(aiCar);
            }

        }
    }
}
