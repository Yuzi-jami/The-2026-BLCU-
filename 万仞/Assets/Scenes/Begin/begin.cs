using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class begin : MonoBehaviour
{
    private bool isGameStarted = false; // 标记游戏是否已开始，防止重复触发
   void Start()
{
  
    // 锁定光标（可选，根据游戏需求）
    Cursor.visible = true;
    Cursor.lockState = CursorLockMode.None;
}

void Update()
{
  
    if (isGameStarted) return;


    if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
    {
        StartGame();
    }
}
/// <summary>
/// 触发游戏开始的核心逻辑
/// </summary>
private void StartGame()
{
    isGameStarted = true;
    
    // 可选：添加过渡效果（比如延迟0.5秒再切换场景，让交互更自然）
    Invoke("LoadGameScene", 0.5f);

    // 如果你不需要切换场景，而是激活游戏逻辑，替换成下面的代码：
    // GameManager.Instance.StartGameLogic(); // 假设你有游戏管理类
    // 或
    // GetComponent<YourGameLogic>().enabled = true;
}

/// <summary>
/// 加载游戏主场景
/// </summary>
private void LoadGameScene()
{
    // 检查场景是否存在，避免报错
    if (!string.IsNullOrEmpty("漫画"))
    {
        SceneManager.LoadScene("漫画");
    }
    else
    {
        Debug.LogWarning("请在Inspector面板设置正确的游戏主场景名称！");
    }

    // // 游戏开始后锁定光标（可选，比如3D游戏）
    // Cursor.visible = false;
    // Cursor.lockState = CursorLockMode.Locked;
}
}
