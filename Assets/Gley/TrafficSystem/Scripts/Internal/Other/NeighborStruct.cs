namespace Gley.TrafficSystem.Internal
{
    public struct NeighborStruct
    {
        public int WaypointIndex;
        public int Angle;

        public NeighborStruct(int waypointIndex, int angle)
        {
            WaypointIndex = waypointIndex;
            Angle = angle;
        }
    }
}