using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Cinemachine;
using static UnityEngine.EventSystems.PointerEventData;

namespace SimpleFPS
{
    /// <summary>
    /// Main player script which handles input processing, visuals.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class Player : NetworkBehaviour
    {
        [Header("Components")]
        public SimpleKCC KCC;
       /* public Weapons Weapons;
        public Health Health;*/
        //public Animator Animator;
        public HitboxRoot HitboxRoot;

        [Header("Setup")]
        public float MoveSpeed = 6f;
        public float JumpForce = 10f;
        public AudioSource JumpSound;
        public AudioClip[] JumpClips;
        public Transform CameraHandle;
        public GameObject FirstPersonRoot;
        public GameObject ThirdPersonRoot;
        public NetworkObject SprayPrefab;

        [Header("Movement")]
        public float UpGravity = 15f;
        public float DownGravity = 25f;
        public float GroundAcceleration = 55f;
        public float GroundDeceleration = 25f;
        public float AirAcceleration = 25f;
        public float AirDeceleration = 1.3f;

        [Networked]
        private NetworkButtons _previousButtons { get; set; }
        [Networked]
        private int _jumpCount { get; set; }
        [Networked]
        private Vector3 _moveVelocity { get; set; }

        private int _visibleJumpCount;

       // private SceneObjects _sceneObjects;

       

        public override void Spawned()
        {
            name = $"{Object.InputAuthority} ({(HasInputAuthority ? "Input Authority" : (HasStateAuthority ? "State Authority" : "Proxy"))})";

            // Enable first person visual for local player, third person visual for proxies.
            SetFirstPersonVisuals(HasInputAuthority);

            if (HasInputAuthority == false)
            {
                // Virtual cameras are enabled only for local player.
                var Cameras = GetComponentsInChildren<Camera>(true);
                for (int i = 0; i < Cameras.Length; i++)
                {
                    Cameras[i].enabled = false;
                }
            }

            //_sceneObjects = Runner.GetSingleton<SceneObjects>();
        }

        public override void FixedUpdateNetwork()
        {



            Debug.Log("Player.cs fixed update network");
            if (GetInput(out NetInput input))
            {
                // Input is processed on InputAuthority and StateAuthority.
                ProcessInput(input);
            }
            else
            {
                // When no input is available, at least continue with movement (e.g. falling).
                MovePlayer();
                RefreshCamera();
            }
        }

        public override void Render()
        {
           

           

           

            

           
        }

        private void LateUpdate()
        {
            if (HasInputAuthority == false)
                return;

            RefreshCamera();
        }

        private void ProcessInput(NetInput input)
        {
            // Processing input - look rotation, jump, movement, weapon fire, weapon switching, weapon reloading, spray decal.

            KCC.AddLookRotation(input.LookRotationDelta, -89f, 89f);

            // It feels better when player falls quicker
            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

            var inputDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            var jumpImpulse = 0f;

            if (input.Buttons.WasPressed(_previousButtons, InputButton.Jump) && KCC.IsGrounded)
            {
                jumpImpulse = JumpForce;
            }

            MovePlayer(inputDirection * MoveSpeed, jumpImpulse);
            RefreshCamera();

            if (KCC.HasJumped)
            {
                _jumpCount++;
            }


            // Store input buttons when the processing is done - next tick it is compared against current input buttons.
            _previousButtons = input.Buttons;
        }

        private void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = default)
        {
            float acceleration = 1f;

            if (desiredMoveVelocity == Vector3.zero)
            {
                // No desired move velocity - we are stopping.
                acceleration = KCC.IsGrounded == true ? GroundDeceleration : AirDeceleration;
            }
            else
            {
                acceleration = KCC.IsGrounded == true ? GroundAcceleration : AirAcceleration;
            }

            _moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, acceleration * Runner.DeltaTime);
            KCC.Move(_moveVelocity, jumpImpulse);
        }

        private void RefreshCamera()
        {
            // Camera is set based on KCC look rotation.
            Vector2 pitchRotation = KCC.GetLookRotation(true, false);
            CameraHandle.localRotation = Quaternion.Euler(pitchRotation);
        }

        private void SetFirstPersonVisuals(bool firstPerson)
        {
            FirstPersonRoot.SetActive(firstPerson);
            ThirdPersonRoot.SetActive(firstPerson == false);
        }

       /* private Vector3 GetAnimationMoveVelocity()
        {
            if (KCC.RealSpeed < 0.01f)
                return default;

            var velocity = KCC.RealVelocity;

            // We only care about X an Z directions.
            velocity.y = 0f;

            if (velocity.sqrMagnitude > 1f)
            {
                velocity.Normalize();
            }

            // Transform velocity vector to local space.
            return transform.InverseTransformVector(velocity);
        }*/
    }
}
