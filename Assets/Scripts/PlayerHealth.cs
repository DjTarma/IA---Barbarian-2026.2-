using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]    // String que define o valor máximo de vida do jogador.
    public int maxHealth = 5;       // 5 hits
    public int currentHealth;

    [Header("Invulnerabilidade")]   // String que define o tempo de invulnerabilidade EM SEGUNDOS até o próximo estado. Isso reage com o TakeDamage.
    [Tooltip("Tempo em segundos que o player fica imune após tomar dano.")]
    public float invulnerabilityDuration = 1f;

    [Header("Knockback")]   // String que faz o jogador ser empurrado para direção oposta ao receber dano de qualquer fonte.
    [Tooltip("Força do empurrão ao tomar dano.")]
    public float knockbackForce = 8f;   // Quase 1 tile de distância.

    [Tooltip("Tempo em segundos que o knockback dura.")] // Quanto tempo o Knockback vai durar.
    public float knockbackDuration = 0.5f;

    private float invulnerabilityTimer; 
    private PlayerController playerController;      // Só para referenciar a matriz/âncora PlayerController.cs

    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();    // âncora que define qual é a classe matriz disso aqui.
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)  // Somente em estados específicos isso aqui muda, e conforme os valores definidos do PlayerController.cs
            invulnerabilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)         // Estado (Não animado) do TakeDamage.
    {
        if (invulnerabilityTimer > 0f) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityDuration;

        Debug.Log("Vida: " + currentHealth);

        if (playerController != null)
        {
            playerController.FlashRed(invulnerabilityDuration);     // Jogador vai brilhar vermelho

            Vector2 knockbackDir = -playerController.LastMoveDirection;     // Jogador é empurrado pra direção oposta.
            playerController.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);

            playerController.PlayTakeDamageAnimation(); // Aqui vem a animação de receber dano no PlayerController.cs
        }

        // NÃO TIRA ISSO PELO AMOR DE DEUS — O texto só vai aparecer quando o HP chega a 0
        if (currentHealth <= 0)
        {
            if (playerController != null)
                playerController.Die();

            GameOverUI gameOver = FindAnyObjectByType<GameOverUI>();    // Não mexe no GameOverUI pelamor de Deus.
            if (gameOver != null)
                gameOver.ShowGameOver();

            return;
        }
    }
}