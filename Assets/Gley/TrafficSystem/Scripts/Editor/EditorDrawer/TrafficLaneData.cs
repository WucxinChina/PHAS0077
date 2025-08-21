using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficLaneData : LaneEditorData<Road, WaypointSettings>
    {
        public TrafficLaneData(RoadEditorData<Road> roadData) : base(roadData)
        {
        }
    }
}