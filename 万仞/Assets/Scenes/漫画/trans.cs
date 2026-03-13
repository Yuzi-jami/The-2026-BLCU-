using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class trans : MonoBehaviour
{
    [Header("把你的9个虚拟摄像机按顺序拖进来")]
    public CinemachineVirtualCamera[] mangaCameras;

    private int currentIndex = 0;

    void Update()
    {
        // 当玩家点击鼠标左键时，切换到下一个镜头
        if (Input.GetMouseButtonDown(0))
        {
            GoToNextPanel();
        }
    }

    public void GoToNextPanel()
    {
        // 如果还没有播完最后一个镜头
        if (currentIndex < mangaCameras.Length - 1)
        {
            // 将当前摄像机的优先级降为 0
            mangaCameras[currentIndex].Priority = 0;
            
            // 索引加 1，指向下一个摄像机
            currentIndex++;
            
            // 将下一个摄像机的优先级提升为 10
            // Cinemachine 会自动平滑地将镜头移动到新摄像机的位置！
            mangaCameras[currentIndex].Priority = 10;
            
            Debug.Log("镜头已移动到第 " + (currentIndex + 1) + " 幕");
        }
        else
        {
            Debug.Log("漫画剧情播放完毕！");
        }
    }
}
