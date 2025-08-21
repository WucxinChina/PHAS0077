namespace Gley.TrafficSystem
{
    public static class ExtensionMethods
    {
        public static float ToKMH(this float speedInMetersPerSecond)
        {
            return speedInMetersPerSecond * 3.6f;
        }

        public static float ToMPH(this float speedInMetersPerSecond)
        {
            return speedInMetersPerSecond * 2.23694f;
        }

        public static float KMHToMS(this float speedInKilometersPerHour)
        {
            return speedInKilometersPerHour / 3.6f;
        }
    }
}