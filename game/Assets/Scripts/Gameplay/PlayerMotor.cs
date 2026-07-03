using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BadFaith.Gameplay
{
    /// <summary>
    /// Contrôleur FPS J0, autorité au propriétaire (le NetworkTransform du prefab
    /// est en client-authoritative). Utilise le new Input System : les touches
    /// sont identifiées par position physique, donc WASD marche tel quel en AZERTY (ZQSD).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _sprintMultiplier = 1.6f;
        [SerializeField] private float _jumpHeight = 1.1f;
        [SerializeField] private float _gravity = -18f;
        [SerializeField] private float _lookSensitivity = 0.12f;
        [SerializeField] private Transform _cameraHolder;

        private CharacterController _controller;
        private float _pitch;
        private float _verticalVelocity;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!IsOwner)
                return;

            // Coupe la caméra de scène et active la nôtre.
            Camera sceneCam = Camera.main;
            Camera ownCam = _cameraHolder.GetComponentInChildren<Camera>(true);
            if (sceneCam != null && ownCam != null && sceneCam != ownCam)
                sceneCam.gameObject.SetActive(false);
            if (ownCam != null)
                ownCam.gameObject.SetActive(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (!IsOwner)
                return;

            // Mort : plus de déplacement, mais on peut encore regarder autour (et écouter…).
            var health = GetComponent<PlayerHealth>();
            bool dead = health != null && health.IsDead;

            Keyboard kb = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (kb == null || mouse == null)
                return;

            // Échap libère le curseur (pour cliquer le HUD), clic le re-capture.
            if (kb.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Vector2 look = mouse.delta.ReadValue() * _lookSensitivity;
                transform.Rotate(0f, look.x, 0f);
                _pitch = Mathf.Clamp(_pitch - look.y, -85f, 85f);
                _cameraHolder.localEulerAngles = new Vector3(_pitch, 0f, 0f);
            }

            if (dead || !_controller.enabled)
                return;

            Vector2 input = Vector2.zero;
            if (kb.wKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            float speed = _moveSpeed * (kb.leftShiftKey.isPressed ? _sprintMultiplier : 1f);
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -1f;
                if (kb.spaceKey.wasPressedThisFrame)
                    _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }
            move.y = _verticalVelocity;

            _controller.Move(move * Time.deltaTime);
        }
    }
}
