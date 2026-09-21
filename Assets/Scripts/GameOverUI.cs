using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject restartText;

    [Header("Config")]
    public KeyCode restartKey = KeyCode.Space;

    private bool isGameOver = false;

    private void Start()
    {
        if (restartText != null)
            restartText.SetActive(false);
    }

    private void Update()
    {
        if (!isGameOver) return;

        if (Input.GetKeyDown(restartKey))
        {
            RestartGame();
        }
    }

    public void ShowGameOver()
    {
        isGameOver = true;

        if (restartText != null)
            restartText.SetActive(true);
    }

    private void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}