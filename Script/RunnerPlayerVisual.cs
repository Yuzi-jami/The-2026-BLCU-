using UnityEngine;

public class RunnerPlayerVisual : MonoBehaviour
{
    private Transform visualRoot;
    private SpriteRenderer spriteRenderer;
    private RunnerController controller;
    private float runAnimTime;
    private float slideAnimTime;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPosition = Vector3.zero;
    private bool wasGrounded;
    private bool wasSliding;
    private float landingSquashTimer;
    private float airborneTimer;
    private float jumpLaunchTimer;
    private Sprite[] runFrames;
    private Sprite[] slideFrames;
    private Sprite[] jumpFrames;

    public void Configure(
        Transform visualTransform,
        SpriteRenderer visualRenderer,
        RunnerController runnerController,
        Sprite[] runSequence,
        Sprite[] slideSequence,
        Sprite[] jumpSequence)
    {
        visualRoot = visualTransform;
        spriteRenderer = visualRenderer;
        controller = runnerController;
        SetSequences(runSequence, slideSequence, jumpSequence);
        if (visualRoot != null)
        {
            baseScale = visualRoot.localScale;
            baseLocalPosition = visualRoot.localPosition;
        }

        wasGrounded = runnerController != null && runnerController.IsGrounded;
        wasSliding = runnerController != null && runnerController.IsSliding;
        airborneTimer = 0f;
        jumpLaunchTimer = 0f;
    }

    public void SetSequences(Sprite[] runSequence, Sprite[] slideSequence, Sprite[] jumpSequence)
    {
        runFrames = runSequence;
        slideFrames = slideSequence;
        jumpFrames = jumpSequence;
    }

    public void RefreshBasePoseFromCurrentTransform()
    {
        if (visualRoot == null)
        {
            return;
        }

        baseScale = visualRoot.localScale;
        baseLocalPosition = visualRoot.localPosition;
    }

