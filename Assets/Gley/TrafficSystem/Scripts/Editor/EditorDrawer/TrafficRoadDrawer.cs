using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficRoadDrawer : RoadDrawer<TrafficRoadData, Road>
    {
        public TrafficRoadDrawer (TrafficRoadData data):base(data) 
        {
        }
    }
}
