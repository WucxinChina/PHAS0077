using Gley.UrbanSystem.Editor;
using UnityEditor;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficSetupWindow : SetupWindowBase
    {
        protected TrafficSettingsWindowData _editorSave;


        public override SetupWindowBase Initialize(WindowProperties windowProperties, SettingsWindowBase window)
        {
            base.Initialize(windowProperties, window);
            _editorSave = new SettingsLoader(TrafficSystemConstants.windowSettingsPath).LoadSettingsAsset<TrafficSettingsWindowData>();
            return this;
        }


        public override void DestroyWindow()
        {
            EditorUtility.SetDirty(_editorSave);
            base.DestroyWindow();
        }
    }
}