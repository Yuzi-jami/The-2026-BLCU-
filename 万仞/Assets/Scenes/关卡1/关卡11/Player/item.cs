using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using static Allcontrol;
public class item : MonoBehaviour
{
        private int cherries =Allcontrol.GameManager.Instance.score;
        [SerializeField] private Text  cherryCountText;
        [SerializeField] private AudioSource Collect;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.tag == "Cherry" || collision.tag == "GoodWood")
            {
                // Collect.Play();
                Destroy(collision.gameObject);
                cherries++;
                cherryCountText.text = "COUNT\n\nSTONE:" + cherries;
            
                Allcontrol.GameManager.Instance.score += cherries;
            }
        
        }

        


}
