using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;       // Quanto de vida o inimigo pode ter. Dá pra editar isso no Unity se a gente quiser.

    void OnTriggerEnter2D(Collider2D other)     // Checagem de colisão.
    {
        if (other.CompareTag("Player"))         // Se achar o jogador ao colidir...
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>(); // PlayerHealth.cs
            if (playerHealth != null)
                playerHealth.TakeDamage(damageAmount);  // dá dano ao player.
        }
    }
}