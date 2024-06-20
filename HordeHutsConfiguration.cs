using Rocket.API;

namespace Zombs_R_Cute_Horde_Huts
{
    public class HordeHutsConfiguration:IRocketPluginConfiguration
    {
        public float TimeToUnlockClosedDoor;  
        public float TimeToUnlockOpenedDoor;

        public void LoadDefaults()
        {
            TimeToUnlockOpenedDoor = 15;
            TimeToUnlockClosedDoor = 600;
        }
    }
}