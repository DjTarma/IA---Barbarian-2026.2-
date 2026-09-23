using UnityEngine;
using System; // Necessário para Action

public class EnemyHealth : MonoBehaviour
{
    [Header("Vida")] // Contagem de vida da criatura. Lembre-se que a chama dá dano ao jogador ao encostá-lo.
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Feedback Visual")]
    [Tooltip("Objeto que pisca ao tomar dano (geralmente o sprite/mesh filho)")]
    public SpriteRenderer spriteRenderer; 
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    [Header("Morte")]
    [Tooltip("Objeto que será destruído ao morrer. Se vazio, usa este GameObject.")]
    public GameObject objectToDestroy;
    public float destroyDelay = 0f;

    [Header("Efeitos")]
    public GameObject deathEffect; // Partícula, prefab, particularidades que a chama vai precisar ter.

    // Eventos: outros scripts podem se inscrever (ex: UI, spawner, IA)
    public event Action<int, int> OnDamaged;   // (dano, vidaAtual)
    public event Action<int, int> OnHealed;    // (cura, vidaAtual)
    public event Action OnDeath;

    private Color originalColor;

    private void Awake()            // Método de entrada ao executar o jogo.
    {
        currentHealth = maxHealth;
        if (objectToDestroy == null) objectToDestroy = gameObject;

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damage)  // Receber dano, dããããã
    {
        if (damage <= 0 || currentHealth <= 0) return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        OnDamaged?.Invoke(damage, currentHealth);
        StartCoroutine(FlashRoutine());

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || currentHealth <= 0) return;

        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        OnHealed?.Invoke(amount, currentHealth);
    }

    private void Die()              // Dá pra inserir uma animação de morte para cada criatura futuramente.
    {
        OnDeath?.Invoke();

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        if (destroyDelay > 0f)
            Destroy(objectToDestroy, destroyDelay);
        else
            Destroy(objectToDestroy);
    }

    // Pisca o inimigo ao tomar dano
    private System.Collections.IEnumerator FlashRoutine()
    {
        if (spriteRenderer == null) yield break;

        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    // Só um apoio extra para observar bugs no console (se tiver, kkkkk)
    private void OnValidate()
    {
        if (Application.isPlaying && currentHealth > maxHealth)
            currentHealth = maxHealth;
    }
}