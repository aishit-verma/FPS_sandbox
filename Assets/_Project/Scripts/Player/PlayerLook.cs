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
        private bool _uiNeedsCursor;

        public void SetUiNeedsCursor(bool needsCursor)
        {
            _uiNeedsCursor = needsCursor;
            Cursor.lockState = needsCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                playerCamera.gameObject.SetActive(false);
                return;
            }

            // Class selection is open at spawn — cursor stays free until a
            // class is chosen (ClassSelectUI calls SetUiNeedsCursor(false)).
            SetUiNeedsCursor(true);
        }

        public void LockCursor()
        {
            SetUiNeedsCursor(false);
        }

        private void Update()
        {
            if (!IsOwner) return;

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                // Only allow click-to-relock when no menu actually needs
                // the cursor — otherwise clicking UI buttons would lock
                // the cursor and make the menu unclickable.
                if (!_uiNeedsCursor && Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch - mouseY, -pitchClamp, pitchClamp);
            playerCamera.transform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }
    }
}