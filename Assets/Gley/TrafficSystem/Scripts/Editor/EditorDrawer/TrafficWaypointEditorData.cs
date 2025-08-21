using Gley.TrafficSystem.Internal;
using Gley.UrbanSystem.Editor;
using Gley.UrbanSystem.Internal;
using System.Collections.Generic;

namespace Gley.TrafficSystem.Editor
{
    public class TrafficWaypointEditorData : EditorData
    {
        private WaypointSettings[] _allWaypoints;
        private WaypointSettings[] _disconnectedWaypoints;
        private WaypointSettings[] _vehicleEditedWaypoints;
        private WaypointSettings[] _speedEditedWaypoints;
        private WaypointSettings[] _priorityEditedWaypoints;
        private WaypointSettings[] _giveWayWaypoints;
        private WaypointSettings[] _complexGiveWayWaypoints;
        private WaypointSettings[] _zipperGiveWayWaypoints;
        private WaypointSettings[] _eventWaypoints;
        private WaypointSettings[] _penaltyEditedWaypoints;
        private WaypointSettings[] _allNonConnectionWaypoints;

        public TrafficWaypointEditorData()
        {
            LoadAllData();
        }

        public WaypointSettings[] GetAllWaypoints()
        {
            return _allWaypoints;
        }


        public WaypointSettings[] GetDisconnectedWaypoints()
        {
            return _disconnectedWaypoints;
        }


        public WaypointSettings[] GetVehicleEditedWaypoints()
        {
            return _vehicleEditedWaypoints;
        }


        public WaypointSettings[] GetSpeedEditedWaypoints()
        {
            return _speedEditedWaypoints;
        }


        public WaypointSettings[] GetPriorityEditedWaypoints()
        {
            return _priorityEditedWaypoints;
        }


        public WaypointSettings[] GetGiveWayWaypoints()
        {
            return _giveWayWaypoints;
        }


        public WaypointSettings[] GetComplexGiveWayWaypoints()
        {
            return _complexGiveWayWaypoints;
        }


        public WaypointSettings[] GetZipperGiveWayWaypoints()
        {
            return _zipperGiveWayWaypoints;
        }


        public WaypointSettings[] GetEventWaypoints()
        {
            return _eventWaypoints;
        }


        public WaypointSettings[] GetPenlatyEditedWaypoints()
        {
            return _penaltyEditedWaypoints;
        }

        public WaypointSettings[] GatNonConnectionWaypoints()
        {
            return _allNonConnectionWaypoints;
        }


        protected override void LoadAllData()
        {
            _allWaypoints = GleyPrefabUtilities.GetAllComponents<WaypointSettings>();

            List<WaypointSettings> disconnectedWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> vehicleEditedWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> speedEditedWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> priorityEditedWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> giveWayWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> complexGiveWayWaypoints = new List<WaypointSettings> ();
            List<WaypointSettings> zipperGiveWayWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> eventWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> penaltyEditedWaypoints = new List<WaypointSettings>();
            List<WaypointSettings> allNonConnectionWaypoints = new List<WaypointSettings>();

            //initialization and checks
            for (int i = 0; i < _allWaypoints.Length; i++)
            {
                _allWaypoints[i].position = _allWaypoints[i].transform.position;
                _allWaypoints[i].VerifyAssignments(false);


                if (_allWaypoints[i].neighbors.Count == 0)
                {
                    disconnectedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].carsLocked)
                {
                    vehicleEditedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].speedLocked)
                {
                    speedEditedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].priorityLocked)
                {
                    priorityEditedWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].giveWay)
                {
                    giveWayWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].complexGiveWay)
                {
                    complexGiveWayWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].zipperGiveWay)
                {
                    zipperGiveWayWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].triggerEvent)
                {
                    eventWaypoints.Add(_allWaypoints[i]);
                }

                if (_allWaypoints[i].penaltyLocked)
                {
                    penaltyEditedWaypoints.Add(_allWaypoints[i]);
                }

                if (!_allWaypoints[i].name.Contains(UrbanSystemConstants.Connect))
                {
                    allNonConnectionWaypoints.Add(_allWaypoints[i]);
                }
            }
            _disconnectedWaypoints = disconnectedWaypoints.ToArray();
            _vehicleEditedWaypoints = vehicleEditedWaypoints.ToArray();
            _speedEditedWaypoints = speedEditedWaypoints.ToArray();
            _priorityEditedWaypoints = priorityEditedWaypoints.ToArray();
            _giveWayWaypoints = giveWayWaypoints.ToArray();
            _complexGiveWayWaypoints = complexGiveWayWaypoints.ToArray();
            _zipperGiveWayWaypoints = zipperGiveWayWaypoints.ToArray();
            _eventWaypoints = eventWaypoints.ToArray();
            _penaltyEditedWaypoints = penaltyEditedWaypoints.ToArray();
            _allNonConnectionWaypoints = allNonConnectionWaypoints.ToArray();
        }
    }
}