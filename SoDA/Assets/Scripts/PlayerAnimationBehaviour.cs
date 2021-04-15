using UnityEngine;

namespace SoDA
{
    public class PlayerAnimationBehaviour : MonoBehaviour
    {
        [Header("Component References")]
        public Animator playerAnimator;
        [SerializeField] private float animSpeedMultiplier = 1f;
        [SerializeField] private float runCycleLegOffset = 0.2f; //specific to the character in sample assets, will need to be modified to work with others
        [SerializeField] private float moveSpeedMultiplier = 1f;
        private Rigidbody _rigidbody;

        private bool _isGrounded;
        
        //Animation String IDs
        private static int _animatorAttack;
        private static int _animatorOnGround;
        private static int _animatorCrouch;
        private static int _animatorTurn;
        private static int _animatorForward;
        private static int _animatorJump;
        private static int _animatorJumpLeg;

        public void SetupBehaviour()
        {
            SetupAnimationIDs();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private static void SetupAnimationIDs()
        {
            _animatorOnGround = Animator.StringToHash("OnGround");
            _animatorCrouch = Animator.StringToHash("Crouch");
            _animatorTurn = Animator.StringToHash("Turn");
            _animatorForward = Animator.StringToHash("Forward");
            _animatorJump = Animator.StringToHash("Jump");
            _animatorJumpLeg = Animator.StringToHash("JumpLeg");
            _animatorAttack = Animator.StringToHash("Attack");
        }

        public void UpdateMovementAnimation(Vector3 movementBlendValue, bool crouch, bool isGrounded, float turnAmount)
        {
            playerAnimator.SetFloat(_animatorForward, movementBlendValue.z, 0.1f, Time.deltaTime);
            playerAnimator.SetFloat(_animatorTurn, turnAmount, 0.1f, Time.deltaTime);
            playerAnimator.SetBool(_animatorCrouch, crouch);
            playerAnimator.SetBool(_animatorOnGround, isGrounded);
            _isGrounded = isGrounded;
            playerAnimator.applyRootMotion = isGrounded;
            
            if (!isGrounded)
            {
                playerAnimator.SetFloat(_animatorJump, _rigidbody.velocity.y);
            }
            // calculate which leg is behind, so as to leave that leg trailing in the jump animation
            // (This code is reliant on the specific run cycle offset in our animations,
            // and assumes one leg passes the other at the normalized clip times of 0.0 and 0.5)
            var runCycle =
                Mathf.Repeat(
                    playerAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime + runCycleLegOffset, 1);
            var jumpLeg = (runCycle < 0.5f ? 1 : -1) * movementBlendValue.z;
            if (isGrounded)
            {
                playerAnimator.SetFloat(_animatorJumpLeg, jumpLeg);
            }

            // the anim speed multiplier allows the overall speed of walking/running to be tweaked in the inspector,
            // which affects the movement speed because of the root motion.
            if (isGrounded && movementBlendValue.magnitude > 0)
            {
                playerAnimator.speed = animSpeedMultiplier;
            }
            else
            {
                // don't use that while airborne
                playerAnimator.speed = 1;
            }
            //playerAnimator.SetFloat(playerMovementAnimationID, movementBlendValue.magnitude);
        }

        public void PlayAttackAnimation()
        {
            playerAnimator.SetTrigger(_animatorAttack);
        }
        
        public void OnAnimatorMove()
        {
            // we implement this function to override the default root motion.
            // this allows us to modify the positional speed before it's applied.
            if (!_isGrounded || !(Time.deltaTime > 0)) return;
            var v = (playerAnimator.deltaPosition * moveSpeedMultiplier) / Time.deltaTime;

            // we preserve the existing y part of the current velocity.
            v.y = _rigidbody.velocity.y;
            _rigidbody.velocity = v;
        }


    }
}