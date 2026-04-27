using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class scenetojieshu : MonoBehaviour
{
    private int s = 1;
    public string nextScene;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            s++;
        }

     
        if (s > 1)
        {
        SceneManager.LoadScene(nextScene);
        }
    }
}
