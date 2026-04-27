using UnityEngine;

public class RunnerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float forwardSpeed = 7.3f;
    [SerializeField] private float maxForwardSpeed = 14.8f;
    [SerializeField] private float speedRampPerSecond = 0.34f;
    [SerializeField] private float jumpForce = 14.6f;
    [SerializeField] private float groundCheckDistance = 0.3f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundProbeThickness = 0.06f;
    [SerializeField] private float groundProbeInset = 0.06f;
    [SerializeField] private int maxJumpCount = 2;
    [SerializeField] private float coyoteTime = 0.14f;
    [SerializeField] private float jumpBufferTime = 0.14f;
    [SerializeField] private float jumpCutMultiplier = 0.48f;
    [SerializeField] private float maxFallSpeed = 28f;
    [SerializeField] private float jumpHangGravityMultiplier = 0.88f;
    [SerializeField] private float fallGravityMultiplier = 1.56f;
    [SerializeField] private float fastFallGravityMultiplier = 1.78f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideCooldown = 0.2f;
    [SerializeField] private float slideHeightScale = 0.48f;
    [SerializeField] private float airDiveDownSpeed = 22f;
    [SerializeField] private float airDiveCooldown = 0.22f;

    private Rigidbody2D body;
    private BoxCollider2D hitbox;
    private RunnerGameManager manager;
    private bool isGrounded;
    private int jumpCountRemaining;
    private bool isSliding;
    private bool airDiveSliding;
    private float slideTimer;
    private float slideCooldownTimer;
    private float airDiveCooldownTimer;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private Vector2 standingSize;
    private Vector2 standingOffset;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[8];

    public bool IsGrounded => isGrounded;
    public bool IsSliding => isSliding;
    public bool IsAirDiving => airDiveSliding;
    public bool IsRising => body != null && body.velocity.y > 0.75f;
    public bool IsFalling => body != null && body.velocity.y < -0.75f;
    public float VerticalSpeed => body != null ? body.velocity.y : 0f;
    public float ForwardVelocity => body != null ? body.velocity.x : 0f;
    public float SpeedNormalized => Mathf.Clamp01(body != null ? body.velocity.x / Mathf.Max(0.1f, maxForwardSpeed) : 0f);

    public void Configure(RunnerGameManager gameManager)
    {
        manager = gameManager;
        manager.RegisterPlayer(this);
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        hitbox = GetComponent<BoxCollider2D>();
        if (hitbox != null)
        {
            standingSize = hitbox.size;
            standingOffset = hitbox.offset;
        }

        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        jumpCountRemaining = maxJumpCount;
    }

    private void Update()
    {
        if (manager == null || manager.IsGameOver || manager.IsPaused || !manager.IsGameplayActive)
        {
            return;
        }

        bool jumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
        bool jumpReleased = Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow);

        isGrounded = CheckGrounded();
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (slideCooldownTimer > 0f)
        {
            slideCooldownTimer -= Time.deltaTime;
        }
        if (airDiveCooldownTimer > 0f)
        {
            airDiveCooldownTimer -= Time.deltaTime;
        }

        if (isSliding)
        {
            if (airDiveSliding)
            {
                if (isGrounded)
                {
                    airDiveSliding = false;
                    slideTimer = Mathf.Max(slideTimer, slideDuration * 0.8f);
                }
            }
            else
            {
                slideTimer -= Time.deltaTime;
                if (slideTimer <= 0f || !isGrounded)
                {
                    EndSlide();
                }
            }
        }

        if (isGrounded && body.velocity.y <= 0.05f)
        {
            jumpCountRemaining = maxJumpCount;
        }

        if (!isSliding &&
            slideCooldownTimer <= 0f &&
            isGrounded &&
            (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            StartSlide();
        }

        if (!isGrounded &&
            airDiveCooldownTimer <= 0f &&
            (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)))
        {
            TriggerAirDive();
        }

        if (!isSliding && jumpBufferTimer > 0f && CanConsumeJump())
        {
            body.velocity = new Vector2(body.velocity.x, jumpForce);
            ConsumeJump();
            EndSlide();
            jumpBufferTimer = 0f;
        }

        if (jumpReleased && body.velocity.y > 0f)
        {
            body.velocity = new Vector2(body.velocity.x, body.velocity.y * jumpCutMultiplier);
        }
    }

    private void FixedUpdate()
    {
        if (manager == null)
        {
            return;
        }

        if (manager.IsGameOver || manager.IsPaused)
        {
            body.velocity = Vector2.zero;
            return;
        }

        isGrounded = CheckGrounded();
        body.gravityScale = ResolveGravityScale();

        if (!manager.IsGameplayActive)
        {
            float settleY = Mathf.Max(-maxFallSpeed, body.velocity.y);
            if (isGrounded && settleY < 0f)
            {
                settleY = 0f;
            }

            body.velocity = new Vector2(0f, settleY);
            return;
        }

        forwardSpeed = Mathf.Min(maxForwardSpeed, forwardSpeed + speedRampPerSecond * Time.fixedDeltaTime);
        if (isSliding)
        {
            forwardSpeed = Mathf.Min(maxForwardSpeed + 1.4f, forwardSpeed + 0.7f * Time.fixedDeltaTime);
        }

        float nextY = Mathf.Max(-maxFallSpeed, body.velocity.y);
        if (isGrounded && nextY < 0f)
        {
            nextY = 0f;
        }
        body.velocity = new Vector2(forwardSpeed, nextY);

        if (transform.position.y < -7f)
        {
            manager.TriggerGameOver();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (manager == null || manager.IsGameOver)
        {
            return;
        }

        if (collision.collider.GetComponent<RunnerObstacle>() != null ||
            collision.collider.GetComponentInParent<RunnerObstacle>() != null)
        {
            manager.TriggerGameOver();
        }
    }

    private bool CheckGrounded()
    {
        if (hitbox == null)
        {
            RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundMask);
            return hit.collider != null && !hit.collider.isTrigger && hit.collider.gameObject != gameObject;
        }

        Vector2 origin = (Vector2)transform.position + hitbox.offset + Vector2.down * (hitbox.size.y * 0.5f - groundProbeInset);
        Vector2 size = new Vector2(Mathf.Max(0.1f, hitbox.size.x - groundProbeInset * 2f), groundProbeThickness);
        int hitCount = Physics2D.BoxCastNonAlloc(origin, size, 0f, Vector2.down, groundHits, groundCheckDistance, groundMask);
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D c = groundHits[i].collider;
            if (c == null || c.isTrigger)
            {
                continue;
            }

            if (c == hitbox || c.transform == transform)
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private void StartSlide()
    {
        isSliding = true;
        airDiveSliding = false;
        slideTimer = slideDuration;
        slideCooldownTimer = slideDuration + slideCooldown;

        if (hitbox != null)
        {
            float newHeight = standingSize.y * slideHeightScale;
            hitbox.size = new Vector2(standingSize.x, newHeight);
            hitbox.offset = standingOffset + Vector2.down * ((standingSize.y - newHeight) * 0.5f);
        }
    }

    private void EndSlide()
    {
        isSliding = false;
        airDiveSliding = false;
        if (hitbox != null)
        {
            hitbox.size = standingSize;
            hitbox.offset = standingOffset;
        }
    }

    private void TriggerAirDive()
    {
        airDiveCooldownTimer = airDiveCooldown;
        slideCooldownTimer = Mathf.Max(slideCooldownTimer, slideCooldown + 0.16f);
        if (!isSliding)
        {
            isSliding = true;
            airDiveSliding = true;
            slideTimer = slideDuration;
            if (hitbox != null)
            {
                float newHeight = standingSize.y * slideHeightScale;
                hitbox.size = new Vector2(standingSize.x, newHeight);
                hitbox.offset = standingOffset + Vector2.down * ((standingSize.y - newHeight) * 0.5f);
            }
        }

        float targetDown = -Mathf.Abs(airDiveDownSpeed);
        body.velocity = new Vector2(body.velocity.x, Mathf.Min(body.velocity.y, targetDown));
    }

    private bool CanConsumeJump()
    {
        if (jumpCountRemaining <= 0)
        {
            return false;
        }

        if (isGrounded)
        {
            return true;
        }

        if (jumpCountRemaining == maxJumpCount)
        {
            return coyoteTimer > 0f;
        }

        return true;
    }

    private void ConsumeJump()
    {
        coyoteTimer = 0f;
        jumpCountRemaining = Mathf.Max(0, jumpCountRemaining - 1);
    }

    private float ResolveGravityScale()
    {
        if (isGrounded)
        {
            return 1f;
        }

        if (airDiveSliding)
        {
            return fastFallGravityMultiplier;
        }

        float verticalSpeed = body.velocity.y;
        if (verticalSpeed > 2.2f)
        {
            return jumpHangGravityMultiplier;
        }

        if (verticalSpeed < 0.35f)
        {
            return fallGravityMultiplier;
        }

        return 1f;
    }
}
