namespace Gley.TrafficSystem.Internal
{
    /// <summary>
    /// Controls the sound volume.
    /// </summary>
    public class SoundManager
    {
        private float _masterVolume;

        public float MasterVolume => _masterVolume;


        public SoundManager(float masterVolume)
        {
            _masterVolume = masterVolume;
        }


        /// <summary>
        /// Update engine volume of the vehicle
        /// </summary>
        /// <param name="volume"></param>
        public void UpdateMasterVolume(float volume)
        {
            _masterVolume = volume;
        }
    }
}