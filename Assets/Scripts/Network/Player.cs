using UnityEngine;
using Fusion;
//using Fusion.Addons.SimpleKCC;
using Fusion.Addons.KCC;
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
        public KCC KCC;
        public Weapons Weapons;
        public Health Health;
        public Animator ThirdPersonAnimator;
        public Animator ThirdPersonWeaponAnimator;
        public Animator FirstPersonAnimator;
        public Animator FirstPersonWeaponAnimator;
        public HitboxRoot HitboxRoot;

        [Header("Setup")]
        public float MoveSpeed = 6f;
        public float JumpForce = 10f;
        public AudioSource JumpSound;
        public AudioClip[] JumpClips;
        public Transform CameraHandle;
        public float MaxCameraPitch = 89f;
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

        private bool _isMoving = false;
        private bool _doubleJump;

        private EnvironmentProcessor _environmentProcessor;


        private SceneObjects _sceneObjects;

        public void PlayFireEffect()
        {
           /* // Player fire animation (hands) is not played when strafing because we lack a proper
            // animation and we do not want to make the animation controller more complex
            if (Mathf.Abs(GetAnimationMoveVelocity().x) > 0.2f)
                return;*/

            ThirdPersonAnimator.SetTrigger("TriggerAction_PrimaryFire");
            //ThirdPersonWeaponAnimator.SetTrigger("TriggerAction_PrimaryFire");
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


           _environmentProcessor = KCC.GetProcessor<EnvironmentProcessor>();
            _sceneObjects = Runner.GetSingleton<SceneObjects>();
        }
       
        public override void FixedUpdateNetwork()
        {


            
            if (_sceneObjects.Gameplay.State == EGamePlayState.Finished)
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
            }


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
            if (_sceneObjects.Gameplay.State == EGamePlayState.Finished)
                return;

            var moveVelocity = GetAnimationMoveVelocity();

            // Set animation parameters.
            SetThirdPersonAnimFloat("LocomotionTime", Time.time * 2f);
            SetThirdPersonAnimBool("AnimState_IsAlive", Health.IsAlive);
            SetThirdPersonAnimBool("AnimState_InAir", !KCC.FixedData.IsGrounded);
            SetThirdPersonAnimBool("AnimState_Run", _isMoving);
            SetThirdPersonAnimBool("AnimState_Stand", !_isMoving);

            SetFirstPersonAnimBool("AnimState_InAir", !KCC.FixedData.IsGrounded);
            SetFirstPersonAnimBool("AnimState_Run", _isMoving);
            SetFirstPersonAnimBool("AnimState_Stand", !_isMoving);
            /*ThirdPersonAnimator.SetFloat("LocomotionTime", Time.time * 2f);
            ThirdPersonAnimator.SetBool("AnimState_IsAlive", Health.IsAlive);
            ThirdPersonAnimator.SetBool("IsGrounded", KCC.FixedData.IsGrounded);
            ThirdPersonAnimator.SetBool("AnimState_InAir",!KCC.FixedData.IsGrounded);
            ThirdPersonAnimator.SetBool("AnimState_Run", _isMoving);
            ThirdPersonAnimator.SetBool("AnimState_Stand", !_isMoving);*/
            if(Weapons.CurrentWeapon.IsReloading)
            {
                SetThirdPersonAnimTrigger("TriggerAction_Reloading");
               /* ThirdPersonAnimator.SetTrigger("TriggerAction_Reloading");
                ThirdPersonWeaponAnimator.SetTrigger("TriggerAction_Reloading");*/
            }
            
            SetThirdPersonAnimFloat("MoveX", moveVelocity.x, 0.05f, Time.deltaTime);
            SetThirdPersonAnimFloat("MoveZ", moveVelocity.z, 0.05f, Time.deltaTime);
            SetThirdPersonAnimFloat("MoveSpeed", moveVelocity.magnitude);
            SetThirdPersonAnimFloat("Look", -KCC.GetLookRotation(true, false).x / 90f);
            /*ThirdPersonAnimator.SetFloat("MoveX", moveVelocity.x, 0.05f, Time.deltaTime);
            ThirdPersonAnimator.SetFloat("MoveZ", moveVelocity.z, 0.05f, Time.deltaTime);
            ThirdPersonAnimator.SetFloat("MoveSpeed", moveVelocity.magnitude);
            ThirdPersonAnimator.SetFloat("Look", -KCC.GetLookRotation(true, false).x / 90f);*/

            if (Health.IsAlive == false)
            {
                // Disable UpperBody (override) and Look (additive) layers. Death animation is full-body.

                int upperBodyLayerIndex = ThirdPersonAnimator.GetLayerIndex("Locomotion");
                ThirdPersonAnimator.SetLayerWeight(upperBodyLayerIndex, Mathf.Max(0f, ThirdPersonAnimator.GetLayerWeight(upperBodyLayerIndex) - Time.deltaTime));

                int lookLayerIndex = ThirdPersonAnimator.GetLayerIndex("Look");
                ThirdPersonAnimator.SetLayerWeight(lookLayerIndex, Mathf.Max(0f, ThirdPersonAnimator.GetLayerWeight(lookLayerIndex) - Time.deltaTime));
            }

            if (_visibleJumpCount < _jumpCount)
            {
                //ThirdPersonAnimator.SetTrigger("Jump");
                SetThirdPersonAnimBool("AnimState_Jump", true);
                SetFirstPersonAnimBool("AnimState_Jump", true);
                //ThirdPersonAnimator.SetBool("AnimState_Jump", true);
                JumpSound.clip = JumpClips[Random.Range(0, JumpClips.Length)];
                JumpSound.Play();
            }

            else if(_visibleJumpCount == _jumpCount)
            {
                //ThirdPersonAnimator.SetBool("AnimState_Jump", false);
                SetThirdPersonAnimBool("AnimState_Jump", false);
                SetFirstPersonAnimBool("AnimState_Jump", false);
            }

            _visibleJumpCount = _jumpCount;








        }

        private void LateUpdate()
        {
            if (HasInputAuthority == false)
                return;
            
            RefreshCamera();
        }

        private void SetMovementData()
        {
            _environmentProcessor.KinematicSpeed = MoveSpeed;
            _environmentProcessor.KinematicAirAcceleration = AirAcceleration;
            _environmentProcessor.KinematicAirFriction = AirDeceleration;
            _environmentProcessor.KinematicGroundAcceleration = GroundAcceleration;
            _environmentProcessor.KinematicGroundFriction = GroundDeceleration;
            _environmentProcessor.Gravity = KCC.FixedData.RealVelocity.y >= 0f ? -(new Vector3(0,UpGravity,0)) : -(new Vector3(0,DownGravity,0));
        }

        private void ProcessInput(NetworkedInput input)
        {
            // Processing input - look rotation, jump, movement, weapon fire, weapon switching, weapon reloading, spray decal.
            SetMovementData();
            KCC.AddLookRotation(input.LookRotationDelta, -MaxCameraPitch, MaxCameraPitch);
            
            // It feels better when player falls quicker
          //  KCC.SetGravity(KCC.Data.RealVelocity.y >= 0f ? -UpGravity : -DownGravity);

            var inputDirection = KCC.FixedData.TransformRotation * new Vector3(input.MoveDirection.x, 0f, input.MoveDirection.y);
            var jumpImpulse = Vector3.zero;
           
            if (input.Buttons.WasPressed(_previousButtons, EInputButton.Jump))
            {
                if (KCC.FixedData.IsGrounded)
                {
                    jumpImpulse.y = JumpForce;
                    _doubleJump = true;
                }
                    
                else if (!KCC.FixedData.IsGrounded && _doubleJump)
                {
                    jumpImpulse.y = JumpForce;
                    _doubleJump = false;
                }

                   
                
                
            }
            
            

            MovePlayer(inputDirection, jumpImpulse);
            RefreshCamera();

            if (KCC.FixedData.HasJumped)
            {
                _jumpCount++;
            }

            if (input.Buttons.IsSet(EInputButton.Fire))
            {
                
                bool justPressed = input.Buttons.WasPressed(_previousButtons, EInputButton.Fire);
               Weapons.Fire(justPressed);
               Health.StopImmortality();
            }
            else if (input.Buttons.IsSet(EInputButton.Reload))
            {
                Weapons.Reload();
            }

            // Store input buttons when the processing is done - next tick it is compared against current input buttons.
            _previousButtons = input.Buttons;
        }

        private void MovePlayer(Vector3 desiredMoveVelocity = default, Vector3 jumpImpulse = default)
        {
            
            
            if(KCC.FixedData.IsGrounded)
                _doubleJump = true;
            if (desiredMoveVelocity == Vector3.zero)
            {
                // No desired move velocity - we are stopping.
                
                _isMoving = false;
            }
            else
            {
                
                _isMoving = true;
            }
            
            //_moveVelocity = Vector3.Lerp(_moveVelocity, desiredMoveVelocity, Runner.DeltaTime);
            _moveVelocity = desiredMoveVelocity;
            KCC.SetInputDirection(_moveVelocity);
            KCC.Jump(jumpImpulse);
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

        private void SetThirdPersonAnimTrigger(string name)
        {
            ThirdPersonAnimator.SetTrigger(name);
            ThirdPersonWeaponAnimator.SetTrigger(name);

        }

        private void SetThirdPersonAnimBool(string name, bool v)
        {
            ThirdPersonAnimator.SetBool(name,v);
            ThirdPersonWeaponAnimator.SetBool(name,v);

        }

        private void SetThirdPersonAnimFloat(string name, float v,float d = default, float h = default)
        {
            ThirdPersonAnimator.SetFloat(name, v,d,h);
            ThirdPersonWeaponAnimator.SetFloat(name, v,d,h);

        }

        private void SetFirstPersonAnimTrigger(string name)
        {
            FirstPersonAnimator.SetTrigger(name);
            FirstPersonWeaponAnimator.SetTrigger(name);

        }

        private void SetFirstPersonAnimBool(string name, bool v)
        {
            FirstPersonAnimator.SetBool(name, v);
            FirstPersonWeaponAnimator.SetBool(name, v);

        }

        private void SetFirstPersonAnimFloat(string name, float v, float d = default, float h = default)
        {
            FirstPersonAnimator.SetFloat(name, v,d,h);
            FirstPersonWeaponAnimator.SetFloat(name, v,d,h);

        }

        private Vector3 GetAnimationMoveVelocity()
        {
            if (KCC.FixedData.RealSpeed < 0.01f)
                return default;

            var velocity = KCC.FixedData.RealVelocity;

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
