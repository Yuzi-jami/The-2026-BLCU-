using UnityEngine;

public class RunnerCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new Vector3(4.25f, 1.18f, -10f);
    [SerializeField] private float speedLookAhead = 1.45f;
    [SerializeField] private float verticalTrackStrength = 0.14f;
    [SerializeField] private float followSmoothness = 0.06f;
    [SerializeField] private float maxFollowSpeed = 120f;
    [SerializeField] private bool snapToCentimeter = true;

    private Transform target;
    private RunnerGameManager manager;
    private float smoothVelocityX;
    private float smoothVelocityY;
    private Vector3 runtimeOffset;

    public void Configure(Transform followTarget, RunnerGameManager gameManager)
    {
        target = followTarget;
        manager = gameManager;
        runtimeOffset = offset;
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        if (manager != null && !manager.IsGameplayActive && !manager.IsGameOver)
        {
            Vector3 readyPosition = new Vector3(target.position.x + runtimeOffset.x, runtimeOffset.y, runtimeOffset.z);
            transform.position = Vector3.Lerp(transform.position, readyPosition, Time.deltaTime * 10f);
            return;
        }

        float targetX = target.position.x + runtimeOffset.x;
        float targetY = runtimeOffset.y + (target.position.y + 1.7f - runtimeOffset.y) * verticalTrackStrength;
        var body = target.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            targetX += Mathf.Clamp(body.velocity.x * 0.04f, 0f, speedLookAhead);
            targetY += Mathf.Clamp(body.velocity.y * 0.03f, -0.3f, 0.45f);
        }

        float nextX = Mathf.SmoothDamp(transform.position.x, targetX, ref smoothVelocityX, followSmoothness, maxFollowSpeed);
        float nextY = Mathf.SmoothDamp(transform.position.y, targetY, ref smoothVelocityY, followSmoothness, maxFollowSpeed);
        if (snapToCentimeter)
        {
            nextX = Mathf.Round(nextX * 100f) / 100f;
            nextY = Mathf.Round(nextY * 100f) / 100f;
        }

        transform.position = new Vector3(nextX, nextY, runtimeOffset.z);

        if (manager != null && manager.IsGameOver)
        {
            runtimeOffset = Vector3.Lerp(runtimeOffset, new Vector3(3.2f, 2f, -10f), Time.deltaTime * 1.4f);
        }
    }
}
