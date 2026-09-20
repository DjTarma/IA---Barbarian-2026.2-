using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Player")]
    public Transform target; // O target é o alvo da entidade que a câmera dará foco. No caso, será o jogador.

    [Header("Suavização")]
    [Tooltip("Quanto maior, mais rápido a câmera alcança o alvo. 0 = instantâneo.")]
    public float smoothSpeed = 5f;

    [Header("Offset")]
    [Tooltip("Camera2D.")] // Como se trata de um projeto 2D, não há o mínimo sentido termos a variável Z da câmera em execução. Deixei em -10f para anular ela.
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Limites (opcional)")]
    public bool useBounds = false;
    public Vector2 minBounds;
    public Vector2 maxBounds;

    private void LateUpdate()
    {
        if (target == null) return;

        // Posição desejada da câmera
        Vector3 desiredPosition = target.position + offset;

        // Limites aplicados se estiver ativado na câmera. Não use no jogador, não faz sentido.
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        // Mantém o Z do offset fixo
        desiredPosition.z = offset.z;

        // Suaviza o movimento para evitar problemas de visão ou enjôo.
        Vector3 smoothedPosition = Vector3.Lerp(
            transform.position,
            desiredPosition,
            smoothSpeed * Time.deltaTime
        );

        transform.position = smoothedPosition;
    }
}