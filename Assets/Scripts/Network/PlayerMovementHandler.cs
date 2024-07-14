using Fusion;
using Fusion.Addons.KCC;
using UnityEngine;
using UnityEngine.Windows;
public class PlayerMovementHandler : NetworkBehaviour
{
    [SerializeField] private KCC kcc;
    [SerializeField] private Transform camTarget;
    [SerializeField] private float maxPitch = 85f;
    [SerializeField] private float lookSensetivity = 0.15f;
    [SerializeField] private Vector3 jumpImpulse = new Vector3 (0f, 10f, 0f);

    [Networked] private NetworkButtons PreviousButtons { get; set; }

    private Camera PlayerCam;

    private bool doubleJump;

    private InputManager inputManager;
    private Vector2 baseLookRotation;
    public override void Spawned()
    {
        

        if (HasInputAuthority)
        {
            CameraFollow.Singleton.SetTarget(camTarget);
            inputManager = Runner.GetComponent<InputManager>();
            inputManager.LocalPlayer = this;
            kcc.Settings.ForcePredictedLookRotation = true;
        }



    }




    public override void FixedUpdateNetwork()
    {
        
        if (GetInput(out NetInput input))
        {
            CheckJump(input);

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                kcc.AddLookRotation(input.LookDelta * lookSensetivity);
            }


            UpdateCameraTarget();
          
            SetInputDirection(input);
            PreviousButtons = input.Buttons;
            baseLookRotation = kcc.GetLookRotation();
        }
    }

    public override void Render()
    {
        if (kcc.Settings.ForcePredictedLookRotation)
        {
            Vector2 predictedLookRotation = baseLookRotation + inputManager.AccumulatedMouseDelta * lookSensetivity;
            kcc.SetLookRotation(predictedLookRotation);
        }

        UpdateCameraTarget();
    }

    private void CheckJump(NetInput input)
    {
        if (kcc.FixedData.IsGrounded)
                doubleJump = true;
        if(input.Buttons.WasPressed(PreviousButtons, InputButton.Jump))
        {

            if (!kcc.FixedData.IsGrounded && doubleJump)
                doubleJump = false;
            else if (!kcc.FixedData.IsGrounded /*|| !(!kcc.FixedData.IsGrounded && doubleJump)*/)
                return;
            kcc.Jump(jumpImpulse);
        }
    }

    private void SetInputDirection(NetInput input)
    {
        Vector3 worldDirection = kcc.FixedData.TransformRotation * new Vector3(input.Direction.x, 0f, input.Direction.y);
        kcc.SetInputDirection(worldDirection);
    }

    private void UpdateCameraTarget()
    {
        camTarget.localRotation = Quaternion.Euler(kcc.GetLookRotation().x, 0f, 0f);
    }

}
