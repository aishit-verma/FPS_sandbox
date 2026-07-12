using UnityEngine;

namespace PolyFrontlines.Data
{
    [CreateAssetMenu(fileName = "Weapon_", menuName = "PolyFrontlines/Weapon Definition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        public string weaponName = "Unnamed Weapon";

        [Header("Damage Zones")]
        public float damageNear = 25f;
        public float rangeNear = 20f;
        public float damageMid = 20f;
        public float rangeMid = 50f;
        public float damageFar = 15f;

        public float fireRate = 0.1f;
    }
}