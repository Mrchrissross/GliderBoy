using System;
using System.Linq;
using GliderBoy.Utility;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GliderBoy.Controllers
{
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerController : MonoBehaviour
    {
        
        #region Cached Data

        // The storage of components for easy access.

        /// <summary>
        /// Cached 'Rigidbody' component.
        /// </summary>

        private Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody) return _rigidbody;

                _rigidbody = GetComponent<Rigidbody>();
                ExtensionsLibrary.CheckComponent(_rigidbody, "Rigidbody Component", name);
                return _rigidbody;
            }
        }

        private Rigidbody _rigidbody;

        /// <summary>
        /// Cached 'Glider' Transform.
        /// </summary>

        private Transform Glider
        {
            get
            {
                if (_glider) return _glider;

                if (!gliderAnimator) Debug.LogError($"{this}: A reference to the glider animator has not been found.");

                _glider = gliderAnimator.transform.parent;
                return _glider;
            }
        }

        private Transform _glider;

        public Animator GFXAnimator;
        public Animator boyAnimator;
        public Animator gliderAnimator;

        #endregion



        #region Properties

        /// <summary>
        /// Allows other scripts to set the paused boolean however they are unable to get it.
        /// </summary>
        
        public bool Paused
        {
            set => _paused = value;
        }
        private bool _paused;
        
        /// <summary>
        /// This is the characters velocity combined with any platform that the character may be standing on.
        /// </summary>

        public Vector3 Velocity
        {
            get
            {
                _velocity = Rigidbody.velocity; 
                return _velocity; 
            }
            set
            {
                _velocity = value;
                Rigidbody.velocity = _velocity;
            }
        }
        // Used to see the velocity in the inspector (Debug Mode). 2018.4.4 does not display RB Values.
        private Vector3 _velocity; 
        
        /// <summary>
        /// Boolean used to control player jumping.
        /// </summary>
        
        private bool Jump
        {
            get => _jump;
            set
            {
                if (!gravityEnabled && value)
                {
                    LevelController.Instance.Paused = false;
                    gravityEnabled = true;
                }

                // If jump is released, allow to jump again.
                if (_jump && value == false) _canJump = true;

                // Update jump value.
                _jump = value;
            }
        }
        private bool _jump = true;

        /// <summary>
        /// The height of the first jump. Be aware that if the upward speed is limited, this value may also be limited.
        /// </summary>

        public float BaseJumpStrength
        {
            get => baseJumpStrength;
            set => baseJumpStrength = Mathf.Max(0.0f, value);
        }

        [SerializeField] private float baseJumpStrength = 3.0f;

        /// <summary>
        /// The maximum amount of time that holding the button will have an effect.
        /// </summary>

        public float HeldJumpTime
        {
            get => heldJumpTime;
            set => heldJumpTime = Mathf.Max(0.0f, value);
        }

        [SerializeField] private float heldJumpTime = 0.5f;

        /// <summary>
        /// The upward power that will be applied while the button is held down.
        /// </summary>

        public float HeldJumpPower
        {
            get => heldJumpPower;
            set => heldJumpPower = Mathf.Max(0.0f, value);
        }

        [SerializeField] private float heldJumpPower = 25.0f;

        /// <summary>
        /// Boolean used to confirm whether the character is jumping or not.
        /// </summary>

        public bool IsJumping
        {
            get
            {
                if (_isJumping && Velocity.y < 0.0001f) _isJumping = false;
                return _isJumping;
            }
            set => _isJumping = value;
        }

        private bool _isJumping;

        /// <summary>
        /// The jump impulse is used when applying an upward jumping impulse.
        /// </summary>

        private float JumpImpulse => Mathf.Sqrt(BaseJumpStrength * GravityStrength);

        /// <summary>
        /// The exact opposite of isJumping.
        /// This boolean used to confirm whether the character is falling or not.
        /// </summary>

        public bool IsFalling => !IsJumping;

        /// <summary>
        /// The overall strength of gravity applied to the character.
        /// </summary>

        public float GravityStrength
        {
            get => gravityStrength;
            set => gravityStrength = Mathf.Max(0.0f, value);
        }

        [SerializeField] private float gravityStrength = 35.0f;

        /// <summary>
        /// The direction in which gravity has an effect on the character.
        /// Use this for additional strengths in other directions.
        /// </summary>

        public Vector3 GravityDirection
        {
            get => gravityDirection * GravityStrength;
            set => gravityDirection = value;
        }

        [SerializeField] public Vector3 gravityDirection = Vector3.down;

        #endregion



        #region Fields

        [SerializeField] private bool gravityEnabled;
        
        [SerializeField] private float characterOverlapRadius = 0.1f;
        [SerializeField] private Transform[] characterOverlapPositions;
        [SerializeField] private float gliderRaycastDistance = 1.0f;
        [SerializeField] private Transform gliderRaycastPosition;

        private float _jumpButtonHeldDownTimer;
        private bool _canJump;
        private bool _updateJumpTimer;
        private float _jumpTimer;
        
        private event Action GameOver;
        private bool _gameOver;

        #endregion


        
        #region Public Functions

        /// <summary>
        /// Resets the character to default.
        /// </summary>
        
        public void ResetCharacter()
        {
            transform.position = Vector3.zero;
            gravityEnabled = false;
            Jump = false;
            _paused = false;
            _gameOver = false;

            GFXAnimator.Play("Idle");
            boyAnimator.enabled = true;
            boyAnimator.speed = 1;
            gliderAnimator.enabled = true;
            gliderAnimator.speed = 1;
            
            Rigidbody.constraints = RigidbodyConstraints.None;
            Velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }
        
        /// <summary>
        /// Invoked when the game is paused.
        /// </summary>
        
        public void PauseCharacter(bool pause)
        {
            _paused = pause;
            
            if (pause)
            {
                boyAnimator.speed = 0;
                gliderAnimator.speed = 0;
                Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
            }
            else
            {
                boyAnimator.speed = 1;
                gliderAnimator.speed = 1;
                Rigidbody.constraints = RigidbodyConstraints.None;
            }
        }

        #endregion
        
        

        #region Private Functions

        /// <summary>
        /// Handles user input.
        /// </summary>

        private void HandleInput()
        {
            // Play jump sound effect.
            if(Input.GetButtonDown("Jump") || Input.GetButtonDown("Fire1")) AudioController.Instance.PlaySound("Whoosh");

            // Jump input.
            Jump = Input.GetButton("Jump") || Input.GetButton("Fire1");
        }

        // -------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Performs raycasts directly forward from three specific points on the character and glider.
        /// </summary>

        private void CollisionDetection()
        {
            var obstacleHit = false;

            if (Physics.Raycast(gliderRaycastPosition.position, -Vector3.left, out var raycastHit,
                gliderRaycastDistance))
                if (raycastHit.transform.CompareTag("Obstacle")) obstacleHit = true;

            if (!obstacleHit)
            {
                foreach (var overlap in characterOverlapPositions)
                {
                    var hitColliders = Physics.OverlapSphere(overlap.position, characterOverlapRadius);

                    if (hitColliders.Length <= 0) continue;

                    if (hitColliders.Any(hitCollider => hitCollider.transform.CompareTag("Obstacle")))
                        obstacleHit = true;

                    if (obstacleHit) break;
                }
            }

            if (obstacleHit && !_gameOver) Die();
        }

        private void Die()
        {
            _gameOver = true;
            GFXAnimator.SetTrigger($"Fall {Random.Range(1, 4)}");
            GameOver?.Invoke();
        }

        // -------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Perform character movement.
        /// </summary>

        private void ProcessInput()
        {
            // Apply gravity to the character.
            if (gravityEnabled) Velocity += (GravityDirection * Time.deltaTime);

            // Jump logic
            PerformJumpLogic();
        }

        /// <summary>
        /// Performs the jump logic, allowing the character to jump up in the air.
        /// </summary>

        private void PerformJumpLogic()
        {
            PerformBaseJump();
            UpdateJumpTimer();
        }

        /// <summary>
        /// Performs the base jump which is the initial jump before any additional jumps in the air.
        /// </summary>

        private void PerformBaseJump()
        {
            // If not grounded, the jump button hasn't been pressed, or it has still not been released, return.
            if (!Jump || !_canJump) return;

            _canJump = false; // Stop the character from jumping again until the jump button has been released.
            IsJumping = true; // Store a boolean to say the character is jumping.
            _updateJumpTimer = true; // Start the jump timer to provide additional force to the jump when the jump button is held.
            
            // Apply an upward impulse to perform the jump.
            Rigidbody.ApplyUpwardImpulse(gravityDirection.y < 0 ? JumpImpulse : -JumpImpulse);
        }

        /// <summary>
        /// When the jump button is held down, additional force is applied to the jump to smoothly increase its height. 
        /// </summary>

        private void UpdateJumpTimer()
        {
            // Ensure the jump button is held down.
            if (!_updateJumpTimer) return;

            // Store the animator parameter for later use.
            const string parameterName = "Fly Speed";

            // If jump button is pressed and the jump timer has not exceeded the max held jump time.
            if (Jump && _jumpTimer < HeldJumpTime)
            {
                // Access and change the sliding animation speed.
                boyAnimator.SetFloat(parameterName, 1);
                gliderAnimator.SetFloat(parameterName, 1);

                // Calculate what percent the jump timer is of the maximum.
                var jumpProgress = _jumpTimer / HeldJumpTime;

                // Apply the percentage as the interpolation ratio (0-1) and perform a linear interpolation
                // from the extra power to zero.
                var proportionalJumpPower = Mathf.Lerp(HeldJumpPower, 0f, jumpProgress);

                // Update the gliders euler angles to give a slight nudge (gust of wind) to the glider. (-> 2.5f -> 0.0f). 
                var localEulerAngles = Glider.localEulerAngles;
                Glider.localEulerAngles =
                    localEulerAngles.WithZ(Mathf.Lerp(localEulerAngles.z, jumpProgress <= 0.5f ? 5f : 0f,
                        jumpProgress));

                // Apply the result to the rigidbody as acceleration in an upward fashion.
                Rigidbody.AddForce((gravityDirection.y < 0 ? Vector3.up : Vector3.down) * proportionalJumpPower,
                    ForceMode.Acceleration);

                // Update jump timer.
                _jumpTimer = Mathf.Min(_jumpTimer + Time.deltaTime, HeldJumpTime);

                return;
            }

            // Reset the gliders euler angles to zero.
            Glider.localEulerAngles = Vector3.zero;

            // Access and change the sliding animation speed.
            boyAnimator.SetFloat(parameterName, 0.25f);
            gliderAnimator.SetFloat(parameterName, 0.25f);

            // Once the button has been released, reset.
            _jumpTimer = 0.0f;
            _updateJumpTimer = false;
        }

        #endregion



        #region Monobehaviour

        private void Start()
        {
            GameController.Instance.OnPauseAction += PauseCharacter;
            GameOver += GameController.Instance.GameOver;

            PauseCharacter(true);
        }

        private void Update()
        {
            if (_paused) return;
            
            CollisionDetection();
            HandleInput();
        }

        private void FixedUpdate()
        {
            if (_paused) return;
            
            ProcessInput();
        }

        #if UNITY_EDITOR
        
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            foreach (var overlap in characterOverlapPositions)
                Gizmos.DrawWireSphere(overlap.position, characterOverlapRadius);

            Handles.color = Color.red;
            var start = gliderRaycastPosition.position;
            Handles.DrawLine(start, start + (-Vector3.left * gliderRaycastDistance));
        }
        
        #endif

        #endregion
        
    }
}