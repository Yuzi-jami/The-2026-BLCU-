using UnityEngine;

public class RunnerVisualSettings : MonoBehaviour
{
    [Header("Camera")]
    public Color cameraBackgroundColor = new Color(0.74f, 0.81f, 0.84f, 1f);

    [Header("Background")]
    public Color skyBandColor = new Color(0.88f, 0.77f, 0.61f, 0.9f);
    public Color skyTopColor = new Color(0.88f, 0.91f, 0.88f, 1f);
    public Color skyBottomColor = new Color(0.63f, 0.74f, 0.79f, 1f);
    public Color mistBandColor = new Color(0.92f, 0.88f, 0.8f, 0.36f);
    public Color cloudColor = new Color(0.96f, 0.95f, 0.92f, 0.92f);
    public Color mountainFarColor = new Color(0.42f, 0.5f, 0.54f, 1f);
    public Color mountainNearColor = new Color(0.31f, 0.39f, 0.44f, 1f);
    public Color hillColor = new Color(0.42f, 0.52f, 0.45f, 1f);
    public Color buildingColor = new Color(0.27f, 0.31f, 0.37f, 1f);
    public Color roofAccentColor = new Color(0.24f, 0.13f, 0.1f, 1f);
    public int hillCountMin = 2;
    public int hillCountMax = 3;
    public int buildingCountMin = 2;
    public int buildingCountMax = 4;
    public int cloudCountMin = 2;
    public int cloudCountMax = 4;

    [Header("World")]
    public Color groundColor = new Color(0.18f, 0.18f, 0.2f, 1f);
    public Color stripePrimaryColor = new Color(0.82f, 0.74f, 0.51f, 1f);
    public Color stripeSecondaryColor = new Color(0.78f, 0.76f, 0.71f, 1f);
    public Color obstacleColor = new Color(0.55f, 0.24f, 0.18f, 1f);
    public Color coinColor = new Color(0.98f, 0.83f, 0.2f, 1f);
    public Color scrollColor = Color.white;
    public Color playerColor = new Color(0.15f, 0.23f, 0.36f, 1f);

    [Header("UI Theme (Guofeng)")]
    public Color uiScreenVignetteColor = new Color(0.11f, 0.09f, 0.07f, 0.24f);
    public Color uiTopPanelColor = new Color(0.93f, 0.87f, 0.73f, 0.96f);
    public Color uiTopPanelBorderColor = new Color(0.36f, 0.18f, 0.12f, 0.98f);
    public Color uiStatusPanelColor = new Color(0.92f, 0.87f, 0.76f, 0.95f);
    public Color uiStatusBorderColor = new Color(0.4f, 0.19f, 0.13f, 0.98f);
    public Color uiPrimaryTextColor = new Color(0.2f, 0.11f, 0.08f, 1f);
    public Color uiTitleTextColor = new Color(0.37f, 0.11f, 0.08f, 1f);
    public Color uiStatusTextColor = new Color(0.23f, 0.13f, 0.09f, 1f);

    [Header("UI Sprite Overrides (Optional)")]
    public Sprite uiScreenVignetteSprite;
    public Sprite uiTopPanelSprite;
    public Sprite uiTopPanelBorderSprite;
    public Sprite uiStatusPanelSprite;
    public Sprite uiStatusBorderSprite;

    private void OnValidate()
    {
        hillCountMin = Mathf.Max(0, hillCountMin);
        hillCountMax = Mathf.Max(hillCountMin, hillCountMax);
        buildingCountMin = Mathf.Max(0, buildingCountMin);
        buildingCountMax = Mathf.Max(buildingCountMin, buildingCountMax);
        cloudCountMin = Mathf.Max(0, cloudCountMin);
        cloudCountMax = Mathf.Max(cloudCountMin, cloudCountMax);
    }
}
