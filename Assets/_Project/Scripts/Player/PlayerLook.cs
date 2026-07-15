using Unity.Netcode;
using UnityEngine;

namespace PolyFrontlines.Gameplay.Player
{
    public class PlayerLook : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float pitchClamp = 80f;

        private float _pitch;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                return;
            }

            Cursor.lockState = CursorLockMode.None;
        }

        public void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            if (!IsOwner) return;

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch - mouseY, -pitchClamp, pitchClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }
    }
}