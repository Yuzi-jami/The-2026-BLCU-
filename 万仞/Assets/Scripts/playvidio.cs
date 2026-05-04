using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class playvidio : MonoBehaviour
{
    public VideoPlayer _videoPlayer;

    void Start()
    {
        _videoPlayer = GetComponent<VideoPlayer>();
        _videoPlayer.playOnAwake = false; // 不自动播放

        // 监听：视频播放完毕 → 自动切场景
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        // 鼠标左键点击 → 播放视频
        if (Input.GetMouseButtonDown(0))
        {
            PlayVideo();
        }
    }

    void PlayVideo()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.Play();
        }
    }

    // 视频播放完后自动调用这里
    void OnVideoFinished(VideoPlayer vp)
    {
        // 切换场景
        SceneManager.LoadScene("Scenes/背景关卡ui/end2");
    }
}