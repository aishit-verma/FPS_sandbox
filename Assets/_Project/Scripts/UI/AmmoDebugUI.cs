using TMPro;
using UnityEngine;
using PolyFrontlines.Core;

namespace PolyFrontlines.Gameplay.UI
{
    // Temporary — just for verifying ammo/reload/resupply visually.
    // Replace with the real HUD once ready; the events it listens to are
    // permanent, this display is not.
    public class AmmoDebugUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text ammoText;
        [SerializeField] private TMP_Text grenadeText;

        private void OnEnable()
        {
            EventBus<AmmoChangedEvent>.Subscribe(OnAmmoChanged);
            EventBus<GrenadeCountChangedEvent>.Subscribe(OnGrenadeCountChanged);
        }

        private void OnDisable()
        {
            EventBus<AmmoChangedEvent>.Unsubscribe(OnAmmoChanged);
            EventBus<GrenadeCountChangedEvent>.Unsubscribe(OnGrenadeCountChanged);
        }

        private void OnAmmoChanged(AmmoChangedEvent e)
        {
            ammoText.text = $"Ammo: {e.MagazineAmmo} / {e.ReserveAmmo}";
        }

        private void OnGrenadeCountChanged(GrenadeCountChangedEvent e)
        {
            grenadeText.text = $"Grenades: {e.CurrentGrenades} / {e.MaxGrenades}";
        }
    }
}