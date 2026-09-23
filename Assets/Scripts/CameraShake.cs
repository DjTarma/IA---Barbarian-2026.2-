using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }        // Classe para a câmera em âncora ao Special2 do PlayerController.cs

    // Offset atual do shake. O CameraFollow lê isso e soma na posição.
    public Vector3 CurrentOffset { get; private set; } = Vector3.zero;

    private Coroutine shakeRoutine;

    private void Awake()        // Método disparador do início.
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            CurrentOffset = new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        CurrentOffset = Vector3.zero;
        shakeRoutine = null;
    }
}