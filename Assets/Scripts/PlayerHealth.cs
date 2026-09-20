using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Invulnerabilidade")]
    [Tooltip("Tempo em segundos que o player fica imune após tomar dano.")]
    public float invulnerabilityDuration = 1f;

    [Header("Knockback")]
    [Tooltip("Força do empurrão ao tomar dano.")]
    public float knockbackForce = 8f;

    [Tooltip("Tempo em segundos que o knockback dura.")]
    public float knockbackDuration = 0.5f;

    private float invulnerabilityTimer;
    private PlayerController playerController;

    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (invulnerabilityTimer > 0f)
            invulnerabilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(int amount)
    {
        if (invulnerabilityTimer > 0f) return;

        currentHealth -= amount;
        invulnerabilityTimer = invulnerabilityDuration;

        Debug.Log("Vida: " + currentHealth);

        if (playerController != null)
        {
            playerController.FlashRed(invulnerabilityDuration);

            Vector2 knockbackDir = -playerController.LastMoveDirection;
            playerController.ApplyKnockback(knockbackDir, knockbackForce, knockbackDuration);

            // Aqui pega a âncora do PlayerController.cs para disparar via Trigger na BlendTree2D a animação de receber dano.
            playerController.PlayTakeDamageAnimation();
        }

        if (currentHealth <= 0)
        {
            if (playerController != null)
                playerController.Die();
            return;
        }
    }
}