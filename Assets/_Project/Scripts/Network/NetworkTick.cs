using UnityEngine;

namespace PolyFrontlines.Networking.Movement
{
    
    public class NetworkTick : MonoBehaviour
    {
        public const float TickInterval = 1f / 64f;

        public static int Current { get; private set; }

        private void Awake()
        {
            Time.fixedDeltaTime = TickInterval;
        }

        private void FixedUpdate()
        {
            Current++;
        }
    }
}