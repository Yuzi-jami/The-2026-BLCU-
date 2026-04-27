using UnityEngine;

public class RunnerScroll : MonoBehaviour
{
    private RunnerGameManager manager;
    private Vector3 origin;
    private float bobPhase;
    private float bobSpeed;

    public RunnerScroll Configure(RunnerGameManager gameManager)
    {
        manager = gameManager;
        return this;
    }

    private void Awake()
    {
        origin = transform.position;
        bobPhase = Random.Range(0f, Mathf.PI * 2f);
        bobSpeed = Random.Range(4.2f, 5.8f);
    }

    private void Update()
    {
        if (manager != null && (manager.IsPaused || !manager.IsGameplayActive || manager.IsGameOver))
        {
            return;
        }

        float bob = Mathf.Sin(Time.time * bobSpeed + bobPhase + origin.x * 0.3f) * 0.08f;
        transform.position = new Vector3(origin.x, origin.y + bob, origin.z);
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
            manager.CollectScroll();
            Destroy(gameObject);
        }
    }
}
