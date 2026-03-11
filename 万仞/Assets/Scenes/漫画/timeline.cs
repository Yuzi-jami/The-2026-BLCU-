using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using  UnityEngine.Playables;
public class timeline : MonoBehaviour
{
    [Header("绑定 Timeline 控制器")]
    [Tooltip("拖入包含 Playable Director 组件的物体")]
    public PlayableDirector director;

    // 标记当前是否处于暂停状态
    private bool isPaused = false;

    void Start()
    {
        // 如果没有手动拖拽，尝试自动获取当前物体上的 PlayableDirector
        if (director == null)
        {
            director = GetComponent<PlayableDirector>();
        }
    }

    void Update()
    {
        // 如果剧情处于暂停状态，并且玩家点击了鼠标左键（或按下空格键）
        if (isPaused && (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)))
        {
            ResumeTimeline();
        }
    }

    /// <summary>
    /// 暂停 Timeline。
    /// 这个方法准备在 Timeline 的 Signal Receiver 中被调用。
    /// </summary>
    public void PauseTimeline()
    {
        if (director != null && director.state == PlayState.Playing)
        {
            director.Pause();
            isPaused = true;
            Debug.Log("剧情已暂停，等待玩家点击...");
        }
    }

    /// <summary>
    /// 恢复播放 Timeline。
    /// </summary>
    public void ResumeTimeline()
    {
        if (director != null && isPaused)
        {
            director.Play();
            isPaused = false;
            Debug.Log("玩家已点击，剧情继续！");
        }
    }
}
