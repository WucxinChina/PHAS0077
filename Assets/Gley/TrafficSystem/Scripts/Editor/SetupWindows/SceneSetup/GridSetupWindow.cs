using Gley.UrbanSystem.Editor;

namespace Gley.TrafficSystem.Editor
{
    public class GridSetupWindow : GridSetupWindowBase
    {
        public override void DrawInScene()
        {
            if (_viewGrid)
            {
                _gridDrawer.DrawGrid(true);
            }
            base.DrawInScene();
        }
    }
}
