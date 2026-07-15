using UnityEngine;

namespace PolyFrontlines.Data
{
    [CreateAssetMenu(fileName = "Grenade_", menuName = "PolyFrontlines/Grenade Definition")]
    public class GrenadeDefinitionSO : ScriptableObject
    {
        public float damage = 80f;
        public float radius = 5f;
        public float fuseTime = 3f;
        public float throwForce = 15f;
    }
}