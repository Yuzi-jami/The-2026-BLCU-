using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RunnerGameManager : MonoBehaviour
{
    private RunnerController player;
    private Text scoreText;
    private Text statusText;
    private CanvasGroup statusGroup;

    private int coinCount;
    private int scrollCount;
    private int score;
    private int bestScore;
    private float startTime;
    private float countdownStartTime;
    private bool gameOver;
    private bool paused;
    private float countdownDuration = 3f;
    private float startBannerDuration = 0.75f;
    private const string BestScorePrefKey = "runner_best_score";

    public bool IsGameOver => gameOver;
    public bool IsPaused => paused;
    public bool IsGameplayActive => !gameOver && !paused && CountdownRemaining <= 0f;
    public float CountdownRemaining => Mathf.Max(0f, countdownDuration - (Time.time - countdownStartTime));

    private void Start()
    {
        bestScore = PlayerPrefs.GetInt(BestScorePrefKey, 0);
        countdownStartTime = Time.time;
        startTime = countdownStartTime + countdownDuration;
        RenderUi();
    }

    public void RegisterPlayer(RunnerController runner)
    {
        player = runner;
        countdownStartTime = Time.time;
        startTime = countdownStartTime + countdownDuration;
        RenderUi();
    }

    public void BindUi(Text score, Text status, CanvasGroup statusPanelGroup = null)
    {
        scoreText = score;
        statusText = status;
        statusGroup = statusPanelGroup;
        RenderUi();
    }

    public void CollectCoin()
    {
        if (gameOver)
        {
            return;
        }

        coinCount++;
        score += 1;
        UpdateBestScoreIfNeeded();
        RenderUi();
    }

    public void CollectScroll()
    {
        if (gameOver)
        {
            return;
        }

        scrollCount++;
        score += 2;
        UpdateBestScoreIfNeeded();
        RenderUi();
    }

    public void TriggerGameOver()
    {
        if (gameOver)
        {
            return;
        }

        if (paused)
        {
            Time.timeScale = 1f;
            paused = false;
        }

        gameOver = true;
        RenderUi();
    }

    private void Update()
    {
        if (!gameOver && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)))
        {
            paused = !paused;
            Time.timeScale = paused ? 0f : 1f;
        }

        if (gameOver && Input.GetKeyDown(KeyCode.R))
        {
            Time.timeScale = 1f;
            var activeScene = SceneManager.GetActiveScene();
            if (!string.IsNullOrEmpty(activeScene.name))
            {
                SceneManager.LoadScene(activeScene.name);
            }
            else if (activeScene.buildIndex >= 0)
            {
                SceneManager.LoadScene(activeScene.buildIndex);
            }

            return;
        }

        RenderUi();
    }

    private void RenderUi()
    {
        if (scoreText != null)
        {
            float distance = 0f;
            if (player != null && IsGameplayActive)
            {
                distance = Mathf.Max(0f, player.transform.position.x + 6f);
            }

            scoreText.text = "<size=30>\u91cc\u7a0b <b>" + distance.ToString("0") + "</b>    " +
                             "\u94dc\u94b1 <b>" + coinCount + "</b>    " +
                             "\u5377\u8f74 <b>" + scrollCount + "</b>    " +
                             "\u5206\u6570 <b>" + score + "</b></size>\n" +
                             "<size=21>\u6700\u4f73 " + bestScore + "</size>";
        }

        if (statusText != null)
        {
            if (gameOver)
            {
                statusText.text = "<size=44>\u884c\u9014\u6682\u6b62</size>\n" +
                                  "<size=28>\u6b64\u756a\u5f97\u5206 " + score + "    \u6700\u4f73 " + bestScore + "</size>\n" +
                                  "<size=24>\u6309 R \u518d\u542f\u884c\u7a0b</size>";
                SetStatusAlpha(1f);
            }
            else if (paused)
            {
                statusText.text = "<size=42>\u4e14\u6b47\u7247\u523b</size>\n<size=24>\u6309 Esc / P \u7ee7\u7eed\u524d\u884c</size>";
                SetStatusAlpha(1f);
            }
            else
            {
                float countdownRemaining = CountdownRemaining;
                if (countdownRemaining > 0f)
                {
                    int seconds = Mathf.CeilToInt(countdownRemaining);
                    statusText.text = "<size=24>\u7a7a\u683c / W / \u4e0a\u952e \u8e0f\u5730\u800c\u8d77\uff08\u53ef\u7eed\u4e00\u8dc3\uff09\n" +
                                      "Shift \u4f0f\u8eab\u6ed1\u884c / \u7a7a\u4e2d\u75be\u5760</size>\n" +
                                      "<size=68><b>" + seconds + "</b></size>";
                    SetStatusAlpha(countdownRemaining / countdownDuration);
                }
                else
                {
                    float postStart = Time.time - startTime;
                    if (postStart < startBannerDuration)
                    {
                        statusText.text = "<size=58>\u542f\u884c</size>";
                        SetStatusAlpha(1f - postStart / startBannerDuration);
                    }
                    else
                    {
                        statusText.text = string.Empty;
                        SetStatusAlpha(0f);
                    }
                }
            }
        }
    }

    private void UpdateBestScoreIfNeeded()
    {
        if (score <= bestScore)
        {
            return;
        }

        bestScore = score;
        PlayerPrefs.SetInt(BestScorePrefKey, bestScore);
        PlayerPrefs.Save();
    }

    private void SetStatusAlpha(float alpha)
    {
        if (statusGroup != null)
        {
            statusGroup.alpha = alpha;
        }
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f)
        {
            Time.timeScale = 1f;
        }
    }
}
