using System;
using System.Collections.Generic;
using GliderBoy.Utility;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace GliderBoy.Controllers
{
    public class LevelController : Singleton<LevelController>
    {

        #region Constant Variables

        private const float PIPE_HEIGHT = 0.8887f;
        private const float SCREEN_HEIGHT = 9.0f;
        private const float MINIMUM_GAP = 2.4f;

        #endregion

        
        
        #region Classes

        private class Pipe 
        {
            private readonly Transform _pipe;
            private readonly bool _onBottom;
            private readonly bool _isSingle;

            public Pipe(Transform pipe, bool onBottom, bool isSingle = false)
            {
                _pipe = pipe;
                _onBottom = onBottom;
                _isSingle = isSingle;
            }

            public void SetYPosition(float yPosition)
            {
                _pipe.position = _pipe.position.WithY(yPosition);

                if (_onBottom) return;
                var localScale = _pipe.localScale;
                _pipe.localScale = localScale.WithY(-localScale.y);
            }
            
            public void Move(float speed) => _pipe.position += Vector3.left * speed * Time.deltaTime;
            public float Position() => _pipe.position.x;
            public void MakeDiagonal(float angle) => _pipe.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            public bool OnBottom() => _onBottom;
            public bool IsSingle() => _isSingle;
            public void Destroy() => Object.Destroy(_pipe.gameObject);
        }
        
        [Serializable]
        public class Difficult
        {
            [Tooltip("Optional: Name of this difficulty.")]
            public string name = "";
            [Tooltip("Number of pipes needed to have passed before enabling this difficulty.")]
            public int pipesPassed = 0;

            [Space, Tooltip("The speed at which the pipes will travel.")] 
            public float pipeSpeed = 3.0f;
            [Tooltip("Minimum gap is 2.5, anything lower will result in the player not fitting.")]
            public Vector2 gapRange = new Vector2(2.5f, 3.0f);
            [Tooltip("Time between pipe spawns (in sec).")]
            public float spawnTimer = 2.0f;
            
            [Space, Tooltip("Whether the pipe will spawn at an angle.")] 
            public bool diagonal;
            [Tooltip("The angle of the pipe.")] 
            public float angle = 25f;
            [Tooltip("The offset of the pipe on the y axis.")] 
            public float yOffset = 0.25f;
        }

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
        /// The difficult index, whenever called, it will update the difficulty as well as return the index.
        /// </summary>
        
        private int DifficultyIndex
        {
            get
            {
                if (_difficultyIndex + 1 < difficulties.Count &&
                    _pipesPassed >= difficulties[_difficultyIndex + 1].pipesPassed - 1) return _difficultyIndex += 1;
                
                return _difficultyIndex;        
            }   
        }
        private int _difficultyIndex;

        #endregion

        
        
        #region Fields
        
        [Space(10f)]
        public string currentDifficulty = "";

        [Space(10f)]
        [SerializeField, Range(0, 10)] private float screenTop = 4.46f;
        [SerializeField, Range(-10, 0)] private float screenBottom = -3.58f;
        
        [Space(10f)]
        [SerializeField, Range(0, 15)] private float spawnPoint = 8;
        [SerializeField, Range(-15, 0)] private float destroyPoint = -8;

        [Space(10f)]
        [SerializeField] private List<Difficult> difficulties = new List<Difficult>();
        
        private readonly Vector2 _pipeWidth = new Vector2(0.5f, 1.2f);
        private float _pipeSpeed;
        private Vector2 _pipeSpawnTimer;
        private int _pipesPassed;
        private float _gapSize;
        private bool _diagonal;
        private float _angle;
        private float _yOffset;
        private List<Pipe> _pipes = new List<Pipe>(); 
        
        public event Action OnScore;

        #endregion


        
        #region Public Functions

        /// <summary>
        /// Completely erases the level. 
        /// </summary>

        public void ResetLevel()
        {
            foreach (var pipe in _pipes) pipe.Destroy();
            _pipes.Clear();

            _pipesPassed = 0;
            _difficultyIndex = 0;
            _paused = true;
            SetDifficulty();
        }
        
        /// <summary>
        /// Invoked when the game is paused.
        /// </summary>
        
        public void PauseLevel(bool pause) => _paused = pause;
        
        #endregion
        
        

        #region Private Functions

        /// <summary>
        /// Generates a single pipe.
        /// </summary>
        /// <param name="position">The position of the pipes on the x axis.</param>
        /// <param name="height">The height of the pipe.</param>
        /// <param name="onBottom">Is this pipe on the bottom of the screen?</param>
        /// <param name="isSingle">Is this *top* pipe alone? (Bottom pipe was not spawned).</param>
        
        private Pipe GenerateSinglePipe(float position, float height, bool onBottom, bool isSingle = false)
        {
            // Instantiate two pipe object.
            var body = Instantiate(AssetController.Instance.pipeBody).transform;
            var head = Instantiate(AssetController.Instance.pipeHead).transform;

            // Set the pipes height and position the head correctly on top of the pipes body..
            body.localScale = body.localScale.WithY(height);
            head.position = Vector3.zero.WithY((PIPE_HEIGHT * height) - 0.4f);

            // Set the head as a child to the body.
            head.SetParent(body);
            
            // Rescale the body (and childed head) and parent it to this game object for organisation.
            var width = Random.Range(_pipeWidth.x, _pipeWidth.y);
            body.position = Vector3.right * position;
            body.localScale = body.localScale.WithX(width).WithZ(width);
            body.SetParent(transform);
            
            // Return the pipe.
            return new Pipe(body, onBottom, isSingle);
        }

        /// <summary>
        /// Generates two pipes with a gap between them.
        /// </summary>
        /// <param name="position">The position of the pipes on the x axis.</param>
        /// <param name="height">The height of the bottom pipe.</param>
        /// <param name="gap">The desired amount of gap between the two pipes.</param>
        
        private void GenerateGapPipe(float position, float height, float gap)
        {
            var bottomPipe = height >= 0;
            var topHeight = SCREEN_HEIGHT - height - (_diagonal ? gap : Mathf.Max(MINIMUM_GAP, gap));
            var topPipe = topHeight >= 0;
            
            // Add the newly created pipes to the list of pipes.
            if (bottomPipe)
            {
                var pipe = GenerateSinglePipe(position, height, true);
                pipe.SetYPosition(screenBottom - _yOffset);
                if(_diagonal) pipe.MakeDiagonal(_angle);
                _pipes.Add(pipe);
            }

            if (topPipe)
            {
                var pipe = GenerateSinglePipe(position, topHeight, false, !bottomPipe);
                pipe.SetYPosition(screenTop + _yOffset);
                if(_diagonal) pipe.MakeDiagonal(-_angle);
                _pipes.Add(pipe);
            }
        }

        // -------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Moves each pipe left and destroys them once they have surpassed the screen space.
        /// </summary>
        
        private void HandlePipeMovement() 
        {
            // Loop through each pipe.
            for (var i = 0; i < _pipes.Count; i++) 
            {
                // Store the object for easy access.
                var pipe = _pipes[i];

                // Check if the pipe has been passed.
                var wasPassed = pipe.Position() <= 0.0f;
                pipe.Move(_pipeSpeed);
                
                // If after moving, the pipe has now been passed, invoke the score method. 
                var isPassed = pipe.Position() <= 0;
                if (!wasPassed && isPassed && (pipe.OnBottom() || pipe.IsSingle()))
                {
                    OnScore?.Invoke();
                    
                    // Update the number of pipes passed and set the difficulty.
                    _pipesPassed++;
                    SetDifficulty();
                }

                // If the pipe has not surpassed the screen space, continue.
                if (pipe.Position() >= destroyPoint) continue;
                
                // Else, destroy the pipe.
                pipe.Destroy();
                _pipes.Remove(pipe);
                i--;
            }
        }
        
        /// <summary>
        /// Spawns a pipe each time the spawn timer runs out.
        /// </summary>
        
        private void HandlePipeSpawning() 
        {
            // Count down the timer.
            _pipeSpawnTimer.y -= Time.deltaTime;
            
            // If the timer has not run out, return.
            if (!(_pipeSpawnTimer.y < 0)) return;
            
            // Else, reset the timer.
            _pipeSpawnTimer.y = _pipeSpawnTimer.x;
            
            // Spawn a new pipe.
            const float offset = 2.0f;
            var minHeight = _gapSize * .5f;
            var maxHeight = SCREEN_HEIGHT - _gapSize * .5f;
            var height = Random.Range(minHeight, maxHeight) - offset;
            GenerateGapPipe(spawnPoint, height, _gapSize);
        }

        // -------------------------------------------------------------------------------------------------------------
        
        /// <summary>
        /// Set the level difficulty.
        /// </summary>
        
        private void SetDifficulty()
        {
            var difficulty = difficulties[DifficultyIndex];
            
            currentDifficulty = difficulty.name;
            _pipeSpeed = difficulty.pipeSpeed;
            _gapSize = Random.Range(difficulty.gapRange.x, difficulty.gapRange.y);
            _pipeSpawnTimer.x = difficulty.spawnTimer;

            _diagonal = difficulty.diagonal;
            _angle = difficulty.angle;
            _yOffset = difficulty.yOffset;
        }

        #endregion
        
        
        
        #region Monobehaviour

        protected override void Awake()
        {
            base.Awake();
            SetDifficulty();
            _paused = true;
        }

        private void Start()
        {
            GameController.Instance.OnPauseAction += PauseLevel;
            OnScore += GameController.Instance.OnScore;

            // SpawnInitialGround();
            // SpawnInitialClouds();
        }

        private void Update()
        {
            if (_paused) return;
            
            HandlePipeMovement();
            HandlePipeSpawning();
            
            // HandleGround();
            // HandleClouds();
        }

        #if UNITY_EDITOR
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.black;
            Gizmos.DrawCube(Vector3.up * screenBottom, new Vector3(6, 0.01f, 0.01f));
            Gizmos.DrawCube(Vector3.up * screenTop, new Vector3(6, 0.01f, 0.01f));
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(Vector3.right * destroyPoint, 0.25f);
            
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(Vector3.right * spawnPoint, 0.25f);
        }

        #endif
        
        #endregion
        
    }
}