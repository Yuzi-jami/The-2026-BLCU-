using System.Collections.Generic;
using UnityEngine;

public class RunnerTileSpawner : MonoBehaviour
{
    [SerializeField] private int initialChunkCount = 8;
    [SerializeField] private float chunkWidth = 14f;
    [SerializeField] private float spawnAheadDistance = 65f;
    [SerializeField] private float despawnBehindDistance = 30f;

    [Header("Rhythm")]
    [SerializeField] private int maxDemandingStreak = 2;
    [SerializeField] private int forcedBreatherInterval = 5;
    [SerializeField] private int minChunksBetweenCombos = 3;
    [SerializeField] private float reactionTime = 0.5f;
    [SerializeField] private float jumpToSlideSwitchTime = 0.46f;
    [SerializeField] private float slideToJumpSwitchTime = 0.66f;

    [Header("Safety")]
    [SerializeField] private float safeStartDistance = 18f;
    [SerializeField] private float minObstacleGap = 2.2f;
    [SerializeField] private float minGlobalRecoveryGap = 3.4f;
    [SerializeField] private float obstacleEdgePadding = 1.2f;

    [Header("Collectibles")]
    [SerializeField] private float coinMinGap = 1.1f;
    [SerializeField] private float coinObstacleAvoidance = 0.75f;
    [SerializeField] private int minCoinsPerChunk = 4;
    [SerializeField] private int maxCoinsPerChunk = 11;
    [SerializeField] private float bonusArcChance = 0.58f;
    [SerializeField] private float waveArcChance = 0.36f;
    [SerializeField] private float verticalArcChance = 0.26f;
    [SerializeField] private float scrollSpawnChance = 0.2f;
    [SerializeField] private float scrollObstacleAvoidance = 1.1f;
    [SerializeField] private float minScrollGap = 18f;

    [Header("Obstacle Look")]
    [SerializeField] private float ceilingY = 4.6f;
    [SerializeField] private float ceilingAnchorY = 5.8f;
    [SerializeField] private Color overheadSupportColor = new Color(0.24f, 0.14f, 0.1f, 0.82f);
    [SerializeField] private float supportColliderWidth = 0.06f;
    [SerializeField] private float minOverheadSupportClearance = 1.16f;
    [SerializeField] private float cloudDriftYMin = 1.6f;
    [SerializeField] private float cloudDriftYMax = 3.8f;
    [SerializeField] private float backgroundArchitectureGap = 0.6f;
    [SerializeField] private int backgroundPlacementAttempts = 18;

    private readonly Queue<GameObject> chunks = new Queue<GameObject>();
    private RunnerGameManager manager;
    private Transform player;
    private RunnerVisualSettings visuals;

    private float nextChunkX;
    private float noObstacleBeforeX;
    private float lastObstacleWorldX = -999f;
    private float lastScrollWorldX = -999f;
    private EncounterType lastEncounterType = EncounterType.None;
    private int demandingStreak;
    private int chunksSinceBreather;
    private int chunksSinceCombo = 99;
    private int gameplayChunkCounter;
    private bool hasLastHazard;
    private bool lastHazardWasOverhead;
    private float lastHazardWorldX = -999f;
    private CollectiblePatternType lastCollectibleType = CollectiblePatternType.None;

    private enum EncounterType
    {
        None,
        Breather,
        JumpBasic,
        SlideBasic,
        JumpDouble,
        JumpSlideCombo,
        SlideJumpCombo
    }

    private struct ObstacleSpawnData
    {
        public GameObject instance;
        public float worldX;
        public float width;
        public float height;
        public bool isOverhead;
        public int beatIndex;
    }

    private enum CollectiblePatternType
    {
        None,
        Coins,
        Scrolls
    }

    public void Configure(RunnerGameManager gameManager, Transform playerTransform, RunnerVisualSettings visualSettings)
    {
        manager = gameManager;
        player = playerTransform;
        visuals = visualSettings;
        noObstacleBeforeX = player.position.x + safeStartDistance;
        BuildInitialChunks();
    }

    private void Update()
    {
        if (manager == null || player == null || manager.IsGameOver || manager.IsPaused || !manager.IsGameplayActive)
        {
            return;
        }

        while (nextChunkX < player.position.x + spawnAheadDistance)
        {
            SpawnChunk(nextChunkX);
            nextChunkX += chunkWidth;
        }

        while (chunks.Count > 0)
        {
            GameObject first = chunks.Peek();
            if (first.transform.position.x + chunkWidth * 0.5f < player.position.x - despawnBehindDistance)
            {
                chunks.Dequeue();
                Destroy(first);
            }
            else
            {
                break;
            }
        }
    }

    private void BuildInitialChunks()
    {
        nextChunkX = -12f;
        for (int i = 0; i < initialChunkCount; i++)
        {
            SpawnChunk(nextChunkX);
            nextChunkX += chunkWidth;
        }
    }

    private void SpawnChunk(float startX)
    {
        var root = new GameObject($"Chunk_{startX:0}");
        root.transform.SetParent(transform);
        root.transform.position = new Vector3(startX + chunkWidth * 0.5f, 0f, 0f);

        CreateBackground(root.transform);
        CreateGround(root.transform);
        float worldProgress = Mathf.Max(0f, startX - noObstacleBeforeX);
        float safePathStart;
        float safePathEnd;
        List<Vector2> blockedRanges;
        EncounterType encounter;
        List<ObstacleSpawnData> obstacles = CreateObstacles(
            root.transform,
            startX,
            worldProgress,
            out safePathStart,
            out safePathEnd,
            out blockedRanges,
            out encounter
        );

        CreateCollectibles(root.transform, startX, worldProgress, safePathStart, safePathEnd, blockedRanges, obstacles, encounter);

        chunks.Enqueue(root);
    }

    private void CreateGround(Transform parent)
    {
        var ground = new GameObject("Ground");
        ground.transform.SetParent(parent);
        ground.transform.localPosition = new Vector3(0f, -3.4f, 0f);
        ground.transform.localScale = new Vector3(chunkWidth, 2.2f, 1f);

        var sr = ground.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = visuals.groundColor;
        sr.sortingOrder = 2;

        var col = ground.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 1f);

