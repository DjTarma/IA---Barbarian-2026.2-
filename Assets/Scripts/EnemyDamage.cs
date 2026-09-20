using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    public int damageAmount = 10;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
                playerHealth.TakeDamage(damageAmount);
        }
    }
}