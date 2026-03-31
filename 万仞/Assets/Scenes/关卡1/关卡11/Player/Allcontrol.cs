using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Allcontrol : MonoBehaviour
{
    // Start is called before the first frame update
    public class GameManager
    {
        private static GameManager _instance;

        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GameManager();
                }
                return _instance;
            }
        }

        public int score;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}