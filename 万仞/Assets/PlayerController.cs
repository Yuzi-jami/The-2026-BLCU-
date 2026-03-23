using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("法阵移动的速度")]
    public float moveSpeed = 8f; 

    [Tooltip("屏幕左右边界的X坐标限制")]

    void Update()
    {

        float horizontalInput = Input.GetAxisRaw("Horizontal");

        Vector3 movement = new Vector3(horizontalInput, 0f, 0f) * moveSpeed * Time.deltaTime;

        transform.Translate(movement);

        Vector3 clampedPosition = transform.position;

        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -12, 12);

        transform.position = clampedPosition;
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果碰到的东西，标签是 "GoodWood"
        if (other.CompareTag("GoodWood"))
        {
            GameManager.instance.AddScore(); 
            Destroy(other.gameObject);     
        }
        // 如果碰到的东西，标签是 "BadWood" (朽木)
        else if (other.CompareTag("BadWood"))
        {
            GameManager.instance.WrongWood();
            Destroy(other.gameObject);      
        }
    }
}