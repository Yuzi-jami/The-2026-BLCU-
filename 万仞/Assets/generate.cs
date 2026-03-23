using System.Runtime.CompilerServices;
using UnityEngine;

public class RandomItemDrop : MonoBehaviour
{
    [Header("掉落物品设置")]
    [Tooltip("存放所有可能掉落的物品预制体")]
    public GameObject[] dropItems; // 物品预制体数组
    
    [Header("生成区域设置")]
    [Tooltip("生成位置的X轴最小值")]
    public float spawnXMin = -4f;
    [Tooltip("生成位置的X轴最大值")]
    public float spawnXMax = 4f;
    [Tooltip("生成位置的Y轴高度")]
    public float spawnY = 6f;
    
    [Header("掉落参数")]
    [Tooltip("生成物品的时间间隔")]
    public float spawnInterval = 1f;
    [Tooltip("物品下落的初始速度")]
    public float fallSpeed = 2f;
    [Tooltip("物品旋转速度")]
    public float rotateSpeed = 90f;

    private float _spawnTimer; // 生成计时器

    void Update()
    {
        // 计时器累加
        _spawnTimer += Time.deltaTime;
        
        // 达到时间间隔就生成物品
        if (_spawnTimer >= spawnInterval)
        {
            SpawnRandomItem();
            _spawnTimer = 0; // 重置计时器
        }
    }

    /// <summary>
    /// 生成随机物品的核心方法
    /// </summary>
    void SpawnRandomItem()
    {
        // 安全检查：如果数组为空或没有元素，直接返回
        if (dropItems == null || dropItems.Length == 0)
        {
            Debug.LogWarning("掉落物品数组为空，请添加预制体！");
            return;
        }

        // 1. 随机选择生成位置（X轴随机，Y轴固定高度，Z轴0）
        float randomX = Random.Range(spawnXMin, spawnXMax);
        Vector3 spawnPosition = new Vector3(randomX, spawnY, 0f);

        // 2. 随机选择数组中的一个物品
        int randomIndex = Random.Range(0, dropItems.Length);
        GameObject selectedItem = dropItems[randomIndex];

        // 3. 实例化物品
        GameObject spawnedItem = Instantiate(selectedItem, spawnPosition, Quaternion.identity);
        
        // 4. 添加物理效果（确保物品有Rigidbody2D组件）
        Rigidbody2D rb = spawnedItem.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            // 如果没有刚体组件，自动添加
            rb = spawnedItem.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f; // 设置重力
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
        
        // 设置初始下落速度
        rb.velocity = new Vector2(0, -fallSpeed);

 
    }




}

internal interface IEnumerator
{
}