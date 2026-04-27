using UnityEngine;

public class RunnerPlayerShadow : MonoBehaviour
{
    private Transform shadowTransform;
    private SpriteRenderer shadowRenderer;
    private RunnerController player;
    private Vector3 baseScale;
    private Vector3 baseLocalPosition;
    private Color baseColor;

    public void Configure(Transform shadowRoot, SpriteRenderer renderer, RunnerController runner)
    {
        shadowTransform = shadowRoot;
        shadowRenderer = renderer;
        player = runner;
        if (shadowTransform != null)
        {
            baseScale = shadowTransform.localScale;
            baseLocalPosition = shadowTransform.localPosition;
        }

        if (shadowRenderer != null)
        {
            baseColor = shadowRenderer.color;
        }
    }

    private void LateUpdate()
    {
        if (shadowTransform == null || shadowRenderer == null || player == null)
        {
            return;
        }

        float airborne = Mathf.Clamp01(Mathf.Abs(player.VerticalSpeed) / 18f);
        float forwardStretch = Mathf.Lerp(0f, 0.1f, player.SpeedNormalized);
        float scaleX = Mathf.Lerp(1f + forwardStretch, 0.74f + forwardStretch * 0.3f, airborne);
        float scaleY = Mathf.Lerp(1f, 0.52f, airborne);
        if (player.IsSliding)
        {
            scaleX *= 1.1f;
            scaleY *= 0.92f;
        }

        shadowTransform.localScale = new Vector3(baseScale.x * scaleX, baseScale.y * scaleY, 1f);
        shadowTransform.localPosition = baseLocalPosition + new Vector3(0f, Mathf.Lerp(0f, -0.04f, airborne), 0f);

        float alpha = player.IsGrounded ? baseColor.a : Mathf.Lerp(baseColor.a, baseColor.a * 0.38f, airborne);
        shadowRenderer.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
    }
}
