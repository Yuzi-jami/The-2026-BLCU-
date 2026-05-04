using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class RunnerBootstrap
{
    
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        int currentLevel = 0;
        if (scene.name == "关卡3") 
        {
            currentLevel = 3;
            // 3. 将关卡信息存下来，供 GameManager 和 Spawner 使用
            PlayerPrefs.SetInt("TargetLevel", currentLevel);

            // 4. 只有在真正的游戏关卡场景中，才执行原有的构建逻辑
            BuildIfNeeded();
        }
        else
        {
            return;
        }

  
    }

    private static void BuildIfNeeded()
    {
        const float groundTopY = -2.3f;

        if (Object.FindObjectOfType<RunnerGameManager>() != null)
        {
            return;
        }

        Application.targetFrameRate = 120;
        Physics2D.gravity = new Vector2(0f, -36f);

        var visualSettings = Object.FindObjectOfType<RunnerVisualSettings>();
        if (visualSettings == null)
        {
            visualSettings = new GameObject("RunnerVisualSettings").AddComponent<RunnerVisualSettings>();
        }

        var managerObject = new GameObject("RunnerGame");
        var manager = managerObject.AddComponent<RunnerGameManager>();

        var playerObject = new GameObject("RunnerPlayer");
        playerObject.transform.position = new Vector3(-6f, groundTopY + 0.08f, 0f);

        var playerVisual = new GameObject("Visual");
        playerVisual.transform.SetParent(playerObject.transform, false);
        var playerRenderer = playerVisual.AddComponent<SpriteRenderer>();
        Sprite[] runFrames;
        Sprite[] slideFrames;
        Sprite[] jumpFrames;
        bool hasSequenceSet = RunnerSpriteUtil.TryLoadRunnerSequenceSet(out runFrames, out slideFrames, out jumpFrames);
        playerRenderer.sprite = hasSequenceSet ? runFrames[0] : RunnerSpriteUtil.PixelSprite;
        playerRenderer.color = hasSequenceSet ? Color.white : visualSettings.playerColor;
        playerRenderer.sortingOrder = 10;
        float sequenceScale = 1f;
        if (hasSequenceSet && runFrames != null && runFrames.Length > 0)
        {
            Rect runOpaqueRect = RunnerSpriteUtil.GetOpaqueLocalRect(runFrames[0]);
            float opaqueHeight = Mathf.Max(0.1f, runOpaqueRect.height);
            sequenceScale = Mathf.Clamp(1.88f / opaqueHeight, 1f, 2.65f);
        }

        playerVisual.transform.localScale = hasSequenceSet
            ? new Vector3(sequenceScale, sequenceScale, 1f)
            : new Vector3(1.12f, 1.12f, 1f);

        var shadow = new GameObject("Shadow");
        shadow.transform.SetParent(playerObject.transform, false);
        shadow.transform.localPosition = new Vector3(0f, -0.88f, 0.02f);
        shadow.transform.localScale = new Vector3(0.7f, 0.16f, 1f);
        var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = RunnerSpriteUtil.ShadowSprite;
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.22f);
        shadowRenderer.sortingOrder = 5;

        var playerCollider = playerObject.AddComponent<BoxCollider2D>();
        if (hasSequenceSet && runFrames != null && runFrames.Length > 0)
        {
            float visibleHeight = GetMaxOpaqueHeight(sequenceScale, runFrames, jumpFrames, slideFrames);
            float visibleWidth = GetMaxOpaqueWidth(sequenceScale, runFrames, jumpFrames, slideFrames);
            float colliderHeight = Mathf.Clamp(visibleHeight * 0.78f, 1.18f, 1.68f);
            float colliderWidth = Mathf.Clamp(visibleWidth * 0.62f, 0.54f, 0.82f);
            playerCollider.size = new Vector2(colliderWidth, colliderHeight);
            playerCollider.offset = new Vector2(0f, colliderHeight * 0.5f - 0.03f);

            float colliderBottomLocalY = playerCollider.offset.y - playerCollider.size.y * 0.5f;
            float footLocalY = RunnerSpriteUtil.GetOpaqueBottomLocalY(runFrames[0]) * sequenceScale;
            playerVisual.transform.localPosition = new Vector3(0f, colliderBottomLocalY - footLocalY + 0.02f, 0f);
        }
        else
        {
            playerCollider.size = new Vector2(1.02f, 1.18f);
            playerCollider.offset = new Vector2(0f, 0.54f);
            playerVisual.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        }

        float colliderBottomWorldOffset = playerCollider.offset.y - playerCollider.size.y * 0.5f;
        playerObject.transform.position = new Vector3(-6f, groundTopY - colliderBottomWorldOffset + 0.01f, 0f);
        shadow.transform.localPosition = new Vector3(0f, playerCollider.offset.y - playerCollider.size.y * 0.5f + 0.02f, 0.02f);
        shadow.transform.localScale = new Vector3(Mathf.Clamp(playerCollider.size.x * 1.18f, 0.66f, 0.9f), 0.16f, 1f);

        var playerBody = playerObject.AddComponent<Rigidbody2D>();
        playerBody.gravityScale = 1f;
        playerBody.freezeRotation = true;
        playerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        playerBody.interpolation = RigidbodyInterpolation2D.Interpolate;

        var player = playerObject.AddComponent<RunnerController>();
        player.Configure(manager);
        var playerShadow = playerObject.AddComponent<RunnerPlayerShadow>();
        playerShadow.Configure(shadow.transform, shadowRenderer, player);
        var playerVisualAnim = playerObject.AddComponent<RunnerPlayerVisual>();
        playerVisualAnim.Configure(playerVisual.transform, playerRenderer, player, runFrames, slideFrames, jumpFrames);

        var spawnerObject = new GameObject("LevelSpawner");
        var spawner = spawnerObject.AddComponent<RunnerTileSpawner>();
        spawner.Configure(manager, player.transform, visualSettings);

        Camera camera = Camera.main;
        if (camera == null)
        {
            camera = Object.FindObjectOfType<Camera>();
        }

        if (camera == null)
        {
            camera = new GameObject("Main Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
        }

        var follow = camera.gameObject.GetComponent<RunnerCameraFollow>();
        if (follow == null)
        {
            follow = camera.gameObject.AddComponent<RunnerCameraFollow>();
        }

        follow.Configure(player.transform, manager);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.transform.position = new Vector3(-2f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = visualSettings.cameraBackgroundColor;

        BuildGuofengHud(manager, visualSettings);
    }

    private static void BuildGuofengHud(RunnerGameManager manager, RunnerVisualSettings visualSettings)
    {
        Font uiFont = ResolveChineseFont();
        Sprite topPanelSprite = visualSettings.uiTopPanelSprite != null ? visualSettings.uiTopPanelSprite : RunnerSpriteUtil.UiPaperSprite;
        Sprite topFrameSprite = visualSettings.uiTopPanelBorderSprite != null ? visualSettings.uiTopPanelBorderSprite : RunnerSpriteUtil.UiFrameSprite;
        Sprite statusPanelSprite = visualSettings.uiStatusPanelSprite != null ? visualSettings.uiStatusPanelSprite : RunnerSpriteUtil.UiPaperSprite;
        Sprite statusFrameSprite = visualSettings.uiStatusBorderSprite != null ? visualSettings.uiStatusBorderSprite : RunnerSpriteUtil.UiFrameSprite;
        Sprite plaqueSprite = RunnerSpriteUtil.UiPlaqueSprite;
        Sprite rodSprite = RunnerSpriteUtil.UiRodSprite;
        Color sealColor = Color.Lerp(visualSettings.uiTopPanelBorderColor, new Color(0.74f, 0.13f, 0.12f, 1f), 0.52f);
        Color lacquerColor = Color.Lerp(visualSettings.uiTopPanelBorderColor, new Color(0.24f, 0.1f, 0.07f, 1f), 0.35f);
        Color bronzeColor = new Color(0.62f, 0.47f, 0.24f, 0.96f);
        Color cordColor = new Color(0.22f, 0.12f, 0.09f, 0.68f);
        Color brushTint = new Color(
            visualSettings.uiTopPanelBorderColor.r,
            visualSettings.uiTopPanelBorderColor.g,
            visualSettings.uiTopPanelBorderColor.b,
            0.2f);

        var canvas = new GameObject("HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var uiRoot = canvas.GetComponent<Canvas>();
        uiRoot.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var fullShade = CreatePanel(
            "ScreenVignette",
            canvas.transform,
            new Vector2(0f, 0f),
            new Vector2(1f, 1f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            visualSettings.uiScreenVignetteColor,
            visualSettings.uiScreenVignetteSprite
        );
        fullShade.raycastTarget = false;

        CreateImageElement(
            "TopHangerBeam",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            new Vector2(1360f, 18f),
            bronzeColor,
            rodSprite
        );

        CreateImageElement(
            "TopCordLeft",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-432f, -58f),
            new Vector2(7f, 94f),
            cordColor,
            RunnerSpriteUtil.PixelSprite
        );

        CreateImageElement(
            "TopCordRight",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(432f, -58f),
            new Vector2(7f, 94f),
            cordColor,
            RunnerSpriteUtil.PixelSprite
        );

        CreateImageElement(
            "TopCordSealLeft",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(-432f, -105f),
            new Vector2(22f, 22f),
            new Color(sealColor.r, sealColor.g, sealColor.b, 0.88f),
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        CreateImageElement(
            "TopCordSealRight",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(432f, -105f),
            new Vector2(22f, 22f),
            new Color(sealColor.r, sealColor.g, sealColor.b, 0.88f),
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        CreateImageElement(
            "TopWash",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -42f),
            new Vector2(1500f, 188f),
            brushTint,
            RunnerSpriteUtil.UiBrushSprite
        );

        CreateImageElement(
            "TopRodUpper",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -20f),
            new Vector2(1322f, 16f),
            lacquerColor,
            rodSprite
        );

        CreatePanel(
            "TopPanelFrame",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(1288f, 144f),
            visualSettings.uiTopPanelBorderColor,
            topFrameSprite
        );

        var topInner = CreatePanel(
            "TopPanelInner",
            canvas.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -28f),
            new Vector2(1248f, 124f),
            visualSettings.uiTopPanelColor,
            topPanelSprite
        );

        CreateImageElement(
            "TopPanelBrush",
            topInner.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(960f, 38f),
            new Color(brushTint.r, brushTint.g, brushTint.b, 0.11f),
            RunnerSpriteUtil.UiBrushSprite
        );

        CreateImageElement(
            "TopRodLower",
            topInner.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 7f),
            new Vector2(1180f, 14f),
            new Color(lacquerColor.r, lacquerColor.g, lacquerColor.b, 0.92f),
            rodSprite
        );

        var titleFrame = CreateImageElement(
            "TitlePlaque",
            topInner.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(42f, 1f),
            new Vector2(286f, 86f),
            lacquerColor,
            plaqueSprite
        );

        CreateImageElement(
            "TitleRod",
            titleFrame.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(264f, 12f),
            bronzeColor,
            rodSprite
        );

        CreateImageElement(
            "TitleSeal",
            titleFrame.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(24f, 0f),
            new Vector2(34f, 34f),
            sealColor,
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        CreateImageElement(
            "TitleStudRight",
            titleFrame.transform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-22f, 0f),
            new Vector2(12f, 12f),
            bronzeColor,
            RunnerSpriteUtil.PixelSprite
        );

        var titleText = CreateText(
            "GameTitle",
            titleFrame.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(24f, 0f),
            new Vector2(176f, 66f),
            uiFont,
            36,
            new Color(0.96f, 0.91f, 0.84f, 1f),
            TextAnchor.MiddleCenter
        );
        titleText.text = "\u4e91\u884c\u5f55";

        CreateImageElement(
            "ScoreDividerL",
            topInner.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-364f, 0f),
            new Vector2(138f, 12f),
            bronzeColor,
            rodSprite
        );

        CreateImageElement(
            "ScoreDividerR",
            topInner.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(364f, 0f),
            new Vector2(138f, 12f),
            bronzeColor,
            rodSprite
        );

        var scoreText = CreateText(
            "ScoreText",
            topInner.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -3f),
            new Vector2(744f, 98f),
            uiFont,
            32,
            visualSettings.uiPrimaryTextColor,
            TextAnchor.MiddleCenter
        );

        var pauseFrame = CreateImageElement(
            "PausePlaque",
            topInner.transform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(-34f, 0f),
            new Vector2(216f, 62f),
            new Color(lacquerColor.r, lacquerColor.g, lacquerColor.b, 0.96f),
            plaqueSprite
        );

        var pauseTip = CreateText(
            "PauseTip",
            pauseFrame.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(180f, 46f),
            uiFont,
            22,
            new Color(0.96f, 0.91f, 0.84f, 1f),
            TextAnchor.MiddleCenter
        );
        pauseTip.text = "Esc / P \u6b47\u811a";

        CreateImageElement(
            "TopSealLeft",
            topInner.transform,
            new Vector2(0f, 0f),
            new Vector2(0f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(94f, 11f),
            new Vector2(20f, 20f),
            sealColor,
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        CreateImageElement(
            "TopSealRight",
            topInner.transform,
            new Vector2(1f, 0f),
            new Vector2(1f, 0f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-94f, 11f),
            new Vector2(20f, 20f),
            sealColor,
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        var statusRoot = new GameObject("StatusRoot", typeof(RectTransform), typeof(CanvasGroup));
        statusRoot.transform.SetParent(canvas.transform, false);
        var statusRootRect = statusRoot.GetComponent<RectTransform>();
        statusRootRect.anchorMin = new Vector2(0.5f, 0.5f);
        statusRootRect.anchorMax = new Vector2(0.5f, 0.5f);
        statusRootRect.pivot = new Vector2(0.5f, 0.5f);
        statusRootRect.anchoredPosition = new Vector2(0f, 18f);
        statusRootRect.sizeDelta = new Vector2(980f, 308f);
        var statusGroup = statusRoot.GetComponent<CanvasGroup>();
        statusGroup.alpha = 1f;
        statusGroup.interactable = false;
        statusGroup.blocksRaycasts = false;

        CreateImageElement(
            "StatusGlow",
            statusRoot.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f),
            new Vector2(860f, 82f),
            new Color(brushTint.r, brushTint.g, brushTint.b, 0.12f),
            RunnerSpriteUtil.UiBrushSprite
        );

        CreateImageElement(
            "StatusRodTop",
            statusRoot.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -8f),
            new Vector2(952f, 16f),
            bronzeColor,
            rodSprite
        );

        CreateImageElement(
            "StatusRodBottom",
            statusRoot.transform,
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f),
            new Vector2(0f, 8f),
            new Vector2(952f, 16f),
            bronzeColor,
            rodSprite
        );

        CreatePanel(
            "StatusFrame",
            statusRoot.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(972f, 296f),
            visualSettings.uiStatusBorderColor,
            statusFrameSprite
        );

        var statusPanel = CreatePanel(
            "StatusPanel",
            statusRoot.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(940f, 264f),
            visualSettings.uiStatusPanelColor,
            statusPanelSprite
        );

        CreateImageElement(
            "StatusBrush",
            statusPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 12f),
            new Vector2(640f, 54f),
            new Color(brushTint.r, brushTint.g, brushTint.b, 0.16f),
            RunnerSpriteUtil.UiBrushSprite
        );

        var statusLabelPlaque = CreateImageElement(
            "StatusLabelPlaque",
            statusPanel.transform,
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -30f),
            new Vector2(254f, 58f),
            lacquerColor,
            plaqueSprite
        );

        CreateImageElement(
            "StatusSealLeft",
            statusPanel.transform,
            new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(50f, -4f),
            new Vector2(28f, 28f),
            new Color(sealColor.r, sealColor.g, sealColor.b, 0.82f),
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        CreateImageElement(
            "StatusSealRight",
            statusPanel.transform,
            new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-50f, -4f),
            new Vector2(28f, 28f),
            new Color(sealColor.r, sealColor.g, sealColor.b, 0.82f),
            RunnerSpriteUtil.UiSealSprite,
            true
        );

        var statusLabel = CreateText(
            "StatusLabel",
            statusLabelPlaque.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 0f),
            new Vector2(190f, 44f),
            uiFont,
            24,
            new Color(0.96f, 0.91f, 0.84f, 1f),
            TextAnchor.MiddleCenter
        );
        statusLabel.text = "\u884c\u65c5\u5e16";

        var statusText = CreateText(
            "StatusText",
            statusPanel.transform,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, 14f),
            new Vector2(822f, 188f),
            uiFont,
            46,
            visualSettings.uiStatusTextColor,
            TextAnchor.MiddleCenter
        );

        manager.BindUi(scoreText, statusText, statusGroup);
    }

    private static float GetMaxOpaqueHeight(float scale, params Sprite[][] sequences)
    {
        float maxHeight = 0f;
        for (int i = 0; i < sequences.Length; i++)
        {
            Sprite[] sequence = sequences[i];
            if (sequence == null)
            {
                continue;
            }

            for (int j = 0; j < sequence.Length; j++)
            {
                if (sequence[j] == null)
                {
                    continue;
                }

                maxHeight = Mathf.Max(maxHeight, RunnerSpriteUtil.GetOpaqueLocalRect(sequence[j]).height * scale);
            }
        }

        return Mathf.Max(1f, maxHeight);
    }

    private static float GetMaxOpaqueWidth(float scale, params Sprite[][] sequences)
    {
        float maxWidth = 0f;
        for (int i = 0; i < sequences.Length; i++)
        {
            Sprite[] sequence = sequences[i];
            if (sequence == null)
            {
                continue;
            }

            for (int j = 0; j < sequence.Length; j++)
            {
                if (sequence[j] == null)
                {
                    continue;
                }

                maxWidth = Mathf.Max(maxWidth, RunnerSpriteUtil.GetOpaqueLocalRect(sequence[j]).width * scale);
            }
        }

        return Mathf.Max(0.5f, maxWidth);
    }

    private static Image CreatePanel(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        Sprite sprite = null)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        image.color = color;
        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private static Image CreateImageElement(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        Sprite sprite,
        bool preserveAspect = false)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var image = go.GetComponent<Image>();
        image.sprite = sprite != null ? sprite : RunnerSpriteUtil.PixelSprite;
        image.color = color;
        if (image.sprite != null && image.sprite.border.sqrMagnitude > 0f)
        {
            image.type = Image.Type.Sliced;
        }

        image.preserveAspect = preserveAspect;
        return image;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Font font,
        int fontSize,
        Color color,
        TextAnchor alignment)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Outline));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;

        var text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.supportRichText = true;
        text.lineSpacing = 1.08f;
        text.text = string.Empty;

        var outline = go.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.2f);
        outline.effectDistance = new Vector2(1.6f, -1.6f);

        return text;
    }

    private static Font ResolveChineseFont()
    {
        string[] fontCandidates =
        {
            "STKaiti",
            "KaiTi",
            "Microsoft YaHei",
            "SimHei",
            "Noto Serif CJK SC"
        };

        Font dynamicFont = Font.CreateDynamicFontFromOSFont(fontCandidates, 42);
        if (dynamicFont != null)
        {
            return dynamicFont;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
