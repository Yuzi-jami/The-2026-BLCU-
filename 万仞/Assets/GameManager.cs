// using UnityEngine;
// using UnityEngine.SceneManagement;
// using UnityEngine.UI; // ⚠️ 注意：控制 UI 必须加上这一句！
//
// public class GameManager : MonoBehaviour
// {
//     public static GameManager instance; 
//
//     [Header("UI 引用")]
//     public Slider progressBar; // 这里用来存放我们刚才做的进度条
//     
//     [Header("关卡设置")]
//     public int scoreToWin = 10; // 接满 10 个正确木头就通关
//     private int currentScore = 0;
//
//     void Awake()
//     {
//         instance = this; // 游戏一开始，自己认领“总管”的身份
//     }
//
//     void Start()
//     {
//         // 初始化进度条
//         progressBar.maxValue = scoreToWin;
//         progressBar.value = 0;
//     
//     }
//
//     // 加分函数：给别的脚本调用的
//     public void AddScore()
//     {
//         currentScore++;
//         progressBar.value = currentScore; // 更新界面
//
//         if(currentScore >= scoreToWin)
//         {
//             // Debug.Log("✨太棒了！水墨断桥修复完成！✨");
//             // 以后这里可以写：弹出过关画面、加载下一关等逻辑
//             SceneManager.LoadScene("对话2");
//         }
//     }
//     
//     // 扣分函数：接错木头时的惩罚
//     public void WrongWood()
//     {
//         currentScore--;
//         if(currentScore < 0) currentScore = 0; // 分数不能扣成负数
//         progressBar.value = currentScore; 
//     }
// }
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public GameObject aud;
    [Header("UI 引用")]
    public Slider progressBar; 
    
    // 💡 新增：用 CanvasGroup 来统一控制 Panel 的透明度
    [Tooltip("请在Unity中把带有CanvasGroup组件的Panel拖到这里")]
    public CanvasGroup panel1CanvasGroup; 

    [Header("关卡设置")]
    public int scoreToWin = 10; 
    private int currentScore = 0;

    // 💡 新增：用来记录当前处于什么阶段
    // 0 = 正在显示提示界面，等待第一次点击
    // 1 = 提示已隐藏，等待第二次点击开始游戏
    // 2 = 游戏正在进行中
    private int gameStateStep = 0; 

    void Awake()
    {
        instance = this; 
    }

    void Start()
    {
        // 初始化进度条
        if(progressBar != null)
        {
            progressBar.maxValue = scoreToWin;
            progressBar.value = 0;
        }
        
        // 💡 建议：游戏刚开始时，时间暂停，防止木头提前掉落
        Time.timeScale = 0f; 
    }

    // 💡 新增：每帧检测玩家输入
    void Update()
    {
        // 检测鼠标左键点击（在手机上等同于点击屏幕）
        if (Input.GetMouseButtonDown(0))
        {
            if (gameStateStep == 0)
            {
                // ✨ 第一次点击：隐藏 Panel1
                HidePromptPanel();
                gameStateStep = 1; // 状态推进到 1
                
            }
            else if (gameStateStep == 1)
            {
                // ✨ 第二次点击：正式开始游戏
                StartRealGame();
                gameStateStep = 2; // 状态推进到 2，之后再点击就不会触发这些逻辑了
            }
        }
    }

    // 隐藏提示面板的逻辑
    private void HidePromptPanel()
    {
        if (panel1CanvasGroup != null)
        {
            panel1CanvasGroup.alpha = 0f; // 透明度设为0（完全透明）
            panel1CanvasGroup.interactable = false; // 让这个面板不再响应点击
            panel1CanvasGroup.blocksRaycasts = false; // 让鼠标点击可以穿透这个面板，点到后面的东西
        }
        else
        {
            Debug.LogWarning("Panel1 Canvas Group 未赋值！请在 Inspector 中拖拽！");
        }
    }

    // 正式开始游戏的逻辑
    private void StartRealGame()
    {
        Debug.Log("✨ 游戏正式开始！ ✨");
        // 💡 恢复游戏时间，木头开始掉落 / 玩家可以移动
        Time.timeScale = 1f; 
        aud.SetActive(true);
    }

    // 加分函数
    public void AddScore()
    {
        // 如果游戏还没开始（状态不为2），不允许加分
        if (gameStateStep != 2) return;

        currentScore++;
        if(progressBar != null) progressBar.value = currentScore; 

        if(currentScore >= scoreToWin)
        {
            SceneManager.LoadScene("对话3");
        }
    }
    
    // 扣分函数
    public void WrongWood()
    {
        // 如果游戏还没开始，不允许扣分
        if (gameStateStep != 2) return;

        currentScore--;
        if(currentScore < 0) currentScore = 0; 
        if(progressBar != null) progressBar.value = currentScore; 
    }
}