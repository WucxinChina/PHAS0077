namespace Gley.TrafficSystem
{
    public class DefaultVehicleBehaviours : IBehaviourList
    {
        public VehicleBehaviour[] GetBehaviours()
        {
            return new VehicleBehaviour[]
            {
                new Stop(),
                new TempStop(),
                new AvoidReverse(),
                new StopInDistance(),
                new StopInPoint(),
                new GiveWay(),
                new Overtake(),
                new FollowVehicle(),
                new Decelerate(),
                new NoWaypoints(),
                new Forward(),
                new FollowPlayer(),
                new OvertakePlayer(),
                new ChangeLane(),
                new DriveOnSide(),
                new SlowDownAndStop(),
                new CurveSlowDown(),
                new Reverse(),
                new ClearPath(),
                new IgnoreTrafficRules(),
            };
        }
    }
}