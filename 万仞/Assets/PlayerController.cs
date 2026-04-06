using Unity.VisualScripting;
using UnityEngine;
using System.Collections;
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("法阵移动的速度")]
    public float moveSpeed = 12f; 
    public Transform characterGraphics;
  
    // ==========================================
    [Header("音效设置")]
    [Tooltip("收到好木头时的音效")]
    public AudioClip goodWoodSound; 
    
    [Tooltip("碰到朽木时的音效")]
    public AudioClip badWoodSound;  
    
    [Header("加速技能设置")]
    [Tooltip("加速持续时间（秒）")]
    public float boostDuration = 3f; 
    [Tooltip("加速倍率（例如1.5就是1.5倍速）")]
    public float speedMultiplier = 2f; 
    [Tooltip("把右下角的粒子效果拖到这里")]
    public GameObject boostParticleObject;

    private bool isBoostReady = true; // 技能是否冷却完毕
    private float currentSpeed;       // 玩家当前帧的实际速度
    void Start()
    {
        currentSpeed = moveSpeed; // 初始速度设为基础速度
        if (boostParticleObject != null)
        {
            boostParticleObject.SetActive(true); // 确保一开始粒子是显示的
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) && isBoostReady)
        {
            StartCoroutine(BoostRoutine()); // 开启加速协程
        }
        float horizontalInput = Input.GetAxisRaw("Horizontal");

        // 修改：只翻转子物体的图像
        // ==========================================
        if (horizontalInput != 0 && characterGraphics != null) 
        {
            Vector3 graphicsScale = characterGraphics.localScale;
            
            if (horizontalInput < 0)
            {
                graphicsScale.x = -Mathf.Abs(graphicsScale.x); // 向左
            }
            else if (horizontalInput > 0)
            {
                graphicsScale.x = Mathf.Abs(graphicsScale.x);  // 向右
            }
            
            characterGraphics.localScale = graphicsScale;
        }
        // ==========================================
        
        // ✨ 修改：把原先的 moveSpeed 替换成了 currentSpeed
        Vector3 movement = new Vector3(horizontalInput, 0f, 0f) * currentSpeed * Time.deltaTime;
        transform.Translate(movement);

        // 3. 限制边界
        Vector3 clampedPosition = transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, -12, 12);
        transform.position = clampedPosition;
    }

    private System.Collections.IEnumerator BoostRoutine()
    {
        // 1. 进入冷却
        isBoostReady = false;

        // 2. 隐藏粒子效果（表示技能消耗了）
        if (boostParticleObject != null)
        {
            boostParticleObject.SetActive(false); 
        }

        // 3. 提升速度
        currentSpeed = moveSpeed * speedMultiplier;

        // 4. 等待指定的秒数（在此期间Update里的移动逻辑会一直使用加速后的currentSpeed）
        yield return new WaitForSeconds(boostDuration);

        // 5. 时间到，速度恢复正常
        currentSpeed = moveSpeed;

        // 6. 重新显示粒子（提示玩家技能转好了）
        if (boostParticleObject != null)
        {
            boostParticleObject.SetActive(true);
        }

        // 7. 技能重新就绪
        isBoostReady = true;
    }
    
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // 如果碰到的东西，标签是 "GoodWood"
        if (other.CompareTag("GoodWood"))
        {
            GameManager.instance.AddScore(); 
            AudioSource.PlayClipAtPoint(goodWoodSound, transform.position);
            Destroy(other.gameObject);    
            
        }
        // 如果碰到的东西，标签是 "BadWood" (朽木)
        else if (other.CompareTag("BadWood"))
        {
            GameManager.instance.WrongWood();
            AudioSource.PlayClipAtPoint(badWoodSound, transform.position);
            Destroy(other.gameObject);      
        }
    }
}