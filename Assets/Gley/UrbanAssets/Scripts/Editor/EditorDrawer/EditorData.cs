using UnityEngine;

namespace Gley.UrbanSystem.Editor
{
    public abstract class EditorData 
    {
        private readonly bool _showDebugMessages = false;

        public delegate void Modified();
        public event Modified OnModified;
        public void TriggerOnModifiedEvent()
        {
            if (_showDebugMessages)
            {
                Debug.Log("Modified " + this);
            }
            LoadAllData();
            OnModified?.Invoke();
        }


        protected abstract void LoadAllData();


        protected EditorData()
        {
            if (_showDebugMessages)
            {
                Debug.Log("EditorData " + this);
            }
        }
    }
}