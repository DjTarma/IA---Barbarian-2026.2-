using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Alvo")]                // Definimos no Unity quem a camerâ vai seguir. No óbvio será o player...
    public Transform target;

    [Header("Suavização")]          // Naturalizar a movimentação da câmera
    [Tooltip("Quanto maior, mais rápido a câmera alcança o alvo. 0 = instantâneo.")]
    public float smoothSpeed = 5f;

    [Header("Offset")]              // Naturalizar a movimentação da câmera.
    [Tooltip("Deslocamento da câmera em relação ao alvo. Use Z = -10 pra 2D.")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);  // ângulo Z usado para a profundidade 2.5D

    [Header("Limites (opcional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private void LateUpdate()
    {
        if (target == null) return;

        // Posição desejada da câmera que precisa ser horizontal.
        Vector3 desiredPosition = target.position + offset;

        // Aplica limites se estiver ativado
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        desiredPosition.z = offset.z;

        // Suaviza o movimento para ninguém ter um ataque epilético. Não quero ser processado :pray:
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position - (CameraShake.Instance != null ? CameraShake.Instance.CurrentOffset : Vector3.zero),
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        // Aplica o offset do shake por cima. Inserir valores pelo Unity, viu? 
        if (CameraShake.Instance != null)
            smoothedPosition += CameraShake.Instance.CurrentOffset;

        transform.position = smoothedPosition;
    }
}