        CreateStripe(parent, -2.45f, visuals.stripePrimaryColor);
        CreateStripe(parent, -2.75f, visuals.stripeSecondaryColor);
    }

    private void CreateStripe(Transform parent, float y, Color color)
    {
        var stripe = new GameObject("RoadStripe");
        stripe.transform.SetParent(parent);
        stripe.transform.localPosition = new Vector3(0f, y, 0f);
        stripe.transform.localScale = new Vector3(chunkWidth, 0.08f, 1f);

        var sr = stripe.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = 3;
    }

    private List<ObstacleSpawnData> CreateObstacles(
        Transform parent,
        float chunkStartX,
        float worldProgress,
        out float safePathStart,
        out float safePathEnd,
        out List<Vector2> blockedRanges,
        out EncounterType encounter)
    {
        blockedRanges = new List<Vector2>();
        var obstacles = new List<ObstacleSpawnData>();

        float chunkEndX = chunkStartX + chunkWidth;
        float usableStart = chunkStartX + obstacleEdgePadding;
        float usableEnd = chunkEndX - obstacleEdgePadding;
        safePathStart = usableStart;
        safePathEnd = usableEnd;

        if (chunkEndX < noObstacleBeforeX)
        {
            encounter = EncounterType.Breather;
            return obstacles;
        }

        int gameplayChunkIndex = gameplayChunkCounter++;
        encounter = ChooseEncounter(worldProgress, gameplayChunkIndex);
        float[] beats = BuildBeats(usableStart, usableEnd);
        float baseGap = GetBaseGap(worldProgress);
        float jumpToSlideGap = GetJumpToSlideGap(worldProgress);
        float slideToJumpGap = GetSlideToJumpGap(worldProgress);
        bool placedAny = false;

        switch (encounter)
        {
            case EncounterType.Breather:
                // Breather chunk intentionally has no mandatory action.
                break;
            case EncounterType.JumpBasic:
                placedAny = TrySpawnGroundObstacle(parent, beats[2], 2, worldProgress, baseGap, obstacles);
                break;
            case EncounterType.SlideBasic:
                placedAny = TrySpawnOverheadObstacle(parent, beats[2], 2, worldProgress, baseGap, obstacles);
                break;
            case EncounterType.JumpDouble:
                placedAny |= TrySpawnGroundObstacle(parent, beats[1], 1, worldProgress, baseGap, obstacles);
                placedAny |= TrySpawnGroundObstacle(parent, beats[4], 4, worldProgress + 8f, baseGap, obstacles);
                break;
            case EncounterType.JumpSlideCombo:
                if (TrySpawnGroundObstacle(parent, beats[1], 1, worldProgress, baseGap, obstacles))
                {
                    placedAny = true;
                    if (!TrySpawnOverheadObstacle(parent, beats[4], 4, worldProgress + 14f, jumpToSlideGap, obstacles))
                    {
                        placedAny |= TrySpawnOverheadObstacle(parent, beats[3], 3, worldProgress + 14f, jumpToSlideGap, obstacles);
                    }
                    else
                    {
                        placedAny = true;
                    }
                }
                break;
            case EncounterType.SlideJumpCombo:
                if (TrySpawnOverheadObstacle(parent, beats[0], 0, worldProgress, baseGap, obstacles))
                {
                    placedAny = true;
                    if (!TrySpawnGroundObstacle(parent, beats[4], 4, worldProgress + 14f, slideToJumpGap, obstacles))
                    {
                        placedAny |= TrySpawnGroundObstacle(parent, beats[3], 3, worldProgress + 14f, slideToJumpGap, obstacles);
                    }
                    else
                    {
                        placedAny = true;
                    }
                }
                break;
        }

        if (!placedAny && encounter != EncounterType.Breather)
        {
            // Fallback to single-jump pattern if spacing constraints filtered all attempts.
            TrySpawnGroundObstacle(parent, beats[2], 2, worldProgress, baseGap, obstacles);
        }

        if (obstacles.Count > 0)
        {
            obstacles.Sort((a, b) => a.worldX.CompareTo(b.worldX));
            EnforcePassableTransitions(obstacles, worldProgress);
            if (obstacles.Count > 0)
            {
                obstacles.Sort((a, b) => a.worldX.CompareTo(b.worldX));
                lastObstacleWorldX = obstacles[obstacles.Count - 1].worldX;
                hasLastHazard = true;
                lastHazardWorldX = obstacles[obstacles.Count - 1].worldX;
                lastHazardWasOverhead = obstacles[obstacles.Count - 1].isOverhead;
                blockedRanges = BuildBlockedRanges(obstacles);
                ComputeSafestWindow(usableStart, usableEnd, obstacles, out safePathStart, out safePathEnd);
            }
        }

        return obstacles;
    }

    private EncounterType ChooseEncounter(float worldProgress, int gameplayChunkIndex)
    {
        if (demandingStreak >= maxDemandingStreak || chunksSinceBreather >= forcedBreatherInterval)
        {
            return RegisterEncounterSelection(EncounterType.Breather);
        }

        float d = Mathf.Clamp01(worldProgress / 180f);
        int phase = Mathf.Abs(gameplayChunkIndex) % 8;

        if (phase == 0 || phase == 4)
        {
            return RegisterEncounterSelection(EncounterType.Breather);
        }

        var options = new List<EncounterType>();
        var weights = new List<float>();
        bool allowCombo = chunksSinceCombo >= minChunksBetweenCombos;

        switch (phase)
        {
            case 1:
                AddWeight(options, weights, EncounterType.JumpBasic, 0.62f);
                AddWeight(options, weights, EncounterType.Breather, 0.18f);
                if (worldProgress > 16f)
                {
                    AddWeight(options, weights, EncounterType.SlideBasic, Mathf.Lerp(0.14f, 0.22f, d));
                }

                break;
            case 2:
                AddWeight(options, weights, EncounterType.JumpBasic, Mathf.Lerp(0.42f, 0.28f, d));
                AddWeight(options, weights, EncounterType.SlideBasic, Mathf.Lerp(0.18f, 0.3f, d));
                AddWeight(options, weights, EncounterType.Breather, 0.16f);
                if (worldProgress > 42f)
                {
                    AddWeight(options, weights, EncounterType.JumpDouble, Mathf.Lerp(0.08f, 0.18f, d));
                }

                break;
            case 3:
                AddWeight(options, weights, EncounterType.JumpBasic, Mathf.Lerp(0.36f, 0.24f, d));
                AddWeight(options, weights, EncounterType.SlideBasic, Mathf.Lerp(0.18f, 0.24f, d));
                AddWeight(options, weights, EncounterType.Breather, 0.14f);
                if (worldProgress > 56f)
                {
                    AddWeight(options, weights, EncounterType.JumpDouble, Mathf.Lerp(0.16f, 0.24f, d));
                }

                break;
            case 5:
                AddWeight(options, weights, EncounterType.SlideBasic, Mathf.Lerp(0.48f, 0.36f, d));
                AddWeight(options, weights, EncounterType.JumpBasic, 0.2f);
                AddWeight(options, weights, EncounterType.Breather, 0.18f);
                if (worldProgress > 88f && allowCombo)
                {
                    AddWeight(options, weights, EncounterType.JumpSlideCombo, Mathf.Lerp(0.06f, 0.16f, d));
                }

                break;
            case 6:
                AddWeight(options, weights, EncounterType.JumpBasic, 0.24f);
                AddWeight(options, weights, EncounterType.SlideBasic, 0.18f);
                AddWeight(options, weights, EncounterType.Breather, 0.14f);
                if (worldProgress > 70f)
                {
                    AddWeight(options, weights, EncounterType.JumpDouble, Mathf.Lerp(0.18f, 0.24f, d));
                }

                if (worldProgress > 92f && allowCombo)
                {
                    AddWeight(options, weights, EncounterType.JumpSlideCombo, Mathf.Lerp(0.08f, 0.22f, d));
                }

                break;
            default:
                AddWeight(options, weights, EncounterType.Breather, 0.24f);
                AddWeight(options, weights, EncounterType.JumpBasic, 0.18f);
                AddWeight(options, weights, EncounterType.SlideBasic, 0.2f);
                if (worldProgress > 112f && allowCombo)
                {
                    AddWeight(options, weights, EncounterType.SlideJumpCombo, Mathf.Lerp(0.08f, 0.2f, d));
                }

                if (worldProgress > 80f)
                {
                    AddWeight(options, weights, EncounterType.JumpDouble, Mathf.Lerp(0.1f, 0.18f, d));
                }

                break;
        }

        for (int i = 0; i < options.Count; i++)
        {
            if (options[i] == lastEncounterType)
            {
                weights[i] *= 0.35f;
            }
        }

        float total = 0f;
        for (int i = 0; i < weights.Count; i++)
        {
            total += weights[i];
        }

        float r = Random.value * Mathf.Max(0.0001f, total);
        float acc = 0f;
        for (int i = 0; i < options.Count; i++)
        {
            acc += weights[i];
            if (r <= acc)
            {
                return RegisterEncounterSelection(options[i]);
            }
        }

        return RegisterEncounterSelection(EncounterType.JumpBasic);
    }

    private static void AddWeight(List<EncounterType> options, List<float> weights, EncounterType type, float weight)
    {
        if (weight <= 0f)
        {
            return;
        }

        options.Add(type);
        weights.Add(weight);
    }

    private static float[] BuildBeats(float usableStart, float usableEnd)
    {
        float span = usableEnd - usableStart;
        return new[]
        {
            usableStart + span * 0.12f,
            usableStart + span * 0.31f,
            usableStart + span * 0.5f,
            usableStart + span * 0.69f,
            usableStart + span * 0.88f
        };
    }

    private bool TrySpawnGroundObstacle(
        Transform parent,
        float worldX,
        int beatIndex,
        float worldProgress,
        float requiredGap,
        List<ObstacleSpawnData> obstacles)
    {
        float[] widthSet = { 0.94f, 1.02f, 1.1f };
        float[] earlyHeights = { 1.08f, 1.2f, 1.3f };
        float[] lateHeights = { 1.18f, 1.34f, 1.5f };
        float width = widthSet[Mathf.Clamp((beatIndex + Random.Range(0, 2)) % widthSet.Length, 0, widthSet.Length - 1)];
        float[] heightSet = worldProgress < 78f ? earlyHeights : lateHeights;
        float height = heightSet[Mathf.Clamp((beatIndex + Mathf.RoundToInt(worldProgress * 0.03f)) % heightSet.Length, 0, heightSet.Length - 1)];
        if (!CanPlaceObstacle(worldX, false, obstacles, requiredGap, worldProgress))
        {
            return false;
        }

        float localX = WorldToLocalX(parent, worldX);
        float localY = -2.35f + height * 0.5f;

        var obstacle = new GameObject("Obstacle");
        obstacle.transform.SetParent(parent);
        obstacle.transform.localPosition = new Vector3(localX, localY, 0f);
        obstacle.transform.localScale = new Vector3(width, height, 1f);

        var sr = obstacle.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.GroundObstacleSprite;
        sr.color = Color.Lerp(visuals.obstacleColor, Color.black, Random.Range(0.04f, 0.16f));
        sr.sortingOrder = 9;
        AddGroundHistoricalTrim(obstacle.transform);

        obstacle.AddComponent<BoxCollider2D>();
        obstacle.AddComponent<RunnerObstacle>();

        obstacles.Add(new ObstacleSpawnData
        {
            instance = obstacle,
            worldX = worldX,
            width = width,
            height = height,
            isOverhead = false,
            beatIndex = beatIndex
        });

        lastObstacleWorldX = Mathf.Max(lastObstacleWorldX, worldX);
        RegisterHazard(worldX, false);
        return true;
    }

    private bool TrySpawnOverheadObstacle(
        Transform parent,
        float worldX,
        int beatIndex,
        float worldProgress,
        float requiredGap,
        List<ObstacleSpawnData> obstacles)
    {
        float[] widthSet = { 1.28f, 1.38f, 1.5f };
        float[] heightSet = { 0.38f, 0.44f, 0.5f };
        float width = widthSet[Mathf.Clamp((beatIndex + Mathf.RoundToInt(worldProgress * 0.025f)) % widthSet.Length, 0, widthSet.Length - 1)];
        float height = heightSet[Mathf.Clamp((beatIndex + 1) % heightSet.Length, 0, heightSet.Length - 1)];
        if (!CanPlaceObstacle(worldX, true, obstacles, requiredGap, worldProgress))
        {
            return false;
        }

        float localX = WorldToLocalX(parent, worldX);
        float localY = -0.98f;

        var obstacle = new GameObject("Obstacle");
        obstacle.transform.SetParent(parent);
        obstacle.transform.localPosition = new Vector3(localX, localY, 0f);
        obstacle.transform.localScale = new Vector3(width, height, 1f);

        var sr = obstacle.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.OverheadObstacleSprite;
        sr.color = Color.Lerp(visuals.obstacleColor, Color.black, Random.Range(0.08f, 0.2f));
        sr.sortingOrder = 9;
        AddOverheadHistoricalTrim(obstacle.transform);
        AddOverheadSupport(parent, localX, localY + height * 0.5f, width);

        obstacle.AddComponent<BoxCollider2D>();
        obstacle.AddComponent<RunnerObstacle>();

        obstacles.Add(new ObstacleSpawnData
        {
            instance = obstacle,
            worldX = worldX,
            width = width,
            height = height,
            isOverhead = true,
            beatIndex = beatIndex
        });

        lastObstacleWorldX = Mathf.Max(lastObstacleWorldX, worldX);
        RegisterHazard(worldX, true);
        return true;
    }

    private void AddOverheadSupport(Transform chunkRoot, float localX, float obstacleTopLocalY, float obstacleWidth)
    {
        float ceilingAnchorLocalY = GetCeilingAnchorLocalY(chunkRoot);
        float supportLength = ceilingAnchorLocalY - obstacleTopLocalY;
        if (supportLength < 0.16f)
        {
            return;
        }

        float halfGap = Mathf.Clamp(obstacleWidth * 0.34f, 0.24f, 0.46f);
        CreateSingleSupport(chunkRoot, localX - halfGap, obstacleTopLocalY, supportLength);
        CreateSingleSupport(chunkRoot, localX + halfGap, obstacleTopLocalY, supportLength);
    }

    private void CreateSingleSupport(Transform chunkRoot, float x, float obstacleTopLocalY, float supportLength)
    {
        var support = new GameObject("CeilingSupport");
        support.transform.SetParent(chunkRoot, false);
        support.transform.localPosition = new Vector3(x, obstacleTopLocalY + supportLength * 0.5f, -0.01f);
        support.transform.localScale = Vector3.one;

        var col = support.AddComponent<BoxCollider2D>();
        col.size = new Vector2(supportColliderWidth, supportLength);
        support.AddComponent<RunnerObstacle>();

        Color poleColor = overheadSupportColor;
        Color bandColor = Color.Lerp(overheadSupportColor, visuals.roofAccentColor, 0.35f);
        Color knotColor = Color.Lerp(visuals.obstacleColor, Color.black, 0.18f);

        var pole = new GameObject("Pole");
        pole.transform.SetParent(support.transform, false);
        pole.transform.localPosition = Vector3.zero;
        pole.transform.localScale = new Vector3(supportColliderWidth, supportLength, 1f);

        var poleSr = pole.AddComponent<SpriteRenderer>();
        poleSr.sprite = RunnerSpriteUtil.PixelSprite;
        poleSr.color = poleColor;
        poleSr.sortingOrder = 8;

        AddSupportBand(support.transform, 0.44f * supportLength, bandColor, 8);
        AddSupportBand(support.transform, -0.36f * supportLength, bandColor, 8);

        var knot = new GameObject("Knot");
        knot.transform.SetParent(support.transform, false);
        knot.transform.localPosition = new Vector3(0f, -supportLength * 0.5f + 0.08f, -0.01f);
        knot.transform.localScale = new Vector3(0.18f, 0.12f, 1f);

        var knotSr = knot.AddComponent<SpriteRenderer>();
        knotSr.sprite = RunnerSpriteUtil.PixelSprite;
        knotSr.color = knotColor;
        knotSr.sortingOrder = 9;

        var cap = new GameObject("CeilingCap");
        cap.transform.SetParent(chunkRoot, false);
        cap.transform.localPosition = new Vector3(x, GetCeilingAnchorLocalY(chunkRoot), -0.01f);
        cap.transform.localScale = new Vector3(0.42f, 0.1f, 1f);

        var capSr = cap.AddComponent<SpriteRenderer>();
        capSr.sprite = RunnerSpriteUtil.PixelSprite;
        capSr.color = Color.Lerp(visuals.buildingColor, visuals.roofAccentColor, 0.32f);
        capSr.sortingOrder = 7;
    }

    private float GetCeilingAnchorLocalY(Transform chunkRoot)
    {
        return ceilingAnchorY - chunkRoot.position.y;
    }

    private void AddObstacleOverlay(
        Transform parent,
        float scaleX,
        float scaleY,
        Color color,
        int sortingOrder,
        float yOffset = 0f)
    {
        var overlay = new GameObject("Overlay");
        overlay.transform.SetParent(parent, false);
        overlay.transform.localPosition = new Vector3(0f, yOffset, -0.01f);
        overlay.transform.localScale = new Vector3(scaleX, scaleY, 1f);

        var sr = overlay.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void AddGroundHistoricalTrim(Transform obstacle)
    {
        Color capColor = Color.Lerp(visuals.obstacleColor, Color.white, 0.24f);
        Color plaqueColor = Color.Lerp(visuals.obstacleColor, visuals.roofAccentColor, 0.42f);
        Color accentColor = Color.Lerp(visuals.stripePrimaryColor, visuals.obstacleColor, 0.24f);

        AddObstacleOverlay(obstacle, 0.88f, 0.14f, capColor, 10, 0.42f);
        AddObstacleOverlay(obstacle, 0.64f, 0.32f, plaqueColor, 10, 0.04f);
        AddObstacleOverlay(obstacle, 0.48f, 0.06f, accentColor, 11, 0.02f);
        AddObstacleOverlay(obstacle, 0.9f, 0.12f, Color.Lerp(visuals.obstacleColor, Color.black, 0.22f), 11, -0.42f);

        CreateTrimStud(obstacle, -0.24f, 0.05f, accentColor);
        CreateTrimStud(obstacle, 0.24f, 0.05f, accentColor);
    }

    private void AddOverheadHistoricalTrim(Transform obstacle)
    {
        Color plaqueColor = Color.Lerp(visuals.obstacleColor, visuals.roofAccentColor, 0.38f);
        Color beamColor = Color.Lerp(visuals.obstacleColor, Color.white, 0.14f);
        Color tasselColor = Color.Lerp(visuals.stripePrimaryColor, visuals.obstacleColor, 0.22f);

        AddObstacleOverlay(obstacle, 0.9f, 0.18f, beamColor, 10, 0.18f);
        AddObstacleOverlay(obstacle, 0.58f, 0.34f, plaqueColor, 10, -0.02f);
        AddObstacleOverlay(obstacle, 0.8f, 0.08f, Color.Lerp(visuals.obstacleColor, Color.black, 0.26f), 11, -0.24f);

        CreateHangingCharm(obstacle, -0.28f, tasselColor);
        CreateHangingCharm(obstacle, 0f, tasselColor);
        CreateHangingCharm(obstacle, 0.28f, tasselColor);
    }

    private void AddSupportBand(Transform supportRoot, float yOffset, Color color, int sortingOrder)
    {
        var band = new GameObject("Band");
        band.transform.SetParent(supportRoot, false);
        band.transform.localPosition = new Vector3(0f, yOffset, -0.01f);
        band.transform.localScale = new Vector3(0.18f, 0.08f, 1f);

        var sr = band.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void CreateTrimStud(Transform parent, float x, float y, Color color)
    {
        var stud = new GameObject("Stud");
        stud.transform.SetParent(parent, false);
        stud.transform.localPosition = new Vector3(x, y, -0.01f);
        stud.transform.localScale = new Vector3(0.08f, 0.08f, 1f);

        var sr = stud.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = 11;
    }

    private void CreateHangingCharm(Transform parent, float x, Color color)
    {
        var charm = new GameObject("Charm");
        charm.transform.SetParent(parent, false);
        charm.transform.localPosition = new Vector3(x, -0.26f, -0.01f);
        charm.transform.localScale = new Vector3(0.06f, 0.16f, 1f);

        var sr = charm.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = 11;
    }

    private bool CanPlaceObstacle(
        float worldX,
        bool isOverhead,
        List<ObstacleSpawnData> existing,
        float requiredGap,
        float worldProgress)
    {
        if (worldX < noObstacleBeforeX + 1f)
        {
            return false;
        }

        float globalGap = Mathf.Max(minGlobalRecoveryGap, requiredGap);
        if (Mathf.Abs(worldX - lastObstacleWorldX) < globalGap)
        {
            return false;
        }

        float localGap = Mathf.Max(minObstacleGap, requiredGap * 0.7f);
        for (int i = 0; i < existing.Count; i++)
        {
            float transitionGap = GetTransitionGap(existing[i].isOverhead, isOverhead, worldProgress);
            if (Mathf.Abs(worldX - existing[i].worldX) < Mathf.Max(localGap, transitionGap))
            {
                return false;
            }
        }

        if (hasLastHazard)
        {
            float requiredTransition = GetTransitionGap(lastHazardWasOverhead, isOverhead, worldProgress);
            if (worldX - lastHazardWorldX < requiredTransition)
            {
                return false;
            }
        }

        return true;
    }

    private EncounterType RegisterEncounterSelection(EncounterType selected)
    {
        lastEncounterType = selected;
        chunksSinceCombo++;
        if (selected == EncounterType.Breather)
        {
            demandingStreak = 0;
            chunksSinceBreather = 0;
            return selected;
        }

        chunksSinceBreather++;
        if (selected == EncounterType.JumpSlideCombo || selected == EncounterType.SlideJumpCombo)
        {
            chunksSinceCombo = 0;
        }

        if (IsDemandingEncounter(selected))
        {
            demandingStreak++;
        }
        else
        {
            demandingStreak = Mathf.Max(0, demandingStreak - 1);
        }

        return selected;
    }

    private static bool IsDemandingEncounter(EncounterType encounter)
    {
        return encounter == EncounterType.JumpDouble ||
               encounter == EncounterType.JumpSlideCombo ||
               encounter == EncounterType.SlideJumpCombo;
    }

    private float EstimatePlayerSpeed(float worldProgress)
    {
        float t = Mathf.Clamp01(worldProgress / 220f);
        return Mathf.Lerp(7f, 14f, t);
    }

    private float GetBaseGap(float worldProgress)
    {
        float speed = EstimatePlayerSpeed(worldProgress);
        return Mathf.Max(minGlobalRecoveryGap, speed * reactionTime);
    }

    private float GetJumpToSlideGap(float worldProgress)
    {
        float speed = EstimatePlayerSpeed(worldProgress);
        return Mathf.Max(minGlobalRecoveryGap, speed * jumpToSlideSwitchTime);
    }

    private float GetSlideToJumpGap(float worldProgress)
    {
        float speed = EstimatePlayerSpeed(worldProgress);
        return Mathf.Max(minGlobalRecoveryGap, speed * slideToJumpSwitchTime);
    }

    private float GetTransitionGap(bool fromOverhead, bool toOverhead, float worldProgress)
    {
        float speed = EstimatePlayerSpeed(worldProgress);
        if (!fromOverhead && !toOverhead)
        {
            return Mathf.Max(minGlobalRecoveryGap, speed * 0.56f);
        }

        if (fromOverhead && toOverhead)
        {
            return Mathf.Max(minGlobalRecoveryGap * 0.8f, speed * 0.3f);
        }

        if (!fromOverhead && toOverhead)
        {
            return GetJumpToSlideGap(worldProgress);
        }

        return GetSlideToJumpGap(worldProgress);
    }

    private void EnforcePassableTransitions(List<ObstacleSpawnData> obstacles, float worldProgress)
    {
        if (obstacles.Count < 2)
        {
            return;
        }

        for (int i = 1; i < obstacles.Count; i++)
        {
            ObstacleSpawnData previous = obstacles[i - 1];
            ObstacleSpawnData current = obstacles[i];
            if (previous.isOverhead == current.isOverhead)
            {
                continue;
            }

            float required = GetTransitionGap(previous.isOverhead, current.isOverhead, worldProgress);

            if (current.worldX - previous.worldX >= required)
            {
                continue;
            }

            if (current.instance != null)
            {
                Destroy(current.instance);
            }

            obstacles.RemoveAt(i);
            i--;
        }
    }

    private void RegisterHazard(float worldX, bool isOverhead)
    {
        hasLastHazard = true;
        lastHazardWorldX = worldX;
        lastHazardWasOverhead = isOverhead;
    }

    private static List<Vector2> BuildBlockedRanges(List<ObstacleSpawnData> obstacles)
    {
        var ranges = new List<Vector2>(obstacles.Count);
        for (int i = 0; i < obstacles.Count; i++)
        {
            var obstacle = obstacles[i];
            ranges.Add(new Vector2(obstacle.worldX - obstacle.width * 0.7f, obstacle.worldX + obstacle.width * 0.7f));
        }

        return ranges;
    }

    private static float WorldToLocalX(Transform parent, float worldX)
    {
        return worldX - parent.position.x;
    }

    private void ComputeSafestWindow(float usableStart, float usableEnd, List<ObstacleSpawnData> obstacles, out float safeStart, out float safeEnd)
    {
        safeStart = usableStart;
        safeEnd = usableEnd;
        float bestLen = -1f;
        float cursor = usableStart;

        for (int i = 0; i < obstacles.Count; i++)
        {
            float left = obstacles[i].worldX - obstacles[i].width * 0.8f;
            if (left - cursor > bestLen)
            {
                bestLen = left - cursor;
                safeStart = cursor;
                safeEnd = left;
            }

            cursor = Mathf.Max(cursor, obstacles[i].worldX + obstacles[i].width * 0.8f);
        }

        if (usableEnd - cursor > bestLen)
        {
            safeStart = cursor;
            safeEnd = usableEnd;
        }

        if (safeEnd - safeStart < 1f)
        {
            float center = (usableStart + usableEnd) * 0.5f;
            safeStart = center - 0.6f;
            safeEnd = center + 0.6f;
        }
    }

    private void CreateCollectibles(
        Transform parent,
        float chunkStartX,
        float worldProgress,
        float safePathStart,
        float safePathEnd,
        List<Vector2> blockedRanges,
        List<ObstacleSpawnData> obstacles,
        EncounterType encounter)
    {
        CollectiblePatternType collectibleType = ChooseCollectiblePattern(worldProgress, encounter);
        if (collectibleType == CollectiblePatternType.Coins)
        {
            CreateCoins(parent, chunkStartX, worldProgress, safePathStart, safePathEnd, blockedRanges, obstacles, encounter);
            return;
        }

        CreateScrollPattern(parent, chunkStartX, worldProgress, safePathStart, safePathEnd, blockedRanges, obstacles, encounter);
    }

    private CollectiblePatternType ChooseCollectiblePattern(float worldProgress, EncounterType encounter)
    {
        float scrollWeight = Mathf.Lerp(0.18f, 0.38f, Mathf.Clamp01(worldProgress / 180f));
        if (encounter == EncounterType.Breather)
        {
            scrollWeight -= 0.08f;
        }

        if (encounter == EncounterType.JumpSlideCombo || encounter == EncounterType.SlideJumpCombo)
        {
            scrollWeight += 0.1f;
        }

        if (lastCollectibleType == CollectiblePatternType.Scrolls)
        {
            scrollWeight *= 0.62f;
        }

        lastCollectibleType = Random.value < Mathf.Clamp01(scrollWeight)
            ? CollectiblePatternType.Scrolls
            : CollectiblePatternType.Coins;
        return lastCollectibleType;
    }

    private void CreateCoins(
        Transform parent,
        float chunkStartX,
        float worldProgress,
        float safePathStart,
        float safePathEnd,
        List<Vector2> blockedRanges,
        List<ObstacleSpawnData> obstacles,
        EncounterType encounter)
    {
        float minX = chunkStartX + obstacleEdgePadding;
        float maxX = chunkStartX + chunkWidth - obstacleEdgePadding;
        int coinBudget = GetCoinBudget(encounter, worldProgress);
        int coinsPlaced = 0;
        float lineStart = Mathf.Clamp(safePathStart + 0.35f, minX, maxX - 0.35f);
        float lineEnd = Mathf.Clamp(safePathEnd - 0.35f, lineStart + coinMinGap * 2f, maxX);
        TryGetNthObstacle(obstacles, false, 0, out ObstacleSpawnData firstGround);
        TryGetNthObstacle(obstacles, false, 1, out ObstacleSpawnData secondGround);
        TryGetNthObstacle(obstacles, true, 0, out ObstacleSpawnData firstOverhead);

        switch (encounter)
        {
            case EncounterType.Breather:
                if (Random.value < 0.55f)
                {
                    coinsPlaced += SpawnCoinLine(parent, lineStart, lineEnd, -0.62f, obstacles, coinBudget, coinObstacleAvoidance);
                }
                else
                {
                    coinsPlaced += SpawnCoinWave(parent, lineStart, lineEnd, -0.44f, 0.22f, obstacles, coinBudget, coinObstacleAvoidance * 0.9f);
                }

                break;
            case EncounterType.JumpBasic:
                if (firstGround.instance != null)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.42f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.54f, arcStart + coinMinGap * 2f, maxX);
                    coinsPlaced += SpawnCoinArc(parent, arcStart, arcEnd, -0.72f, 1.22f, obstacles, coinBudget, coinObstacleAvoidance * 0.65f, firstGround);
                }
                else
                {
                    coinsPlaced += SpawnCoinArc(parent, lineStart, lineEnd, -0.64f, 1.12f, obstacles, coinBudget, coinObstacleAvoidance);
                }

                break;
            case EncounterType.JumpDouble:
                if (firstGround.instance != null)
                {
                    int firstBudget = Mathf.Max(4, coinBudget / 2);
                    float firstArcStart = Mathf.Clamp(firstGround.worldX - 1.28f, minX, maxX);
                    float firstArcEnd = Mathf.Clamp(firstGround.worldX + 1.32f, firstArcStart + coinMinGap * 2f, maxX);
                    coinsPlaced += SpawnCoinArc(parent, firstArcStart, firstArcEnd, -0.7f, 1.08f, obstacles, firstBudget, coinObstacleAvoidance * 0.68f, firstGround);
                }

                if (secondGround.instance != null)
                {
                    float secondArcStart = Mathf.Clamp(secondGround.worldX - 1.32f, minX, maxX);
                    float secondArcEnd = Mathf.Clamp(secondGround.worldX + 1.38f, secondArcStart + coinMinGap * 2f, maxX);
                    coinsPlaced += SpawnCoinArc(parent, secondArcStart, secondArcEnd, -0.66f, 1.2f, obstacles, coinBudget - coinsPlaced, coinObstacleAvoidance * 0.68f, secondGround);
                }

                break;
            case EncounterType.SlideBasic:
                if (firstOverhead.instance != null)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.96f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.96f, slideStart + coinMinGap, maxX);
                    coinsPlaced += SpawnCoinLine(parent, slideStart, slideEnd, -1.56f, obstacles, coinBudget, coinObstacleAvoidance * 0.48f, firstOverhead);
                }
                else
                {
                    coinsPlaced += SpawnCoinLine(parent, lineStart, lineEnd, -0.92f, obstacles, coinBudget, coinObstacleAvoidance);
                }

                break;
            case EncounterType.JumpSlideCombo:
                if (firstGround.instance != null)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.36f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.44f, arcStart + coinMinGap * 2f, maxX);
                    coinsPlaced += SpawnCoinArc(parent, arcStart, arcEnd, -0.7f, 1.14f, obstacles, Mathf.Max(4, coinBudget / 2), coinObstacleAvoidance * 0.65f, firstGround);
                }

                if (firstOverhead.instance != null && coinsPlaced < coinBudget)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.9f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.9f, slideStart + coinMinGap, maxX);
                    coinsPlaced += SpawnCoinLine(parent, slideStart, slideEnd, -1.54f, obstacles, coinBudget - coinsPlaced, coinObstacleAvoidance * 0.48f, firstOverhead);
                }

                break;
            case EncounterType.SlideJumpCombo:
                if (firstOverhead.instance != null)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.92f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.92f, slideStart + coinMinGap, maxX);
                    coinsPlaced += SpawnCoinLine(parent, slideStart, slideEnd, -1.54f, obstacles, Mathf.Max(3, coinBudget / 2), coinObstacleAvoidance * 0.48f, firstOverhead);
                }

                if (firstGround.instance != null && coinsPlaced < coinBudget)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.34f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.4f, arcStart + coinMinGap * 2f, maxX);
                    coinsPlaced += SpawnCoinArc(parent, arcStart, arcEnd, -0.68f, 1.18f, obstacles, coinBudget - coinsPlaced, coinObstacleAvoidance * 0.65f, firstGround);
                }

                break;
        }

        if (coinsPlaced < minCoinsPerChunk)
        {
            coinsPlaced += SpawnCoinLine(
                parent,
                Mathf.Clamp(((lineStart + lineEnd) * 0.5f) - coinMinGap * 1.5f, minX, maxX),
                Mathf.Clamp(((lineStart + lineEnd) * 0.5f) + coinMinGap * 1.5f, minX, maxX),
                -0.64f,
                obstacles,
                minCoinsPerChunk - coinsPlaced);
        }
    }

    private int GetCoinBudget(EncounterType encounter, float worldProgress)
    {
        int budget = 6;
        switch (encounter)
        {
            case EncounterType.Breather:
                budget = 5;
                break;
            case EncounterType.JumpBasic:
            case EncounterType.SlideBasic:
                budget = 7;
                break;
            case EncounterType.JumpDouble:
                budget = 9;
                break;
            case EncounterType.JumpSlideCombo:
            case EncounterType.SlideJumpCombo:
                budget = 10;
                break;
        }

        if (worldProgress > 120f)
        {
            budget += 1;
        }

        return Mathf.Clamp(budget, minCoinsPerChunk, maxCoinsPerChunk);
    }

    private int SpawnCoinLine(
        Transform parent,
        float startWorldX,
        float endWorldX,
        float y,
        List<ObstacleSpawnData> obstacles,
        int budget,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (budget <= 0 || endWorldX < startWorldX)
        {
            return 0;
        }

        if (avoidance < 0f)
        {
            avoidance = coinObstacleAvoidance;
        }

        int placed = 0;
        float x = startWorldX;
        while (x <= endWorldX + 0.01f && placed < budget)
        {
            if (!IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                CreateCoin(parent, x, y);
                placed++;
            }

            x += coinMinGap;
        }

        return placed;
    }

    private int SpawnCoinArc(
        Transform parent,
        float startWorldX,
        float endWorldX,
        float startY,
        float peakY,
        List<ObstacleSpawnData> obstacles,
        int budget,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (budget <= 0 || endWorldX < startWorldX)
        {
            return 0;
        }

        if (avoidance < 0f)
        {
            avoidance = coinObstacleAvoidance;
        }

        int placed = 0;
        float distance = Mathf.Max(0.1f, endWorldX - startWorldX);
        int count = Mathf.Clamp(Mathf.RoundToInt(distance / coinMinGap), 2, 7);
        for (int i = 0; i <= count; i++)
        {
            if (placed >= budget)
            {
                break;
            }

            float t = count == 0 ? 0f : (float)i / count;
            float x = Mathf.Lerp(startWorldX, endWorldX, t);
            float y = Mathf.Lerp(startY, peakY, Mathf.Sin(t * Mathf.PI));
            if (IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                continue;
            }

            CreateCoin(parent, x, y);
            placed++;
        }

        return placed;
    }

    private int SpawnCoinWave(
        Transform parent,
        float startWorldX,
        float endWorldX,
        float centerY,
        float amplitude,
        List<ObstacleSpawnData> obstacles,
        int budget,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (budget <= 0 || endWorldX <= startWorldX)
        {
            return 0;
        }

        if (avoidance < 0f)
        {
            avoidance = coinObstacleAvoidance;
        }

        int placed = 0;
        float distance = endWorldX - startWorldX;
        int count = Mathf.Clamp(Mathf.RoundToInt(distance / coinMinGap), 3, 9);
        for (int i = 0; i <= count; i++)
        {
            if (placed >= budget)
            {
                break;
            }

            float t = (float)i / count;
            float x = Mathf.Lerp(startWorldX, endWorldX, t);
            float y = centerY + Mathf.Sin(t * Mathf.PI * 2f) * amplitude;
            if (IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                continue;
            }

            CreateCoin(parent, x, y);
            placed++;
        }

        return placed;
    }

    private int SpawnCoinVerticalArc(
        Transform parent,
        float centerX,
        float startY,
        float endY,
        float sideOffset,
        List<ObstacleSpawnData> obstacles,
        int budget,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (budget <= 0)
        {
            return 0;
        }

        if (avoidance < 0f)
        {
            avoidance = coinObstacleAvoidance;
        }

        int placed = 0;
        int count = Mathf.Clamp(Mathf.RoundToInt((endY - startY) / 0.22f), 4, 10);
        for (int i = 0; i <= count; i++)
        {
            if (placed >= budget)
            {
                break;
            }

            float t = count == 0 ? 0f : (float)i / count;
            float y = Mathf.Lerp(startY, endY, t);
            float x = centerX + Mathf.Sin(t * Mathf.PI) * sideOffset;
            if (IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                continue;
            }

            CreateCoin(parent, x, y);
            placed++;
        }

        return placed;
    }

    private bool TryGetNthObstacle(List<ObstacleSpawnData> obstacles, bool overhead, int index, out ObstacleSpawnData obstacle)
    {
        int found = 0;
        for (int i = 0; i < obstacles.Count; i++)
        {
            if (obstacles[i].isOverhead != overhead)
            {
                continue;
            }

            if (found == index)
            {
                obstacle = obstacles[i];
                return true;
            }

            found++;
        }

        obstacle = default(ObstacleSpawnData);
        return false;
    }

    private bool IsCollectibleBlocked(float worldX, List<ObstacleSpawnData> obstacles, float avoidance, ObstacleSpawnData? ignoredObstacle = null)
    {
        for (int i = 0; i < obstacles.Count; i++)
        {
            ObstacleSpawnData obstacle = obstacles[i];
            if (ignoredObstacle.HasValue && IsSameObstacle(obstacle, ignoredObstacle.Value))
            {
                continue;
            }

            float halfWidth = obstacle.width * 0.58f + avoidance;
            if (Mathf.Abs(worldX - obstacle.worldX) < halfWidth)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSameObstacle(ObstacleSpawnData a, ObstacleSpawnData b)
    {
        return a.isOverhead == b.isOverhead &&
               Mathf.Abs(a.worldX - b.worldX) < 0.01f &&
               Mathf.Abs(a.width - b.width) < 0.01f &&
               Mathf.Abs(a.height - b.height) < 0.01f;
    }

    private void CreateCoin(Transform parent, float worldX, float worldY)
    {
        var coin = new GameObject("Coin");
        coin.transform.SetParent(parent);
        coin.transform.position = new Vector3(worldX, worldY, 0f);
        coin.transform.localScale = new Vector3(0.66f, 0.66f, 1f);

        var sr = coin.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.AncientCoinSprite;
        sr.color = visuals.coinColor;
        sr.sortingOrder = 11;

        var trigger = coin.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.36f;

        coin.AddComponent<RunnerCoin>().Configure(manager);
    }

    private void CreateScrollPattern(
        Transform parent,
        float chunkStartX,
        float worldProgress,
        float safePathStart,
        float safePathEnd,
        List<Vector2> blockedRanges,
        List<ObstacleSpawnData> obstacles,
        EncounterType encounter)
    {
        if (chunkStartX + chunkWidth < noObstacleBeforeX + 4f)
        {
            return;
        }

        float d = Mathf.Clamp01(worldProgress / 140f);
        float chance = scrollSpawnChance + d * 0.08f;
        if (encounter == EncounterType.JumpSlideCombo || encounter == EncounterType.SlideJumpCombo)
        {
            chance += 0.12f;
        }
        else if (encounter == EncounterType.Breather)
        {
            chance -= 0.06f;
        }

        chance = Mathf.Clamp(chance, 0.08f, 0.42f);
        if (Random.value > chance)
        {
            return;
        }

        float minX = chunkStartX + obstacleEdgePadding;
        float maxX = chunkStartX + chunkWidth - obstacleEdgePadding;
        float startX = Mathf.Clamp(safePathStart + 0.45f, minX, maxX);
        float endX = Mathf.Clamp(safePathEnd - 0.45f, startX, maxX);
        float segmentCenter = (startX + endX) * 0.5f;
        TryGetNthObstacle(obstacles, false, 0, out ObstacleSpawnData firstGround);
        TryGetNthObstacle(obstacles, false, 1, out ObstacleSpawnData secondGround);
        TryGetNthObstacle(obstacles, true, 0, out ObstacleSpawnData firstOverhead);
        if (segmentCenter - lastScrollWorldX < minScrollGap)
        {
            return;
        }

        float spacing = coinMinGap * 1.6f;
        switch (encounter)
        {
            case EncounterType.Breather:
                SpawnScrollLine(parent, startX, endX, -0.36f, spacing, obstacles, 2, scrollObstacleAvoidance);
                break;
            case EncounterType.JumpBasic:
                if (firstGround.instance != null)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.16f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.28f, arcStart + spacing, maxX);
                    SpawnScrollArc(parent, arcStart, arcEnd, -0.54f, 1.06f, spacing, obstacles, 3, scrollObstacleAvoidance * 0.72f, firstGround);
                }
                else
                {
                    SpawnScrollArc(parent, startX, endX, -0.42f, 1.06f, spacing, obstacles, 3, scrollObstacleAvoidance);
                }

                break;
            case EncounterType.JumpDouble:
                if (firstGround.instance != null)
                {
                    float firstArcStart = Mathf.Clamp(firstGround.worldX - 1.04f, minX, maxX);
                    float firstArcEnd = Mathf.Clamp(firstGround.worldX + 1.08f, firstArcStart + spacing, maxX);
                    SpawnScrollArc(parent, firstArcStart, firstArcEnd, -0.56f, 0.92f, spacing, obstacles, 2, scrollObstacleAvoidance * 0.72f, firstGround);
                }

                if (secondGround.instance != null)
                {
                    float secondArcStart = Mathf.Clamp(secondGround.worldX - 1.08f, minX, maxX);
                    float secondArcEnd = Mathf.Clamp(secondGround.worldX + 1.18f, secondArcStart + spacing, maxX);
                    SpawnScrollArc(parent, secondArcStart, secondArcEnd, -0.5f, 1.02f, spacing, obstacles, 2, scrollObstacleAvoidance * 0.72f, secondGround);
                }

                break;
            case EncounterType.SlideBasic:
                if (firstOverhead.instance != null)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.78f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.78f, slideStart + spacing * 0.8f, maxX);
                    SpawnScrollLine(parent, slideStart, slideEnd, -1.5f, spacing * 0.9f, obstacles, 2, scrollObstacleAvoidance * 0.56f, firstOverhead);
                }
                else
                {
                    SpawnScrollLine(parent, startX, endX, -0.82f, spacing, obstacles, 2, scrollObstacleAvoidance);
                }

                break;
            case EncounterType.JumpSlideCombo:
                if (firstGround.instance != null)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.12f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.16f, arcStart + spacing, maxX);
                    SpawnScrollArc(parent, arcStart, arcEnd, -0.5f, 0.98f, spacing, obstacles, 2, scrollObstacleAvoidance * 0.72f, firstGround);
                }

                if (firstOverhead.instance != null)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.76f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.76f, slideStart + spacing * 0.8f, maxX);
                    SpawnScrollLine(parent, slideStart, slideEnd, -1.48f, spacing, obstacles, 1, scrollObstacleAvoidance * 0.56f, firstOverhead);
                }

                break;
            case EncounterType.SlideJumpCombo:
                if (firstOverhead.instance != null)
                {
                    float slideStart = Mathf.Clamp(firstOverhead.worldX - 0.76f, minX, maxX);
                    float slideEnd = Mathf.Clamp(firstOverhead.worldX + 0.76f, slideStart + spacing * 0.8f, maxX);
                    SpawnScrollLine(parent, slideStart, slideEnd, -1.48f, spacing, obstacles, 1, scrollObstacleAvoidance * 0.56f, firstOverhead);
                }

                if (firstGround.instance != null)
                {
                    float arcStart = Mathf.Clamp(firstGround.worldX - 1.1f, minX, maxX);
                    float arcEnd = Mathf.Clamp(firstGround.worldX + 1.18f, arcStart + spacing, maxX);
                    SpawnScrollArc(parent, arcStart, arcEnd, -0.48f, 1.02f, spacing, obstacles, 2, scrollObstacleAvoidance * 0.72f, firstGround);
                }

                break;
        }

        lastScrollWorldX = segmentCenter;
    }

    private int SpawnScrollLine(
        Transform parent,
        float startWorldX,
        float endWorldX,
        float y,
        float spacing,
        List<ObstacleSpawnData> obstacles,
        int maxCount,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (avoidance < 0f)
        {
            avoidance = scrollObstacleAvoidance;
        }

        int placed = 0;
        float x = startWorldX;
        while (x <= endWorldX + 0.01f && placed < maxCount)
        {
            if (!IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                CreateScroll(parent, x, y);
                placed++;
            }

            x += spacing;
        }

        return placed;
    }

    private int SpawnScrollArc(
        Transform parent,
        float startWorldX,
        float endWorldX,
        float startY,
        float peakY,
        float spacing,
        List<ObstacleSpawnData> obstacles,
        int maxCount,
        float avoidance = -1f,
        ObstacleSpawnData? ignoredObstacle = null)
    {
        if (endWorldX <= startWorldX)
        {
            return 0;
        }

        if (avoidance < 0f)
        {
            avoidance = scrollObstacleAvoidance;
        }

        int placed = 0;
        int count = Mathf.Max(2, maxCount - 1);
        for (int i = 0; i <= count && placed < maxCount; i++)
        {
            float t = count == 0 ? 0f : (float)i / count;
            float x = Mathf.Lerp(startWorldX, endWorldX, t);
            float y = Mathf.Lerp(startY, peakY, Mathf.Sin(t * Mathf.PI));
            if (i > 0)
            {
                float prevX = Mathf.Lerp(startWorldX, endWorldX, (float)(i - 1) / count);
                if (x - prevX < spacing * 0.72f)
                {
                    continue;
                }
            }

            if (IsCollectibleBlocked(x, obstacles, avoidance, ignoredObstacle))
            {
                continue;
            }

            CreateScroll(parent, x, y);
            placed++;
        }

        return placed;
    }

    private void CreateScroll(Transform parent, float worldX, float worldY)
    {
        var scroll = new GameObject("Scroll");
        scroll.transform.SetParent(parent);
        scroll.transform.position = new Vector3(worldX, worldY, 0f);
        scroll.transform.localScale = new Vector3(0.82f, 0.56f, 1f);

        var sr = scroll.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.ScrollSprite;
        sr.color = visuals.scrollColor;
        sr.sortingOrder = 12;

        var trigger = scroll.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(0.95f, 0.8f);

        scroll.AddComponent<RunnerScroll>().Configure(manager);
    }

    private void CreateBackground(Transform parent)
    {
        var architectureSlots = new List<Vector2>();
        float architectureMinX = -chunkWidth * 0.48f;
        float architectureMaxX = chunkWidth * 0.48f;

        CreateBackgroundBand(parent, -5.6f, 8f, visuals.skyBottomColor, -8);
        CreateBackgroundBand(parent, 2.8f, 1.5f, visuals.skyBandColor, -10);
        CreateBackgroundBand(parent, 0.5f, 3.6f, visuals.skyTopColor, -9);
        CreateBackgroundBand(parent, -1.15f, 1.2f, visuals.mistBandColor, -7);
        CreateBackgroundBand(parent, -0.45f, 0.52f, new Color(visuals.skyBandColor.r, visuals.skyBandColor.g, visuals.skyBandColor.b, 0.18f), -6);

        if (Random.value < 0.26f)
        {
            float glowX = Random.Range(-chunkWidth * 0.34f, chunkWidth * 0.34f);
            float glowY = Random.Range(1.9f, 3.3f);
            float glowScale = Random.Range(1.8f, 2.8f);
            CreateSunWash(parent, glowX, glowY, glowScale);
        }

        int mistRibbonCount = Random.Range(1, 3);
        for (int i = 0; i < mistRibbonCount; i++)
        {
            float x = Random.Range(-chunkWidth * 0.5f, chunkWidth * 0.5f);
            float y = Random.Range(-0.1f, 2.1f);
            float width = Random.Range(4.6f, 8.8f);
            float height = Random.Range(0.42f, 0.78f);
            CreateMistRibbon(parent, x, y, width, height, i % 2 == 0 ? -5 : -6);
        }

        int cloudCount = Random.Range(visuals.cloudCountMin, visuals.cloudCountMax + 1);
        for (int i = 0; i < cloudCount; i++)
        {
            float x = Random.Range(-chunkWidth * 0.46f, chunkWidth * 0.46f);
            float y = Random.Range(cloudDriftYMin, cloudDriftYMax);
            float scale = Random.Range(0.8f, 1.36f);
            CreateCloudCluster(parent, x, y, scale);
        }

        int hillCount = Random.Range(visuals.hillCountMin, visuals.hillCountMax + 1);
        for (int i = 0; i < hillCount; i++)
        {
            float x = Random.Range(-chunkWidth * 0.56f, chunkWidth * 0.56f);
            float w = Random.Range(4.8f, 8.8f);
            float h = Random.Range(1.9f, 3.2f);
            CreateBackgroundMass(parent, "FarHill", x, -2.4f, w, h, visuals.mountainFarColor, -3);
        }

        int nearHillCount = Random.Range(2, 4);
        for (int i = 0; i < nearHillCount; i++)
        {
            float x = Random.Range(-chunkWidth * 0.56f, chunkWidth * 0.56f);
            float w = Random.Range(3.6f, 6.4f);
            float h = Random.Range(1.4f, 2.35f);
            CreateBackgroundMass(parent, "NearHill", x, -2.6f, w, h, visuals.mountainNearColor, -2);
        }

        int foregroundHillCount = Random.Range(1, 3);
        for (int i = 0; i < foregroundHillCount; i++)
        {
            float x = Random.Range(-chunkWidth * 0.54f, chunkWidth * 0.54f);
            float w = Random.Range(4.2f, 7.2f);
            float h = Random.Range(1.1f, 1.8f);
            CreateBackgroundMass(parent, "ForegroundHill", x, -2.85f, w, h, visuals.hillColor, -1);
        }

        int buildingCount = Random.Range(visuals.buildingCountMin, visuals.buildingCountMax + 1);
        for (int i = 0; i < buildingCount; i++)
        {
            float w = PickArchitectureValue(new[] { 1.5f, 1.76f, 2.04f, 2.34f }, 0.94f, 1.06f);
            float h = PickArchitectureValue(new[] { 2.8f, 3.35f, 3.95f, 4.55f }, 0.94f, 1.05f);
            float halfSpan = w * 0.82f;
            if (!TryFindBackgroundSlot(architectureSlots, architectureMinX, architectureMaxX, halfSpan, backgroundArchitectureGap, out float x))
            {
                continue;
            }

            CreateBackgroundBuilding(parent, x, w, h, Random.Range(-0.08f, 0.12f));
        }

        int pavilionCount = Random.Range(1, 3);
        for (int i = 0; i < pavilionCount; i++)
        {
            float scale = PickArchitectureValue(new[] { 0.94f, 1.08f, 1.22f }, 0.96f, 1.05f);
            float halfSpan = 0.92f * scale;
            if (!TryFindBackgroundSlot(architectureSlots, architectureMinX, architectureMaxX, halfSpan, backgroundArchitectureGap * 0.92f, out float x))
            {
                continue;
            }

            CreatePavilionSilhouette(parent, x, -2.08f + Random.Range(-0.04f, 0.06f), scale);
        }

        int pagodaCount = Random.Range(0, 2);
        for (int i = 0; i < pagodaCount; i++)
        {
            float scale = PickArchitectureValue(new[] { 1.02f, 1.16f, 1.3f }, 0.97f, 1.05f);
            float halfSpan = 0.84f * scale;
            if (!TryFindBackgroundSlot(architectureSlots, architectureMinX, architectureMaxX, halfSpan, backgroundArchitectureGap * 1.06f, out float x))
            {
                continue;
            }

            CreatePagodaSilhouette(parent, x, -2.1f + Random.Range(-0.03f, 0.05f), scale);
        }

        if (Random.value < 0.78f)
        {
            float scale = PickArchitectureValue(new[] { 0.9f, 1.02f, 1.12f }, 0.97f, 1.04f);
            float halfSpan = 0.7f * scale;
            if (TryFindBackgroundSlot(architectureSlots, architectureMinX, architectureMaxX, halfSpan, backgroundArchitectureGap * 0.88f, out float x))
            {
                CreateAncientWellSilhouette(parent, x, -2.3f + Random.Range(-0.03f, 0.03f), scale);
            }
        }
    }

    private bool TryFindBackgroundSlot(
        List<Vector2> occupiedSlots,
        float minX,
        float maxX,
        float halfSpan,
        float padding,
        out float x)
    {
        float left = minX + halfSpan;
        float right = maxX - halfSpan;
        if (right <= left)
        {
            x = 0f;
            return false;
        }

        for (int attempt = 0; attempt < backgroundPlacementAttempts; attempt++)
        {
            float candidate = Random.Range(left, right);
            if (!IsBackgroundSlotBlocked(occupiedSlots, candidate, halfSpan, padding))
            {
                occupiedSlots.Add(new Vector2(candidate, halfSpan));
                x = candidate;
                return true;
            }
        }

        occupiedSlots.Sort((a, b) => a.x.CompareTo(b.x));
        float currentLeft = left;
        for (int i = 0; i <= occupiedSlots.Count; i++)
        {
            float nextRightEdge = i < occupiedSlots.Count
                ? occupiedSlots[i].x - occupiedSlots[i].y - padding - halfSpan
                : right;

            if (nextRightEdge - currentLeft >= 0.08f)
            {
                x = (currentLeft + nextRightEdge) * 0.5f;
                occupiedSlots.Add(new Vector2(x, halfSpan));
                return true;
            }

            if (i < occupiedSlots.Count)
            {
                currentLeft = Mathf.Max(currentLeft, occupiedSlots[i].x + occupiedSlots[i].y + padding + halfSpan);
            }
        }

        x = 0f;
        return false;
    }

    private static bool IsBackgroundSlotBlocked(List<Vector2> occupiedSlots, float candidateX, float halfSpan, float padding)
    {
        for (int i = 0; i < occupiedSlots.Count; i++)
        {
            if (Mathf.Abs(candidateX - occupiedSlots[i].x) < occupiedSlots[i].y + halfSpan + padding)
            {
                return true;
            }
        }

        return false;
    }

    private static float PickArchitectureValue(float[] presets, float jitterMin, float jitterMax)
    {
        if (presets == null || presets.Length == 0)
        {
            return 1f;
        }

        float baseValue = presets[Random.Range(0, presets.Length)];
        return baseValue * Random.Range(jitterMin, jitterMax);
    }

    private void CreateBackgroundBuilding(Transform parent, float x, float width, float height, float yOffset)
    {
        var building = new GameObject("Building");
        building.transform.SetParent(parent, false);
        building.transform.localPosition = new Vector3(x, -1.22f + yOffset + height * 0.5f, 0f);
        building.transform.localScale = new Vector3(width, height, 1f);

        var sr = building.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = visuals.buildingColor;
        sr.sortingOrder = 0;

        if (Random.value < 0.52f)
        {
            float annexWidth = width * Random.Range(0.26f, 0.4f);
            float annexHeight = height * Random.Range(0.34f, 0.5f);
            float direction = Random.value < 0.5f ? -1f : 1f;
            CreateBuildingAnnex(building.transform, direction, width, height, annexWidth, annexHeight);
        }

        CreateRoofSilhouette(building.transform, width, height);
    }

    private void CreateBuildingAnnex(Transform building, float direction, float mainWidth, float mainHeight, float annexWidth, float annexHeight)
    {
        var annex = new GameObject("Annex");
        annex.transform.SetParent(building, false);
        annex.transform.localPosition = new Vector3(direction * (mainWidth * 0.5f - annexWidth * 0.42f), -mainHeight * 0.5f + annexHeight * 0.5f, -0.01f);
        annex.transform.localScale = new Vector3(annexWidth, annexHeight, 1f);

        var sr = annex.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = Color.Lerp(visuals.buildingColor, visuals.mountainNearColor, 0.12f);
        sr.sortingOrder = 0;

        CreateRoofSilhouette(annex.transform, annexWidth, annexHeight);
    }

    private void CreateBackgroundMass(Transform parent, string name, float x, float yBase, float width, float height, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = new Vector3(x, yBase + height * 0.5f, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void CreateCloudCluster(Transform parent, float x, float y, float scale)
    {
        var root = new GameObject("Cloud");
        root.transform.SetParent(parent);
        root.transform.localPosition = new Vector3(x, y, 0f);

        CreateCloudPuff(root.transform, -0.62f * scale, -0.08f * scale, 0.82f * scale, 0.34f * scale);
        CreateCloudPuff(root.transform, -0.36f * scale, -0.04f * scale, 1.02f * scale, 0.56f * scale);
        CreateCloudPuff(root.transform, 0f, 0.05f * scale, 1.26f * scale, 0.64f * scale);
        CreateCloudPuff(root.transform, 0.42f * scale, -0.02f * scale, 0.9f * scale, 0.5f * scale);
        CreateCloudPuff(root.transform, 0.78f * scale, -0.06f * scale, 0.7f * scale, 0.28f * scale);
    }

    private void CreateCloudPuff(Transform parent, float x, float y, float width, float height)
    {
        var puff = new GameObject("Puff");
        puff.transform.SetParent(parent);
        puff.transform.localPosition = new Vector3(x, y, 0f);
        puff.transform.localScale = new Vector3(width, height, 1f);

        var sr = puff.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = visuals.cloudColor;
        sr.sortingOrder = -4;
    }

    private void CreateMistRibbon(Transform parent, float x, float y, float width, float height, int sortingOrder)
    {
        var ribbon = new GameObject("MistRibbon");
        ribbon.transform.SetParent(parent, false);
        ribbon.transform.localPosition = new Vector3(x, y, 0f);
        ribbon.transform.localScale = new Vector3(width, height, 1f);

        var sr = ribbon.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.ShadowSprite;
        sr.color = new Color(visuals.mistBandColor.r, visuals.mistBandColor.g, visuals.mistBandColor.b, visuals.mistBandColor.a * 0.85f);
        sr.sortingOrder = sortingOrder;
    }

    private void CreateSunWash(Transform parent, float x, float y, float scale)
    {
        var glow = new GameObject("SunWash");
        glow.transform.SetParent(parent, false);
        glow.transform.localPosition = new Vector3(x, y, 0f);
        glow.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = glow.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.SunWashSprite;
        sr.color = new Color(visuals.skyBandColor.r, visuals.skyBandColor.g, visuals.skyBandColor.b, 0.18f);
        sr.sortingOrder = -11;
    }

    private void CreatePavilionSilhouette(Transform parent, float x, float yBase, float scale)
    {
        var root = new GameObject("Pavilion");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, yBase, 0f);

        Color pavilionColor = Color.Lerp(visuals.buildingColor, visuals.mountainNearColor, 0.35f);
        Color roofColor = Color.Lerp(visuals.roofAccentColor, visuals.buildingColor, 0.12f);

        CreatePavilionPart(root.transform, "Platform", new Vector3(0f, 0.18f * scale, 0f), new Vector3(1.4f * scale, 0.14f * scale, 1f), pavilionColor, -1);
        CreatePavilionPart(root.transform, "Body", new Vector3(0f, 0.62f * scale, 0f), new Vector3(0.88f * scale, 0.76f * scale, 1f), pavilionColor, -1);
        CreatePavilionPart(root.transform, "Roof", new Vector3(0f, 1.08f * scale, -0.01f), new Vector3(1.5f * scale, 0.16f * scale, 1f), roofColor, 0);
        CreatePavilionPart(root.transform, "RoofWingL", new Vector3(-0.82f * scale, 1.0f * scale, -0.01f), new Vector3(0.18f * scale, 0.1f * scale, 1f), roofColor, 0);
        CreatePavilionPart(root.transform, "RoofWingR", new Vector3(0.82f * scale, 1.0f * scale, -0.01f), new Vector3(0.18f * scale, 0.1f * scale, 1f), roofColor, 0);
        CreatePavilionPart(root.transform, "ColumnL", new Vector3(-0.26f * scale, 0.44f * scale, -0.01f), new Vector3(0.08f * scale, 0.7f * scale, 1f), roofColor, 0);
        CreatePavilionPart(root.transform, "ColumnR", new Vector3(0.26f * scale, 0.44f * scale, -0.01f), new Vector3(0.08f * scale, 0.7f * scale, 1f), roofColor, 0);
    }

    private void CreatePavilionPart(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }

    private void CreatePagodaSilhouette(Transform parent, float x, float yBase, float scale)
    {
        var root = new GameObject("Pagoda");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, yBase, 0f);

        Color towerColor = Color.Lerp(visuals.buildingColor, visuals.mountainNearColor, 0.16f);
        Color roofColor = Color.Lerp(visuals.roofAccentColor, visuals.buildingColor, 0.08f);
        Color trimColor = Color.Lerp(visuals.cloudColor, visuals.roofAccentColor, 0.2f);

        CreatePavilionPart(root.transform, "Base", new Vector3(0f, 0.16f * scale, 0f), new Vector3(1.3f * scale, 0.14f * scale, 1f), towerColor, 0);

        for (int i = 0; i < 4; i++)
        {
            float tierScale = 1f - i * 0.14f;
            float levelY = 0.42f * scale + i * 0.54f * scale;
            CreatePavilionPart(root.transform, "TierBody" + i, new Vector3(0f, levelY, 0f), new Vector3(0.58f * tierScale * scale, 0.42f * scale, 1f), towerColor, 0);
            CreatePavilionPart(root.transform, "TierRoof" + i, new Vector3(0f, levelY + 0.28f * scale, -0.01f), new Vector3(1.1f * tierScale * scale, 0.1f * scale, 1f), roofColor, 1);
            CreatePavilionPart(root.transform, "TierWingL" + i, new Vector3(-0.6f * tierScale * scale, levelY + 0.22f * scale, -0.01f), new Vector3(0.14f * scale, 0.08f * scale, 1f), roofColor, 1);
            CreatePavilionPart(root.transform, "TierWingR" + i, new Vector3(0.6f * tierScale * scale, levelY + 0.22f * scale, -0.01f), new Vector3(0.14f * scale, 0.08f * scale, 1f), roofColor, 1);
            CreatePavilionPart(root.transform, "TierTrim" + i, new Vector3(0f, levelY + 0.1f * scale, -0.01f), new Vector3(0.3f * tierScale * scale, 0.04f * scale, 1f), trimColor, 1);
        }

        CreatePavilionPart(root.transform, "Spire", new Vector3(0f, 2.45f * scale, -0.02f), new Vector3(0.06f * scale, 0.36f * scale, 1f), roofColor, 1);
        CreatePavilionPart(root.transform, "SpireFinial", new Vector3(0f, 2.66f * scale, -0.02f), new Vector3(0.18f * scale, 0.08f * scale, 1f), trimColor, 1);
    }

    private void CreateAncientWellSilhouette(Transform parent, float x, float yBase, float scale)
    {
        var root = new GameObject("AncientWell");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = new Vector3(x, yBase, 0f);

        Color stoneColor = Color.Lerp(visuals.buildingColor, visuals.mountainNearColor, 0.22f);
        Color woodColor = Color.Lerp(visuals.roofAccentColor, visuals.buildingColor, 0.18f);
        Color bucketColor = Color.Lerp(visuals.obstacleColor, visuals.roofAccentColor, 0.24f);

        CreatePavilionPart(root.transform, "WellBase", new Vector3(0f, 0.16f * scale, 0f), new Vector3(0.98f * scale, 0.32f * scale, 1f), stoneColor, 1);
        CreatePavilionPart(root.transform, "WellLip", new Vector3(0f, 0.26f * scale, -0.01f), new Vector3(1.08f * scale, 0.08f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "WellOpening", new Vector3(0f, 0.19f * scale, -0.02f), new Vector3(0.52f * scale, 0.12f * scale, 1f), new Color(0.08f, 0.09f, 0.11f, 0.62f), 1);
        CreatePavilionPart(root.transform, "PostL", new Vector3(-0.28f * scale, 0.66f * scale, -0.01f), new Vector3(0.08f * scale, 0.84f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "PostR", new Vector3(0.28f * scale, 0.66f * scale, -0.01f), new Vector3(0.08f * scale, 0.84f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "Beam", new Vector3(0f, 1.06f * scale, -0.01f), new Vector3(0.78f * scale, 0.08f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "Roof", new Vector3(0f, 1.3f * scale, -0.02f), new Vector3(1.18f * scale, 0.12f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "RoofWingL", new Vector3(-0.66f * scale, 1.22f * scale, -0.02f), new Vector3(0.14f * scale, 0.08f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "RoofWingR", new Vector3(0.66f * scale, 1.22f * scale, -0.02f), new Vector3(0.14f * scale, 0.08f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "Rope", new Vector3(0f, 0.82f * scale, -0.02f), new Vector3(0.03f * scale, 0.4f * scale, 1f), woodColor, 2);
        CreatePavilionPart(root.transform, "Bucket", new Vector3(0f, 0.58f * scale, -0.02f), new Vector3(0.16f * scale, 0.22f * scale, 1f), bucketColor, 2);
    }

    private void CreateRoofSilhouette(Transform building, float width, float height)
    {
        float roofWidth = width * Random.Range(1.06f, 1.22f);
        var roof = new GameObject("Roof");
        roof.transform.SetParent(building, false);
        roof.transform.localPosition = new Vector3(0f, height * 0.5f + 0.12f, -0.01f);
        roof.transform.localScale = new Vector3(roofWidth, 0.16f, 1f);

        var roofSr = roof.AddComponent<SpriteRenderer>();
        roofSr.sprite = RunnerSpriteUtil.PixelSprite;
        roofSr.color = visuals.roofAccentColor;
        roofSr.sortingOrder = 1;

        var eaveL = new GameObject("EaveL");
        eaveL.transform.SetParent(building, false);
        eaveL.transform.localPosition = new Vector3(-roofWidth * 0.5f - 0.04f, height * 0.5f + 0.07f, -0.01f);
        eaveL.transform.localScale = new Vector3(0.14f, 0.1f, 1f);
        var eaveLSr = eaveL.AddComponent<SpriteRenderer>();
        eaveLSr.sprite = RunnerSpriteUtil.PixelSprite;
        eaveLSr.color = visuals.roofAccentColor;
        eaveLSr.sortingOrder = 1;

        var eaveR = new GameObject("EaveR");
        eaveR.transform.SetParent(building, false);
        eaveR.transform.localPosition = new Vector3(roofWidth * 0.5f + 0.04f, height * 0.5f + 0.07f, -0.01f);
        eaveR.transform.localScale = new Vector3(0.14f, 0.1f, 1f);
        var eaveRSr = eaveR.AddComponent<SpriteRenderer>();
        eaveRSr.sprite = RunnerSpriteUtil.PixelSprite;
        eaveRSr.color = visuals.roofAccentColor;
        eaveRSr.sortingOrder = 1;
    }

    private void CreateBackgroundBand(Transform parent, float y, float height, Color color, int sortingOrder)
    {
        var band = new GameObject("SkyBand");
        band.transform.SetParent(parent);
        band.transform.localPosition = new Vector3(0f, y + height * 0.5f, 0f);
        band.transform.localScale = new Vector3(chunkWidth, height, 1f);

        var sr = band.AddComponent<SpriteRenderer>();
        sr.sprite = RunnerSpriteUtil.PixelSprite;
        sr.color = color;
        sr.sortingOrder = sortingOrder;
    }
}
