using UnityEngine;

public class RunnerCoin : MonoBehaviour
{
    private RunnerGameManager manager;
    private Vector3 baseScale;
    private float pulseOffset;
    private float spinSpeed;

    public RunnerCoin Configure(RunnerGameManager gameManager)
    {
        manager = gameManager;
        return this;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        pulseOffset = Random.Range(0f, Mathf.PI * 2f);
        spinSpeed = Random.Range(110f, 180f);
    }

    private void Update()
    {
        if (manager != null && (manager.IsPaused || !manager.IsGameplayActive || manager.IsGameOver))
        {
            return;
        }

        float pulse = 1f + Mathf.Sin(Time.time * 10f + pulseOffset) * 0.08f;
        transform.localScale = baseScale * pulse;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (manager == null || manager.IsGameOver)
        {
            return;
        }

        if (other.GetComponent<RunnerController>() != null ||
            other.GetComponentInParent<RunnerController>() != null)
        {
            manager.CollectCoin();
            Destroy(gameObject);
        }
    }
}
