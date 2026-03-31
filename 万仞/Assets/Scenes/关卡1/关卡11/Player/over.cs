using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class over : MonoBehaviour
{
    [SerializeField] private AudioSource finishSound;

    private bool LevelComplete = false;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player" && !LevelComplete)
        {
            finishSound.Play();
            LevelComplete = true;
            Debug.Log("Level Complete");
            Invoke("CompleteLevel", 2f);
        }
    }

    void CompleteLevel()
    {
        SceneManager.LoadScene("整体关卡");
    }

    void Update()
    {

    }
}