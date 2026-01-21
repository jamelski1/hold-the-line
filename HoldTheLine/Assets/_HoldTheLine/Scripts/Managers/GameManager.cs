using UnityEngine;
using HoldTheLine.Player;

namespace HoldTheLine.Managers
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;

        private int score;
        private bool isGameOver;

        public int Score => score;
        public bool IsGameOver => isGameOver;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }

            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath += HandlePlayerDeath;
            }

            StartGame();
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnPlayerDeath -= HandlePlayerDeath;
            }
        }

        public void StartGame()
        {
            score = 0;
            isGameOver = false;
            Time.timeScale = 1f;
        }

        public void AddScore(int points)
        {
            if (isGameOver) return;
            score += points;
        }

        private void HandlePlayerDeath()
        {
            isGameOver = true;
            Debug.Log($"Game Over! Final Score: {score}");
        }

        public void RestartGame()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }
}
