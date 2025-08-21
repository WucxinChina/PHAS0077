using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficRoadData : RoadEditorData<Road>
    {
        public override Road[] GetAllRoads()
        {
            return _allRoads;
        }
    }
}
