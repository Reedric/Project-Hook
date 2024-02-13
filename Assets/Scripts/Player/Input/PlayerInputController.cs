using ASK.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEditor;
using UnityEngine.Events;

namespace Player
{
    public class PlayerInputController : MonoBehaviour, IInputController
    {
        private PlayerControls controls;
        private PlayerControls.GameplayActions inputActions;

        private System.Action PauseAction;

        private InputMode mode;

        private float inputTimer = 2.0f;

        public int HasPutInput = 0;

        [SerializeField] private UnityEvent firstInput;

        private enum InputMode
        {
            KeyboardAndMouse,
            Controller
        }

        private void OnEnable()
        {
            if (controls == null)
            {
                controls = new PlayerControls();
                inputActions = controls.Gameplay;
            }

            inputActions.Enable();

            EnableDevices();
            if (Gamepad.current != null)
            {
                InputSystem.DisableDevice(Gamepad.current);
            }

            inputActions.Get().actionTriggered += OnAction;
            inputActions.Pause.performed += OnPause;
            #if UNITY_EDITOR
            inputActions.Debug.performed += OnDebug;
            #endif
        }

        private void OnDisable()
        {
            inputActions.Get().actionTriggered -= OnAction;
            inputActions.Pause.performed -= OnPause;
            #if UNITY_EDITOR
            inputActions.Debug.performed -= OnDebug;
            #endif
            inputActions.Disable();
        }

        private void Update()
        {
            if (inputTimer > 0.0f)
            {
                inputTimer -= Time.deltaTime;
            }
            else
            {
                EnableDevices();
            }
        }

        public bool AnyKeyPressed()
        {
            return MovementStarted() || DiveStarted() || JumpStarted();
        }

        public int GetMovementInput()
        {
            if (Game.Instance.FakeControlsArrows != -2)
            {
                return Game.Instance.FakeControlsArrows;
            }
            
            
            int rightInput = inputActions.MoveRight.IsPressed() ? 1 : 0;
            int leftInput = inputActions.MoveLeft.IsPressed() ? 1 : 0;
            return rightInput - leftInput;
        }

        public bool MovementStarted()
        {
            bool bothDirsDifferent = inputActions.MoveRight.IsPressed() ^ inputActions.MoveLeft.IsPressed();
            return MovementChanged() && bothDirsDifferent;
        }

        public bool MovementFinished()
        {
            //If left and right are held at the same time, the player will not move.
            bool bothDirsSame = inputActions.MoveRight.IsPressed() == inputActions.MoveLeft.IsPressed();
            return MovementChanged() && bothDirsSame;
        }
        
        public bool RetryStarted()
        {
            return inputActions.Restart.WasPerformedThisFrame();
        }
        
        public bool GetJumpInput()
        {
            // if (!Game.Instance.FakeControlsZ.Disabled) return Game.Instance.FakeControlsZ.Value;
            return inputActions.Jump.IsPressed();
        }

        public bool JumpStarted()
        {
            // if (!Game.Instance.FakeControlsZ.Disabled) return Game.Instance.FakeControlsZ.WasPressedThisFrame();
            return inputActions.Jump.WasPressedThisFrame();
        }

        public bool JumpFinished()
        {
            // if (!Game.Instance.FakeControlsZ.Disabled) return Game.Instance.FakeControlsZ.WasReleasedThisFrame();
            return inputActions.Jump.WasReleasedThisFrame();
        }

        public bool GetDiveInput()
        {
            return inputActions.Dive.IsPressed();
        }

        public bool DiveStarted()
        {
            return inputActions.Dive.WasPressedThisFrame();
        }

        public bool DiveFinished()
        {
            return inputActions.Dive.WasReleasedThisFrame();
        }

        public bool GetGrappleInput()
        {
            return inputActions.Grapple.IsPressed();
        }

        public bool GrappleStarted()
        {
            if (inputActions.Grapple.WasPressedThisFrame())
            {
                HasPutInput++;
                if (HasPutInput == 2)
                {
                    firstInput?.Invoke();
                }
            }
            return inputActions.Grapple.WasPressedThisFrame();
        }

        public bool GrappleFinished()
        {
            return inputActions.Grapple.WasReleasedThisFrame();
        }
        
        public bool GetParryInput()
        {
            return inputActions.Parry.IsPressed();
        }

        public bool ParryStarted()
        {
            return inputActions.Parry.WasPressedThisFrame();
        }

        public bool ParryFinished()
        {
            return inputActions.Parry.WasReleasedThisFrame();
        }

        public Vector3 GetMousePos()
        {
            Vector2 mPos = Mouse.current.position.ReadValue();
            return Camera.main.ScreenToWorldPoint(mPos);
        }

        public Vector2 GetStickAim()
        {
            return inputActions.Aim.ReadValue<Vector2>();
        }
        
        public Vector2 GetAimPos(Vector3 playerPos)
        {
            Vector2 stickInput = GetStickAim();
            if (stickInput != Vector2.zero) return stickInput*30 + (Vector2)playerPos;
            return GetMousePos();
        }

        public void AddToPauseAction(System.Action action)
        {
            PauseAction += action;
        }

        public void RemoveFromPauseAction(System.Action action)
        {
            PauseAction -= action;
        }

        private void OnPause(InputAction.CallbackContext ctx)
        {
            PauseAction?.Invoke();
        }

        public bool PausePressed()
        {
            return inputActions.Pause.WasPressedThisFrame();
        }

        private bool MovementChanged()
        {
            bool dirPressed = inputActions.MoveRight.WasPressedThisFrame() || inputActions.MoveLeft.WasPressedThisFrame();
            bool dirReleased = inputActions.MoveRight.WasReleasedThisFrame() || inputActions.MoveLeft.WasReleasedThisFrame();

            return dirPressed || dirReleased;
        }

        private void OnAction(InputAction.CallbackContext ctx)
        {
            if (inputTimer <= 0.0f)
            {
                InputMode newMode = CheckMode(ctx);
                if (!mode.Equals(newMode))
                {
                    mode = newMode;
                    SwitchMode();
                }
            }
            inputTimer = 3.0f;
        }
        
        private void SwitchMode()
        {
            if (mode.Equals(InputMode.KeyboardAndMouse))
            {
                InputSystem.EnableDevice(Keyboard.current);
                InputSystem.EnableDevice(Mouse.current);
                InputSystem.DisableDevice(Gamepad.current);
            }
            else
            {
                InputSystem.EnableDevice(Gamepad.current);
                InputSystem.DisableDevice(Keyboard.current);
                InputSystem.DisableDevice(Mouse.current);
            }
        }

        private InputMode CheckMode(InputAction.CallbackContext ctx)
        {
            if (ctx.action.activeControl.device.name.Equals("Keyboard") || ctx.action.activeControl.device.name.Equals("Mouse"))
            {
                return InputMode.KeyboardAndMouse;
            }
            return InputMode.Controller;
        }

        private void EnableDevices()
        {
            InputSystem.EnableDevice(Keyboard.current);
            InputSystem.EnableDevice(Mouse.current);
            if (Gamepad.current != null)
            {
                InputSystem.EnableDevice(Gamepad.current);
            }
        }

        #if UNITY_EDITOR
        private void OnDebug(InputAction.CallbackContext ctx)
        {
            Game.Instance.IsDebug = true;
        }
        #endif
    }
}