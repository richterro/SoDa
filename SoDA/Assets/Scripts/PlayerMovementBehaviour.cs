using UnityEngine;

namespace SoDA
{
    public class PlayerMovementBehaviour : MonoBehaviour
    {

        [Header("Component References")]
        public Rigidbody playerRigidbody;

        [Header("Movement Settings")]
        [SerializeField] private float stationaryTurnSpeed = 180;
        [SerializeField] private float movingTurnSpeed = 360;
        [SerializeField] private float jumpPower = 12f;
        [SerializeField] private float groundCheckDistance = 0.1f;
        [Range(1f, 4f)][SerializeField] private float gravityMultiplier = 2f;
        //Stored Values
        private Camera _mainCamera;
        private Vector3 _groundNormal;
        private bool _isGrounded;
        private float _turnAmount;
        private float _forwardAmount;
        private bool _crouching;
        private float _origGroundCheckDistance;
        private float _capsuleHeight;
        private Vector3 _capsuleCenter;
        private CapsuleCollider _capsule;
        private Vector3 _moveVector;

        public void SetupBehaviour()
        {
            playerRigidbody.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationY | RigidbodyConstraints.FreezeRotationZ;

            SetGameplayCamera();
            _origGroundCheckDistance = groundCheckDistance;
            _capsule = GetComponent<CapsuleCollider>();
            _capsuleHeight = _capsule.height;
            _capsuleCenter = _capsule.center;
        }

        private void SetGameplayCamera()
        {
            _mainCamera = CameraManager.Instance.GetGameplayCamera();
        }

        public void UpdateMovementData(Vector3 move,  bool crouch, bool jump)
        {
            if (move.magnitude > 1f) move.Normalize();
            //
            move = transform.InverseTransformDirection(move);
            CheckGroundStatus();
            move = Vector3.ProjectOnPlane(move, _groundNormal);
            _turnAmount = Mathf.Atan2(move.x, move.z);
            _forwardAmount = move.z;
            _moveVector = move;
            ApplyExtraTurnRotation();
                
            // control and velocity handling is different when grounded and airborne:
            if (_isGrounded)
            {
                HandleGroundedMovement(crouch, jump);
            }
            else
            {
                HandleAirborneMovement();
            }
        
            ScaleCapsuleForCrouching(crouch);
            PreventStandingInLowHeadroom();
        }

        public float get_TurnAmount()
        {
            return _turnAmount;
        }

        public Vector3 get_move_vector()
        {
            return _moveVector;
        }

        public bool get_crouching()
        {
            return _crouching;
        }

        public bool CheckGroundStatus()
        {
#if UNITY_EDITOR
            // helper to visualise the ground check ray in the scene view
            var position = transform.position;
            Debug.DrawLine(position + (Vector3.up * 0.1f), position + (Vector3.up * 0.1f) + (Vector3.down * groundCheckDistance));
#endif
            // 0.1f is a small offset to start the ray from inside the character
            // it is also good to note that the transform position in the sample assets is at the base of the character
            if (Physics.Raycast(transform.position + (Vector3.up * 0.1f), Vector3.down, out var hitInfo, groundCheckDistance))
            {
                _groundNormal = hitInfo.normal;
                _isGrounded = true;
                //m_Animator.applyRootMotion = true;
            }
            else
            {
                _isGrounded = false;
                _groundNormal = Vector3.up;
                //m_Animator.applyRootMotion = false;
            }

            return _isGrounded;
        }

        private void ApplyExtraTurnRotation()
        {
            // help the character turn faster (this is in addition to root rotation in the animation)
            var turnSpeed = Mathf.Lerp(stationaryTurnSpeed, movingTurnSpeed, _forwardAmount);
            transform.Rotate(0, _turnAmount * turnSpeed * Time.deltaTime, 0);
        }

        private void HandleGroundedMovement(bool crouch, bool jump)
        {
            // check whether conditions are right to allow a jump:
            if (jump && !crouch) // && m_Animator.GetCurrentAnimatorStateInfo(0).IsName("Grounded"))
            {
                // jump!
                var velocity = playerRigidbody.velocity;
                velocity = new Vector3(velocity.x, jumpPower, velocity.z);
                playerRigidbody.velocity = velocity;
                _isGrounded = false;
                //m_Animator.applyRootMotion = false;
                groundCheckDistance = 0.1f;
            }
        }


        private void HandleAirborneMovement()
        {
            // apply extra gravity from multiplier:
            var extraGravityForce = (Physics.gravity * gravityMultiplier) - Physics.gravity;
            playerRigidbody.AddForce(extraGravityForce);

            groundCheckDistance = playerRigidbody.velocity.y < 0 ? _origGroundCheckDistance : 0.01f;
        }

        private Vector3 CameraDirection(Vector3 movementDirection)
        {
            var cameraTransform = _mainCamera.transform;
            var cameraForward = cameraTransform.forward;
            var cameraRight = cameraTransform.right;

            cameraForward.y = 0f;
            cameraRight.y = 0f;
        
            return cameraForward * movementDirection.z + cameraRight * movementDirection.x; 
   
        }

        private void ScaleCapsuleForCrouching(bool crouch)
        {
            if (_isGrounded && crouch)
            {
                if (_crouching) return;
                _capsule.height /= 2f;
                _capsule.center /= 2f;
                _crouching = true;
            }
            else
            {
                var radius = _capsule.radius;
                var crouchRay = new Ray(playerRigidbody.position + Vector3.up * (radius * 0.5f), Vector3.up);
                var crouchRayLength = _capsuleHeight - radius * 0.5f;
                if (Physics.SphereCast(crouchRay, _capsule.radius * 0.5f, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
                {
                    _crouching = true;
                    return;
                }
                _capsule.height = _capsuleHeight;
                _capsule.center = _capsuleCenter;
                _crouching = false;
            }
        }

        private void PreventStandingInLowHeadroom()
        {
            // prevent standing up in crouch-only zones
            if (_crouching) return;
            var radius = _capsule.radius;
            var crouchRay = new Ray(playerRigidbody.position + Vector3.up * (radius * 0.5f), Vector3.up);
            var crouchRayLength = _capsuleHeight - radius * 0.5f;
            if (Physics.SphereCast(crouchRay, _capsule.radius * 0.5f, crouchRayLength, Physics.AllLayers, QueryTriggerInteraction.Ignore))
            {
                _crouching = true;
            }
        }
    
    }
}