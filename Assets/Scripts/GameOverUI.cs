using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject restartText;

    [Header("Config")] // Jogador recebendo o aviso de GameOver, ele aperta a barra de espaço para reiniciar a partida.
    public KeyCode restartKey = KeyCode.Space;

    private bool isGameOver = false;    // Se não, nada acontece (Jogador está com no mímino 1 de HP)

    private void Start()
    {
        if (restartText != null)
            restartText.SetActive(false);
    }

    private void Update()
    {
        if (!isGameOver) return;

        if (Input.GetKeyDown(restartKey))   // Tá lá em cima qual é a tecla que reinicia a partida quando o GameOver aparece.
        {
            RestartGame();      // Vou deixar em baixo, que é melhor. Temos que fazer o texto GameOver aparecer na tela antes.
        }
    }

    public void ShowGameOver()  // Depois de inserir esse script na câmera, ele vai disparar a mensagem de Game Over.
    {
        isGameOver = true;      // Não esquece de colocar no Unity o texto "Game Over! /nPress Space Bar to continue."

        if (restartText != null)
            restartText.SetActive(true);
    }

    private void RestartGame()      // Reiniciar a partida do jogo após estar no estado "Die" e apertar espaço.
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}