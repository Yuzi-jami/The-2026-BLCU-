using UnityEngine;
using UnityEngine.SceneManagement;

// 挂载在 Canvas 下的答题面板节点上
public class QuizShow : MonoBehaviour
{
    public CanvasGroup cg;
    public string nextScene;

    void Start()
    {
        // 初始隐藏
        cg.alpha = 0;
        cg.interactable = false;
    }

    void Update()
    {
        // 点击鼠标左键显示
        if (Input.GetMouseButtonDown(0))
        {
            cg.alpha = 1;
            cg.interactable = true;
        }
    }

    // 按钮点击事件（把每个按钮的 OnClick 都绑定到这个方法）
    public void OnButtonClick()
    {
        SceneManager.LoadScene(nextScene);
    }
}