    private void Update()
    {
        if (visualRoot == null || controller == null)
        {
            return;
        }

        if (controller.IsGrounded)
        {
            airborneTimer = 0f;
        }
        else
        {
            airborneTimer += Time.deltaTime;
        }

        if (!controller.IsGrounded && wasGrounded)
        {
            jumpLaunchTimer = 0.12f;
        }

        if (controller.IsGrounded && !wasGrounded)
        {
            landingSquashTimer = 0.09f;
        }
        wasGrounded = controller.IsGrounded;
        if (wasSliding != controller.IsSliding)
        {
            slideAnimTime = 0f;
            wasSliding = controller.IsSliding;
        }

        if (landingSquashTimer > 0f)
        {
            landingSquashTimer -= Time.deltaTime;
        }
        if (jumpLaunchTimer > 0f)
        {
            jumpLaunchTimer -= Time.deltaTime;
        }

        runAnimTime += Time.deltaTime * Mathf.Lerp(2.4f, 5.8f, controller.SpeedNormalized);
        if (controller.IsSliding)
        {
            slideAnimTime += Time.deltaTime * Mathf.Lerp(10f, 15f, controller.SpeedNormalized);
        }
        UpdateSpriteFrame();

        Vector3 targetScale;
        Vector3 targetOffset;
        bool frameDriven = runFrames != null && runFrames.Length > 1;
        if (controller.IsSliding)
        {
            if (frameDriven)
            {
                targetScale = Vector3.one;
                targetOffset = new Vector3(0.008f, controller.IsAirDiving ? -0.05f : -0.04f, 0f);
            }
            else
            {
                targetScale = controller.IsAirDiving
                    ? new Vector3(1.08f, 0.8f, 1f)
                    : new Vector3(1.12f, 0.72f, 1f);
                targetOffset = new Vector3(0.04f, controller.IsAirDiving ? -0.1f : -0.12f, 0f);
            }
        }
        else
        {
            float speed = Mathf.Clamp01(controller.SpeedNormalized);
            float bob = 0f;
            float squash = 0f;
            float tiltForward = Mathf.Lerp(0.01f, 0.03f, speed);
            if (frameDriven)
            {
                if (controller.IsGrounded)
                {
                    bob = Mathf.Sin(runAnimTime * Mathf.PI) * Mathf.Lerp(0.0015f, 0.0042f, speed);
                }
                else
                {
                    bob = controller.IsFalling ? -0.012f : 0.004f;
                }

                targetScale = Vector3.one;
                targetOffset = new Vector3(0f, bob, 0f);
            }
            else if (controller.IsGrounded)
            {
                bob = Mathf.Sin(runAnimTime * Mathf.PI) * Mathf.Lerp(0.006f, 0.016f, speed);
                squash = Mathf.Sin(runAnimTime * Mathf.PI * 2f) * Mathf.Lerp(0.008f, 0.018f, speed);
                targetScale = new Vector3(1.01f + tiltForward - squash, 1.01f + squash, 1f);
                targetOffset = new Vector3(0f, bob, 0f);
            }
            else
            {
                bob = controller.IsFalling ? -0.02f : 0.014f;
                squash = controller.IsFalling ? -0.018f : -0.006f;
                targetScale = new Vector3(1.01f + tiltForward - squash, 1.01f + squash, 1f);
                targetOffset = new Vector3(0f, bob, 0f);
            }
        }

        if (!frameDriven && landingSquashTimer > 0f)
        {
            float t = 1f - landingSquashTimer / 0.09f;
            float impulse = Mathf.Sin(t * Mathf.PI) * 0.09f;
            targetScale.x += impulse;
            targetScale.y -= impulse;
            targetOffset.y -= impulse * 0.22f;
        }

        Vector3 finalScale = Vector3.Scale(baseScale, targetScale);
        Vector3 finalPosition = baseLocalPosition + targetOffset;
        float poseResponse = frameDriven ? 22f : 14f;
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, finalScale, Time.deltaTime * poseResponse);
        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, finalPosition, Time.deltaTime * poseResponse);
    }

    private void UpdateSpriteFrame()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (controller.IsSliding && slideFrames != null && slideFrames.Length > 0)
        {
            int slideStart = slideFrames.Length >= 6 ? 2 : 0;
            int slideCount = Mathf.Max(1, slideFrames.Length - slideStart - (slideFrames.Length >= 6 ? 2 : 0));
            int slideIndex = slideStart + Mathf.Min(Mathf.FloorToInt(slideAnimTime), slideCount - 1);
            spriteRenderer.sprite = slideFrames[slideIndex];
            return;
        }

        bool stableAirborne = !controller.IsGrounded && (airborneTimer > 0.04f || Mathf.Abs(controller.VerticalSpeed) > 1.2f);
        if (stableAirborne && jumpFrames != null && jumpFrames.Length > 0)
        {
            float jumpT;
            if (jumpLaunchTimer > 0f)
            {
                jumpT = 0f;
            }
            else if (controller.IsRising)
            {
                jumpT = Mathf.InverseLerp(14f, 0f, controller.VerticalSpeed) * 0.45f;
            }
            else if (controller.IsFalling)
            {
                jumpT = 0.52f + Mathf.InverseLerp(0f, -18f, controller.VerticalSpeed) * 0.48f;
            }
            else
            {
                jumpT = 0.48f;
            }

            int jumpIndex = Mathf.Clamp(Mathf.RoundToInt(jumpT * (jumpFrames.Length - 1)), 0, jumpFrames.Length - 1);
            spriteRenderer.sprite = jumpFrames[jumpIndex];
            return;
        }

        if (runFrames != null && runFrames.Length > 0)
        {
            int runTrim = runFrames.Length >= 10 ? 2 : (runFrames.Length >= 6 ? 1 : 0);
            int runStart = runTrim;
            int runCount = Mathf.Max(1, runFrames.Length - runTrim * 2);
            int runIndex = runStart + (Mathf.FloorToInt(runAnimTime) % runCount);
            spriteRenderer.sprite = runFrames[runIndex];
        }
    }
}
