using UnityEngine;
using Fusion;
using Fusion.Addons.SimpleKCC;
using Cinemachine;
using static UnityEngine.EventSystems.PointerEventData;
using UnityEditor.Rendering;
using System.Collections.Generic;

namespace FPS_personal_project
{
    /// <summary>
    /// Main player script which handles input processing, visuals.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class Player : NetworkBehaviour
    {
        [Header("Components")]
        public SimpleKCC KCC;
       public Weapons Weapons;
        public Health Health;
        public Animator Animator;
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

        private bool doubleJump;

        // private SceneObjects _sceneObjects;

        public void PlayFireEffect()
        {
           /* // Player fire animation (hands) is not played when strafing because we lack a proper
            // animation and we do not want to make the animation controller more complex
            if (Mathf.Abs(GetAnimationMoveVelocity().x) > 0.2f)
                return;*/

            Animator.SetTrigger("Fire");
        }

        public override void Spawned()
        {
            name = $"{Object.InputAuthority} ({(HasInputAuthority ? "Input Authority" : (HasStateAuthority ? "State Authority" : "Proxy"))})";

            // Enable first person visual for local player, third person visual for proxies.
            SetFirstPersonVisuals(HasInputAuthority);

            if (HasInputAuthority == false)
            {
                // Virtual cameras are enabled only for local player.
                var Cameras = GetComponentsInChildren<CinemachineVirtualCamera>(true);
                for (int i = 0; i < Cameras.Length; i++)
                {
                    Cameras[i].enabled = false;
                }
            }

            if(HasInputAuthority)
            {
                Camera[] Cameras = Camera.allCameras;
                foreach (Camera cam in Cameras)
                {
                    if (cam.gameObject.tag == "PlayerCamera")
                    {
                        cam.enabled = true;
                        continue;
                    }

                    cam.enabled = false;
                }

            }

            //_sceneObjects = Runner.GetSingleton<SceneObjects>();
        }
       
        public override void FixedUpdateNetwork()
        {
            Debug.LogWarning(Health.IsAlive);
           

           /* if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
            {
                // After gameplay is finished we still want the player to finish movement and not stuck in the air.
                MovePlayer();
                return;
            }

            if (Health.IsAlive == false)
            {
                // We want dead body to finish movement - fall to ground etc.
                MovePlayer();

                // Disable physics casts and collisions with other players.
                KCC.SetColliderLayer(LayerMask.NameToLayer("Ignore Raycast"));
                KCC.SetCollisionLayerMask(LayerMask.GetMask("Default"));

                HitboxRoot.HitboxRootActive = false;

                // Force enable third person visual for local player.
                SetFirstPersonVisuals(false);
                return;
            }*/


            //Debug.Log("Player.cs fixed update network");
            if (GetInput(out NetworkedInput input))
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
            /*if (_sceneObjects.Gameplay.State == EGameplayState.Finished)
                return;

            var moveVelocity = GetAnimationMoveVelocity();

            // Set animation parameters.
            Animator.SetFloat("LocomotionTime", Time.time * 2f);
            Animator.SetBool("IsAlive", Health.IsAlive);
            Animator.SetBool("IsGrounded", KCC.IsGrounded);
            Animator.SetBool("IsReloading", Weapons.CurrentWeapon.IsReloading);
            Animator.SetFloat("MoveX", moveVelocity.x, 0.05f, Time.deltaTime);
            Animator.SetFloat("MoveZ", moveVelocity.z, 0.05f, Time.deltaTime);
            Animator.SetFloat("MoveSpeed", moveVelocity.magnitude);
            Animator.SetFloat("Look", -KCC.GetLookRotation(true, false).x / 90f);

            if (Health.IsAlive == false)
            {
                // Disable UpperBody (override) and Look (additive) layers. Death animation is full-body.

                int upperBodyLayerIndex = Animator.GetLayerIndex("UpperBody");
                Animator.SetLayerWeight(upperBodyLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(upperBodyLayerIndex) - Time.deltaTime));

                int lookLayerIndex = Animator.GetLayerIndex("Look");
                Animator.SetLayerWeight(lookLayerIndex, Mathf.Max(0f, Animator.GetLayerWeight(lookLayerIndex) - Time.deltaTime));
            }

            if (_visibleJumpCount < _jumpCount)
            {
                Animator.SetTrigger("Jump");

                JumpSound.clip = JumpClips[Random.Range(0, JumpClips.Length)];
                JumpSound.Play();
            }

            _visibleJumpCount = _jumpCount;
*/







        }

        private void LateUpdate()
        {
            if (HasInputAuthority == false)
                return;

            RefreshCamera();
        }

        private void ProcessInput(NetworkedInput input)
        {
            // Processing input - look rotation, jump, movement, weapon fire, weapon switching, weapon reloading, spray decal.

            KCC.AddLookRotation(input.LookRotationDelta, -89f, 89f);

            // It feels better when player falls quicker
            KCC.SetGravity(KCC.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

            var inputDirection = KCC.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            var jumpImpulse = 0f;
           
            if (input.Buttons.WasPressed(_previousButtons, EInputButton.Jump))
            {
                if (KCC.IsGrounded)
                {
                    jumpImpulse = JumpForce;
                    doubleJump = true;
                }
                    
                else if (!KCC.IsGrounded && doubleJump)
                {
                    jumpImpulse = JumpForce;
                    doubleJump = false;
                }

                   
                
                
            }
            
            

            MovePlayer(inputDirection * MoveSpeed, jumpImpulse);
            RefreshCamera();

            if (KCC.HasJumped)
            {
                _jumpCount++;
            }

            if (input.Buttons.IsSet(EInputButton.Fire))
            {
                bool justPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
               Weapons.Fire(justPressed);
               // Health.StopImmortality();
            }
            else if (input.Buttons.IsSet(EInputButton.Reload))
            {
                Weapons.Reload();
            }

            // Store input buttons when the processing is done - next tick it is compared against current input buttons.
            _previousButtons = input.Buttons;
        }

        private void MovePlayer(Vector3 desiredMoveVelocity = default, float jumpImpulse = default)
        {
            float acceleration = 1f;
            if(KCC.IsGrounded)
                doubleJump = true;
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

        private Vector3 GetAnimationMoveVelocity()
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
        }
    }
}
