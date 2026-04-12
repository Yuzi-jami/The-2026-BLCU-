using UnityEngine;
using UnityEngine.SceneManagement;

public class QuizThreeSteps : MonoBehaviour
{
    public CanvasGroup panel1;
    public CanvasGroup panel2;
    public CanvasGroup panel3;

    public string finalScene;

    private int _current = 1;
    private bool canClickMouse = true; 
    void Start()
    {
        panel1.alpha = 0;
        panel2.alpha = 0;
        panel3.alpha = 0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && canClickMouse )
        {
            Show(1);
            canClickMouse = false;
        }
    }

    public void CheckAnswer()
    {
        _current++;

        if (_current == 2)
            Show(2);
        else if (_current == 3)
            Show(3);
        else
            SceneManager.LoadScene(finalScene);
    }

    void Show(int num)
    {
        // 全部关掉
        panel1.alpha = 0;
        panel1.interactable = false;
        panel1.blocksRaycasts = false;

        panel2.alpha = 0;
        panel2.interactable = false;
        panel2.blocksRaycasts = false;

        panel3.alpha = 0;
        panel3.interactable = false;
        panel3.blocksRaycasts = false;

        // 打开对应面板
        if (num == 1)
        {
            panel1.alpha = 1;
            panel1.interactable = true;
            panel1.blocksRaycasts = true;
        }
        if (num == 2)
        {
            panel2.alpha = 1;
            panel2.interactable = true;
            panel2.blocksRaycasts = true;
        }
        if (num == 3)
        {
            panel3.alpha = 1;
            panel3.interactable = true;
            panel3.blocksRaycasts = true;
        }
    }
}