using UnityEngine;

namespace PolyFrontlines.Data
{
    // Lives once in the scene. Every machine has an identical copy of this
    // list (same build data), so syncing just an index over the network is
    // enough for everyone to resolve the same ClassDefinitionSO locally.
    public class ClassCatalog : MonoBehaviour
    {
        public static ClassCatalog Instance { get; private set; }

        [SerializeField] private ClassDefinitionSO[] classes; // 0 = Assault, 1 = Medic, 2 = Support

        private void Awake()
        {
            Instance = this;
        }

        public ClassDefinitionSO Get(int index)
        {
            if (index < 0 || index >= classes.Length) return null;
            return classes[index];
        }
    }
}