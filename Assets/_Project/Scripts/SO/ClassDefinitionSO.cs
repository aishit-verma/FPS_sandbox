using UnityEngine;
using PolyFrontlines.Data;

namespace PolyFrontlines.Data
{
    [CreateAssetMenu(fileName = "Class_", menuName = "PolyFrontlines/Class Definition")]
    public class ClassDefinitionSO : ScriptableObject
    {
        public string className = "Unnamed Class";
        public WeaponDefinitionSO primaryWeapon;
        public float maxHealth = 100f;
        public float moveSpeed = 5f;
    }
}