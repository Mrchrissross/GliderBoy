using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GliderBoy.Controllers
{
    public class GameController : Singleton<GameController>
    {
        
        #region Constant Variables

        private const string HIGH_SCORE = "HighScore";
        private const string GAME_OVER_SOUND = "Game Over 1";

        #endregion
        
        
        
        #region Fields
        
        public event Action<bool> OnPauseAction;

        [SerializeField] private TMP_Text bestScoreText;
        
        [SerializeField] private TMP_Text[] scoreTexts;
        [SerializeField] private TMP_Text[] difficultyTexts;
        
        [SerializeField] private int score = 0;
        [SerializeField] private bool paused;

        public UnityEvent OnStart;
        public UnityEvent OnGameOver;
        public UnityEvent OnQuit;

        #endregion


        
        #region Public Functions

        public void StartGame()
        {
            score = 0;
            paused = false;
            
            OnStart.Invoke();
            UpdateUITexts();
        }

        public void Pause()
        {
            paused = !paused;
            OnPauseAction?.Invoke(paused);
        }
        
        public void Pause(bool pause)
        {
            paused = pause;
            OnPauseAction?.Invoke(pause);
        }

        public void QuitInGame()
        {
            OnQuit.Invoke();
        }

        public void OnScore()
        {
            score++;
            UpdateUITexts();
            
            // SoundManager.PlaySound(SoundManager.Sound.Score);
        }

        public void GameOver()
        {
            AudioController.Instance.PlaySound(GAME_OVER_SOUND);
            
            if(score > PlayerPrefs.GetFloat(HIGH_SCORE))
                PlayerPrefs.SetFloat(HIGH_SCORE, score);

            bestScoreText.text = $"High Score: {PlayerPrefs.GetFloat(HIGH_SCORE)}";
            
            OnGameOver.Invoke();
        }

        public void QuitGame()
        {
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        #endregion



        #region Private Functions

        [ContextMenu("Remove Saved Content")]
        private void DeletePlayerPrefs() => PlayerPrefs.DeleteAll();


        private void UpdateUITexts()
        {
            try
            {
                var currentScore = score.ToString("0");
                for (var i = 0; i < scoreTexts.Length; i++)
                    scoreTexts[i].text = i == 0 ? currentScore : $"Score: {currentScore}";
            } catch {Debug.LogError($"{this}: An error has occured while updating the score texts.");}

            try
            {
                var currentDifficulty = LevelController.Instance.currentDifficulty;
                for (var i = 0; i < difficultyTexts.Length; i++)
                    difficultyTexts[i].text = i == 0 ? currentDifficulty : $"Difficulty: {currentDifficulty}";
            } catch {Debug.LogError($"{this}: An error has occured while updating the difficulty texts.");}
        }

        #endregion



        #region Monobehaviour

        private void Start() => UpdateUITexts();
        
        #endregion
        
    }
}
