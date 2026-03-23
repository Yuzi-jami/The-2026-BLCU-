using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("掉落物设置")]
    [Tooltip("把做好的【正确木块】预制体拖到这里")]
    public GameObject[] goodWoodPrefabs;

    [Tooltip("把做好的【朽木】预制体拖到这里")]
    public GameObject[] badWoodPrefabs;

    [Header("生成规则")]
    [Tooltip("每次掉落的时间间隔（秒）")]
    public float spawnInterval = 1.5f;

    [Tooltip("生成的横向范围（限制在屏幕宽度内）")]
    public float spawnRangeX = 7f;

    [Tooltip("掉落朽木的概率 (0 到 1 之间，0.3 表示 30% 是朽木)")]
    [Range(0f, 1f)]
    public float badWoodChance = 0.3f;

    private float timer;

    void Update()
    {
        // 1. 计时器累加时间
        timer += Time.deltaTime;

        // 2. 当时间到达设定的间隔时，执行生成逻辑
        if (timer >= spawnInterval)
        {
            SpawnWood();
            timer = 0f; // 重置计时器，准备下一次生成
        }
    }

    void SpawnWood()
    {
        GameObject[] selectedPool;

        // 3. 掷骰子决定这次掉落好木头还是坏木头
        // Random.value 会产生一个 0.0 到 1.0 之间的随机小数
        if (Random.value <= badWoodChance && badWoodPrefabs.Length > 0)
        {
            selectedPool = badWoodPrefabs;
        }
        else if (goodWoodPrefabs.Length > 0)
        {
            selectedPool = goodWoodPrefabs;
        }
        else
        {
            Debug.LogWarning("生成器里没有放入任何预制体！");
            return; // 数组为空时直接退出，防止报错
        }

        // 4. 从选好的类别里，随机抽选一个具体的图片样式
        int randomIndex = Random.Range(0, selectedPool.Length);
        GameObject prefabToSpawn = selectedPool[randomIndex];

        // 5. 决定掉落的具体位置（X轴随机，Y轴为生成器本身的高度）
        float randomX = Random.Range(-spawnRangeX, spawnRangeX);
        Vector3 spawnPosition = new Vector3(randomX, transform.position.y, 0f);

        // 6. 核心魔法：在场景中实例化（克隆）这个木块！
        Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
    }
}