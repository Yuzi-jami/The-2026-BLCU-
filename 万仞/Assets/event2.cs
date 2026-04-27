using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  UnityEngine.SceneManagement;
public class event2 : MonoBehaviour
{
    public CanvasGroup panel1;
    public CanvasGroup panel2;
    public string ss;
    private int s = 1;
    // Start is called before the first frame update
    void Start()
    {
        panel1.alpha = 1;
        panel2.alpha = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            s++;
        }

        if (s >= 4)
        {
            panel1.alpha = 0;
            panel2.alpha = 1;
        }

        if (s > 7)
        {
            SceneManager.LoadScene(ss);
        }
    }
}
