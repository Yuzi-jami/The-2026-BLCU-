using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ⚠️ 注意：控制 UI 必须加上这一句！

public class GameManager : MonoBehaviour
{
    public static GameManager instance; 

    [Header("UI 引用")]
    public Slider progressBar; // 这里用来存放我们刚才做的进度条

    [Header("关卡设置")]
    public int scoreToWin = 10; // 接满 10 个正确木头就通关
    private int currentScore = 0;

    void Awake()
    {
        instance = this; // 游戏一开始，自己认领“总管”的身份
    }

    void Start()
    {
        // 初始化进度条
        progressBar.maxValue = scoreToWin;
        progressBar.value = 0;
    }

    // 加分函数：给别的脚本调用的
    public void AddScore()
    {
        currentScore++;
        progressBar.value = currentScore; // 更新界面

        if(currentScore >= scoreToWin)
        {
            // Debug.Log("✨太棒了！水墨断桥修复完成！✨");
            // 以后这里可以写：弹出过关画面、加载下一关等逻辑
            SceneManager.LoadScene("对话2");
        }
    }

    // 扣分函数：接错木头时的惩罚
    public void WrongWood()
    {
        currentScore--;
        if(currentScore < 0) currentScore = 0; // 分数不能扣成负数
        progressBar.value = currentScore; 
    }
}