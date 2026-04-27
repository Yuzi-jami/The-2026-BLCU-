using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;

public static class RunnerSpriteUtil
{
    private static Sprite pixelSprite;
    private static Sprite ancientCoinSprite;
    private static Sprite scrollSprite;
    private static Sprite ancientRunnerSprite;
    private static Sprite groundObstacleSprite;
    private static Sprite overheadObstacleSprite;
    private static Sprite shadowSprite;
    private static Sprite uiPaperSprite;
    private static Sprite uiFrameSprite;
    private static Sprite uiPlaqueSprite;
    private static Sprite uiRodSprite;
    private static Sprite uiSealSprite;
    private static Sprite uiBrushSprite;
    private static Sprite sunWashSprite;
    private static Texture2D externalSamuraiSheet;
    private static Sprite[] externalRunnerRunFrames;
    private static Sprite[] externalRunnerSlideFrames;
    private static Sprite[] externalRunnerJumpFrames;
    private static readonly Dictionary<int, Rect> opaqueLocalRectCache = new Dictionary<int, Rect>();

    public static Sprite PixelSprite
    {
        get
        {
            if (pixelSprite == null)
            {
                pixelSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f
                );
            }

            return pixelSprite;
        }
    }

    public static Sprite AncientCoinSprite
    {
        get
        {
            if (ancientCoinSprite == null)
            {
                ancientCoinSprite = CreateAncientCoinSprite();
            }

            return ancientCoinSprite;
        }
    }

    public static Sprite ScrollSprite
    {
        get
        {
            if (scrollSprite == null)
            {
                scrollSprite = CreateScrollSprite();
            }

            return scrollSprite;
        }
    }

    public static Sprite AncientRunnerSprite
    {
        get
        {
            if (ancientRunnerSprite == null)
            {
                ancientRunnerSprite = CreateAncientRunnerSprite();
            }

            return ancientRunnerSprite;
        }
    }

    public static Sprite GroundObstacleSprite
    {
        get
        {
            if (groundObstacleSprite == null)
            {
                groundObstacleSprite = CreateGroundObstacleSprite();
            }

            return groundObstacleSprite;
        }
    }

    public static Sprite OverheadObstacleSprite
    {
        get
        {
            if (overheadObstacleSprite == null)
            {
                overheadObstacleSprite = CreateOverheadObstacleSprite();
            }

            return overheadObstacleSprite;
        }
    }

    public static Sprite ShadowSprite
    {
        get
        {
            if (shadowSprite == null)
            {
                shadowSprite = CreateShadowSprite();
            }

            return shadowSprite;
        }
    }

    public static Sprite UiPaperSprite
    {
        get
        {
            if (uiPaperSprite == null)
            {
                uiPaperSprite = CreateUiPaperSprite();
            }

            return uiPaperSprite;
        }
    }

    public static Sprite UiFrameSprite
    {
        get
        {
            if (uiFrameSprite == null)
            {
                uiFrameSprite = CreateUiFrameSprite();
            }

            return uiFrameSprite;
        }
    }

    public static Sprite UiPlaqueSprite
    {
        get
        {
            if (uiPlaqueSprite == null)
            {
                uiPlaqueSprite = CreateUiPlaqueSprite();
            }

            return uiPlaqueSprite;
        }
    }

    public static Sprite UiRodSprite
    {
        get
        {
            if (uiRodSprite == null)
            {
                uiRodSprite = CreateUiRodSprite();
            }

            return uiRodSprite;
        }
    }

    public static Sprite UiSealSprite
    {
        get
        {
            if (uiSealSprite == null)
            {
                uiSealSprite = CreateUiSealSprite();
            }

            return uiSealSprite;
        }
    }

    public static Sprite UiBrushSprite
    {
        get
        {
            if (uiBrushSprite == null)
            {
                uiBrushSprite = CreateUiBrushSprite();
            }

            return uiBrushSprite;
        }
    }

    public static Sprite SunWashSprite
    {
        get
        {
            if (sunWashSprite == null)
            {
                sunWashSprite = CreateSunWashSprite();
            }

            return sunWashSprite;
        }
    }

    public static bool TryLoadExternalSamuraiSpriteSet(out Sprite idle, out Sprite[] runFrames, out Sprite slide)
    {
        idle = null;
        runFrames = null;
        slide = null;

        string localPath = Path.Combine(Application.dataPath, "Art/samurai_sprites/samurai.png");
        if (!File.Exists(localPath))
        {
            return false;
        }

        if (externalSamuraiSheet == null)
        {
            byte[] bytes = File.ReadAllBytes(localPath);
            externalSamuraiSheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            externalSamuraiSheet.LoadImage(bytes, false);
            externalSamuraiSheet.filterMode = FilterMode.Point;
            externalSamuraiSheet.wrapMode = TextureWrapMode.Clamp;
        }

        const int cols = 6;
        const int rows = 10;
        int frameWidth = externalSamuraiSheet.width / cols;
        int frameHeight = externalSamuraiSheet.height / rows;
        if (frameWidth <= 0 || frameHeight <= 0)
        {
            return false;
        }

        // Top row of this sheet is a clean standing/running sequence.
        idle = CreateCellSprite(externalSamuraiSheet, 0, 0, frameWidth, frameHeight);
        runFrames = new Sprite[6];
        for (int i = 0; i < 6; i++)
        {
            runFrames[i] = CreateCellSprite(externalSamuraiSheet, i, 0, frameWidth, frameHeight);
        }

        // A lower stance frame suitable for slide pose.
        slide = CreateCellSprite(externalSamuraiSheet, 2, 3, frameWidth, frameHeight);
        return true;
    }

    public static bool TryLoadRunnerSequenceSet(out Sprite[] runFrames, out Sprite[] slideFrames, out Sprite[] jumpFrames)
    {
        runFrames = null;
        slideFrames = null;
        jumpFrames = null;

        string root = Path.Combine(Application.dataPath, "Art/runner_sequences");
        if (!Directory.Exists(root))
        {
            return false;
        }

        if (externalRunnerRunFrames == null)
        {
            float runPixelsPerUnit;
            externalRunnerRunFrames = LoadSpriteSequence(Path.Combine(root, "run"), "run", 14, null, out runPixelsPerUnit, true);
            externalRunnerSlideFrames = LoadSpriteSequence(Path.Combine(root, "slide"), "huachan", 12, runPixelsPerUnit, out _, true);
            externalRunnerJumpFrames = LoadSpriteSequence(Path.Combine(root, "jump"), "jump", 12, runPixelsPerUnit, out _);
        }

        if (externalRunnerRunFrames == null || externalRunnerRunFrames.Length == 0 ||
            externalRunnerSlideFrames == null || externalRunnerSlideFrames.Length == 0 ||
            externalRunnerJumpFrames == null || externalRunnerJumpFrames.Length == 0)
        {
            return false;
        }

        runFrames = externalRunnerRunFrames;
        slideFrames = externalRunnerSlideFrames;
        jumpFrames = externalRunnerJumpFrames;
        return true;
    }

    public static Rect GetOpaqueLocalRect(Sprite sprite)
    {
        if (sprite == null)
        {
            return new Rect(-0.5f, -0.5f, 1f, 1f);
        }

        int cacheKey = sprite.GetInstanceID();
        if (opaqueLocalRectCache.TryGetValue(cacheKey, out Rect cached))
        {
            return cached;
        }

        Texture2D texture = sprite.texture;
        if (texture == null)
        {
            return new Rect(-0.5f, -0.5f, 1f, 1f);
        }

        Color32[] pixels = texture.GetPixels32();
        int texWidth = texture.width;
        Rect textureRect = sprite.rect;
        int startX = Mathf.Clamp(Mathf.RoundToInt(textureRect.xMin), 0, texWidth - 1);
        int startY = Mathf.Clamp(Mathf.RoundToInt(textureRect.yMin), 0, texture.height - 1);
        int endX = Mathf.Clamp(Mathf.RoundToInt(textureRect.xMax), startX + 1, texWidth);
        int endY = Mathf.Clamp(Mathf.RoundToInt(textureRect.yMax), startY + 1, texture.height);

        int minX = endX;
        int minY = endY;
        int maxX = startX - 1;
        int maxY = startY - 1;

        for (int y = startY; y < endY; y++)
        {
            int row = y * texWidth;
            for (int x = startX; x < endX; x++)
            {
                if (pixels[row + x].a <= 8)
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        Rect localRect;
        if (maxX < minX || maxY < minY)
        {
            float fullWidth = textureRect.width / sprite.pixelsPerUnit;
            float fullHeight = textureRect.height / sprite.pixelsPerUnit;
            localRect = new Rect(-sprite.pivot.x / sprite.pixelsPerUnit, -sprite.pivot.y / sprite.pixelsPerUnit, fullWidth, fullHeight);
        }
        else
        {
            float xMin = (minX - textureRect.xMin - sprite.pivot.x) / sprite.pixelsPerUnit;
            float yMin = (minY - textureRect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit;
            float xMax = (maxX + 1f - textureRect.xMin - sprite.pivot.x) / sprite.pixelsPerUnit;
            float yMax = (maxY + 1f - textureRect.yMin - sprite.pivot.y) / sprite.pixelsPerUnit;
            localRect = Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        opaqueLocalRectCache[cacheKey] = localRect;
        return localRect;
    }

    public static float GetOpaqueBottomLocalY(Sprite sprite)
    {
        return GetOpaqueLocalRect(sprite).yMin;
    }

    private static Sprite CreateAncientCoinSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 rim = new Color32(197, 132, 54, 255);
        Color32 inner = new Color32(159, 95, 39, 255);
        Color32 darkRim = new Color32(123, 68, 28, 255);

        int center = size / 2;
        float radius = 29f;
        float innerRadius = 22f;
        float holeHalf = 9f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center + 0.5f;
                float dy = y - center + 0.5f;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool inSquareHole = Mathf.Abs(dx) < holeHalf && Mathf.Abs(dy) < holeHalf;

                Color32 color = clear;
                if (dist < radius && !inSquareHole)
                {
                    color = dist > innerRadius ? darkRim : rim;
                    if (dist < innerRadius - 2.5f)
                    {
                        color = inner;
                    }
                }

                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateScrollSprite()
    {
        const int width = 80;
        const int height = 56;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 paper = new Color32(234, 216, 177, 255);
        Color32 edge = new Color32(182, 138, 84, 255);
        Color32 seal = new Color32(154, 39, 36, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 9; y < height - 9; y++)
        {
            for (int x = 9; x < width - 9; x++)
            {
                bool border = y < 12 || y > height - 13 || x < 12 || x > width - 13;
                texture.SetPixel(x, y, border ? edge : paper);
            }
        }

        for (int y = 20; y < height - 20; y++)
        {
            for (int x = width / 2 - 5; x <= width / 2 + 5; x++)
            {
                texture.SetPixel(x, y, edge);
            }
        }

        for (int y = 4; y < height - 4; y++)
        {
            for (int x = 2; x < 9; x++)
            {
                float dx = x - 5.5f;
                float dy = y - height * 0.5f;
                if (dx * dx + dy * dy <= 12f)
                {
                    texture.SetPixel(x, y, edge);
                }
            }
        }

        for (int y = 4; y < height - 4; y++)
        {
            for (int x = width - 9; x < width - 2; x++)
            {
                float dx = x - (width - 5.5f);
                float dy = y - height * 0.5f;
                if (dx * dx + dy * dy <= 12f)
                {
                    texture.SetPixel(x, y, edge);
                }
            }
        }

        for (int y = height / 2 - 3; y <= height / 2 + 3; y++)
        {
            for (int x = width / 2 + 9; x <= width / 2 + 15; x++)
            {
                float dx = x - (width / 2 + 12f);
                float dy = y - (height / 2f);
                if (dx * dx + dy * dy <= 8f)
                {
                    texture.SetPixel(x, y, seal);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateAncientRunnerSprite()
    {
        const int width = 64;
        const int height = 88;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 robe = new Color32(53, 75, 109, 255);
        Color32 dark = new Color32(33, 46, 68, 255);
        Color32 skin = new Color32(236, 205, 167, 255);
        Color32 belt = new Color32(143, 70, 43, 255);
        Color32 hat = new Color32(44, 33, 30, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        FillRect(texture, 24, 18, 16, 34, robe);
        FillRect(texture, 25, 46, 14, 5, belt);
        FillRect(texture, 22, 14, 6, 16, dark);
        FillRect(texture, 36, 14, 6, 16, dark);
        FillRect(texture, 23, 52, 18, 12, skin);
        FillRect(texture, 20, 63, 24, 7, hat);
        FillRect(texture, 17, 59, 5, 6, hat);
        FillRect(texture, 42, 59, 5, 6, hat);
        FillRect(texture, 24, 32, 3, 10, robe);
        FillRect(texture, 37, 32, 3, 10, robe);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.12f), 70f);
    }

    private static Sprite CreateGroundObstacleSprite()
    {
        const int width = 58;
        const int height = 80;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 body = new Color32(220, 220, 220, 255);
        Color32 darkEdge = new Color32(140, 140, 140, 255);
        Color32 lightEdge = new Color32(244, 244, 244, 255);
        Color32 inset = new Color32(188, 188, 188, 255);
        Color32 crack = new Color32(114, 114, 114, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        FillRect(texture, 8, 4, width - 16, 12, darkEdge);
        FillRect(texture, 12, 6, width - 24, 8, body);
        FillRect(texture, 14, 0, 10, 6, darkEdge);
        FillRect(texture, width - 24, 0, 10, 6, darkEdge);

        for (int y = 16; y < height - 18; y++)
        {
            int left = y > height - 28 ? 12 : (y < 28 ? 14 : 12);
            int right = width - left;
            for (int x = left; x < right; x++)
            {
                bool border = x <= left + 1 || x >= right - 2 || y <= 18 || y >= height - 20;
                texture.SetPixel(x, y, border ? darkEdge : body);
            }
        }

        FillRect(texture, 18, 26, width - 36, 30, darkEdge);
        FillRect(texture, 20, 28, width - 40, 26, inset);
        FillRect(texture, 22, 30, width - 44, 22, lightEdge);
        FillRect(texture, 12, height - 22, width - 24, 8, darkEdge);
        FillRect(texture, 16, height - 20, width - 32, 4, lightEdge);
        FillRect(texture, width / 2 - 3, 32, 6, 20, darkEdge);
        FillRect(texture, 18, 40, width - 36, 4, darkEdge);

        for (int x = 14; x < width - 14; x++)
        {
            texture.SetPixel(x, 15, lightEdge);
            if ((x + 3) % 11 < 4)
            {
                texture.SetPixel(x, height - 24, crack);
            }
        }

        for (int y = 18; y < height - 18; y += 10)
        {
            for (int x = 17; x < width - 17; x++)
            {
                if ((x * 3 + y * 2) % 17 == 0)
                {
                    texture.SetPixel(x, y, crack);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), height);
    }

    private static Sprite CreateOverheadObstacleSprite()
    {
        const int width = 116;
        const int height = 44;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 body = new Color32(222, 222, 222, 255);
        Color32 darkEdge = new Color32(138, 138, 138, 255);
        Color32 highlight = new Color32(244, 244, 244, 255);
        Color32 accent = new Color32(184, 184, 184, 255);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 12; y < height - 8; y++)
        {
            int taper = y > height - 14 ? 8 : (y < 18 ? 7 : 5);
            for (int x = taper; x < width - taper; x++)
            {
                bool border = x <= taper + 1 || x >= width - taper - 2 || y <= 13 || y >= height - 10;
                texture.SetPixel(x, y, border ? darkEdge : body);
            }
        }

        FillRect(texture, 12, height - 14, width - 24, 6, darkEdge);
        FillRect(texture, 18, height - 12, width - 36, 2, highlight);
        FillRect(texture, 22, 14, width - 44, 18, darkEdge);
        FillRect(texture, 24, 16, width - 48, 14, accent);
        FillRect(texture, 28, 19, width - 56, 8, highlight);
        FillRect(texture, 14, 10, 10, 16, darkEdge);
        FillRect(texture, width - 24, 10, 10, 16, darkEdge);
        FillRect(texture, 10, 14, 12, 8, accent);
        FillRect(texture, width - 22, 14, 12, 8, accent);
        FillRect(texture, 16, 8, width - 32, 4, accent);

        for (int x = 14; x < width - 14; x++)
        {
            texture.SetPixel(x, 11, highlight);
            if ((x + 1) % 17 <= 2)
            {
                texture.SetPixel(x, height - 16, darkEdge);
            }
        }

        for (int i = 24; i < width - 20; i += 24)
        {
            FillRect(texture, i, 4, 3, 6, darkEdge);
            FillRect(texture, i - 1, 9, 5, 2, accent);
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 64f);
    }

    private static Sprite CreateShadowSprite()
    {
        const int width = 128;
        const int height = 48;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 shadow = new Color32(255, 255, 255, 255);
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - halfW) / (halfW - 4f);
                float dy = (y + 0.5f - halfH) / (halfH - 6f);
                float dist = dx * dx + dy * dy;
                if (dist > 1f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float alpha = Mathf.Clamp01(1f - dist);
                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                texture.SetPixel(x, y, new Color32(shadow.r, shadow.g, shadow.b, a));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 96f);
    }

    private static Sprite CreateUiPaperSprite()
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int border = Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y));
                int edgeFade = Mathf.Clamp(18 - border, 0, 18);
                float wash = Mathf.Sin((x + 3) * 0.13f) * 2.2f + Mathf.Cos((y + 7) * 0.19f) * 1.7f;
                int red = Mathf.Clamp(244 - edgeFade * 3 + Mathf.RoundToInt(wash), 186, 248);
                int green = Mathf.Clamp(232 - edgeFade * 4 + Mathf.RoundToInt(wash * 0.9f), 172, 240);
                int blue = Mathf.Clamp(206 - edgeFade * 5 + Mathf.RoundToInt(wash * 0.7f), 148, 220);

                if (((x * 13 + y * 7) % 29) == 0)
                {
                    red = Mathf.Clamp(red - 10, 170, 248);
                    green = Mathf.Clamp(green - 12, 156, 236);
                    blue = Mathf.Clamp(blue - 10, 138, 214);
                }

                if ((x > 10 && x < size - 11 && y > 10 && y < size - 11) && ((x * 5 + y * 11) % 41) == 0)
                {
                    red = Mathf.Clamp(red - 6, 170, 248);
                    green = Mathf.Clamp(green - 6, 156, 236);
                    blue = Mathf.Clamp(blue - 4, 140, 220);
                }

                texture.SetPixel(x, y, new Color32((byte)red, (byte)green, (byte)blue, 255));
            }
        }

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect,
            new Vector4(18f, 18f, 18f, 18f));
    }

    private static Sprite CreateUiFrameSprite()
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 line = new Color32(255, 255, 255, 255);
        Color32 soft = new Color32(214, 214, 214, 255);
        Color32 dim = new Color32(166, 166, 166, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        DrawFrameLine(texture, 5, 5, size - 10, size - 10, line);
        DrawFrameLine(texture, 11, 11, size - 22, size - 22, soft);
        DrawFrameLine(texture, 17, 17, size - 34, size - 34, dim);

        FillRect(texture, 10, size - 20, 24, 5, line);
        FillRect(texture, size - 34, size - 20, 24, 5, line);
        FillRect(texture, 10, 15, 24, 5, line);
        FillRect(texture, size - 34, 15, 24, 5, line);
        FillRect(texture, 15, size - 34, 5, 24, line);
        FillRect(texture, size - 20, size - 34, 5, 24, line);
        FillRect(texture, 15, 10, 5, 24, line);
        FillRect(texture, size - 20, 10, 5, 24, line);

        FillRect(texture, size / 2 - 12, size - 15, 24, 4, line);
        FillRect(texture, size / 2 - 12, 11, 24, 4, line);
        FillRect(texture, 11, size / 2 - 2, 10, 4, soft);
        FillRect(texture, size - 21, size / 2 - 2, 10, 4, soft);

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect,
            new Vector4(20f, 20f, 20f, 20f));
    }

    private static Sprite CreateUiPlaqueSprite()
    {
        const int size = 96;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                texture.SetPixel(x, y, new Color32(0, 0, 0, 0));
            }
        }

        for (int y = 10; y < size - 10; y++)
        {
            for (int x = 8; x < size - 8; x++)
            {
                int border = Mathf.Min(Mathf.Min(x - 8, size - 9 - x), Mathf.Min(y - 10, size - 11 - y));
                int tone = Mathf.Clamp(230 - Mathf.Max(0, 12 - border) * 6 + Mathf.RoundToInt(Mathf.Sin((x + y) * 0.18f) * 4f), 120, 238);
                texture.SetPixel(x, y, new Color32((byte)tone, (byte)tone, (byte)tone, 255));
            }
        }

        DrawFrameLine(texture, 8, 10, size - 16, size - 20, new Color32(148, 148, 148, 255));
        DrawFrameLine(texture, 14, 16, size - 28, size - 32, new Color32(210, 210, 210, 255));
        FillRect(texture, 20, 22, size - 40, size - 44, new Color32(178, 178, 178, 255));
        FillRect(texture, 22, 24, size - 44, size - 48, new Color32(232, 232, 232, 255));
        FillRect(texture, 8, size - 18, size - 16, 4, new Color32(132, 132, 132, 255));
        FillRect(texture, 8, 14, size - 16, 4, new Color32(132, 132, 132, 255));
        FillRect(texture, 14, size / 2 - 2, 8, 4, new Color32(188, 188, 188, 255));
        FillRect(texture, size - 22, size / 2 - 2, 8, 4, new Color32(188, 188, 188, 255));
        FillRect(texture, 18, size - 24, 6, 6, new Color32(252, 252, 252, 255));
        FillRect(texture, size - 24, size - 24, 6, 6, new Color32(252, 252, 252, 255));
        FillRect(texture, 18, 18, 6, 6, new Color32(252, 252, 252, 255));
        FillRect(texture, size - 24, 18, 6, 6, new Color32(252, 252, 252, 255));

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f),
            size,
            0,
            SpriteMeshType.FullRect,
            new Vector4(18f, 18f, 18f, 18f));
    }

    private static Sprite CreateUiRodSprite()
    {
        const int width = 96;
        const int height = 24;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, clear);
            }
        }

        for (int y = 6; y < height - 6; y++)
        {
            FillRect(texture, 12, y, width - 24, 1, new Color32(220, 220, 220, 255));
        }

        FillRect(texture, 12, 7, width - 24, 2, new Color32(132, 132, 132, 255));
        FillRect(texture, 12, height - 9, width - 24, 2, new Color32(248, 248, 248, 255));
        FillRect(texture, 6, 4, 10, height - 8, new Color32(160, 160, 160, 255));
        FillRect(texture, width - 16, 4, 10, height - 8, new Color32(160, 160, 160, 255));
        FillRect(texture, 2, 8, 8, height - 16, new Color32(236, 236, 236, 255));
        FillRect(texture, width - 10, 8, 8, height - 16, new Color32(236, 236, 236, 255));

        texture.Apply();
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, width, height),
            new Vector2(0.5f, 0.5f),
            width,
            0,
            SpriteMeshType.FullRect,
            new Vector4(16f, 6f, 16f, 6f));
    }

    private static Sprite CreateUiSealSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 body = new Color32(255, 255, 255, 255);
        Color32 inner = new Color32(210, 210, 210, 255);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Abs(x - size / 2);
                int dy = Mathf.Abs(y - size / 2);
                bool inside = dx + dy < 41 && dx < 27 && dy < 27;
                if (!inside)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                bool mark = (dx > 17 || dy > 17 || (x + y) % 11 == 0);
                texture.SetPixel(x, y, mark ? body : inner);
            }
        }

        FillRect(texture, 22, 18, 4, 26, body);
        FillRect(texture, 30, 18, 4, 26, body);
        FillRect(texture, 18, 22, 18, 4, body);
        FillRect(texture, 18, 36, 18, 4, body);

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateUiBrushSprite()
    {
        const int width = 128;
        const int height = 32;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32 clear = new Color32(0, 0, 0, 0);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (float)x / (width - 1);
                float ny = Mathf.Abs((y - height * 0.5f) / (height * 0.5f));
                float alpha = Mathf.Clamp01(1f - ny * 1.5f);
                alpha *= Mathf.Clamp01(1f - Mathf.Abs(nx - 0.5f) * 1.5f);
                alpha *= 0.9f - ((x * 17 + y * 13) % 9) * 0.06f;
                byte a = (byte)Mathf.Clamp(Mathf.RoundToInt(alpha * 255f), 0, 255);
                texture.SetPixel(x, y, a <= 4 ? clear : new Color32(255, 255, 255, a));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), width);
    }

    private static Sprite CreateSunWashSprite()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        Color32 clear = new Color32(0, 0, 0, 0);
        float half = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f - half) / (half - 8f);
                float dy = (y + 0.5f - half) / (half - 8f);
                float dist = dx * dx + dy * dy;
                if (dist > 1f)
                {
                    texture.SetPixel(x, y, clear);
                    continue;
                }

                float alpha = Mathf.Pow(1f - dist, 1.45f);
                byte a = (byte)Mathf.RoundToInt(alpha * 255f);
                texture.SetPixel(x, y, new Color32(255, 255, 255, a));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite CreateCellSprite(Texture2D texture, int col, int topRow, int frameWidth, int frameHeight)
    {
        int x = col * frameWidth;
        int y = texture.height - (topRow + 1) * frameHeight;
        return Sprite.Create(
            texture,
            new Rect(x, y, frameWidth, frameHeight),
            new Vector2(0.5f, 0.12f),
            frameWidth
        );
    }

    private static Sprite[] LoadSpriteSequence(
        string directory,
        string prefix,
        int maxFrames,
        float? forcedPixelsPerUnit,
        out float resolvedPixelsPerUnit,
        bool preferLoopFrames = false)
    {
        resolvedPixelsPerUnit = 0f;
        if (!Directory.Exists(directory))
        {
            return Array.Empty<Sprite>();
        }

        string[] files = Directory.GetFiles(directory, prefix + "*.png");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        if (files.Length == 0)
        {
            return Array.Empty<Sprite>();
        }

        if (preferLoopFrames)
        {
            files = TrimSequenceEdges(files);
        }

        if (maxFrames > 0 && files.Length > maxFrames)
        {
            files = preferLoopFrames
                ? PickCentralWindow(files, maxFrames)
                : PickSequenceSamples(files, maxFrames);
        }

        var textures = new Texture2D[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            byte[] bytes = File.ReadAllBytes(files[i]);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(bytes, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            textures[i] = texture;
        }

        Rect sequenceRect = GetSequenceRect(textures);
        float pixelsPerUnit = forcedPixelsPerUnit ?? Mathf.Max(96f, sequenceRect.height / 1.68f);
        resolvedPixelsPerUnit = pixelsPerUnit;
        var sprites = new Sprite[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            sprites[i] = Sprite.Create(
                textures[i],
                sequenceRect,
                new Vector2(0.5f, 0.025f),
                pixelsPerUnit
            );
        }

        return sprites;
    }

    private static string[] TrimSequenceEdges(string[] files)
    {
        if (files == null || files.Length < 8)
        {
            return files;
        }

        int trimEachSide = Mathf.Clamp(files.Length / 9, 1, 2);
        int keep = files.Length - trimEachSide * 2;
        if (keep < 6)
        {
            return files;
        }

        var trimmed = new string[keep];
        Array.Copy(files, trimEachSide, trimmed, 0, keep);
        return trimmed;
    }

    private static string[] PickSequenceSamples(string[] files, int sampleCount)
    {
        if (files.Length <= sampleCount)
        {
            return files;
        }

        var picked = new string[sampleCount];
        float step = (float)(files.Length - 1) / Mathf.Max(1, sampleCount - 1);
        for (int i = 0; i < sampleCount; i++)
        {
            int index = Mathf.Clamp(Mathf.RoundToInt(step * i), 0, files.Length - 1);
            picked[i] = files[index];
        }

        return picked;
    }

    private static string[] PickCentralWindow(string[] files, int sampleCount)
    {
        if (files.Length <= sampleCount)
        {
            return files;
        }

        int start = Mathf.Max(0, (files.Length - sampleCount) / 2);
        var picked = new string[sampleCount];
        Array.Copy(files, start, picked, 0, sampleCount);
        return picked;
    }

    private static Rect GetSequenceRect(Texture2D[] textures)
    {
        if (textures == null || textures.Length == 0)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        int[] sampleIndices =
        {
            0,
            textures.Length / 2,
            textures.Length - 1
        };

        Rect union = new Rect(0f, 0f, 0f, 0f);
        bool hasUnion = false;
        for (int i = 0; i < sampleIndices.Length; i++)
        {
            int idx = Mathf.Clamp(sampleIndices[i], 0, textures.Length - 1);
            Rect rect = GetOpaqueRect(textures[idx]);
            if (!hasUnion)
            {
                union = rect;
                hasUnion = true;
            }
            else
            {
                union = UnionRect(union, rect);
            }
        }

        float pad = 6f;
        float x = Mathf.Max(0f, union.xMin - pad);
        float y = Mathf.Max(0f, union.yMin - pad);
        float maxWidth = textures[0].width - x;
        float maxHeight = textures[0].height - y;
        float width = Mathf.Min(maxWidth, union.width + pad * 2f);
        float height = Mathf.Min(maxHeight, union.height + pad * 2f);
        return new Rect(x, y, Mathf.Max(1f, width), Mathf.Max(1f, height));
    }

    private static Rect UnionRect(Rect a, Rect b)
    {
        float xMin = Mathf.Min(a.xMin, b.xMin);
        float yMin = Mathf.Min(a.yMin, b.yMin);
        float xMax = Mathf.Max(a.xMax, b.xMax);
        float yMax = Mathf.Max(a.yMax, b.yMax);
        return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
    }

    private static Rect GetOpaqueRect(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int width = texture.width;
        int height = texture.height;
        Rect dominantRect = FindDominantOpaqueRect(pixels, width, height);
        if (dominantRect.width <= 0f || dominantRect.height <= 0f)
        {
            return new Rect(0f, 0f, width, height);
        }

        float pad = 2f;
        float rectX = Mathf.Max(0f, dominantRect.xMin - pad);
        float rectY = Mathf.Max(0f, dominantRect.yMin - pad);
        float rectW = Mathf.Min(width - rectX, dominantRect.width + pad * 2f);
        float rectH = Mathf.Min(height - rectY, dominantRect.height + pad * 2f);
        return new Rect(rectX, rectY, rectW, rectH);
    }

    private static Rect FindDominantOpaqueRect(Color32[] pixels, int width, int height)
    {
        var visited = new bool[width * height];
        int bestCount = 0;
        int bestMinX = width;
        int bestMinY = height;
        int bestMaxX = -1;
        int bestMaxY = -1;

        var queue = new int[width * height];
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                int start = row + x;
                if (visited[start] || pixels[start].a <= 8)
                {
                    continue;
                }

                int head = 0;
                int tail = 0;
                queue[tail++] = start;
                visited[start] = true;

                int count = 0;
                int minX = x;
                int minY = y;
                int maxX = x;
                int maxY = y;

                while (head < tail)
                {
                    int index = queue[head++];
                    int px = index % width;
                    int py = index / width;
                    count++;

                    if (px < minX) minX = px;
                    if (py < minY) minY = py;
                    if (px > maxX) maxX = px;
                    if (py > maxY) maxY = py;

                    EnqueueOpaqueNeighbor(px - 1, py, width, height, pixels, visited, queue, ref tail);
                    EnqueueOpaqueNeighbor(px + 1, py, width, height, pixels, visited, queue, ref tail);
                    EnqueueOpaqueNeighbor(px, py - 1, width, height, pixels, visited, queue, ref tail);
                    EnqueueOpaqueNeighbor(px, py + 1, width, height, pixels, visited, queue, ref tail);
                }

                if (count > bestCount)
                {
                    bestCount = count;
                    bestMinX = minX;
                    bestMinY = minY;
                    bestMaxX = maxX;
                    bestMaxY = maxY;
                }
            }
        }

        if (bestCount <= 0)
        {
            return new Rect(0f, 0f, 0f, 0f);
        }

        return Rect.MinMaxRect(bestMinX, bestMinY, bestMaxX + 1, bestMaxY + 1);
    }

    private static void EnqueueOpaqueNeighbor(
        int x,
        int y,
        int width,
        int height,
        Color32[] pixels,
        bool[] visited,
        int[] queue,
        ref int tail)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return;
        }

        int index = y * width + x;
        if (visited[index] || pixels[index].a <= 8)
        {
            return;
        }

        visited[index] = true;
        queue[tail++] = index;
    }

    private static void FillRect(Texture2D texture, int x, int y, int w, int h, Color32 color)
    {
        for (int iy = y; iy < y + h; iy++)
        {
            for (int ix = x; ix < x + w; ix++)
            {
                if (ix >= 0 && ix < texture.width && iy >= 0 && iy < texture.height)
                {
                    texture.SetPixel(ix, iy, color);
                }
            }
        }
    }

    private static void DrawFrameLine(Texture2D texture, int x, int y, int w, int h, Color32 color)
    {
        FillRect(texture, x, y, w, 2, color);
        FillRect(texture, x, y + h - 2, w, 2, color);
        FillRect(texture, x, y, 2, h, color);
        FillRect(texture, x + w - 2, y, 2, h, color);
    }
}
