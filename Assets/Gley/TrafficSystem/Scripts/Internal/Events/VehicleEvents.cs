namespace Gley.TrafficSystem.Internal
{
    public static class VehicleEvents
    {
        public delegate void ObstacleInTriggerAdded(int vehicleIndex, Obstacle newObstacle);
        public static event ObstacleInTriggerAdded OnObstacleInTriggerAdded;
        public static void TriggerObstacleInTriggerAddedEvent(int vehicleIndex, Obstacle newObstacle)
        {
            OnObstacleInTriggerAdded?.Invoke(vehicleIndex, newObstacle);
        }


        public delegate void ObstacleInTriggerRemoved(int vehicleIndex, Obstacle obstacleToRemove);
        public static event ObstacleInTriggerRemoved OnObstacleInTriggerRemoved;
        public static void TriggerObstacleInTriggerRemovedEvent(int vehicleIndex, Obstacle obstacleToRemove)
        {
            if (OnObstacleInTriggerRemoved != null)
            {
                OnObstacleInTriggerRemoved(vehicleIndex, obstacleToRemove);
            }
        }
    }
}