namespace Gley.TrafficSystem.Internal
{
    // Helping class for crossing the road.
    public class PedestrianCrossing
    {
        public int PedestrianIndex { get; }
        public int Road { get; }
        public bool Crossing { get; set; }

        public PedestrianCrossing(int pedestrianIndex, int road)
        {
            PedestrianIndex = pedestrianIndex;
            Road = road;
            Crossing = false;
        }
    }
}