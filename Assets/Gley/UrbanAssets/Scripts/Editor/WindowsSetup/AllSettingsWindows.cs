using System.Linq;

namespace Gley.UrbanSystem.Editor
{
    public class AllSettingsWindows
    {
        private WindowProperties[] _allWindows;


        public void Initialize(WindowProperties[] allWindowsProperties)
        {
            _allWindows = allWindowsProperties;
        }


        public WindowProperties GetWindowProperties(string className)
        {
            return _allWindows.First(cond => cond.ClassName == className);
        }


        public string GetWindowName(string className)
        {
            return GetWindowProperties(className).Title;
        }
    }
}