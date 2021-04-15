using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoDA.InputSystem
{
    public class CharacterController : MonoBehaviour
    {
        //Player ID
        private int _playerId;
        
        [Header("Sub Behaviours")]
        public PlayerMovementBehaviour playerMovementBehaviour;
        public PlayerAnimationBehaviour playerAnimationBehaviour;
        //public PlayerVisualsBehaviour playerVisualsBehaviour;

        [Header("Input Settings")]
        public PlayerInput playerInput;
        public float movementSmoothingSpeed = 1f;
        private Vector3 _rawInputMovement;
        private Vector3 _smoothInputMovement;
        private float _rawJumpInput;
        
        [Header("Debug Infos")]
        [SerializeField]private bool jumpPressed;
        [SerializeField]private bool walkPressed;
        [SerializeField]private bool crouchPressed;

        // Camera Info
        private Transform _cam;  
        private Vector3 _camForward;             // The current forward direction of the camera

        //Action Maps
        private const string ActionMapPlayerControls = "Player Controls";
        private const string ActionMapMenuControls = "Menu Controls";

        //Current Control Scheme
        private string _currentControlScheme;


        //This is called from the GameManager; when the game is being setup.
        public void SetupPlayer(int newPlayerId)
        {
            _playerId = newPlayerId;

            _currentControlScheme = playerInput.currentControlScheme;
            
            playerMovementBehaviour.SetupBehaviour();
            playerAnimationBehaviour.SetupBehaviour();
            GetCamera();
        }

        private void GetCamera()
        {
            // get the transform of the main camera
            if (Camera.main != null)
            {
                _cam = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning(
                    "Warning: no main camera found. Third person character needs a Camera tagged \"MainCamera\", for camera-relative controls.", gameObject);
                // we use self-relative controls in this case, which probably isn't what the user wants, but hey, we warned them!
            }
        }
        
        
        //INPUT SYSTEM ACTION METHODS --------------

        //This is called from PlayerInput; when a joystick or arrow keys has been pushed.
        //It stores the input Vector as a Vector3 to then be used by the smoothing function.
        
        public void OnMovement(InputAction.CallbackContext value)
        {
            var inputMovement = value.ReadValue<Vector2>();
            _rawInputMovement = new Vector3(inputMovement.x, 0, inputMovement.y);
        }
        
        public void OnJump(InputAction.CallbackContext value)
        {
            var inputJump = value.ReadValue<float>();
            _rawJumpInput = inputJump;
            jumpPressed = _rawJumpInput > 0.1;
        }
        
        public void OnWalk(InputAction.CallbackContext value)
        {
            var inputWalk = value.ReadValue<float>();
            walkPressed = inputWalk > 0.1;
        }
        
        public void OnCrouch(InputAction.CallbackContext value)
        {
            var inputCrouch = value.ReadValue<float>();
            crouchPressed = inputCrouch > 0.1;
        }


        
        //INPUT SYSTEM AUTOMATIC CALLBACKS --------------

        //This is automatically called from PlayerInput, when the input device has changed
        //(IE: Keyboard -> Xbox Controller)
        public void OnControlsChanged()
        {
            if (playerInput.currentControlScheme == _currentControlScheme) return;
            
            _currentControlScheme = playerInput.currentControlScheme;
            //playerVisualsBehaviour.UpdatePlayerVisuals();
            RemoveAllBindingOverrides();
        }
        
        
        //This is automatically called from PlayerInput, when the input device has been disconnected and can not be identified
        //IE: Device unplugged or has run out of batteries



        public void OnDeviceLost()
        {
            //playerVisualsBehaviour.SetDisconnectedDeviceVisuals();
        }
        
        public void OnDeviceRegained()
        {
            StartCoroutine(WaitForDeviceToBeRegained());
        }

        private static IEnumerator WaitForDeviceToBeRegained()
        {
            yield return new WaitForSeconds(0.1f);
            //playerVisualsBehaviour.UpdatePlayerVisuals();
        }
        
        
        
        //Update Loop - Used for calculating frame-based data
        private void FixedUpdate()
        {
            CalculateMovementInputSmoothing();
            CalculateCameraInfluence();
            UpdatePlayerMovement();
            UpdatePlayerAnimationMovement();
        }
        
        
        
        //Input's Axes values are raw
        private void CalculateMovementInputSmoothing()
        {
            _smoothInputMovement = Vector3.Lerp(_smoothInputMovement, _rawInputMovement, Time.deltaTime * movementSmoothingSpeed);
            _smoothInputMovement = _rawInputMovement;
            if (walkPressed)
            {
                _smoothInputMovement *= 0.5f;
            }
        }

        private void CalculateCameraInfluence()
        {
            var h = _smoothInputMovement.x;
            var v = _smoothInputMovement.z;
            // calculate move direction to pass to character
            if ((object)_cam != null)
            {
                // calculate camera relative direction to move:
                
                _camForward = Vector3.Scale(_cam.forward, new Vector3(1, 0, 1)).normalized;
                _smoothInputMovement = v*_camForward + h*_cam.right;
            }
            else
            {
                // we use world-relative directions in the case of no main camera
                _smoothInputMovement = v*Vector3.forward + h*Vector3.right;
            }
        }

        private void UpdatePlayerMovement()
        {
            playerMovementBehaviour.UpdateMovementData(_smoothInputMovement, crouchPressed,jumpPressed);
        }

        private void UpdatePlayerAnimationMovement()
        {
            playerAnimationBehaviour.UpdateMovementAnimation(
                playerMovementBehaviour.get_move_vector(),
                playerMovementBehaviour.get_crouching(),
                playerMovementBehaviour.CheckGroundStatus(),
                playerMovementBehaviour.get_TurnAmount());
        }


        public void SetInputActiveState(bool gameIsPaused)
        {
            switch (gameIsPaused)
            {
                case true:
                    playerInput.DeactivateInput();
                    break;

                case false:
                    playerInput.ActivateInput();
                    break;
            }
        }

        private void RemoveAllBindingOverrides()
        {
            InputActionRebindingExtensions.RemoveAllBindingOverrides(playerInput.currentActionMap);
        }
        
        
        //Switching Action Maps ----
        public void EnableGameplayControls()
        {
            playerInput.SwitchCurrentActionMap(ActionMapPlayerControls);  
        }

        public void EnablePauseMenuControls()
        {
            playerInput.SwitchCurrentActionMap(ActionMapMenuControls);
        }
        

        //Get Data ----
        public int GetPlayerId()
        {
            return _playerId;
        }

        public InputActionAsset GetActionAsset()
        {
            return playerInput.actions;
        }

        public PlayerInput GetPlayerInput()
        {
            return playerInput;
        }

        
    }
}

